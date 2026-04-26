(function () {
	const HEADING_PATTERN = /^\[(=+)(.*)\1\]$/;
	const RULE_PATTERN = /^\[-{3,}\]$/;
	const MARKER_PATTERN = /^([-o*#>])\s+(.*)$/;
	const IMAGE_TARGET_PATTERN =
		/^(?:data:image\/[a-z0-9.+-]+(?:;[^,]*)?,|.+\.(?:avif|bmp|gif|ico|jpe?g|png|svg|webp)(?:$|[?#]))/i;
	const INLINE_MARKERS = {
		_: { close: "_]", tagName: "u" },
		"-": { close: "-]", tagName: "del" },
		"/": { close: "/]", tagName: "em" },
		"*": { close: "*]", tagName: "strong" },
		"!": {
			close: "!]",
			tagName: "span",
			className: "wiki-alert",
			styles: [
				["color", "#c62828"]
			]
		},
		"(": { close: ")]", tagName: "small" },
		"'": { close: "']", tagName: "sup" },
		",": { close: ",]", tagName: "sub" }
	};
	const TEXT_TOKEN_PATTERN =
		/(?:https?:\/\/|ftp:\/\/|www\.)[^\s<>\]]+|[A-Za-z0-9._-]+@(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,63}|\[>|<\]|\["|"\]/gi;

	function render(source, context) {
		context.prepareArticle("wiki-article");

		const fragment = document.createDocumentFragment();
		const blocks = parseBlocks(source);
		let headingCount = 0;
		let title = context.fileNameLabel(context.filePath);

		if (!blocks.length) {
			const paragraph = document.createElement("p");
			paragraph.className = "status";
			paragraph.textContent = "Die Datei ist leer.";
			context.setPageTitle(title, "Wiki");
			context.articleElement.append(paragraph);
			return;
		}

		for (const block of blocks) {
			if (block.type === "heading") {
				headingCount += 1;
				if (headingCount === 1) {
					title = block.text || title;
				}

				fragment.append(createHeadingElement(block, headingCount, context));
				continue;
			}

			if (block.type === "rule") {
				fragment.append(document.createElement("hr"));
				continue;
			}

			if (block.type === "tableBlock") {
				fragment.append(createTableElement(block, context));
				continue;
			}

			if (block.type === "objectBlock") {
				fragment.append(createObjectBlockElement(block, context));
				continue;
			}

			fragment.append(createParagraphElement(block, context));
		}

		context.setPageTitle(title, "Wiki");
		context.articleElement.append(fragment);
	}

	function createHeadingElement(block, headingCount, context) {
		const resolvedLevel = headingCount === 1
			? 1
			: Math.max(2, Math.min(6, block.level || 2));
		const heading = document.createElement("h" + resolvedLevel);
		context.appendNodes(heading, parseInline(block.text, context));
		return heading;
	}

	function createParagraphElement(block, context) {
		const paragraph = document.createElement("p");
		paragraph.className = "wiki-paragraph";
		paragraph.style.setProperty("--indent", String(block.indent));

		if (!block.marker) {
			appendInlineLines(paragraph, block.lines, context);
			return paragraph;
		}

		paragraph.classList.add("has-marker");

		const marker = document.createElement("span");
		marker.className = "marker";
		marker.textContent = block.marker;

		const content = document.createElement("span");
		content.className = "content";
		appendInlineLines(content, block.lines, context);
		paragraph.append(marker, content);

		return paragraph;
	}

	function createTableElement(block, context) {
		const wrapper = document.createElement("div");
		wrapper.className = "wiki-table-wrapper";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(block.indent);
		wrapper.style.overflowX = "auto";

		const table = document.createElement("table");
		table.className = "wiki-table";
		table.style.width = "100%";
		table.style.borderCollapse = "collapse";
		table.style.border = "1px solid rgba(36, 29, 24, 0.22)";
		table.style.background = "rgba(255, 252, 247, 0.9)";

		for (let rowIndex = 0; rowIndex < block.rows.length; rowIndex += 1) {
			const row = document.createElement("tr");
			row.style.background = rowIndex % 2 === 0
				? "rgba(239, 224, 202, 0.55)"
				: "rgba(255, 255, 255, 0.78)";

			for (const cellText of block.rows[rowIndex]) {
				const cell = document.createElement("td");
				cell.style.padding = "0.65rem 0.8rem";
				cell.style.border = "1px solid rgba(36, 29, 24, 0.16)";
				cell.style.verticalAlign = "top";
				appendInlineLines(cell, splitCellLines(cellText), context);
				row.append(cell);
			}

			table.append(row);
		}

		wrapper.append(table);
		return wrapper;
	}

	function createObjectBlockElement(block, context) {
		const lines = block.lines.filter((line, index, values) => {
			return line !== "" || index !== values.length - 1 || values.length === 1;
		});
		const singleLine = lines.length === 1 ? lines[0].trim() : "";

		if (singleLine && isImageTarget(singleLine)) {
			const wrapper = document.createElement("div");
			wrapper.className = "wiki-object-image";
			wrapper.style.margin = "0 0 1rem";
			wrapper.style.marginInlineStart = indentToMargin(block.indent);
			wrapper.append(createImageNode(singleLine, context, false));
			return wrapper;
		}

		const wrapper = document.createElement("div");
		wrapper.className = "wiki-object-block";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(block.indent);

		const label = document.createElement("div");
		label.className = "wiki-object-block-label";
		label.textContent = "." + block.kind;
		label.style.margin = "0 0 0.35rem";
		label.style.color = "var(--muted)";
		label.style.fontSize = "0.8rem";
		label.style.fontWeight = "700";
		label.style.letterSpacing = "0.08em";
		label.style.textTransform = "uppercase";

		const pre = document.createElement("pre");
		pre.className = "wiki-object-block-content";
		pre.style.margin = "0";
		pre.style.padding = "0.85rem 1rem";
		pre.style.borderRadius = "0.8rem";
		pre.style.background = "var(--object)";
		pre.style.overflowX = "auto";
		pre.style.lineHeight = "1.55";
		pre.style.fontFamily = "\"Cascadia Code\", Consolas, \"SFMono-Regular\", \"Courier New\", monospace";
		pre.style.fontSize = "0.93rem";

		const code = document.createElement("code");
		code.dataset.kind = block.kind;
		code.textContent = lines.join("\n");

		pre.append(code);
		wrapper.append(label, pre);
		return wrapper;
	}

	function appendInlineLines(parent, lines, context) {
		for (let index = 0; index < lines.length; index += 1) {
			if (index > 0) {
				parent.append(document.createElement("br"));
			}

			context.appendNodes(parent, parseInline(lines[index], context));
		}
	}

	function parseBlocks(source) {
		const lines = source.replace(/\r\n?/g, "\n").split("\n");
		const blocks = [];
		let currentParagraph = null;

		const flushParagraph = () => {
			if (!currentParagraph) {
				return;
			}

			blocks.push(currentParagraph);
			currentParagraph = null;
		};

		for (let index = 0; index < lines.length; index += 1) {
			const line = lines[index];
			const indent = countIndent(line);
			const content = line.trim();

			if (!content) {
				flushParagraph();
				continue;
			}

			const tableBlock = readTableBlock(lines, index);
			if (tableBlock) {
				flushParagraph();
				blocks.push(tableBlock.block);
				index = tableBlock.nextIndex;
				continue;
			}

			const objectBlock = readObjectBlock(lines, index);
			if (objectBlock) {
				flushParagraph();
				blocks.push(objectBlock.block);
				index = objectBlock.nextIndex;
				continue;
			}

			const headingMatch = content.match(HEADING_PATTERN);
			if (headingMatch) {
				flushParagraph();
				blocks.push({
					type: "heading",
					level: headingMatch[1].length,
					text: headingMatch[2].trim()
				});
				continue;
			}

			if (RULE_PATTERN.test(content)) {
				flushParagraph();
				blocks.push({ type: "rule" });
				continue;
			}

			const markerMatch = content.match(MARKER_PATTERN);
			const marker = markerMatch ? markerMatch[1] : "";
			const text = markerMatch ? markerMatch[2] : content;

			if (
				currentParagraph &&
				currentParagraph.type === "paragraph" &&
				!marker &&
				!currentParagraph.marker &&
				currentParagraph.marker === marker &&
				currentParagraph.indent === indent
			) {
				currentParagraph.lines.push(text);
				continue;
			}

			flushParagraph();
			currentParagraph = {
				type: "paragraph",
				marker,
				indent,
				lines: [text]
			};
		}

		flushParagraph();
		return blocks;
	}

	function readTableBlock(lines, startIndex) {
		if (!lines[startIndex].trim().startsWith("[|")) {
			return null;
		}

		const rows = [];
		let index = startIndex;

		while (index < lines.length) {
			while (index < lines.length && lines[index].trim() === "") {
				index += 1;
			}

			if (index >= lines.length || !lines[index].trim().startsWith("[|")) {
				break;
			}

			const rowBlock = readDelimitedBlock(lines, index, "[|", "|]");
			if (!rowBlock) {
				return null;
			}

			rows.push(parseTableRow(rowBlock.content));
			index = rowBlock.nextIndex + 1;
		}

		if (!rows.length) {
			return null;
		}

		return {
			block: {
				type: "tableBlock",
				indent: countIndent(lines[startIndex]),
				rows
			},
			nextIndex: index - 1
		};
	}

	function readObjectBlock(lines, startIndex) {
		const line = lines[startIndex];
		const trimmed = line.trim();
		if (!trimmed.startsWith("[.") || trimmed.endsWith("]")) {
			return null;
		}

		const baseWhitespace = line.match(/^[ \t]*/)[0];
		const rawKind = trimmed.slice(2).trim();
		const bodyLines = [];

		for (let index = startIndex + 1; index < lines.length; index += 1) {
			if (lines[index].trim() === ".]") {
				return {
					block: {
						type: "objectBlock",
						indent: countIndent(line),
						kind: normalizeObjectKind(rawKind),
						lines: bodyLines
					},
					nextIndex: index
				};
			}

			bodyLines.push(stripObjectBlockLine(lines[index], baseWhitespace));
		}

		return null;
	}

	function readDelimitedBlock(lines, startIndex, openingToken, closingToken) {
		const firstLine = lines[startIndex];
		const baseWhitespace = firstLine.match(/^[ \t]*/)[0];
		const firstContent = trimSharedIndent(firstLine, baseWhitespace).trimStart();
		if (!firstContent.startsWith(openingToken)) {
			return null;
		}

		let content = firstContent.slice(openingToken.length);
		const firstCloseIndex = content.indexOf(closingToken);
		if (firstCloseIndex !== -1) {
			return {
				content: content.slice(0, firstCloseIndex),
				nextIndex: startIndex
			};
		}

		for (let index = startIndex + 1; index < lines.length; index += 1) {
			const lineContent = trimSharedIndent(lines[index], baseWhitespace);
			const closeIndex = lineContent.indexOf(closingToken);

			if (closeIndex !== -1) {
				content += "\n" + lineContent.slice(0, closeIndex);
				return {
					content,
					nextIndex: index
				};
			}

			content += "\n" + lineContent;
		}

		return null;
	}

	function parseTableRow(content) {
		const normalizedContent = content.replace(/^\n/, "");
		const cells = [];
		let buffer = "";
		let index = 0;

		while (index < normalizedContent.length) {
			if (normalizedContent.startsWith("||", index)) {
				buffer += "|";
				index += 2;
				continue;
			}

			if (normalizedContent[index] === "|") {
				cells.push(buffer);
				buffer = "";
				index += 1;
				continue;
			}

			buffer += normalizedContent[index];
			index += 1;
		}

		cells.push(buffer);
		return cells;
	}

	function stripObjectBlockLine(line, baseWhitespace) {
		let text = trimSharedIndent(line, baseWhitespace);

		if (text.startsWith("|")) {
			text = text.slice(1);
			if (text.startsWith(" ")) {
				text = text.slice(1);
			}
		}

		return text;
	}

	function splitCellLines(text) {
		return text.split("\n");
	}

	function trimSharedIndent(line, baseWhitespace) {
		return line.startsWith(baseWhitespace)
			? line.slice(baseWhitespace.length)
			: line;
	}

	function normalizeObjectKind(rawKind) {
		const normalized = rawKind.replace(/^\.+/, "").trim().toLowerCase();
		return normalized || "txt";
	}

	function countIndent(line) {
		let indent = 0;
		for (const char of line) {
			if (char === " ") {
				indent += 1;
				continue;
			}

			if (char === "\t") {
				indent += 2;
				continue;
			}

			break;
		}

		return indent;
	}

	function parseInline(text, context, stopToken = null, startIndex = 0) {
		const nodes = [];
		let buffer = "";
		let index = startIndex;

		const flushBuffer = () => {
			if (!buffer) {
				return;
			}

			appendTextNodes(nodes, buffer, context);
			buffer = "";
		};

		while (index < text.length) {
			if (isRealClosingToken(text, index, stopToken)) {
				flushBuffer();
				return { nodes, index: index + stopToken.length, closed: true };
			}

			if (text.startsWith("[[", index)) {
				buffer += "[";
				index += 2;
				continue;
			}

			if (text.startsWith("]]", index)) {
				buffer += "]";
				index += 2;
				continue;
			}

			if (text[index] === "[" && index + 1 < text.length) {
				const marker = text[index + 1];
				const inlineMarker = INLINE_MARKERS[marker];

				if (inlineMarker) {
					const inner = parseInline(text, context, inlineMarker.close, index + 2);
					if (inner.closed) {
						flushBuffer();
						nodes.push(wrapInlineMarker(marker, inner.nodes, context));
						index = inner.index;
						continue;
					}
				}

				if (marker === "<" || marker === "?") {
					const link = parseLink(text, context, index + 2, marker);
					if (link) {
						flushBuffer();
						nodes.push(link.node);
						index = link.index;
						continue;
					}
				}

				if (marker === "#") {
					const objectToken = readTokenContent(text, index + 2, "#]");
					if (objectToken) {
						flushBuffer();
						nodes.push(createInlineObjectNode(objectToken.content, context));
						index = objectToken.index;
						continue;
					}
				}
			}

			buffer += text[index];
			index += 1;
		}

		flushBuffer();
		return stopToken ? { nodes, index, closed: false } : nodes;
	}

	function wrapInlineMarker(marker, children, context) {
		const definition = INLINE_MARKERS[marker];
		const element = document.createElement(definition ? definition.tagName : "span");
		if (definition && definition.className) {
			element.className = definition.className;
		}
		if (definition && definition.styles) {
			for (const [name, value] of definition.styles) {
				element.style.setProperty(name, value);
			}
		}
		context.appendNodes(element, children);
		return element;
	}

	function appendTextNodes(nodes, text, context) {
		if (!text) {
			return;
		}

		TEXT_TOKEN_PATTERN.lastIndex = 0;
		let lastIndex = 0;
		let match = null;

		while ((match = TEXT_TOKEN_PATTERN.exec(text))) {
			if (match.index > lastIndex) {
				nodes.push(document.createTextNode(text.slice(lastIndex, match.index)));
			}

			if (isLiteralWikiSyntaxToken(text, match.index, match[0].length)) {
				nodes.push(document.createTextNode(match[0]));
			} else {
				nodes.push(...createTextTokenNodes(match[0], context));
			}
			lastIndex = match.index + match[0].length;
		}

		if (lastIndex < text.length) {
			nodes.push(document.createTextNode(text.slice(lastIndex)));
		}
	}

	function createTextTokenNodes(token, context) {
		if (token === "[>") {
			return [document.createTextNode("\u00bb")];
		}

		if (token === "<]") {
			return [document.createTextNode("\u00ab")];
		}

		if (token === "[\"") {
			return [document.createTextNode("\u201e")];
		}

		if (token === "\"]") {
			return [document.createTextNode("\u201c")];
		}

		const { value, trailing } = splitTrailingPunctuation(token);
		const nodes = [];

		if (looksLikeEmailAddress(value)) {
			nodes.push(createAutoLinkNode("mailto:" + value, value));
		} else if (context.isExternalLink(value)) {
			nodes.push(createExternalLinkNode(value, context));
		} else {
			nodes.push(document.createTextNode(token));
			return nodes;
		}

		if (trailing) {
			nodes.push(document.createTextNode(trailing));
		}

		return nodes;
	}

	function isLiteralWikiSyntaxToken(text, startIndex, tokenLength) {
		const prefix = text.slice(Math.max(0, startIndex - 2), startIndex);
		const suffix = text[startIndex + tokenLength] || "";
		return (prefix === "[<" || prefix === "[?") &&
			(suffix === ">" || suffix === "|" || suffix === "?");
	}

	function createAutoLinkNode(href, label) {
		const anchor = document.createElement("a");
		anchor.href = href;
		anchor.textContent = label;
		return anchor;
	}

	function createExternalLinkNode(target, context) {
		const anchor = document.createElement("a");
		anchor.href = context.normalizeExternalLink(target);
		anchor.target = "_blank";
		anchor.rel = "noreferrer noopener";
		anchor.textContent = target;
		return anchor;
	}

	function splitTrailingPunctuation(token) {
		const match = /[.,;:!?]+$/.exec(token);
		if (!match) {
			return {
				value: token,
				trailing: ""
			};
		}

		return {
			value: token.slice(0, -match[0].length),
			trailing: match[0]
		};
	}

	function looksLikeEmailAddress(text) {
		return /^[A-Za-z0-9._-]+@(?:[A-Za-z0-9-]+\.)+[A-Za-z]{2,63}$/.test(text);
	}

	function createInlineObjectNode(content, context) {
		const value = decodeEscapedBrackets(content).trim();
		if (value && isImageTarget(value)) {
			return createImageNode(value, context, true);
		}

		const code = document.createElement("code");
		code.className = "wiki-object";
		code.textContent = value;
		return code;
	}

	function createImageNode(target, context, inline) {
		const image = document.createElement("img");
		image.className = inline ? "wiki-inline-image" : "wiki-block-image";
		image.src = resolveAssetUrl(target, context);
		image.alt = defaultAssetLabel(target, context);
		image.loading = "lazy";
		image.decoding = "async";
		image.style.maxWidth = "100%";
		image.style.height = "auto";
		image.style.borderRadius = "0.8rem";

		if (inline) {
			image.style.display = "inline-block";
			image.style.verticalAlign = "middle";
			image.style.maxHeight = "14rem";
		} else {
			image.style.display = "block";
		}

		return image;
	}

	function resolveAssetUrl(target, context) {
		if (context.isExternalLink(target)) {
			return context.normalizeExternalLink(target);
		}

		const resolvedPath = context.resolveRelativePath(context.filePath, target);
		return resolvedPath || target;
	}

	function defaultAssetLabel(target, context) {
		const cleanedTarget = target.replace(/[?#].*$/, "");
		return context.fileNameLabel(cleanedTarget) || cleanedTarget || target;
	}

	function isImageTarget(target) {
		return IMAGE_TARGET_PATTERN.test(target);
	}

	function parseLink(text, context, startIndex, marker) {
		const endToken = marker === "?" ? "?]" : ">]";
		const token = readTokenContent(text, startIndex, endToken);
		if (!token) {
			return null;
		}

		const split = splitLinkTargetAndLabel(token.content);
		const target = decodeEscapedBrackets(split.target).trim();
		if (!target) {
			return null;
		}

		const anchor = document.createElement("a");
		const label = split.label !== null
			? split.label
			: defaultLinkLabel(split.target, context);

		context.appendNodes(anchor, parseInline(label, context));

		if (marker === "?") {
			anchor.href = "board.php?titel=" + encodeURIComponent(target);
		} else if (context.isExternalLink(target)) {
			anchor.href = context.normalizeExternalLink(target);
			anchor.target = "_blank";
			anchor.rel = "noreferrer noopener";
		} else {
			if (hasExplicitFileExtension(target) && !context.hasKnownRenderableExtension(target)) {
				anchor.href = resolveAssetUrl(target, context);
				return {
					node: anchor,
					index: token.index
				};
			}

			const rawTarget = needsImplicitWikiExtension(target, context)
				? target + ".wiki"
				: target;
			const resolvedPath = context.resolveRelativePath(context.filePath, rawTarget);
			anchor.href = resolvedPath ? context.createNavigationUrl(resolvedPath) : "#";
		}

		return {
			node: anchor,
			index: token.index
		};
	}

	function needsImplicitWikiExtension(target, context) {
		return !context.hasKnownRenderableExtension(target) && !hasExplicitFileExtension(target);
	}

	function hasExplicitFileExtension(target) {
		const cleanedTarget = target.replace(/[?#].*$/, "");
		const lastSegment = cleanedTarget.split("/").pop() || "";
		return /\.[^.]+$/.test(lastSegment);
	}

	function readTokenContent(text, startIndex, endToken) {
		let index = startIndex;
		let content = "";

		while (index < text.length) {
			if (isRealClosingToken(text, index, endToken)) {
				return {
					content,
					index: index + endToken.length
				};
			}

			if (text.startsWith("[[", index)) {
				content += "[[";
				index += 2;
				continue;
			}

			if (text.startsWith("]]", index)) {
				content += "]]";
				index += 2;
				continue;
			}

			content += text[index];
			index += 1;
		}

		return null;
	}

	function splitLinkTargetAndLabel(content) {
		let index = 0;
		while (index < content.length) {
			if (content.startsWith("[[", index) || content.startsWith("]]", index)) {
				index += 2;
				continue;
			}

			if (content[index] === "|") {
				return {
					target: content.slice(0, index),
					label: content.slice(index + 1)
				};
			}

			index += 1;
		}

		return {
			target: content,
			label: null
		};
	}

	function isRealClosingToken(text, index, token) {
		if (!token || !text.startsWith(token, index)) {
			return false;
		}

		if (!token.endsWith("]")) {
			return true;
		}

		let runLength = 0;
		let runIndex = index + token.length - 1;
		while (runIndex < text.length && text[runIndex] === "]") {
			runLength += 1;
			runIndex += 1;
		}

		return runLength % 2 === 1;
	}

	function defaultLinkLabel(rawTarget, context) {
		const withoutArticleMeta = rawTarget
			.replace(/^\[\[[^\]]+\]\]/, "")
			.replace(/\[\[[^\]]+\]\]$/, "");

		const decoded = decodeEscapedBrackets(withoutArticleMeta).trim();
		if (context.isExternalLink(decoded)) {
			return decoded;
		}

		if (context.hasKnownRenderableExtension(decoded) || hasExplicitFileExtension(decoded)) {
			return context.fileNameLabel(decoded.replace(/[?#].*$/, ""));
		}

		return decoded || rawTarget;
	}

	function decodeEscapedBrackets(text) {
		return text
			.replace(/\[\[/g, "[")
			.replace(/\]\]/g, "]");
	}

	function indentToMargin(indent) {
		return "calc(" + indent + " * 0.85ch)";
	}

	window.WikiViewer = {
		render
	};
})();
