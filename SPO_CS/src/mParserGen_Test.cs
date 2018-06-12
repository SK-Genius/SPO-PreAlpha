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

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class mParserGen_Test {
	private static readonly mStd.tSpan<mStd.tEmpty> cTestSpan = default(mStd.tSpan<mStd.tEmpty>);
	//================================================================================
	private static mList.tList<(mStd.tSpan<mStd.tEmpty>, t)>
	TestStream<t>(
		params t[] a 
	//================================================================================
	) => mList.List(a).Map(_ => (cTestSpan, _));
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mParserGen),
		mTest.Test(
			"AtomParser",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				mStd.Assert(A.StartParse(TestStream('A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(A.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A.StartParse(TestStream('B', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...+...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'B', _ => "miss B");
				var AB = A + B;
				mStd.Assert(AB.StartParse(TestStream('A', 'B', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'B'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(AB.Parse(TestStream<tChar>(), aDebugStream, null).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...-...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(a => a == 'B', _ => "miss B");
				var AB = A - B;
				mStd.Assert(AB.StartParse(TestStream('A', 'B', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(AB.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"-...",
			aDebugStream => {
				var A = -mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "unexpected A");
				mStd.Assert(A.StartParse(TestStream('A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList<mStd.tEmpty>(default));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(A.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"n*...",
			aDebugStream => {
				var A3 = 3 * mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A");
				mStd.Assert(A3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A3.StartParse(TestStream('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('A', '_'));
				
				mStd.AssertNot(A3.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"-n*...",
			aDebugStream => {
				var A3 = -3 * mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A");
				mStd.Assert(A3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList<mStd.tEmpty>(default));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A3.StartParse(TestStream('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList<mStd.tEmpty>(default));
				mStd.AssertEq(RestStream, TestStream('A', '_'));
				
				mStd.AssertNot(A3.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...*n",
			aDebugStream => {
				var A3 = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A") * 3;
				mStd.Assert(A3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(A3.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A3.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...|...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'B', _ => "miss B");
				var AB = A | B;
				mStd.Assert(AB.StartParse(TestStream('A', 'B'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A'));
				mStd.AssertEq(RestStream, TestStream('B'));
				
				mStd.Assert(AB.StartParse(TestStream('B', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'B'));
				mStd.AssertEq(RestStream, TestStream('A'));
				
				mStd.AssertNot(AB.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(AB.StartParse(TestStream('_', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...[m, n]",
			aDebugStream => {
				var A2_3 = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A")[2, 3];
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A2_3.StartParse(TestStream('A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('A', '_'));
				
				mStd.AssertNot(A2_3.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_3.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"...[n, null]",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A")[2, null];
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'A', 'A', 'A'));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(A2_.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"....ModifyList(...=>...)",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A")[2, null]
				.ModifyList(
					a => {
						var Tail = a;
						var N = 0;
						while (Tail.GetHeadTail(out char Head, out Tail)) {
							N += 1;
						}
						return mParserGen.ResultList(cTestSpan, N);
					}
				);
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 2));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 5));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(A2_.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"....ModifyList(a => a.Reduce(...))",
			aDebugStream => {
				var A2_ = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A")[2, null]
				.ModifyList(a => a.Reduce(0, (int aCount, tChar aElem) => aCount + 1));
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 2));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(A2_.StartParse(TestStream('A', 'A', 'A', 'A', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 5));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.AssertNot(A2_.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(A2_.StartParse(TestStream('A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"~...",
			aDebugStream => {
				var A = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'A', _ => "miss A");
				var B = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == 'B', _ => "miss B");
				var AB = (A + B).ModifyList(a => mParserGen.ResultList(cTestSpan, "AB"));
				var nAB = ~AB;
				mStd.Assert(nAB.StartParse(TestStream('A', 'B', '_'), aDebugStream).Match(out var Result, out var RestStream, out var ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, "AB"));
				mStd.AssertEq(RestStream, TestStream('_'));
				
				mStd.Assert(nAB.StartParse(TestStream('B', 'A', 'A', 'B', 'A', '_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertEq(Result, mParserGen.ResultList(cTestSpan, 'B', 'A', "AB"));
				mStd.AssertEq(RestStream, TestStream('A', '_'));
				
				mStd.AssertNot(nAB.StartParse(TestStream<tChar>(), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('_'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('_', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
				mStd.AssertNot(nAB.StartParse(TestStream('A', '_', 'B', 'A'), aDebugStream).Match(out Result, out RestStream, out ErrorList));
			}
		),
		mTest.Test(
			"Eval('MathExpr')",
			aDebugStream => {
				var CharIn = mStd.Func(
					(tText aChars) => mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
						(tChar aChar) => {
							foreach (var Char in aChars) {
								if (Char == aChar) { return true; }
							}
							return false;
						},
						a => $"miss one of [{aChars}]"
					)
				);
				
				var Token = mStd.Func(
					(tText aTocken) => {
						var Parser = mParserGen.EmptyParser<mStd.tEmpty, tChar, tText>();
						foreach (var Char in aTocken) {
							Parser += mParserGen.AtomParser<mStd.tEmpty, tChar, tText>(
								aChar => aChar == Char,
								a => $"miss {Char}"
							);
						}
						return Parser;
					}
				);
				
				var _ = -CharIn(" \t");
				var __ = -_[0, null];
				
				var P = mStd.Func((mParserGen.tParser<mStd.tEmpty, tChar, tText> aParser) => (-Token("(") -__ +aParser -__ -Token(")")));
				
				var Digit = CharIn("0123456789")
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan, tChar a) => (tInt32)a - (tInt32)'0');
				
				var Nat = Digit[1, null]
				.ModifyList(aDigits => aDigits.Reduce(0, (tInt32 aNat, tInt32 aDigit) => aNat*10 + aDigit));
				
				var PosSignum = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == '+', a => $"miss +")
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan, tChar a) => +1);
				
				var NegSignum = mParserGen.AtomParser<mStd.tEmpty, tChar, tText>((tChar a) => a == '-', a => "miss -")
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan, tChar a) => -1);
				
				var Signum = (PosSignum | NegSignum);
				
				var Int = (+Signum +Nat)
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan, tInt32 aSignum, tInt32 aNat) => aSignum * aNat);
				
				var Number = Nat | Int;
				
				var OpAdd = (-Token("+"))
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan) => mStd.Func((tInt32 a1, tInt32 a2) => a1 + a2));
				
				var OpSub = (-Token("-"))
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan) => mStd.Func((tInt32 a1, tInt32 a2) => a1 - a2));
				
				var OpMul = (-Token("*"))
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan) => mStd.Func((tInt32 a1, tInt32 a2) => a1 * a2));
				
				var OpDiv = (-Token("/"))
				.Modify((mStd.tSpan<mStd.tEmpty> aSpan) => mStd.Func((tInt32 a1, tInt32 a2) => a1 / a2));
				
				var Op = OpAdd | OpSub | OpMul | OpDiv;
				
				var Expression = mParserGen.UndefParser<mStd.tEmpty, tChar, tText>();
				
				Expression.Def(
					Number |
					P(+Expression -__ +Op -__ +Expression)
					.Modify((mStd.tSpan<mStd.tEmpty> aSpan, tInt32 a1, mStd.tFunc<tInt32, tInt32, tInt32> aOp, tInt32 a2) => aOp(a1, a2))
				);
				
				var Eval = mStd.Func(
					(tText aExpr) => {
						var X = Expression.StartParse(
							TestStream(aExpr.ToCharArray()),
							aDebugStream
						);
						mStd.Assert(X.Match(out var Tuple, out var _));
						var (ResultList, Rest) = Tuple;
						
						mStd.Assert(ResultList.Match(out tInt32 Result));
						
						return Result;
					}
				);
				
				mStd.AssertEq(Eval("1"), 1);
				mStd.AssertEq(Eval("-2"), -2);
				mStd.AssertEq(Eval("(3+4)"), 7);
				mStd.AssertEq(Eval("( 5 - 6)"), -1);
				mStd.AssertEq(Eval("( 5 --6)"), 11);
				mStd.AssertEq(Eval("( 7 * (8 / 4))"), 14);
				mStd.AssertEq(Eval("( 7 * (8 / (4  -6)))"), -28);
			}
		)
	);
	
	[xArg("AtomParser")]
	[xArg("...+...")]
	[xArg("...-...")]
	[xArg("-...")]
	[xArg("n*...")]
	[xArg("-n*...")]
	[xArg("...*n")]
	[xArg("...|...")]
	[xArg("...[m, n]")]
	[xArg("...[n, null]")]
	[xArg("....ModifyList(...=>...)")]
	[xArg("....ModifyList(a => a.Reduce(...))")]
	[xArg("~...")]
	[xArg("Eval('MathExpr')")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
