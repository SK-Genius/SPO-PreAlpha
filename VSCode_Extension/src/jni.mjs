export const JNI_BACKGROUNDS = Object.freeze(["rgba(255, 0, 255, 0.1)", "rgba(68, 68, 255, 0.1)", "rgba(255, 255, 0, 0.1)"]);
export const JNI_FOREGROUNDS = Object.freeze(["#ff00ff", "#4444ff", "#ffff00"]);

// Offsets use UTF-16 columns, as do VSCode ranges and JavaScript strings.
// Plain values retain their own level without interpreting their contents.
export function analyzeJni(source) {
	const lines = String(source ?? "").split(/\r\n|\r|\n/).map(text => ({ text, segments: [] }));
	const pending = [{ lines: lines.map((_, line) => ({ line, start: 0 })), level: 0 }];
	while (pending.length) {
		const scope = pending.pop();
		const staged = [];
		const children = [];
		let valid = true;
		const emit = (line, start, end, type = null) => {
			staged.push({ line, start, end, level: scope.level, type });
		};
		const emitIgnoredLine = (ref, text) => {
			const indent = text.length - text.trimStart().length;
			if (indent || !text.length) emit(ref.line, ref.start, ref.start + indent, "indent");
			if (indent < text.length) emit(ref.line, ref.start + indent, ref.start + text.length, "comment");
		};
		for (let index = 0; index < scope.lines.length;) {
			const ref = scope.lines[index++];
			const text = lines[ref.line].text.slice(ref.start);
			const header = text.trim();
			if (!header || header.startsWith("#")) {
				emitIgnoredLine(ref, text);
				continue;
			}
			const equals = text.indexOf("=");
			const multiline = equals < 0 && header.endsWith(":");
			const separator = multiline ? text.lastIndexOf(":") : equals;
			const keyStart = text.length - text.trimStart().length;
			const keyEnd = text.slice(0, separator).trimEnd().length;
			if (header.startsWith("|") || (equals < 0 && !multiline) || keyStart >= keyEnd) {
				valid = false;
				break;
			}
			if (keyStart > 0) emit(ref.line, ref.start, ref.start + keyStart, "indent");
			emit(ref.line, ref.start + keyStart, ref.start + keyEnd, "key");
			emit(ref.line, ref.start + keyEnd, ref.start + separator + 1);
			if (!multiline) {
				children.push({ lines: [{ line: ref.line, start: ref.start + separator + 1 }], level: scope.level + 1 });
				continue;
			}
			if (separator + 1 < text.length) {
				emit(ref.line, ref.start + separator + 1, ref.start + text.length);
			}
			const value = [];
			while (index < scope.lines.length) {
				const next = scope.lines[index];
				const nextText = lines[next.line].text.slice(next.start);
				if (nextText.startsWith("|")) {
					emit(next.line, next.start, next.start + 1, "indent");
					value.push({ line: next.line, start: next.start + 1 });
				} else if (!nextText.trim() || nextText.trimStart().startsWith("#")) {
					emitIgnoredLine(next, nextText);
				} else {
					break;
				}
				index++;
			}
			if (value.length) children.push({ lines: value, level: scope.level + 1 });
		}
		if (!valid) {
			// Commit no partial keys or comments when this whole value is plain text.
			for (const ref of scope.lines) {
				lines[ref.line].segments.push({ start: ref.start, end: lines[ref.line].text.length, level: scope.level, type: "plain" });
			}
			continue;
		}
		for (const { line, ...segment } of staged) lines[line].segments.push(segment);
		for (const child of children) pending.push(child);
	}
	for (const line of lines) line.segments.sort((left, right) => left.start - right.start);
	return lines;
}
