const assert = require('node:assert/strict');
const vscode = require('vscode');

exports.run = async () => {
	await vscode.extensions.getExtension('local.spo-lang').activate();
	let checked = 0;
	async function check(name, source, selections, expected, cursors, undo = false) {
		const document = await vscode.workspace.openTextDocument({ language: 'jni', content: source });
		const editor = await vscode.window.showTextDocument(document);
		const originalText = document.getText();
		const newline = document.eol === vscode.EndOfLine.CRLF ? '\r\n' : '\n';
		const result = expected.replace(/\r\n|\r|\n/g, newline);
		if (source.includes('\r\n')) assert.equal(document.eol, vscode.EndOfLine.CRLF, name + ': CRLF');
		editor.selections = selections.map(([startLine, startColumn, endLine = startLine, endColumn = startColumn]) =>
			new vscode.Selection(startLine, startColumn, endLine, endColumn));
		const original = editor.selections;
		await vscode.commands.executeCommand('spo.jniEnter');
		assert.equal(document.getText(), result, name);
		assert.deepEqual(editor.selections.map(selection => [selection.active.line, selection.active.character]), cursors, name + ': cursor');
		assert.ok(editor.selections.every(selection => selection.isEmpty), name + ': collapsed selections');
		if (undo) {
			await vscode.commands.executeCommand('undo');
			assert.equal(document.getText(), originalText, name + ': one undo');
			assert.deepEqual(editor.selections, original, name + ': undo selections');
			await vscode.commands.executeCommand('redo');
			assert.equal(document.getText(), result, name + ': one redo');
			assert.deepEqual(editor.selections.map(selection => [selection.active.line, selection.active.character]), cursors, name + ': redo cursors');
		}
		checked++;
		await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
	}
	await check('root value', 'key=value', [[0, 9]], 'key=value\n', [[1, 0]]);
	await check('root block', 'root:', [[0, 5]], 'root:\n|', [[1, 1]], true);
	await check('nested value', 'root:\n|body:\n||key=value', [[2, 11]], 'root:\n|body:\n||key=value\n||', [[3, 2]]);
	await check('nested block', 'root:\n|body:', [[1, 6]], 'root:\n|body:\n||', [[2, 2]]);
	await check('local whitespace', 'root:\n| \tbody:  ', [[1, 10]], 'root:\n| \tbody:  \n|| \t', [[2, 4]]);
	await check('root comment', '# comment', [[0, 9]], '# comment\n# ', [[1, 2]], true);
	await check('nested comment', 'root:\n|body:\n||# comment', [[2, 11]], 'root:\n|body:\n||# comment\n||# ', [[3, 4]], true);
	await check('comment colon', 'root:\n|# comment:', [[1, 11]], 'root:\n|# comment:\n|# ', [[2, 3]]);
	await check('comment whitespace', 'root:\n| \t# comment', [[1, 12]], 'root:\n| \t# comment\n| \t# ', [[2, 5]]);
	await check('empty comment', 'root:\n|# ', [[1, 3]], 'root:\n|# \n|# ', [[2, 3]]);
	await check('split comment', 'root:\n|# firstlast', [[1, 8]], 'root:\n|# first\n|# last', [[2, 3]], true);
	await check('before comment marker', 'root:\n|# comment', [[1, 1]], 'root:\n|\n|# comment', [[2, 1]]);
	await check('plain hash', 'root:\n|body:\n||plain text\n||# literal', [[3, 11]], 'root:\n|body:\n||plain text\n||# literal\n||# ', [[4, 4]]);
	await check('value colon', 'root:\n|key=value:', [[1, 11]], 'root:\n|key=value:\n||', [[2, 2]]);
	await check('plain text colon', 'root:\n|body:\n||Text:\n||plain text', [[2, 7]], 'root:\n|body:\n||Text:\n|||\n||plain text', [[3, 3]]);
	await check('unparsed pipes', '|||text', [[0, 7]], '|||text\n|||', [[1, 3]], true);
	await check('unparsed comment', '|||# text:', [[0, 10]], '|||# text:\n|||# ', [[1, 5]], true);
	await check('empty pipe line', 'root:\n|body:\n||', [[2, 2]], 'root:\n|body:\n||\n||', [[3, 2]]);
	await check('split value', 'root:\n|key=firstlast', [[1, 10]], 'root:\n|key=first\n|last', [[2, 1]], true);
	await check('split prefix', 'root:\n|body:\n||key=value', [[2, 1]], 'root:\n|body:\n|\n||key=value', [[3, 1]]);
	await check('reversed selection', 'root:\n|key=firstlast', [[1, 14, 1, 10]], 'root:\n|key=first\n|', [[2, 1]], true);
	await check('multiline selection', 'root:\n|key=first\n|next=last', [[1, 5, 2, 6]], 'root:\n|key=\n|last', [[2, 1]], true);
	await check('multiple cursors', 'outer:\n|first=one\n|inner:\n||second=two', [[3, 12], [1, 10]],
		'outer:\n|first=one\n|\n|inner:\n||second=two\n||', [[2, 1], [5, 2]], true);
	await check('CRLF', 'root:\r\n|key=value', [[1, 10]], 'root:\r\n|key=value\r\n|', [[2, 1]], true);
	console.log(`JNI Enter: ${checked} integration cases passed.`);
};
