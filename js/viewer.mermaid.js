(function () {
	const CUSTOM_ELEMENT_NAME = "spo-mermaid";
	const MERMAID_BUNDLE_PATH = "js/vendor/mermaid/mermaid.min.js";
	const MERMAID_EXTENSIONS = ["mermaid", "mmd"];
	let mermaidLoadPromise = null;
	let mermaidConfigured = false;
	let diagramCounter = 0;

	function render(
		source,
		context
	) {
		context.prepareArticle("source-view");

		const title = context.fileNameLabel(context.filePath) || "Mermaid";
		context.setPageTitle(title, "Mermaid");

		const card = document.createElement("section");
		card.className = "source-card";

		const bar = document.createElement("div");
		bar.className = "source-bar";

		const languageElement = document.createElement("div");
		languageElement.className = "source-language";
		languageElement.textContent = "MERMAID";

		const filenameElement = document.createElement("div");
		filenameElement.className = "source-filename";
		filenameElement.textContent = context.showSourceFilename ? context.filePath : "";

		const panel = document.createElement("div");
		panel.style.padding = "1rem";
		panel.style.background =
			"linear-gradient(180deg, rgba(255, 255, 255, 0.72), rgba(249, 243, 233, 0.62))";

		const diagramElement = document.createElement(CUSTOM_ELEMENT_NAME);
		diagramElement.style.display = "block";
		diagramElement.source = typeof source === "string" ? source : "";

		panel.append(diagramElement);
		bar.append(languageElement, filenameElement);
		card.append(bar, panel);
		context.articleElement.append(card);
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
						frame.style.overflowX = "auto";
						frame.style.padding = "0.35rem";
						frame.style.border = "1px solid rgba(125, 102, 78, 0.22)";
						frame.style.borderRadius = "14px";
						frame.style.background = "rgba(255, 255, 255, 0.88)";
						frame.style.boxShadow = "inset 0 1px 0 rgba(255, 255, 255, 0.45)";

						const graph = document.createElement("div");
						graph.style.minWidth = "max-content";
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
		element.textContent = message;
		element.style.padding = "0.9rem 1rem";
		element.style.border = "1px solid " +
			(isError ? "rgba(185, 28, 28, 0.28)" : "rgba(125, 102, 78, 0.2)");
		element.style.borderRadius = "12px";
		element.style.background = isError
			? "rgba(254, 242, 242, 0.96)"
			: "rgba(255, 252, 247, 0.96)";
		element.style.color = isError ? "#991b1b" : "var(--muted)";
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

	function isMermaidLikeFile(
		path
	) {
		return /\.(mermaid|mmd)$/i.test(path);
	}

	const viewer = {
		render,
		isMermaidLikeFile
	};

	ensureCustomElement();
	window.MermaidViewer = viewer;

	if (window.ViewerHost && typeof window.ViewerHost.registerViewer === "function") {
		window.ViewerHost.registerViewer(MERMAID_EXTENSIONS, viewer);
	}
})();
