import { renderSource } from "./source-view.js";
import { analyzeJni, JNI_BACKGROUNDS, JNI_FOREGROUNDS } from "../VSCode_Extension/src/jni.mjs";

export const extensions = ["jni"];
export const isBinary = false;

export function render(source, context) {
	if (!document.getElementById("jni-viewer-style")) {
		const style = document.createElement("style");
		style.id = "jni-viewer-style";
		style.textContent = `
		.source-line-text.jni-line-text,
		.jni-inline-code .jni-line-text { display: flex; min-height: 1lh; }
		.jni-line-text > span { flex: 0 0 auto; }
		.jni-line-text > span:last-child { flex: 1 0 auto; }
		.jni-inline-code { display: inline-block; vertical-align: baseline; background: var(--source-surface); }
		.jni-comment { color: #27591b; font-style: italic; }
		:root[data-theme="dark"] .jni-comment { color: #b2d78b; }

		`;
		document.head.append(style);
	}
	renderSource(source, context, {
		language: "JNI",
		createLines: text => analyzeJni(text).map(line => ({ ...line, indent: line.text.match(/^\|*/)[0].length })),
		appendLine: appendJniLine,
		appendInline(parent, text) {
			parent.classList.add("jni-inline-code");
			for (const line of analyzeJni(text)) {
				const row = document.createElement("span");
				appendJniLine(row, line);
				parent.append(row);
			}
		}
	});
}

function appendJniLine(parent, line) {
	parent.classList.add("jni-line-text");
	for (const segment of line.segments) {
		const span = document.createElement("span");
		if (segment.type) span.className = "jni-" + segment.type;
		const level = segment.level % JNI_BACKGROUNDS.length;
		span.style.backgroundColor = JNI_BACKGROUNDS[level];
		if (segment.type === "indent" || segment.type === "key") span.style.color = JNI_FOREGROUNDS[level];
		span.textContent = line.text.slice(segment.start, segment.end);
		parent.append(span);
	}
}

export default Object.freeze({ extensions, isBinary, renderer: { render } });
