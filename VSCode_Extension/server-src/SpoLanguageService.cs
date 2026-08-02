using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

internal sealed class
SpoLanguageService {
	public IReadOnlyList<SpoDiagnostic>
	GetDiagnostics(
		tText uri,
		tText text
	) {
		try {
			var diagnostics = new List<SpoDiagnostic>();
			foreach (var diagnostic in mSPO_Diagnostics.GetModuleDiagnostics(text, uri, _ => {})) {
				diagnostics.Add(
					new(
						Range: ToRange(diagnostic.Pos),
						Severity: (tInt32)diagnostic.Severity,
						Source: diagnostic.Source,
						Message: diagnostic.Message
					)
				);
			}
			return diagnostics;
		} catch (Exception exception) {
			return [
				new(
					Range: ToRange(mSpan.Span(mTextStream.Pos(uri, 1, 1))),
					Severity: (tInt32)mSPO_Diagnostics.tDiagnosticSeverity.Error,
					Source: "lsp",
					Message: "internal server error: " + exception.Message
				)
			];
		}
	}
	
	public IReadOnlyList<SpoDocumentSymbol>
	GetDocumentSymbols(
		tText uri,
		tText text
	) {
		try {
			var Symbols = new List<SpoDocumentSymbol>();
			foreach (var Symbol in mSPO_Navigation.GetDocumentSymbols(text, uri, _ => {})) {
				Symbols.Add(ToDocumentSymbol(Symbol));
			}
			return Symbols;
		} catch {
			return Array.Empty<SpoDocumentSymbol>();
		}
	}
	
	public SpoLocation?
	GetDefinition(
		tText uri,
		tText text,
		tInt32 line,
		tInt32 character
	) {
		try {
			var Definition = mSPO_Navigation.GetDefinition(
				text,
				uri,
				ToParserPosition(uri, line, character),
				_ => {}
			);
			return Definition.IsSome(out var Location)
			? ToLocation(Location)
			: null;
		} catch {
			return null;
		}
	}

	public IReadOnlyList<SpoLocation>
	GetReferences(
		tText uri,
		tText text,
		tInt32 line,
		tInt32 character,
		tBool includeDeclaration
	) {
		try {
			var Locations = new List<SpoLocation>();
			foreach (var Location in mSPO_Navigation.GetReferences(
				text,
				uri,
				ToParserPosition(uri, line, character),
				includeDeclaration,
				_ => {}
			)) {
				Locations.Add(ToLocation(Location));
			}
			return Locations;
		} catch {
			return Array.Empty<SpoLocation>();
		}
	}

	public SpoRenameTarget?
	PrepareRename(
		tText uri,
		tText text,
		tInt32 line,
		tInt32 character
	) {
		try {
			var Target = mSPO_Navigation.PrepareRename(
				text,
				uri,
				ToParserPosition(uri, line, character),
				_ => {}
			);
			return Target.IsSome(out var Value)
				? new(ToRange(Value.Range), Value.Placeholder)
				: null;
		} catch {
			return null;
		}
	}

	public IReadOnlyList<SpoTextEdit>?
	Rename(
		tText uri,
		tText text,
		tInt32 line,
		tInt32 character,
		tText newName
	) {
		try {
			var Rename = mSPO_Navigation.Rename(
				text,
				uri,
				ToParserPosition(uri, line, character),
				newName,
				_ => {}
			);
			if (!Rename.IsSome(out var NavigationEdits)) {
				return null;
			}

			var Edits = new List<SpoTextEdit>();
			foreach (var Edit in NavigationEdits) {
				Edits.Add(new(ToRange(Edit.Range), Edit.NewText));
			}
			return Edits;
		} catch {
			return null;
		}
	}
	
	private static SpoRange
	ToRange(
		tSpan span
	) {
		var start = ToStartPosition(span.Start);
		var end = ToEndPosition(span.End);
		if (start.Line == end.Line && end.Character <= start.Character) {
			end = end with { Character = start.Character + 1 };
		}
		
		return new SpoRange(
			Start: start,
			End: end with {
				Line = Math.Max(end.Line, start.Line)
			}
		);
	}
	
	private static SpoPosition
	ToStartPosition(
		tPos pos
	) => new(
		Line: Math.Max((tInt32)pos.Row - 1, 0),
		Character: Math.Max((tInt32)pos.Col - 1, 0)
	);
	
	private static SpoPosition
	ToEndPosition(
		tPos pos
	) => new(
		Line: Math.Max((tInt32)pos.Row - 1, 0),
		Character: Math.Max((tInt32)pos.Col, 0)
	);

	private static tPos
	ToParserPosition(
		tText uri,
		tInt32 line,
		tInt32 character
	) => mTextStream.Pos(
		uri,
		(tNat32)Math.Max(line, 0) + 1,
		(tNat32)Math.Max(character, 0) + 1
	);
	
	private static SpoDocumentSymbol
	ToDocumentSymbol(
		mSPO_Navigation.tDocumentSymbol symbol
	) {
		var Children = new List<SpoDocumentSymbol>();
		foreach (var Child in symbol.Children) {
			Children.Add(ToDocumentSymbol(Child));
		}
		
		return new(
			Name: symbol.Name,
			Detail: symbol.Detail,
			Kind: (tInt32)symbol.Kind,
			Range: ToRange(symbol.Range),
			SelectionRange: ToRange(symbol.SelectionRange),
			Children: Children
		);
	}
	
	private static SpoLocation
	ToLocation(
		mSPO_Navigation.tLocation location
	) => new(
		Uri: location.Uri,
		Range: ToRange(location.Range)
	);
}

internal sealed record
SpoDiagnostic(
	SpoRange Range,
	tInt32 Severity,
	tText Source,
	tText Message
);

internal sealed record
SpoRange(
	SpoPosition Start,
	SpoPosition End
);

internal sealed record
SpoPosition(
	tInt32 Line,
	tInt32 Character
);

internal sealed record
SpoDocumentSymbol(
	tText Name,
	tText Detail,
	tInt32 Kind,
	SpoRange Range,
	SpoRange SelectionRange,
	IReadOnlyList<SpoDocumentSymbol> Children
);

internal sealed record
SpoLocation(
	tText Uri,
	SpoRange Range
);

internal sealed record
SpoRenameTarget(
	SpoRange Range,
	tText Placeholder
);

internal sealed record
SpoTextEdit(
	SpoRange Range,
	tText NewText
);
