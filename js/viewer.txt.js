console.log("[txt-viewer] top");

export const extensions = ["txt", "text"]; // empty extension is mapped to txt by the caller
export const isBinary = false;

function el(tagName, options = {}, ...children) {
	const element = document.createElement(tagName);
	if (options.className) element.className = options.className;
	if (options.text !== undefined) element.textContent = options.text;
	if (options.attrs) {		for (const [key, value] of Object.entries(options.attrs)) {
			element.setAttribute(key, String(value));
		}
	}
	for (const child of children) if (child) element.append(child);
	return element;
}

export function render(source, context) {
	const target = context && context.articleElement;
	if (!target) return;

	const text = String(source ?? "");
	const filePath = context && context.filePath ? context.filePath : "Text";
	const mode = getRenderMode(target);

	if (context && typeof context.prepareArticle === "function") {
		context.prepareArticle(mode === "file" ? "source-view" : target.className);
	}

	if (mode === "inline") {
		target.replaceChildren(el("code", { className: "wiki-object", text }));
		return;
	}

	if (mode === "embedded") {
		target.replaceChildren(
			el("pre", { className: "wiki-object wiki-text-embedded" },
				el("code", { text })
			)
		);
		return;
	}

	const wrapper = el("section", { className: "source-card source-card-embedded" });
	const bar = el("div", { className: "source-bar" },
		el("div", { className: "source-language", text: "TXT" }),
		el("div", { className: "source-filename", text: filePath })
	);
	const pre = el("pre", { className: "source-code" },
		el("code", { text })
	);

	wrapper.append(bar, pre);
	target.replaceChildren(wrapper);

	if (context && typeof context.setPageTitle === "function") {
		context.setPageTitle(filePath, "Text");
	}
}

function getRenderMode(target) {
	if (target.classList && target.classList.contains("wiki-embedded-inline")) return "inline";
	if (target.classList && target.classList.contains("wiki-resource-inline")) return "inline";
	if (target.classList && target.classList.contains("wiki-embedded-block")) return "embedded";
	if (target.classList && target.classList.contains("wiki-resource-block")) return "embedded";
	return "file";
}

export default Object.freeze({
	extensions,
	isBinary,
	renderer: { render }
});

console.log("[txt-viewer] bottom");
