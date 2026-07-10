export const extensions = ["wiki"];
export const isBinary = false;

const DEBUG = false;
const MAX_PARSE_STEPS = 10000;
const LF = String.fromCharCode(10);
const CR = String.fromCharCode(13);

function log(...args) {
	if (DEBUG) console.log("[viewer]", ...args);
}

function normalizeNewlines(source) {
	return String(source || "").split(CR + LF).join(LF).split(LF + CR).join(LF).split(CR).join(LF);
}

function splitLines(source) {
	const lines = normalizeNewlines(source).split(LF);
	if (lines.length > 0 && lines[lines.length - 1] === "") lines.pop();
	return lines;
}

function isWhitespaceChar(ch) {
	return ch === " " || ch === String.fromCharCode(9);
}

function trimText(text) {
	let start = 0;
	let end = String(text || "").length;
	const value = String(text || "");
	while (start < end && isWhitespaceChar(value[start])) start += 1;
	while (end > start && isWhitespaceChar(value[end - 1])) end -= 1;
	return value.slice(start, end);
}

function isEmptyLine(line) {
	return trimText(line) === "";
}

function createParserState(source) {
	return {
		lines: splitLines(source),
		index: 0,
		steps: 0
	};
}

function step(state, label) {
	state.steps += 1;
	if (state.steps > MAX_PARSE_STEPS) {
		throw new Error("Parser watchdog near line " + (state.index + 1) + " while " + label);
	}
}

export function parse(source) {
	const normalizedSource = normalizeNewlines(source);
	log("parse start", { length: normalizedSource.length });
	const state = createParserState(normalizedSource);
	const documentNode = {
		type: "document",
		title: "",
		blocks: [],
		sourceText: normalizedSource,
		sourceLines: state.lines
	};

	const firstLine = state.lines[0] || "";
	const title = parseDocumentTitle(firstLine);
	if (title !== null) {
		documentNode.title = title;
		state.index = 1;
		if (isEmptyLine(state.lines[state.index])) state.index += 1;
	}

	while (state.index < state.lines.length) {
		step(state, "document");

		if (isEmptyLine(state.lines[state.index])) {
			state.index += 1;
			continue;
		}

		const before = state.index;
		documentNode.blocks.push(parseBlockSkeleton(state));

		if (state.index <= before) {
			throw new Error("Parser did not advance at line " + (state.index + 1));
		}
	}

	log("parse done", { blocks: documentNode.blocks.length, steps: state.steps });
	return documentNode;
}

function parseDocumentTitle(line) {
	let text = String(line || "");
	if (text.charCodeAt(0) === 0xFEFF) text = text.slice(1);
	if (!text.startsWith("[§") || !text.endsWith("§]")) return null;
	return decodeRestrictedContent(text.slice(2, -2));
}

function parseHeading(line) {
	const text = trimText(line);
	if (!text.startsWith("[=") || !text.endsWith("=]")) return null;
	return decodeRestrictedContent(text.slice(2, -2));
}

function decodeRestrictedContent(content) {
	let result = "";
	for (let index = 0; index < content.length;) {
		if (content.startsWith("[[", index)) {
			result += "[";
			index += 2;
			continue;
		}
		if (content.startsWith("]]", index)) {
			result += "]";
			index += 2;
			continue;
		}
		if (content[index] === "[" || content[index] === "]") return null;
		result += content[index];
		index += 1;
	}
	return trimText(result);
}

function parseBlockSkeleton(state) {
	step(state, "block skeleton");
	const startLine = state.index;
	const line = state.lines[state.index];
	const trimmed = trimText(line);
	const heading = parseHeading(line);
	let block = null;

	if (heading !== null) {
		state.index += 1;
		if (isEmptyLine(state.lines[state.index])) state.index += 1;
		block = { type: "heading", text: heading, inline: parseInline(heading) };
	} else if (trimmed === "[---]") {
		state.index += 1;
		block = { type: "rule" };
	} else if (isTableRowStart(trimmed)) {
		block = parseTable(state);
	} else if (trimmed === "[\"") {
		block = parseContentBlock(state, "[\"", "\"]", "quoteBlock");
	} else if (trimmed === "[#") {
		block = parseContentBlock(state, "[#", "#]", "hashBlock");
	} else {
		const objectExtension = parseObjectBlockOpen(line);
		if (objectExtension !== null) {
			block = parseContentBlock(state, trimmed, ".]", "objectBlock");
			if (block.type === "objectBlock") block.extension = objectExtension;
		} else if (parseListMarker(line) !== null) {
			block = parseList(state);
		} else {
			block = parseParagraphSkeleton(state);
		}
	}

	if (block && typeof block === "object") {
		block.lineStart = startLine;
		block.lineEnd = state.index;
	}

	return block;
}

function parseContentBlock(state, openToken, closeToken, type) {
	const startLine = state.index + 1;
	state.index += 1;
	const contentLines = [];

	while (state.index < state.lines.length) {
		step(state, type);
		const line = state.lines[state.index];
		const trimmed = trimText(line);

		if (isBlockCloseLine(trimmed, closeToken)) {
			state.index += 1;
			return { type, content: contentLines.join(LF), line: startLine };
		}

		const content = parseContentLine(line);
		if (content === null) {
			state.index += 1;
			return {
				type: "error",
				message: "Content line in " + openToken + " block must start with |.",
				line: state.index
			};
		}

		contentLines.push(content);
		state.index += 1;
	}

	return {
		type: "error",
		message: "Missing close token " + closeToken + ".",
		line: startLine
	};
}

function parseContentLine(line) {
	const text = String(line || "");
	let index = 0;
	while (index < text.length && isWhitespaceChar(text[index])) index += 1;
	if (text[index] !== "|") return null;
	return text.slice(index + 1);
}

function parseObjectBlockOpen(line) {
	const text = trimText(line);
	if (!text.startsWith("[.")) return null;
	if (text.endsWith("]")) return null;
	const extension = text.slice(2).trim();
	if (extension.includes("[") || extension.includes("]") || extension.includes("|") || extension.includes(" ") || extension.includes(String.fromCharCode(9))) return null;
	return extension.toLowerCase();
}

function parseTable(state) {
	const rows = [];

	while (state.index < state.lines.length) {
		step(state, "table");
		const line = state.lines[state.index];
		const trimmed = trimText(line);

		if (!isTableRowStart(trimmed)) break;

		rows.push(parseTableRow(state));
	}

	return { type: "table", rows };
}

function isTableRowStart(trimmedLine) {
	return String(trimmedLine || "").startsWith("[|");
}

function parseTableRow(state) {
	const line = state.lines[state.index];
	const trimmed = trimText(line);

	if (trimmed === "[|") {
		return parseBlockTableRow(state);
	}

	return parseLineTableRow(state);
}

function parseLineTableRow(state) {
	const sourceLine = state.index + 1;
	const trimmed = trimText(state.lines[state.index]);
	state.index += 1;

	if (!trimmed.endsWith("|]")) {
		return { type: "errorRow", message: "Missing |] in table row.", line: sourceLine };
	}

	const inner = trimmed.slice(2, -2);
	return splitTableCells(inner).map(x => parseInline(trimText(x)));
}

function splitTableCells(text) {
	const cells = [];
	let buffer = "";
	let depth = 0;

	for (let index = 0; index < text.length;) {
		if (text.startsWith("[[", index) || text.startsWith("]]", index)) {
			buffer += text.slice(index, index + 2);
			index += 2;
			continue;
		}

		if (depth === 0 && text.startsWith(" ][ ", index)) {
			cells.push(buffer);
			buffer = "";
			index += 4;
			continue;
		}

		const ch = text[index];
		buffer += ch;
		if (ch === "[") depth += 1;
		else if (ch === "]" && depth > 0) depth -= 1;
		index += 1;
	}

	cells.push(buffer);
	return cells;
}

function parseBlockTableRow(state) {
	const sourceLine = state.index + 1;
	const cells = [];
	let currentLines = [];

	state.index += 1;

	while (state.index < state.lines.length) {
		step(state, "block table row");
		const line = state.lines[state.index];
		const trimmed = trimText(line);

		if (trimmed === "][") {
			cells.push(parse(currentLines.join(LF)));
			currentLines = [];
			state.index += 1;
			continue;
		}

		if (trimmed === "|]") {
			cells.push(parse(currentLines.join(LF)));
			state.index += 1;
			return cells;
		}

		currentLines.push(line);
		state.index += 1;
	}

	return [{ type: "errorCell", message: "Missing |] in block table row.", line: sourceLine }];
}

function parseListMarker(line) {
	const t = trimText(line);
	if (!t) return null;
	const m = t[0];
	if (!"-o*#>".includes(m)) return null;
	let i = 0;
	while (i < t.length && t[i] === m) i++;
	if (i < t.length && !isWhitespaceChar(t[i])) return null;
	return { marker: m, depth: i, text: trimText(t.slice(i)) };
}

function parseList(state) {
	const items = [];
	while (state.index < state.lines.length) {
		step(state, "list");
		const info = parseListMarker(state.lines[state.index]);
		if (!info) break;

		state.index++;
		const lines = [parseInline(info.text)];

		while (state.index < state.lines.length) {
			step(state, "list continuation");
			const line = state.lines[state.index];
			if (isEmptyLine(line)) break;
			if (parseListMarker(line) !== null) break;
			if (isSkeletonBlockStart(line)) break;

			lines.push(parseInline(trimText(line)));
			state.index++;
		}

		items.push({
			type: "listItem",
			marker: info.marker,
			depth: info.depth,
			lines
		});
	}
	return { type: "list", items };
}

function parseParagraphSkeleton(state) {
	const lines = [];

	while (state.index < state.lines.length) {
		step(state, "paragraph skeleton");
		const line = state.lines[state.index];

		if (isEmptyLine(line)) break;
		if (lines.length > 0 && isSkeletonBlockStart(line)) break;

		lines.push(parseInline(trimText(line)));
		state.index += 1;
	}

	return { type: "paragraph", lines };
}

function isSkeletonBlockStart(line) {
	const trimmed = trimText(line);
	return parseHeading(line) !== null
		|| trimmed === "[---]"
		|| isTableRowStart(trimmed)
		|| trimmed === "[\""
		|| trimmed === "[#"
		|| parseObjectBlockOpen(line) !== null
		|| parseListMarker(line) !== null;
}

function parseInline(text) {
	const state = {
		text: String(text || ""),
		index: 0,
		steps: 0
	};
	return parseInlineUntil(state, null).tokens;
}

function parseInlineUntil(state, closeToken) {
	const tokens = [];
	let buffer = "";

	function flush() {
		if (buffer !== "") {
			tokens.push({ type: "text", text: buffer });
			buffer = "";
		}
	}

	while (state.index < state.text.length) {
		state.steps += 1;
		if (state.steps > MAX_PARSE_STEPS) throw new Error("Inline parser watchdog near offset " + state.index);

		if (closeToken !== null && isRealClose(state.text, state.index, closeToken)) {
			flush();
			state.index += closeToken.length;
			return { tokens, closed: true };
		}

		if (closeToken !== null) {
			const escapedClose = readEscapedCloseLiteral(state.text, state.index, closeToken);
			if (escapedClose !== null) {
				buffer += escapedClose.text;
				state.index = escapedClose.next;
				continue;
			}
		}

		if (state.text.startsWith("[[", state.index)) {
			buffer += "[";
			state.index += 2;
			continue;
		}

		if (state.text.startsWith("]]", state.index)) {
			buffer += "]";
			state.index += 2;
			continue;
		}

		if (state.text.startsWith("[---]", state.index)) {
			flush();
			tokens.push({ type: "rule" });
			state.index += 5;
			continue;
		}

		if (state.text[state.index] === "[" && state.index + 1 < state.text.length) {
			const parsed = parseInlineItem(state);
			if (parsed !== null) {
				flush();
				tokens.push(parsed);
				continue;
			}
		}

		if (state.text[state.index] === "[" || state.text[state.index] === "]") {
			flush();
			tokens.push({ type: "syntaxError", text: state.text[state.index] });
			state.index += 1;
			continue;
		}

		buffer += state.text[state.index];
		state.index += 1;
	}

	flush();
	return { tokens, closed: closeToken === null };
}

function isBlockCloseLine(text, closeToken) {
	if (!isRealClose(text, 0, closeToken)) return false;
	let index = closeToken.length;
	while (index < text.length) {
		if (!text.startsWith("]]", index)) return false;
		index += 2;
	}
	return true;
}

function isRealClose(text, index, closeToken) {
	if (!text.startsWith(closeToken, index)) return false;
	if (!closeToken.endsWith("]")) return true;

	let after = index + closeToken.length;
	let closingBracketCount = 0;
	while (after < text.length && text[after] === "]") {
		closingBracketCount += 1;
		after += 1;
	}

	return closingBracketCount % 2 === 0;
}

function readEscapedCloseLiteral(text, index, closeToken) {
	if (!closeToken || !closeToken.endsWith("]")) return null;
	if (!text.startsWith(closeToken, index)) return null;
	if (isRealClose(text, index, closeToken)) return null;
	if (text[index + closeToken.length] !== "]") return null;
	return {
		text: closeToken,
		next: index + closeToken.length + 1
	};
}

function parseInlineItem(state) {
	const marker = state.text[state.index + 1];
	const style = getStyleMarker(marker);
	if (style !== null) {
		const start = state.index;
		state.index += 2;
		const inner = parseInlineUntil(state, style.close);
		if (inner.closed) return { type: "styled", marker, children: inner.tokens };
		state.index = start;
		return null;
	}

	if (state.text.startsWith("[\"", state.index)) return parseInlineContentToken(state, "[\"", "\"]", "quote");
	if (state.text.startsWith("[#", state.index)) return parseInlineContentToken(state, "[#", "#]", "resource");
	if (state.text.startsWith("[<", state.index)) return parseInlineLink(state);
	if (state.text.startsWith("[.", state.index)) return parseInlineEmbed(state);
	return null;
}

function getStyleMarker(marker) {
	switch (marker) {
		case "*": return { close: "*]", tag: "strong" };
		case "/": return { close: "/]", tag: "em" };
		case "_": return { close: "_]", tag: "u" };
		case "-": return { close: "-]", tag: "del" };
		case "!": return { close: "!]", tag: "span", className: "wiki-alert" };
		case "(": return { close: ")]", tag: "small" };
		case "'": return { close: "']", tag: "sup" };
		case ",": return { close: ",]", tag: "sub" };
		default: return null;
	}
}

function parseInlineContentToken(state, openToken, closeToken, type) {
	const start = state.index;
	state.index += openToken.length;
	const content = readContentText(state, closeToken);
	if (content === null) {
		state.index = start;
		return null;
	}
	if (type === "quote") return { type, text: content };
	return { type, target: trimText(content) };
}

function parseInlineLink(state) {
	const start = state.index;
	state.index += 2;
	const content = readContentText(state, ">]");
	if (content === null) {
		state.index = start;
		return null;
	}
	const pipe = findFirstPipe(content);
	return {
		type: "link",
		target: trimText(pipe < 0 ? content : content.slice(0, pipe)),
		label: pipe < 0 ? null : content.slice(pipe + 1)
	};
}

function parseInlineEmbed(state) {
	const start = state.index;
	state.index += 2;
	const content = readOpaqueContentText(state, ".]");
	if (content === null) {
		state.index = start;
		return null;
	}
	const pipe = findFirstPipe(content);
	if (pipe < 0) {
		state.index = start;
		return null;
	}
	return {
		type: "embed",
		extension: trimText(content.slice(0, pipe)).toLowerCase(),
		value: content.slice(pipe + 1)
	};
}

function readOpaqueContentText(state, closeToken) {
	let content = "";
	while (state.index < state.text.length) {
		state.steps += 1;
		if (state.steps > MAX_PARSE_STEPS) throw new Error("Inline content watchdog near offset " + state.index);

		if (state.text.startsWith("[[", state.index)) {
			content += "[";
			state.index += 2;
			continue;
		}

		if (state.text.startsWith("]]", state.index)) {
			content += "]";
			state.index += 2;
			continue;
		}

		if (isRealClose(state.text, state.index, closeToken)) {
			state.index += closeToken.length;
			return content;
		}

		const escapedClose = readEscapedCloseLiteral(state.text, state.index, closeToken);
		if (escapedClose !== null) {
			content += escapedClose.text;
			state.index = escapedClose.next;
			continue;
		}

		content += state.text[state.index];
		state.index += 1;
	}

	return null;
}

function readContentText(state, closeToken) {
	let content = "";
	while (state.index < state.text.length) {
		state.steps += 1;
		if (state.steps > MAX_PARSE_STEPS) throw new Error("Inline content watchdog near offset " + state.index);

		if (state.text.startsWith("[[", state.index)) {
			content += "[";
			state.index += 2;
			continue;
		}

		if (state.text.startsWith("]]", state.index)) {
			content += "]";
			state.index += 2;
			continue;
		}

		if (isRealClose(state.text, state.index, closeToken)) {
			state.index += closeToken.length;
			return content;
		}

		const escapedClose = readEscapedCloseLiteral(state.text, state.index, closeToken);
		if (escapedClose !== null) {
			content += escapedClose.text;
			state.index = escapedClose.next;
			continue;
		}

		if (state.text[state.index] === "[" || state.text[state.index] === "]") return null;
		content += state.text[state.index];
		state.index += 1;
	}
	return null;
}

function findFirstPipe(text) {
	for (let index = 0; index < text.length; index += 1) {
		if (text[index] === "|") return index;
	}
	return -1;
}

function el(tagName, options = {}, ...children) {
	const element = document.createElement(tagName);
	if (options.className) element.className = options.className;
	if (options.text !== undefined) element.textContent = options.text;
	if (options.attrs) {
		for (const [key, value] of Object.entries(options.attrs)) {
			element.setAttribute(key, String(value));
		}
	}
	for (const child of children) if (child) element.append(child);
	return element;
}

export function render(source, context) {
	ensureWikiStyles();
	log("render entry", {
		sourceType: typeof source,
		sourceLength: typeof source === "string" ? source.length : null
	});

	if (context && typeof context.prepareArticle === "function") {
		log("prepareArticle call");
		context.prepareArticle("wiki-article");
	}

	const target = context && context.articleElement;
	if (!target) return;

	try {
		const documentNode = typeof source === "string" ? parse(source) : source;
		target.replaceChildren(renderDocumentSkeleton(documentNode, context, typeof source === "string" ? source : ""));
		log("render exit");
	} catch (error) {
		console.error("[viewer] render failed", error);
		target.replaceChildren(el("pre", {
			className: "error",
			text: error && error.stack ? error.stack : String(error)
		}));
	}
}

function ensureWikiStyles() {
	if (document.getElementById("wiki-viewer-style")) return;

	const style = document.createElement("style");
	style.id = "wiki-viewer-style";
	style.textContent = `
		.wiki-article {
			font-family: "Palatino Linotype", "Book Antiqua", Palatino, serif;
			color: var(--ink);
			line-height: 1.35;
		}

		.wiki-article h1 {
			margin: 0 0 1.25rem;
			padding: 1rem 1.15rem;
			border: 1px solid var(--card-border);
			border-radius: 0.95rem;
			background: linear-gradient(180deg, var(--card-bg-start), var(--card-bg-end));
			box-shadow: var(--card-shadow);
			text-decoration: underline;
			text-decoration-color: var(--line);
			text-underline-offset: 0.16em;
		}

		.wiki-article h2 {
			margin: 1.55rem 0 0.8rem;
			padding: 0.5rem 0.75rem 0.55rem 1rem;
			border: 1px solid var(--card-border);
			border-radius: 0.8rem;
			background: linear-gradient(180deg, var(--card-bg-start), var(--card-bg-end));
			text-decoration: underline;
			text-decoration-color: var(--line);
			text-underline-offset: 0.16em;
		}

		.wiki-toc {
			margin: 0 0 1.35rem;
			border: 1px solid var(--card-border);
			border-radius: 0.95rem;
			background: linear-gradient(180deg, var(--card-bg-start), var(--card-bg-end));
			box-shadow: var(--card-shadow);
			overflow: hidden;
		}

		.wiki-toc::before {
			content: "Contents";
			display: block;
			padding: 0.8rem 1.05rem;
			border-bottom: 1px solid var(--bar-border);
			background: linear-gradient(180deg, var(--bar-bg-start), var(--bar-bg-end));
			font-size: 0.85rem;
			font-weight: 700;
			letter-spacing: 0.08em;
			text-transform: uppercase;
		}

		.wiki-toc ol {
			display: grid;
			grid-auto-flow: column;
			grid-auto-columns: 20rem;
			column-gap: 2rem;
			row-gap: 0.5rem;
			list-style: none;
			margin: 0;
			overflow-x: auto;
			padding: 0.95rem 1.1rem 1rem;
		}

		.wiki-toc li {
			min-width: 0;
			margin: 0;
		}

		.wiki-article a {
			color: var(--accent);
			text-decoration-thickness: 0.08em;
			text-underline-offset: 0.16em;
		}

		.wiki-paragraph {
			margin: 0 0 0.75rem;
		}

		.wiki-list {
			margin: 0 0 1rem;
		}

		.wiki-list-item {
			display: flex;
			align-items: flex-start;
			gap: 0.8rem;
			margin: 0 0 0.38rem;
		}

		.wiki-list-marker {
			position: relative;
			flex: 0 0 1rem;
			width: 1rem;
			height: 1.5rem;
			color: var(--accent-strong);
		}

		.wiki-list-marker::before {
			content: "";
			position: absolute;
			left: 50%;
			box-sizing: border-box;
		}

		.wiki-list-item.is-dash .wiki-list-marker::before {
			top: 0.64rem;
			width: 0.84rem;
			height: 0.18rem;
			transform: translateX(-50%);
			border-radius: 999px;
			background: currentColor;
		}

		.wiki-list-item.is-circle .wiki-list-marker::before {
			top: 0.34rem;
			width: 0.72rem;
			height: 0.72rem;
			transform: translateX(-50%);
			border: 0.15rem solid currentColor;
			border-radius: 50%;
			background: transparent;
		}

		.wiki-list-item.is-dot .wiki-list-marker::before {
			top: 0.42rem;
			width: 0.68rem;
			height: 0.68rem;
			transform: translateX(-50%);
			border-radius: 50%;
			background: currentColor;
		}

		.wiki-list-item.is-square .wiki-list-marker::before {
			top: 0.4rem;
			width: 0.64rem;
			height: 0.64rem;
			transform: translateX(-50%);
			border: 0.15rem solid currentColor;
			background: transparent;
		}

		.wiki-list-item.is-arrow .wiki-list-marker::before {
			top: 0.44rem;
			width: 0.48rem;
			height: 0.48rem;
			transform: translateX(-50%) rotate(45deg);
			border-top: 0.14rem solid currentColor;
			border-right: 0.14rem solid currentColor;
		}

		.wiki-list-body {
			flex: 1 1 auto;
			min-width: 0;
		}

		.wiki-quote {
			margin: 0 0 0.9rem;
			padding: 0.45rem 0.9rem;
			border-left: 4px solid var(--quote-border);
			border-radius: 0.45rem;
			background: var(--quote-bg);
			color: var(--quote-text);
			font-style: italic;
		}

		.wiki-article table {
			width: max-content;
			max-width: 100%;
			border-collapse: collapse;
			margin: 0 0 1rem;
			border: 1px solid var(--card-border);
			background: var(--table-bg);
		}

		.wiki-article td {
			padding: 0.5rem 0.65rem;
			border: 1px solid var(--bar-border);
			vertical-align: top;
		}

		.wiki-article tr:nth-child(odd) > td {
			background: var(--table-row-odd);
		}

		.wiki-article tr:nth-child(even) > td {
			background: var(--table-row-even);
		}

		.wiki-article td > *:first-child { margin-top: 0; }
		.wiki-article td > *:last-child { margin-bottom: 0; }
		.wiki-article td table { margin: 0.35rem 0; }

		.wiki-object {
			display: inline-block;
			padding: 0.1rem 0.45rem;
			border: 1px solid var(--button-border);
			border-radius: 0.45rem;
			background: var(--object-bg);
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
			font-size: 0.92em;
			white-space: pre-wrap;
		}

		pre.wiki-object {
			display: block;
			margin: 0 0 1rem;
			padding: 0.75rem 0.9rem;
			border-radius: 0.45rem;
			overflow-x: auto;
		}

		.wiki-article del {
			text-decoration: line-through;
			text-decoration-thickness: 0.08em;
			text-decoration-color: currentColor;
		}

		.wiki-alert {
			color: var(--alert);
			font-weight: 700;
		}

		.wiki-alert a {
			color: inherit;
		}

		.wiki-embedded-block,
		.wiki-resource-block {
			margin: 0 0 1rem;
		}

		.wiki-embedded-inline,
		.wiki-resource-inline {
			display: inline-block;
			max-width: 100%;
			vertical-align: baseline;
		}

		.wiki-syntax-error {
			display: inline-block;
			padding: 0 0.08rem;
			border: 1px solid var(--syntax-border);
			border-radius: 0.18rem;
			background: var(--syntax-bg);
			color: var(--syntax-text);
			line-height: 1.05;
		}

		.wiki-edit-bar {
			display: flex;
			justify-content: flex-end;
			margin: 0 0 0.85rem;
		}

		.wiki-edit-bar button,
		.wiki-edit-actions button {
			border: 1px solid var(--button-border);
			border-radius: 999px;
			padding: 0.42rem 0.75rem;
			background: linear-gradient(180deg, var(--button-bg-start), var(--button-bg-end));
			color: var(--button-text);
			font: inherit;
			font-weight: 700;
			cursor: pointer;
		}

		.wiki-edit-panel:not(.wiki-edit-inline) {
			box-sizing: border-box;
			position: fixed !important;
			top: 0 !important;
			right: 0 !important;
			bottom: 0 !important;
			left: auto !important;
			height: 100vh !important;
			height: 100dvh !important;
			width: min(42rem, 42vw);
			z-index: 10000;
			display: flex;
			flex-direction: column;
			margin: 0;
			padding: 0.85rem;
			border: 1px solid var(--card-border);
			border-radius: 0.95rem 0 0 0.95rem;
			background: linear-gradient(180deg, var(--card-bg-start), var(--card-bg-end));
			box-shadow: var(--panel-shadow);
		}

		.wiki-edit-panel.wiki-edit-inline {
			box-sizing: border-box;
			position: static !important;
			top: auto !important;
			right: auto !important;
			bottom: auto !important;
			left: auto !important;
			height: auto !important;
			border-radius: 0.95rem;
			width: auto !important;
			max-width: none !important;
			min-height: 100%;
			max-height: none;
			z-index: auto;
			margin: 0;
			box-shadow: var(--card-shadow);
			overflow: visible;
		}

		.wiki-section-split {
			display: grid;
			grid-template-columns: minmax(0, 1fr);
			gap: 1rem;
			align-items: stretch;
		}

		.wiki-section-split.has-editor {
			grid-template-columns: minmax(0, 1fr) 0.35rem minmax(18rem, var(--wiki-editor-width, 42%));
		}

		.wiki-section-body {
			min-width: 0;
		}

		.wiki-section-splitter {
			display: none;
			width: 0.35rem;
			border-radius: 999px;
			background: var(--splitter-bg);
			cursor: col-resize;
		}

		.wiki-section-split.has-editor .wiki-section-splitter {
			display: block;
		}

		.wiki-section-splitter:hover,
		.wiki-section-splitter.is-dragging {
			background: var(--splitter-active);
		}

		.wiki-section-split.has-editor .wiki-edit-textarea {
			display: block;
			width: 100%;
			min-height: 0;
			flex: 1 1 auto;
			padding: 0.75rem;
			border: 1px solid var(--input-border);
			border-radius: 0.6rem;
			background: var(--input-bg);
			color: var(--input-text);
			font-family: Consolas, "SFMono-Regular", "Courier New", monospace;
			font-size: 0.95rem;
			line-height: 1.35;
			white-space: pre;
			overflow-x: auto;
			overflow-y: hidden;
			resize: none;
		}

		.wiki-edit-actions {
			display: flex;
			gap: 0.6rem;
			margin-top: 0.7rem;
			flex: 0 0 auto;
		}

		.wiki-heading-bar .wiki-edit-actions,
		.wiki-title-bar .wiki-edit-actions {
			display: inline-flex;
			margin-top: 0;
			margin-left: 0.5rem;
		}

		.wiki-edit-status {
			margin: 0.7rem 0 0;
			white-space: pre-wrap;
			color: var(--syntax-text);
		}

		body.wiki-editor-open .wiki-article {
			margin-right: 0;
		}

		.wiki-title-bar,
		.wiki-heading-bar {
			display: flex;
			align-items: center;
			justify-content: space-between;
			gap: 0.5rem;
		}

		.wiki-title-bar h1,
		.wiki-heading-bar h2 {
			margin: 0;
			flex: 1 1 auto;
		}

		.wiki-title-bar button,
		.wiki-heading-bar button {
			margin-left: 0.5rem;
			font-size: 0.85rem;
			padding: 0.3rem 0.6rem;
		}

	`;
	document.head.append(style);
}

function renderDocumentSkeleton(documentNode, context, sourceText) {
	const fragment = document.createDocumentFragment();

	if (documentNode.title) {
		const header = el("div", { className: "wiki-title-bar" });
		const h1 = el("h1", { text: documentNode.title });
		header.append(h1);
		if (canEditWiki(context)) {
			const btn = el("button", { text: "Edit", attrs: { type: "button" } });
			btn.addEventListener("click", () => openWikiEditor(header, context, sourceText));
			header.append(btn);
		}
		fragment.append(header);
	}

	const blocks = documentNode.blocks || [];
	const headings = prepareHeadingAnchors(blocks);
	let tocInserted = false;
	let index = 0;

	while (index < blocks.length) {
		const block = blocks[index];

		if (block.type !== "heading") {
			fragment.append(renderBlockSkeleton(block, context, documentNode));
			index += 1;
			continue;
		}

		if (!tocInserted && headings.length > 0) {
			fragment.append(renderTableOfContents(headings));
			tocInserted = true;
		}

		const section = el("section", { className: "wiki-section" });
		section.append(renderBlockSkeleton(block, context, documentNode));

		const sectionSplit = el("div", { className: "wiki-section-split" });
		const sectionBody = el("div", { className: "wiki-section-body" });
		const splitter = el("div", { className: "wiki-section-splitter", attrs: { role: "separator", "aria-orientation": "vertical", title: "Resize editor" } });
		attachSectionSplitter(splitter, sectionSplit);
		index += 1;
		while (index < blocks.length && blocks[index].type !== "heading") {
			sectionBody.append(renderBlockSkeleton(blocks[index], context, documentNode));
			index += 1;
		}
		sectionSplit.append(sectionBody, splitter);
		section.append(sectionSplit);
		fragment.append(section);
	}

	return fragment;
}

function canEditWiki(context) {
	return Boolean(context && typeof context.updateWikiSource === "function");
}

function autoGrowTextarea(textarea) {
	textarea.style.height = "auto";
	textarea.style.height = textarea.scrollHeight + "px";
}

function attachSectionSplitter(splitter, split) {
	splitter.addEventListener("pointerdown", (event) => {
		event.preventDefault();
		splitter.setPointerCapture(event.pointerId);
		splitter.classList.add("is-dragging");
		const rect = split.getBoundingClientRect();

		function move(moveEvent) {
			const rightWidth = rect.right - moveEvent.clientX;
			const min = 280;
			const max = Math.max(min, rect.width - 280);
			const clamped = Math.max(min, Math.min(max, rightWidth));
			split.style.setProperty("--wiki-editor-width", clamped + "px");
		}

		function up(upEvent) {
			splitter.classList.remove("is-dragging");
			splitter.releasePointerCapture(upEvent.pointerId);
			window.removeEventListener("pointermove", move);
			window.removeEventListener("pointerup", up);
		}

		window.addEventListener("pointermove", move);
		window.addEventListener("pointerup", up);
	});
}

function openWikiEditor(anchor, context, sourceText, transformSource) {
	const existing = document.getElementById("wiki-edit-panel");
	if (existing) {
		const parent = existing.parentElement;
		existing.remove();
		if (parent && parent.classList) parent.classList.remove("has-editor");
	}
	document.body.classList.remove("wiki-editor-open");

	const isSection = Boolean(transformSource);

	const panel = el("section", {
		className: isSection ? "wiki-edit-panel wiki-edit-inline" : "wiki-edit-panel",
		attrs: { id: "wiki-edit-panel" }
	});

	const textarea = el("textarea", { className: "wiki-edit-textarea" });
	textarea.value = normalizeNewlines(sourceText || "");
	textarea.addEventListener("input", () => autoGrowTextarea(textarea));

	const status = el("pre", { className: "wiki-edit-status" });
	const apply = el("button", { text: "Apply", attrs: { type: "button" } });
	const cancel = el("button", { text: "Cancel", attrs: { type: "button" } });
	const actions = el(isSection ? "span" : "div", { className: "wiki-edit-actions" }, apply, cancel);
	const editButton = anchor.querySelector ? anchor.querySelector("button") : null;
	if (isSection) {
		if (editButton) editButton.hidden = true;
		anchor.append(actions);
	}

	apply.addEventListener("click", async () => {
		apply.disabled = true;
		cancel.disabled = true;
		textarea.disabled = true;
		status.textContent = "";
		try {
			const nextSource = normalizeNewlines(textarea.value);
			if (!isSection) document.body.classList.remove("wiki-editor-open");
			const parent = panel.parentElement;
			if (parent && parent.classList) parent.classList.remove("has-editor");
			if (isSection) {
				actions.remove();
				if (editButton) editButton.hidden = false;
			}
			await context.updateWikiSource(transformSource ? transformSource(nextSource) : nextSource);
		} catch (error) {
			apply.disabled = false;
			cancel.disabled = false;
			textarea.disabled = false;
			status.textContent = error && error.stack ? error.stack : String(error);
		}
	});

	cancel.addEventListener("click", () => {
		if (!isSection) document.body.classList.remove("wiki-editor-open");
		const parent = panel.parentElement;
		panel.remove();
		if (parent && parent.classList) parent.classList.remove("has-editor");
		if (isSection) {
			actions.remove();
			if (editButton) editButton.hidden = false;
		}
	});

	if (isSection) panel.append(textarea, status);
	else panel.append(textarea, status, actions);

	if (isSection) {
		const section = anchor.closest ? anchor.closest(".wiki-section") : null;
		const split = section ? section.querySelector(".wiki-section-split") : null;
		if (split) {
			split.append(panel);
			split.classList.add("has-editor");
		} else {
			anchor.insertAdjacentElement("afterend", panel);
		}
	} else {
		document.body.append(panel);
		document.body.classList.add("wiki-editor-open");
	}

	autoGrowTextarea(textarea);
	textarea.focus();
	textarea.setSelectionRange(0, 0);
	textarea.scrollTop = 0;
	textarea.scrollLeft = 0;
}

function findNextHeadingLine(blocks, startIndex, fallbackLine) {
	for (let index = startIndex + 1; index < blocks.length; index += 1) {
		if (blocks[index].type === "heading") return blocks[index].lineStart;
	}
	return fallbackLine;
}

function getSectionSource(documentNode, headingBlock) {
	const blocks = documentNode.blocks || [];
	const blockIndex = blocks.indexOf(headingBlock);
	const start = headingBlock.lineStart;
	const end = findNextHeadingLine(blocks, blockIndex, documentNode.sourceLines.length);
	return documentNode.sourceLines.slice(start, end).join(LF);
}

function replaceSectionSource(documentNode, headingBlock, nextSectionSource) {
	const blocks = documentNode.blocks || [];
	const blockIndex = blocks.indexOf(headingBlock);
	const start = headingBlock.lineStart;
	const end = findNextHeadingLine(blocks, blockIndex, documentNode.sourceLines.length);
	const before = documentNode.sourceLines.slice(0, start);
	const after = documentNode.sourceLines.slice(end);
	const replacement = splitLines(nextSectionSource);
	return before.concat(replacement).concat(after).join(LF);
}

function openSectionEditor(anchor, context, documentNode, headingBlock) {
	openWikiEditor(
		anchor,
		context,
		getSectionSource(documentNode, headingBlock),
		(nextSource) => replaceSectionSource(documentNode, headingBlock, nextSource)
	);
}

function prepareHeadingAnchors(blocks) {
	const headings = [];
	const used = new Map();

	for (const block of blocks || []) {
		if (block.type !== "heading") continue;
		const base = createAnchorBase(block.text || "chapter");
		const count = used.get(base) || 0;
		used.set(base, count + 1);
		block.anchorId = count === 0 ? base : base + "-" + (count + 1);
		headings.push(block);
	}

	return headings;
}

function createAnchorBase(text) {
	const raw = String(text || "")
		.toLowerCase()
		.normalize("NFD")
		.replace(/[̀-ͯ]/g, "");
	let result = "";
	let lastWasDash = false;

	for (const ch of raw) {
		const code = ch.charCodeAt(0);
		const isAsciiLetter = code >= 97 && code <= 122;
		const isDigit = code >= 48 && code <= 57;
		if (isAsciiLetter || isDigit) {
			result += ch;
			lastWasDash = false;
			continue;
		}

		if (!lastWasDash) {
			result += "-";
			lastWasDash = true;
		}
	}

	result = result.replace(/^-+|-+$/g, "");
	return result || "chapter";
}

function renderTableOfContents(headings) {
	const nav = el("nav", { className: "wiki-toc" });
	const list = el("ol");
	const itemCount = headings.length;

	for (const heading of headings) {
		const link = el("a", { attrs: { href: "#" + heading.anchorId } });
		appendInline(link, heading.inline || parseInline(heading.text), null);
		list.append(el("li", {}, link));
	}

	{
		const applyLayout = () => {
			if (itemCount < 1) return;

			let maxItemsPerCol = Math.ceil(Math.sqrt(itemCount));
			const colCount = Math.ceil(itemCount / maxItemsPerCol);
			const listStyle = getComputedStyle(list);
			const columnWidth = parseFloat(listStyle.gridAutoColumns) || list.clientWidth;
			const columnGap = parseFloat(listStyle.columnGap) || 0;
			const contentWidth = list.clientWidth - (parseFloat(listStyle.paddingLeft) || 0) - (parseFloat(listStyle.paddingRight) || 0);
			const colCountMax = Math.max(1, Math.floor((contentWidth + columnGap) / (columnWidth + columnGap)));

			if (colCount > colCountMax) {
				maxItemsPerCol = Math.ceil(itemCount / colCountMax);
			}

			list.style.gridTemplateRows = "repeat(" + maxItemsPerCol + ", auto)";
		};

		requestAnimationFrame(() => {
			applyLayout();
			if (typeof ResizeObserver !== "function") return;

			const observer = new ResizeObserver(() => {
				if (!nav.isConnected) {
					observer.disconnect();
					return;
				}
				applyLayout();
			});
			observer.observe(list);
		});
	}

	nav.append(list);
	return nav;
}

function renderBlockSkeleton(block, context, documentNode) {
	switch (block.type) {
		case "table": {
			const table = el("table");
			for (const row of block.rows) {
				const tr = el("tr");
				for (const cell of row) {
					const td = el("td");

					if (cell && typeof cell === "object" && cell.type === "errorCell") {
						td.append(el("pre", { className: "error", text: "Line " + cell.line + ": " + cell.message }));
					} else if (cell && typeof cell === "object" && cell.type === "document") {
						for (const childBlock of cell.blocks || []) {
							td.append(renderBlockSkeleton(childBlock, context, cell));
						}
					} else {
						appendInline(td, Array.isArray(cell) ? cell : parseInline(String(cell || "")), context);
					}

					tr.append(td);
				}
				table.append(tr);
			}
			return table;
		}
		case "list":
			return renderList(block, context);
		case "quoteBlock":
			return el("blockquote", { className: "wiki-quote", text: block.content });
		case "hashBlock":
			return renderResourceReference(block.content, context, false);
		case "objectBlock":
			return renderEmbeddedSource(block.extension || "txt", block.content, context, false);
		case "error":
			return el("pre", { className: "error", text: "Line " + block.line + ": " + block.message });
		case "heading": {
			return renderHeading(block, context, documentNode);
		}
		case "rule":
			return el("hr");
		case "paragraph":
			return renderParagraph(block, context);
		default:
			return el("pre", { className: "error", text: "Unknown block: " + JSON.stringify(block) });
	}
}

function renderHeading(block, context, documentNode) {
	const attrs = block.anchorId ? { id: block.anchorId } : null;
	const wrapper = el("div", { className: "wiki-heading-bar" });
	const heading = renderInlineContainer("h2", { attrs }, block.inline || parseInline(block.text), context);
	wrapper.append(heading);

	if (canEditWiki(context) && documentNode && documentNode.sourceLines) {
		const button = el("button", { text: "Edit", attrs: { type: "button" } });
		button.addEventListener("click", () => openSectionEditor(wrapper, context, documentNode, block));
		wrapper.append(button);
	}

	return wrapper;
}

function renderList(block, context) {
	const root = el("div", { className: "wiki-list", attrs: { role: "list" } });
	for (const it of block.items) {
		const item = el("div", {
			className: "wiki-list-item " + getListMarkerClassName(it.marker),
			attrs: { role: "listitem" }
		});
		item.style.marginInlineStart = (it.depth - 1) * 1.5 + "rem";

		const marker = el("span", {
			className: "wiki-list-marker",
			attrs: { "aria-hidden": "true" }
		});
		const body = el("div", { className: "wiki-list-body" });

		const lines = it.lines || [it.tokens || []];
		for (let index = 0; index < lines.length; index++) {
			if (index > 0) body.append(document.createElement("br"));
			appendInline(body, lines[index], context);
		}

		item.append(marker, body);
		root.append(item);
	}
	return root;
}

function getListMarkerClassName(marker) {
	switch (marker) {
		case "-": return "is-dash";
		case "o": return "is-circle";
		case "*": return "is-dot";
		case "#": return "is-square";
		case ">": return "is-arrow";
		default: return "is-dash";
	}
}

function renderParagraph(block, context) {
	const paragraph = el("p", { className: "wiki-paragraph" });
	for (let index = 0; index < block.lines.length; index += 1) {
		if (index > 0) paragraph.append(document.createTextNode(" "));
		appendInline(paragraph, Array.isArray(block.lines[index]) ? block.lines[index] : parseInline(String(block.lines[index] || "")), context);
	}
	return paragraph;
}

function renderInlineContainer(tagName, options, tokens, context) {
	const node = el(tagName, options || {});
	appendInline(node, tokens || [], context);
	return node;
}

function appendInline(parent, tokens, context) {
	for (const token of tokens || []) parent.append(renderInline(token, context));
}

function renderInline(token, context) {
	switch (token.type) {
		case "text": return document.createTextNode(token.text);
		case "syntaxError": return el("span", { className: "wiki-syntax-error", text: token.text });
		case "rule": return el("span", { text: "---" });
		case "quote": return el("q", { text: token.text });
		case "resource": return renderResourceReference(token.target, context, true);
		case "embed": return renderEmbeddedSource(token.extension || "txt", token.value, context, true);
		case "link": return renderLink(token, context);
		case "styled": return renderStyledInline(token, context);
		default: return document.createTextNode("");
	}
}

function renderStyledInline(token, context) {
	const style = getStyleMarker(token.marker);
	const node = el(style ? style.tag : "span", { className: style && style.className ? style.className : "" });
	appendInline(node, token.children || [], context);
	return node;
}

function renderEmbeddedSource(extension, source, context, inline) {
	const container = el(inline ? "span" : "div", { className: inline ? "wiki-embedded-inline" : "wiki-embedded-block" });
	const ext = extension || "txt";

	if (!context || typeof context.renderEmbeddedSource !== "function") {
		container.append(el("code", { className: "wiki-object", text: String(source || "") }));
		return container;
	}

	container.textContent = "";
	void context.renderEmbeddedSource(ext, String(source || ""), container, "embedded." + ext).catch((error) => {
		container.replaceChildren(el("pre", {
			className: "error",
			text: error && error.stack ? error.stack : String(error)
		}));
	});

	return container;
}

function renderResourceReference(target, context, inline) {
	const container = el(inline ? "span" : "div", { className: inline ? "wiki-resource-inline" : "wiki-resource-block" });
	const rawTarget = trimText(target);
	if (!rawTarget) return container;

	if (!context || typeof context.renderEmbeddedSource !== "function") {
		container.append(el("code", { className: "wiki-object", text: rawTarget }));
		return container;
	}

	container.textContent = "Loading " + rawTarget + " ...";

	void (async () => {
		try {
			const resolved = context && typeof context.resolveRelativePath === "function"
				? context.resolveRelativePath(context.filePath || "", rawTarget)
				: rawTarget;
			const ext = getFileExtension(resolved) || "txt";
			if (typeof context.renderResourceReference === "function") {
				await context.renderResourceReference(ext, resolved, container);
				return;
			}

			const response = await fetch(resolved, { cache: "no-store" });
			if (!response.ok) throw new Error(resolved + " HTTP " + response.status);
			const source = await response.text();
			await context.renderEmbeddedSource(ext, source, container, resolved);
		} catch (error) {
			container.replaceChildren(el("pre", {
				className: "error",
				text: error && error.stack ? error.stack : String(error)
			}));
		}
	})();

	return container;
}

function renderLink(token, context) {
	const target = String(token.target || "");
	const href = resolveLinkHref(target, context);
	const label = token.label !== null ? token.label : defaultLinkLabel(target, context);
	const attrs = { href };
	if (isExternalLink(target)) {
		attrs.target = "_blank";
		attrs.rel = "noreferrer noopener";
	}
	return el("a", { attrs, text: label });
}

function resolveLinkHref(target, context) {
	if (isExternalLink(target)) return normalizeExternalLink(target);
	const resolved = context && typeof context.resolveRelativePath === "function"
		? context.resolveRelativePath(context.filePath || "", target)
		: target;
	const path = resolved || target;
	if (hasRenderableExtension(path, context) && context && typeof context.createNavigationUrl === "function") {
		return context.createNavigationUrl(path);
	}
	return path;
}

function defaultLinkLabel(target, context) {
	const cleanTarget = String(target || "").split("?")[0].split("#")[0];
	const extension = getFileExtension(cleanTarget);
	if (extension === "wiki") return fileNameWithoutExtension(cleanTarget).replace(/_/g, " ");
	return target;
}

function hasRenderableExtension(path, context) {
	if (context && typeof context.hasKnownRenderableExtension === "function") return context.hasKnownRenderableExtension(path);
	return getFileExtension(path) === "wiki";
}

function getFileExtension(path) {
	const fileName = String(path || "").replace(/\\/g, "/").split("/").pop() || "";
	const dot = fileName.lastIndexOf(".");
	return dot < 0 ? "" : fileName.slice(dot + 1).toLowerCase();
}

function fileNameWithoutExtension(path) {
	const fileName = String(path || "").replace(/\\/g, "/").split("/").pop() || "";
	const dot = fileName.lastIndexOf(".");
	return dot < 0 ? fileName : fileName.slice(0, dot);
}

function isExternalLink(target) {
	return /^(https?:\/\/|ftp:\/\/|mailto:)/i.test(String(target || "")) || /^www\./i.test(String(target || ""));
}

function normalizeExternalLink(target) {
	return /^www\./i.test(String(target || "")) ? "https://" + target : target;
}

export default Object.freeze({
	extensions,
	isBinary,
	renderer: { render },
	parse
});
