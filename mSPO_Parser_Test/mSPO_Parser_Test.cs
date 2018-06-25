﻿using tBool = System.Boolean;

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

using tResults = mList.tList<mStd.tAny>;

using tPos = mTextParser.tPos;
using tSpan = mStd.tSpan<mTextParser.tPos>;

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class mSPO_Parser_Test {
	//================================================================================
	private static tSpan Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	//================================================================================
	) => new tSpan {
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
		nameof(mSPO_Parser),
		mTest.Test(
			"Atoms",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Num.ParseText("+1_234", aStreamOut),
					(Span((1, 1), (1, 6)), mSPO_AST.Number(Span((1, 1), (1, 6)), 1234))
				);
				mStd.AssertEq(
					mSPO_Parser.Literal.ParseText("+1_234", aStreamOut),
					(Span((1, 1), (1, 6)), mSPO_AST.Number(Span((1, 1), (1, 6)), 1234))
				);
				mStd.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("+1_234", aStreamOut),
					(Span((1, 1), (1, 6)), mSPO_AST.Number(Span((1, 1), (1, 6)), 1234))
				);
				
				mStd.AssertEq(
					mSPO_Parser.String.ParseText("\"BLA\"", aStreamOut),
					(Span((1, 1), (1, 5)), mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"))
				);
				mStd.AssertEq(
					mSPO_Parser.Literal.ParseText("\"BLA\"", aStreamOut),
					(Span((1, 1), (1, 5)), mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"))
				);
				mStd.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("\"BLA\"", aStreamOut),
					(Span((1, 1), (1, 5)), mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"))
				);
				
				mStd.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("BLA", aStreamOut),
					(Span((1, 1), (1, 3)), mSPO_AST.Ident(Span((1, 1), (1, 3)), "BLA"))
				);
			}
		),
		mTest.Test(
			"Tuple",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.ExpressionInCall.ParseText("(+1_234, \"BLA\")", aStreamOut),
					(
						Span((1, 1), (1, 15)),
						mSPO_AST.Tuple(
							Span((1, 1), (1, 15)),
							mList.List<mSPO_AST.tExpressionNode<tPos>>(
								mSPO_AST.Number(Span((1, 2), (1, 7)), 1234),
								mSPO_AST.Text(Span((1, 10), (1, 14)), "BLA")
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Match1",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Match.ParseText("12", aStreamOut),
					(
						Span((1, 1), (1, 2)),
						mSPO_AST.Match(
							Span((1, 1), (1, 2)),
							mSPO_AST.Number(Span((1, 1), (1, 2)), 12),
							null
						)
					)
				);
				mStd.AssertEq(
					mSPO_Parser.Match.ParseText("x", aStreamOut),
					(
						Span((1, 1), (1, 1)),
						mSPO_AST.Match(
							Span((1, 1), (1, 1)),
							mSPO_AST.Ident(Span((1, 1), (1, 1)), "x"),
							null
						)
					)
				);
				mStd.AssertEq(
					mSPO_Parser.Match.ParseText("(12, x)", aStreamOut),
					(
						Span((1, 1), (1, 7)), 
						mSPO_AST.Match(
							Span((1, 1), (1, 7)), 
							mSPO_AST.MatchTuple(
								Span((1, 1), (1, 7)), 
								mList.List(
									mSPO_AST.Match(Span((1, 2), (1, 3)), mSPO_AST.Number(Span((1, 2), (1, 3)), 12), null),
									mSPO_AST.Match(Span((1, 6), (1, 6)), mSPO_AST.Ident(Span((1, 6), (1, 6)), "x"), null)
								)
							),
							null
						)
					)
				);
			}
		),
		mTest.Test(
			"FunctionCall",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText("x .* x", aStreamOut),
					(
						Span((1, 1), (1, 6)),
						mSPO_AST.Call(
							Span((1, 1), (1, 6)),
							mSPO_AST.Ident(Span((1, 1), (1, 6)), "...*..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 6)),
								mList.List<mSPO_AST.tExpressionNode<tPos>>(
									mSPO_AST.Ident(Span((1, 1), (1, 2)), "x"), // TODO: Span isn't correct ((1, 1), (1, 1))
									mSPO_AST.Ident(Span((1, 6), (1, 6)), "x")
								)
							)
						)
					)
				);
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText(".sin x", aStreamOut),
					(
						Span((1, 1), (1, 6)),
						mSPO_AST.Call(
							Span((1, 1), (1, 6)),
							mSPO_AST.Ident(Span((1, 2), (1, 6)), "sin..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 6)),
								mList.List<mSPO_AST.tExpressionNode<tPos>>(
									mSPO_AST.Ident(Span((1, 6), (1, 6)), "x")
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Lambda",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText("x => x .* x", aStreamOut),
					(
						Span((1, 1), (1, 11)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 11)),
							mSPO_AST.Match(
								Span((1, 1), (1, 2)),
								mSPO_AST.Ident(Span((1, 1), (1, 2)), "x"),
								null
							),
							mSPO_AST.Call(
								Span((1, 6), (1, 11)), 
								mSPO_AST.Ident(Span((1, 6), (1, 11)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 6), (1, 11)), 
									mList.List<mSPO_AST.tExpressionNode<tPos>>(
										mSPO_AST.Ident(Span((1, 6), (1, 7)), "x"),
										mSPO_AST.Ident(Span((1, 11), (1, 11)), "x")
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"TypedMatch",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText("(x € MyType) => x .* x", aStreamOut),
					(
						Span((1, 1), (1, 22)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 22)),
							mSPO_AST.Match(
								Span((1, 1), (1, 13)),
								mSPO_AST.Match(
									Span((1, 2), (1, 11)),
									mSPO_AST.Match(
										Span((1, 2), (1, 3)),
										mSPO_AST.Ident(Span((1, 2), (1, 3)), "x"),
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
									mList.List<mSPO_AST.tExpressionNode<tPos>>(
										mSPO_AST.Ident(Span((1, 17), (1, 18)), "x"),
										mSPO_AST.Ident(Span((1, 22), (1, 22)), "x")
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Expression",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText("2 .< (4 .+ 3) < 3", aStreamOut),
					(
						Span((1, 1), (1, 17)),
						mSPO_AST.Call(
							Span((1, 1), (1, 17)),
							mSPO_AST.Ident(Span((1, 1), (1, 17)), "...<...<..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 17)),
								mList.List<mSPO_AST.tExpressionNode<tPos>>(
									mSPO_AST.Number(Span((1, 1), (1, 1)), 2),
									mSPO_AST.Call(
										Span((1, 7), (1, 12)),
										mSPO_AST.Ident(Span((1, 7), (1, 12)), "...+..."),
										mSPO_AST.Tuple(
											Span((1, 7), (1, 12)),
											mList.List<mSPO_AST.tExpressionNode<tPos>>(
												mSPO_AST.Number(Span((1, 7), (1, 7)), 4),
												mSPO_AST.Number(Span((1, 12), (1, 12)), 3)
											)
										)
									),
									mSPO_AST.Number(Span((1, 17), (1, 17)), 3)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"NestedMatch",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText("(a, b, (x, y, z)) => a .* z", aStreamOut),
					(
						Span((1, 1), (1, 27)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 27)),
							mSPO_AST.Match(
								Span((1, 1), (1, 18)),
								mSPO_AST.MatchTuple(
									Span((1, 1), (1, 18)),
									mList.List(
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
												mList.List(
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
									mList.List<mSPO_AST.tExpressionNode<tPos>>(
										mSPO_AST.Ident(Span((1, 22), (1, 23)), "a"),
										mSPO_AST.Ident(Span((1, 27), (1, 27)), "z")
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"PrefixMatch",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Expression.ParseText("(1 #* a) => a", aStreamOut),
					(
						Span((1, 1), (1, 13)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 13)),
							mSPO_AST.Match(
								Span((1, 1), (1, 9)),
								mSPO_AST.MatchPrefix(
									Span((1, 1), (1, 9)),
									mSPO_AST.Ident(Span((1, 2), (1, 7)), "...*..."),
									mSPO_AST.Match(
										Span((1, 1), (1, 9)), 
										mSPO_AST.MatchTuple(
											Span((1, 1), (1, 9)),
											mList.List(
												mSPO_AST.Match(
													Span((1, 2), (1, 3)),
													mSPO_AST.Number(Span((1, 2), (1, 2)), 1),
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
						)
					)
				);
			}
		),
		mTest.Test(
			"MethodCall",
			aStreamOut => {
				mStd.AssertEq(
					mSPO_Parser.Command.ParseText("o := ((§TO_VAL o) .+ i) .\n", aStreamOut),
					(
						Span((1, 1), (1, 26)),
						mSPO_AST.MethodCallStatment(
							Span((1, 1), (1, 25)),
							mSPO_AST.Ident(Span((1, 1), (1, 2)), "o"),
							mList.List(
								mSPO_AST.MethodCall(
									Span((1, 4), (1, 24)),
									mSPO_AST.Ident(Span((1, 4), (1, 24)), "=..."),
									mSPO_AST.Call(
										Span((1, 7), (1, 22)),
										mSPO_AST.Ident(Span((1, 7), (1, 22)), "...+..."),
										mSPO_AST.Tuple(
											Span((1, 7), (1, 22)), 
											mList.List<mSPO_AST.tExpressionNode<tPos>>(
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
						)
					)
				);
			}
		)
	);
	
	[xArg("Atoms")]
	[xArg("Tuple")]
	[xArg("Match1")]
	[xArg("FunctionCall")]
	[xArg("Lambda")]
	[xArg("Expression")]
	[xArg("TypedMatch")]
	[xArg("NestedMatch")]
	[xArg("PrefixMatch")]
	[xArg("MethodCall")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}