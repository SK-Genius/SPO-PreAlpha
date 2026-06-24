using System.Buffers;
using System.Text;
using System.Text.Json;

internal sealed class
LspServer {
	private readonly Stream Input;
	private readonly Stream Output;
	private readonly SpoLanguageService LanguageService;
	private readonly Dictionary<tText, tText> Documents = [];
	
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
					writer => {
						writer.WriteStartObject();
						writer.WriteStartObject("capabilities");
						writer.WriteStartObject("textDocumentSync");
						writer.WriteBoolean("openClose", true);
						writer.WriteNumber("change", 1);
						writer.WriteStartObject("save");
						writer.WriteBoolean("includeText", false);
						writer.WriteEndObject();
						writer.WriteEndObject();
						writer.WriteBoolean("definitionProvider", true);
						writer.WriteBoolean("documentSymbolProvider", true);
						writer.WriteEndObject();
						writer.WriteStartObject("serverInfo");
						writer.WriteString("name", "SPO Language Server");
						writer.WriteString("version", "0.1.0");
						writer.WriteEndObject();
						writer.WriteEndObject();
					}
				);
				return false;
			}
			case "initialized": {
				return false;
			}
			case "shutdown": {
				this.ShutdownRequested = true;
				this.WriteResult(id, writeResult: null);
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
			writer => {
				writer.WriteStartObject();
				writer.WriteString("uri", uri);
				writer.WriteStartArray("diagnostics");
				writer.WriteEndArray();
				writer.WriteEndObject();
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
			this.WriteResult(id, writeResult: null);
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
		
		if (location is null) {
			this.WriteResult(id, writeResult: null);
			return;
		}

		this.WriteResult(id, writer => {
			writer.WriteStartObject();
			writer.WriteString("uri", location.Uri);
			writer.WritePropertyName("range");
			WriteRange(writer, location.Range);
			writer.WriteEndObject();
		});
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
			this.WriteResult(id, writer => {
				writer.WriteStartArray();
				writer.WriteEndArray();
			});
			return;
		}
		
		var uri = GetRequiredString(textDocument, "uri");
		var text = this.Documents.GetValueOrDefault(uri, "");
		var symbols = this.LanguageService.GetDocumentSymbols(uri, text);
		this.WriteResult(id, writer => {
			writer.WriteStartArray();
			foreach (var symbol in symbols) {
				WriteDocumentSymbol(writer, symbol);
			}
			writer.WriteEndArray();
		});
	}
	
	private void
	PublishDiagnostics(
		tText uri,
		tText text
	) {
		var diagnostics = this.LanguageService.GetDiagnostics(uri, text);
		this.WriteNotification(
			"textDocument/publishDiagnostics",
			writer => {
				writer.WriteStartObject();
				writer.WriteString("uri", uri);
				writer.WriteStartArray("diagnostics");
				foreach (var diagnostic in diagnostics) {
					writer.WriteStartObject();
					writer.WritePropertyName("range");
					WriteRange(writer, diagnostic.Range);
					writer.WriteNumber("severity", diagnostic.Severity);
					writer.WriteString("source", diagnostic.Source);
					writer.WriteString("message", diagnostic.Message);
					writer.WriteEndObject();
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
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
		Action<Utf8JsonWriter>? writeResult
	) => this.WriteMessage(writer => {
		writer.WriteStartObject();
		writer.WriteString("jsonrpc", "2.0");
		writer.WritePropertyName("id");
		if (id is {} idValue) {
			idValue.WriteTo(writer);
		} else {
			writer.WriteNullValue();
		}
		writer.WritePropertyName("result");
		if (writeResult is null) {
			writer.WriteNullValue();
		} else {
			writeResult(writer);
		}
		writer.WriteEndObject();
	});
	
	private void
	WriteError(
		JsonElement? id,
		tInt32 code,
		tText message
	) => this.WriteMessage(writer => {
		writer.WriteStartObject();
		writer.WriteString("jsonrpc", "2.0");
		writer.WritePropertyName("id");
		if (id is {} idValue) {
			idValue.WriteTo(writer);
		} else {
			writer.WriteNullValue();
		}
		writer.WriteStartObject("error");
		writer.WriteNumber("code", code);
		writer.WriteString("message", message);
		writer.WriteEndObject();
		writer.WriteEndObject();
	});
	
	private void
	WriteNotification(
		tText method,
		Action<Utf8JsonWriter> writeParams
	) => this.WriteMessage(writer => {
		writer.WriteStartObject();
		writer.WriteString("jsonrpc", "2.0");
		writer.WriteString("method", method);
		writer.WritePropertyName("params");
		writeParams(writer);
		writer.WriteEndObject();
	});
	
	private void
	WriteMessage(
		Action<Utf8JsonWriter> writeMessage
	) {
		var payload = new ArrayBufferWriter<tNat8>();
		using (var writer = new Utf8JsonWriter(payload)) {
			writeMessage(writer);
			writer.Flush();
		}
		var header = Encoding.ASCII.GetBytes($"Content-Length: {payload.WrittenCount}\r\n\r\n");
		
		this.Output.Write(header, 0, header.Length);
		this.Output.Write(payload.WrittenSpan);
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
	
	private static void
	WriteDocumentSymbol(
		Utf8JsonWriter writer,
		SpoDocumentSymbol symbol
	) {
		writer.WriteStartObject();
		writer.WriteString("name", symbol.Name);
		writer.WriteString("detail", symbol.Detail);
		writer.WriteNumber("kind", symbol.Kind);
		writer.WritePropertyName("range");
		WriteRange(writer, symbol.Range);
		writer.WritePropertyName("selectionRange");
		WriteRange(writer, symbol.SelectionRange);
		writer.WriteStartArray("children");
		foreach (var child in symbol.Children) {
			WriteDocumentSymbol(writer, child);
		}
		writer.WriteEndArray();
		writer.WriteEndObject();
	}
	
	private static void
	WriteRange(
		Utf8JsonWriter writer,
		SpoRange range
	) {
		writer.WriteStartObject();
		writer.WriteStartObject("start");
		writer.WriteNumber("line", range.Start.Line);
		writer.WriteNumber("character", range.Start.Character);
		writer.WriteEndObject();
		writer.WriteStartObject("end");
		writer.WriteNumber("line", range.End.Line);
		writer.WriteNumber("character", range.End.Character);
		writer.WriteEndObject();
		writer.WriteEndObject();
	}
}
