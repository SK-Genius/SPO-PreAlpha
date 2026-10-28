#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mStream.cs
#:ref Common/mTextStream.cs
#:ref Common/mParserGen.cs
#:ref Common/mTextParser.cs
#:ref Common/mSpan.cs
#:ref mTokenizer.cs

using tPos = mTextStream.tPos;

public static class
mTokenizer_Tests {
	private static mSpan.tSpan<tPos>
	Span(
		(tNat32 Row, tNat32 Col) aStart,
		(tNat32 Row, tNat32 Col) aEnd
	) => mSpan.Span(
		new tPos {
			Id = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		new tPos {
			Id = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mTokenizer),
		[
			mTest.Test("Identifiers beginning with numbers and numeric token boundaries",
				aDebug => {
					var Tokens = mTokenizer.Tokenizer.TryParseText(
						"01_03_xyz 1abc 1_234 -2 +3 1..2 1.Foo 1... 1...2",
						"",
						a => aDebug(a())
					).ElseThrow(
					).Result;
					
					mAssert.AreEquals(
						Tokens.Map(a => (a.Type, a.Text)),
						mStream.Stream(
							(mTokenizer.tTokenType.Id, "01_03_xyz"),
							(mTokenizer.tTokenType.Id, "1abc"),
							(mTokenizer.tTokenType.Number, "1234"),
							(mTokenizer.tTokenType.Number, "-2"),
							(mTokenizer.tTokenType.Number, "3"),
							(mTokenizer.tTokenType.Number, "1"),
							(mTokenizer.tTokenType.SpecialToken, ".."),
							(mTokenizer.tTokenType.Number, "2"),
							(mTokenizer.tTokenType.Number, "1"),
							(mTokenizer.tTokenType.SpecialToken, "."),
							(mTokenizer.tTokenType.Id, "Foo"),
							(mTokenizer.tTokenType.Id, "1..."),
							(mTokenizer.tTokenType.Id, "1...2")
						)
					);
				}
			),
			mTest.Test("Numeric tokens and identifiers stop before reserved characters",
				aDebug => {
					foreach (
						var (Source, Type, Text) in mStream.Stream(
							("01_03_xyz", mTokenizer.tTokenType.Id, "01_03_xyz"),
							("1abc", mTokenizer.tTokenType.Id, "1abc"),
							("123", mTokenizer.tTokenType.Number, "123"),
							("1_234", mTokenizer.tTokenType.Number, "1234"),
							("-2", mTokenizer.tTokenType.Number, "-2"),
							("+3", mTokenizer.tTokenType.Number, "3")
						)
					) {
						foreach (
							var Suffix in mStream.Stream(
								"",
								"€next",
								"#Tag",
								"§INT",
								"\"Text\"",
								".",
								"..2",
								":next",
								",next",
								";next",
								"|next",
								"(next",
								")next",
								"[next",
								"]next",
								"{next",
								"}next",
								" next",
								"\tnext",
								"\nnext",
								"\r\nnext"
							)
						) {
							var Input = Source + Suffix;
							var Result = mTokenizer.Token.StartParse(
								Input.ToStream("").Map(a => (mSpan.Span(a.Pos), a.Char)),
								a => aDebug(a())
							).AssertNotError(a => a.ToText(Input.Split('\n')));
							var Token = Result.Result.Value;
							mAssert.AreEquals(
								(
									Token.Type,
									Token.Text,
									Token.Span,
									Result.RemainingStream.Map(a => a.Value).Reduce("", (aText, aChar) => aText + aChar)
								),
								(
									Type,
									Text,
									Span((1, 1), (1, (tNat32)Source.Length)),
									Suffix.Replace("\r", "")
								)
							);
						}
					}
				}
			),
			mTest.Test("TwoLines",
				aDebugStream => {
					var TokenList = mTokenizer.Tokenizer.TryParseText(
						"""
						a := §INT b <=> c
						a := [#b c d "Text"]
						a = "
						|Line1
						|Line2
						"
						""",
						"",
						__ => aDebugStream(__())
					).ElseThrow(
					).Result;
					
					mAssert.AreEquals(
						TokenList,
						mStream.Stream(
							new mTokenizer.tToken { Span = Span((1, 1), (1, 1)), Text = "a", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((1, 3), (1, 3)), Text = ":", Type = mTokenizer.tTokenType.SpecialToken },
							new mTokenizer.tToken { Span = Span((1, 4), (1, 4)), Text = "=", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((1, 6), (1, 9)), Text = "§INT", Type = mTokenizer.tTokenType.SpecialId },
							new mTokenizer.tToken { Span = Span((1, 11), (1, 11)), Text = "b", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((1, 13), (1, 15)), Text = "<=>", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((1, 17), (1, 17)), Text = "c", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((1, 18), (1, 18)), Text = "\n", Type = mTokenizer.tTokenType.SpecialToken },
							new mTokenizer.tToken { Span = Span((2, 1), (2, 1)), Text = "a", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((2, 3), (2, 3)), Text = ":", Type = mTokenizer.tTokenType.SpecialToken },
							new mTokenizer.tToken { Span = Span((2, 4), (2, 4)), Text = "=", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((2, 6), (2, 6)), Text = "[", Type = mTokenizer.tTokenType.SpecialToken },
							new mTokenizer.tToken { Span = Span((2, 7), (2, 8)), Text = "#b", Type = mTokenizer.tTokenType.SpecialId },
							new mTokenizer.tToken { Span = Span((2, 10), (2, 10)), Text = "c", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((2, 12), (2, 12)), Text = "d", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((2, 14), (2, 19)), Text = "Text", Type = mTokenizer.tTokenType.Text },
							new mTokenizer.tToken { Span = Span((2, 20), (2, 20)), Text = "]", Type = mTokenizer.tTokenType.SpecialToken },
							new mTokenizer.tToken { Span = Span((2, 21), (2, 21)), Text = "\n", Type = mTokenizer.tTokenType.SpecialToken },
							new mTokenizer.tToken { Span = Span((3, 1), (3, 1)), Text = "a", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((3, 3), (3, 3)), Text = "=", Type = mTokenizer.tTokenType.Id },
							new mTokenizer.tToken { Span = Span((3, 5), (6, 1)), Text = "Line1\nLine2", Type = mTokenizer.tTokenType.Text }
						)
					);
				}
			)
		]
	);
}
