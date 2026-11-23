#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:property PublishAot = "false"
#:include ../_GlobalUsings.cs
#:include $(MSBuildBinPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll
#:include $(MSBuildBinPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll
#:ref mCodeAnalyser.cs
#:ref mStd.cs
#:ref mTest.cs
#:ref mAssert.cs

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

public static class
mCodeAnalyser_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mCodeAnalyser),
		[
			Test(
				"Inline delimiters",
				"""
				class C<T> { int[] A = [1, 2]; void F() { F(); } }
				"""
			),
			Test(
				"Multiline blocks and argument lists",
				"""
				class C {
					void F(
						int X
					) {
						F(
							X
						);
					}
				}
				"""
			),
			Test(
				"Multiline collection",
				"""
				int[] A = [
					1,
					2
				];
				"""
			),
			Test(
				"Multiline indexer",
				"""
				var X = A[
					0
				];
				"""
			),
			Test(
				"Multiline generic arguments",
				"""
				A<
					B,
					C
				> X;
				"""
			),
			Test(
				"Multiline generic parameters",
				"""
				class C<
					T,
					U
				> { }
				"""
			),
			Test(
				"Nested generic arguments",
				"""
				A<
					B<
						C
					>
				> X;
				"""
			),
			Test(
				"Function pointer parameters",
				"""
				unsafe class C {
					delegate*<
						int,
						void
					> F;
				}
				"""
			),
			Test(
				"Comments after opening delimiters",
				"""
				F( /* [] < > */ // )
					1
				);
				"""
			),
			Test(
				"Block comment across lines",
				"""
				F( /* comment
				 ] ) < {
				 */
					1
				);
				"""
			),
			Test(
				"Comments and documentation follow delimiter depth",
				"""
				class C {
					// ( [ < {
					/// <summary>[(])</summary>
					/// More text
					int X;
				}
				"""
			),
			Test(
				"Blank lines",
				"""
				F(
				
					
					1
				);
				"""
			),
			Test(
				"Literal delimiters",
				"""
				var X = "([{< )]}>";
				var Y = '(';
				"""
			),
			Test(
				"Verbatim string content",
				"""
				class C {
					string X = @"text
				   ([{<
				text";
				}
				"""
			),
			Test(
				"Raw string content",
				""""
				class C {
					string X = """
				   ([{<
				   """;
				}
				""""
			),
			Test(
				"Interpolated strings",
				"""
				var X = $"{{ text }} {F(1)}";
				"""
			),
			Test(
				"Interpolated raw string content",
				""""
				class C {
					string X = $"""
				   text {F(1)}
				   """;
				}
				""""
			),
			Test(
				"Raw string indentation before a line-leading interpolation is literal content",
				""""
				if (true) {
					System.Console.WriteLine(
						$"""
							{Value}
						"""
					);
				}
				""""
			),
			Test(
				"Verbatim string indentation before a line-leading interpolation is literal content",
				"""
				class C {
					string X = $@"text
				   {Value}
				text";
				}
				"""
			),
			Test(
				"Raw strings with multiple dollar signs may start lines with interpolations",
				""""
				F(
					$$"""
					   {{Value}}
					"""
				);
				""""
			),
			Test(
				"Code after an interpolated raw string still requires delimiter indentation",
				""""
				F(
					$"""
						{Value}
					"""
				);
					Other();
				"""",
				"SPO003@6:1"
			),
			Test(
				"Code inside a multiline interpolation still requires delimiter indentation",
				""""
				var X = $"""
					{
				Value
				}
				""";
				"""",
				"SPO003@3:1"
			),
			Test(
				"Comparison and shift operators",
				"""
				var X = 1 <
				2;
				var Y = 4 >
				3;
				var Z = 8 >>
				1;
				var W = 1 <<
				2;
				"""
			),
			Test(
				"Unbraced control flow has no indentation",
				"""
				if (true)
				F();
				else
				G();
				"""
			),
			Test(
				"Continuation has no indentation",
				"""
				var X = A
				.Where(
					X => X > 0
				);
				"""
			),
			Test(
				"Closing delimiter may continue an expression",
				"""
				F(
					1
				).G();
				"""
			),
			Test(
				"Else braces",
				"""
				if (true) {
					F();
				} else {
					G();
				}
				"""
			),
			Test(
				"CRLF",
				"""
				F(
					1
				);
				""".ReplaceLineEndings("\r\n")
			),
			Test(
				"Incomplete syntax does not invent a closing delimiter",
				"""
				F(
				1
				"""
			),
			Test(
				"Inactive source and directives",
				"""
				#if false
				    ([{<
				#endif
				F();
				"""
			),
			Test(
				"Inline lists and blocks inside a multiline block allow separators on one line",
				"""
				class C {
					void F() { G(1, 2); G(3, 4); }
				}
				"""
			),
			Test(
				"Nested inline lists use their own delimiters",
				"""
				F(
					G(1, 2),
					(3, 4),
					new[] { 5, 6 }
				);
				"""
			),
			Test(
				"Comments after separators are allowed",
				"""
				F(
					1, // comment
					2, /* comment
				 */
					3
				); // comment
				G();
				"""
			),
			Test(
				"Multiline for headers separate clauses with newlines",
				"""
				for (
					int I = 0;
					I < 3;
					I++
				) {
					F();
					G();
				}
				"""
			),
			Test(
				"Inline for headers inside multiline blocks allow inline semicolons",
				"""
				for (int I = 0; I < 3; I++) {
					F();
				}
				"""
			),
			Test(
				"Separators in literals and comments are ignored",
				"""
				var X = "1, 2; 3"; // , ;
				var Y = ';';
				var Z = $"{Value,10}; text";
				"""
			),
			Test(
				"Comma in a multiline argument list requires a newline",
				"""
				F(
					1, 2
				);
				""",
				"SPO004@2:3"
			),
			Test(
				"Code after a separator comment still requires a newline",
				"""
				F(
					1, /* comment */ 2
				);
				""",
				"SPO004@2:3"
			),
			Test(
				"Comma in a multiline generic list requires a newline",
				"""
				A<
					B, C
				> X;
				""",
				"SPO004@2:3"
			),
			Test(
				"Comma in a multiline collection requires a newline",
				"""
				int[] A = [
					1, 2
				];
				""",
				"SPO004@2:3"
			),
			Test(
				"Declaration commas use the enclosing block",
				"""
				class C {
					int A, B;
				}
				""",
				"SPO004@2:7"
			),
			Test(
				"Statements in a multiline block require newlines after semicolons",
				"""
				if (true) {
					F(); G();
				}
				""",
				"SPO004@2:5"
			),
			Test(
				"Top-level statements require newlines after semicolons",
				"""
				F(); G();
				""",
				"SPO004@1:4"
			),
			Test(
				"Multiline for headers require newlines after semicolons",
				"""
				for (
					int I = 0; I < 3;
					I++
				) { }
				""",
				"SPO004@2:11"
			),
			Test(
				"Leading commas still require a following newline",
				"""
				F(
					1
					,2
				);
				""",
				"SPO004@3:2"
			),
			Test(
				"Trailing commas before multiline closing delimiters require a newline",
				"""
				int[] A = [
					1,];
				""",
				"""
				SPO002@2:4
				SPO004@2:3
				"""
			),
			Test(
				"Semicolons after closing a multiline call use the surrounding scope",
				"""
				F(
					1
				); G();
				""",
				"SPO004@3:2"
			),
			Test(
				"Code after opening parenthesis",
				"""
				F(1,
					2
				);
				""",
				"SPO001@1:2"
			),
			Test(
				"Code after opening brace",
				"""
				class C { int X;
				}
				""",
				"SPO001@1:9"
			),
			Test(
				"Code after opening bracket",
				"""
				int[] A = [1,
					2
				];
				""",
				"SPO001@1:11"
			),
			Test(
				"Code after opening generic delimiter",
				"""
				A<B,
					C
				> X;
				""",
				"SPO001@1:2"
			),
			Test(
				"Code after comment after opening delimiter",
				"""
				F(/* comment */ 1,
					2
				);
				""",
				"SPO001@1:2"
			),
			Test(
				"Closing delimiter after code",
				"""
				F(
					1);
				""",
				"SPO002@2:3"
			),
			Test(
				"Closing delimiter after comment",
				"""
				F(
					1
					/* comment */ );
				""",
				"SPO002@3:16"
			),
			Test(
				"Nested multiline closing delimiters need separate lines",
				"""
				F(
					G(
						1
					));
				""",
				"SPO002@4:3"
			),
			Test(
				"Missing indentation",
				"""
				F(
				1
				);
				""",
				"SPO003@2:1"
			),
			Test(
				"Too much indentation",
				"""
				F(
						1
				);
				""",
				"SPO003@2:1"
			),
			Test(
				"Spaces instead of tabs",
				"""
				F(
				    1
				);
				""",
				"SPO003@2:1"
			),
			Test(
				"Closing delimiter indentation",
				"""
				F(
					1
					);
				""",
				"SPO003@3:1"
			),
			Test(
				"Unbraced control flow cannot add indentation",
				"""
				if (true)
					F();
				""",
				"SPO003@2:1"
			),
			Test(
				"Continuation cannot add indentation",
				"""
				var X = A
					.G();
				""",
				"SPO003@2:1"
			),
			Test(
				"Root declarations cannot add indentation",
				"""
					class C { }
				""",
				"SPO003@1:1"
			),
			Test(
				"Standalone comment indentation",
				"""
				class C {
				// comment
				}
				""",
				"SPO003@2:1"
			),
			Test(
				"Documentation comment indentation",
				"""
				class C {
					/// First
				/// Second
					int X;
				}
				""",
				"SPO003@3:1"
			),
			TestFix(
				"Fix opening delimiters, closing delimiters, separators and indentation together",
				"""
				class C { // comment
				void F() {
				F(1, 2,
				3); G();
				}
				}
				""",
				"""
				class C { // comment
					void F() {
						F(
							1,
							2,
							3
						);
						G();
					}
				}
				"""
			),
			TestFix(
				"Fix preserves comments after opening delimiters and separators",
				"""
				F(/* opening */ 1, /* separator */ 2,
				3/* closing */);
				""",
				"""
				F(/* opening */
					1, /* separator */
					2,
					3/* closing */
				);
				"""
			),
			TestFix(
				"Fix separates nested closing delimiters",
				"""
				F(
				G(
				1));
				""",
				"""
				F(
					G(
						1
					)
				);
				"""
			),
			TestFix(
				"Fix separates nested generic delimiters without changing tokens",
				"""
				A<B<
				C, D>> X;
				""",
				"""
				A<
					B<
						C,
						D
					>
				> X;
				"""
			),
			TestFix(
				"Fix preserves inline nested lists",
				"""
				F(
				G(1, 2), (3, 4)
				);
				""",
				"""
				F(
					G(1, 2),
					(3, 4)
				);
				"""
			),
			TestFix(
				"Fix multiline for headers",
				"""
				for (
				int I = 0; I < 3;
				I++
				) { }
				""",
				"""
				for (
					int I = 0;
					I < 3;
					I++
				) { }
				"""
			),
			TestFix(
				"Fix preserves raw string content and line-leading interpolation whitespace",
				""""
				class C {
				string X = $"""
				   {Value}
				   """;
				}
				"""",
				""""
				class C {
					string X = $"""
				   {Value}
				   """;
				}
				""""
			),
			TestFix(
				"Fix preserves CRLF line endings",
				"""
				F(1,
				2);
				""".ReplaceLineEndings("\r\n"),
				"""
				F(
					1,
					2
				);
				""".ReplaceLineEndings("\r\n")
			),
			TestFix(
				"Fix respects pragma suppression",
				"""
				#pragma warning disable SPO003
					F();
				#pragma warning restore SPO003
					G();
				""",
				"""
				#pragma warning disable SPO003
					F();
				#pragma warning restore SPO003
				G();
				"""
			),
			TestFix(
				"A structural fix selected alone also indents the new lines",
				"""
				F(1,
					2
				);
				""",
				"""
				F(
					1,
					2
				);
				""",
				aOnlyDiagnostic: "SPO001"
			),
			TestFix(
				"Fix respects the selected diagnostic ID",
				"""
				F(
				    1, 2
				);
				""",
				"""
				F(
					1, 2
				);
				""",
				aOnlyDiagnostic: "SPO003"
			),
			Test(
				"Multiple constants require separate declarations",
				"""
				const int A = 0, B = 258;
				""",
				"SPO005@1:16",
				aDiagnosticId: "SPO005"
			),
			Test(
				"Multiple fields require separate declarations",
				"""
				class C {
					int A, B;
				}
				""",
				"SPO005@2:7",
				aDiagnosticId: "SPO005"
			),
			Test(
				"Multiple for variables require separate declarations",
				"""
				for (int I = 0, J = 1; I < J; I++) { }
				""",
				"SPO005@1:15",
				aDiagnosticId: "SPO005"
			),
			Test(
				"Deconstruction remains one declaration",
				"""
				var (A, B) = (1, 2);
				""",
				aDiagnosticId: "SPO005"
			),
			Test(
				"Only statement needs no surrounding blank lines",
				"""
				F(
					1
				);
				""",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Multiline first statement requires a following blank line",
				"""
				F(
					1
				);
				G();
				""",
				"SPO006@4:1",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Multiline last statement requires a preceding blank line",
				"""
				F();
				G(
					1
				);
				""",
				"SPO006@2:1",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Adjacent multiline statements report their shared boundary once",
				"""
				F(
					1
				);
				G(
					2
				);
				""",
				"SPO006@4:1",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Indented blank lines satisfy statement spacing",
				"""
				{
					F(
						1
					);
					
					G();
				}
				""",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Comment lines do not replace blank lines",
				"""
				F(
					1
				);
				// G comment
				G();
				""",
				"SPO006@5:1",
				aDiagnosticId: "SPO006"
			),
			Test(
				"If else and try catch finally form whole statements",
				"""
				if (true) {
					F();
				} else {
					G();
				}
				
				try {
					F();
				} catch {
					G();
				} finally {
					H();
				}
				""",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Switch sections have independent statement lists",
				"""
				switch (X) {
					case 0:
					F(
						1
					);
					break;
					case 1:
					G();
					break;
				}
				""",
				"SPO006@6:2",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Statement rule ignores member declarations",
				"""
				class C {
					int X = F(
						1
					);
					int Y;
					void F() {
						G();
					}
					void G() { }
				}
				""",
				aDiagnosticId: "SPO006"
			),
			Test(
				"Field spacing has its own diagnostic",
				"""
				class C {
					int A;
					int B = F(
						1
					);
					int D;
				}
				""",
				"SPO007@3:2\nSPO007@6:2",
				aDiagnosticId: "SPO007"
			),
			Test(
				"Only field needs no surrounding blank lines",
				"""
				class C {
					int A = F(
						1
					);
				}
				""",
				aDiagnosticId: "SPO007"
			),
			Test(
				"Methods do not trigger the field rule",
				"""
				class C {
					int X;
					void F() {
						G();
					}
					void G() { }
					int Y;
				}
				""",
				aDiagnosticId: "SPO007"
			),
			Test(
				"A multiline field requires a gap to an adjacent method",
				"""
				class C {
					int X = F(
						1
					);
					void F() { }
				}
				""",
				"SPO007@5:2",
				aDiagnosticId: "SPO007"
			),
			TestFix(
				"Split constants as requested",
				"""
				const tNat32 cWaitSignaled = 0, cWaitTimeout = 258;
				""",
				"""
				const tNat32 cWaitSignaled = 0;
				const tNat32 cWaitTimeout = 258;
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split multiline constant lists without moving indentation into the type",
				"""
				class C {
					void F() {
						const int A = 0,
						B = 258;
					}
				}
				""",
				"""
				class C {
					void F() {
						const int A = 0;
						const int B = 258;
					}
				}
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split fields with attributes and modifiers",
				"""
				class C {
					[System.Obsolete] public static readonly int A = F(), B = G();
				}
				""",
				"""
				class C {
					[System.Obsolete] public static readonly int A = F();
					[System.Obsolete] public static readonly int B = G();
				}
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split event fields",
				"""
				class C {
					event System.Action A, B;
				}
				""",
				"""
				class C {
					event System.Action A;
					event System.Action B;
				}
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split declarations preserving initializer order and comments",
				"""
				// Both variables
				int A = F(), /* separator */ B = G(A); // Last variable
				""",
				"""
				// Both variables
				int A = F(); /* separator */
				int B = G(A); // Last variable
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split declarations within conditional compilation",
				"""
				#if true
				int A = 1, B = 2;
				#endif
				""",
				"""
				#if true
				int A = 1;
				int B = 2;
				#endif
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split using declarations",
				"""
				using R A = F(), B = G();
				""",
				"""
				using R A = F();
				using R B = G();
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split for declarations within their original scope",
				"""
				for (int I = 0, J = 1; I < J; I++) { }
				""",
				"""
				{
					int I = 0;
					int J = 1;
					for (; I < J; I++) { }
				}
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split using statements through ordered nesting",
				"""
				using (R A = F(), B = G()) { }
				""",
				"""
				using (R A = F()) using (R B = G()) { }
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split await using statements through ordered nesting",
				"""
				await using (R A = F(), B = G()) { }
				""",
				"""
				await using (R A = F()) await using (R B = G()) { }
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Split fixed declarations through ordered nesting",
				"""
				unsafe { fixed (int* A = P, B = Q) { } }
				""",
				"""
				unsafe { fixed (int* A = P) fixed (int* B = Q) { } }
				""",
				aOnlyDiagnostic: "SPO005"
			),
			TestFix(
				"Statement fix adds gaps before and after a middle multiline statement",
				"""
				{
					F();
					G(
						1
					);
					H();
				}
				""",
				"""
				{
					F();
					
					G(
						1
					);
					
					H();
				}
				""",
				aOnlyDiagnostic: "SPO006"
			),
			TestFix(
				"Statement fix keeps leading comments with their statement",
				"""
				F(
					1
				); // F comment
				// G comment
				G();
				""",
				"""
				F(
					1
				); // F comment
				
				// G comment
				G();
				""",
				aOnlyDiagnostic: "SPO006"
			),
			TestFix(
				"Statement fix handles an inline boundary",
				"""
				F(); G(
					1
				);
				""",
				"""
				F();
				
				G(
					1
				);
				""",
				aOnlyDiagnostic: "SPO006"
			),
			TestFix(
				"Statement fix preserves multiline raw string values and indentation",
				""""
				{
					var Text = """
				   ([{<
				    literal indentation
				   """;
					F();
				}
				"""",
				""""
				{
					var Text = """
				   ([{<
				    literal indentation
				   """;
					
					F();
				}
				"""",
				aOnlyDiagnostic: "SPO006"
			),
			TestFix(
				"Field fix adds gaps separately from statement spacing",
				"""
				class C {
					int A;
					int B = F(
						1
					);
					int D;
				}
				""",
				"""
				class C {
					int A;
					
					int B = F(
						1
					);
					
					int D;
				}
				""",
				aOnlyDiagnostic: "SPO007"
			),
			TestExecution(
				"Splitting preserves field and local initialization order",
				"""
				public static class C {
					static int Log;
					static readonly int A = Next(), B = Next();
					static int Next() => ++Log;
					public static int Run() {
						int X = Next(), Y = Next() + X;
						return A * 1000 + B * 100 + X * 10 + Y;
					}
				}
				""",
				1237
			),
			TestExecution(
				"Splitting preserves for variable captures",
				"""
				public static class C {
					public static int Run() {
						var F = new System.Func<int>[2];
						for (int I = 0, J = 10; I < 2; I++) F[I] = () => I + J;
						return F[0]() + F[1]();
					}
				}
				""",
				24
			),
			TestExecution(
				"Splitting preserves using acquisition and reverse disposal",
				"""
				public static class C {
					static int Log;
					sealed class R : System.IDisposable {
						int Id;
						public R(int I) { Id = I; Log = Log * 10 + I; }
						public void Dispose() { Log = Log * 10 + Id; }
					}
					public static int Run() {
						using (R A = new R(1), B = new R(2)) { }
						return Log;
					}
				}
				""",
				1221
			),
			TestExecution(
				"Splitting disposes earlier using resources if later acquisition throws",
				"""
				public static class C {
					static int Log;
					sealed class R : System.IDisposable {
						int Id;
						public R(int I) {
							Id = I;
							Log = Log * 10 + I;
							if (I == 2) throw new System.Exception();
						}
						public void Dispose() { Log = Log * 10 + Id; }
					}
					public static int Run() {
						try { using (R A = new R(1), B = new R(2)) { } } catch { }
						return Log;
					}
				}
				""",
				121
			),
			TestExecution(
				"Splitting preserves fixed pointer initializers",
				"""
				public static class C {
					public static unsafe int Run() {
						var P = new[] { 3 };
						var Q = new[] { 4 };
						fixed (int* A = P, B = Q) return *A + *B;
					}
				}
				""",
				7
			),
			Test(
				"File without a final newline is valid",
				"""
				F();
				""",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Final LF requires removal",
				"""
				F();
				
				""",
				"SPO008@1:5",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Final CRLF requires removal",
				"""
				F();
				
				""".ReplaceLineEndings("\r\n"),
				"SPO008@1:5",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Indented trailing blank lines are one file-end violation",
				"""
				F();
					
					
				""" + "\t",
				"SPO008@1:5",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Horizontal whitespace alone after code is outside the final-newline rule",
				"""
				F();
				""" + "\t ",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Empty file has no file-end violation",
				"""
				
				""",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Unterminated verbatim string whitespace is literal content",
				"""
				var Text = @"content
				
				""",
				aDiagnosticId: "SPO008"
			),
			Test(
				"Unterminated raw string whitespace is literal content",
				""""
				var Text = """
				content
				
				"""",
				aDiagnosticId: "SPO008"
			),
			Test(
				"UTF-8 encoding with a preamble is a BOM violation",
				"""
				F();
				""",
				"SPO009@1:1",
				aDiagnosticId: "SPO009",
				aEncoding: new System.Text.UTF8Encoding(true)
			),
			Test(
				"UTF-8 without a preamble is valid",
				"""
				F();
				""",
				aDiagnosticId: "SPO009",
				aEncoding: new System.Text.UTF8Encoding(false)
			),
			Test(
				"UTF-16 with a preamble is also a BOM violation",
				"""
				F();
				""",
				"SPO009@1:1",
				aDiagnosticId: "SPO009",
				aEncoding: new System.Text.UnicodeEncoding(false, true)
			),
			Test(
				"A textual leading BOM is a violation",
				"\uFEFF" + """
				F();
				""",
				"SPO009@1:1",
				aDiagnosticId: "SPO009"
			),
			Test(
				"BOM characters inside literals are not file signatures",
				"""
				var Text = "\uFEFF";
				""",
				aDiagnosticId: "SPO009"
			),
			TestFix(
				"Remove final newline and indented blank lines",
				"""
				F();
					
					
				""" + "\t ",
				"""
				F();
				""",
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"Remove final CRLF while retaining internal CRLF",
				"""
				F();
				G();
				
				""".ReplaceLineEndings("\r\n"),
				"""
				F();
				G();
				""".ReplaceLineEndings("\r\n"),
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"Remove final newline after a comment without removing the comment",
				"""
				F(); // End
				
				""",
				"""
				F(); // End
				""",
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"Remove final newline after a preprocessor directive",
				"""
				#if true
				F();
				#endif
				
				""",
				"""
				#if true
				F();
				#endif
				""",
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"Whitespace-only files become empty",
				"""
					
					
				""" + "\t ",
				"",
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"Final-newline fix preserves blank lines inside raw literals",
				""""
				var Text = """
				   Content
				
				   """;
				
				"""",
				""""
				var Text = """
				   Content
				
				   """;
				"""",
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"Final-newline fix leaves a suppressed violation unchanged",
				"""
				#pragma warning disable SPO008
				F();
				
				""",
				"""
				#pragma warning disable SPO008
				F();
				
				""",
				aOnlyDiagnostic: "SPO008"
			),
			TestFix(
				"BOM fix changes encoding even when source text is unchanged",
				"""
				var Text = "Grüße";
				""",
				"""
				var Text = "Grüße";
				""",
				aOnlyDiagnostic: "SPO009",
				aEncoding: new System.Text.UTF8Encoding(true),
				aExpectedEncoding: new System.Text.UTF8Encoding(false)
			),
			TestFix(
				"BOM fix converts UTF-16 to UTF-8 without changing Unicode values",
				"""
				var Text = "Grüße 🌍";
				""",
				"""
				var Text = "Grüße 🌍";
				""",
				aOnlyDiagnostic: "SPO009",
				aEncoding: new System.Text.UnicodeEncoding(false, true),
				aExpectedEncoding: new System.Text.UTF8Encoding(false)
			),
			TestFix(
				"BOM fix removes textual leading BOM characters",
				"\uFEFF\uFEFF" + """
				var Text = "\uFEFF";
				""",
				"""
				var Text = "\uFEFF";
				""",
				aOnlyDiagnostic: "SPO009",
				aExpectedEncoding: new System.Text.UTF8Encoding(false)
			),
			TestFix(
				"BOM fix selected alone preserves the final newline",
				"""
				F();
				
				""",
				"""
				F();
				
				""",
				aOnlyDiagnostic: "SPO009",
				aEncoding: new System.Text.UTF8Encoding(true),
				aExpectedEncoding: new System.Text.UTF8Encoding(false)
			),
			TestFix(
				"Final-newline fix selected alone preserves the BOM encoding",
				"""
				F();
				
				""",
				"""
				F();
				""",
				aOnlyDiagnostic: "SPO008",
				aEncoding: new System.Text.UTF8Encoding(true)
			)
		]
	);
	
	private static mTest.tTest
	Test(
		tText aName,
		tText aSource,
		tText aExpected = "",
		tText? aDiagnosticId = null,
		System.Text.Encoding? aEncoding = null,
		[CallerFilePath] tText aFile = "",
		[CallerLineNumber] tInt32 aLine = 0
	) => mTest.Test(
		aName,
		_ => {
			var Tree = CSharpSyntaxTree.ParseText(aSource, new CSharpParseOptions(LanguageVersion.Preview), path: "Test.cs", encoding: aEncoding);
			var Suppressed = ImmutableDictionary<tText, ReportDiagnostic>.Empty;
			
			foreach (var Rule in new mCodeAnalyser.tAnalyzer().SupportedDiagnostics) {
				if (aDiagnosticId is null ? Rule.Id is not ("SPO001" or "SPO002" or "SPO003" or "SPO004") : Rule.Id != aDiagnosticId) {
					Suppressed = Suppressed.Add(Rule.Id, ReportDiagnostic.Suppress);
				}
			}
			
			var Diagnostics = CSharpCompilation.Create(
				"CodeAnalyserTests",
				[Tree],
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithSpecificDiagnosticOptions(Suppressed)
			).WithAnalyzers(
				[new mCodeAnalyser.tAnalyzer()]
			).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
			
			var Actual = new tText[Diagnostics.Length];
			
			for (var Index = 0; Index < Diagnostics.Length; Index += 1) {
				var Diagnostic = Diagnostics[Index];
				var Position = Diagnostic.Location.GetLineSpan().StartLinePosition;
				Actual[Index] = $"{Diagnostic.Id}@{Position.Line + 1}:{Position.Character + 1}";
			}
			
			System.Array.Sort(Actual, System.StringComparer.Ordinal);
			mAssert.AreEquals(tText.Join("\n", Actual), aExpected);
		},
		aFile,
		aLine
	);
	
	private static mTest.tTest
	TestExecution(
		tText aName,
		tText aSource,
		tInt32 aExpected,
		[CallerFilePath] tText aFile = "",
		[CallerLineNumber] tInt32 aLine = 0
	) => mTest.Test(
		aName,
		_ => {
			var Tree = CSharpSyntaxTree.ParseText(aSource, new CSharpParseOptions(LanguageVersion.Preview));
			var Fixed = Tree.FixAsync(ImmutableHashSet.Create("SPO005", "SPO006", "SPO007")).GetAwaiter().GetResult();
			
			foreach (var Version in new[] { Tree, Fixed }) {
				using var Stream = new System.IO.MemoryStream();
				
				var Result = CSharpCompilation.Create(
					"ExecutionTest",
					[Version],
					[MetadataReference.CreateFromFile(typeof(tUnknown).Assembly.Location)],
					new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true)
				).Emit(Stream);
				
				mAssert.IsTrue(Result.Success, () => tText.Join("\n", Result.Diagnostics));
				Stream.Position = 0;
				var Context = new System.Runtime.Loader.AssemblyLoadContext("ExecutionTest", isCollectible: true);
				
				try {
					var Assembly = Context.LoadFromStream(Stream);
					mAssert.AreEquals((tInt32)Assembly.GetType("C")!.GetMethod("Run")!.Invoke(null, null)!, aExpected);
				} finally {
					Context.Unload();
				}
			}
		},
		aFile,
		aLine
	);
	
	private static mTest.tTest
	TestFix(
		tText aName,
		tText aSource,
		tText aExpected,
		tText? aOnlyDiagnostic = null,
		System.Text.Encoding? aEncoding = null,
		System.Text.Encoding? aExpectedEncoding = null,
		[CallerFilePath] tText aFile = "",
		[CallerLineNumber] tInt32 aLine = 0
	) => mTest.Test(
		aName,
		_ => {
			var Tree = CSharpSyntaxTree.ParseText(aSource, new CSharpParseOptions(LanguageVersion.Preview), encoding: aEncoding);
			
			var Ids = (
				aOnlyDiagnostic is null
				? ImmutableHashSet.Create("SPO001", "SPO002", "SPO003", "SPO004")
				: ImmutableHashSet.Create(aOnlyDiagnostic)
			);
			
			var Fixed = Tree.FixAsync(Ids).GetAwaiter().GetResult();
			var Text = Fixed.GetTextAsync().GetAwaiter().GetResult();
			mAssert.AreEquals(Text.ToString(), aExpected);
			mAssert.IsTrue(Equals(Text.Encoding, aExpectedEncoding ?? aEncoding));
			var Repeated = Fixed.FixAsync(Ids).GetAwaiter().GetResult();
			mAssert.AreEquals(Repeated.GetTextAsync().GetAwaiter().GetResult().ToString(), aExpected);
			mAssert.IsTrue(Equals(Repeated.GetTextAsync().GetAwaiter().GetResult().Encoding, Text.Encoding));
		},
		aFile,
		aLine
	);
}