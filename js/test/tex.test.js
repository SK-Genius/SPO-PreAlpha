import * as tex from "../viewer.tex.js";

// Import and run in a browser served from the repository root.
export async function run() {
	let checks = 0;
	const assert = (condition, message) => {
		if (!condition) throw new Error(message);
		checks++;
	};
	const target = document.createElement("article");
	target.style.width = "320px";
	document.body.append(target);
	let title = "";
	const render = (source, mode = "file") => {
		target.className = mode === "file" ? "" : "wiki-" + mode;
		tex.render(source, {
			articleElement: target,
			filePath: "formulas/rule.tex",
			isEmbedded: mode !== "file",
			prepareArticle(className) { target.className = className; target.replaceChildren(); },
			setPageTitle(value) { title = value; }
		});
	};
	try {
		const rule = String.raw`\frac{\rho(y)=\ell\quad H(\ell)=v}{\rho[x\mapsto v]}\quad\text{(E-VarGet)}`;
		for (const mode of ["file", "embedded-block", "resource-block", "embedded-inline", "resource-inline"]) {
			title = "";
			render(rule, mode);
			assert(target.querySelector(".katex"), mode + ": formula is typeset");
			assert(target.querySelector("math annotation").textContent === rule, mode + ": accessible TeX source retained");
			assert(Boolean(target.querySelector(".katex-display")) === !mode.endsWith("inline"), mode + ": correct display mode");
			assert(title === (mode === "file" ? "rule.tex" : ""), mode + ": only a standalone formula sets the page title");
		}
		const stylesheet = document.querySelector('link[href$="/vendor/katex/katex.min.css"]');
		if (!stylesheet.sheet) await new Promise((resolve, reject) => {
			stylesheet.addEventListener("load", resolve, { once: true });
			stylesheet.addEventListener("error", reject, { once: true });
		});
		await document.fonts.ready;
		assert(document.querySelectorAll("#tex-viewer-style").length === 1, "Viewer styles are installed once");
		assert(document.querySelectorAll('link[href$="/vendor/katex/katex.min.css"]').length === 1, "Font stylesheet is loaded once");
		render(String.raw`\frac{\rho(y)=\ell\quad H(\ell)=v}{\langle x:=\S\mathtt{VAR}\ y\to;C,\rho,H\rangle\longrightarrow\langle C,\rho[x\mapsto v],H\rangle}`, "embedded-block");
		assert(target.scrollWidth <= target.clientWidth + 1, "Wide rules scroll inside the formula instead of widening the article");
		render(String.raw`\notARealCommand{<img src=x onerror=alert(1)>}`, "embedded-block");
		assert(target.querySelector(".tex-error")?.textContent.includes("\\notARealCommand"), "Invalid input displays a diagnostic and the original source");
		assert(!target.querySelector("img,script"), "Invalid TeX is kept as text");
		render(String.raw`\href{javascript:alert(1)}{run}`, "embedded-inline");
		assert(!target.querySelector("a,script"), "TeX cannot insert a trusted link");
		render("x+1", "embedded-inline");
		assert(target.querySelector(".katex") && !target.querySelector(".tex-error"), "A valid formula replaces an earlier error");
		return { checks };
	} finally {
		target.remove();
	}
}
