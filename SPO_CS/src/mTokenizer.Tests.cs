//IMPORT mTest.cs
//IMPORT mTokenizer.cs

#nullable enable

using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

using tPos = mTextStream.tPos;

public static class
mTokenizer_Tests {
	
	private static mSpan.tSpan<tPos>
	Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	) => mSpan.Span(
		new tPos {
			Ident = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		new tPos {
			Ident = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mTokenizer),
		mTest.Test(
			"TwoLines",
			aDebugStream => {
				var TokenList = mTokenizer.Tokenizer.ParseText(
					"a := §INT b <=> c \n a := [#b c]",
					"",
					a => aDebugStream(a())
				).Result;
				mAssert.AreEquals(
					TokenList,
					mStream.Stream(
						new mTokenizer.tToken{ Span = Span((1, 1), (1, 1)), Text = "a", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((1, 3), (1, 3)), Text = ":", Type = mTokenizer.tTokenType.SpecialToken },
						new mTokenizer.tToken{ Span = Span((1, 4), (1, 4)), Text = "=", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((1, 6), (1, 9)), Text = "§INT", Type = mTokenizer.tTokenType.SpecialIdent },
						new mTokenizer.tToken{ Span = Span((1, 11), (1, 11)), Text = "b", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((1, 13), (1, 15)), Text = "<=>", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((1, 17), (1, 17)), Text = "c", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((1, 19), (1, 19)), Text = "\n", Type = mTokenizer.tTokenType.SpecialToken },
						new mTokenizer.tToken{ Span = Span((2, 2), (2, 2)), Text = "a", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((2, 4), (2, 4)), Text = ":", Type = mTokenizer.tTokenType.SpecialToken },
						new mTokenizer.tToken{ Span = Span((2, 5), (2, 5)), Text = "=", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((2, 7), (2, 7)), Text = "[", Type = mTokenizer.tTokenType.SpecialToken },
						new mTokenizer.tToken{ Span = Span((2, 8), (2, 9)), Text = "#b", Type = mTokenizer.tTokenType.SpecialIdent },
						new mTokenizer.tToken{ Span = Span((2, 11), (2, 11)), Text = "c", Type = mTokenizer.tTokenType.Ident },
						new mTokenizer.tToken{ Span = Span((2, 12), (2, 12)), Text = "]", Type = mTokenizer.tTokenType.SpecialToken }
					)
				);
			}
		)
	);
}
