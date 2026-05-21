#:include mStd.cs
#:include mTest.cs
#:include mSpan.cs
#:include mStream.cs
#:include mParserGen.cs
#:include mAssert.cs
#:include mResult.cs

public static class
mParserGen_Tests {
	private static tBool
	AreEqual_Int(
		tInt32 a1,
		tInt32 a2
	) => a1 == a2;
	
	private static tBool
	AreEqual_Nat(
		tNat32 a1,
		tNat32 a2
	) => a1 == a2;
	
	private static readonly mSpan.tSpan<mStd.tEmpty> cTestSpan = default;
	
	private static mStream.tStream<(mSpan.tSpan<mStd.tEmpty>, t)>
	TestStream<t>(
		System.ReadOnlySpan<t> aList
	) => mStream.Stream(aList).Map(__ => (cTestSpan, __));
	
	private static tInt32
	ComparePos(
		mStd.tEmpty a1,
		mStd.tEmpty a2
	) => 0;
	
	private static tBool
	AreErrorsEqual(
		tText a1,
		tText a2
	) => a1 == a2;
	
	private static readonly mStream.tStream<(mStd.tEmpty Pos, tText Message)>
	cNoError = mStd.cEmpty;
	
	private static mResult.tResultFail<mStream.tStream<(mStd.tEmpty Pos, tText Message)>>
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
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual);
					
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, 'A'), TestStream(System.MemoryExtensions.AsSpan("_")), cNoError)
					);
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("B_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("...+...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'B', __ => (__.Span.Start, "miss B"), ComparePos, AreErrorsEqual);
					var AB = mParserGen.Seq(A, B);
					
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("AB_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, ('A', 'B')), TestStream(System.MemoryExtensions.AsSpan("_")), cNoError)
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						Fail(["miss B"])
					);
				}
			),
			mTest.Test("...-...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'B', __ => (__.Span.Start, "miss B"), ComparePos, AreErrorsEqual);
					var AB = A +-B;
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("AB_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, 'A'), TestStream(System.MemoryExtensions.AsSpan("_")), cNoError)
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						Fail(["miss B"])
					);
				}
			),
			mTest.Test("-...",
				aDebugStream => {
					var A = -mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', _ => (_.Span.Start, "unexpected A"), ComparePos, AreErrorsEqual);
					
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStd.cEmpty), TestStream(System.MemoryExtensions.AsSpan("_")), cNoError)
					);
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["unexpected A"])
					);
					mAssert.AreEquals(
						A.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["unexpected A"])
					);
				}
			),
			mTest.Test("...|...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'B', __ => (__.Span.Start, "miss B"), ComparePos, AreErrorsEqual);
					var AB = A | B;
					
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("AB")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, 'A'), TestStream(['B']), cNoError)
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("BA")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, 'B'), TestStream(System.MemoryExtensions.AsSpan("A")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["miss A", "miss B"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A", "miss B"])
					);
					mAssert.AreEquals(
						AB.StartParse(TestStream(System.MemoryExtensions.AsSpan("__")), __ => aDebugStream(__())),
						Fail(["miss A", "miss B"])
					);
				}
			),
			mTest.Test("...[m, n]",
				aDebugStream => {
					var A2_3 = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual)[2..3];
					
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("AA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan("AA"))), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("AAA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan("AAA"))), TestStream(System.MemoryExtensions.AsSpan("_")), cNoError)
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("AAAA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan("AAA"))), TestStream(System.MemoryExtensions.AsSpan("A_")), cNoError)
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("_A")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("A")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_3.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("...[0, null]",
				aDebugStream => {
					var A0_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual)[0..];
					
					mAssert.AreEquals(
						A0_.StartParse(TestStream(System.MemoryExtensions.AsSpan("AA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan("AA"))), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						A0_.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan(""))), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
				}
			),
			mTest.Test("...[n, null]",
				aDebugStream => {
					var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual)[2..];
					
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("AA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan("AA"))), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("AAA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, mStream.Stream(System.MemoryExtensions.AsSpan("AAA"))), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("_A")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("A")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("....Modify(...=>...)",
				aDebugStream => {
					var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual)[2..]
					.Modify(aChars => aChars.Count());
					
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("AA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, 2u), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("AAAAA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, 5u), TestStream(System.MemoryExtensions.AsSpan("_")), mStream.Stream((mStd.cEmpty, "miss A")))
					);
					mAssert.AreEquals(
						A2_.StartParse(mStd.cEmpty, __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("_A")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("A")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
					mAssert.AreEquals(
						A2_.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_")), __ => aDebugStream(__())),
						Fail(["miss A"])
					);
				}
			),
			mTest.Test("~...",
				aDebugStream => {
					var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'A', __ => (__.Span.Start, "miss A"), ComparePos, AreErrorsEqual);
					var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(__ => __ == 'B', __ => (__.Span.Start, "miss B"), ComparePos, AreErrorsEqual);
					var AB = mParserGen.Seq(A, B).Modify(_ => "AB");
					var NotAB = ~AB;
					
					mAssert.AreEquals(
						NotAB.StartParse(TestStream(System.MemoryExtensions.AsSpan("AB_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, (mStream.Stream(System.MemoryExtensions.AsSpan("")), "AB")), TestStream(System.MemoryExtensions.AsSpan("_")), cNoError)
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream(System.MemoryExtensions.AsSpan("BAABA_")), __ => aDebugStream(__())),
						mParserGen.ParserResult((cTestSpan, (mStream.Stream(System.MemoryExtensions.AsSpan("BA")), "AB")), TestStream(System.MemoryExtensions.AsSpan("A_")), cNoError)
					);
					mAssert.AreEquals(
						NotAB.StartParse(mStd.cEmpty, __ => aDebugStream(__())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream(System.MemoryExtensions.AsSpan("_")), __ => aDebugStream(__())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream(System.MemoryExtensions.AsSpan("_A")), __ => aDebugStream(__())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream(System.MemoryExtensions.AsSpan("A")), __ => aDebugStream(__())),
						Fail([])
					);
					mAssert.AreEquals(
						NotAB.StartParse(TestStream(System.MemoryExtensions.AsSpan("A_BA")), __ => aDebugStream(__())),
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
					
					var PosSignum = (-mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(aChar => aChar == '+', __ => (__.Span.Start, "miss +"), ComparePos, AreErrorsEqual))
					.Modify(() => +1);
					
					var NegSignum = (-mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(aChar => aChar == '-', __ => (__.Span.Start, "miss -"), ComparePos, AreErrorsEqual))
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
					
					var Expression = mParserGen.UndefParser<mStd.tEmpty, tChar, tInt32, tText>(ComparePos, AreErrorsEqual);
					
					Expression.Def(
						Number |
						P(mParserGen.Seq(Expression, __, Op, __, Expression))
						.Modify((a1, ___, aOp, ____, a2) => aOp(a1, a2))
					);
					
					tInt32 Eval(tText aExpr) {
						var X = Expression.StartParse(
							TestStream(System.MemoryExtensions.AsSpan(aExpr)),
							__ => aDebugStream(__())
						);
						return X.AssertNotError("").Result.Value;
					}
					
					mAssert.AreEquals(Eval("1"), 1, AreEqual_Int);
					mAssert.AreEquals(Eval("-2"), -2, AreEqual_Int);
					mAssert.AreEquals(Eval("(3+4)"), 7, AreEqual_Int);
					mAssert.AreEquals(Eval("( 5 - 6)"), -1, AreEqual_Int);
					mAssert.AreEquals(Eval("( 5 --6)"), 11, AreEqual_Int);
					mAssert.AreEquals(Eval("( 7 * (8 / 4))"), 14, AreEqual_Int);
					mAssert.AreEquals(Eval("( 7 * (8 / (4  -6)))"), -28, AreEqual_Int);
					
					mParserGen.tParser<mStd.tEmpty, tChar, tChar, tText>
					CharIn(
						tText aChars
					) => mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
						aChar => mStream.Stream(System.MemoryExtensions.AsSpan(aChars)).Any(__ => __ == aChar),
						__ => (__.Span.Start, $"miss one of [{aChars}]"),
						ComparePos,
						AreErrorsEqual
					);
					
					mParserGen.tParser<mStd.tEmpty, tChar, tText, tText>
					Token(
						tText aToken
					) {
						var Parser = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
							__ => __ == aToken[0],
							__ => (__.Span.Start, ""),
							ComparePos,
							AreErrorsEqual
						).Modify(aChar => "" + aChar);
						foreach (var Char in aToken[1..]) {
							Parser = mParserGen.Seq(
								Parser,
								mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
									aChar => aChar == Char,
									__ => (__.Span.Start, "miss {Char}"),
									ComparePos,
									AreErrorsEqual
								)
							).Modify(__ => __.Item1 + __.Item2);
						}
						return Parser;
					}
					
					mParserGen.tParser<mStd.tEmpty, tChar, tOut, tText>
					P<tOut>(
						mParserGen.tParser<mStd.tEmpty, tChar, tOut, tText> aParser
					) => mParserGen.Seq(Token("("), __, aParser, __, Token(")")).Modify(__ => __.Item3);
				}
			)
		]
	);
}
