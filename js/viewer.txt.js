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
	ensureTextViewerStyles();
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

function ensureTextViewerStyles() {
	if (document.getElementById("txt-viewer-style")) return;

	const style = document.createElement("style");
	style.id = "txt-viewer-style";
	style.textContent = `
		.source-view {
			color: var(--ink);
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
		}

		.source-card {
			border: 1px solid var(--card-border);
			border-radius: 0.95rem;
			background: linear-gradient(180deg, var(--card-bg-start), var(--card-bg-end));
			box-shadow: var(--card-shadow);
			overflow: hidden;
		}

		.source-bar {
			display: flex;
			align-items: center;
			gap: 0.85rem;
			padding: 0.75rem 0.9rem;
			border-bottom: 1px solid var(--bar-border);
			background: linear-gradient(180deg, var(--bar-bg-start), var(--bar-bg-end));
		}

		.source-language {
			flex: 0 0 auto;
			font-size: 0.8rem;
			font-weight: 700;
			letter-spacing: 0.08em;
			text-transform: uppercase;
			color: var(--source-language);
		}

		.source-filename {
			flex: 1 1 auto;
			min-width: 0;
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
			color: var(--source-filename);
			font-size: 0.92rem;
		}

		.source-code,
		pre.wiki-object.wiki-text-embedded {
			margin: 0;
			padding: 0.9rem 1rem;
			overflow: auto;
			background: var(--source-surface);
			color: var(--ink);
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
		}

		.source-code {
			border-radius: 0 0 0.95rem 0.95rem;
		}

		pre.wiki-object.wiki-text-embedded {
			margin: 0 0 1rem;
			border: 1px solid var(--source-sticky-border);
			border-radius: 0.8rem;
			background: var(--source-embed-bg);
		}
	`;
	document.head.append(style);
}

export default Object.freeze({
	extensions,
	isBinary,
	renderer: { render }
});

console.log("[txt-viewer] bottom");
