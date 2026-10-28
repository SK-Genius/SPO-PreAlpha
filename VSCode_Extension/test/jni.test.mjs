import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import Module, { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import test from "node:test";
import { analyzeJni, JNI_BACKGROUNDS } from "../src/jni.mjs";

const example = readFileSync(new URL("./highlighting.jni", import.meta.url), "utf8");

test("nested documents retain each prefix scope and plain leaf", () => {
	const lines = analyzeJni("modules:\n|App:\n||imports:\n|||Std=Std");
	assert.deepEqual(lines[3].segments, [
		{ start: 0, end: 1, level: 0, type: "indent" },
		{ start: 1, end: 2, level: 1, type: "indent" },
		{ start: 2, end: 3, level: 2, type: "indent" },
		{ start: 3, end: 6, level: 3, type: "key" },
		{ start: 6, end: 7, level: 3, type: null },
		{ start: 7, end: 10, level: 4, type: "plain" }
	]);
	assert.deepEqual(JNI_BACKGROUNDS, ["rgba(255, 0, 255, 0.1)", "rgba(68, 68, 255, 0.1)", "rgba(255, 255, 0, 0.1)"]);
});

test("one-line values recurse through equals and retain whitespace", () => {
	const [line] = analyzeJni("  options = theme=dark  ");
	assert.deepEqual(line.segments.filter(segment => segment.type === "key").map(segment => [
		line.text.slice(segment.start, segment.end), segment.level
	]), [["options", 0], ["theme", 1]]);
	const leaf = line.segments.at(-1);
	assert.equal(leaf.type, "plain");
	assert.equal(leaf.level, 2);
	assert.equal(line.text.slice(leaf.start), "dark  ");
	assert.equal(line.segments.map(segment => line.text.slice(segment.start, segment.end)).join(""), line.text);
});

test("a failed value stays entirely plain, including apparent keys and comments", () => {
	const lines = analyzeJni("body:\n|valid=one\n|not JNI\n|# literal\n|last=two\nnext=okay");
	for (const line of lines.slice(1, 5)) {
		assert.deepEqual(line.segments.at(-1), { start: 1, end: line.text.length, level: 1, type: "plain" });
		assert.equal(line.segments.some(segment => segment.type === "key" || segment.type === "comment"), false);
	}
	assert.equal(lines.at(-1).segments[0].type, "key");
});

test("outer comments and empty lines interrupt but do not end a deeper text value", () => {
	const lines = analyzeJni(example);
	for (const [prefix, level] of [["||# Ebene 2:", 2], ["|# Ebene 1:", 1], ["# Ebene 0:", 0]]) {
		const line = lines.find(line => line.text.startsWith(prefix));
		assert.equal(line.segments.at(-1).type, "comment");
		assert.equal(line.segments.at(-1).level, level);
	}
	const literal = lines.find(line => line.text.startsWith("|||# bleibt"));
	assert.equal(literal.segments.at(-1).type, "plain");
	assert.equal(literal.segments.at(-1).level, 3);
	const continuation = lines.find(line => line.text.startsWith("|||weiter="));
	assert.equal(continuation.segments.at(-1).type, "plain");
	const blanks = lines.filter(line => /^\|*$/.test(line.text)).map(line => line.segments.at(-1).level);
	assert.ok(blanks.includes(0) && blanks.includes(1) && blanks.includes(2) && blanks.includes(3));
	assert.ok(lines.some(line => line.text === "|||" && line.segments.at(-1).type === "plain"));
});

test("indentation is structural; pipes and indentation within plain values remain literal", () => {
	const lines = analyzeJni("\tkey=value\n  # comment\n \t\ntext:\n|| literal pipe\n| \tplain text");
	assert.deepEqual(lines[0].segments[0], { start: 0, end: 1, level: 0, type: "indent" });
	assert.deepEqual(lines[1].segments, [
		{ start: 0, end: 2, level: 0, type: "indent" },
		{ start: 2, end: 11, level: 0, type: "comment" }
	]);
	assert.deepEqual(lines[2].segments, [{ start: 0, end: 2, level: 0, type: "indent" }]);
	for (const line of lines.slice(4)) {
		assert.deepEqual(line.segments[0], { start: 0, end: 1, level: 0, type: "indent" });
		assert.deepEqual(line.segments[1], { start: 1, end: line.text.length, level: 1, type: "plain" });
	}
});

test("line endings, source columns, tabs and empty terminal spans are preserved", () => {
	for (const newline of ["\n", "\r\n", "\r"]) {
		const source = ["# top", "\t😀 = value", "body:", "|", "last=", ""].join(newline);
		const lines = analyzeJni(source);
		assert.equal(lines.length, 6);
		for (const line of lines) {
			let end = 0;
			for (const segment of line.segments) {
				assert.equal(segment.start, end);
				end = segment.end;
			}
			assert.equal(end, line.text.length);
		}
		assert.deepEqual(lines[1].segments.find(segment => segment.type === "key"), { start: 1, end: 3, level: 0, type: "key" });
		assert.equal(lines[3].segments.at(-1).start, 1);
		assert.equal(lines[3].segments.at(-1).end, 1);
		assert.equal(lines[4].segments.at(-1).level, 1);
	}
});

test("list duplicates are valid; malformed headers and indented pipes are plain", () => {
	assert.equal(analyzeJni("key=one\nkey=two").every(line => line.segments[0].type === "key"), true);
	for (const source of [" =value", " :", "|orphan", "  |orphan", "# comment\nbroken", "body:\n |value"]) {
		assert.equal(analyzeJni(source).every(line => line.segments.length === 1 && line.segments[0].type === "plain"), true);
	}
	const [line] = analyzeJni("key:part=one");
	assert.equal(line.text.slice(0, line.segments[0].end), "key:part");
});

test("deep nesting uses no JavaScript recursion limit", () => {
	const depth = 4096;
	const [line] = analyzeJni("key=".repeat(depth) + "leaf");
	assert.equal(line.segments.filter(segment => segment.type === "key").at(-1).level, depth - 1);
	assert.equal(line.segments.at(-1).start, depth * 4);
	assert.equal(line.segments.at(-1).type, "plain");
});

test("VSCode handles lexical Enter, refreshes highlights and starts SPO lazily", async () => {
	const handlers = {};
	const decorationTypes = [];
	const document = { languageId: "jni", getText: () => example };
	const createEditor = () => ({ document, decorations: new Map(), setDecorations(type, ranges) { this.decorations.set(type, ranges); } });
	const editor = createEditor();
	const disposable = () => ({ dispose() {} });
	let starts = 0;
	let stops = 0;
	let enterEdit;
	const vscode = {
		commands: { registerCommand: (name, callback) => { handlers.enter = callback; return disposable(); } },
		EndOfLine: { LF: 1, CRLF: 2 },
		SnippetString: function (value) { this.value = value; },
		SnippetTextEdit: function (range, snippet) { Object.assign(this, { range, snippet }); },
		WorkspaceEdit: function () { this.set = (uri, edits) => { Object.assign(this, { uri, edits }); }; },
		Range: function (line, start, endLine, end) { Object.assign(this, { line, start, endLine, end }); },
		DecorationRangeBehavior: { ClosedClosed: 1 },
		window: {
			visibleTextEditors: [editor],
			createOutputChannel: () => ({ ...disposable(), appendLine() {} }),
			createTextEditorDecorationType: options => {
				const type = { ...disposable(), options };
				decorationTypes.push(type);
				return type;
			},
			onDidChangeVisibleTextEditors: callback => { handlers.visible = callback; return disposable(); }
		},
		workspace: {
			applyEdit: async edit => { enterEdit = edit; return true; },
			textDocuments: [document],
			onDidChangeTextDocument: callback => { handlers.change = callback; return disposable(); },
			onDidOpenTextDocument: callback => { handlers.open = callback; return disposable(); },
			createFileSystemWatcher: disposable
		}
	};
	const originalLoad = Module._load;
	Module._load = function (request, ...args) {
		if (request === "vscode") return vscode;
		if (request === "vscode-languageclient/node") return {
			LanguageClient: function () {
				this.start = async () => { starts++; };
				this.stop = async () => { stops++; };
			}
		};
		return originalLoad.call(this, request, ...args);
	};
	const context = { subscriptions: [], extensionPath: fileURLToPath(new URL("../", import.meta.url)) };
	try {
		const require = createRequire(import.meta.url);
		const extension = require("../src/extension.js");
		await extension.activate(context);
		assert.equal(starts, 0);
		for (const type of decorationTypes.filter(type => type.options.backgroundColor)) {
			assert.match(type.options.backgroundColor, /^rgba\(\d+, \d+, \d+, 0\.1\)$/, 'Every JNI background must preserve the editor highlights underneath');
		}
		{
			const getText = document.getText;
			document.getText = () => { throw new Error('Enter must not parse the document'); };
			document.uri = 'jni:test';
			document.eol = vscode.EndOfLine.LF;
			vscode.window.activeTextEditor = editor;
			for (const [before, inserted] of [
				['', '\n'], ['|||text', '\n|||'], ['||# text', '\n||# '],
				['||value:', '\n|||'], ['||key=value:', '\n|||'], ['|:', '\n||'],
				['||key=# text', '\n||'], ['| \ttext', '\n| \t'], ['# comment:', '\n# ']
			]) {
				document.lineAt = () => ({ text: before });
				editor.selections = [{ start: { line: 0, character: before.length }, end: { line: 0, character: before.length } }];
				await handlers.enter();
				assert.equal(enterEdit.uri, document.uri);
				assert.equal(enterEdit.edits[0].snippet.value, inserted);
				assert.equal(enterEdit.edits[0].keepWhitespace, true);
			}
			document.eol = vscode.EndOfLine.CRLF;
			document.lineAt = line => ({ text: ['||# first', '|second'][line] });
			editor.selections = [9, 7].map((character, line) => ({ start: { line, character }, end: { line, character } }));
			await handlers.enter();
			assert.deepEqual(enterEdit.edits.map(edit => edit.snippet.value), ['\r\n||# ', '\r\n|']);
			enterEdit = null;
			document.languageId = 'plaintext';
			await handlers.enter();
			assert.equal(enterEdit, null);
			document.languageId = 'jni';
			document.getText = getText;
		}
		const textBackground = decorationTypes.find(type => type.options.isWholeLine && type.options.backgroundColor === "rgba(68, 68, 255, 0.1)");
		assert.ok(editor.decorations.get(textBackground).some(range => range.line === 2));
		for (const [index, color] of ["#ff00ff", "#4444ff", "#ffff00"].entries()) {
			const indent = decorationTypes.find(type => type.options.color === color);
			assert.ok(editor.decorations.get(indent).some(range => range.line === 24 && range.start === index && range.end === index + 1));
		}
		const nestedSource = [
			"a:", "|b:", "||c:", "|||d:", "||||e:", "|||||f:",
			"|||||| \tkey=value", "||||||# comment", "||||||"
		].join("\n");
		document.getText = () => nestedSource;
		handlers.change({ document });
		const foregrounds = decorationTypes.filter(type => type.options.color);
		for (const line of [6, 7, 8]) {
			assert.deepEqual(foregrounds.flatMap(type => editor.decorations.get(type)
				.filter(range => range.line === line)
				.map(range => [range.start, range.end, type.options.color]))
				.sort((left, right) => left[0] - right[0]), [
				[0, 1, "#ff00ff"], [1, 2, "#4444ff"], [2, 3, "#ffff00"],
				[3, 4, "#ff00ff"], [4, 5, "#4444ff"], [5, 6, "#ffff00"],
				...(line === 6 ? [[6, 8, "#ff00ff"], [8, 11, "#ff00ff"]] : [])
			]);
		}
		assert.ok(editor.decorations.get(textBackground).some(range => range.line === 6));
		document.getText = () => "options=theme=dark";
		handlers.change({ document });
		assert.deepEqual(foregrounds.flatMap(type => editor.decorations.get(type)
			.map(range => [range.start, range.end, type.options.color]))
			.sort((left, right) => left[0] - right[0]), [
			[0, 7, "#ff00ff"], [8, 13, "#4444ff"]
		]);
		const literalSource = "text:\n|| literal pipe\n| \tplain text";
		document.getText = () => literalSource;
		handlers.change({ document });
		for (const decoration of decorationTypes.filter(type => type.options.color)) {
			assert.equal(editor.decorations.get(decoration).some(range => range.line > 0 && range.end > 1), false);
		}
		document.getText = () => example;
		handlers.change({ document });
		const comments = decorationTypes.find(type => type.options.fontStyle === "italic");
		assert.ok(editor.decorations.get(comments).some(range => range.line === 19 && range.start === 2));
		assert.equal(editor.decorations.get(comments).some(range => range.line === 24), false);
		const split = createEditor();
		vscode.window.visibleTextEditors.push(split);
		handlers.visible(vscode.window.visibleTextEditors);
		assert.deepEqual(split.decorations, editor.decorations);
		document.getText = () => "entry=changed";
		handlers.change({ document });
		assert.equal(editor.decorations.get(comments).length, 0);
		assert.equal(split.decorations.get(textBackground).length, 1);
		document.languageId = "plaintext";
		handlers.open(document);
		assert.equal([...editor.decorations.values(), ...split.decorations.values()].every(ranges => ranges.length === 0), true);
		handlers.open({ languageId: "spo" });
		handlers.open({ languageId: "spo" });
		assert.equal(starts, 1);
		await extension.deactivate();
		assert.equal(stops, 1);
	} finally {
		Module._load = originalLoad;
		for (const subscription of context.subscriptions) subscription.dispose();
	}
});
