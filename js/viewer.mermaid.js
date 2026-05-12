export const extensions = ["mermaid", "mmd"];
export const isBinary = false;

const CUSTOM_ELEMENT_NAME = "spo-mermaid";
const MERMAID_BUNDLE_PATH = "js/vendor/mermaid/mermaid.min.js";
let mermaidLoadPromise = null;
let mermaidConfigured = false;
let mermaidConfiguredTheme = "";
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
	const theme = document.documentElement.dataset.theme === "dark" ? "dark" : "default";
	if (mermaidConfigured && mermaidConfiguredTheme === theme) {
		return;
	}

	mermaid.initialize({
		startOnLoad: false,
		securityLevel: "strict",
		theme
	});
	mermaidConfigured = true;
	mermaidConfiguredTheme = theme;
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
				this._handleThemeChange = () => {
					void this.renderDiagram();
				};
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
				window.addEventListener("viewer-themechange", this._handleThemeChange);
				if (!this._source) {
					this._source = this.textContent || "";
				}

				void this.renderDiagram();
			}

			disconnectedCallback() {
				window.removeEventListener("viewer-themechange", this._handleThemeChange);
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
			color: var(--ink);
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
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
		}

		.mermaid-panel {
			display: block;
			width: 100%;
		}

		.mermaid-panel-embedded {
			padding: 1rem;
			background: linear-gradient(180deg, var(--frame-bg-start), var(--frame-bg-end));
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
			border: 1px solid var(--source-sticky-border);
			border-radius: 14px;
			background: var(--message-bg);
			box-shadow: inset 0 1px 0 var(--surface-highlight);
		}

		.mermaid-graph {
			min-width: max-content;
		}

		.mermaid-message {
			padding: 0.9rem 1rem;
			border: 1px solid var(--message-border);
			border-radius: 12px;
			background: var(--message-bg);
			color: var(--message-text);
			font-family: "Palatino Linotype", "Book Antiqua", Palatino, serif;
		}

		.mermaid-message.is-error {
			border-color: var(--message-error-border);
			background: var(--message-error-bg);
			color: var(--message-error-text);
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
