//IMPORT mTest.cs
//IMPORT mSPO_Parser.cs

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
using tSpan = mSpan.tSpan<mTextStream.tPos>;

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class
mSPO_Parser_Test {
	
	private static tSpan
	Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	) => new tSpan {
		Start = {
			Ident = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		End = {
			Ident = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	};
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Parser),
		mTest.Test(
			"Atoms",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Number.ParseText("+1_234", "", a => aStreamOut(a())),
					mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.Literal.ParseText("+1_234", "", a => aStreamOut(a())),
					mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("+1_234", "", a => aStreamOut(a())),
					mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
					mSPO_AST.AreEqual
				);
				
				mAssert.AssertEq(
					mSPO_Parser.Text.ParseText("\"BLA\"", "", a => aStreamOut(a())),
					mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.Literal.ParseText("\"BLA\"", "", a => aStreamOut(a())),
					mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("\"BLA\"", "", a => aStreamOut(a())),
					mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
					mSPO_AST.AreEqual
				);
				
				mAssert.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("BLA", "", a => aStreamOut(a())),
					mSPO_AST.Ident(Span((1, 1), (1, 3)), "BLA"),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"Tuple",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("(+1_234, \"BLA\")", "", a => aStreamOut(a())),
					mSPO_AST.Tuple(
						Span((1, 1), (1, 15)),
						mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
							mSPO_AST.Int(Span((1, 2), (1, 7)), 1234),
							mSPO_AST.Text(Span((1, 10), (1, 14)), "BLA")
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"Match1",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Match.ParseText("12", "", a => aStreamOut(a())),
					mSPO_AST.Match(
						Span((1, 1), (1, 2)),
						mSPO_AST.Int(Span((1, 1), (1, 2)), 12),
						null
					),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.Match.ParseText("x", "", a => aStreamOut(a())),
					mSPO_AST.Match(
						Span((1, 1), (1, 1)),
						mSPO_AST.Ident(Span((1, 1), (1, 1)), "x"),
						null
					),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.Match.ParseText("(12, x)", "", a => aStreamOut(a())),
					mSPO_AST.Match(
						Span((1, 1), (1, 7)), 
						mSPO_AST.MatchTuple(
							Span((1, 1), (1, 7)), 
							mStream.Stream(
								mSPO_AST.Match(Span((1, 2), (1, 3)), mSPO_AST.Int(Span((1, 2), (1, 3)), 12), null),
								mSPO_AST.Match(Span((1, 6), (1, 6)), mSPO_AST.Ident(Span((1, 6), (1, 6)), "x"), null)
							)
						),
						null
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"FunctionCall",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText("x .* x", "", a => aStreamOut(a())),
					mSPO_AST.Call(
						Span((1, 1), (1, 6)),
						mSPO_AST.Ident(Span((1, 1), (1, 6)), "...*..."),
						mSPO_AST.Tuple(
							Span((1, 1), (1, 6)),
							mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
								mSPO_AST.Ident(Span((1, 1), (1, 1)), "x"),
								mSPO_AST.Ident(Span((1, 6), (1, 6)), "x")
							)
						)
					),
					mSPO_AST.AreEqual
				);
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText(".sin x", "", a => aStreamOut(a())),
					mSPO_AST.Call(
						Span((1, 1), (1, 6)),
						mSPO_AST.Ident(Span((1, 2), (1, 6)), "sin..."),
						mSPO_AST.Tuple(
							Span((1, 1), (1, 6)),
							mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
								mSPO_AST.Ident(Span((1, 6), (1, 6)), "x")
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"Lambda",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText("x => x .* x", "", a => aStreamOut(a())),
					mSPO_AST.Lambda(
						Span((1, 1), (1, 11)),
						mSPO_AST.Match(
							Span((1, 1), (1, 1)),
							mSPO_AST.Ident(Span((1, 1), (1, 1)), "x"),
							null
						),
						mSPO_AST.Call(
							Span((1, 6), (1, 11)), 
							mSPO_AST.Ident(Span((1, 6), (1, 11)), "...*..."),
							mSPO_AST.Tuple(
								Span((1, 6), (1, 11)), 
								mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
									mSPO_AST.Ident(Span((1, 6), (1, 6)), "x"),
									mSPO_AST.Ident(Span((1, 11), (1, 11)), "x")
								)
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"TypedMatch",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText("(x € MyType) => x .* x", "", a => aStreamOut(a())),
					mSPO_AST.Lambda(
						Span((1, 1), (1, 22)),
						mSPO_AST.Match(
							Span((1, 1), (1, 12)),
							mSPO_AST.Match(
								Span((1, 2), (1, 11)),
								mSPO_AST.Match(
									Span((1, 2), (1, 2)),
									mSPO_AST.Ident(Span((1, 2), (1, 2)), "x"),
									null
								),
								mSPO_AST.Ident(Span((1, 6), (1, 11)), "MyType")
							),
							null
						),
						mSPO_AST.Call(
							Span((1, 17), (1, 22)),
							mSPO_AST.Ident(Span((1, 17), (1, 22)), "...*..."),
							mSPO_AST.Tuple(
								Span((1, 17), (1, 22)),
								mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
									mSPO_AST.Ident(Span((1, 17), (1, 17)), "x"),
									mSPO_AST.Ident(Span((1, 22), (1, 22)), "x")
								)
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"Expression",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText("2 .< (4 .+ 3) < 3", "", a => aStreamOut(a())),
					mSPO_AST.Call(
						Span((1, 1), (1, 17)),
						mSPO_AST.Ident(Span((1, 1), (1, 17)), "...<...<..."),
						mSPO_AST.Tuple(
							Span((1, 1), (1, 17)),
							mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
								mSPO_AST.Int(Span((1, 1), (1, 1)), 2),
								mSPO_AST.Call(
									Span((1, 7), (1, 12)),
									mSPO_AST.Ident(Span((1, 7), (1, 12)), "...+..."),
									mSPO_AST.Tuple(
										Span((1, 7), (1, 12)),
										mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
											mSPO_AST.Int(Span((1, 7), (1, 7)), 4),
											mSPO_AST.Int(Span((1, 12), (1, 12)), 3)
										)
									)
								),
								mSPO_AST.Int(Span((1, 17), (1, 17)), 3)
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"NestedMatch",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText("(a, b, (x, y, z)) => a .* z", "", a => aStreamOut(a())),
					mSPO_AST.Lambda(
						Span((1, 1), (1, 27)),
						mSPO_AST.Match(
							Span((1, 1), (1, 17)),
							mSPO_AST.MatchTuple(
								Span((1, 1), (1, 17)),
								mStream.Stream(
									mSPO_AST.Match(
										Span((1, 2), (1, 2)),
										mSPO_AST.Ident(Span((1, 2), (1, 2)), "a"),
										null
									),
									mSPO_AST.Match(
										Span((1, 5), (1, 5)),
										mSPO_AST.Ident(Span((1, 5), (1, 5)), "b"),
										null
									),
									mSPO_AST.Match(
										Span((1, 8), (1, 16)),
										mSPO_AST.MatchTuple(
											Span((1, 8), (1, 16)),
											mStream.Stream(
												mSPO_AST.Match(
													Span((1, 9), (1, 9)),
													mSPO_AST.Ident(Span((1, 9), (1, 9)), "x"),
													null
												),
												mSPO_AST.Match(
													Span((1, 12), (1, 12)),
													mSPO_AST.Ident(Span((1, 12), (1, 12)), "y"),
													null
												),
												mSPO_AST.Match(
													Span((1, 15), (1, 15)),
													mSPO_AST.Ident(Span((1, 15), (1, 15)), "z"),
													null
												)
											)
										),
										null
									)
								)
							),
							null
						),
						mSPO_AST.Call(
							Span((1, 22), (1, 27)),
							mSPO_AST.Ident(Span((1, 22), (1, 27)), "...*..."),
							mSPO_AST.Tuple(
								Span((1, 22), (1, 27)),
								mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
									mSPO_AST.Ident(Span((1, 22), (1, 22)), "a"),
									mSPO_AST.Ident(Span((1, 27), (1, 27)), "z")
								)
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"PrefixMatch",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Expression.ParseText("(1 #* a) => a", "", a => aStreamOut(a())),
					mSPO_AST.Lambda(
						Span((1, 1), (1, 13)),
						mSPO_AST.Match(
							Span((1, 1), (1, 8)),
							mSPO_AST.MatchPrefix(
								Span((1, 1), (1, 8)),
								mSPO_AST.Ident(Span((1, 2), (1, 7)), "...*..."),
								mSPO_AST.Match(
									Span((1, 1), (1, 8)), 
									mSPO_AST.MatchTuple(
										Span((1, 1), (1, 8)),
										mStream.Stream(
											mSPO_AST.Match(
												Span((1, 2), (1, 2)),
												mSPO_AST.Int(Span((1, 2), (1, 2)), 1),
												null
											),
											mSPO_AST.Match(
												Span((1, 7), (1, 7)),
												mSPO_AST.Ident(Span((1, 7), (1, 7)), "a"),
												null
											)
										)
									),
									null
								)
							),
							null
						),
						mSPO_AST.Ident(Span((1, 13), (1, 13)), "a")
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"MethodCall",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Command.ParseText("o := ((§TO_VAL o) .+ i) .\n", "", a => aStreamOut(a())),
					mSPO_AST.MethodCallStatment(
						Span((1, 1), (1, 25)),
						mSPO_AST.Ident(Span((1, 1), (1, 1)), "o"),
						mStream.Stream(
							mSPO_AST.MethodCall(
								Span((1, 4), (1, 23)),
								mSPO_AST.Ident(Span((1, 4), (1, 23)), "=..."),
								mSPO_AST.Call(
									Span((1, 7), (1, 22)),
									mSPO_AST.Ident(Span((1, 7), (1, 22)), "...+..."),
									mSPO_AST.Tuple(
										Span((1, 7), (1, 22)), 
										mStream.Stream<mSPO_AST.tExpressionNode<tSpan>>(
											mSPO_AST.VarToVal(
												Span((1, 8), (1, 16)),
												mSPO_AST.Ident(Span((1, 16), (1, 16)), "o")
											),
											mSPO_AST.Ident(Span((1, 22), (1, 22)), "i")
										)
									)
								),
								null
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		),
		mTest.Test(
			"Record",
			aStreamOut => {
				mAssert.AssertEq(
					mSPO_Parser.Command.ParseText(
						//        1         2         3         4         5        6          7         8
						//2345678901234567890123456789012345678901234567890123456789012345678901234567890
						"{a: §DEF x, b: §DEF y} = {a: 1, b: 2}\n",
						"",
						a => aStreamOut(a())
					),
					mSPO_AST.Def(
						Span((1, 1), (1, 37)),
						mSPO_AST.Match(
							Span((1, 1), (1, 22)),
							mSPO_AST.MatchRecord(
								Span((1, 1), (1, 22)),
								mStream.Stream<(mSPO_AST.tIdentNode<tSpan> Key, mSPO_AST.tMatchNode<tSpan> Value)>(
									(
										mSPO_AST.Ident(Span((1, 2), (1, 2)), "a"),
										mSPO_AST.Match(Span((1, 5), (1, 10)), mSPO_AST.MatchFreeIdent(Span((1, 5), (1, 10)), "x"), null)
									), (
										mSPO_AST.Ident(Span((1, 13), (1, 13)), "b"),
										mSPO_AST.Match(Span((1, 16), (1, 21)), mSPO_AST.MatchFreeIdent(Span((1, 16), (1, 21)), "y"), null)
									)
								)
							),
							null
						),
						mSPO_AST.Record(
							Span((1, 26), (1, 37)),
							mStream.Stream<(mSPO_AST.tIdentNode<tSpan> Key, mSPO_AST.tExpressionNode<tSpan> Value)>(
								(mSPO_AST.Ident(Span((1, 27), (1, 27)), "a"), mSPO_AST.Int(Span((1, 30), (1, 30)), 1)),
								(mSPO_AST.Ident(Span((1, 33), (1, 33)), "b"), mSPO_AST.Int(Span((1, 36), (1, 36)), 2))
							)
						)
					),
					mSPO_AST.AreEqual
				);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("Atoms")]
	[xTestCase("Tuple")]
	[xTestCase("Match1")]
	[xTestCase("FunctionCall")]
	[xTestCase("Lambda")]
	[xTestCase("Expression")]
	[xTestCase("TypedMatch")]
	[xTestCase("NestedMatch")]
	[xTestCase("PrefixMatch")]
	[xTestCase("MethodCall")]
	[xTestCase("Record")]
	public static void _(tText a) {
		mAssert.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
