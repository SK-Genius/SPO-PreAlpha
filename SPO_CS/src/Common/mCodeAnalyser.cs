#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property LangVersion = "preview"
#:property TargetFramework = "net11.0"
#:property Nullable = "enable"
//#:property OutputType = "Exe"
//#:property PublishAot = "false"
#:include $(MSBuildBinPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll
#:include $(MSBuildBinPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll
#:include $(MSBuildBinPath)/DotnetTools/dotnet-format/Microsoft.CodeAnalysis.Workspaces.dll
#:include $(MSBuildBinPath)/DotnetTools/dotnet-format/System.Composition.AttributedModel.dll

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using tBool = System.Boolean;
using tChar = System.Char;
using tInt32 = System.Int32;
using tText = System.String;

public static class
mCodeAnalyser {
	public static async Task<tInt32>
	Main(
		tText[] aArgs
	) {
		var Check = false;
		var Symbols = new tText[] { "DEBUG", "TRACE", "noMY_TRACE", "noDISABLE_DEBUGGER_HIDDEN" };
		var Paths = ImmutableArray<tText>.Empty;
		
		foreach (var Arg in aArgs) {
			if (Arg is "--help" or "-h") {
				System.Console.WriteLine(
					"""
					dotnet run --file SPO_CS/src/Common/mCodeAnalyser.cs -- [--check] [--define=SYMBOLS] [paths...]
					Without paths, formats SPO_CS/src and SPO. Paths may be C# files or directories.
					--check reports pending changes without writing files; exit code 1 means changes are needed.
					--define replaces the default DEBUG,TRACE,noMY_TRACE,noDISABLE_DEBUGGER_HIDDEN symbols.
					"""
				);
				
				return 0;
			} else if (Arg == "--check") {
				Check = true;
			} else if (Arg.StartsWith("--define=", System.StringComparison.Ordinal)) {
				Symbols = Arg["--define=".Length..].Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
			} else if (Arg.StartsWith("-", System.StringComparison.Ordinal)) {
				System.Console.Error.WriteLine($"Unknown option: {Arg}");
				return 2;
			} else {
				Paths = Paths.Add(Arg);
			}
		}
		
		if (Paths.IsEmpty) {
			var Directory = System.AppContext.GetData("EntryPointFileDirectoryPath") as tText;
			
			var Root = (
				Directory is null
				? System.Environment.CurrentDirectory
				: System.IO.Path.GetFullPath(System.IO.Path.Combine(Directory, "../../.."))
			);
			
			Paths = [System.IO.Path.Combine(Root, "SPO_CS/src"), System.IO.Path.Combine(Root, "SPO")];
		}
		
		var Files = ImmutableSortedSet.Create<tText>(System.StringComparer.OrdinalIgnoreCase);
		
		foreach (var Path in Paths) {
			if (System.IO.File.Exists(Path) && System.IO.Path.GetExtension(Path).Equals(".cs", System.StringComparison.OrdinalIgnoreCase)) {
				Files = Files.Add(System.IO.Path.GetFullPath(Path));
			} else if (System.IO.Directory.Exists(Path)) {
				foreach (var File in System.IO.Directory.EnumerateFiles(Path, "*.cs", System.IO.SearchOption.AllDirectories)) {
					Files = Files.Add(System.IO.Path.GetFullPath(File));
				}
			} else {
				System.Console.Error.WriteLine($"Not a C# file or directory: {Path}");
				return 2;
			}
		}
		
		var Options = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: Symbols);
		var Ids = ImmutableHashSet.Create("SPO001", "SPO002", "SPO003", "SPO004", "SPO005", "SPO006", "SPO007", "SPO008", "SPO009");
		var Changed = 0;
		
		foreach (var File in Files) {
			try {
				var Bytes = await System.IO.File.ReadAllBytesAsync(File).ConfigureAwait(false);
				using var Stream = new System.IO.MemoryStream(Bytes, writable: false);
				var Source = SourceText.From(Stream, new System.Text.UTF8Encoding(false));
				var Tree = CSharpSyntaxTree.ParseText(Source, Options, path: File);
				var Fixed = await Tree.FixAsync(Ids).ConfigureAwait(false);
				var Text = await Fixed.GetTextAsync().ConfigureAwait(false);
				
				if (Text.ContentEquals(Source) && Equals(Text.Encoding, Source.Encoding)) {
					continue;
				}
				
				if (!Check) {
					var Current = await System.IO.File.ReadAllBytesAsync(File).ConfigureAwait(false);
					
					if (!System.Linq.Enumerable.SequenceEqual(Current, Bytes)) {
						throw new System.IO.IOException("The file changed while formatting.");
					}
					
					await System.IO.File.WriteAllTextAsync(
						File,
						Text.ToString(),
						Text.Encoding ?? new System.Text.UTF8Encoding(false)
					).ConfigureAwait(false);
				}
				
				Changed += 1;
				System.Console.WriteLine($"{(Check ? "Would format" : "Formatted")}: {File}");
			} catch (System.Exception Error) {
				System.Console.Error.WriteLine($"{File}: {Error.Message}");
				return 2;
			}
		}
		
		System.Console.WriteLine($"{Changed} of {Files.Count} files {(Check ? "need formatting" : "formatted")}.");
		return Check && Changed > 0 ? 1 : 0;
	}
	
	private static readonly DiagnosticDescriptor cOpening = new(
		"SPO001",
		"Multiline opening delimiter",
		"Only whitespace and comments may follow '{0}' when its closing delimiter is on another line",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cClosing = new(
		"SPO002",
		"Multiline closing delimiter",
		"Closing delimiter '{0}' must be the first non-whitespace text on its line",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cIndentation = new(
		"SPO003",
		"Delimiter indentation",
		"Use exactly {0} tab(s) of indentation, one per enclosing multiline delimiter",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cSeparator = new(
		"SPO004",
		"Multiline separator",
		"Only whitespace and comments may follow separator '{0}' unless its enclosing delimiters are on the same line",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cDeclaration = new(
		"SPO005",
		"Single variable declaration",
		"Declare each variable or constant separately",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cStatementSpacing = new(
		"SPO006",
		"Multiline statement spacing",
		"Separate multiline statements from adjacent statements with a blank line",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cFieldSpacing = new(
		"SPO007",
		"Multiline field spacing",
		"Separate multiline fields from adjacent members with a blank line",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cFileEnd = new(
		"SPO008",
		"No final newline or blank lines",
		"Remove the final newline and trailing blank lines",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	private static readonly DiagnosticDescriptor cBom = new(
		"SPO009",
		"No byte order mark",
		"Save the file as UTF-8 without a byte order mark (BOM)",
		"Formatting",
		DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);
	
	public static async Task<SyntaxTree>
	FixAsync(
		this SyntaxTree aTree,
		ImmutableHashSet<tText> aDiagnosticIds,
		AnalyzerOptions? aOptions = null,
		CSharpCompilationOptions? aCompilationOptions = null,
		CancellationToken aCancellationToken = default
	) {
		// Declaration and spacing fixes also format the code they create.
		var DiagnosticIds = aDiagnosticIds;
		
		if (DiagnosticIds.Overlaps(["SPO005", "SPO006", "SPO007"])) {
			DiagnosticIds = DiagnosticIds.Union(["SPO001", "SPO002", "SPO003", "SPO004"]);
		} else if (DiagnosticIds.Overlaps(["SPO001", "SPO002", "SPO004"])) {
			DiagnosticIds = DiagnosticIds.Add("SPO003");
		}
		
		var Tree = aTree;
		var Source = await Tree.GetTextAsync(aCancellationToken).ConfigureAwait(false);
		var NewLine = Source.ToString().Contains("\r\n", System.StringComparison.Ordinal) ? "\r\n" : "\n";
		
		while (true) {
			aCancellationToken.ThrowIfCancellationRequested();
			var Root = await Tree.GetRootAsync(aCancellationToken).ConfigureAwait(false);
			
			var Diagnostics = await CSharpCompilation.Create(
				"SPO_CodeFix",
				[Tree],
				options: aCompilationOptions
			).WithAnalyzers(
				[new tAnalyzer()],
				aOptions
			).GetAnalyzerDiagnosticsAsync(aCancellationToken).ConfigureAwait(false);
			
			var ExpectedRoot = Root;
			
			if (DiagnosticIds.Contains("SPO005")) {
				foreach (var Diagnostic in Diagnostics) {
					if (Diagnostic.Id == "SPO005") {
						var Declaration = (VariableDeclarationSyntax)Root.FindToken(Diagnostic.Location.SourceSpan.Start).Parent!;
						ExpectedRoot = SplitDeclaration(Root, Declaration, Source, NewLine);
						break;
					}
				}
			}
			
			var Changes = ImmutableSortedDictionary<tInt32, TextChange>.Empty;
			
			foreach (var Diagnostic in Diagnostics) {
				if (
					ExpectedRoot != Root || !DiagnosticIds.Contains(Diagnostic.Id) ||
					Diagnostic.Id is not ("SPO001" or "SPO002" or "SPO004")
				) {
					continue;
				}
				
				var Token = Root.FindToken(Diagnostic.Location.SourceSpan.Start);
				
				if (Diagnostic.Id is "SPO001" or "SPO004") {
					Token = Token.GetNextToken(includeSkipped: true);
				}
				
				var Line = Source.Lines.GetLineFromPosition(Token.SpanStart);
				var Start = Token.SpanStart;
				
				while (Start > Line.Start && tChar.IsWhiteSpace(Source[Start - 1])) {
					Start -= 1;
				}
				
				Changes = Changes.SetItem(Start, new TextChange(TextSpan.FromBounds(Start, Token.SpanStart), NewLine));
			}
			
			// Structural edits change line depths, so recompute diagnostics before fixing indentation.
			if (ExpectedRoot == Root && Changes.IsEmpty && DiagnosticIds.Contains("SPO003")) {
				foreach (var Diagnostic in Diagnostics) {
					if (Diagnostic.Id != "SPO003") {
						continue;
					}
					
					var Line = Source.Lines.GetLineFromPosition(Diagnostic.Location.SourceSpan.Start);
					var End = Line.Start;
					
					while (End < Line.End && tChar.IsWhiteSpace(Source[End])) {
						End += 1;
					}
					
					var Depth = tInt32.Parse(Diagnostic.Properties["Indentation"]!, System.Globalization.CultureInfo.InvariantCulture);
					
					Changes = Changes.SetItem(
						Line.Start,
						new TextChange(TextSpan.FromBounds(Line.Start, End), new tText('\t', Depth))
					);
				}
			}
			
			if (ExpectedRoot == Root && Changes.IsEmpty) {
				foreach (var Diagnostic in Diagnostics) {
					if (!DiagnosticIds.Contains(Diagnostic.Id) || Diagnostic.Id is not ("SPO006" or "SPO007")) {
						continue;
					}
					
					var Start = tInt32.Parse(Diagnostic.Properties["Start"]!, System.Globalization.CultureInfo.InvariantCulture);
					var Length = tInt32.Parse(Diagnostic.Properties["Length"]!, System.Globalization.CultureInfo.InvariantCulture);
					var Whitespace = Diagnostic.Properties["Whitespace"]!;
					
					var Text = (
						Diagnostic.Properties["Inline"] == "true"
						? NewLine + Whitespace + NewLine + Whitespace
						: Whitespace + NewLine
					);
					
					Changes = Changes.SetItem(Start, new TextChange(new TextSpan(Start, Length), Text));
				}
			}
			
			var Encoding = Source.Encoding;
			
			if (ExpectedRoot == Root && Changes.IsEmpty) {
				foreach (var Diagnostic in Diagnostics) {
					if (!DiagnosticIds.Contains(Diagnostic.Id)) {
						continue;
					}
					
					if (Diagnostic.Id == "SPO008") {
						var Span = Diagnostic.Location.SourceSpan;
						Changes = Changes.SetItem(Span.Start, new TextChange(Span, ""));
					} else if (Diagnostic.Id == "SPO009") {
						Encoding = new System.Text.UTF8Encoding(false);
						var End = 0;
						
						while (End < Source.Length && Source[End] == '\uFEFF') {
							End += 1;
						}
						
						if (End > 0) {
							Changes = Changes.SetItem(0, new TextChange(new TextSpan(0, End), ""));
						}
					}
				}
			}
			
			if (ExpectedRoot == Root && Changes.IsEmpty && Encoding == Source.Encoding) {
				return Tree;
			}
			
			Source = (
				ExpectedRoot == Root
				? Source.WithChanges(Changes.Values)
				: SourceText.From(ExpectedRoot.ToFullString(), Source.Encoding, Source.ChecksumAlgorithm)
			);
			
			if (Encoding != Source.Encoding) {
				Source = SourceText.From(Source.ToString(), Encoding, Source.ChecksumAlgorithm);
			}
			
			var UpdatedTree = Tree.WithChangedText(Source);
			var UpdatedRoot = await UpdatedTree.GetRootAsync(aCancellationToken).ConfigureAwait(false);
			using var Before = ExpectedRoot.DescendantTokens().GetEnumerator();
			using var After = UpdatedRoot.DescendantTokens().GetEnumerator();
			
			while (Before.MoveNext()) {
				if (
					!After.MoveNext() || Before.Current.RawKind != After.Current.RawKind ||
					Before.Current.Text != After.Current.Text || Before.Current.ValueText != After.Current.ValueText
				) {
					throw new System.InvalidOperationException("The SPO formatting fix would change tokens or string values.");
				}
			}
			
			if (After.MoveNext()) {
				throw new System.InvalidOperationException("The SPO formatting fix would add C# tokens.");
			}
			
			if (
				!SyntaxFactory.AreEquivalent(
					CSharpSyntaxTree.ParseText(ExpectedRoot.ToFullString(), (CSharpParseOptions)Tree.Options).GetRoot(aCancellationToken),
					CSharpSyntaxTree.ParseText(UpdatedRoot.ToFullString(), (CSharpParseOptions)Tree.Options).GetRoot(aCancellationToken)
				)
			) {
				throw new System.InvalidOperationException("The SPO formatting fix would change C# syntax.");
			}
			
			Tree = UpdatedTree;
		}
		
		static SyntaxNode
		SplitDeclaration(
			SyntaxNode aRoot,
			VariableDeclarationSyntax aDeclaration,
			SourceText aSource,
			tText aNewLine
		) {
			var Parent = aDeclaration.Parent!;
			
			var HeaderEnd = Parent switch {
				ForStatementSyntax Header => Header.CloseParenToken.SpanStart,
				UsingStatementSyntax Header => Header.CloseParenToken.SpanStart,
				FixedStatementSyntax Header => Header.CloseParenToken.SpanStart,
				_ => aDeclaration.Span.End
			};
			
			var HeaderSpan = TextSpan.FromBounds(Parent.SpanStart, HeaderEnd);
			
			foreach (var Trivia in Parent.DescendantTrivia()) {
				if (Trivia.IsDirective && HeaderSpan.Contains(Trivia.Span)) {
					throw new System.InvalidOperationException("Split declarations with interleaved preprocessor directives manually.");
				}
			}
			
			var Variables = aDeclaration.Variables;
			var Parts = ImmutableArray.CreateBuilder<VariableDeclarationSyntax>();
			
			for (var Index = 0; Index < Variables.Count; Index += 1) {
				var Leading = aDeclaration.GetLeadingTrivia();
				
				if (Parent is ForStatementSyntax or UsingStatementSyntax or FixedStatementSyntax) {
					Leading = Variables[Index].GetLeadingTrivia();
					
					if (Index == 0) {
						Leading = aDeclaration.GetLeadingTrivia().AddRange(Leading);
					} else {
						var Separator = Variables.GetSeparator(Index - 1);
						Leading = Separator.LeadingTrivia.AddRange(Separator.TrailingTrivia).AddRange(Leading);
					}
					
					while (Leading.Count > 0 && Leading[0].IsKind(SyntaxKind.WhitespaceTrivia)) {
						Leading = Leading.RemoveAt(0);
					}
				}
				
				Parts.Add(
					aDeclaration.WithVariables(
						SyntaxFactory.SingletonSeparatedList(Variables[Index].WithoutLeadingTrivia())
					).WithLeadingTrivia(Leading)
				);
			}
			
			if (aDeclaration.Parent is ForStatementSyntax For) {
				var Statements = ImmutableArray.CreateBuilder<StatementSyntax>();
				
				foreach (var Part in Parts) {
					Statements.Add(SyntaxFactory.LocalDeclarationStatement(Part));
				}
				
				Statements.Add(For.WithDeclaration(null).WithoutLeadingTrivia());
				
				var Block = SyntaxFactory.Block(Statements).WithOpenBraceToken(
					SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(SyntaxFactory.EndOfLine(aNewLine))
				).WithLeadingTrivia(For.GetLeadingTrivia());
				
				return aRoot.ReplaceNode(For, Block);
			}
			
			if (aDeclaration.Parent is UsingStatementSyntax Using) {
				var Body = Using.Statement;
				
				for (var Index = Parts.Count - 1; Index >= 0; Index -= 1) {
					Body = Using.WithDeclaration(Parts[Index]).WithStatement(Body).WithoutLeadingTrivia();
				}
				
				return aRoot.ReplaceNode(Using, Body.WithLeadingTrivia(Using.GetLeadingTrivia()));
			}
			
			if (aDeclaration.Parent is FixedStatementSyntax Fixed) {
				var Body = Fixed.Statement;
				
				for (var Index = Parts.Count - 1; Index >= 0; Index -= 1) {
					Body = Fixed.WithDeclaration(Parts[Index]).WithStatement(Body).WithoutLeadingTrivia();
				}
				
				return aRoot.ReplaceNode(Fixed, Body.WithLeadingTrivia(Fixed.GetLeadingTrivia()));
			}
			
			var Nodes = ImmutableArray.CreateBuilder<SyntaxNode>();
			
			for (var Index = 0; Index < Parts.Count; Index += 1) {
				var Semicolon = (
					Index + 1 < Parts.Count
					? SyntaxFactory.Token(
						Variables.GetSeparator(Index).LeadingTrivia,
						SyntaxKind.SemicolonToken,
						Variables.GetSeparator(Index).TrailingTrivia
					)
					: Parent.GetLastToken()
				);
				
				SyntaxNode Single = Parent switch {
					LocalDeclarationStatementSyntax Local => Local.WithDeclaration(Parts[Index]).WithSemicolonToken(Semicolon),
					FieldDeclarationSyntax Field => Field.WithDeclaration(Parts[Index]).WithSemicolonToken(Semicolon),
					EventFieldDeclarationSyntax Event => Event.WithDeclaration(Parts[Index]).WithSemicolonToken(Semicolon),
					_ => throw new System.InvalidOperationException("Unsupported variable declaration.")
				};
				
				Single = Single.WithLeadingTrivia(
					Index == 0
					? Parent.GetLeadingTrivia().AddRange(Variables[Index].GetLeadingTrivia())
					: Variables[Index].GetLeadingTrivia()
				);
				
				if (
					Index + 1 < Parts.Count &&
					aSource.Lines.GetLineFromPosition(Variables.GetSeparator(Index).SpanStart).LineNumber ==
					aSource.Lines.GetLineFromPosition(Variables[Index + 1].SpanStart).LineNumber
				) {
					var Trailing = Single.GetTrailingTrivia();
					
					while (Trailing.Count > 0 && Trailing[^1].IsKind(SyntaxKind.WhitespaceTrivia)) {
						Trailing = Trailing.RemoveAt(Trailing.Count - 1);
					}
					
					Single = Single.WithTrailingTrivia(Trailing.Add(SyntaxFactory.EndOfLine(aNewLine)));
				}
				
				Nodes.Add(Parent.Parent is GlobalStatementSyntax Global ? Global.WithStatement((StatementSyntax)Single) : Single);
			}
			
			return aRoot.ReplaceNode(Parent.Parent is GlobalStatementSyntax Wrapper ? Wrapper : Parent, Nodes);
		}
	}
	
	private static async Task<Document>
	FixDocumentAsync(
		this Document aDocument,
		ImmutableHashSet<tText> aDiagnosticIds,
		CancellationToken aCancellationToken
	) {
		var Tree = (await aDocument.GetSyntaxTreeAsync(aCancellationToken).ConfigureAwait(false))!;
		
		var Fixed = await Tree.FixAsync(
			aDiagnosticIds,
			aDocument.Project.AnalyzerOptions,
			aDocument.Project.CompilationOptions as CSharpCompilationOptions,
			aCancellationToken
		).ConfigureAwait(false);
		
		return aDocument.WithText(await Fixed.GetTextAsync(aCancellationToken).ConfigureAwait(false));
	}
	
	[ExportCodeFixProvider(LanguageNames.CSharp)]
	public sealed class
	tCodeFixProvider : CodeFixProvider {
		public override ImmutableArray<tText>
		FixableDiagnosticIds => ["SPO001", "SPO002", "SPO003", "SPO004", "SPO005", "SPO006", "SPO007", "SPO008", "SPO009"];
		
		public override FixAllProvider
		GetFixAllProvider(
		) => FixAllProvider.Create(
			async (aContext, aDocument, _) => await aDocument.FixDocumentAsync(
				ImmutableHashSet.CreateRange(aContext.DiagnosticIds),
				aContext.CancellationToken
			).ConfigureAwait(false)
		);
		
		public override Task
		RegisterCodeFixesAsync(
			CodeFixContext aContext
		) {
			var Ids = ImmutableHashSet<tText>.Empty;
			
			foreach (var Diagnostic in aContext.Diagnostics) {
				Ids = Ids.Add(Diagnostic.Id);
			}
			
			aContext.RegisterCodeFix(
				CodeAction.Create(
					"Fix SPO formatting in document",
					aCancellationToken => aContext.Document.FixDocumentAsync(Ids, aCancellationToken),
					equivalenceKey: "SPOFormatting"
				),
				aContext.Diagnostics
			);
			
			return Task.CompletedTask;
		}
	}
	
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public sealed class
	tAnalyzer : DiagnosticAnalyzer {
		public override ImmutableArray<DiagnosticDescriptor>
		SupportedDiagnostics => [cOpening, cClosing, cIndentation, cSeparator, cDeclaration, cStatementSpacing, cFieldSpacing, cFileEnd, cBom];
		
		public override void
		Initialize(
			AnalysisContext aContext
		) {
			aContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			aContext.EnableConcurrentExecution();
			aContext.RegisterSyntaxTreeAction(Analyse);
			
			static void
			Analyse(
				SyntaxTreeAnalysisContext aContext
			) {
				var Source = aContext.Tree.GetText(aContext.CancellationToken);
				var Root = aContext.Tree.GetRoot(aContext.CancellationToken);
				var Depths = new tInt32[Source.Lines.Count + 1];
				var Delimiters = ImmutableStack<(tInt32 End, tBool IsMultiline)>.Empty;
				
				var End = Source.Length;
				var LastTokenEnd = Root.GetLastToken(includeZeroWidth: false).Span.End;
				
				while (End > LastTokenEnd && tChar.IsWhiteSpace(Source[End - 1])) {
					End -= 1;
				}
				
				if (End < Source.Length && (End == 0 || End < Source.Lines[^1].Start)) {
					aContext.ReportDiagnostic(Diagnostic.Create(cFileEnd, Location.Create(aContext.Tree, TextSpan.FromBounds(End, Source.Length))));
				}
				
				if ((Source.Encoding?.GetPreamble().Length ?? 0) > 0 || (Source.Length > 0 && Source[0] == '\uFEFF')) {
					aContext.ReportDiagnostic(Diagnostic.Create(cBom, Location.Create(aContext.Tree, new TextSpan(0, 0))));
				}
				
				foreach (var Token in Root.DescendantTokens()) {
					aContext.CancellationToken.ThrowIfCancellationRequested();
					
					if (Token.IsMissing) {
						continue;
					}
					
					while (!Delimiters.IsEmpty && Delimiters.Peek().End <= Token.SpanStart) {
						Delimiters = Delimiters.Pop();
					}
					
					if (Token.Kind() is SyntaxKind.CommaToken or SyntaxKind.SemicolonToken) {
						var Next = Token.GetNextToken(includeSkipped: true);
						
						if (
							(Delimiters.IsEmpty || Delimiters.Peek().IsMultiline) &&
							Next.RawKind != 0 &&
							Next.SpanStart < Source.Lines.GetLineFromPosition(Token.SpanStart).End
						) {
							aContext.ReportDiagnostic(Diagnostic.Create(cSeparator, Token.GetLocation(), Token.Text));
						}
					}
					
					var ClosingKind = Token.Kind() switch {
						SyntaxKind.OpenParenToken => SyntaxKind.CloseParenToken,
						SyntaxKind.OpenBracketToken => SyntaxKind.CloseBracketToken,
						SyntaxKind.OpenBraceToken => SyntaxKind.CloseBraceToken,
						SyntaxKind.LessThanToken when Token.Parent is (
							TypeArgumentListSyntax or TypeParameterListSyntax or FunctionPointerParameterListSyntax
						) => SyntaxKind.GreaterThanToken,
						_ => SyntaxKind.None
					};
					
					if (ClosingKind == SyntaxKind.None) {
						continue;
					}
					
					// A syntax node owns both delimiters; operators and literal text cannot form pairs.
					foreach (var Closing in Token.Parent!.ChildTokens()) {
						if (!Closing.IsKind(ClosingKind) || Closing.IsMissing || Closing.SpanStart < Token.Span.End) {
							continue;
						}
						
						var OpeningLine = Source.Lines.GetLineFromPosition(Token.SpanStart);
						var ClosingLine = Source.Lines.GetLineFromPosition(Closing.SpanStart);
						var IsMultiline = OpeningLine.LineNumber != ClosingLine.LineNumber;
						Delimiters = Delimiters.Push((Closing.SpanStart, IsMultiline));
						
						if (!IsMultiline) {
							break;
						}
						
						if (Token.GetNextToken(includeSkipped: true).SpanStart < OpeningLine.End) {
							aContext.ReportDiagnostic(Diagnostic.Create(cOpening, Token.GetLocation(), Token.Text));
						}
						
						var ClosingStartsLine = true;
						
						for (var Position = ClosingLine.Start; Position < Closing.SpanStart; Position += 1) {
							if (!tChar.IsWhiteSpace(Source[Position])) {
								ClosingStartsLine = false;
								break;
							}
						}
						
						if (!ClosingStartsLine) {
							aContext.ReportDiagnostic(Diagnostic.Create(cClosing, Closing.GetLocation(), Closing.Text));
						}
						
						Depths[OpeningLine.LineNumber + 1] += 1;
						// A misplaced closing delimiter still leaves preceding code inside the pair.
						Depths[ClosingLine.LineNumber + (ClosingStartsLine ? 0 : 1)] -= 1;
						break;
					}
				}
				
				var Depth = 0;
				
				foreach (var Line in Source.Lines) {
					aContext.CancellationToken.ThrowIfCancellationRequested();
					Depth += Depths[Line.LineNumber];
					Depths[Line.LineNumber] = Depth;
					var Start = Line.Start;
					
					while (Start < Line.End && tChar.IsWhiteSpace(Source[Start])) {
						Start += 1;
					}
					
					if (Start == Line.End) {
						continue;
					}
					
					var Trivia = Root.FindTrivia(Start);
					
					if (Trivia.RawKind != 0) {
						// Check comment starts and /// lines, preserving comment bodies and inactive source.
						if (Trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)) {
							if (
								Start + 2 >= Line.End || Source[Start] != '/' ||
								Source[Start + 1] != '/' || Source[Start + 2] != '/'
							) {
								continue;
							}
						} else if (
							Trivia.SpanStart != Start || Trivia.Kind() is not (
								SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or
								SyntaxKind.MultiLineDocumentationCommentTrivia
							)
						) {
							continue;
						}
					} else {
						var Token = Root.FindToken(Start);
						
						// Whitespace before a line-leading interpolation belongs to the literal, too.
						if (
							Token.SpanStart != Start ||
							(Token.IsKind(SyntaxKind.OpenBraceToken) && Token.Parent is InterpolationSyntax) ||
							Token.Kind() is (
								SyntaxKind.InterpolatedStringTextToken or SyntaxKind.InterpolatedStringEndToken or
								SyntaxKind.InterpolatedRawStringEndToken
							)
						) {
							continue;
						}
					}
					
					var HasCorrectIndentation = Start - Line.Start == Depth;
					
					for (var Position = Line.Start; HasCorrectIndentation && Position < Start; Position += 1) {
						HasCorrectIndentation = Source[Position] == '\t';
					}
					
					if (!HasCorrectIndentation) {
						aContext.ReportDiagnostic(
							Diagnostic.Create(
								cIndentation,
								Location.Create(
									aContext.Tree,
									TextSpan.FromBounds(Line.Start, System.Math.Max(Line.Start + 1, Start))
								),
								properties: ImmutableDictionary<tText, tText?>.Empty.Add(
									"Indentation",
									Depth.ToString(System.Globalization.CultureInfo.InvariantCulture)
								),
								messageArgs: [Depth]
							)
						);
					}
				}
				
				foreach (var Node in Root.DescendantNodesAndSelf()) {
					aContext.CancellationToken.ThrowIfCancellationRequested();
					
					if (Node is VariableDeclarationSyntax Declaration && Declaration.Variables.Count > 1 && !Declaration.ContainsDiagnostics) {
						aContext.ReportDiagnostic(Diagnostic.Create(cDeclaration, Declaration.Variables.GetSeparator(0).GetLocation()));
					}
					
					var Fields = Node is TypeDeclarationSyntax;
					
					if (!Fields && Node is not (BlockSyntax or SwitchSectionSyntax or CompilationUnitSyntax)) {
						continue;
					}
					
					SyntaxNode? Previous = null;
					
					foreach (var Child in Node.ChildNodes()) {
						SyntaxNode? Current = Fields ? Child as MemberDeclarationSyntax : Child switch {
							GlobalStatementSyntax Global => Global.Statement,
							StatementSyntax Statement => Statement,
							_ => null
						};
						
						if (Current is null || Current.IsMissing || Current.Span.IsEmpty) {
							Previous = null;
							continue;
						}
						
						if (Previous is not null) {
							var PreviousStart = Source.Lines.GetLineFromPosition(Previous.SpanStart).LineNumber;
							var PreviousEnd = Source.Lines.GetLineFromPosition(Previous.Span.End - 1).LineNumber;
							var CurrentStart = Source.Lines.GetLineFromPosition(Current.SpanStart).LineNumber;
							var CurrentEnd = Source.Lines.GetLineFromPosition(Current.Span.End - 1).LineNumber;
							
							if (
								(PreviousStart != PreviousEnd && (!Fields || Previous is BaseFieldDeclarationSyntax)) ||
								(CurrentStart != CurrentEnd && (!Fields || Current is BaseFieldDeclarationSyntax))
							) {
								var HasBlank = false;
								
								for (var Index = PreviousEnd + 1; Index < CurrentStart; Index += 1) {
									var Line = Source.Lines[Index];
									var Position = Line.Start;
									
									while (Position < Line.End && tChar.IsWhiteSpace(Source[Position])) {
										Position += 1;
									}
									
									if (Position == Line.End) {
										HasBlank = true;
										break;
									}
								}
								
								if (!HasBlank) {
									var At = Current.FullSpan.Start;
									var Line = Source.Lines.GetLineFromPosition(At);
									var Start = Line.Start;
									
									while (Start < At && tChar.IsWhiteSpace(Source[Start])) {
										Start += 1;
									}
									
									var Inline = Start < At;
									
									if (Inline) {
										Start = At;
										
										while (Start > Previous.Span.End && tChar.IsWhiteSpace(Source[Start - 1])) {
											Start -= 1;
										}
									} else {
										Start = Line.Start;
										At = Start;
									}
									
									aContext.ReportDiagnostic(
										Diagnostic.Create(
											Fields ? cFieldSpacing : cStatementSpacing,
											Current.GetFirstToken().GetLocation(),
											properties: ImmutableDictionary<tText, tText?>.Empty
											.Add("Start", Start.ToString(System.Globalization.CultureInfo.InvariantCulture))
											.Add("Length", (At - Start).ToString(System.Globalization.CultureInfo.InvariantCulture))
											.Add("Whitespace", new tText('\t', Depths[CurrentStart]))
											.Add("Inline", Inline ? "true" : "false")
										)
									);
								}
							}
						}
						
						Previous = Current;
					}
				}
			}
		}
	}
}