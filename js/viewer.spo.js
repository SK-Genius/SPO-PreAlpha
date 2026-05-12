export const extensions = ["spo", "ilt"];
export const isBinary = false;

const MAX_STICKY_LINES = 6;
const TAB_SIZE = 4;
const RAINBOW_INDENT_COLORS = [
	"var(--source-indent-rainbow-1)",
	"var(--source-indent-rainbow-2)",
	"var(--source-indent-rainbow-3)"
];
const OPENING_BRACKET_BY_CLOSER = {
	")": "(",
	"]": "[",
	"}": "{"
};
let activeStandaloneLayoutCleanup = null;
		
		// SPO and ILT both use compound keywords whose trailing parts are plain ids.
		const SPO_KEYWORD_PARTS = new Set([
			"IF",
			"MATCH"
		]);
		
		const ILT_KEYWORD_PARTS = new Set([
			"AS_BOOL",
			"AS_EMPTY",
			"AS_INT",
			"AS_PAIR",
			"AS_RECORD",
			"AS_REF",
			"AS_TYPE",
			"AS_VAR",
			"FROM",
			"IF",
			"IF_NOT_EMPTY"
		]);
		
		const TOKEN_INLINE_STYLES = {
			comment: { color: "var(--tok-comment)", fontStyle: "italic" },
			string: { color: "var(--tok-string)" },
			number: { color: "var(--tok-number)" },
			keyword: { color: "var(--tok-keyword)", fontWeight: "700" },
			enum: { color: "var(--tok-enum)", fontWeight: "700" },
			identifier: { color: "var(--tok-identifier)" },
			punctuation: { color: "var(--tok-punctuation)" }
		};
		
export function render(
	source,
	context
) {
	ensureSourceStyles();
	clearStandaloneSourceLayout();

	const target = context && context.articleElement;
	if (!target) {
		return;
	}

	const renderMode = getRenderMode(target);
	const filePath = context && context.filePath ? context.filePath : "source.spo";
	const language = /\.ilt$/i.test(filePath) ? "ILT" : "SPO";
	const text = String(source ?? "");

	if (context && typeof context.prepareArticle === "function") {
		context.prepareArticle(renderMode === "file" ? "source-view" : target.className);
	}

	if (renderMode === "inline") {
		target.replaceChildren(createInlineCodeElement(text, language));
		return;
	}

	if (renderMode === "embedded") {
		const lineModels = createLineModels(text, language);
		const embeddedView = createEmbeddedCodeElement(lineModels, language, filePath, context);
		target.replaceChildren(embeddedView.element);
		enableStickyLines(
			embeddedView.scrollElement,
			embeddedView.stickyElement,
			embeddedView.linesElement,
			lineModels
		);
		return;
	}

	const lineModels = createLineModels(text, language);
	if (context && typeof context.setPageTitle === "function") {
		context.setPageTitle(getFileNameLabel(context, filePath), language);
	}

	const card = document.createElement("section");
	card.className = "source-card source-card-standalone";

	const bar = document.createElement("div");
	bar.className = "source-bar";

	const languageElement = document.createElement("div");
	languageElement.className = "source-language";
	languageElement.textContent = language;

	const filenameElement = document.createElement("div");
	filenameElement.className = "source-filename";
	if (shouldShowSourceFilename(context)) {
		appendSourceFilename(filenameElement, context, filePath);
	}

	const sourceElement = document.createElement("div");
	sourceElement.className = "source-code";
	sourceElement.style.setProperty(
		"--source-gutter-width",
		Math.max(3, String(lineModels.length).length + 1) + "ch"
	);

	const stickyElement = document.createElement("div");
	stickyElement.className = "source-sticky";
	stickyElement.hidden = true;
	stickyElement.setAttribute("aria-hidden", "true");

	const linesElement = document.createElement("div");
	linesElement.className = "source-lines";
	for (const lineModel of lineModels) {
		linesElement.append(createLineElement(lineModel));
	}

	sourceElement.append(stickyElement, linesElement);

	bar.append(languageElement, filenameElement);
	card.append(bar, sourceElement);
	target.replaceChildren(card);

	installStandaloneSourceLayout(card, bar, sourceElement);
	enableStickyLines(sourceElement, stickyElement, linesElement, lineModels);
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

function createInlineCodeElement(
	source,
	language
) {
	const code = document.createElement("code");
	code.className = "wiki-object source-inline";
	appendHighlightedSource(code, source, language, { inlineStyles: true });
	return code;
}

function createEmbeddedCodeElement(
	lineModels,
	language,
	filePath,
	context
) {
	const pre = document.createElement("pre");
	pre.className = "wiki-object wiki-text-embedded source-embed";
	pre.style.setProperty(
		"--source-gutter-width",
		Math.max(3, String(Math.max(1, lineModels.length)).length + 1) + "ch"
	);

	const linesElement = document.createElement("div");
	linesElement.className = "source-lines source-lines-embedded";
	for (const lineModel of lineModels) {
		linesElement.append(createLineElement(lineModel));
	}

	const stickyElement = document.createElement("div");
	stickyElement.className = "source-sticky";
	stickyElement.hidden = true;
	stickyElement.setAttribute("aria-hidden", "true");

	pre.append(stickyElement, linesElement);

	if (!shouldShowSourceFilename(context)) {
		return {
			element: pre,
			scrollElement: pre,
			stickyElement,
			linesElement
		};
	}

	const card = document.createElement("section");
	card.className = "source-card source-card-embedded";

	const bar = document.createElement("div");
	bar.className = "source-bar";

	const languageElement = document.createElement("div");
	languageElement.className = "source-language";
	languageElement.textContent = language;

	const filenameElement = document.createElement("div");
	filenameElement.className = "source-filename";
	appendSourceFilename(filenameElement, context, filePath);

	bar.append(languageElement, filenameElement);
	card.append(bar, pre);

	return {
		element: card,
		scrollElement: pre,
		stickyElement,
		linesElement
	};
}

function getFileNameLabel(
	context,
	filePath
) {
	if (context && typeof context.fileNameLabel === "function") {
		return context.fileNameLabel(filePath);
	}

	const normalizedPath = String(filePath || "").replace(/\\/g, "/");
	return normalizedPath.split("/").pop() || normalizedPath || "Source";
}

function shouldShowSourceFilename(
	context
) {
	return context && typeof context.showSourceFilename === "boolean"
		? context.showSourceFilename
		: true;
}

function appendSourceFilename(
	parent,
	context,
	filePath
) {
	const text = String(filePath || "").replace(/\\/g, "/");
	if (!text) {
		return;
	}

	if (
		context &&
		typeof context.createSourceFileUrl === "function"
	) {
		const link = document.createElement("a");
		link.className = "source-filename-link";
		link.href = context.createSourceFileUrl(filePath);
		link.textContent = text;
		parent.append(link);
		return;
	}

	parent.textContent = text;
}

function ensureSourceStyles(
) {
	if (document.getElementById("spo-viewer-style")) {
		return;
	}

	const style = document.createElement("style");
	style.id = "spo-viewer-style";
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
		
		function createLineModels(
			source,
			language
		) {
			const tokenLines = splitTokensIntoLines(tokenizeSpo(source, normalizeSpoLanguage(language)));
			const lineModels = tokenLines.map(
				(
					segments,
					index
				) => {
					const text = segments.map((segment) => segment.text).join("");
					
					return {
						number: index + 1,
						segments,
						text,
						indent: countIndent(text),
						leadingTabColumns: collectLeadingTabColumns(text),
						isBlank: text.trim() === "",
						parentIndex: null,
						startsBlock: false,
						leadingOpenerIndices: []
					};
				}
			);
			
			assignLineHierarchy(lineModels);
			assignBracketLinks(lineModels);
			return lineModels;
		}
		
export function appendHighlightedSource(
	parent,
	source,
	language,
	options = null
) {
	const tokens = tokenizeSpo(String(source || ""), normalizeSpoLanguage(language));
	appendHighlightedTokens(parent, tokens, options);
	return parent;
}
		
		function normalizeSpoLanguage(
			language
		) {
			return String(language || "").toUpperCase() === "ILT" ? "ILT" : "SPO";
		}
		
		function tokenizeSpo(
			source,
			language
		) {
			const tokens = [];
			const keywordParts = language === "ILT" ? ILT_KEYWORD_PARTS : SPO_KEYWORD_PARTS;
			let index = 0;
			
			while (index < source.length) {
				if (source[index] === "\"") {
					const endIndex = readSpoStringEnd(source, index);
					tokens.push(
						{
							type: "string",
							text: source.slice(index, endIndex)
						}
					);
					index = endIndex;
					continue;
				}
				
				const punctuationToken = readSpoPunctuation(source, index);
				if (punctuationToken) {
					tokens.push(
						{
							type: "punctuation",
							text: punctuationToken
						}
					);
					index += punctuationToken.length;
					continue;
				}
				
				const keywordToken = readSpoKeyword(source, index);
				if (keywordToken) {
					tokens.push(
						{
							type: "keyword",
							text: keywordToken
						}
					);
					index += keywordToken.length;
					continue;
				}
				
				const keywordPartToken = readSpoKeywordPart(source, index, keywordParts);
				if (keywordPartToken) {
					tokens.push(
						{
							type: "keyword",
							text: keywordPartToken
						}
					);
					index += keywordPartToken.length;
					continue;
				}
				
				const enumToken = readSpoEnum(source, index);
				if (enumToken) {
					tokens.push(
						{
							type: "enum",
							text: enumToken
						}
					);
					index += enumToken.length;
					continue;
				}
				
				const numberToken = readSpoNumber(source, index);
				if (numberToken) {
					tokens.push(
						{
							type: "number",
							text: numberToken
						}
					);
					index += numberToken.length;
					continue;
				}
				
				const identifierToken = readSpoIdentifier(source, index);
				if (identifierToken) {
					tokens.push(
						{
							type: "identifier",
							text: identifierToken
						}
					);
					index += identifierToken.length;
					continue;
				}
				
				tokens.push(
					{
						type: "",
						text: source[index]
					}
				);
				index += 1;
			}
			
			return tokens;
		}
		
		function splitTokensIntoLines(
			tokens
		) {
			if (!tokens.length) {
				return [[]];
			}
			
			const lines = [[]];
			
			for (const token of tokens) {
				let index = 0;
				let segmentStart = 0;
				
				while (index < token.text.length) {
					const lineBreakLength = readLineBreakLength(token.text, index);
					if (!lineBreakLength) {
						index += 1;
						continue;
					}
					
					pushLineSegment(
						lines[lines.length - 1],
						token.type,
						token.text.slice(segmentStart, index)
					);
					lines.push([]);
					index += lineBreakLength;
					segmentStart = index;
				}
				
				pushLineSegment(
					lines[lines.length - 1],
					token.type,
					token.text.slice(segmentStart)
				);
			}
			
			return lines;
		}
		
		function pushLineSegment(
			line,
			type,
			text
		) {
			if (!text) {
				return;
			}
			
			line.push({ type, text });
		}
		
		function assignLineHierarchy(
			lineModels
		) {
			const ancestorStack = [];
			
			for (let index = 0; index < lineModels.length; index += 1) {
				const lineModel = lineModels[index];
				if (lineModel.isBlank) {
					continue;
				}
				
				while (
					ancestorStack.length &&
					lineModels[ancestorStack[ancestorStack.length - 1]].indent >= lineModel.indent
				) {
					ancestorStack.pop();
				}
				
				lineModel.parentIndex = ancestorStack.length
					? ancestorStack[ancestorStack.length - 1]
					: null;
				
				if (lineModel.parentIndex !== null) {
					lineModels[lineModel.parentIndex].startsBlock = true;
				}
				
				ancestorStack.push(index);
			}
		}
		
		function assignBracketLinks(
			lineModels
		) {
			const openerStack = [];
			
			for (let lineIndex = 0; lineIndex < lineModels.length; lineIndex += 1) {
				const lineModel = lineModels[lineIndex];
				let prefixState = 0;
				
				for (const segment of lineModel.segments) {
					for (const char of segment.text) {
						const isLeadingCloser = isLeadingCloserChar(char, prefixState);
						
						if (prefixState === 0) {
							if (char === " " || char === "\t") {
								// keep scanning indentation
							} else if (isClosingBracket(char)) {
								prefixState = 1;
							} else {
								prefixState = 2;
							}
						} else if (prefixState === 1 && !isClosingBracket(char)) {
							prefixState = 2;
						}
						
						if (segment.type === "string") {
							continue;
						}
						
						if (isOpeningBracket(char)) {
							openerStack.push({
								char,
								lineIndex
							});
							continue;
						}
						
						if (!isClosingBracket(char)) {
							continue;
						}
						
						const openerLineIndex = popMatchingOpenerLine(openerStack, char);
						if (
							isLeadingCloser &&
							openerLineIndex !== null &&
							openerLineIndex !== lineIndex
						) {
							lineModel.leadingOpenerIndices.push(openerLineIndex);
						}
					}
				}
			}
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

		function collectLeadingTabColumns(
			text
		) {
			const columns = [];
			let indent = 0;

			for (const char of text) {
				if (char === " ") {
					indent += 1;
					continue;
				}

				if (char === "\t") {
					indent += TAB_SIZE;
					columns.push(indent);
					continue;
				}

				break;
			}

			return columns;
		}
		
		function createLineElement(
			lineModel,
			sticky = false
		) {
			const lineElement = document.createElement("div");
			lineElement.className = "source-line";
			lineElement.dataset.line = String(lineModel.number);
			
			if (lineModel.isBlank) {
				lineElement.classList.add("is-blank");
			}
			
			if (lineModel.startsBlock) {
				lineElement.classList.add("is-block-start");
			}
			
			if (sticky) {
				lineElement.classList.add("is-sticky");
			}
			
			const gutterElement = document.createElement("span");
			gutterElement.className = "source-gutter";
			gutterElement.textContent = String(lineModel.number);

			const separatorElement = document.createElement("span");
			separatorElement.className = "source-separator";
			separatorElement.setAttribute("aria-hidden", "true");
			
			const textElement = document.createElement("span");
			textElement.className = "source-line-text";
			if (lineModel.isBlank) {
				textElement.classList.add("is-empty");
			}
			applyRainbowIndentBackground(textElement, lineModel.leadingTabColumns);
			appendHighlightedSegments(textElement, lineModel.segments);
			
			lineElement.append(gutterElement, separatorElement, textElement);
			return lineElement;
		}

		function applyRainbowIndentBackground(
			element,
			leadingTabColumns
		) {
			const layers = [];
			const tabStops = Array.isArray(leadingTabColumns) ? leadingTabColumns : [];

			for (let level = tabStops.length; level >= 0; level -= 1) {
				const color = RAINBOW_INDENT_COLORS[level % RAINBOW_INDENT_COLORS.length];
				const column = level === 0 ? 0 : tabStops[level - 1];
				layers.push(
					"linear-gradient(to right, transparent 0, transparent " +
						column +
						"ch, " +
						color +
						" " +
						column +
						"ch, " +
						color +
						" 100%)"
				);
			}

			element.style.backgroundImage = layers.join(", ");
			element.style.backgroundRepeat = "no-repeat";
		}
		
		function appendHighlightedSegments(
			parent,
			segments
		) {
			for (const segment of segments) {
				if (!segment.type) {
					parent.append(document.createTextNode(segment.text));
					continue;
				}
				
				parent.append(createTokenNode(segment.type, segment.text));
			}
		}
		
		function appendHighlightedTokens(
			parent,
			tokens,
			options = null
		) {
			for (const token of tokens) {
				if (!token.type) {
					parent.append(document.createTextNode(token.text));
					continue;
				}
				
				parent.append(createTokenNode(token.type, token.text, options));
			}
		}
		
		function enableStickyLines(
			sourceElement,
			stickyElement,
			linesElement,
			lineModels
		) {
			const lineElements = Array.from(linesElement.children);
			let scheduled = false;
			let lastStickyKey = "";
			
			const syncStickyLines = () => {
				scheduled = false;
				
				stickyElement.style.width = Math.max(
					sourceElement.scrollWidth,
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
					uniqueStickyIndices(baseStickyLineIndices),
					lineModels
				).slice(-MAX_STICKY_LINES);
				
				const stickyKey = stickyLineIndices.join(",");
				if (stickyKey === lastKey) {
					break;
				}
				
				lastKey = stickyKey;
				effectiveOffset = scrollTop + measureStickyLineHeight(
					stickyLineIndices,
					lineElements
				);
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
		
		function uniqueStickyIndices(
			indices
		) {
			const uniqueIndices = [];
			const seen = new Set();
			
			for (const index of indices) {
				if (seen.has(index)) {
					continue;
				}
				
				seen.add(index);
				uniqueIndices.push(index);
			}
			
			return uniqueIndices;
		}
		
		function measureStickyLineHeight(
			indices,
			lineElements
		) {
			let height = 0;
			
			for (const index of indices) {
				height += lineElements[index].offsetHeight;
			}
			
			return height;
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
		
		function isOpeningBracket(
			char
		) {
			return char === "(" || char === "[" || char === "{";
		}
		
		function isClosingBracket(
			char
		) {
			return char === ")" || char === "]" || char === "}";
		}
		
		function isLeadingCloserChar(
			char,
			prefixState
		) {
			return (prefixState === 0 || prefixState === 1) && isClosingBracket(char);
		}
		
		function popMatchingOpenerLine(
			openerStack,
			closerChar
		) {
			const openerChar = OPENING_BRACKET_BY_CLOSER[closerChar];
			if (!openerChar) {
				return null;
			}
			
			for (let index = openerStack.length - 1; index >= 0; index -= 1) {
				if (openerStack[index].char !== openerChar) {
					continue;
				}
				
				const [entry] = openerStack.splice(index, 1);
				return entry.lineIndex;
			}
			
			return null;
		}
		
		function createTokenNode(
			type,
			text,
			options = null
		) {
			const span = document.createElement("span");
			span.className = "tok-" + type;
			span.textContent = text;
			if (options && options.inlineStyles) {
				applyInlineTokenStyle(span, type);
			}
			return span;
		}
		
		function applyInlineTokenStyle(
			element,
			type
		) {
			const styles = TOKEN_INLINE_STYLES[type];
			if (!styles) {
				return;
			}
			
			for (const [key, value] of Object.entries(styles)) {
				element.style[key] = value;
			}
		}
		
		function readLineBreakLength(
			text,
			index
		) {
			if (text[index] === "\r" && text[index + 1] === "\n") {
				return 2;
			}
			
			if (text[index] === "\r" || text[index] === "\n") {
				return 1;
			}
			
			return 0;
		}
		
		function readSpoStringEnd(
			text,
			startIndex
		) {
			const multilineStart = readSpoLineBreak(text, startIndex + 1);
			if (multilineStart !== null) {
				let index = multilineStart;
				
				while (index < text.length) {
					index = skipSpoInlineWhitespace(text, index);
					
					if (text[index] === "\"") {
						return index + 1;
					}
					
					if (text[index] !== "|") {
						return text.length;
					}
					
					index += 1;
					while (index < text.length && text[index] !== "\r" && text[index] !== "\n") {
						index += 1;
					}
					
					const lineBreak = readSpoLineBreak(text, index);
					if (lineBreak === null) {
						return text.length;
					}
					
					index = lineBreak;
				}
				
				return text.length;
			}
			
			let index = startIndex + 1;
			
			while (index < text.length) {
				if (text[index] === "\"") {
					return index + 1;
				}
				
				index += 1;
			}
			
			return text.length;
		}
		
		function readSpoKeyword(
			text,
			startIndex
		) {
			return readSpoPrefixedId(text, startIndex, "\u00A7");
		}
		
		function readSpoKeywordPart(
			text,
			startIndex,
			keywordParts
		) {
			const idToken = readSpoId(text, startIndex);
			return idToken && keywordParts.has(idToken) ? idToken : "";
		}
		
		function readSpoEnum(
			text,
			startIndex
		) {
			return readSpoPrefixedId(text, startIndex, "#");
		}
		
		function readSpoNumber(
			text,
			startIndex
		) {
			const match = /^[+-]?\d(?:_*\d)*/.exec(text.slice(startIndex));
			return match ? match[0] : "";
		}
		
		function readSpoIdentifier(
			text,
			startIndex
		) {
			return readSpoId(text, startIndex);
		}
		
		function readSpoPunctuation(
			text,
			startIndex
		) {
			if (text.startsWith("...", startIndex)) {
				return "";
			}
			
			if (text.startsWith("=>", startIndex)) {
				return text.slice(startIndex, startIndex + 2);
			}
			
			if (text.startsWith("..", startIndex) && !text.startsWith("...", startIndex)) {
				return "..";
			}
			
			return isSpoPunctuation(text[startIndex]) ? text[startIndex] : "";
		}
		
		function isSpoPunctuation(
			char
		) {
			return char === "(" ||
				char === ")" ||
				char === "[" ||
				char === "]" ||
				char === "{" ||
				char === "}" ||
				char === "." ||
				char === "," ||
				char === ":" ||
				char === ";" ||
				char === "|" ||
				char === "\u20AC";
		}
		
		function readSpoPrefixedId(
			text,
			startIndex,
			prefix
		) {
			if (text[startIndex] !== prefix) {
				return "";
			}
			
			const idToken = readSpoId(text, startIndex + 1);
			return idToken ? prefix + idToken : "";
		}
		
		function readSpoId(
			text,
			startIndex
		) {
			let index = startIndex;
			let consumed = false;
			
			while (index < text.length) {
				if (text.startsWith("...", index)) {
					index += 3;
					consumed = true;
					continue;
				}
				
				if (isSpoSpecialChar(text[index])) {
					break;
				}
				
				index += 1;
				consumed = true;
			}
			
			return consumed ? text.slice(startIndex, index) : "";
		}
		
		function isSpoSpecialChar(
			char
		) {
			return char === "#" ||
				char === "\u00A7" ||
				char === "\u20AC" ||
				char === "\"" ||
				char === "." ||
				char === ":" ||
				char === "," ||
				char === ";" ||
				char === "|" ||
				char === "(" ||
				char === ")" ||
				char === "[" ||
				char === "]" ||
				char === "{" ||
				char === "}" ||
				char === " " ||
				char === "\t" ||
				char === "\n" ||
				char === "\r";
		}
		
		function skipSpoInlineWhitespace(
			text,
			startIndex
		) {
			let index = startIndex;
			while (text[index] === " " || text[index] === "\t" || text[index] === "\r") {
				index += 1;
			}
			
			return index;
		}
		
		function readSpoLineBreak(
			text,
			startIndex
		) {
			if (text[startIndex] === "\r" && text[startIndex + 1] === "\n") {
				return startIndex + 2;
			}
			
			if (text[startIndex] === "\n") {
				return startIndex + 1;
			}
			
			return null;
		}
		
export function disposeStandaloneLayout(
) {
	clearStandaloneSourceLayout();
}

export function isSpoLikeFile(
	path
) {
	return /\.(spo|ilt)$/i.test(path);
}

const api = Object.freeze({
	extensions,
	isBinary,
	renderer: { render },
	appendHighlightedSource,
	disposeStandaloneLayout,
	render,
	isSpoLikeFile
});

if (typeof window !== "undefined") {
	window.SpoViewer = api;
}

export default api;
