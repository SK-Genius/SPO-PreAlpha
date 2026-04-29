(function () {
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
	const IMAGE_RESOURCE_EXTENSIONS = new Set([
		"avif",
		"bmp",
		"gif",
		"ico",
		"jpeg",
		"jpg",
		"png",
		"svg",
		"webp"
	]);
	const VIDEO_RESOURCE_EXTENSIONS = new Set([
		"mp4",
		"ogv",
		"webm"
	]);
	const LEGACY_CHROME = {
		frameBorder: "1px solid rgba(125, 102, 78, 0.28)",
		dividerBorder: "1px solid rgba(127, 79, 36, 0.16)",
		panelBackground:
			"linear-gradient(180deg, rgba(255, 255, 255, 0.94), rgba(249, 243, 233, 0.92))",
		surfaceBackground:
			"linear-gradient(180deg, rgba(255, 255, 255, 0.9), rgba(249, 243, 233, 0.88))",
		badgeBackground:
			"linear-gradient(180deg, rgba(247, 237, 219, 0.98), rgba(239, 224, 202, 0.94))",
		badgeText: "var(--accent)",
		sectionAccent:
			"linear-gradient(180deg, rgba(236, 218, 194, 0.98), rgba(221, 198, 170, 0.94))",
		controlBackground:
			"linear-gradient(180deg, rgba(247, 237, 219, 0.88), rgba(239, 224, 202, 0.82))",
		controlLinkBackground:
			"linear-gradient(180deg, rgba(255, 255, 255, 0.92), rgba(249, 243, 233, 0.9))",
		railBackground:
			"linear-gradient(180deg, rgba(255, 255, 255, 0.7), rgba(239, 224, 202, 0.52) 48%, rgba(214, 189, 158, 0.56))",
		markerColor: "#7f4f24",
		tableStripeA: "rgba(247, 237, 219, 0.78)",
		tableStripeB: "rgba(255, 252, 247, 0.95)"
	};
	let embeddedResourceCounter = 0;

	function render(source, context) {
		context.prepareArticle("wiki-article");

		const normalizedSource = normalizeLegacyEscapes(source);
		const fragment = document.createDocumentFragment();
		const blocks = parseBlocks(normalizedSource);
		const articleInfo = resolveArticleTitle(context.filePath, context);
		const headingState = describeHeadings(blocks, context, articleInfo.title);

		if (!blocks.length) {
			const paragraph = document.createElement("p");
			paragraph.className = "status";
			paragraph.textContent = "Die Datei ist leer.";
			context.setPageTitle(articleInfo.fullTitle, "Wiki");
			context.articleElement.append(paragraph);
			return;
		}

		fragment.append(
			createAnchorTarget("wiki-begin"),
			createArticleHeader(articleInfo, context.filePath)
		);

		if (headingState.entries.length > 0) {
			fragment.append(createTableOfContents(headingState.entries, context));
		}

		appendRenderedBlocks(fragment, blocks, context);

		fragment.append(createAnchorTarget("wiki-end"));
		context.setPageTitle(articleInfo.fullTitle, "Wiki");
		context.articleElement.append(fragment);
	}

	function appendRenderedBlocks(parent, blocks, context, compact = false) {
		for (let index = 0; index < blocks.length; index += 1) {
			const element = createBlockElement(blocks[index], context);
			if (compact) {
				element.style.marginBottom = index === blocks.length - 1 ? "0" : "0.35rem";
			}

			parent.append(element);
		}
	}

	function createBlockElement(block, context) {
		if (block.type === "heading") {
			return createHeadingElement(block, context);
		}

		if (block.type === "rule") {
			return createRuleElement();
		}

		if (block.type === "tableBlock") {
			return createTableElement(block, context);
		}

		if (block.type === "hashBlock") {
			return createHashBlockElement(block, context);
		}

		if (block.type === "objectBlock") {
			return createObjectBlockElement(block, context);
		}

		return createParagraphElement(block, context);
	}

	function resolveArticleTitle(filePath, context) {
		const fullTitle = context.fileNameLabel(filePath) || "Wiki";
		const match = fullTitle.match(/^\[([^\]]+)\](.+)$/);
		if (!match) {
			return {
				owner: "",
				title: fullTitle,
				fullTitle
			};
		}

		return {
			owner: match[1].trim(),
			title: match[2].trim() || fullTitle,
			fullTitle
		};
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

	function createLegacyRail(height = "0.625rem") {
		const rail = document.createElement("div");
		rail.style.height = height;
		rail.style.background = LEGACY_CHROME.railBackground;
		rail.style.boxShadow =
			"inset 0 1px 0 rgba(255, 255, 255, 0.45), inset 0 -1px 0 rgba(127, 79, 36, 0.08)";
		return rail;
	}

	function createArticleHeader(articleInfo, filePath) {
		const header = document.createElement("header");
		header.className = "wiki-article-header";
		header.style.margin = "0 0 1.35rem";
		header.style.border = LEGACY_CHROME.frameBorder;
		header.style.borderRadius = "0.95rem";
		header.style.background = LEGACY_CHROME.panelBackground;
		header.style.boxShadow = "0 10px 24px rgba(70, 49, 31, 0.05)";
		header.style.overflow = "hidden";

		const body = document.createElement("div");
		body.style.display = "grid";
		body.style.gridTemplateColumns = "auto minmax(0, 1fr)";
		body.style.alignItems = "stretch";

		const badge = document.createElement("div");
		badge.textContent = "WIKI";
		badge.style.display = "flex";
		badge.style.alignItems = "center";
		badge.style.justifyContent = "center";
		badge.style.minWidth = "6.4rem";
		badge.style.padding = "1rem 1.1rem";
		badge.style.borderInlineEnd = LEGACY_CHROME.dividerBorder;
		badge.style.background = LEGACY_CHROME.badgeBackground;
		badge.style.color = LEGACY_CHROME.badgeText;
		badge.style.fontSize = "0.8rem";
		badge.style.fontWeight = "700";
		badge.style.letterSpacing = "0.16em";
		badge.style.textTransform = "uppercase";

		const content = document.createElement("div");
		content.style.minWidth = "0";
		content.style.padding = "1rem 1.15rem 1.05rem";
		content.style.background = "transparent";

		const heading = document.createElement("div");
		heading.style.display = "flex";
		heading.style.flexWrap = "wrap";
		heading.style.alignItems = "baseline";
		heading.style.gap = "0.32rem";
		heading.style.fontSize = "clamp(1.5rem, 3vw, 2.1rem)";
		heading.style.fontWeight = "700";
		heading.style.lineHeight = "1.1";
		heading.style.letterSpacing = "-0.02em";
		heading.style.textDecoration = "underline";
		heading.style.textDecorationThickness = "0.07em";
		heading.style.textUnderlineOffset = "0.15em";
		heading.style.textDecorationColor = "rgba(127, 79, 36, 0.56)";

		if (articleInfo.owner) {
			const owner = document.createElement("small");
			owner.textContent = "[" + articleInfo.owner + "]";
			owner.style.fontSize = "0.68em";
			owner.style.fontWeight = "700";
			owner.style.color = "var(--accent)";
			owner.style.lineHeight = "1";
			heading.append(owner);
		}

		const titleText = document.createElement("span");
		titleText.textContent = articleInfo.title;
		heading.append(titleText);

		const meta = document.createElement("div");
		meta.textContent = "Datei: " + filePath;
		meta.style.margin = "0.45rem 0 0";
		meta.style.color = "var(--muted)";
		meta.style.fontSize = "0.88rem";
		meta.style.wordBreak = "break-word";

		content.append(heading, meta);
		body.append(badge, content);
		header.append(createLegacyRail(), body, createLegacyRail());
		return header;
	}

	function createHeadingElement(block, context) {
		const section = document.createElement("section");
		section.className = "wiki-section-heading";
		section.style.margin = block.headingIndex === 1
			? "1.1rem 0 0.75rem"
			: "1.55rem 0 0.8rem";
		if (block.anchorId) {
			section.id = block.anchorId;
		}

		if (block.anchorNumber) {
			section.append(createAnchorTarget(block.anchorNumber));
		}

		const heading = document.createElement("div");
		heading.style.display = "flex";
		heading.style.alignItems = "center";
		heading.style.gap = "0";
		heading.style.border = LEGACY_CHROME.frameBorder;
		heading.style.borderRadius = "0.8rem";
		heading.style.background = LEGACY_CHROME.surfaceBackground;
		heading.style.overflow = "hidden";

		const controls = document.createElement("div");
		controls.style.display = "inline-flex";
		controls.style.alignItems = "center";
		controls.style.gap = "0";
		controls.style.marginInlineStart = "auto";
		controls.style.marginInlineEnd = "0";
		controls.style.padding = "0.18rem 0.3rem 0.18rem 0.24rem";
		controls.style.background =
			"linear-gradient(180deg, rgba(247, 237, 219, 0.44), rgba(239, 224, 202, 0.3))";
		controls.style.borderInlineStart = LEGACY_CHROME.dividerBorder;
		controls.style.fontFamily = "inherit";
		controls.style.fontSize = "0.9rem";
		controls.style.whiteSpace = "nowrap";

		const navTop = createSectionNavLink("#wiki-begin", "\u2191", "Zum Anfang");
		const navBottom = createSectionNavLink("#wiki-end", "\u2193", "Zum Ende");
		navBottom.style.borderInlineStart = "1px solid rgba(127, 79, 36, 0.08)";
		controls.append(navTop, navBottom);

		const title = document.createElement("h2");
		title.style.flex = "1 1 auto";
		title.style.margin = "0";
		title.style.minWidth = "0";
		title.style.fontSize = "clamp(1.08rem, 2.1vw, 1.28rem)";
		title.style.fontWeight = "700";
		title.style.lineHeight = "1.2";
		title.style.border = "0";
		title.style.background = "transparent";
		title.style.letterSpacing = "-0.015em";

		const titleLink = block.anchorId
			? document.createElement("a")
			: document.createElement("span");
		if (block.anchorId) {
			titleLink.href = "#" + block.anchorId;
			titleLink.title = "Direktlink";
			titleLink.setAttribute("aria-label", "Direktlink zu " + block.text);
		}
		titleLink.style.display = "block";
		titleLink.style.padding = "0.5rem 0.75rem 0.55rem 1rem";
		titleLink.style.color = "var(--ink)";
		titleLink.style.textDecoration = "underline";
		titleLink.style.textDecorationThickness = "0.06em";
		titleLink.style.textUnderlineOffset = "0.16em";
		titleLink.style.textDecorationColor = "rgba(127, 79, 36, 0.48)";

		context.appendNodes(titleLink, parseInline(block.text, context));
		title.append(titleLink);

		const end = document.createElement("div");
		end.style.width = "0.45rem";
		end.style.alignSelf = "stretch";
		end.style.background = LEGACY_CHROME.sectionAccent;
		end.style.borderInlineStart = LEGACY_CHROME.dividerBorder;

		heading.append(title, controls, end);
		section.append(heading);
		return section;
	}

	function createSectionNavLink(href, text, title) {
		const link = document.createElement("a");
		link.href = href;
		link.textContent = text;
		link.title = title;
		link.setAttribute("aria-label", title);
		link.style.display = "inline-flex";
		link.style.alignItems = "center";
		link.style.justifyContent = "center";
		link.style.minWidth = "1.65rem";
		link.style.minHeight = "1.65rem";
		link.style.padding = "0";
		link.style.background = "transparent";
		link.style.color = "rgba(127, 79, 36, 0.64)";
		link.style.textDecoration = "none";
		link.style.textAlign = "center";
		link.style.lineHeight = "1";
		link.style.fontWeight = "600";
		link.style.borderRadius = "0.55rem";
		link.style.opacity = "0.82";
		link.style.transition = "background-color 120ms ease, color 120ms ease, opacity 120ms ease";

		const setInteractiveState = (active) => {
			link.style.background = active
				? "rgba(255, 252, 247, 0.36)"
				: "transparent";
			link.style.color = active
				? "rgba(36, 29, 24, 0.86)"
				: "rgba(127, 79, 36, 0.64)";
			link.style.opacity = active ? "1" : "0.82";
		};

		link.addEventListener("mouseenter", () => setInteractiveState(true));
		link.addEventListener("mouseleave", () => setInteractiveState(false));
		link.addEventListener("focus", () => setInteractiveState(true));
		link.addEventListener("blur", () => setInteractiveState(false));

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
			marker.style.display = "inline-flex";
			marker.style.alignItems = "center";
			marker.style.justifyContent = "center";
			marker.style.minWidth = "1.1rem";
			marker.style.marginInlineEnd = "0.45rem";
			item.append(marker);
			appendLegacyMarkerGlyph(marker, block.marker);
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
		nav.style.margin = "0 0 1.4rem";
		nav.style.border = LEGACY_CHROME.frameBorder;
		nav.style.borderRadius = "0.95rem";
		nav.style.background = LEGACY_CHROME.panelBackground;
		nav.style.boxShadow = "0 10px 24px rgba(70, 49, 31, 0.05)";
		nav.style.overflow = "hidden";

		const body = document.createElement("div");
		body.style.display = "grid";
		body.style.gridTemplateColumns = "auto minmax(0, 1fr)";
		body.style.alignItems = "stretch";

		const title = document.createElement("div");
		title.textContent = "Inhalt";
		title.style.display = "flex";
		title.style.alignItems = "center";
		title.style.justifyContent = "center";
		title.style.minWidth = "6.4rem";
		title.style.padding = "0.95rem 1rem";
		title.style.background = LEGACY_CHROME.badgeBackground;
		title.style.borderInlineEnd = LEGACY_CHROME.dividerBorder;
		title.style.color = LEGACY_CHROME.badgeText;
		title.style.fontSize = "0.92rem";
		title.style.fontWeight = "700";
		title.style.letterSpacing = "0.08em";

		const list = document.createElement("ul");
		list.style.columnWidth = "13rem";
		list.style.columnGap = "1.5rem";
		list.style.margin = "0";
		list.style.padding = "0.95rem 1.1rem 1rem";
		list.style.listStyle = "none";

		for (const entry of entries) {
			const item = document.createElement("li");
			item.style.margin = "0 0 0.28rem";
			item.style.breakInside = "avoid";

			const link = document.createElement("a");
			link.href = "#" + entry.anchorId;
			link.style.display = "inline-block";
			link.style.color = "var(--ink)";
			link.style.lineHeight = "1.35";
			link.style.textDecorationColor = "rgba(127, 79, 36, 0.46)";

			context.appendNodes(link, parseInline(entry.text, context));
			item.append(link);
			list.append(item);
		}

		body.append(title, list);
		nav.append(createLegacyRail(), body, createLegacyRail());
		return nav;
	}

	function appendLegacyMarkerGlyph(marker, type) {
		if (type === ">") {
			const triangle = document.createElement("span");
			triangle.style.display = "inline-block";
			triangle.style.width = "0";
			triangle.style.height = "0";
			triangle.style.borderTop = "0.34rem solid transparent";
			triangle.style.borderBottom = "0.34rem solid transparent";
			triangle.style.borderLeft = "0.54rem solid " + LEGACY_CHROME.markerColor;
			marker.append(triangle);
			return;
		}

		const dash = document.createElement("span");
		dash.style.display = "inline-block";
		dash.style.width = "0.68rem";
		dash.style.height = "0.14rem";
		dash.style.background = LEGACY_CHROME.markerColor;
		marker.append(dash);
	}

	function createRuleElement(inline = false) {
		const rule = document.createElement("div");
		rule.className = "wiki-rule";
		rule.style.display = inline ? "inline-block" : "block";
		rule.style.width = inline ? "10rem" : "100%";
		rule.style.maxWidth = "100%";
		rule.style.height = "0.625rem";
		rule.style.margin = inline ? "0 0.35rem" : "1rem 0";
		rule.style.border = LEGACY_CHROME.dividerBorder;
		rule.style.borderRadius = "999px";
		rule.style.background = LEGACY_CHROME.railBackground;
		if (inline) {
			rule.style.verticalAlign = "middle";
		}
		return rule;
	}

	function tryParseHeadingText(content) {
		if (!content.startsWith("[=") || !content.endsWith("=]")) {
			return null;
		}

		const text = content.slice(2, -2);
		if (!text || text.startsWith("=") || text.endsWith("=")) {
			return null;
		}

		const normalizedText = text.trim();
		return normalizedText || null;
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
		table.style.border = LEGACY_CHROME.frameBorder;
		table.style.background = "rgba(255, 252, 247, 0.96)";

		for (let rowIndex = 0; rowIndex < block.rows.length; rowIndex += 1) {
			const row = document.createElement("tr");
			row.style.background = rowIndex % 2 === 0
				? LEGACY_CHROME.tableStripeA
				: LEGACY_CHROME.tableStripeB;

			for (const cellText of block.rows[rowIndex]) {
				const cell = document.createElement("td");
				cell.style.padding = "0.5rem 0.65rem";
				cell.style.border = LEGACY_CHROME.dividerBorder;
				cell.style.verticalAlign = "top";
				appendLegacyTextFlow(cell, cellText, context, true);
				row.append(cell);
			}

			table.append(row);
		}

		wrapper.append(table);
		return wrapper;
	}

	function createObjectBlockElement(block, context) {
		const resource = createEmbeddedResourceDescriptor(block, context);
		if (resource) {
			return createResourceBlockElement(resource, block.indent, context, "." + resource.extension);
		}

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

		const resource = createExternalResourceDescriptor(content, context);
		if (resource) {
			return createResourceBlockElement(resource, block.indent, context, "#");
		}

		if (isImageTarget(content)) {
			return createImageBlockElement(content, block.indent, context);
		}

		if (isReferenceTarget(content, context)) {
			return createReferenceBlockElement(content, block.indent, context, "#");
		}

		return createCodeBlockElement("#", rawContent, block.indent, "txt");
	}

	function createEmbeddedResourceDescriptor(block, context) {
		const extension = normalizeResourceExtension(block.kind);
		if (!extension) {
			return null;
		}

		const lines = normalizeBlockLines(block.lines);
		const filePath = createEmbeddedResourceFilePath(context.filePath, extension);
		if (context.isTextResourceExtension(extension)) {
			return {
				sourceType: "embedded",
				dataKind: "text",
				extension,
				filePath,
				text: lines.join("\n")
			};
		}

		return {
			sourceType: "embedded",
			dataKind: "binary",
			extension,
			filePath,
			base64: lines.join("")
		};
	}

	function createExternalResourceDescriptor(target, context) {
		const extension = extractResourceExtension(target);
		if (!extension) {
			return null;
		}

		return {
			sourceType: "external",
			dataKind: context.isTextResourceExtension(extension) ? "text" : "binary",
			extension,
			target,
			filePath: resolveResourceFilePath(target, context)
		};
	}

	function createEmbeddedResourceFilePath(parentFilePath, extension) {
		const normalizedParentPath = String(parentFilePath || "").replace(/\\/g, "/");
		const separatorIndex = normalizedParentPath.lastIndexOf("/");
		const directory = separatorIndex === -1
			? ""
			: normalizedParentPath.slice(0, separatorIndex + 1);
		embeddedResourceCounter += 1;
		return directory + "__embedded_" + embeddedResourceCounter + "." + extension;
	}

	function createResourceBlockElement(resource, indent, context, badgeText) {
		const wrapper = document.createElement("div");
		wrapper.className = "wiki-resource-block";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(indent);

		const content = document.createElement("div");
		wrapper.append(content);
		renderResourceBlockContent(content, resource, context, badgeText);
		return wrapper;
	}

	function renderResourceBlockContent(container, resource, context, badgeText) {
		container.replaceChildren(createResourceMessageElement("Ressource wird geladen ..."));

		void (async () => {
			try {
				const viewer = await resolveResourceViewer(resource, context);
				if (viewer) {
					const renderInput = await createResourceRenderInput(resource, context);
					viewer.render(
						renderInput,
						context.createChildContext(resource.filePath, container, { suppressTitle: true })
					);
					return;
				}

				container.replaceChildren(
					await createResourceFallbackElement(resource, badgeText, context)
				);
			} catch (error) {
				container.replaceChildren(
					createResourceErrorElement(resource, badgeText, context, error)
				);
			}
		})();
	}

	async function resolveResourceViewer(resource, context) {
		return context.resolveViewerForExtension(resource.extension) ||
			await context.ensureViewerLoaded(resource.extension) ||
			null;
	}

	async function createResourceRenderInput(resource, context) {
		const preparedResource = await ensureResourceLoaded(resource, context);
		if (preparedResource.dataKind === "text") {
			return preparedResource.text;
		}

		return {
			kind: "binary-resource",
			bytes: preparedResource.bytes,
			mimeType: context.getMimeTypeForExtension(preparedResource.extension),
			objectUrl: ensureResourceObjectUrl(preparedResource, context),
			filePath: preparedResource.filePath
		};
	}

	async function createResourceFallbackElement(resource, badgeText, context) {
		const preparedResource = await ensureResourceLoaded(resource, context);
		if (isImageResourceExtension(preparedResource.extension)) {
			return createResourceImageElement(preparedResource, context);
		}

		if (isVideoResourceExtension(preparedResource.extension)) {
			return createResourceVideoElement(preparedResource, context);
		}

		if (preparedResource.dataKind === "text") {
			const codeBlock = createCodeBlockElement(
				"." + preparedResource.extension,
				preparedResource.text,
				0,
				preparedResource.extension
			);
			codeBlock.style.margin = "0";
			return codeBlock;
		}

		if (preparedResource.sourceType === "external") {
			const referenceBlock = createReferenceBlockElement(
				preparedResource.target,
				0,
				context,
				badgeText
			);
			referenceBlock.style.margin = "0";
			return referenceBlock;
		}

		return createResourceMessageElement(
			"Fuer ." + preparedResource.extension + " ist noch kein passender Viewer vorhanden."
		);
	}

	async function ensureResourceLoaded(resource, context) {
		if (resource.isLoaded) {
			return resource;
		}

		if (resource.sourceType === "external") {
			const loadedResource = await context.loadResourceBytes(resource.target);
			resource.filePath = loadedResource.path || resource.filePath;
			resource.bytes = loadedResource.bytes;
			if (resource.dataKind === "text") {
				resource.text = context.decodeTextBytes(loadedResource.bytes);
			}

			resource.isLoaded = true;
			return resource;
		}

		if (resource.dataKind === "text") {
			resource.isLoaded = true;
			return resource;
		}

		resource.bytes = decodeBase64Resource(resource.base64);
		resource.isLoaded = true;
		return resource;
	}

	function ensureResourceObjectUrl(resource, context) {
		if (resource.objectUrl) {
			return resource.objectUrl;
		}

		let bytes = resource.bytes;
		if (!bytes && resource.dataKind === "text") {
			bytes = new TextEncoder().encode(resource.text);
		}

		if (!bytes) {
			return "";
		}

		resource.objectUrl = URL.createObjectURL(
			new Blob([bytes], { type: context.getMimeTypeForExtension(resource.extension) })
		);
		return resource.objectUrl;
	}

	function decodeBase64Resource(base64Text) {
		const normalizedBase64 = String(base64Text || "").replace(/\s+/g, "");
		if (!normalizedBase64) {
			return new Uint8Array(0);
		}

		const rawText = atob(normalizedBase64);
		const bytes = new Uint8Array(rawText.length);
		for (let index = 0; index < rawText.length; index += 1) {
			bytes[index] = rawText.charCodeAt(index);
		}

		return bytes;
	}

	function createResourceMessageElement(message) {
		const card = document.createElement("div");
		card.className = "wiki-resource-message";
		card.style.padding = "0.8rem 0.9rem";
		card.style.border = LEGACY_CHROME.frameBorder;
		card.style.borderRadius = "0.45rem";
		card.style.background = LEGACY_CHROME.surfaceBackground;
		card.style.color = "var(--muted)";
		card.textContent = message;
		return card;
	}

	function createResourceErrorElement(resource, badgeText, context, error) {
		const card = document.createElement("div");
		card.className = "wiki-resource-message";
		card.style.padding = "0.8rem 0.9rem";
		card.style.border = "1px solid rgba(127, 29, 29, 0.28)";
		card.style.borderRadius = "0.45rem";
		card.style.background = "rgba(202, 70, 70, 0.08)";

		const title = document.createElement("strong");
		title.textContent = "Die Ressource konnte nicht gerendert werden.";
		title.style.display = "block";
		title.style.margin = "0 0 0.2rem";
		title.style.color = "var(--error)";

		const detail = document.createElement("div");
		detail.style.color = "var(--muted)";
		detail.style.whiteSpace = "pre-wrap";
		detail.textContent = [
			resource.sourceType === "external"
				? badgeText + " " + resource.target
				: "." + resource.extension,
			error && error.message ? error.message : "Unbekannter Fehler."
		].join("\n");

		card.append(title, detail);
		return card;
	}

	function createResourceImageElement(resource, context) {
		const image = createImageNodeFromUrl(
			ensureResourceObjectUrl(resource, context),
			context.fileNameLabel(resource.filePath),
			false
		);

		const wrapper = document.createElement("div");
		wrapper.className = "wiki-object-image";
		wrapper.style.margin = "0";
		wrapper.append(image);
		return wrapper;
	}

	function createResourceVideoElement(resource, context) {
		const video = document.createElement("video");
		video.controls = true;
		video.preload = "metadata";
		video.style.display = "block";
		video.style.maxWidth = "100%";
		video.style.height = "auto";
		video.style.border = "1px solid #000000";
		video.style.background = "#ffffff";
		video.src = ensureResourceObjectUrl(resource, context);

		const wrapper = document.createElement("div");
		wrapper.className = "wiki-object-video";
		wrapper.style.margin = "0";
		wrapper.append(video);
		return wrapper;
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

		const showLabel = !(kind === "txt" && labelText === ".txt");
		const label = showLabel
			? document.createElement("div")
			: null;
		if (label) {
			label.className = "wiki-object-block-label";
			label.textContent = labelText;
			label.style.margin = "0 0 0.35rem";
			label.style.color = "var(--muted)";
			label.style.fontSize = "0.8rem";
			label.style.fontWeight = "700";
			label.style.letterSpacing = "0.08em";
			label.style.textTransform = "uppercase";
		}

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
		if (label) {
			wrapper.append(label);
		}
		wrapper.append(pre);
		return wrapper;
	}

	function createReferenceBlockElement(target, indent, context, badgeText = "#") {
		const wrapper = document.createElement("div");
		wrapper.className = "wiki-reference-block";
		wrapper.style.margin = "0 0 1rem";
		wrapper.style.marginInlineStart = indentToMargin(indent);

		const anchor = createReferenceAnchor(target, context);
		anchor.className = "wiki-reference-link";
		anchor.style.display = "block";
		anchor.style.padding = "0.65rem 0.8rem";
		anchor.style.border = "1px solid #000000";
		anchor.style.background = "#ffffdd";
		anchor.style.color = "#000000";
		anchor.style.textDecoration = "none";

		const badge = document.createElement("div");
		badge.textContent = badgeText;
		badge.style.color = "#000000";
		badge.style.fontSize = "0.75rem";
		badge.style.fontWeight = "700";
		badge.style.letterSpacing = "0.08em";
		badge.style.textTransform = "uppercase";
		badge.style.margin = "0 0 0.18rem";

		const title = document.createElement("strong");
		title.textContent = defaultAssetLabel(target, context);
		title.style.display = "block";
		title.style.color = "#000000";
		title.style.fontSize = "1rem";
		title.style.textDecoration = "underline";
		title.style.margin = "0 0 0.2rem";

		const path = document.createElement("code");
		path.textContent = target;
		path.style.color = "#333333";
		path.style.background = "transparent";
		path.style.padding = "0";
		path.style.fontSize = "0.88rem";
		path.style.fontFamily = "\"Cascadia Code\", Consolas, \"SFMono-Regular\", \"Courier New\", monospace";
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
		wrapper.style.border = "1px solid #000000";
		wrapper.style.background = "#ffffdd";
		wrapper.style.overflow = "hidden";

		const code = document.createElement("code");
		code.className = "wiki-object";
		code.dataset.kind = objectData.kind;
		code.textContent = objectData.value;
		code.style.display = "block";
		code.style.padding = "0.22rem 0.5rem";
		code.style.background = "transparent";
		code.style.whiteSpace = "pre-wrap";

		wrapper.append(code);
		return wrapper;
	}

	function applyLegacyInlineObjectStyle(element) {
		element.style.display = "inline-block";
		element.style.padding = "0.08rem 0.38rem";
		element.style.border = "1px solid #000000";
		element.style.background = "#ffffdd";
		element.style.borderRadius = "0";
		element.style.fontFamily = "\"Cascadia Code\", Consolas, \"SFMono-Regular\", \"Courier New\", monospace";
		element.style.fontSize = "0.92em";
	}

	function appendInlineLines(parent, lines, context) {
		for (let index = 0; index < lines.length; index += 1) {
			if (index > 0) {
				// Paragraph line breaks are soft wraps so the source may use
				// one sentence per line without forcing visible `<br>` output.
				parent.append(document.createTextNode(" "));
			}

			context.appendNodes(parent, parseInline(lines[index], context));
		}
	}

	function appendLegacyTextFlow(parent, source, context, compact = false) {
		const blocks = parseBlocks(source);
		if (!blocks.length) {
			return;
		}

		appendRenderedBlocks(parent, blocks, context, compact);
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

			const headingText = tryParseHeadingText(content);
			if (headingText !== null) {
				flushParagraph();
				blocks.push({
					type: "heading",
					text: headingText
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
			const rowBlock = readTableRowBlock(lines, index);
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

	// Table rows use [| ... |], so cell parsing must ignore separators that
	// belong to nested wiki syntax and must not confuse [|] with the row end.
	function readTableRowBlock(lines, startIndex) {
		const firstLine = lines[startIndex];
		const baseWhitespace = firstLine.match(/^[ \t]*/)[0];
		const firstContent = trimSharedIndent(firstLine, baseWhitespace).trimStart();
		if (!firstContent.startsWith("[|")) {
			return null;
		}

		let content = "";
		let lineIndex = startIndex;
		let lineContent = firstContent.slice(2);
		let bracketDepth = 0;

		while (lineIndex < lines.length) {
			let charIndex = 0;

			while (charIndex < lineContent.length) {
				if (lineContent.startsWith("[|]", charIndex)) {
					content += "[|]";
					charIndex += 3;
					continue;
				}

				if (
					lineContent.startsWith("[[", charIndex) ||
					lineContent.startsWith("]]", charIndex)
				) {
					content += lineContent.slice(charIndex, charIndex + 2);
					charIndex += 2;
					continue;
				}

				if (bracketDepth === 0 && lineContent.startsWith("|]", charIndex)) {
					return {
						content,
						nextIndex: lineIndex
					};
				}

				const char = lineContent[charIndex];
				content += char;

				if (char === "[") {
					bracketDepth += 1;
				} else if (char === "]" && bracketDepth > 0) {
					bracketDepth -= 1;
				}

				charIndex += 1;
			}

			lineIndex += 1;
			if (lineIndex >= lines.length) {
				return null;
			}

			content += "\n";
			lineContent = trimSharedIndent(lines[lineIndex], baseWhitespace);
		}

		return null;
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
		// Embedded resource blocks start with a standalone opener like `[.txt`.
		// Compact inline objects such as `[.txt|value.]` may begin a sentence and
		// must stay in the paragraph parser instead of swallowing following lines.
		if (!rawKind || /[\s|\]]/.test(rawKind)) {
			return null;
		}

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
		let bracketDepth = 0;

		while (index < normalizedContent.length) {
			if (normalizedContent.startsWith("[|]", index)) {
				buffer += "[|]";
				index += 3;
				continue;
			}

			if (
				normalizedContent.startsWith("[[", index) ||
				normalizedContent.startsWith("]]", index)
			) {
				buffer += normalizedContent.slice(index, index + 2);
				index += 2;
				continue;
			}

			const char = normalizedContent[index];
			if (char === "|" && bracketDepth === 0) {
				cells.push(buffer);
				buffer = "";
				index += 1;
				continue;
			}

			buffer += char;
			if (char === "[") {
				bracketDepth += 1;
			} else if (char === "]" && bracketDepth > 0) {
				bracketDepth -= 1;
			}
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
			block.anchorNumber = String(headingCount);

			const plainText = extractInlineText(block.text, context) || fallbackTitle;
			block.anchorId = createHeadingAnchorId(plainText || "abschnitt", usedIds);
			entries.push({
				anchorId: block.anchorId,
				anchorNumber: block.anchorNumber,
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

	function parseInline(text, context, stopToken = null, startIndex = 0, options = {}) {
		const nodes = [];
		let buffer = "";
		let index = startIndex;

		const flushBuffer = () => {
			if (!buffer) {
				return;
			}

			appendTextNodes(nodes, buffer, context, options);
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
				if (text.startsWith("[---]", index)) {
					flushBuffer();
					nodes.push(createRuleElement(true));
					index += 5;
					continue;
				}

				const marker = text[index + 1];
				const inlineMarker = INLINE_MARKERS[marker];

				if (inlineMarker) {
					const inner = parseInline(text, context, inlineMarker.close, index + 2, options);
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

	function appendTextNodes(nodes, text, context, options = {}) {
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
				nodes.push(...createTextTokenNodes(match[0], context, options));
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

	function createTextTokenNodes(token, context, options = {}) {
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

		if (options.disableAutoLinks) {
			nodes.push(document.createTextNode(token));
			return nodes;
		}

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
		applyLegacyInlineObjectStyle(code);
		return code;
	}

	function createInlineReferenceNode(target, context) {
		const anchor = createReferenceAnchor(target, context);
		anchor.className = "wiki-object";
		anchor.textContent = defaultAssetLabel(target, context);
		applyLegacyInlineObjectStyle(anchor);
		anchor.style.textDecoration = "none";
		anchor.style.whiteSpace = "nowrap";
		anchor.title = target;
		return anchor;
	}

	function createImageNode(target, context, inline) {
		return createImageNodeFromUrl(
			resolveAssetUrl(target, context),
			defaultAssetLabel(target, context),
			inline
		);
	}

	function createImageNodeFromUrl(url, altText, inline) {
		const image = document.createElement("img");
		image.className = inline ? "wiki-inline-image" : "wiki-block-image";
		image.src = url;
		image.alt = altText;
		image.loading = "lazy";
		image.decoding = "async";
		image.style.maxWidth = "100%";
		image.style.height = "auto";
		image.style.border = "1px solid #000000";
		image.style.borderRadius = "0";
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

	function resolveResourceFilePath(target, context) {
		if (context.isExternalLink(target)) {
			return context.normalizeExternalLink(target);
		}

		return context.resolveRelativePath(context.filePath, target) || target;
	}

	function extractResourceExtension(target) {
		const cleanedTarget = String(target || "")
			.trim()
			.replace(/[?#].*$/, "");
		const lastSegment = cleanedTarget.split(/[\\/]/).pop() || "";
		const match = /\.([^.]+)$/.exec(lastSegment);
		return match ? normalizeResourceExtension(match[1]) : "";
	}

	function normalizeResourceExtension(extension) {
		return normalizeObjectKind(String(extension || "").replace(/^\.+/, ""));
	}

	function isImageResourceExtension(extension) {
		return IMAGE_RESOURCE_EXTENSIONS.has(normalizeResourceExtension(extension));
	}

	function isVideoResourceExtension(extension) {
		return VIDEO_RESOURCE_EXTENSIONS.has(normalizeResourceExtension(extension));
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

		context.appendNodes(anchor, parseInline(label, context, null, 0, { disableAutoLinks: true }));

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
