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
				mTextStream.Pos(
					uri,
					(tNat32)Math.Max(line, 0) + 1,
					(tNat32)Math.Max(character, 0) + 1
				),
				_ => {}
			);
			return Definition.IsSome(out var Location)
			? ToLocation(Location)
			: null;
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
