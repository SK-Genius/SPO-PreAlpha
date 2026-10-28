import { renderSource, appendTokens, TAB_SIZE } from "./source-view.js";

export const extensions = ["spo", "sig", "ilt"];
export const isBinary = false;

const RAINBOW_INDENT_COLORS = [
	"var(--source-indent-rainbow-1)",
	"var(--source-indent-rainbow-2)",
	"var(--source-indent-rainbow-3)"
];
const OPENING_BRACKET_BY_CLOSER = { ")": "(", "]": "[", "}": "{" };

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

export function render(source, context) {
	const language = /\.ilt$/i.test(context?.filePath || "") ? "ILT" : "SPO";
	renderSource(source, context, {
		language,
		createLines(text) {
			const lines = splitTokensIntoLines(tokenizeSpo(text, language)).map(segments => {
				const text = segments.map(segment => segment.text).join("");
				return { text, segments, leadingTabColumns: collectLeadingTabColumns(text), leadingOpenerIndices: [] };
			});
			assignBracketLinks(lines);
			return lines;
		},
		appendLine(parent, line) {
			applyRainbowIndentBackground(parent, line.leadingTabColumns);
			appendTokens(parent, line.segments);
		},
		appendInline(parent, text) {
			appendHighlightedSource(parent, text, language, { inlineStyles: true });
		}
	});
}

export function appendHighlightedSource(parent, source, language, options = null) {
	return appendTokens(parent, tokenizeSpo(String(source ?? ""), normalizeSpoLanguage(language)), options);
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

export default Object.freeze({ extensions, isBinary, render, renderer: { render }, appendHighlightedSource });
