#!dotnet
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property ExperimentalFileBasedProgramEnableTransitiveDirectives = true
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property OutputType = Exe
#:include _GlobalUsings.cs
#:include mSPO_Diagnostics.cs
#:include mSPO_Navigation.cs
#:include SpoLanguageService.cs
#:include LspServer.cs

using tPos = mTextStream.tPos;

const tText SourcePath = "navigation-test.spo";

{
	var Folder = mFS.CWD() / "URI Tests #1 %20";
	var SourceFile = Folder.GetFile("Consumer.SPO");
	var SignatureFile = Folder.GetFile("Contract #1.SIG");
	var LeafFile = Folder.GetFile("Nested/Leaf %20.SIG");
	var Files = new Dictionary<tText, tText>(
		(
			System.OperatingSystem.IsWindows()
			? StringComparer.OrdinalIgnoreCase
			: StringComparer.Ordinal
		)
	) {
		[SignatureFile.Path.ToText()] = "[§LOAD \"Nested/Leaf %20.SIG\"]",
		[LeafFile.Path.ToText()] = "§INT"
	};
	var Service = new SpoLanguageService(
		new mTokenizer.tFileContext(
			aFile => {
				Assert(aFile.Path.IsAbsolutePath, "compiler received a relative path from LSP");
				return Files[aFile.Path.ToText()];
			}
		)
	);
	var Source = "§DEF tLoaded = [§LOAD \"Contract #1.SIG\"]\n§DEF Value € tLoaded = 1\n§EXPORT Value";
	var CanonicalUri = new System.Uri(SourceFile.Path.ToAbsolute().ToText()).AbsoluteUri;
	var Uris = new List<tText> {
		CanonicalUri
	};
	if (System.OperatingSystem.IsWindows()) {
		var DriveColon = CanonicalUri.IndexOf(':', "file:///".Length);
		if (DriveColon >= 0) {
			var DriveUri = CanonicalUri[..(DriveColon - 1)] + char.ToLowerInvariant(CanonicalUri[DriveColon - 1]) +
			CanonicalUri[DriveColon..];
			Uris.Add(DriveUri.Replace("file:///", "file://", StringComparison.Ordinal));
			Uris.Add(DriveUri[..DriveColon] + "%3a" + DriveUri[(DriveColon + 1)..]);
		}
		
		Assert(
			LspFiles.ToPath("file://server/share/Some%20Folder/Type%23.SIG") == "//server/share/Some Folder/Type#.SIG",
			"LSP did not decode the UNC URI"
		);
		Assert(
			LspFiles.ToUri("//server/share/Some Folder/Type#.SIG") == "file://server/share/Some%20Folder/Type%23.SIG",
			"LSP did not escape the UNC path"
		);
	}
	
	foreach (var SourceUri in Uris) {
		var Diagnostics = Service.GetDiagnostics(SourceUri, Source);
		Assert(
			Diagnostics.Count == 0,
			"escaped file URI has diagnostics: " + SourceUri + " " +
			string.Join("; ", Diagnostics.Select(aDiagnostic => aDiagnostic.Message))
		);
		var Target = Service.GetDefinition(
			SourceUri,
			Source,
			0,
			Source.IndexOf(
				"Contract",
				StringComparison.Ordinal
			)
		);
		Assert(
			Target is not null && mFS.File(new System.Uri(Target.Uri).LocalPath).IsSameFile(SignatureFile),
			"escaped file URI does not navigate to the signature"
		);
		Assert(
			Target!.Uri.Contains("%20", StringComparison.Ordinal) &&
			Target.Uri.Contains("%23", StringComparison.Ordinal) &&
			Target.Uri.Contains("%2520", StringComparison.Ordinal),
			"definition URI lost escaping for spaces, hashes or literal percent sequences"
		);
		Assert(Service.GetDocumentSymbols(SourceUri, Source).Count == 2, "escaped file URI lost symbols");
		Assert(
			Service.GetDefinition(SourceUri, Source, 2, 8)?.Uri == SourceUri,
			"local definition changed document URI"
		);
		var References = Service.GetReferences(SourceUri, Source, 2, 8, true);
		Assert(
			References.Count == 2 && References.All(aLocation => aLocation.Uri == SourceUri),
			"local references changed document URI"
		);
		Assert(
			Service.PrepareRename(SourceUri, Source, 2, 8)?.Placeholder == "Value",
			"escaped URI cannot prepare rename"
		);
		Assert(Service.Rename(SourceUri, Source, 2, 8, "Renamed")?.Count == 2, "escaped URI cannot rename");
		var Messages = new List<tNat8>();
		AddMessage(
			Messages,
			"{\"jsonrpc\":\"2.0\",\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":" +
			JsonString(SourceUri) +
			",\"text\":" +
			JsonString(Source) +
			"}}}"
		);
		AddMessage(Messages, """{"jsonrpc":"2.0","id":1,"method":"shutdown","params":{}}""");
		AddMessage(Messages, """{"jsonrpc":"2.0","method":"exit","params":{}}""");
		using var Input = new MemoryStream(Messages.ToArray());
		using var Output = new MemoryStream();
		Assert(new LspServer(Input, Output, Service).Run() == 0, "escaped URI broke JSON-RPC handling");
		var Responses = System.Text.Encoding.UTF8.GetString(Output.ToArray());
		Assert(
			Responses.Contains("\"diagnostics\":[]", StringComparison.Ordinal),
			"escaped URI failed LSP diagnostics"
		);
	}
}

{
	var Code = """
	§DEF ...Map...With... = §DEF x => x
	§DEF left = ()
	§DEF arg = ()
	§DEF result = left .Map arg With arg
	§EXPORT result
	""";
	var WithPosition = FindPosition(Code, "With", 1);
	var Definition = mSPO_Navigation.GetDefinition(
		Code,
		SourcePath,
		WithPosition,
		_ => {}
	);
	Assert(Definition.IsSome(out var DefinitionLocation), "mixfix definition missing");
	Assert(
		mTextStream.Eq(DefinitionLocation.Range.Start, FindPosition(Code, "...Map...With...", 0)),
		"mixfix definition starts at the wrong position"
	);
	var ArgumentDefinition = mSPO_Navigation.GetDefinition(
		Code,
		SourcePath,
		FindPosition(Code, "arg", 1),
		_ => {}
	);
	Assert(ArgumentDefinition.IsSome(out var ArgumentLocation), "argument definition missing");
	Assert(
		mTextStream.Eq(ArgumentLocation.Range.Start, FindPosition(Code, "arg", 0)),
		"an argument was mistaken for the enclosing mixfix call"
	);
	var References = ToArray(
		mSPO_Navigation.GetReferences(
			Code,
			SourcePath,
			WithPosition,
			true,
			_ => {}
		)
	);
	Assert(References.Length == 2, $"expected 2 logical mixfix references, got {References.Length}");
	Assert(
		ToArray(
			mSPO_Navigation.GetReferences(
				Code,
				SourcePath,
				WithPosition,
				false,
				_ => {}
			)
		).Length == 1,
		"includeDeclaration=false was ignored"
	);
	Assert(
		mTextStream.Eq(References[1].Range.Start, FindPosition(Code, "Map", 1)),
		"the mixfix use is not represented by its first name part"
	);
	var Target = mSPO_Navigation.PrepareRename(
		Code,
		SourcePath,
		WithPosition,
		_ => {}
	);
	Assert(Target.IsSome(out var RenameTarget), "mixfix rename target missing");
	Assert(RenameTarget.Placeholder == "...Map...With...", "wrong mixfix rename placeholder");
	Assert(
		mTextStream.Eq(RenameTarget.Range.Start, WithPosition),
		"prepareRename did not select the invoked name part"
	);
	var Rename = mSPO_Navigation.Rename(
		Code,
		SourcePath,
		WithPosition,
		"...Transform...Using...",
		_ => {}
	);
	Assert(Rename.IsSome(out var RenameStream), "valid shape-preserving mixfix rename was rejected");
	var Edits = ToArray(RenameStream);
	Assert(Edits.Length == 3, $"expected 3 mixfix edits, got {Edits.Length}");
	Assert(Edits[0].NewText == "...Transform...Using...", "wrong definition edit");
	Assert(Edits[1].NewText == "Transform", "wrong first mixfix part edit");
	Assert(Edits[2].NewText == "Using", "wrong second mixfix part edit");
	Assert(
		!mSPO_Navigation.Rename(
			Code,
			SourcePath,
			WithPosition,
			"Transform",
			_ => {}
		).IsSome(out _),
		"shape-changing mixfix rename was accepted"
	);
}

{
	var Code = """
	§DEF ...Generation_StartWith... = ()
	§DEF ...AsList = ()
	§DEF ...ExtendBy...Rows = ()
	§DEF ...ToTextLines = ()
	§DEF RowOfSize... = ()
	§DEF Rows = ()
	§EXPORT .RowOfSize 31 §> .Generation_StartWith 16 §> .AsList §> .ExtendBy 14 Rows §> .ToTextLines
	""";
	foreach (var Name in new[] {
			"Generation_StartWith",
			"AsList",
			"ExtendBy",
			"ToTextLines"
		}
	
	) {
		var Use = FindPosition(Code, Name, 1);
		var Definition = mSPO_Navigation.GetDefinition(
			Code,
			SourcePath,
			Use,
			_ => {}
		);
		Assert(Definition.IsSome(out var Location), $"pipe definition missing for {Name}");
		Assert(
			mTextStream.Eq(Location.Range.Start, FindPosition(Code, "..." + Name, 0)),
			$"pipe resolved {Name} to the wrong definition"
		);
	}
	
	var Rename = mSPO_Navigation.Rename(
		Code,
		SourcePath,
		FindPosition(Code, "ExtendBy", 1),
		"...GrowBy...Lines",
		_ => {}
	);
	Assert(Rename.IsSome(out var RenameStream), "pipe mixfix rename missing");
	var Edits = ToArray(RenameStream);
	Assert(Edits.Length == 3, $"expected 3 pipe mixfix edits, got {Edits.Length}");
	Assert(Edits[1].NewText == "GrowBy", "wrong first pipe mixfix edit");
	Assert(Edits[2].NewText == "Lines", "wrong second pipe mixfix edit");
}

{
	var Code = """
	§DEF x = ()
	§DEF f = §DEF x => x
	§EXPORT x
	""";
	var OuterReferences = ToArray(
		mSPO_Navigation.GetReferences(
			Code,
			SourcePath,
			FindPosition(Code, "x", 0),
			true,
			_ => {}
		)
	);
	var InnerReferences = ToArray(
		mSPO_Navigation.GetReferences(
			Code,
			SourcePath,
			FindPosition(Code, "x", 1),
			true,
			_ => {}
		)
	);
	Assert(OuterReferences.Length == 2, "outer binding references crossed a shadowing boundary");
	Assert(InnerReferences.Length == 2, "inner binding references crossed a shadowing boundary");
	Assert(
		OuterReferences[0].Range.Start.Col == FindPosition(Code, "x", 0).Col,
		"rename range includes the §DEF keyword"
	);
	Assert(
		!mSPO_Navigation.PrepareRename(
			Code,
			SourcePath,
			FindPosition(Code, "§DEF", 0),
			_ => {}
		).IsSome(out _),
		"the §DEF keyword was treated as part of the binding name"
	);
}

{
	var LspUri = "file:///lsp-test.spo";
	var Code = """
	§DEF ...Map...With... = §DEF x => x
	§DEF left = ()
	§DEF arg = ()
	§DEF result = left .Map arg With arg
	§EXPORT result
	""";
	var InputBytes = new List<tNat8>();
	AddMessage(InputBytes, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"capabilities":{}}}""");
	AddMessage(
		InputBytes,
		$$$$"""{"jsonrpc":"2.0","method":"textDocument/didOpen","params":{"textDocument":{"uri":{{{{JsonString(LspUri)}}}},"languageId":"spo","version":1,"text":{{{{JsonString(Code)}}}}}}}"""
	);
	AddMessage(
		InputBytes,
		$$$$"""{"jsonrpc":"2.0","id":2,"method":"textDocument/references","params":{"textDocument":{"uri":{{{{JsonString(LspUri)}}}}},"position":{"line":3,"character":28},"context":{"includeDeclaration":true}}}"""
	);
	AddMessage(
		InputBytes,
		$$$$"""{"jsonrpc":"2.0","id":3,"method":"textDocument/prepareRename","params":{"textDocument":{"uri":{{{{JsonString(LspUri)}}}}},"position":{"line":3,"character":28}}}"""
	);
	AddMessage(
		InputBytes,
		$$$$"""{"jsonrpc":"2.0","id":4,"method":"textDocument/rename","params":{"textDocument":{"uri":{{{{JsonString(LspUri)}}}}},"position":{"line":3,"character":28},"newName":"...Transform...Using..."}}"""
	);
	AddMessage(InputBytes, """{"jsonrpc":"2.0","id":5,"method":"shutdown","params":{}}""");
	AddMessage(InputBytes, """{"jsonrpc":"2.0","method":"exit","params":{}}""");
	using var Input = new MemoryStream(InputBytes.ToArray());
	using var Output = new MemoryStream();
	Assert(new LspServer(Input, Output, new SpoLanguageService()).Run() == 0, "LSP server failed");
	var Responses = System.Text.Encoding.UTF8.GetString(Output.ToArray());
	Assert(
		Responses.Contains("\"referencesProvider\":true", StringComparison.Ordinal),
		"references capability missing"
	);
	Assert(
		Responses.Contains("\"placeholder\":\"...Map...With...\"", StringComparison.Ordinal),
		"prepareRename placeholder missing"
	);
	Assert(
		Responses.Contains("\"newText\":\"Transform\"", StringComparison.Ordinal),
		"first mixfix LSP edit missing"
	);
	Assert(Responses.Contains("\"newText\":\"Using\"", StringComparison.Ordinal), "second mixfix LSP edit missing");
}

{
	var Folder = mFS.CWD() / "SignatureTests";
	var SignatureFile = Folder.GetFile("Contract.SIG");
	var Files = new System.Collections.Generic.Dictionary<tText, tText>();
	var SignatureUri = LspFiles.ToUri(SignatureFile.Path.ToText());
	var Signature = "[§SIG_WITH F € [§TYPE => §TYPE] IN [< Value: [§GENERIC t [t => [.F t]]] >]]";
	Files[SignatureFile.Path.ToText()] = Signature;
	var Service = new SpoLanguageService(new mTokenizer.tFileContext(aFile => Files[aFile.Path.ToText()]));
	Assert(Service.GetDiagnostics(SignatureUri, Signature).Count == 0, "valid SIG has diagnostics");
	Assert(Service.GetDiagnostics(SignatureUri, Signature + "\n§INT").Count > 0, "SIG accepted two types");
	Assert(
		Service.GetDiagnostics(SignatureUri, "[§LOAD \"./Contract.SIG\"]").Count > 0,
		"unsaved SIG can load itself from disk"
	);
	Assert(Service.GetDocumentSymbols(SignatureUri, Signature).Count > 0, "SIG symbols missing");
	var HeadUse = Signature.IndexOf(".F", StringComparison.Ordinal) + 1;
	var Definition = Service.GetDefinition(SignatureUri, Signature, 0, HeadUse);
	Assert(
		Definition is not null && Definition.Uri == SignatureUri &&
		Definition.Range.Start.Character == Signature.IndexOf("F", StringComparison.Ordinal),
		"SIG head definition missing"
	);
	var Rename = Service.Rename(SignatureUri, Signature, 0, HeadUse, "G");
	Assert(Rename is not null && Rename.Count == 2, "SIG head rename missing");
	var SourceUri = LspFiles.ToUri(Folder.GetFile("Consumer.SPO").Path.ToText());
	var Source = "§EXPORT [§LOAD \"Contract.SIG\"]\n";
	Assert(Service.GetDiagnostics(SourceUri, Source).Count == 0, "SPO cannot load a SIG");
	var Target = Service.GetDefinition(SourceUri, Source, 0, Source.IndexOf("Contract", StringComparison.Ordinal));
	Assert(Target is not null && Target.Uri == SignatureUri, "LOAD does not navigate to its file");
	var Multiline = "[\n§LOAD \"Contract.SIG\"\n]";
	Assert(
		Service.GetDefinition(SignatureUri, Multiline, 1, 8)?.Uri == SignatureUri,
		"multiline LOAD does not navigate to its file"
	);
	var Unbracketed = "§LOAD \"Contract.SIG\"";
	Assert(
		Service.GetDefinition(SignatureUri, Unbracketed, 0, 8)is null,
		"unbracketed LOAD navigates as a type load"
	);
	Files[SignatureFile.Path.ToText()] = "[§LOAD \"./Contract.SIG\"]";
	var Diagnostics = Service.GetDiagnostics(SourceUri, Source);
	Assert(
		Diagnostics.Count == 1 && Diagnostics[0].Source == "parser" &&
		Diagnostics[0].Message.Contains("recursive §LOAD"),
		"recursive SIG is not a parser diagnostic"
	);
	Assert(Diagnostics[0].Range.Start.Character == 8, "LOAD error is not located at the directive");
	var Messages = new List<tNat8>();
	AddMessage(
		Messages,
		"{\"jsonrpc\":\"2.0\",\"method\":\"textDocument/didOpen\",\"params\":{\"textDocument\":{\"uri\":" +
		JsonString(SourceUri) +
		",\"text\":" +
		JsonString(Source) +
		"}}}"
	);
	AddMessage(
		Messages,
		"{\"jsonrpc\":\"2.0\",\"method\":\"textDocument/didSave\",\"params\":{\"textDocument\":{\"uri\":" +
		JsonString(SignatureUri) +
		"}}}"
	);
	AddMessage(
		Messages,
		(
			"{\"jsonrpc\":\"2.0\",\"method\":\"workspace/didChangeWatchedFiles\",\"params\":{\"changes\":" +
			"[{\"uri\":"
		) +
		JsonString(SignatureUri) +
		",\"type\":2}]}}"
	);
	AddMessage(Messages, "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"shutdown\"}");
	AddMessage(Messages, "{\"jsonrpc\":\"2.0\",\"method\":\"exit\"}");
	using var Input = new System.IO.MemoryStream(Messages.ToArray());
	using var Output = new System.IO.MemoryStream();
	Assert(new LspServer(Input, Output, Service).Run() == 0, "SIG notification session failed");
	var Responses = System.Text.Encoding.UTF8.GetString(Output.ToArray());
	Assert(
		Responses.Split("textDocument/publishDiagnostics").Length == 4,
		"SIG saves and file changes do not recheck consumers"
	);
	Files[SignatureFile.Path.ToText()] = "t";
	var Context = "[§GENERIC t [§LOAD \"Contract.SIG\"]]";
	var ContextUri = LspFiles.ToUri(Folder.GetFile("Context.SIG").Path.ToText());
	Assert(
		Service.GetDiagnostics(ContextUri, Context).Count == 0,
		"loaded type cannot use its enclosing type parameter"
	);
	Assert(
		Service.Rename(ContextUri, Context, 0, Context.IndexOf(" t ", StringComparison.Ordinal) + 1, "u")is null,
		"rename returned edits from another file as local edits"
	);
	var References = Service.GetReferences(
		ContextUri,
		Context,
		0,
		Context.IndexOf(" t ", StringComparison.Ordinal) +
		1,
		false
	);
	Assert(
		References.Count == 1 && References[0].Uri == SignatureUri,
		"loaded reference points to the containing file instead of the SIG"
	);
	Assert(
		References[0].Range.Start.Line == 0 && References[0].Range.Start.Character == 0,
		"loaded reference has the wrong position"
	);
}

{
	var Code = """
	§RECURSIVE {
		§DEF First... = (§VAR aSelf € §INT) : (§DEF aStop € §BOOL) {
			§RETURN §TO_VAL aSelf IF aStop
			aSelf : Second §TRUE => §DEF Value .
			§RETURN Value
		}
		§DEF Second... = (§VAR aSelf € §INT) : (§DEF aStop € §BOOL) => {
			aSelf : First §TRUE => §DEF Value .
			§RETURN Value
		}
	}
	§VAR Cell := 7 .
	Cell : First §FALSE => §DEF Value .
	§EXPORT Value
	""";
	foreach (var Name in new[] {
			"First",
			"Second"
		}
	
	) {
		var Definition = mSPO_Navigation.GetDefinition(
			Code,
			SourcePath,
			FindPosition(Code, Name + " §TRUE", 0),
			_ => {}
		);
		Assert(Definition.IsSome(out var Location), "recursive method definition missing: " + Name);
		var Declaration = FindPosition(Code, Name + "...", 0);
		Assert(mTextStream.Eq(Location.Range.Start, Declaration), "wrong recursive method definition: " + Name);
	}
	
	var References = ToArray(
		mSPO_Navigation.GetReferences(
			Code,
			SourcePath,
			FindPosition(Code, "First", 0),
			true,
			_ => {}
		)
	);
	Assert(References.Length == 3, "recursive method references missing");
}

{
	var Code = """
	§DEF t = [§INT]
	§DEF Echo... = (§DEF t) <=> (§VAR aSelf € §INT) : (§DEF aValue € t) => aValue
	§VAR Cell := 7 .
	Cell : Echo §TRUE => §DEF Value .
	§EXPORT Value
	""";
	var BeforeType = FindPosition(Code, "€ t", 0);
	var TypeUse = mTextStream.Pos(SourcePath, BeforeType.Row, BeforeType.Col + 2);
	var Declaration = FindPosition(Code, "t)", 0);
	var Definition = mSPO_Navigation.GetDefinition(
		Code,
		SourcePath,
		TypeUse,
		_ => {}
	);
	Assert(Definition.IsSome(out var Location), "generic method type parameter definition missing");
	Assert(
		mTextStream.Eq(Location.Range.Start, Declaration),
		"generic method type parameter did not shadow outer t"
	);
	var References = ToArray(
		mSPO_Navigation.GetReferences(
			Code,
			SourcePath,
			Declaration,
			true,
			_ => {}
		)
	);
	Assert(References.Length == 2, "generic method type parameter references missing or include outer t");
}

{
	var Code = """
	§DEF iReader = [§INTERFACE t [< Read: [t : [] => §INT] >]]
	§VAR Number := 7 .
	§DEF Reader = §INTERFACE iReader WITH Number IN {
		Read: (§VAR aValue € §INT) : () => §TO_VAL aValue
	}
	§EXPORT 7
	""";
	foreach (var Name in new[] {
			"iReader",
			"Number",
			"aValue"
		}
	
	) {
		var Use = FindPosition(Code, Name, 1);
		var Definition = mSPO_Navigation.GetDefinition(
			Code,
			SourcePath,
			Use,
			_ => {}
		);
		Assert(Definition.IsSome(out var Location), "interface definition missing: " + Name);
		Assert(
			mTextStream.Eq(Location.Range.Start, FindPosition(Code, Name, 0)),
			"wrong interface definition: " + Name
		);
		var References = ToArray(
			mSPO_Navigation.GetReferences(
				Code,
				SourcePath,
				Use,
				true,
				_ => {}
			)
		);
		Assert(References.Length == 2, "interface references missing: " + Name);
	}
	
	var Symbols = ToArray(
		mSPO_Navigation.GetDocumentSymbols(
			Code,
			SourcePath,
			_ => {}
		)
	);
	var Reader = Symbols.Single(Symbol => Symbol.Name == "Reader");
	Assert(
		ToArray(Reader.Children).Any(Symbol => Symbol.Name == "aValue"),
		"interface method parameter symbol missing"
	);
}

return 0;
static void
AddMessage(
	List<tNat8> aBytes,
	tText aMessage
) {
	var Body = System.Text.Encoding.UTF8.GetBytes(aMessage);
	var Header = System.Text.Encoding.ASCII.GetBytes($"Content-Length: {Body.Length}\r\n\r\n");
	aBytes.AddRange(Header);
	aBytes.AddRange(Body);
}

static tText
JsonString(
	tText aText
) => "\"" + System.Text.Json.JsonEncodedText.Encode(aText) + "\"";
static tPos
FindPosition(
	tText aCode,
	tText aNeedle,
	tInt32 aOccurrence
) {
	var Index = -1;
	for (var I = 0; I <= aOccurrence; I += 1) {
		Index = aCode.IndexOf(aNeedle, Index + 1, StringComparison.Ordinal);
		if (Index < 0) {
			throw new InvalidOperationException($"'{aNeedle}' occurrence {aOccurrence} not found");
		}
	}
	
	var Row = 1u;
	var Col = 1u;
	for (var I = 0; I < Index; I += 1) {
		if (aCode[I] == '\n') {
			Row += 1;
			Col = 1;
		} else if (aCode[I] != '\r') {
			Col += 1;
		}
	}
	
	return mTextStream.Pos(SourcePath, Row, Col);
}

static t[]
ToArray<t>(
	mStream.tStream<t> aStream
) {
	var Result = new List<t>();
	foreach (var Item in aStream) {
		Result.Add(Item);
	}
	
	return Result.ToArray();
}

static void
Assert(
	tBool aCondition,
	tText aMessage
) {
	if (!aCondition) {
		throw new InvalidOperationException(aMessage);
	}
}