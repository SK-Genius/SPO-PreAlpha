#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mTest.cs
#:ref mAssert.cs
#:ref mTextStream.cs
#:ref mParserGen.cs
#:ref mTextParser.cs
#:ref mSpan.cs

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
					var Res1 = CharParser.TryParseText("a", "", __ => aDebugStream(__())).ElseThrow();
					mAssert.AreEquals(Res1.Result, 'a');
					mAssert.AreEquals(Res1.Span, Span((1, 1), (1, 1)));
				}
			),
			mTest.Test("GetChar failure",
				aDebugStream => {
					var CharParser = mTextParser.GetChar('a');
					mAssert.IsFalse(
						CharParser.TryParseText("b", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			),
			mTest.Test("GetNotChar success",
				aDebugStream => {
					var NotCharParser = mTextParser.GetNotChar('a');
					var Res2 = NotCharParser.TryParseText("b", "", __ => aDebugStream(__())).ElseThrow();
					mAssert.AreEquals(Res2.Result, 'b');
					mAssert.AreEquals(Res2.Span, Span((1, 1), (1, 1)));
				}
			),
			mTest.Test("GetNotChar failure",
				aDebugStream => {
					var NotCharParser = mTextParser.GetNotChar('a');
					mAssert.IsFalse(
						NotCharParser.TryParseText("a", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			),
			mTest.Test("GetCharIn failure",
				aDebugStream => {
					var InParser = mTextParser.GetCharIn("abc");
					var Res1 = InParser.TryParseText("b", "", __ => aDebugStream(__())).ElseThrow();
					mAssert.AreEquals(Res1.Result, 'b');
					mAssert.IsFalse(
						InParser.TryParseText("d", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			),
			mTest.Test("GetCharNotIn failure",
				aDebugStream => {
					var NotInParser = mTextParser.GetCharNotIn("ab");
					var Res2 = NotInParser.TryParseText("c", "", __ => aDebugStream(__())).ElseThrow();
					mAssert.AreEquals(Res2.Result, 'c');
					mAssert.IsFalse(
						NotInParser.TryParseText("a", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			),
			mTest.Test("GetCharInRange failure",
				aDebugStream => {
					var RangeParser = mTextParser.GetCharInRange('a', 'c');
					var Res3 = RangeParser.TryParseText("b", "", __ => aDebugStream(__())).ElseThrow();
					mAssert.AreEquals(Res3.Result, 'b');
					mAssert.IsFalse(
						RangeParser.TryParseText("d", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			),
			mTest.Test("GetToken success",
				aDebugStream => {
					var TokenParser = mTextParser.GetToken("foo");
					var Res = TokenParser.TryParseText("foo", "", __ => aDebugStream(__())).ElseThrow();
					mAssert.AreEquals(Res.Result, "foo");
					mAssert.AreEquals(Res.Span, Span((1, 1), (1, 3)));
				}
			),
			mTest.Test("GetToken failure 1",
				aDebugStream => {
					var TokenParser = mTextParser.GetToken("foo");
					mAssert.IsFalse(
						TokenParser.TryParseText("bar", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			),
			mTest.Test("GetToken failure 2",
				aDebugStream => {
					var TokenParser = mTextParser.GetToken("foo");
					mAssert.IsFalse(
						TokenParser.TryParseText("foo!", "", __ => aDebugStream(__())).Match(out _, out _)
					);
				}
			)
		]
	);
}
