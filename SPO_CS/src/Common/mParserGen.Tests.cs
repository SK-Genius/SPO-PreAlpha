public static class
mParserGen_Tests {
	
	private static readonly mSpan.tSpan<mStd.tEmpty> cTestSpan = default;
	
	private static mStream.tStream<(mSpan.tSpan<mStd.tEmpty>, t)>?
	TestStream<t>(
		params t[] aList
	) => mStream.Stream(aList).Map(_ => (cTestSpan, _));
	
	private static tInt32
	ComparePos(
		mStd.tEmpty a1,
		mStd.tEmpty a2
	) {
		return 0;
	}
	
	private static readonly mStream.tStream<(mStd.tEmpty Pos, tText Message)>?
	cNoError = default;
	
	private static mResult.tResultFail<mStream.tStream<(mStd.tEmpty Pos, tText Message)>?>
	Fail(
		params tText[] aError
	) {
		var Result = mStream.Stream<(mStd.tEmpty Pos, tText Message)>();
		foreach (var Message in aError) {
			Result = mStream.Stream<(mStd.tEmpty Pos, tText Message)>(
				(Pos: mStd.cEmpty, Message),
				Result
			);
		}
		return mResult.Fail(Result.Reverse());
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mParserGen),
		mTest.Test("AtomParser",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
				
				mAssert.AreEquals(
					A.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, 'A'), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					A.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A.StartParse(TestStream('B', '_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
			}
		),
		mTest.Test("...+...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
				var AB = mParserGen.Seq(A, B);
				
				mAssert.AreEquals(
					AB.StartParse(TestStream('A', 'B', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, ('A', 'B')), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					Fail("miss B", "miss B") // TODO: remove redundant error messages
				);
			}
		),
		mTest.Test("...-...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
				var AB = A +-B;
				mAssert.AreEquals(
					AB.StartParse(TestStream('A', 'B', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, 'A'), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					Fail("miss B", "miss B")
				);
			}
		),
		mTest.Test("-...",
			aDebugStream => {
				var A = -mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "unexpected A"), ComparePos);
				
				mAssert.AreEquals(
					A.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStd.cEmpty), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					A.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("unexpected A")
				);
				mAssert.AreEquals(
					A.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("unexpected A")
				);
			}
		),
		mTest.Test("...|...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
				var AB = A | B;
				
				mAssert.AreEquals(
					AB.StartParse(TestStream('A', 'B'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, 'A'), TestStream('B'), cNoError))
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('B', 'A'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, 'B'), TestStream('A'), mStream.Stream((default(mStd.tEmpty), "miss A"))))
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A", "miss B")
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A", "miss B")
				);
				mAssert.AreEquals(
					AB.StartParse(TestStream('_', '_'), _ => aDebugStream(_())),
					Fail("miss A", "miss B")
				);
			}
		),
		mTest.Test("...[m, n]",
			aDebugStream => {
				var A2_3 = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[2..3];
				
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream('A', 'A')), TestStream('_'), mStream.Stream((default(mStd.tEmpty), "miss A"))))
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('A', 'A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('A', 'A', 'A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('A', '_'), cNoError))
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('_', 'A'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('A'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_3.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
			}
		),
		mTest.Test("...[0, null]",
			aDebugStream => {
				var A0_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[0..];
				
				mAssert.AreEquals(
					A0_.StartParse(TestStream('A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream('A', 'A')), TestStream('_'), mStream.Stream((default(mStd.tEmpty), "miss A"))))
				);
				mAssert.AreEquals(
					A0_.StartParse(TestStream( '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream<tChar>()), TestStream('_'), mStream.Stream((default(mStd.tEmpty), "miss A"))))
				);
			}
		),
		mTest.Test("...[n, null]",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[2..];
				
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream('A', 'A')), TestStream('_'), mStream.Stream((default(mStd.tEmpty), "miss A"))))
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A', 'A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('_'), mStream.Stream((default(mStd.tEmpty), "miss A"))))
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('_', 'A'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
			}
		),
		mTest.Test("....Modify(...=>...)",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[2..]
				.Modify(aChars => aChars.Count());
				
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, 2u), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A', 'A', 'A', 'A', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, 5u), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('_', 'A'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A'), _ => aDebugStream(_())),
					Fail("miss A")
				);
				mAssert.AreEquals(
					A2_.StartParse(TestStream('A', '_'), _ => aDebugStream(_())),
					Fail("miss A")
				);
			}
		),
		mTest.Test("~...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
				var AB = mParserGen.Seq(A, B).Modify(_ => "AB");
				var NotAB = ~AB;
				
				mAssert.AreEquals(
					NotAB.StartParse(TestStream('A', 'B', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, (mStream.Stream<tChar>(), "AB")), TestStream('_'), cNoError))
				);
				mAssert.AreEquals(
					NotAB.StartParse(TestStream('B', 'A', 'A', 'B', 'A', '_'), _ => aDebugStream(_())),
					mResult.OK(mParserGen.ParserResult((cTestSpan, (mStream.Stream('B', 'A'), "AB")), TestStream('A', '_'), cNoError))
				);
				mAssert.AreEquals(
					NotAB.StartParse(TestStream<tChar>(), _ => aDebugStream(_())),
					Fail()
				);
				mAssert.AreEquals(
					NotAB.StartParse(TestStream('_'), _ => aDebugStream(_())),
					Fail()
				);
				mAssert.AreEquals(
					NotAB.StartParse(TestStream('_', 'A'), _ => aDebugStream(_())),
					Fail()
				);
				mAssert.AreEquals(
					NotAB.StartParse(TestStream('A'), _ => aDebugStream(_())),
					Fail()
				);
				mAssert.AreEquals(
					NotAB.StartParse(TestStream('A', '_', 'B', 'A'), _ => aDebugStream(_())),
					Fail()
				);
			}
		),
		mTest.Test("Eval('MathExpr')",
			aDebugStream => {
				var _ = -CharIn(" \t");
				var __ = -_[0..];
				
				var Digit = CharIn("0123456789")
				.Modify(aChar => aChar - '0');
				
				var Nat = Digit[1..]
				.Modify(aDigits => aDigits.Reduce(0, (aNat, aDigit) => aNat*10 + aDigit));
				
				var PosSignum = (-mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(aChar => aChar == '+', _ => (_.Span.Start, "miss +"), ComparePos))
				.Modify(() => +1);
				
				var NegSignum = (-mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(aChar => aChar == '-', _ => (_.Span.Start, "miss -"), ComparePos))
				.Modify(() => -1);
				
				var Signum = PosSignum | NegSignum;
				
				var Int = mParserGen.Seq(Signum, Nat)
				.Modify((aSignum, aNat) => aSignum * aNat);
				
				var Number = Nat | Int;
				
				var OpAdd = (-Token("+"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 + a2));
				
				var OpSub = (-Token("-"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 - a2));
				
				var OpMul = (-Token("*"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 * a2));
				
				var OpDiv = (-Token("/"))
				.Modify(() => mStd.Func((tInt32 a1, tInt32 a2) => a1 / a2));
				
				var Op = OpAdd | OpSub | OpMul | OpDiv;
				
				var Expression = mParserGen.UndefParser<mStd.tEmpty, tChar, tInt32, tText>(ComparePos);
				
				Expression.Def(
					Number |
					P(mParserGen.Seq(Expression, __, Op, __, Expression))
					.Modify((a1, ___, aOp, ____, a2) => aOp(a1, a2))
				);
				
				tInt32 Eval(tText aExpr) {
					var X = Expression.StartParse(
						TestStream(aExpr.ToCharArray()),
						_ => aDebugStream(_())
					);
					return X.ElseThrow("").Result.Value;
				}
				
				mAssert.AreEquals(Eval("1"), 1);
				mAssert.AreEquals(Eval("-2"), -2);
				mAssert.AreEquals(Eval("(3+4)"), 7);
				mAssert.AreEquals(Eval("( 5 - 6)"), -1);
				mAssert.AreEquals(Eval("( 5 --6)"), 11);
				mAssert.AreEquals(Eval("( 7 * (8 / 4))"), 14);
				mAssert.AreEquals(Eval("( 7 * (8 / (4  -6)))"), -28);
				
				mParserGen.tParser<mStd.tEmpty, char, char, string>
				CharIn(
					tText aChars
				) => mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
					(tChar aChar) => {
						foreach (var Char in aChars) {
							if (Char == aChar) { return true; }
						}
						return false;
					},
					_ => (_.Span.Start, $"miss one of [{aChars}]"),
					ComparePos
				);
				
				mParserGen.tParser<mStd.tEmpty, char, string, string>
				Token(
					tText aToken
				) {
					var Parser = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
						_ => _ == aToken[0],
						_ => (_.Span.Start, ""),
						ComparePos
					).Modify(aChar => "" + aChar);
					foreach (var Char in aToken[1..]) {
						Parser = mParserGen.Seq(
							Parser,
							mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
								aChar => aChar == Char,
								_ => (_.Span.Start, "miss {Char}"),
								ComparePos
							)
						).Modify(_ => _.Item1 + _.Item2);
					}
					return Parser;
				}
				
				mParserGen.tParser<mStd.tEmpty, tChar, tOut, tText>
				P<tOut>(
					mParserGen.tParser<mStd.tEmpty, tChar, tOut, tText> aParser
				) => mParserGen.Seq(Token("("), __, aParser, __, Token(")")).Modify(_ => _.Item3);
			}
		)
	);
}
