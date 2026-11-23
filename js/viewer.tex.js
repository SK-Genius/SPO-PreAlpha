import katex from "./vendor/katex/katex.mjs";

export const extensions = ["tex"];
export const isBinary = false;

export function render(source, context) {
	const target = context?.articleElement;
	if (!target) return;

	if (!document.getElementById("tex-viewer-style")) {
		const fonts = document.createElement("link");
		fonts.rel = "stylesheet";
		fonts.href = new URL("./vendor/katex/katex.min.css", import.meta.url).href;
		const style = document.createElement("style");
		style.id = "tex-viewer-style";
		style.textContent = `
			.tex-view { min-width: 0; }
			.tex-formula-block { max-width: 100%; overflow-x: auto; padding: 0.8em 0.15em; }
			.tex-formula-block .katex-display { margin: 0; text-align: left; }
			.tex-error { color: var(--message-error-text, #b91c1c); white-space: pre-wrap; }
		`;
		document.head.append(fonts, style);
	}

	const inline = target.matches(".wiki-embedded-inline, .wiki-resource-inline");
	const embedded = inline || context.isEmbedded || target.matches(".wiki-embedded-block, .wiki-resource-block");
	context.prepareArticle?.(embedded ? target.className : "tex-view");
	if (!embedded) {
		const filePath = context.filePath || "formula.tex";
		context.setPageTitle?.(context.fileNameLabel?.(filePath) || filePath.split(/[\\/]/).pop(), "TeX");
	}

	const formula = document.createElement(inline ? "span" : "div");
	formula.className = inline ? "tex-formula-inline" : "tex-formula-block";
	const text = String(source ?? "");
	try {
		katex.render(text, formula, { displayMode: !inline, throwOnError: true, strict: "error", trust: false });
	} catch (error) {
		const message = document.createElement(inline ? "span" : "pre");
		message.className = "tex-error";
		message.textContent = (error instanceof Error ? error.message : String(error)) + "\n\n" + text;
		formula.replaceChildren(message);
	}
	target.replaceChildren(formula);
}

export default Object.freeze({ extensions, isBinary, renderer: { render } });
