export const TAB_SIZE = 4;
const MAX_STICKY_LINES = 6;
let activeStandaloneLayoutCleanup = null;

// createLines supplies text, optional indent/bracket links and format-specific data.
// appendLine draws the contents; this module adds gutters, layout and sticky copies.
export function renderSource(source, context, {
	language,
	createLines = text => text.split(/\r\n|\r|\n/).map(text => ({ text })),
	appendLine = (parent, line) => parent.append(document.createTextNode(line.text)),
	appendInline = null
}) {
	const target = context?.articleElement;
	if (!target) return;
	ensureSourceStyles();
	const inline = target.classList.contains("wiki-embedded-inline") || target.classList.contains("wiki-resource-inline");
	const embedded = target.classList.contains("wiki-embedded-block") || target.classList.contains("wiki-resource-block");
	const text = String(source ?? "");
	const filePath = context.filePath || "source." + language.toLowerCase();
	context.prepareArticle?.(inline || embedded ? target.className : "source-view");
	if (inline) {
		const code = document.createElement("code");
		code.className = "wiki-object source-inline";
		if (appendInline) appendInline(code, text);
		else createLines(text).forEach((line, index) => {
			if (index) code.append(document.createTextNode("\n"));
			appendLine(code, line);
		});
		target.replaceChildren(code);
		return;
	}
	const lineModels = createLines(text).map((line, index) => ({
		...line,
		number: index + 1,
		indent: line.indent ?? countIndent(line.text),
		isBlank: line.isBlank ?? line.text.trim() === "",
		parentIndex: null,
		startsBlock: false,
		leadingOpenerIndices: line.leadingOpenerIndices || []
	}));
	const ancestors = [];
	for (const [index, line] of lineModels.entries()) {
		if (line.isBlank) continue;
		while (ancestors.length && lineModels[ancestors.at(-1)].indent >= line.indent) ancestors.pop();
		line.parentIndex = ancestors.at(-1) ?? null;
		if (line.parentIndex !== null) lineModels[line.parentIndex].startsBlock = true;
		ancestors.push(index);
	}
	function createLineElement(line, sticky = false) {
		const element = document.createElement("div");
		element.className = "source-line";
		element.dataset.line = String(line.number);
		element.classList.toggle("is-blank", line.isBlank);
		element.classList.toggle("is-block-start", line.startsBlock);
		element.classList.toggle("is-sticky", sticky);
		const gutter = document.createElement("span");
		gutter.className = "source-gutter";
		gutter.textContent = String(line.number);
		const separator = document.createElement("span");
		separator.className = "source-separator";
		separator.setAttribute("aria-hidden", "true");
		const content = document.createElement("span");
		content.className = "source-line-text";
		content.classList.toggle("is-empty", line.isBlank);
		appendLine(content, line);
		element.append(gutter, separator, content);
		return element;
	}
	const scroll = document.createElement(embedded ? "pre" : "div");
	scroll.className = embedded ? "wiki-object wiki-text-embedded source-embed" : "source-code";
	scroll.style.setProperty("--source-gutter-width", Math.max(3, String(lineModels.length).length + 1) + "ch");
	const sticky = document.createElement("div");
	sticky.className = "source-sticky";
	sticky.hidden = true;
	sticky.setAttribute("aria-hidden", "true");
	const lines = document.createElement("div");
	lines.className = embedded ? "source-lines source-lines-embedded" : "source-lines";
	for (const line of lineModels) lines.append(createLineElement(line));
	scroll.append(sticky, lines);
	if (!embedded || context.showSourceFilename !== false) {
		const card = document.createElement("section");
		card.className = "source-card " + (embedded ? "source-card-embedded" : "source-card-standalone");
		const bar = document.createElement("div");
		bar.className = "source-bar";
		const label = document.createElement("div");
		label.className = "source-language";
		label.textContent = language;
		const filename = document.createElement("div");
		filename.className = "source-filename";
		if (context.showSourceFilename !== false) {
			const name = String(filePath).replace(/\\/g, "/");
			if (context.createSourceFileUrl) {
				const link = document.createElement("a");
				link.className = "source-filename-link";
				link.href = context.createSourceFileUrl(filePath);
				link.textContent = name;
				filename.append(link);
			} else filename.textContent = name;
		}
		bar.append(label, filename);
		card.append(bar, scroll);
		target.replaceChildren(card);
		if (!embedded) {
			clearStandaloneSourceLayout();
			installStandaloneSourceLayout(card, bar, scroll);
			const title = context.fileNameLabel?.(filePath) || String(filePath).replace(/\\/g, "/").split("/").pop();
			context.setPageTitle?.(title, language);
		}
	} else target.replaceChildren(scroll);
	enableStickyLines(scroll, sticky, lines, lineModels, createLineElement);
}

export function appendTokens(parent, tokens, options = null) {
	const styles = options?.inlineStyles ? {
		comment: { color: "var(--tok-comment)", fontStyle: "italic" },
		string: { color: "var(--tok-string)" },
		number: { color: "var(--tok-number)" },
		keyword: { color: "var(--tok-keyword)", fontWeight: "700" },
		enum: { color: "var(--tok-enum)", fontWeight: "700" },
		identifier: { color: "var(--tok-identifier)" },
		punctuation: { color: "var(--tok-punctuation)" }
	} : null;
	for (const token of tokens) {
		if (!token.type) parent.append(document.createTextNode(token.text));
		else {
			const span = document.createElement("span");
			span.className = "tok-" + token.type;
			span.textContent = token.text;
			if (styles) Object.assign(span.style, styles[token.type]);
			parent.append(span);
		}
	}
	return parent;
}


function ensureSourceStyles() {
	if (document.getElementById("source-viewer-style")) return;
	const style = document.createElement("style");
	style.id = "source-viewer-style";
	style.textContent = `
		.source-view {
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
			color: var(--ink);
		}

		.source-inline,
		.source-code,
		.source-lines,
		.source-sticky,
		.source-line,
		.source-line-text {
			tab-size: ${TAB_SIZE};
		}

		.source-card {
			border: 1px solid var(--card-border);
			border-radius: 0.95rem;
			background: linear-gradient(180deg, var(--card-bg-start), var(--card-bg-end));
			box-shadow: var(--card-shadow);
			overflow: hidden;
		}

		pre.wiki-object.source-embed {
			position: relative;
			margin: 0 0 1rem;
			padding: 0;
			border: 1px solid var(--source-sticky-border);
			border-radius: 0.8rem;
			background: var(--source-embed-bg);
			box-shadow: inset 0 1px 0 var(--surface-highlight);
			max-height: 50vh;
			overflow: auto;
		}

		.source-card-embedded > pre.wiki-object.source-embed {
			margin: 0;
			border: 0;
			border-radius: 0;
			box-shadow: none;
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

		.source-filename-link {
			color: inherit;
			text-decoration: underline;
			text-underline-offset: 0.14em;
		}

		.source-filename-link:hover {
			color: var(--accent);
		}

		.source-code {
			position: relative;
			overflow: auto;
			padding: 0;
			background: var(--source-surface);
		}

		.source-lines,
		.source-sticky {
			font-size: 0.95rem;
			line-height: 1.5;
		}

		.source-lines-embedded {
			padding: 0.5rem 0;
		}

		.source-sticky {
			position: absolute;
			top: 0;
			left: 0;
			right: auto;
			z-index: 1;
			pointer-events: none;
			background: linear-gradient(180deg, var(--source-sticky-start), var(--source-sticky-end));
			border-bottom: 1px solid var(--source-sticky-border);
			box-shadow: var(--source-sticky-shadow);
		}

		.source-sticky::after {
			content: "";
			position: absolute;
			left: 0;
			right: 0;
			bottom: -14px;
			height: 14px;
			background: linear-gradient(180deg, var(--source-sticky-tail-start), transparent);
		}

		.source-line {
			display: grid;
			grid-template-columns: var(--source-gutter-width, 4ch) 1ch minmax(0, 1fr);
			align-items: baseline;
			padding: 0 0.9rem;
			white-space: pre;
		}

		.source-embed .source-line {
			padding-left: 0.75rem;
			padding-right: 0.75rem;
		}

		.source-line.is-sticky {
			background: var(--source-line-sticky);
		}

		.source-line.is-sticky.is-block-start {
			background: var(--source-line-sticky-block);
		}

		.source-line.is-block-start {
			background: var(--source-line-block);
		}

		.source-line:hover {
			background: var(--source-line-hover);
		}

		.source-gutter,
		.source-separator {
			color: var(--source-gutter);
			user-select: none;
		}

		.source-gutter {
			text-align: right;
			padding-right: 0.35rem;
		}

		.source-separator::before {
			content: "|";
		}

		.source-line-text {
			display: block;
			min-width: 0;
		}

		.source-line-text.is-empty {
			align-self: stretch;
			min-height: 1lh;
		}

		.tok-comment { color: var(--tok-comment); font-style: italic; }
		.tok-string { color: var(--tok-string); }
		.tok-number { color: var(--tok-number); }
		.tok-keyword { color: var(--tok-keyword); font-weight: 700; }
		.tok-enum { color: var(--tok-enum); font-weight: 700; }
		.tok-identifier { color: var(--tok-identifier); }
		.tok-punctuation { color: var(--tok-punctuation); }

		.source-lines { width: max-content; min-width: 100%; }
	`;
	document.head.append(style);
}

function clearStandaloneSourceLayout(
) {
	if (!activeStandaloneLayoutCleanup) {
		return;
	}

	const cleanup = activeStandaloneLayoutCleanup;
	activeStandaloneLayoutCleanup = null;
	cleanup();
}

function installStandaloneSourceLayout(
	card,
	bar,
	sourceElement
) {
	let frameId = 0;
	let resizeObserver = null;

	const syncLayout = () => {
		frameId = 0;

		if (!card.isConnected) {
			cleanup(false);
			return;
		}

		const viewportHeight = readViewportHeight();
		const cardTop = Math.max(0, card.getBoundingClientRect().top);
		const headerHeight = bar.offsetHeight;
		const availableHeight = Math.max(
			160,
			Math.floor(viewportHeight - cardTop - headerHeight - 2)
		);

		sourceElement.style.height = availableHeight + "px";
		sourceElement.style.maxHeight = availableHeight + "px";
	};

	const scheduleLayoutSync = () => {
		if (frameId !== 0) {
			return;
		}

		frameId = window.requestAnimationFrame(syncLayout);
	};

	const onResize = () => scheduleLayoutSync();
	const visualViewport = window.visualViewport || null;

	const cleanup = (
		clearInlineStyles = true
	) => {
		if (frameId !== 0) {
			window.cancelAnimationFrame(frameId);
			frameId = 0;
		}

		window.removeEventListener("resize", onResize);
		if (visualViewport) {
			visualViewport.removeEventListener("resize", onResize);
		}
		if (resizeObserver) {
			resizeObserver.disconnect();
			resizeObserver = null;
		}

		if (clearInlineStyles) {
			sourceElement.style.removeProperty("height");
			sourceElement.style.removeProperty("max-height");
		}

		if (activeStandaloneLayoutCleanup === cleanup) {
			activeStandaloneLayoutCleanup = null;
		}
	};

	activeStandaloneLayoutCleanup = cleanup;

	window.addEventListener("resize", onResize, { passive: true });
	if (visualViewport) {
		visualViewport.addEventListener("resize", onResize, { passive: true });
	}
	if (window.ResizeObserver) {
		resizeObserver = new window.ResizeObserver(scheduleLayoutSync);
		resizeObserver.observe(card);
		resizeObserver.observe(bar);
	}

	scheduleLayoutSync();
	window.setTimeout(scheduleLayoutSync, 0);
	window.setTimeout(scheduleLayoutSync, 80);
}

function readViewportHeight(
) {
	return window.visualViewport && window.visualViewport.height
		? window.visualViewport.height
		: window.innerHeight;
}

function countIndent(
	text
) {
	let indent = 0;

	for (const char of text) {
		if (char === " ") {
			indent += 1;
			continue;
		}

		if (char === "\t") {
			indent += TAB_SIZE;
			continue;
		}

		break;
	}

	return indent;
}

function enableStickyLines(
	sourceElement,
	stickyElement,
	linesElement,
	lineModels,
	createLineElement
) {
	const lineElements = Array.from(linesElement.children);
	let scheduled = false;
	let lastStickyKey = "";

	const syncStickyLines = () => {
		scheduled = false;
		if (!sourceElement.isConnected) {
			resizeObserver?.disconnect();
			sourceElement.removeEventListener("scroll", scheduleStickySync);
			return;
		}

		stickyElement.style.width = Math.max(
			linesElement.scrollWidth,
			sourceElement.clientWidth
		) + "px";
		stickyElement.style.transform = "translateY(" + sourceElement.scrollTop + "px)";

		const stickyLineIndices = resolveStickyLineIndices(
			sourceElement.scrollTop,
			lineModels,
			lineElements
		);
		const stickyKey = stickyLineIndices.join(",");

		if (stickyKey !== lastStickyKey) {
			lastStickyKey = stickyKey;
			stickyElement.replaceChildren(
				...stickyLineIndices.map(
					(
						index
					) => createLineElement(lineModels[index], true)
				)
			);
		}

		stickyElement.hidden = stickyLineIndices.length === 0;
	};

	const scheduleStickySync = (
	) => {
		if (scheduled) {
			return;
		}

		scheduled = true;
		window.requestAnimationFrame(syncStickyLines);
	};

	sourceElement.addEventListener("scroll", scheduleStickySync, { passive: true });
	const resizeObserver = window.ResizeObserver ? new ResizeObserver(scheduleStickySync) : null;
	resizeObserver?.observe(sourceElement);
	resizeObserver?.observe(linesElement);
	scheduleStickySync();
}

function resolveStickyLineIndices(
	scrollTop,
	lineModels,
	lineElements
) {
	if (!lineModels.length || !lineElements.length || scrollTop <= 0) {
		return [];
	}

	let stickyLineIndices = [];
	let effectiveOffset = scrollTop;
	let lastKey = "";

	for (let iteration = 0; iteration < MAX_STICKY_LINES + 2; iteration += 1) {
		const baseStickyLineIndices = resolveStickyLineIndicesForOffset(
			effectiveOffset,
			lineModels,
			lineElements
		);
		stickyLineIndices = expandStickyLineIndices(
			[...new Set(baseStickyLineIndices)],
			lineModels
		).slice(-MAX_STICKY_LINES);

		const stickyKey = stickyLineIndices.join(",");
		if (stickyKey === lastKey) {
			break;
		}

		lastKey = stickyKey;
		effectiveOffset = scrollTop + stickyLineIndices.reduce((height, index) => height + lineElements[index].offsetHeight, 0);
	}

	return stickyLineIndices;
}

function resolveStickyLineIndicesForOffset(
	offsetTop,
	lineModels,
	lineElements
) {
	const topLineIndex = findLineIndexAtOffset(lineElements, offsetTop);
	const referenceLineIndex = findReferenceLineIndex(lineModels, topLineIndex);
	if (referenceLineIndex === null) {
		return [];
	}

	const stickyLineIndices = [];
	let parentIndex = lineModels[referenceLineIndex].parentIndex;

	while (parentIndex !== null) {
		stickyLineIndices.unshift(parentIndex);
		parentIndex = lineModels[parentIndex].parentIndex;
	}

	const topLineModel = lineModels[topLineIndex];
	if (
		!topLineModel.isBlank &&
		topLineModel.startsBlock &&
		lineElements[topLineIndex].offsetTop < offsetTop
	) {
		stickyLineIndices.push(topLineIndex);
	}

	if (stickyLineIndices.length === 0) {
		stickyLineIndices.push(findStickyFallbackLineIndex(
			lineModels,
			lineElements,
			topLineIndex,
			offsetTop
		));
	}

	return stickyLineIndices;
}

function findStickyFallbackLineIndex(
	lineModels,
	lineElements,
	topLineIndex,
	offsetTop
) {
	if (!lineModels[topLineIndex].isBlank) {
		return topLineIndex;
	}

	for (let index = topLineIndex - 1; index >= 0; index -= 1) {
		if (!lineModels[index].isBlank && lineElements[index].offsetTop <= offsetTop) {
			return index;
		}
	}

	for (let index = topLineIndex + 1; index < lineModels.length; index += 1) {
		if (!lineModels[index].isBlank) {
			return index;
		}
	}

	return topLineIndex;
}

function findLineIndexAtOffset(
	lineElements,
	scrollTop
) {
	let low = 0;
	let high = lineElements.length - 1;
	let result = 0;

	while (low <= high) {
		const middle = (low + high) >> 1;
		if (lineElements[middle].offsetTop <= scrollTop) {
			result = middle;
			low = middle + 1;
		} else {
			high = middle - 1;
		}
	}

	return result;
}

function findReferenceLineIndex(
	lineModels,
	startIndex
) {
	for (let index = startIndex; index < lineModels.length; index += 1) {
		if (!lineModels[index].isBlank) {
			return index;
		}
	}

	for (let index = startIndex - 1; index >= 0; index -= 1) {
		if (!lineModels[index].isBlank) {
			return index;
		}
	}

	return null;
}

function expandStickyLineIndices(
	indices,
	lineModels
) {
	const expandedIndices = [];
	const added = new Set();
	const visiting = new Set();

	for (const index of indices) {
		appendStickyLineIndex(
			expandedIndices,
			added,
			visiting,
			lineModels,
			index
		);
	}

	return expandedIndices;
}

function appendStickyLineIndex(
	expandedIndices,
	added,
	visiting,
	lineModels,
	index
) {
	if (index === null || added.has(index) || visiting.has(index)) {
		return;
	}

	visiting.add(index);
	for (const openerIndex of lineModels[index].leadingOpenerIndices) {
		appendStickyLineIndex(
			expandedIndices,
			added,
			visiting,
			lineModels,
			openerIndex
		);
	}
	visiting.delete(index);

	if (added.has(index)) {
		return;
	}

	added.add(index);
	expandedIndices.push(index);
}
