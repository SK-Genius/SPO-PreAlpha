// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT Common/mParserGen
// IMPORT Common/mTextParser
// IMPORT Common/mSpan
// IMPORT mTokenizer

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
			mTest.Test("TwoLines",
				aDebugStream => {
					var TokenList = mTokenizer.Tokenizer.ParseText(
						"""
						a := §INT b <=> c
						a := [#b c d "Text"]
						a = "
						|Line1
						|Line2
						"
						""",
						"",
						_ => aDebugStream(_())
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
