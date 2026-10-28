const fs = require('fs');
const path = require('path');
const vscode = require('vscode');

let client;

async function activate(context) {
	const outputChannel = vscode.window.createOutputChannel('SPO Language');
	context.subscriptions.push(outputChannel);
	outputChannel.appendLine('Activating SPO Language extension.');
	
	const { analyzeJni, JNI_BACKGROUNDS, JNI_FOREGROUNDS } = await import('./jni.mjs');
	context.subscriptions.push(vscode.commands.registerCommand('spo.jniEnter', async () => {
		const editor = vscode.window.activeTextEditor;
		if (!editor || editor.document.languageId !== 'jni') return;
		const document = editor.document;
		const newline = document.eol === vscode.EndOfLine.CRLF ? '\r\n' : '\n';
		const edits = editor.selections.map(range => {
			const before = document.lineAt(range.start.line).text.slice(0, range.start.character);
			const [, pipes, whitespace, comment] = before.match(/^(\|*)([\t ]*)(#?)/);
			const block = !comment && before.trimEnd().endsWith(':');
			const edit = new vscode.SnippetTextEdit(range,
				new vscode.SnippetString(newline + pipes + (block ? '|' : '') + whitespace + (comment ? '# ' : '')));
			edit.keepWhitespace = true;
			return edit;
		});
		const edit = new vscode.WorkspaceEdit();
		edit.set(document.uri, edits);
		await vscode.workspace.applyEdit(edit);
	}));
	const backgrounds = JNI_BACKGROUNDS.map((color, index) => {
		return {
			line: vscode.window.createTextEditorDecorationType({
				backgroundColor: color,
				isWholeLine: true,
				rangeBehavior: vscode.DecorationRangeBehavior.ClosedClosed
			}),
			span: vscode.window.createTextEditorDecorationType({
				backgroundColor: color,
				rangeBehavior: vscode.DecorationRangeBehavior.ClosedClosed
			}),
			foreground: vscode.window.createTextEditorDecorationType({
				color: JNI_FOREGROUNDS[index],
				rangeBehavior: vscode.DecorationRangeBehavior.ClosedClosed
			})
		};
	});
	const comment = vscode.window.createTextEditorDecorationType({
		fontStyle: 'italic', light: { color: '#27591b' }, dark: { color: '#b2d78b' }
	});
	context.subscriptions.push(...backgrounds.flatMap(value => [value.line, value.span, value.foreground]), comment);
	function updateJni(editor) {
		const wholeLines = backgrounds.map(() => []);
		const spans = backgrounds.map(() => []);
		const foregrounds = backgrounds.map(() => []);
		const comments = [];
		if (editor.document.languageId === 'jni') {
			for (const [number, line] of analyzeJni(editor.document.getText()).entries()) {
				for (const [index, segment] of line.segments.entries()) {
					const background = segment.level % backgrounds.length;
					if (index === line.segments.length - 1) {
						wholeLines[background].push(new vscode.Range(number, 0, number, line.text.length));
					}
					if (segment.start === segment.end) continue;
					const range = new vscode.Range(number, segment.start, number, segment.end);
					if (index < line.segments.length - 1) spans[background].push(range);
					if (segment.type === 'indent' || segment.type === 'key') foregrounds[background].push(range);
					if (segment.type === 'comment') comments.push(range);
				}
			}
		}
		for (const [index, decoration] of backgrounds.entries()) {
			editor.setDecorations(decoration.line, wholeLines[index]);
			editor.setDecorations(decoration.span, spans[index]);
			editor.setDecorations(decoration.foreground, foregrounds[index]);
		}
		editor.setDecorations(comment, comments);
	}
	context.subscriptions.push(
		vscode.window.onDidChangeVisibleTextEditors(editors => editors.forEach(updateJni)),
		vscode.workspace.onDidChangeTextDocument(({ document }) => {
			for (const editor of vscode.window.visibleTextEditors) {
				if (editor.document === document) updateJni(editor);
			}
		}),
		vscode.workspace.onDidOpenTextDocument(document => {
			if (document.languageId === 'spo') startSpoClient();
			for (const editor of vscode.window.visibleTextEditors) {
				if (editor.document === document) updateJni(editor);
			}
		})
	);
	vscode.window.visibleTextEditors.forEach(updateJni);
	if (vscode.workspace.textDocuments.some(document => document.languageId === 'spo')) startSpoClient();

	function startSpoClient() {
		if (client) return;
		const serverExecutable = path.join(context.extensionPath, 'server', 'SPO.LSP.Server.exe');
		if (!fs.existsSync(serverExecutable)) {
			const message = `SPO language server executable was not found at ${serverExecutable}.`;
			outputChannel.appendLine(message);
			throw new Error(message);
		}

		let languageClient;
		let lastError;
		for (const candidate of [
			'vscode-languageclient/node',
			path.join(context.extensionPath, 'node_modules', 'vscode-languageclient', 'node.js'),
			path.join(context.extensionPath, 'node_modules', 'vscode-languageclient', 'lib', 'node', 'main.js')
		]) {
			try {
				languageClient = require(candidate);
				break;
			} catch (error) {
				lastError = error;
			}
		}
		if (!languageClient) throw lastError;
		const { LanguageClient } = languageClient;
		const serverOptions = {
			run: {
				command: serverExecutable
			},
			debug: {
				command: serverExecutable
			}
		};

		const clientOptions = {
			documentSelector: [{ scheme: 'file', language: 'spo' }],
			synchronize: {
				fileEvents: vscode.workspace.createFileSystemWatcher('**/*.{SIG,sig}')
			}
		};
		context.subscriptions.push(clientOptions.synchronize.fileEvents);

		client = new LanguageClient('spo-lang', 'SPO Language Server', serverOptions, clientOptions);
		outputChannel.appendLine(`Starting language server: ${serverExecutable}`);
		client.start();
	}
}

function deactivate() {
	if (!client) return undefined;
	return client.stop();
}

module.exports = {
	activate,
	deactivate
};
