using System.Text;
using System.Text.Json;

internal sealed class
LspServer {
	private readonly Stream Input;
	private readonly Stream Output;
	private readonly SpoLanguageService LanguageService;
	private readonly Dictionary<tText, tText> Documents = [];
	private readonly JsonSerializerOptions JsonOptions = new();
	
	private tBool ShutdownRequested;
	
	public
	LspServer(
		Stream input,
		Stream output,
		SpoLanguageService languageService
	) {
		this.Input = input;
		this.Output = output;
		this.LanguageService = languageService;
	}
	
	public tInt32
	Run(
	) {
		try {
			while (true) {
				var payload = this.ReadMessage();
				if (payload is null) {
					return this.ShutdownRequested ? 0 : 1;
				}
				
				using var document = JsonDocument.Parse(payload);
				if (this.HandleMessage(document.RootElement)) {
					return this.ShutdownRequested ? 0 : 1;
				}
			}
		} catch (System.Exception exception) {
			this.Log("fatal", exception.ToString());
			return 1;
		}
	}
	
	private tBool
	HandleMessage(
		JsonElement message
	) {
		var hasId = message.TryGetProperty("id", out var idProperty);
		var id = hasId ? idProperty.Clone() : default;
		var method = message.TryGetProperty("method", out var methodProperty)
			? methodProperty.GetString()
			: null;
		
		switch (method) {
			case "initialize": {
				this.WriteResult(
					id,
					new {
						capabilities = new {
							textDocumentSync = new {
								openClose = true,
								change = 1,
								save = new {
									includeText = false
								}
							},
							definitionProvider = true,
							documentSymbolProvider = true
						},
						serverInfo = new {
							name = "SPO Language Server",
							version = "0.1.0"
						}
					}
				);
				return false;
			}
			case "initialized": {
				return false;
			}
			case "shutdown": {
				this.ShutdownRequested = true;
				this.WriteResult(id, result: null);
				return false;
			}
			case "exit": {
				return true;
			}
			case "textDocument/didOpen": {
				this.HandleDidOpen(message);
				return false;
			}
			case "textDocument/didChange": {
				this.HandleDidChange(message);
				return false;
			}
			case "textDocument/didClose": {
				this.HandleDidClose(message);
				return false;
			}
			case "textDocument/didSave": {
				this.HandleDidSave(message);
				return false;
			}
			case "textDocument/definition": {
				this.HandleDefinition(message, id);
				return false;
			}
			case "textDocument/documentSymbol": {
				this.HandleDocumentSymbol(message, id);
				return false;
			}
			default: {
				if (hasId) {
					this.WriteError(id, -32601, "Method not found");
				}
				return false;
			}
		}
	}
	
	private void
	HandleDidOpen(
		JsonElement message
	) {
		if (
			!message.TryGetProperty("params", out var paramsProperty) ||
			!paramsProperty.TryGetProperty("textDocument", out var textDocument)
		) {
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		var text = GetRequiredString(textDocument, "text");
		this.Documents[uri] = text;
		this.PublishDiagnostics(uri, text);
	}
	
	private void
	HandleDidChange(
		JsonElement message
	) {
		if (
			!message.TryGetProperty("params", out var paramsProperty) ||
			!paramsProperty.TryGetProperty("textDocument", out var textDocument) ||
			!paramsProperty.TryGetProperty("contentChanges", out var contentChanges)
		) {
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		var text = contentChanges.GetArrayLength() > 0
			? GetRequiredString(contentChanges[contentChanges.GetArrayLength() - 1], "text")
			: this.Documents.GetValueOrDefault(uri, "");
		
		this.Documents[uri] = text;
		this.PublishDiagnostics(uri, text);
	}
	
	private void
	HandleDidClose(
		JsonElement message
	) {
		if (
			!message.TryGetProperty("params", out var paramsProperty) ||
			!paramsProperty.TryGetProperty("textDocument", out var textDocument)
		) {
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		this.Documents.Remove(uri);
		this.WriteNotification(
			"textDocument/publishDiagnostics",
			new {
				uri,
				diagnostics = Array.Empty<tUnknown>()
			}
		);
	}
	
	private void
	HandleDidSave(
		JsonElement message
	) {
		if (
			!message.TryGetProperty("params", out var paramsProperty) ||
			!paramsProperty.TryGetProperty("textDocument", out var textDocument)
		) {
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		if (this.Documents.TryGetValue(uri, out var text)) {
			this.PublishDiagnostics(uri, text);
		}
	}
	
	private void
	HandleDefinition(
		JsonElement message,
		JsonElement? id
	) {
		if (
			!message.TryGetProperty("params", out var paramsProperty) ||
			!paramsProperty.TryGetProperty("textDocument", out var textDocument) ||
			!paramsProperty.TryGetProperty("position", out var position)
		) {
			this.WriteResult(id, result: null);
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		var text = this.Documents.GetValueOrDefault(uri, "");
		var location = this.LanguageService.GetDefinition(
			uri,
			text,
			GetRequiredInt(position, "line"),
			GetRequiredInt(position, "character")
		);
		
		this.WriteResult(
			id,
			location is null
				? null
				: new {
					uri = location.Uri,
					range = ToLspRange(location.Range)
				}
		);
	}
	
	private void
	HandleDocumentSymbol(
		JsonElement message,
		JsonElement? id
	) {
		if (
			!message.TryGetProperty("params", out var paramsProperty) ||
			!paramsProperty.TryGetProperty("textDocument", out var textDocument)
		) {
			this.WriteResult(id, Array.Empty<tUnknown>());
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		var text = this.Documents.GetValueOrDefault(uri, "");
		var symbols = this.LanguageService.GetDocumentSymbols(uri, text).Select(
			ToLspDocumentSymbol
		).ToArray();
		
		this.WriteResult(id, symbols);
	}
	
	private void
	PublishDiagnostics(
		tText uri,
		tText text
	) {
		var diagnostics = this.LanguageService.GetDiagnostics(uri, text).Select(
			diagnostic => new {
				range = ToLspRange(diagnostic.Range),
				severity = diagnostic.Severity,
				source = diagnostic.Source,
				message = diagnostic.Message
			}
		).ToArray();
		
		this.WriteNotification(
			"textDocument/publishDiagnostics",
			new {
				uri,
				diagnostics
			}
		);
	}
	
	private tText?
	ReadMessage(
	) {
		var contentLength = 0;
		while (true) {
			var line = this.ReadHeaderLine();
			if (line is null) {
				return null;
			}
			
			if (line.Length == 0) {
				break;
			}
			
			var separatorIndex = line.IndexOf(':');
			if (separatorIndex <= 0) {
				continue;
			}
			
			var headerName = line[..separatorIndex];
			var headerValue = line[(separatorIndex + 1)..].Trim();
			if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) {
				contentLength = tInt32.Parse(headerValue, System.Globalization.CultureInfo.InvariantCulture);
			}
		}
		
		if (contentLength <= 0) {
			return null;
		}
		
		var buffer = new tNat8[contentLength];
		var offset = 0;
		while (offset < contentLength) {
			var read = this.Input.Read(buffer, offset, contentLength - offset);
			if (read <= 0) {
				throw new EndOfStreamException("Unexpected end of stream while reading message body.");
			}
			offset += read;
		}
		
		return Encoding.UTF8.GetString(buffer);
	}
	
	private tText?
	ReadHeaderLine(
	) {
		var buffer = new List<tNat8>();
		while (true) {
			var value = this.Input.ReadByte();
			if (value < 0) {
				if (buffer.Count == 0) {
					return null;
				}
				throw new EndOfStreamException("Unexpected end of stream while reading message headers.");
			}
			
			if (value == '\r') {
				var lineFeed = this.Input.ReadByte();
				if (lineFeed != '\n') {
					throw new InvalidDataException("Invalid LSP header termination.");
				}
				return Encoding.ASCII.GetString(buffer.ToArray());
			}
			
			buffer.Add((tNat8)value);
		}
	}
	
	private void
	WriteResult(
		JsonElement? id,
		tUnknown? result
	) => this.WriteMessage(
		new {
			jsonrpc = "2.0",
			id,
			result
		}
	);
	
	private void
	WriteError(
		JsonElement? id,
		tInt32 code,
		tText message
	) => this.WriteMessage(
		new {
			jsonrpc = "2.0",
			id,
			error = new {
				code,
				message
			}
		}
	);
	
	private void
	WriteNotification(
		tText method,
		tUnknown? @params
	) => this.WriteMessage(
		new {
			jsonrpc = "2.0",
			method,
			@params
		}
	);
	
	private void
	WriteMessage(
		tUnknown message
	) {
		var payload = JsonSerializer.SerializeToUtf8Bytes(message, this.JsonOptions);
		var header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");
		
		this.Output.Write(header, 0, header.Length);
		this.Output.Write(payload, 0, payload.Length);
		this.Output.Flush();
	}
	
	private void
	Log(
		tText category,
		tText message
	) {
		Console.Error.WriteLine($"[{category}] {message}");
		Console.Error.Flush();
	}
	
	private static tText
	GetRequiredString(
		JsonElement element,
		tText name
	) => element.TryGetProperty(name, out var value)
		? value.GetString() ?? ""
		: "";
	
	private static tInt32
	GetRequiredInt(
		JsonElement element,
		tText name
	) => element.TryGetProperty(name, out var value) &&
		value.ValueKind == JsonValueKind.Number &&
		value.TryGetInt32(out var result)
		? result
		: 0;
	
	private static tUnknown
	ToLspDocumentSymbol(
		SpoDocumentSymbol symbol
	) => new {
		name = symbol.Name,
		detail = symbol.Detail,
		kind = symbol.Kind,
		range = ToLspRange(symbol.Range),
		selectionRange = ToLspRange(symbol.SelectionRange),
		children = symbol.Children.Select(ToLspDocumentSymbol).ToArray()
	};
	
	private static tUnknown
	ToLspRange(
		SpoRange range
	) => new {
		start = new {
			line = range.Start.Line,
			character = range.Start.Character
		},
		end = new {
			line = range.End.Line,
			character = range.End.Character
		}
	};
}
