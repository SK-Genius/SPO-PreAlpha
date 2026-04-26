using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

internal sealed class
SpoLanguageService {
	public IReadOnlyList<SpoDiagnostic>
	GetDiagnostics(
		string uri,
		string text
	) {
		try {
			var diagnostics = new List<SpoDiagnostic>();
			foreach (var diagnostic in mSPO_Diagnostics.GetModuleDiagnostics(text, uri, _ => {})) {
				diagnostics.Add(
					new(
						Range: ToRange(diagnostic.Pos),
						Severity: (int)diagnostic.Severity,
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
					Severity: (int)mSPO_Diagnostics.tDiagnosticSeverity.Error,
					Source: "lsp",
					Message: "internal server error: " + exception.Message
				)
			];
		}
	}
	
	public IReadOnlyList<SpoDocumentSymbol>
	GetDocumentSymbols(
		string uri,
		string text
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
		string uri,
		string text,
		int line,
		int character
	) {
		try {
			var Definition = mSPO_Navigation.GetDefinition(
				text,
				uri,
				mTextStream.Pos(
					uri,
					(uint)Math.Max(line, 0) + 1,
					(uint)Math.Max(character, 0) + 1
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
	
	static SpoRange
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
	
	static SpoPosition
	ToStartPosition(
		tPos pos
	) => new(
		Line: Math.Max((int)pos.Row - 1, 0),
		Character: Math.Max((int)pos.Col - 1, 0)
	);
	
	static SpoPosition
	ToEndPosition(
		tPos pos
	) => new(
		Line: Math.Max((int)pos.Row - 1, 0),
		Character: Math.Max((int)pos.Col, 0)
	);
	
	static SpoDocumentSymbol
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
			Kind: (int)symbol.Kind,
			Range: ToRange(symbol.Range),
			SelectionRange: ToRange(symbol.SelectionRange),
			Children: Children
		);
	}
	
	static SpoLocation
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
	int Severity,
	string Source,
	string Message
);

internal sealed record
SpoRange(
	SpoPosition Start,
	SpoPosition End
);

internal sealed record
SpoPosition(
	int Line,
	int Character
);

internal sealed record
SpoDocumentSymbol(
	string Name,
	string Detail,
	int Kind,
	SpoRange Range,
	SpoRange SelectionRange,
	IReadOnlyList<SpoDocumentSymbol> Children
);

internal sealed record
SpoLocation(
	string Uri,
	SpoRange Range
);
