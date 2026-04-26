(function () {
	const MAX_STICKY_LINES = 6;
	const TAB_SIZE = 4;
	const OPENING_BRACKET_BY_CLOSER = {
		")": "(",
		"]": "[",
		"}": "{"
	};

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

	function render(source, context) {
		context.prepareArticle("source-view");

		const title = context.fileNameLabel(context.filePath);
		const language = /\.ilt$/i.test(context.filePath) ? "ILT" : "SPO";
		const lineModels = createLineModels(source, language);

		context.setPageTitle(title, language);

		const card = document.createElement("section");
		card.className = "source-card";

		const bar = document.createElement("div");
		bar.className = "source-bar";

		const languageElement = document.createElement("div");
		languageElement.className = "source-language";
		languageElement.textContent = language;

		const filenameElement = document.createElement("div");
		filenameElement.className = "source-filename";
		filenameElement.textContent = context.filePath;

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
		context.articleElement.append(card);

		enableStickyLines(sourceElement, stickyElement, linesElement, lineModels);
	}

	function createLineModels(source, language) {
		const tokenLines = splitTokensIntoLines(tokenizeSpo(source, language));
		const lineModels = tokenLines.map((segments, index) => {
			const text = segments.map((segment) => segment.text).join("");

			return {
				number: index + 1,
				segments,
				text,
				indent: countIndent(text),
				isBlank: text.trim() === "",
				parentIndex: null,
				startsBlock: false,
				leadingOpenerIndices: []
			};
		});

		assignLineHierarchy(lineModels);
		assignBracketLinks(lineModels);
		return lineModels;
	}

	function tokenizeSpo(source, language) {
		const tokens = [];
		const keywordParts = language === "ILT" ? ILT_KEYWORD_PARTS : SPO_KEYWORD_PARTS;
		let index = 0;

		while (index < source.length) {
			if (source[index] === "\"") {
				const endIndex = readSpoStringEnd(source, index);
				tokens.push({
					type: "string",
					text: source.slice(index, endIndex)
				});
				index = endIndex;
				continue;
			}

			const punctuationToken = readSpoPunctuation(source, index);
			if (punctuationToken) {
				tokens.push({
					type: "punctuation",
					text: punctuationToken
				});
				index += punctuationToken.length;
				continue;
			}

			const keywordToken = readSpoKeyword(source, index);
			if (keywordToken) {
				tokens.push({
					type: "keyword",
					text: keywordToken
				});
				index += keywordToken.length;
				continue;
			}

			const keywordPartToken = readSpoKeywordPart(source, index, keywordParts);
			if (keywordPartToken) {
				tokens.push({
					type: "keyword",
					text: keywordPartToken
				});
				index += keywordPartToken.length;
				continue;
			}

			const enumToken = readSpoEnum(source, index);
			if (enumToken) {
				tokens.push({
					type: "enum",
					text: enumToken
				});
				index += enumToken.length;
				continue;
			}

			const numberToken = readSpoNumber(source, index);
			if (numberToken) {
				tokens.push({
					type: "number",
					text: numberToken
				});
				index += numberToken.length;
				continue;
			}

			const identifierToken = readSpoIdentifier(source, index);
			if (identifierToken) {
				tokens.push({
					type: "identifier",
					text: identifierToken
				});
				index += identifierToken.length;
				continue;
			}

			tokens.push({
				type: "",
				text: source[index]
			});
			index += 1;
		}

		return tokens;
	}

	function splitTokensIntoLines(tokens) {
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

	function pushLineSegment(line, type, text) {
		if (!text) {
			return;
		}

		line.push({ type, text });
	}

	function assignLineHierarchy(lineModels) {
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

	function assignBracketLinks(lineModels) {
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

	function countIndent(text) {
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

	function createLineElement(lineModel, sticky = false) {
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

		const textElement = document.createElement("span");
		textElement.className = "source-line-text";
		appendHighlightedSegments(textElement, lineModel.segments);

		lineElement.append(gutterElement, textElement);
		return lineElement;
	}

	function appendHighlightedSegments(parent, segments) {
		for (const segment of segments) {
			if (!segment.type) {
				parent.append(document.createTextNode(segment.text));
				continue;
			}

			parent.append(createTokenNode(segment.type, segment.text));
		}
	}

	function enableStickyLines(sourceElement, stickyElement, linesElement, lineModels) {
		const lineElements = Array.from(linesElement.children);
		let scheduled = false;
		let lastStickyKey = "";

		const syncStickyLines = () => {
			scheduled = false;

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
					...stickyLineIndices.map((index) => createLineElement(lineModels[index], true))
				);
			}

			stickyElement.hidden = stickyLineIndices.length === 0;
		};

		const scheduleStickySync = () => {
			if (scheduled) {
				return;
			}

			scheduled = true;
			window.requestAnimationFrame(syncStickyLines);
		};

		sourceElement.addEventListener("scroll", scheduleStickySync, { passive: true });
		scheduleStickySync();
	}

	function resolveStickyLineIndices(scrollTop, lineModels, lineElements) {
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

	function resolveStickyLineIndicesForOffset(offsetTop, lineModels, lineElements) {
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

		return stickyLineIndices;
	}

	function findLineIndexAtOffset(lineElements, scrollTop) {
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

	function findReferenceLineIndex(lineModels, startIndex) {
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

	function uniqueStickyIndices(indices) {
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

	function measureStickyLineHeight(indices, lineElements) {
		let height = 0;

		for (const index of indices) {
			height += lineElements[index].offsetHeight;
		}

		return height;
	}

	function expandStickyLineIndices(indices, lineModels) {
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

	function isOpeningBracket(char) {
		return char === "(" || char === "[" || char === "{";
	}

	function isClosingBracket(char) {
		return char === ")" || char === "]" || char === "}";
	}

	function isLeadingCloserChar(char, prefixState) {
		return (prefixState === 0 || prefixState === 1) && isClosingBracket(char);
	}

	function popMatchingOpenerLine(openerStack, closerChar) {
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

	function createTokenNode(type, text) {
		const span = document.createElement("span");
		span.className = "tok-" + type;
		span.textContent = text;
		return span;
	}

	function readLineBreakLength(text, index) {
		if (text[index] === "\r" && text[index + 1] === "\n") {
			return 2;
		}

		if (text[index] === "\r" || text[index] === "\n") {
			return 1;
		}

		return 0;
	}

	function readSpoStringEnd(text, startIndex) {
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

	function readSpoKeyword(text, startIndex) {
		return readSpoPrefixedId(text, startIndex, "\u00A7");
	}

	function readSpoKeywordPart(text, startIndex, keywordParts) {
		const idToken = readSpoId(text, startIndex);
		return idToken && keywordParts.has(idToken) ? idToken : "";
	}

	function readSpoEnum(text, startIndex) {
		return readSpoPrefixedId(text, startIndex, "#");
	}

	function readSpoNumber(text, startIndex) {
		const match = /^[+-]?\d(?:_*\d)*/.exec(text.slice(startIndex));
		return match ? match[0] : "";
	}

	function readSpoIdentifier(text, startIndex) {
		return readSpoId(text, startIndex);
	}

	function readSpoPunctuation(text, startIndex) {
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

	function isSpoPunctuation(char) {
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

	function readSpoPrefixedId(text, startIndex, prefix) {
		if (text[startIndex] !== prefix) {
			return "";
		}

		const idToken = readSpoId(text, startIndex + 1);
		return idToken ? prefix + idToken : "";
	}

	function readSpoId(text, startIndex) {
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

	function isSpoSpecialChar(char) {
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

	function skipSpoInlineWhitespace(text, startIndex) {
		let index = startIndex;
		while (text[index] === " " || text[index] === "\t" || text[index] === "\r") {
			index += 1;
		}

		return index;
	}

	function readSpoLineBreak(text, startIndex) {
		if (text[startIndex] === "\r" && text[startIndex + 1] === "\n") {
			return startIndex + 2;
		}

		if (text[startIndex] === "\n") {
			return startIndex + 1;
		}

		return null;
	}

	function isSpoLikeFile(path) {
		return /\.(spo|ilt)$/i.test(path);
	}

	window.SpoViewer = {
		render,
		isSpoLikeFile
	};
})();
