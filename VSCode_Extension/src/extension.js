const fs = require('fs');
const path = require('path');
const vscode = require('vscode');

let client;
let outputChannel;

function loadLanguageClient(context) {
	const candidates = [
		'vscode-languageclient/node',
		path.join(context.extensionPath, 'node_modules', 'vscode-languageclient', 'node.js'),
		path.join(context.extensionPath, 'node_modules', 'vscode-languageclient', 'lib', 'node', 'main.js'),
	];
	
	let lastError;
	for (const candidate of candidates) {
		try {
			return require(candidate);
		} catch (error) {
			lastError = error;
		}
	}
	
	throw lastError;
}

function activate(context) {
	outputChannel = vscode.window.createOutputChannel('SPO Language');
	context.subscriptions.push(outputChannel);
	outputChannel.appendLine('Activating SPO Language extension.');
	
	const serverExecutable = path.join(context.extensionPath, 'server', 'SPO.LSP.Server.exe');
	if (!fs.existsSync(serverExecutable)) {
		const message = `SPO language server executable was not found at ${serverExecutable}.`;
		outputChannel.appendLine(message);
		throw new Error(message);
	}
	
	const { LanguageClient } = loadLanguageClient(context);
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
	};
	
	client = new LanguageClient('spo-lang', 'SPO Language Server', serverOptions, clientOptions);
	outputChannel.appendLine(`Starting language server: ${serverExecutable}`);
	context.subscriptions.push(client.start());
}

function deactivate() {
	if (!client) return undefined;
	return client.stop();
}

module.exports = {
	activate,
	deactivate
};
