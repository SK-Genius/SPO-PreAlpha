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

const tText Uri = "file:///navigation-test.spo";

{
	var Code = """
		§DEF ...Map...With... = §DEF x => x
		§DEF left = ()
		§DEF arg = ()
		§DEF result = left .Map arg With arg
		§EXPORT result
		""";
	var WithPosition = FindPosition(Code, "With", 1);

	var Definition = mSPO_Navigation.GetDefinition(Code, Uri, WithPosition, _ => {});
	Assert(Definition.IsSome(out var DefinitionLocation), "mixfix definition missing");
	Assert(
		mTextStream.Eq(DefinitionLocation.Range.Start, FindPosition(Code, "...Map...With...", 0)),
		"mixfix definition starts at the wrong position"
	);

	var ArgumentDefinition = mSPO_Navigation.GetDefinition(
		Code,
		Uri,
		FindPosition(Code, "arg", 1),
		_ => {}
	);
	Assert(ArgumentDefinition.IsSome(out var ArgumentLocation), "argument definition missing");
	Assert(
		mTextStream.Eq(ArgumentLocation.Range.Start, FindPosition(Code, "arg", 0)),
		"an argument was mistaken for the enclosing mixfix call"
	);

	var References = ToArray(mSPO_Navigation.GetReferences(Code, Uri, WithPosition, true, _ => {}));
	Assert(References.Length == 2, $"expected 2 logical mixfix references, got {References.Length}");
	Assert(
		ToArray(mSPO_Navigation.GetReferences(Code, Uri, WithPosition, false, _ => {})).Length == 1,
		"includeDeclaration=false was ignored"
	);
	Assert(
		mTextStream.Eq(References[1].Range.Start, FindPosition(Code, "Map", 1)),
		"the mixfix use is not represented by its first name part"
	);

	var Target = mSPO_Navigation.PrepareRename(Code, Uri, WithPosition, _ => {});
	Assert(Target.IsSome(out var RenameTarget), "mixfix rename target missing");
	Assert(RenameTarget.Placeholder == "...Map...With...", "wrong mixfix rename placeholder");
	Assert(
		mTextStream.Eq(RenameTarget.Range.Start, WithPosition),
		"prepareRename did not select the invoked name part"
	);

	var Rename = mSPO_Navigation.Rename(
		Code,
		Uri,
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
		!mSPO_Navigation.Rename(Code, Uri, WithPosition, "Transform", _ => {}).IsSome(out _),
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

	foreach (var Name in new[] { "Generation_StartWith", "AsList", "ExtendBy", "ToTextLines" }) {
		var Use = FindPosition(Code, Name, 1);
		var Definition = mSPO_Navigation.GetDefinition(Code, Uri, Use, _ => {});
		Assert(Definition.IsSome(out var Location), $"pipe definition missing for {Name}");
		Assert(
			mTextStream.Eq(Location.Range.Start, FindPosition(Code, "..." + Name, 0)),
			$"pipe resolved {Name} to the wrong definition"
		);
	}

	var Rename = mSPO_Navigation.Rename(
		Code,
		Uri,
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
		mSPO_Navigation.GetReferences(Code, Uri, FindPosition(Code, "x", 0), true, _ => {})
	);
	var InnerReferences = ToArray(
		mSPO_Navigation.GetReferences(Code, Uri, FindPosition(Code, "x", 1), true, _ => {})
	);
	Assert(OuterReferences.Length == 2, "outer binding references crossed a shadowing boundary");
	Assert(InnerReferences.Length == 2, "inner binding references crossed a shadowing boundary");
	Assert(
		OuterReferences[0].Range.Start.Col == FindPosition(Code, "x", 0).Col,
		"rename range includes the §DEF keyword"
	);
	Assert(
		!mSPO_Navigation.PrepareRename(Code, Uri, FindPosition(Code, "§DEF", 0), _ => {}).IsSome(out _),
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
	Assert(Responses.Contains("\"referencesProvider\":true", StringComparison.Ordinal), "references capability missing");
	Assert(
		Responses.Contains("\"placeholder\":\"...Map...With...\"", StringComparison.Ordinal),
		"prepareRename placeholder missing"
	);
	Assert(Responses.Contains("\"newText\":\"Transform\"", StringComparison.Ordinal), "first mixfix LSP edit missing");
	Assert(Responses.Contains("\"newText\":\"Using\"", StringComparison.Ordinal), "second mixfix LSP edit missing");
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
	return mTextStream.Pos(Uri, Row, Col);
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
