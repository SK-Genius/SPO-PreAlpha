﻿//IMPORT mTest.cs
//IMPORT mParserGen.cs

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

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class mParserGen_Test {
	
	private static readonly mStd.tSpan<mStd.tEmpty> cTestSpan = default;
	
	//================================================================================
	private static mStream.tStream<(mStd.tSpan<mStd.tEmpty>, t)>
	TestStream<t>(
		params t[] a 
	//================================================================================
	) => mStream.Stream(a).Map(_ => (cTestSpan, _));
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mParserGen),
		mTest.Test(
			"AtomParser",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				mStd.Assert(A.StartParse(TestStream('A', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, 'A'), TestStream('_')));
				
				mStd.AssertNot(A.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A.StartParse(TestStream('B', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"...+...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'B', _ => "miss B");
				var AB = mParserGen.Seq(A, B);
				mStd.Assert(AB.StartParse(TestStream('A', 'B', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, ('A', 'B')), TestStream('_')));
				
				mStd.AssertNot(AB.Parse(TestStream<tChar>(), aDebugStream, null).Match(out Result, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"...-...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'B', _ => "miss B");
				var AB = A +-B;
				mStd.Assert(AB.StartParse(TestStream('A', 'B', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, 'A'), TestStream('_')));
				
				mStd.AssertNot(AB.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"-...",
			aDebugStream => {
				var A = -mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "unexpected A");
				mStd.Assert(A.StartParse(TestStream('A', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStd.cEmpty), TestStream('_')));
				
				mStd.AssertNot(A.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"...|...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'B', _ => "miss B");
				var AB = A | B;
				mStd.Assert(AB.StartParse(TestStream('A', 'B'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, 'A'), TestStream('B')));
				
				mStd.Assert(AB.StartParse(TestStream('B', 'A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, 'B'), TestStream('A')));
				
				mStd.AssertNot(AB.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"...[m, n]",
			aDebugStream => {
				var A2_3 = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A")[2, 3];
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A')), TestStream('_')));
				
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('_')));
				
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('_')));
				
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('A', '_')));
				
				mStd.AssertNot(A2_3.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"...[0, null]",
			aDebugStream => {
				var A0_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A")[0, null];
				mStd.Assert(A0_.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A')), TestStream('_')));
				
				mStd.Assert(A0_.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream<tChar>()), TestStream('_')));
			}
		),
		mTest.Test(
			"...[n, null]",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A")[2, null];
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A')), TestStream('_')));
				
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, mStream.Stream('A', 'A', 'A')), TestStream('_')));
				
				mStd.AssertNot(A2_.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"....Modify(...=>...)",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A")[2, null]
				.Modify(aChars => aChars.Reduce(0, (aCount, aChar) => aCount + 1));
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, 2), TestStream('_')));
				
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, 5), TestStream('_')));
				
				mStd.AssertNot(A2_.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"~...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'B', _ => "miss B");
				var AB = mParserGen.Seq(A, B).Modify(a => "AB");
				var nAB = ~AB;
				mStd.Assert(nAB.StartParse(TestStream('A', 'B', '_'), aDebugStream).Match(out var Result, out var ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, (mStream.Stream<tChar>(), "AB")), TestStream('_')));
				
				mStd.Assert(nAB.StartParse(TestStream('B', 'A', 'A', 'B', 'A', '_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertEq(Result, ((cTestSpan, (mStream.Stream('B', 'A'), "AB")), TestStream('A', '_')));
				
				mStd.AssertNot(nAB.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('A'), aDebugStream).Match(out Result, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('A', '_', 'B', 'A'), aDebugStream).Match(out Result, out ErrorList));
			}
		),
		mTest.Test(
			"Eval('MathExpr')",
			aDebugStream => {
				var _ = -CharIn(" \t");
				var __ = -_[0, null];
				
				var Digit = CharIn("0123456789")
				.Modify(aChar => (tInt32)aChar - (tInt32)'0');
				
				var Nat = Digit[1, null]
				.Modify(aDigits => aDigits.Reduce(0, (aNat, aDigit) => aNat*10 + aDigit));
				
				var PosSignum = (-mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(aChar => aChar == '+', a => $"miss +"))
				.Modify(() => +1);
				
				var NegSignum = (-mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(aChar => aChar == '-', a => "miss -"))
				.Modify(() => -1);
				
				var Signum = (PosSignum | NegSignum);
				
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
				
				var Expression = mParserGen.UndefParser<mStd.tEmpty, tChar, tInt32, tText>();
				
				Expression.Def(
					Number |
					P(mParserGen.Seq(Expression, __, Op, __, Expression))
					.Modify((a1, ___, aOp, ____, a2) => aOp(a1, a2))
				);
				
				var Eval = mStd.Func(
					(tText aExpr) => {
						var X = Expression.StartParse(
							TestStream(aExpr.ToCharArray()),
							aDebugStream
						);
						mStd.Assert(X.Match(out var Result, out var _));
						return Result.Result.Value;
					}
				);
				
				mStd.AssertEq(Eval("1"), 1);
				mStd.AssertEq(Eval("-2"), -2);
				mStd.AssertEq(Eval("(3+4)"), 7);
				mStd.AssertEq(Eval("( 5 - 6)"), -1);
				mStd.AssertEq(Eval("( 5 --6)"), 11);
				mStd.AssertEq(Eval("( 7 * (8 / 4))"), 14);
				mStd.AssertEq(Eval("( 7 * (8 / (4  -6)))"), -28);
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
					a => $"miss one of [{aChars}]"
				);
				
				mParserGen.tParser<mStd.tEmpty, char, string, string>
				Token(
					tText aTocken
				) {
					var Parser = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
						a => a == aTocken[0],
						a => ""
					).Modify(aChar => "" + aChar);
					foreach (var Char in aTocken.Substring(1)) {
						Parser = mParserGen.Seq(
							Parser,
							mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
								aChar => aChar == Char,
								a => $"miss {Char}"
							)
						).Modify(a => a.Item1 + a.Item2);
					}
					return Parser;
				}
				
				mParserGen.tParser<mStd.tEmpty, tChar, tOut, tText>
				P<tOut>(
					mParserGen.tParser<mStd.tEmpty, tChar, tOut, tText> aParser
				) => mParserGen.Seq(Token("("), __, aParser, __, Token(")")).Modify(a => a.Item3);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("AtomParser")]
	[xTestCase("...+...")]
	[xTestCase("...-...")]
	[xTestCase("-...")]
	[xTestCase("...|...")]
	[xTestCase("...[m, n]")]
	[xTestCase("...[0, null]")]
	[xTestCase("...[n, null]")]
	[xTestCase("....Modify(...=>...)")]
	[xTestCase("~...")]
	[xTestCase("Eval('MathExpr')")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
