import { renderSource, appendTokens } from "./source-view.js";

export const extensions = ["ini"];
export const isBinary = false;

export function render(source, context) {
	renderSource(source, context, {
		language: "INI",
		createLines(text) {
			let inSection = false;
			return text.split(/\r\n|\r|\n/).map(text => {
				const section = /^\s*\[[^\]]+\]\s*(?:[;#].*)?$/.test(text);
				if (section) inSection = true;
				const entry = text.match(/^(\s*[^=:\s][^=:]*?)(\s*[=:]\s*)(.*)$/);
				const segments = /^\s*[;#]/.test(text) ? [{ type: "comment", text }]
					: section ? [{ type: "keyword", text }]
					: entry ? [
						{ type: "identifier", text: entry[1] },
						{ type: "punctuation", text: entry[2] },
						{ type: "", text: entry[3] }
					] : [{ type: "", text }];
				return { text, segments, indent: section || !inSection ? 0 : 1 };
			});
		},
		appendLine(parent, line) {
			appendTokens(parent, line.segments);
		}
	});
}

export default Object.freeze({ extensions, isBinary, renderer: { render } });
