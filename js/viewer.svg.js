export const extensions = ["svg"];
export const isBinary = false;

export function render(
	source,
	context
) {
	ensureSvgViewerStyles();

	const target = context && context.articleElement;
	if (!target) {
		return;
	}

	const renderMode = getRenderMode(target);
	const filePath = context && context.filePath ? context.filePath : "image.svg";
	const svgSource = String(source ?? "");
	const imageUrl = createSvgDataUrl(svgSource, filePath);

	if (context && typeof context.prepareArticle === "function") {
		context.prepareArticle(renderMode === "file" ? "source-view" : target.className);
	}

	if (renderMode === "file") {
		if (context && typeof context.setPageTitle === "function") {
			context.setPageTitle(getFileNameLabel(context, filePath) || "SVG", "SVG");
		}
		target.replaceChildren(createStandaloneCard(imageUrl, filePath, context));
		return;
	}

	target.replaceChildren(createEmbeddedImage(imageUrl, renderMode, filePath));
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
	imageUrl,
	filePath,
	context
) {
	const card = document.createElement("section");
	card.className = "source-card source-card-standalone";

	const bar = document.createElement("div");
	bar.className = "source-bar";

	const languageElement = document.createElement("div");
	languageElement.className = "source-language";
	languageElement.textContent = "SVG";

	const filenameElement = document.createElement("div");
	filenameElement.className = "source-filename";
	filenameElement.textContent = shouldShowSourceFilename(context) ? filePath : "";

	const frame = createImageFrame(imageUrl, filePath, "file");

	bar.append(languageElement, filenameElement);
	card.append(bar, frame);
	return card;
}

function createEmbeddedImage(
	imageUrl,
	renderMode,
	filePath
) {
	return createImageFrame(imageUrl, filePath, renderMode);
}

function createImageFrame(
	imageUrl,
	filePath,
	renderMode
) {
	const frameTag = renderMode === "inline" ? "span" : "div";
	const frame = document.createElement(frameTag);
	frame.className = renderMode === "inline"
		? "wiki-object svg-frame svg-frame-inline"
		: "svg-frame svg-frame-embedded";

	const image = document.createElement("img");
	image.className = "svg-image";
	image.alt = fileNameWithoutExtension(filePath) || "SVG image";
	image.decoding = "async";
	image.loading = "lazy";
	image.src = imageUrl;

	frame.append(image);
	return frame;
}

function createSvgDataUrl(
	source,
	filePath
) {
	const trimmed = String(source || "").trim();
	if (!trimmed) {
		return normalizeImageUrl(filePath);
	}
	if (/^data:image\/svg\+xml/i.test(trimmed)) {
		return trimmed;
	}
	return "data:image/svg+xml;charset=utf-8," + encodeURIComponent(trimmed);
}

function normalizeImageUrl(
	path
) {
	return String(path || "");
}

function getFileNameLabel(
	context,
	filePath
) {
	if (context && typeof context.fileNameLabel === "function") {
		return context.fileNameLabel(filePath);
	}

	const normalizedPath = String(filePath || "").replace(/\\/g, "/");
	return normalizedPath.split("/").pop() || normalizedPath || "SVG";
}

function shouldShowSourceFilename(
	context
) {
	return context && typeof context.showSourceFilename === "boolean"
		? context.showSourceFilename
		: true;
}

function fileNameWithoutExtension(
	path
) {
	const fileName = String(path || "").replace(/\\/g, "/").split("/").pop() || "";
	const dot = fileName.lastIndexOf(".");
	return dot < 0 ? fileName : fileName.slice(0, dot);
}

function ensureSvgViewerStyles(
) {
	if (document.getElementById("svg-viewer-style")) {
		return;
	}

	const style = document.createElement("style");
	style.id = "svg-viewer-style";
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

		.svg-frame {
			display: block;
			width: 100%;
			padding: 1rem;
			background: linear-gradient(180deg, rgba(255, 255, 255, 0.72), rgba(249, 243, 233, 0.62));
		}

		.svg-frame-inline {
			display: inline-flex;
			width: auto;
			max-width: min(18rem, 100%);
			padding: 0.35rem;
			vertical-align: middle;
		}

		.svg-image {
			display: block;
			max-width: 100%;
			height: auto;
			margin: 0 auto;
			border-radius: 0.35rem;
			background: rgba(255, 255, 255, 0.78);
		}

		.svg-frame-inline .svg-image {
			max-height: 5rem;
			width: auto;
		}
	`;
	document.head.append(style);
}

const api = Object.freeze({
	extensions,
	isBinary,
	renderer: { render },
	render
});

export default api;
