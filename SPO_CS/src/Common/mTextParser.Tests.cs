// IMPORT mStd
// IMPORT mTest
// IMPORT mAssert
// IMPORT mTextStream
// IMPORT mParserGen
// IMPORT mTextParser
// IMPORT mSpan

using tPos = mTextStream.tPos;

public static class
mTextParser_Tests {

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
		nameof(mTextParser),
		[
			mTest.Test("GetChar success",
				aDebugStream => {
					var CharParser = mTextParser.GetChar('a');
					var Res1 = CharParser.ParseText("a", "", _ => aDebugStream(_()));
					mAssert.AreEquals(Res1.Result, 'a');
					mAssert.AreEquals(Res1.Span, Span((1, 1), (1, 1)));
				}
			),
			mTest.Test("GetChar failure",
				aDebugStream => {
					var CharParser = mTextParser.GetChar('a');
					mAssert.ThrowsError(
						() => { CharParser.ParseText("b", "", _ => aDebugStream(_())); }
					);
				}
			),
			mTest.Test("GetNotChar success",
				aDebugStream => {
					var NotCharParser = mTextParser.GetNotChar('a');
					var Res2 = NotCharParser.ParseText("b", "", _ => aDebugStream(_()));
					mAssert.AreEquals(Res2.Result, 'b');
					mAssert.AreEquals(Res2.Span, Span((1, 1), (1, 1)));
				}
			),
			mTest.Test("GetNotChar failure",
				aDebugStream => {
					var NotCharParser = mTextParser.GetNotChar('a');
					mAssert.ThrowsError(
						() => { NotCharParser.ParseText("a", "", _ => aDebugStream(_())); }
					);
				}
			),
			mTest.Test("GetCharIn failure",
				aDebugStream => {
					var InParser = mTextParser.GetCharIn("abc");
					var Res1 = InParser.ParseText("b", "", _ => aDebugStream(_()));
					mAssert.AreEquals(Res1.Result, 'b');
					mAssert.ThrowsError(
						() => { InParser.ParseText("d", "", _ => aDebugStream(_())); }
					);
				}
			),
			mTest.Test("GetCharNotIn failure",
				aDebugStream => {
					var NotInParser = mTextParser.GetCharNotIn("ab");
					var Res2 = NotInParser.ParseText("c", "", _ => aDebugStream(_()));
					mAssert.AreEquals(Res2.Result, 'c');
					mAssert.ThrowsError(
						() => { NotInParser.ParseText("a", "", _ => aDebugStream(_())); }
					);
				}
			),
			mTest.Test("GetCharInRange failure",
				aDebugStream => {
					var RangeParser = mTextParser.GetCharInRange('a', 'c');
					var Res3 = RangeParser.ParseText("b", "", _ => aDebugStream(_()));
					mAssert.AreEquals(Res3.Result, 'b');
					mAssert.ThrowsError(
						() => { RangeParser.ParseText("d", "", _ => aDebugStream(_())); }
					);
				}
			),
			mTest.Test("GetToken success",
				aDebugStream => {
					var TokenParser = mTextParser.GetToken("foo");
					var Res = TokenParser.ParseText("foo", "", _ => aDebugStream(_()));
					mAssert.AreEquals(Res.Result, "foo");
					mAssert.AreEquals(Res.Span, Span((1, 1), (1, 3)));
				}
			),
			mTest.Test("GetToken failure 1",
				aDebugStream => {
					var TokenParser = mTextParser.GetToken("foo");
					mAssert.ThrowsError(
						() => { TokenParser.ParseText("bar", "", _ => aDebugStream(_())); }
					);
				}
			),
			mTest.Test("GetToken failure 2",
				aDebugStream => {
					var TokenParser = mTextParser.GetToken("foo");
					mAssert.ThrowsError(
						() => { TokenParser.ParseText("foo!", "", _ => aDebugStream(_())); }
					);
				}
			)
		]
	);
}
