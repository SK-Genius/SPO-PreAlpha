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
	const ESCAPED_OPEN_BRACKET = "\uE000";
	const ESCAPED_CLOSE_BRACKET = "\uE001";
	const ESCAPED_PIPE = "\uE002";

	function render(source, context) {
		context.prepareArticle("wiki-article");

		const normalizedSource = normalizeLegacyEscapes(source);
		const fragment = document.createDocumentFragment();
		const blocks = parseBlocks(normalizedSource);
		const title = resolveArticleTitle(context.filePath, context);
		const headingState = describeHeadings(blocks, context, title);

		if (!blocks.length) {
			const paragraph = document.createElement("p");
			paragraph.className = "status";
			paragraph.textContent = "Die Datei ist leer.";
			context.setPageTitle(title, "Wiki");
			context.articleElement.append(paragraph);
			return;
		}

		fragment.append(
			createAnchorTarget("wiki-begin"),
			createArticleHeader(title, context.filePath)
		);

		if (headingState.entries.length > 0) {
			fragment.append(createTableOfContents(headingState.entries, context));
		}

		for (const block of blocks) {
			if (block.type === "heading") {
				fragment.append(createHeadingElement(block, context));
				continue;
			}

			if (block.type === "rule") {
				fragment.append(createRuleElement());
				continue;
			}

			if (block.type === "tableBlock") {
				fragment.append(createTableElement(block, context));
				continue;
			}

			if (block.type === "hashBlock") {
				fragment.append(createHashBlockElement(block, context));
				continue;
			}

			if (block.type === "objectBlock") {
				fragment.append(createObjectBlockElement(block, context));
				continue;
			}

			fragment.append(createParagraphElement(block, context));
		}

		fragment.append(createAnchorTarget("wiki-end"));
		context.setPageTitle(title, "Wiki");
		context.articleElement.append(fragment);
	}

	function resolveArticleTitle(filePath, context) {
		return context.fileNameLabel(filePath) || "Wiki";
	}

	function normalizeLegacyEscapes(source) {
		return source
			.replace(/\[\\/g, ESCAPED_OPEN_BRACKET)
			.replace(/\\\]/g, ESCAPED_CLOSE_BRACKET);
	}

	function decodeLegacyEscapePlaceholders(text) {
		return text
			.replace(new RegExp(ESCAPED_OPEN_BRACKET, "g"), "[")
			.replace(new RegExp(ESCAPED_PIPE, "g"), "|")
			.replace(new RegExp(ESCAPED_CLOSE_BRACKET, "g"), "]");
	}

	function createAnchorTarget(id) {
		const anchor = document.createElement("a");
		anchor.id = id;
		anchor.setAttribute("aria-hidden", "true");
		anchor.style.display = "block";
		anchor.style.position = "relative";
		anchor.style.top = "-0.35rem";
		return anchor;
	}

	function createArticleHeader(title, filePath) {
		const header = document.createElement("header");
		header.className = "wiki-article-header";
		header.style.margin = "0 0 1rem";
		header.style.padding = "0.85rem 1rem 0.95rem";
		header.style.border = "1px solid rgba(36, 29, 24, 0.24)";
		header.style.borderRadius = "0.45rem";
		header.style.background =
			"linear-gradient(180deg, rgba(255, 255, 255, 0.94), rgba(243, 230, 209, 0.86))";
		header.style.boxShadow = "0 8px 18px rgba(70, 49, 31, 0.05)";

		const eyebrow = document.createElement("div");
		eyebrow.textContent = "Artikel";
		eyebrow.style.margin = "0 0 0.25rem";
		eyebrow.style.color = "var(--muted)";
		eyebrow.style.fontSize = "0.76rem";
		eyebrow.style.fontWeight = "700";
		eyebrow.style.letterSpacing = "0.12em";
		eyebrow.style.textTransform = "uppercase";

		const heading = document.createElement("div");
		heading.style.fontSize = "clamp(1.55rem, 3vw, 2.15rem)";
		heading.style.fontWeight = "700";
		heading.style.lineHeight = "1.1";
		heading.style.textDecoration = "underline";
		heading.style.textDecorationThickness = "0.08em";
		heading.style.textUnderlineOffset = "0.16em";
		heading.textContent = title;

		const meta = document.createElement("div");
		meta.textContent = filePath;
		meta.style.margin = "0.5rem 0 0";
		meta.style.color = "var(--muted)";
		meta.style.fontSize = "0.92rem";
		meta.style.wordBreak = "break-word";

		header.append(eyebrow, heading, meta);
		return header;
	}

	function createHeadingElement(block, context) {
		const heading = document.createElement("section");
		heading.className = "wiki-section-heading";
		if (block.anchorId) {
			heading.id = block.anchorId;
		}

		heading.style.display = "grid";
		heading.style.gridTemplateColumns = "auto minmax(0, 1fr) auto";
		heading.style.alignItems = "center";
		heading.style.gap = "0.75rem";
		heading.style.margin = block.headingIndex === 1
			? "1rem 0 0.7rem"
			: "1.45rem 0 0.7rem";
		heading.style.padding = "0.35rem 0.5rem 0.35rem 0.35rem";
		heading.style.border = "1px solid rgba(36, 29, 24, 0.28)";
		heading.style.borderRadius = "0.35rem";
		heading.style.background =
			"linear-gradient(180deg, rgba(255, 255, 255, 0.95), rgba(239, 224, 202, 0.92))";

		const controls = document.createElement("div");
		controls.style.display = "inline-flex";
		controls.style.alignItems = "center";
		controls.style.gap = "0.32rem";
		controls.style.fontFamily = "\"Cascadia Code\", Consolas, monospace";
		controls.style.fontSize = "0.84rem";
		controls.style.whiteSpace = "nowrap";

		controls.append(
			createSectionNavLink("#" + block.anchorId, "#" + block.headingIndex, "Direktlink"),
			createSectionNavLink("#wiki-begin", "\u2191", "Zum Anfang"),
			createSectionNavLink("#wiki-end", "\u2193", "Zum Ende")
		);

		const title = document.createElement("h2");
		title.style.margin = "0";
		title.style.minWidth = "0";
		title.style.fontSize = "1.1rem";
		title.style.lineHeight = "1.2";
		title.style.border = "0";
		title.style.padding = "0";

		context.appendNodes(title, parseInline(block.text, context));

		const meta = document.createElement("div");
		meta.textContent = "Kapitel " + block.headingIndex;
		meta.style.color = "var(--muted)";
		meta.style.fontSize = "0.8rem";
		meta.style.whiteSpace = "nowrap";

		heading.append(controls, title, meta);
		return heading;
	}

	function createSectionNavLink(href, text, title) {
		const link = document.createElement("a");
		link.href = href;
		link.textContent = text;
		link.title = title;
		link.style.display = "inline-block";
		link.style.minWidth = "2.1ch";
		link.style.padding = "0.08rem 0.28rem";
		link.style.border = "1px solid rgba(36, 29, 24, 0.18)";
		link.style.borderRadius = "0.22rem";
		link.style.background = "rgba(255, 255, 255, 0.72)";
		link.style.color = "var(--ink)";
		link.style.textDecoration = "none";
		link.style.textAlign = "center";
		return link;
	}

	function createParagraphElement(block, context) {
		if (block.marker) {
			return createMarkerParagraphElement(block, context);
		}

		const paragraph = document.createElement("p");
		paragraph.className = "wiki-paragraph";
		paragraph.style.setProperty("--indent", String(block.indent));
		paragraph.style.marginInlineStart = indentToMargin(block.indent);
		appendInlineLines(paragraph, block.lines, context);
		return paragraph;
	}

	function createMarkerParagraphElement(block, context) {
		const list = document.createElement("ul");
		list.className = "wiki-paragraph has-marker";
		list.style.display = "block";
		list.style.margin = "0 0 0.85rem";
		list.style.marginInlineStart = indentToMargin(block.indent);
		list.style.paddingInlineStart = "1.45rem";

		const item = document.createElement("li");
		item.style.margin = "0";
		item.style.padding = "0";

		if (block.marker === "o") {
			list.style.listStyleType = "circle";
		} else if (block.marker === "*") {
			list.style.listStyleType = "disc";
		} else if (block.marker === "#") {
			list.style.listStyleType = "square";
		} else {
			list.style.listStyleType = "none";
			list.style.paddingInlineStart = "0";

			const marker = document.createElement("span");
			marker.className = "marker";
			marker.textContent = block.marker === ">" ? "\u25b8" : "\u2013";
			marker.style.display = "inline-block";
			marker.style.minWidth = "1.1rem";
			marker.style.marginInlineEnd = "0.45rem";
			marker.style.color = "var(--muted)";
			marker.style.fontWeight = "700";
			item.append(marker);
		}

		const content = document.createElement("span");
		content.className = "content";
		appendInlineLines(content, block.lines, context);
		item.append(content);
		list.append(item);
		return list;
	}

	function createTableOfContents(entries, context) {
		const nav = document.createElement("nav");
		nav.className = "wiki-toc";
		nav.setAttribute("aria-label", "Inhaltsverzeichnis");
		nav.style.margin = "0 0 1.25rem";
		nav.style.padding = "0.85rem 1rem 0.95rem";
		nav.style.border = "1px solid rgba(36, 29, 24, 0.24)";
		nav.style.borderRadius = "0.45rem";
		nav.style.background = "rgba(255, 252, 247, 0.88)";
		nav.style.boxShadow = "0 8px 18px rgba(70, 49, 31, 0.05)";

		const title = document.createElement("div");
		title.textContent = "Inhalt";
		title.style.margin = "0 0 0.55rem";
		title.style.color = "var(--ink)";
		title.style.fontSize = "0.92rem";
		title.style.fontWeight = "700";

		const list = document.createElement("ol");
		list.style.margin = "0";
		list.style.paddingInlineStart = "1.35rem";
		list.style.columnGap = "1.8rem";
		list.style.columnCount = String(Math.min(3, Math.max(1, Math.ceil(entries.length / 4))));

		for (const entry of entries) {
			const item = document.createElement("li");
			item.style.breakInside = "avoid";
			item.style.margin = "0 0 0.18rem";

			const link = document.createElement("a");
			link.href = "#" + entry.anchorId;
			context.appendNodes(link, parseInline(entry.text, context));
			item.append(link);
			list.append(item);
		}

		nav.append(title, list);
		return nav;
	}

	function createRuleElement() {
		const rule = document.createElement("div");
		rule.className = "wiki-rule";
		rule.style.height = "0.65rem";
		rule.style.margin = "1rem 0";
		rule.style.border = "1px solid rgba(36, 29, 24, 0.18)";
		rule.style.borderRadius = "999px";
		rule.style.background =
			"linear-gradient(90deg, rgba(127, 79, 36, 0.2), rgba(127, 79, 36, 0.08), rgba(127, 79, 36, 0.2))";
		return rule;
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
		table.style.border = "1px solid #000000";
		table.style.background = "#ffffff";

		for (let rowIndex = 0; rowIndex < block.rows.length; rowIndex += 1) {
			const row = document.createElement("tr");
			row.style.background = rowIndex % 2 === 0 ? "#ddddff" : "#ffffdd";

			for (const cellText of block.rows[rowIndex]) {
				const cell = document.createElement("td");
				cell.style.padding = "0.5rem 0.65rem";
				cell.style.border = "1px solid #000000";
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
		const lines = normalizeBlockLines(block.lines);
		const singleLine = lines.length === 1 ? lines[0].trim() : "";

		if (singleLine && isImageTarget(singleLine)) {
			return createImageBlockElement(singleLine, block.indent, context);
		}

		if (singleLine && isReferenceTarget(singleLine, context)) {
			return createReferenceBlockElement(singleLine, block.indent, context, "." + block.kind);
		}

		return createCodeBlockElement("." + block.kind, lines.join("\n"), block.indent, block.kind);
	}

	function createHashBlockElement(block, context) {
		const rawContent = decodeLegacyEscapePlaceholders(block.content).replace(/^\n/, "");
		const content = rawContent.trim();
		if (!content) {
			return createCodeBlockElement("#", "", block.indent, "txt");
		}

		if (isImageTarget(content)) {
			return createImageBlockElement(content, block.indent, context);
		}

		if (isReferenceTarget(content, context)) {
			return createReferenceBlockElement(content, block.indent, context, "#");
		}

		return createCodeBlockElement("#", rawContent, block.indent, "txt");
	}

	function createImageBlockElement(target, indent, context) {
		const wrapper = document.createElement("div");
		wrapper.className = "wiki-object-image";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(indent);
		wrapper.append(createImageNode(target, context, false));
		return wrapper;
	}

	function createCodeBlockElement(labelText, content, indent, kind = "txt") {
		const wrapper = document.createElement("div");
		wrapper.className = "wiki-object-block";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(indent);

		const label = document.createElement("div");
		label.className = "wiki-object-block-label";
		label.textContent = labelText;
		label.style.margin = "0 0 0.35rem";
		label.style.color = "var(--muted)";
		label.style.fontSize = "0.8rem";
		label.style.fontWeight = "700";
		label.style.letterSpacing = "0.08em";
		label.style.textTransform = "uppercase";

		const pre = document.createElement("pre");
		pre.className = "wiki-object-block-content";
		pre.style.margin = "0";
		pre.style.padding = "0.75rem 0.9rem";
		pre.style.border = "1px solid #000000";
		pre.style.borderRadius = "0.2rem";
		pre.style.background = "#ffffdd";
		pre.style.overflowX = "auto";
		pre.style.lineHeight = "1.55";
		pre.style.fontFamily = "\"Cascadia Code\", Consolas, \"SFMono-Regular\", \"Courier New\", monospace";
		pre.style.fontSize = "0.93rem";

		const code = document.createElement("code");
		code.dataset.kind = kind;
		code.textContent = content;

		pre.append(code);
		wrapper.append(label, pre);
		return wrapper;
	}

	function createReferenceBlockElement(target, indent, context, badgeText = "#") {
		const wrapper = document.createElement("div");
		wrapper.className = "wiki-reference-block";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(indent);

		const anchor = createReferenceAnchor(target, context);
		anchor.className = "wiki-reference-link";
		anchor.style.display = "grid";
		anchor.style.gap = "0.2rem";
		anchor.style.padding = "0.8rem 0.95rem";
		anchor.style.border = "1px solid rgba(36, 29, 24, 0.14)";
		anchor.style.borderRadius = "0.9rem";
		anchor.style.background = "rgba(255, 252, 247, 0.84)";
		anchor.style.boxShadow = "0 8px 20px rgba(70, 49, 31, 0.05)";
		anchor.style.textDecoration = "none";

		const badge = document.createElement("div");
		badge.textContent = badgeText;
		badge.style.color = "var(--muted)";
		badge.style.fontSize = "0.75rem";
		badge.style.fontWeight = "700";
		badge.style.letterSpacing = "0.08em";
		badge.style.textTransform = "uppercase";

		const title = document.createElement("strong");
		title.textContent = defaultAssetLabel(target, context);
		title.style.color = "var(--ink)";
		title.style.fontSize = "1rem";

		const path = document.createElement("code");
		path.textContent = target;
		path.style.color = "var(--muted)";
		path.style.fontSize = "0.88rem";
		path.style.whiteSpace = "pre-wrap";
		path.style.wordBreak = "break-word";

		anchor.append(badge, title, path);
		wrapper.append(anchor);
		return wrapper;
	}

	function createCompactObjectNode(objectData) {
		const wrapper = document.createElement("span");
		wrapper.className = "wiki-object-inline";
		wrapper.style.display = "inline-flex";
		wrapper.style.verticalAlign = "middle";
		wrapper.style.margin = "0.08rem 0";
		wrapper.style.maxWidth = "100%";
		wrapper.style.borderRadius = "0.55rem";
		wrapper.style.background = "var(--object)";
		wrapper.style.overflow = "hidden";

		const label = document.createElement("span");
		label.textContent = "." + objectData.kind;
		label.style.flex = "0 0 auto";
		label.style.padding = "0.22rem 0.42rem";
		label.style.background = "rgba(36, 29, 24, 0.08)";
		label.style.color = "var(--muted)";
		label.style.fontSize = "0.78rem";
		label.style.fontWeight = "700";
		label.style.letterSpacing = "0.05em";
		label.style.textTransform = "uppercase";

		const code = document.createElement("code");
		code.className = "wiki-object";
		code.dataset.kind = objectData.kind;
		code.textContent = objectData.value;
		code.style.display = "block";
		code.style.padding = "0.22rem 0.5rem";
		code.style.background = "transparent";
		code.style.whiteSpace = "pre-wrap";

		wrapper.append(label, code);
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

			const hashBlock = readHashBlock(lines, index);
			if (hashBlock) {
				flushParagraph();
				blocks.push(hashBlock.block);
				index = hashBlock.nextIndex;
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

		while (index < lines.length && lines[index].trim().startsWith("[|")) {
			const rowBlock = readDelimitedBlock(lines, index, "[|", "|]");
			if (!rowBlock) {
				return null;
			}

			rows.push(parseTableRow(rowBlock.content));
			index = rowBlock.nextIndex + 1;

			if (index < lines.length && lines[index].trim() === "") {
				break;
			}
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

	function readHashBlock(lines, startIndex) {
		const blockData = readStandaloneDelimitedBlock(lines, startIndex, "[#", "#]");
		if (!blockData) {
			return null;
		}

		return {
			block: {
				type: "hashBlock",
				indent: countIndent(lines[startIndex]),
				content: blockData.content
			},
			nextIndex: blockData.nextIndex
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

	function readStandaloneDelimitedBlock(lines, startIndex, openingToken, closingToken) {
		const firstLine = lines[startIndex];
		const baseWhitespace = firstLine.match(/^[ \t]*/)[0];
		const firstContent = trimSharedIndent(firstLine, baseWhitespace).trimStart();
		if (!firstContent.startsWith(openingToken)) {
			return null;
		}

		let content = firstContent.slice(openingToken.length);
		const firstCloseIndex = content.indexOf(closingToken);
		if (firstCloseIndex !== -1) {
			if (content.slice(firstCloseIndex + closingToken.length).trim() !== "") {
				return null;
			}

			return {
				content: content.slice(0, firstCloseIndex),
				nextIndex: startIndex
			};
		}

		for (let index = startIndex + 1; index < lines.length; index += 1) {
			const lineContent = trimSharedIndent(lines[index], baseWhitespace);
			const closeIndex = lineContent.indexOf(closingToken);

			if (closeIndex !== -1) {
				if (lineContent.slice(closeIndex + closingToken.length).trim() !== "") {
					return null;
				}

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

	function normalizeBlockLines(lines) {
		const decodedLines = lines.map((line) => decodeLegacyEscapePlaceholders(line));
		return decodedLines.filter((line, index, values) => {
			return line !== "" || index !== values.length - 1 || values.length === 1;
		});
	}

	function parseCompactObjectData(content) {
		const separatorIndex = findFirstUnescapedPipe(content);
		if (separatorIndex === -1) {
			return null;
		}

		return {
			kind: normalizeObjectKind(content.slice(0, separatorIndex)),
			value: decodeLegacyEscapePlaceholders(content.slice(separatorIndex + 1))
		};
	}

	function findFirstUnescapedPipe(text) {
		let index = 0;
		while (index < text.length) {
			if (
				text.startsWith("[[", index) ||
				text.startsWith("]]", index) ||
				text.startsWith("[|]", index)
			) {
				index += text.startsWith("[|]", index) ? 3 : 2;
				continue;
			}

			if (text[index] === "|") {
				return index;
			}

			index += 1;
		}

		return -1;
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

	function describeHeadings(blocks, context, fallbackTitle) {
		const usedIds = new Map();
		const entries = [];
		let headingCount = 0;

		for (const block of blocks) {
			if (block.type !== "heading") {
				continue;
			}

			headingCount += 1;
			block.headingIndex = headingCount;

			const plainText = extractInlineText(block.text, context) || fallbackTitle;
			block.anchorId = createHeadingAnchorId(plainText || "abschnitt", usedIds);
			entries.push({
				anchorId: block.anchorId,
				level: 1,
				text: block.text
			});
		}

		return { entries, title: fallbackTitle };
	}

	function extractInlineText(text, context) {
		const probe = document.createElement("span");
		context.appendNodes(probe, parseInline(text, context));
		return probe.textContent.trim();
	}

	function createHeadingAnchorId(text, usedIds) {
		const baseId = text
			.toLowerCase()
			.normalize("NFD")
			.replace(/[\u0300-\u036f]/g, "")
			.replace(/[^a-z0-9]+/g, "-")
			.replace(/^-+|-+$/g, "") || "abschnitt";

		const seenCount = usedIds.get(baseId) || 0;
		usedIds.set(baseId, seenCount + 1);
		return seenCount === 0 ? baseId : baseId + "-" + (seenCount + 1);
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
				buffer += ESCAPED_OPEN_BRACKET;
				index += 2;
				continue;
			}

			if (text.startsWith("[|]", index)) {
				buffer += ESCAPED_PIPE;
				index += 3;
				continue;
			}

			if (text.startsWith("]]", index)) {
				buffer += ESCAPED_CLOSE_BRACKET;
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

				if (marker === ".") {
					const objectToken = readTokenContent(text, index + 2, ".]");
					const objectData = objectToken ? parseCompactObjectData(objectToken.content) : null;
					if (objectToken && objectData) {
						flushBuffer();
						nodes.push(createCompactObjectNode(objectData));
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
				appendLiteralTextNodes(nodes, text.slice(lastIndex, match.index));
			}

			if (isLiteralWikiSyntaxToken(text, match.index, match[0].length)) {
				appendLiteralTextNodes(nodes, match[0]);
			} else {
				nodes.push(...createTextTokenNodes(match[0], context));
			}
			lastIndex = match.index + match[0].length;
		}

		if (lastIndex < text.length) {
			appendLiteralTextNodes(nodes, text.slice(lastIndex));
		}
	}

	function appendLiteralTextNodes(nodes, text) {
		if (!text) {
			return;
		}

		let buffer = "";

		const flushBuffer = () => {
			if (!buffer) {
				return;
			}

			nodes.push(document.createTextNode(buffer));
			buffer = "";
		};

		for (const char of text) {
			if (char === ESCAPED_OPEN_BRACKET) {
				buffer += "[";
				continue;
			}

			if (char === ESCAPED_CLOSE_BRACKET) {
				buffer += "]";
				continue;
			}

			if (char === ESCAPED_PIPE) {
				buffer += "|";
				continue;
			}

			if (char === "[" || char === "]" || char === "|") {
				flushBuffer();
				nodes.push(createSyntaxErrorNode(char));
				continue;
			}

			buffer += char;
		}

		flushBuffer();
	}

	function createSyntaxErrorNode(text) {
		const marker = document.createElement("span");
		marker.className = "wiki-syntax-error";
		marker.textContent = text;
		marker.style.padding = "0 0.08rem";
		marker.style.background = "#ff3333";
		marker.style.color = "#111111";
		return marker;
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
		const value = decodeLegacyEscapePlaceholders(content).trim();
		if (value && isImageTarget(value)) {
			return createImageNode(value, context, true);
		}

		if (value && isReferenceTarget(value, context)) {
			return createInlineReferenceNode(value, context);
		}

		const code = document.createElement("code");
		code.className = "wiki-object";
		code.textContent = value;
		return code;
	}

	function createInlineReferenceNode(target, context) {
		const anchor = createReferenceAnchor(target, context);
		anchor.className = "wiki-object";
		anchor.textContent = defaultAssetLabel(target, context);
		anchor.style.display = "inline-block";
		anchor.style.textDecoration = "none";
		anchor.style.whiteSpace = "nowrap";
		anchor.title = target;
		return anchor;
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
		image.style.border = "1px solid rgba(36, 29, 24, 0.24)";
		image.style.borderRadius = "0.2rem";
		image.style.background = "#ffffff";

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

	function isReferenceTarget(target, context) {
		return Boolean(target) && (
			context.isExternalLink(target) ||
			context.hasKnownRenderableExtension(target) ||
			hasExplicitFileExtension(target) ||
			/[\\/]/.test(target)
		);
	}

	function createReferenceAnchor(target, context) {
		const anchor = document.createElement("a");
		const reference = resolveReferenceTarget(target, context);
		anchor.href = reference.href;
		if (reference.external) {
			anchor.target = "_blank";
			anchor.rel = "noreferrer noopener";
		}
		return anchor;
	}

	function resolveReferenceTarget(target, context) {
		if (context.isExternalLink(target)) {
			return {
				href: context.normalizeExternalLink(target),
				external: true
			};
		}

		if (context.hasKnownRenderableExtension(target)) {
			const resolvedPath = context.resolveRelativePath(context.filePath, target);
			return {
				href: resolvedPath ? context.createNavigationUrl(resolvedPath) : "#",
				external: false
			};
		}

		return {
			href: resolveAssetUrl(target, context),
			external: false
		};
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
			if (
				content.startsWith("[[", index) ||
				content.startsWith("]]", index) ||
				content.startsWith("[|]", index)
			) {
				index += content.startsWith("[|]", index) ? 3 : 2;
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
		return decodeLegacyEscapePlaceholders(text)
			.replace(/\[\[/g, "[")
			.replace(/\[\|\]/g, "|")
			.replace(/\]\]/g, "]");
	}

	function indentToMargin(indent) {
		return "calc(" + indent + " * 1.6ch)";
	}

	window.WikiViewer = {
		render
	};
})();
