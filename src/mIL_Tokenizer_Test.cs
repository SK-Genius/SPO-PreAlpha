//IMPORT mTest.cs
//IMPORT mIL_Tokenizer.cs

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

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class mIL_Tokenizer_Test {
	
	//================================================================================
	private static mStd.tSpan<tPos> Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	//================================================================================
	) => new mStd.tSpan<tPos> {
		Start = {
			Row = aStart.Row,
			Col = aStart.Col
		},
		End = {
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	};
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_Tokenizer),
		mTest.Test(
			"TwoLines",
			aDebugStream => {
				var TokenList = mIL_Tokenizer.Tokenizer.ParseText(
					"a := §INT b <=> c \n a := [#b c]",
					aDebugStream
				).Result;
				mStd.AssertEq(
					TokenList,
					mList.List(
						new mIL_Tokenizer.tToken{ Span = Span((1, 1), (1, 1)), Text = "a", Type = mIL_Tokenizer.tTokenType.Ident },
						new mIL_Tokenizer.tToken{ Span = Span((1, 3), (1, 4)), Text = ":=", Type = mIL_Tokenizer.tTokenType.SpecialToken },
						new mIL_Tokenizer.tToken{ Span = Span((1, 6), (1, 9)), Text = "INT", Type = mIL_Tokenizer.tTokenType.KeyWord },
						new mIL_Tokenizer.tToken{ Span = Span((1, 11), (1, 11)), Text = "b", Type = mIL_Tokenizer.tTokenType.Ident },
						new mIL_Tokenizer.tToken{ Span = Span((1, 13), (1, 15)), Text = "<=>", Type = mIL_Tokenizer.tTokenType.Ident },
						new mIL_Tokenizer.tToken{ Span = Span((1, 17), (1, 17)), Text = "c", Type = mIL_Tokenizer.tTokenType.Ident },
						new mIL_Tokenizer.tToken{ Span = Span((1, 19), (1, 19)), Text = "\n", Type = mIL_Tokenizer.tTokenType.SpecialToken },
						new mIL_Tokenizer.tToken{ Span = Span((2, 2), (2, 2)), Text = "a", Type = mIL_Tokenizer.tTokenType.Ident },
						new mIL_Tokenizer.tToken{ Span = Span((2, 4), (2, 5)), Text = ":=", Type = mIL_Tokenizer.tTokenType.SpecialToken },
						new mIL_Tokenizer.tToken{ Span = Span((2, 7), (2, 7)), Text = "[", Type = mIL_Tokenizer.tTokenType.SpecialToken },
						new mIL_Tokenizer.tToken{ Span = Span((2, 8), (2, 9)), Text = "b", Type = mIL_Tokenizer.tTokenType.Prefix },
						new mIL_Tokenizer.tToken{ Span = Span((2, 11), (2, 11)), Text = "c", Type = mIL_Tokenizer.tTokenType.Ident },
						new mIL_Tokenizer.tToken{ Span = Span((2, 12), (2, 12)), Text = "]", Type = mIL_Tokenizer.tTokenType.SpecialToken }
					)
				);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("TwoLines")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
