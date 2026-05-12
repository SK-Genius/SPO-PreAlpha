export const extensions = ["png", "jpg", "jpeg", "gif", "webp", "avif", "bmp", "ico"];
export const isBinary = true;

const MIME_BY_EXTENSION = {
	png: "image/png",
	jpg: "image/jpeg",
	jpeg: "image/jpeg",
	gif: "image/gif",
	webp: "image/webp",
	avif: "image/avif",
	bmp: "image/bmp",
	ico: "image/x-icon"
};

export function render(
	source,
	context
) {
	ensureImageViewerStyles();

	const target = context && context.articleElement;
	if (!target) {
		return;
	}

	const renderMode = getRenderMode(target);
	const filePath = context && context.filePath ? context.filePath : "image";
	const ext = getFileExtension(filePath);
	const imageUrl = resolveImageUrl(String(source ?? ""), filePath, ext);

	if (context && typeof context.prepareArticle === "function") {
		context.prepareArticle(renderMode === "file" ? "source-view" : target.className);
	}

	if (renderMode === "file") {
		if (context && typeof context.setPageTitle === "function") {
			context.setPageTitle(getFileNameLabel(context, filePath) || "Image", "Image");
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
	languageElement.textContent = "IMAGE";

	const filenameElement = document.createElement("div");
	filenameElement.className = "source-filename";
	filenameElement.textContent = shouldShowSourceFilename(context) ? filePath : "";

	bar.append(languageElement, filenameElement);
	card.append(bar, createImageFrame(imageUrl, filePath, "file"));
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
		? "wiki-object image-frame image-frame-inline"
		: "image-frame image-frame-embedded";

	const image = document.createElement("img");
	image.className = "rendered-image";
	image.alt = fileNameWithoutExtension(filePath) || "Image";
	image.decoding = "async";
	image.loading = "lazy";
	image.src = imageUrl;

	const status = document.createElement("span");
	status.className = "image-status";
	status.textContent = "Bild wird geladen ...";

	image.addEventListener("load", () => {
		status.remove();
	});

	image.addEventListener("error", () => {
		status.textContent = "Bild konnte nicht geladen werden.";
		frame.classList.add("has-error");
	});

	frame.append(image, status);
	return frame;
}

function resolveImageUrl(
	source,
	filePath,
	extension
) {
	const trimmed = String(source || "").trim();
	if (!trimmed) {
		return String(filePath || "");
	}
	if (/^data:image\//i.test(trimmed)) {
		return trimmed;
	}

	const mimeType = MIME_BY_EXTENSION[extension] || "application/octet-stream";
	const compact = trimmed.replace(/\s+/g, "");
	if (/^[A-Za-z0-9+/=]+$/.test(compact)) {
		return "data:" + mimeType + ";base64," + compact;
	}

	return trimmed;
}

function getFileNameLabel(
	context,
	filePath
) {
	if (context && typeof context.fileNameLabel === "function") {
		return context.fileNameLabel(filePath);
	}

	const normalizedPath = String(filePath || "").replace(/\\/g, "/");
	return normalizedPath.split("/").pop() || normalizedPath || "Image";
}

function shouldShowSourceFilename(
	context
) {
	return context && typeof context.showSourceFilename === "boolean"
		? context.showSourceFilename
		: true;
}

function getFileExtension(
	path
) {
	const fileName = String(path || "").replace(/\\/g, "/").split("/").pop() || "";
	const dot = fileName.lastIndexOf(".");
	return dot < 0 ? "" : fileName.slice(dot + 1).toLowerCase();
}

function fileNameWithoutExtension(
	path
) {
	const fileName = String(path || "").replace(/\\/g, "/").split("/").pop() || "";
	const dot = fileName.lastIndexOf(".");
	return dot < 0 ? fileName : fileName.slice(0, dot);
}

function ensureImageViewerStyles(
) {
	if (document.getElementById("image-viewer-style")) {
		return;
	}

	const style = document.createElement("style");
	style.id = "image-viewer-style";
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

		.image-frame {
			position: relative;
			display: block;
			width: 100%;
			padding: 1rem;
			background: linear-gradient(180deg, var(--frame-bg-start), var(--frame-bg-end));
		}

		.image-frame-inline {
			display: inline-flex;
			width: auto;
			max-width: min(18rem, 100%);
			padding: 0.35rem;
			vertical-align: middle;
		}

		.rendered-image {
			display: block;
			max-width: 100%;
			height: auto;
			margin: 0 auto;
			border-radius: 0.35rem;
			background: var(--image-bg);
		}

		.image-frame-inline .rendered-image {
			max-height: 5rem;
			width: auto;
		}

		.image-status {
			display: inline-block;
			margin-top: 0.6rem;
			color: var(--status-text);
			font-family: "Palatino Linotype", "Book Antiqua", Palatino, serif;
		}

		.image-frame-inline .image-status {
			margin-top: 0;
			margin-left: 0.5rem;
			font-size: 0.85em;
		}

		.image-frame.has-error .rendered-image {
			display: none;
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
