using System;

public static class
mParserGen_Tests {
	private static readonly mSpan.tSpan<mStd.tEmpty> cTestSpan = default;
	
	private static mStream.tStream<(mSpan.tSpan<mStd.tEmpty>, t)>?
	TestStream<t>(
		System.ReadOnlySpan<t> aList
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
		System.Span<tText> aError
	) {
		var Result = mStream.Stream<(mStd.tEmpty Pos, tText Message)>([]);
		foreach (var Message in aError) {
			Result = mStream.Stream(
				(Pos: mStd.cEmpty, Message),
				Result
			);
		}
		return mResult.Fail(Result.Reverse());
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mParserGen),
		[
			mTest.Test("AtomParser",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
					
					mAssert.AreEquals(
						A.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, 'A'), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						A.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A.StartParse(TestStream("B_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("...+...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
					var AB = mParserGen.Seq(A, B);
					
					mAssert.AreEquals(
						AB.StartParse(TestStream("AB_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, ('A', 'B')), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss B", "miss B"]) // TODO: remove redundant error messages
					);
				}
			),
			mTest.Test("...-...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
					var AB = A +-B;
					mAssert.AreEquals(
						AB.StartParse(TestStream("AB_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, 'A'), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss B", "miss B"])
					);
				}
			),
			mTest.Test("-...",
				aDebugStream => {
					var A = -mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "unexpected A"), ComparePos);
					
					mAssert.AreEquals(
						A.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStd.cEmpty), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						A.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["unexpected A"])
					);
					mAssert.AreEquals(
						A.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["unexpected A"])
					);
				}
			),
			mTest.Test("...|...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'B', _ => (_.Span.Start, "miss B"), ComparePos);
					var AB = A | B;
					
					mAssert.AreEquals(
						AB.StartParse(TestStream("AB".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, 'A'), TestStream(['B']), cNoError))
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("BA".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, 'B'), TestStream("A".AsSpan()), mStream.Stream([(mStd.cEmpty, "miss A")])))
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A", "miss B"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A", "miss B"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream("__".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A", "miss B"])
					);
				}
			),
			mTest.Test("...[m, n]",
				aDebugStream => {
					var A2_3 = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[2..3];
					
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("AA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("AA".AsSpan())), TestStream("_".AsSpan()), mStream.Stream([(mStd.cEmpty, "miss A")])))
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("AAA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("AAA".AsSpan())), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("AAAA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("AAA".AsSpan())), TestStream("A_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("_A".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("A".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("...[0, null]",
				aDebugStream => {
					var A0_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[0..];
					
					mAssert.AreEquals(
						A0_.StartParse(TestStream("AA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("AA".AsSpan())), TestStream("_".AsSpan()), mStream.Stream([(mStd.cEmpty, "miss A")])))
					);
					mAssert.AreEquals(
						A0_.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("".AsSpan())), TestStream("_".AsSpan()), mStream.Stream([(mStd.cEmpty, "miss A")])))
					);
				}
			),
			mTest.Test("...[n, null]",
				aDebugStream => {
					var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[2..];
					
					mAssert.AreEquals(
						A2_.StartParse(TestStream("AA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("AA".AsSpan())), TestStream("_".AsSpan()), mStream.Stream([(mStd.cEmpty, "miss A")])))
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("AAA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, mStream.Stream("AAA".AsSpan())), TestStream("_".AsSpan()), mStream.Stream([(mStd.cEmpty, "miss A")])))
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("_A".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("A".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("....Modify(...=>...)",
				aDebugStream => {
					var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(_ => _ == 'A', _ => (_.Span.Start, "miss A"), ComparePos)[2..]
					.Modify(aChars => aChars.Count());
					
					mAssert.AreEquals(
						A2_.StartParse(TestStream("AA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, 2u), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("AAAAA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, 5u), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						A2_.StartParse(mStd.cEmpty, _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("_A".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("A".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream("A_".AsSpan()), _ => aDebugStream(_())),
						Fail(["miss A"])
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
						NotAB.StartParse(TestStream("AB_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, (mStream.Stream("".AsSpan()), "AB")), TestStream("_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream("BAABA_".AsSpan()), _ => aDebugStream(_())),
						mResult.OK(mParserGen.ParserResult((cTestSpan, (mStream.Stream("BA".AsSpan()), "AB")), TestStream("A_".AsSpan()), cNoError))
					);
					mAssert.AreEquals(
						NotAB.StartParse(mStd.cEmpty, _ => aDebugStream(_())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream("_".AsSpan()), _ => aDebugStream(_())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream("_A".AsSpan()), _ => aDebugStream(_())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream("A".AsSpan()), _ => aDebugStream(_())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream("A_BA".AsSpan()), _ => aDebugStream(_())),
						Fail([])
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
							TestStream(aExpr.AsSpan()),
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
					
					mParserGen.tParser<mStd.tEmpty, tChar, tChar, tText>
					CharIn(
						tText aChars
					) => mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
						aChar => {
							foreach (var Char in aChars) {
								if (Char == aChar) { return true; }
							}
							return false;
						},
						_ => (_.Span.Start, $"miss one of [{aChars}]"),
						ComparePos
					);
					
					mParserGen.tParser<mStd.tEmpty, tChar, tText, tText>
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
		]
	);
}
