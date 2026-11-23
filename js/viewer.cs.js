import { renderSource, appendTokens } from "./source-view.js";

export const extensions = ["cs"];
export const isBinary = false;

export function render(source, context) {
	if (!context?.articleElement) return;
	if (!document.getElementById("cs-viewer-style")) {
		const style = document.createElement("style");
		style.id = "cs-viewer-style";
		style.textContent = `
			.tok-cs-type { color: var(--tok-string); font-weight: 600; }
			.tok-cs-module { color: var(--tok-string); }
			.tok-cs-argument { color: var(--tok-number); }
			.tok-cs-constant { color: var(--tok-enum); font-weight: 700; }
			.tok-cs-global { color: #9d174d; }
			.tok-cs-field { color: var(--tok-identifier); font-style: italic; }
			.tok-cs-function { color: #6d28d9; }
			.tok-cs-directive { color: var(--tok-comment); }
			:root[data-theme="dark"] .tok-cs-global { color: #f9a8d4; }
			:root[data-theme="dark"] .tok-cs-function { color: #c4b5fd; }
		`;
		document.head.append(style);
	}
	renderSource(source, context, {
		language: "C#",
		createLines(text) {
			const lines = [[]];
			for (const token of tokenize(text)) {
				for (const [index, part] of token.text.split(/\r\n|\r|\n/).entries()) {
					if (index) lines.push([]);
					if (part) lines.at(-1).push({ type: token.type, text: part });
				}
			}
			return lines.map(segments => {
				const text = segments.map(token => token.text).join("");
				return {
					text,
					segments,
					pinPreviousSibling: /^\s*[)}\]>]/.test(text) && segments.find(token => token.text.trim())?.type === "punctuation"
				};
			});
		},
		appendLine(parent, line) { appendTokens(parent, line.segments); },
		appendInline(parent, text) { appendTokens(parent, tokenize(text)); }
	});
}

function tokenize(source) {
	const keywords = new Set((
		"abstract add alias allows and as ascending async await base break by case catch checked class " +
		"const continue default delegate descending do else enum equals event explicit extension extern " +
		"false field file finally fixed for foreach from get global goto group if implicit in init interface " +
		"internal into is join let lock managed nameof namespace new not notnull null on operator or orderby " +
		"out override params partial private protected public readonly record ref remove required return " +
		"scoped sealed select set sizeof stackalloc static struct switch this throw true try typeof " +
		"unchecked unmanaged unsafe using value var virtual volatile when where while with yield"
	).split(" "));
	const types = new Set("bool byte char decimal double dynamic float int long nint nuint object sbyte short string uint ulong ushort void".split(" "));
	const atom = new RegExp([
		String.raw`\s+`,
		String.raw`(?<directive>#[^\r\n]*)`,
		String.raw`(?<number>0[xX][\da-fA-F_]+[uUlL]{0,2}|0[bB][01_]+[uUlL]{0,2}|(?:\d[\d_]*(?:\.\d[\d_]*)?|\.\d[\d_]*)(?:[eE][+-]?[\d_]+)?[fFdDmMuUlL]{0,2})`,
		String.raw`(?<identifier>@?[_\p{L}\p{Nl}][_\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}]*)`,
		String.raw`(?<punctuation>[{}()[\].,;:+*/%&|^!?~=<>-])`,
		String.raw`[\s\S]`
	].join("|"), "uy");
	const comment = /\/\/[^\r\n]*|\/\*[\s\S]*?(?:\*\/|$)/y;
	const opener = /(\$+@?|@\$?)?(['"])/y;
	const call = /\s*\(/y;
	const tokens = [];
	let index = 0;
	while (index < source.length) {
		comment.lastIndex = index;
		const note = comment.exec(source);
		if (note) {
			tokens.push({ type: "comment", text: note[0] });
			index += note[0].length;
			continue;
		}
		const end = literalEnd(index);
		if (end !== null) {
			tokens.push({ type: "string", text: source.slice(index, end) });
			index = end;
			continue;
		}
		atom.lastIndex = index;
		const match = atom.exec(source);
		const text = match[0];
		index += text.length;
		let type = match.groups.directive ? "cs-directive" : match.groups.number ? "number" : match.groups.punctuation ? "punctuation" : "";
		if (match.groups.identifier) {
			const name = text.replace(/^@/, "");
			call.lastIndex = index;
			type = text[0] !== "@" && types.has(name) ? "cs-type"
				: text[0] !== "@" && keywords.has(name) ? "keyword"
				: /^(?:t(?:[A-Z]|$)|i[A-Z])/.test(name) ? "cs-type"
				: /^(?:a(?:[A-Z\d]|$)|_\d+$)/.test(name) ? "cs-argument"
				: /^c[A-Z]/.test(name) ? "cs-constant"
				: /^m[A-Z]/.test(name) ? "cs-module"
				: /^g[A-Z]/.test(name) ? "cs-global"
				: /^_[A-Z]/.test(name) ? "cs-field"
				: call.test(source) ? "cs-function" : "identifier";
		}
		tokens.push({ type, text });
	}
	return tokens;

	// Keep interpolated literals intact, including quotes inside their expressions.
	function literalEnd(start) {
		opener.lastIndex = start;
		const opening = opener.exec(source);
		if (!opening || (opening[1] && opening[2] === "'")) return null;
		const prefix = opening[1] || "";
		const verbatim = prefix.includes("@");
		const dollars = prefix.split("$").length - 1;
		const quote = opening[2];
		let cursor = start + opening[0].length;
		let quoteCount = 1;
		if (quote === '"' && !verbatim) {
			while (source[cursor + quoteCount - 1] === '"') quoteCount++;
		}
		const raw = quoteCount >= 3;
		const delimiter = quote.repeat(raw ? quoteCount : 1);
		if (raw) cursor += quoteCount - 1;
		while (cursor < source.length) {
			if (source.startsWith(delimiter, cursor)) {
				if (verbatim && source[cursor + 1] === '"') { cursor += 2; continue; }
				const end = cursor + delimiter.length;
				return quote === '"' && source.startsWith("u8", end) ? end + 2 : end;
			}
			if (!raw && !verbatim && /[\r\n]/.test(source[cursor])) return cursor;
			if (!raw && !verbatim && source[cursor] === "\\") {
				cursor += /[\r\n]/.test(source[cursor + 1] || "") ? 1 : 2;
				continue;
			}
			if (dollars && source[cursor] === "{") {
				if (!raw && source[cursor + 1] === "{") { cursor += 2; continue; }
				let braces = 1;
				while (source[cursor + braces] === "{") braces++;
				if (!raw || braces >= dollars) {
					cursor = expressionEnd(cursor + braces, raw ? dollars : 1);
					continue;
				}
			}
			cursor++;
		}
		return source.length;
	}

	function expressionEnd(start, closingBraces) {
		let depth = 0;
		let cursor = start;
		while (cursor < source.length) {
			comment.lastIndex = cursor;
			const note = comment.exec(source);
			if (note) { cursor += note[0].length; continue; }
			const end = literalEnd(cursor);
			if (end !== null) { cursor = end; continue; }
			if (source[cursor] === "{") depth++;
			if (source[cursor] === "}") {
				if (!depth && source.startsWith("}".repeat(closingBraces), cursor)) return cursor + closingBraces;
				if (depth) depth--;
			}
			cursor++;
		}
		return source.length;
	}
}

export default Object.freeze({ extensions, isBinary, renderer: { render } });
