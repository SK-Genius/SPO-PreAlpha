export const extensions = ["mermaid", "mmd"];
export const isBinary = false;

const CUSTOM_ELEMENT_NAME = "spo-mermaid";
const MERMAID_BUNDLE_PATH = "js/vendor/mermaid/mermaid.min.js";
let mermaidLoadPromise = null;
let mermaidConfigured = false;
let diagramCounter = 0;

export function render(
	source,
	context
) {
	ensureMermaidViewerStyles();
	ensureCustomElement();

	const target = context && context.articleElement;
	if (!target) {
		return;
	}

	const renderMode = getRenderMode(target);
	const filePath = context && context.filePath ? context.filePath : "diagram.mermaid";
	const text = typeof source === "string" ? source : String(source ?? "");

	if (context && typeof context.prepareArticle === "function") {
		context.prepareArticle(renderMode === "file" ? "source-view" : target.className);
	}

	if (renderMode === "file") {
		if (context && typeof context.setPageTitle === "function") {
			context.setPageTitle(getFileNameLabel(context, filePath) || "Mermaid", "Mermaid");
		}

		target.replaceChildren(createStandaloneCard(text, filePath, context));
		return;
	}

	target.replaceChildren(createEmbeddedDiagram(text, renderMode));
}

function getRenderMode(
	target
) {
	if (target.classList && target.classList.contains("wiki-embedded-inline")) return "inline";
	if (target.classList && target.classList.contains("wiki-resource-inline")) return "inline";
	if (target.classList && target.classList.contains("wiki-embedded-block")) return "embedded";
	if (target.classList && target.classList.contains("wiki-resource-block")) return "embedded";
	return "file";
}

function createStandaloneCard(
	source,
	filePath,
	context
) {
	const card = document.createElement("section");
	card.className = "source-card source-card-standalone";

	const bar = document.createElement("div");
	bar.className = "source-bar";

	const languageElement = document.createElement("div");
	languageElement.className = "source-language";
	languageElement.textContent = "MERMAID";

	const filenameElement = document.createElement("div");
	filenameElement.className = "source-filename";
	filenameElement.textContent = shouldShowSourceFilename(context) ? filePath : "";

	bar.append(languageElement, filenameElement);
	card.append(bar, createDiagramPanel(source, "file"));
	return card;
}

function createEmbeddedDiagram(
	source,
	renderMode
) {
	return createDiagramPanel(source, renderMode);
}

function createDiagramPanel(
	source,
	renderMode
) {
	const panelTag = renderMode === "inline" ? "span" : "div";
	const panel = document.createElement(panelTag);
	panel.className = renderMode === "inline"
		? "wiki-object mermaid-panel mermaid-panel-inline"
		: "mermaid-panel mermaid-panel-embedded";

	const diagramElement = document.createElement(CUSTOM_ELEMENT_NAME);
	diagramElement.className = "mermaid-diagram";
	diagramElement.source = typeof source === "string" ? source : "";

	panel.append(diagramElement);
	return panel;
}

function getFileNameLabel(
	context,
	filePath
) {
	if (context && typeof context.fileNameLabel === "function") {
		return context.fileNameLabel(filePath);
	}

	const normalizedPath = String(filePath || "").replace(/\\/g, "/");
	return normalizedPath.split("/").pop() || normalizedPath || "Diagram";
}

function shouldShowSourceFilename(
	context
) {
	return context && typeof context.showSourceFilename === "boolean"
		? context.showSourceFilename
		: true;
}

async function ensureMermaid(
) {
	if (globalThis.mermaid) {
		configureMermaid(globalThis.mermaid);
		return globalThis.mermaid;
	}

	if (!mermaidLoadPromise) {
		mermaidLoadPromise = new Promise((resolve, reject) => {
			const existingScript = document.querySelector("script[data-spo-mermaid-bundle='true']");
			if (existingScript) {
				existingScript.addEventListener(
					"load",
					() => resolve(globalThis.mermaid),
					{ once: true }
				);
				existingScript.addEventListener(
					"error",
					() => reject(new Error("Mermaid konnte nicht geladen werden.")),
					{ once: true }
				);
				return;
			}

			const script = document.createElement("script");
			script.src = MERMAID_BUNDLE_PATH;
			script.async = true;
			script.dataset.spoMermaidBundle = "true";
			script.onload = () => {
				if (!globalThis.mermaid) {
					reject(new Error("Das Mermaid-Bundle hat kein globales API exportiert."));
					return;
				}

				resolve(globalThis.mermaid);
			};
			script.onerror = () => reject(new Error("Mermaid konnte nicht geladen werden."));
			document.head.append(script);
		}).catch((error) => {
			mermaidLoadPromise = null;
			throw error;
		});
	}

	const mermaid = await mermaidLoadPromise;
	configureMermaid(mermaid);
	return mermaid;
}

function configureMermaid(
	mermaid
) {
	if (mermaidConfigured) {
		return;
	}

	mermaid.initialize({
		startOnLoad: false,
		securityLevel: "strict"
	});
	mermaidConfigured = true;
}

function ensureCustomElement(
) {
	if (customElements.get(CUSTOM_ELEMENT_NAME)) {
		return;
	}

	customElements.define(
		CUSTOM_ELEMENT_NAME,
		class extends HTMLElement {
			constructor() {
				super();
				this._source = "";
				this._renderToken = 0;
			}

			get source() {
				return this._source;
			}

			set source(value) {
				this._source = typeof value === "string" ? value : "";
				if (this.isConnected) {
					void this.renderDiagram();
				}
			}

			connectedCallback() {
				if (!this._source) {
					this._source = this.textContent || "";
				}

				void this.renderDiagram();
			}

			async renderDiagram() {
				const renderToken = ++this._renderToken;
				const source = this._source.trim();

				if (!source) {
					this.replaceChildren(createMessageElement("Das Diagramm ist leer."));
					return;
				}

				this.replaceChildren(createMessageElement("Diagramm wird gerendert ..."));

				try {
					const mermaid = await ensureMermaid();
					if (!this.isConnected || renderToken !== this._renderToken) {
						return;
					}

					const diagramId = "spo-mermaid-" + (++diagramCounter);
					const renderResult = await mermaid.render(diagramId, source);
					if (!this.isConnected || renderToken !== this._renderToken) {
						return;
					}

					const frame = document.createElement("div");
					frame.className = "mermaid-frame";

					const graph = document.createElement("div");
					graph.className = "mermaid-graph";
					graph.innerHTML = renderResult.svg;
					frame.append(graph);
					this.replaceChildren(frame);

					const svg = graph.querySelector("svg");
					if (svg) {
						svg.style.display = "block";
						svg.style.maxWidth = "100%";
						svg.style.height = "auto";
					}

					if (typeof renderResult.bindFunctions === "function") {
						renderResult.bindFunctions(graph);
					}
				} catch (error) {
					if (!this.isConnected || renderToken !== this._renderToken) {
						return;
					}

					this.replaceChildren(
						createMessageElement(buildErrorMessage(error), true)
					);
				}
			}
		}
	);
}

function createMessageElement(
	message,
	isError = false
) {
	const element = document.createElement("div");
	element.className = isError ? "mermaid-message is-error" : "mermaid-message";
	element.textContent = message;
	return element;
}

function buildErrorMessage(
	error
) {
	if (error && typeof error.message === "string" && error.message.trim()) {
		return error.message.trim();
	}

	return "Das Diagramm konnte nicht gerendert werden.";
}

function ensureMermaidViewerStyles(
) {
	if (document.getElementById("mermaid-viewer-style")) {
		return;
	}

	const style = document.createElement("style");
	style.id = "mermaid-viewer-style";
	style.textContent = `
		.source-view {
			color: #241d18;
		}

		.source-card {
			border: 1px solid rgba(125, 102, 78, 0.28);
			border-radius: 0.95rem;
			background: linear-gradient(180deg, rgba(255, 255, 255, 0.94), rgba(249, 243, 233, 0.92));
			box-shadow: 0 10px 24px rgba(70, 49, 31, 0.05);
			overflow: hidden;
		}

		.source-bar {
			display: flex;
			align-items: center;
			gap: 0.85rem;
			padding: 0.75rem 0.9rem;
			border-bottom: 1px solid rgba(127, 79, 36, 0.16);
			background: linear-gradient(180deg, rgba(236, 218, 194, 0.98), rgba(221, 198, 170, 0.94));
		}

		.source-language {
			flex: 0 0 auto;
			font-size: 0.8rem;
			font-weight: 700;
			letter-spacing: 0.08em;
			text-transform: uppercase;
			color: #7f4f24;
		}

		.source-filename {
			flex: 1 1 auto;
			min-width: 0;
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
			color: #5a4a3e;
			font-size: 0.92rem;
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
		}

		.mermaid-panel {
			display: block;
			width: 100%;
		}

		.mermaid-panel-embedded {
			padding: 1rem;
			background: linear-gradient(180deg, rgba(255, 255, 255, 0.72), rgba(249, 243, 233, 0.62));
		}

		.mermaid-panel-inline {
			display: inline-block;
			width: min(30rem, 100%);
			max-width: 100%;
			padding: 0.45rem;
			vertical-align: middle;
		}

		.mermaid-diagram {
			display: block;
			width: 100%;
		}

		.mermaid-frame {
			overflow-x: auto;
			padding: 0.35rem;
			border: 1px solid rgba(125, 102, 78, 0.22);
			border-radius: 14px;
			background: rgba(255, 255, 255, 0.88);
			box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.45);
		}

		.mermaid-graph {
			min-width: max-content;
		}

		.mermaid-message {
			padding: 0.9rem 1rem;
			border: 1px solid rgba(125, 102, 78, 0.2);
			border-radius: 12px;
			background: rgba(255, 252, 247, 0.96);
			color: #695a4d;
			font-family: "Palatino Linotype", "Book Antiqua", Palatino, serif;
		}

		.mermaid-message.is-error {
			border-color: rgba(185, 28, 28, 0.28);
			background: rgba(254, 242, 242, 0.96);
			color: #991b1b;
		}
	`;
	document.head.append(style);
}

export function isMermaidLikeFile(
	path
) {
	return /\.(mermaid|mmd)$/i.test(path);
}

const api = Object.freeze({
	extensions,
	isBinary,
	renderer: { render },
	render,
	isMermaidLikeFile
});

if (typeof window !== "undefined") {
	window.MermaidViewer = api;
}

export default api;
