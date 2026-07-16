#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mSpan.cs
#:ref Common/mStream.cs
#:ref Common/mTextStream.cs
#:ref Common/mParserGen.cs
#:ref mTokenizer.cs
#:ref mSPO_AST.cs
#:ref mSPO_Parser.cs

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO_Parser_Tests {
	private static tSpan
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
		nameof(mSPO_Parser),
		[
			mTest.Test("Atoms",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Number.ParseText("+1_234", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Literal.ParseText("+1_234", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.ParseText("+1_234", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
						mSPO_AST.AreEqual
					);
					
					mAssert.AreEquals(
						mSPO_Parser.Text.ParseText("\"BLA\"", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Literal.ParseText("\"BLA\"", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.ParseText("\"BLA\"", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
						mSPO_AST.AreEqual
					);
					
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.ParseText("BLA", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Id(Span((1, 1), (1, 3)), "BLA"),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Tuple",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(+1_234, "BLA")
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Tuple(
							Span((1, 1), (1, 15)),
							[
								mSPO_AST.Int(Span((1, 2), (1, 7)), 1234),
								mSPO_AST.Text(Span((1, 10), (1, 14)), "BLA")
							]
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Pair",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							"""
							(1; "BLA")
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Pair(
							Span((1, 1), (1, 10)),
							mSPO_AST.Int(Span((1, 2), (1, 2)), 1),
							mSPO_AST.Text(Span((1, 5), (1, 9)), "BLA")
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("MatchPair",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Pattern.ParseText("(Xs; X)", "", __ => { aStreamOut(__()); }),
						mSPO_AST.PairPattern(
							Span((1, 1), (1, 7)),
							mSPO_AST.Id(Span((1, 2), (1, 3)), "Xs"),
							mSPO_AST.Id(Span((1, 6), (1, 6)), "X")
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Match1",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Pattern.ParseText("12", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Int(Span((1, 1), (1, 2)), 12),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Pattern.ParseText("x", "", __ => { aStreamOut(__()); }),
						mSPO_AST.Id(Span((1, 1), (1, 1)), "x"),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Pattern.ParseText("(12, x)", "", __ => { aStreamOut(__()); }),
						mSPO_AST.TuplePattern(
							Span((1, 1), (1, 7)),
							mStream.Stream<mSPO_AST.tPatternNode<tSpan>>(
								[
									mSPO_AST.Int(Span((1, 2), (1, 3)), 12),
									mSPO_AST.Id(Span((1, 6), (1, 6)), "x"),
								]
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Is",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							"""
							X §IS 12
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Is(
							Span((1, 1), (1, 8)),
							mSPO_AST.Id(Span((1, 1), (1, 1)), "X"),
							mSPO_AST.Int(Span((1, 7), (1, 8)), 12)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("FunctionCall",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							x .* x
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Call(
							Span((1, 1), (1, 6)),
							mSPO_AST.Id(Span((1, 1), (1, 6)), "...*..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 6)),
								[
									mSPO_AST.Id(Span((1, 1), (1, 1)), "x"),
									mSPO_AST.Id(Span((1, 6), (1, 6)), "x")
								]
							)
						),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							.sin x
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Call(
							Span((1, 1), (1, 6)),
							mSPO_AST.Id(Span((1, 1), (1, 6)), "sin..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 6)),
								[
									mSPO_AST.Id(Span((1, 6), (1, 6)), "x")
								]
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Lambda",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							x => x .* x
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 11)),
							mStd.cEmpty,
							mSPO_AST.Id(Span((1, 1), (1, 1)), "x"),
							mSPO_AST.Call(
								Span((1, 6), (1, 11)), 
								mSPO_AST.Id(Span((1, 6), (1, 11)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 6), (1, 11)), 
									[
										mSPO_AST.Id(Span((1, 6), (1, 6)), "x"),
										mSPO_AST.Id(Span((1, 11), (1, 11)), "x")
									]
								)
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("TypedMatch",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(x € MyType) => x .* x
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 22)),
							mStd.cEmpty,
							mSPO_AST.Pattern(
								Span((1, 2), (1, 11)),
								mSPO_AST.Id(Span((1, 2), (1, 2)), "x"),
								mSPO_AST.Id(Span((1, 6), (1, 11)), "MyType")
							),
							mSPO_AST.Call(
								Span((1, 17), (1, 22)),
								mSPO_AST.Id(Span((1, 17), (1, 22)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 17), (1, 22)),
									[
										mSPO_AST.Id(Span((1, 17), (1, 17)), "x"),
										mSPO_AST.Id(Span((1, 22), (1, 22)), "x")
									]
								)
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Expression",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							2 .< (4 .+ 3) < 3
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Call(
							Span((1, 1), (1, 17)),
							mSPO_AST.Id(Span((1, 1), (1, 17)), "...<...<..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 17)),
								[
									mSPO_AST.Int(Span((1, 1), (1, 1)), 2),
									mSPO_AST.Call(
										Span((1, 7), (1, 12)),
										mSPO_AST.Id(Span((1, 7), (1, 12)), "...+..."),
										mSPO_AST.Tuple(
											Span((1, 7), (1, 12)),
											[
												mSPO_AST.Int(Span((1, 7), (1, 7)), 4),
												mSPO_AST.Int(Span((1, 12), (1, 12)), 3)
											]
										)
									),
									mSPO_AST.Int(Span((1, 17), (1, 17)), 3)
								]
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("NestedMatch",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(a, b, (x, y, z)) => a .* z
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 27)),
							mStd.cEmpty,
							mSPO_AST.TuplePattern(
								Span((1, 1), (1, 17)),
								mStream.Stream<mSPO_AST.tPatternNode<tSpan>>(
									[
										mSPO_AST.Id(Span((1, 2), (1, 2)), "a"),
										mSPO_AST.Id(Span((1, 5), (1, 5)), "b"),
										mSPO_AST.TuplePattern(
											Span((1, 8), (1, 16)),
											mStream.Stream<mSPO_AST.tPatternNode<tSpan>>(
												[
													mSPO_AST.Id(Span((1, 9), (1, 9)), "x"),
													mSPO_AST.Id(Span((1, 12), (1, 12)), "y"),
													mSPO_AST.Id(Span((1, 15), (1, 15)), "z"),
												]
											)
										),
									]
								)
							),
							mSPO_AST.Call(
								Span((1, 22), (1, 27)),
								mSPO_AST.Id(Span((1, 22), (1, 27)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 22), (1, 27)),
									[
										mSPO_AST.Id(Span((1, 22), (1, 22)), "a"),
										mSPO_AST.Id(Span((1, 27), (1, 27)), "z")
									]
								)
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("PrefixMatch",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(1 #* a) => a
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 13)),
							mStd.cEmpty,
							mSPO_AST.PrefixPattern(
								Span((1, 1), (1, 8)),
								"_...*...",
								mSPO_AST.TuplePattern(
									Span((1, 1), (1, 8)),
									mStream.Stream<mSPO_AST.tPatternNode<tSpan>>(
										[
											mSPO_AST.Int(Span((1, 2), (1, 2)), 1),
											mSPO_AST.Id(Span((1, 7), (1, 7)), "a"),
										]
									)
								)
							),
							mSPO_AST.Id(Span((1, 13), (1, 13)), "a")
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("MethodCall",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Command.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							o := ((§TO_VAL o) .+ i) .
							
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.MethodCallStatement(
							Span((1, 1), (1, 25)),
							mSPO_AST.Id(Span((1, 1), (1, 1)), "o"),
							mStream.Stream(
								mSPO_AST.MethodCall(
									Span((1, 4), (1, 23)),
									mSPO_AST.Id(Span((1, 4), (1, 23)), "=..."),
									mSPO_AST.Call(
										Span((1, 7), (1, 22)),
										mSPO_AST.Id(Span((1, 7), (1, 22)), "...+..."),
										mSPO_AST.Tuple(
											Span((1, 7), (1, 22)), 
											[
												mSPO_AST.VarToVal(
													Span((1, 8), (1, 16)),
													mSPO_AST.Id(Span((1, 16), (1, 16)), "o")
												),
												mSPO_AST.Id(Span((1, 22), (1, 22)), "i")
											]
										)
									),
									mStd.cEmpty
								)
							)
						),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Record",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Command.ParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							{a: §DEF x, b: §DEF y} = {a: 1, b: 2}
							
							""",
							"",
							__ => { aStreamOut(__()); }
						),
						mSPO_AST.Def(
							Span((1, 1), (1, 37)),
							mSPO_AST.RecordPattern(
								Span((1, 1), (1, 22)),
								mStream.Stream<(mSPO_AST.tIdNode<tSpan> Key, mSPO_AST.tPatternNode<tSpan> Pattern)>(
									[
										(
											mSPO_AST.Id(Span((1, 2), (1, 2)), "a"),
											mSPO_AST.FreeIdPattern(Span((1, 5), (1, 10)), "x")
										), (
											mSPO_AST.Id(Span((1, 13), (1, 13)), "b"),
											mSPO_AST.FreeIdPattern(Span((1, 16), (1, 21)), "y")
										)
									]
								)
							),
							mSPO_AST.Record(
								Span((1, 26), (1, 37)),
								[
									(mSPO_AST.Id(Span((1, 27), (1, 27)), "a"), mSPO_AST.Int(Span((1, 30), (1, 30)), 1)),
									(mSPO_AST.Id(Span((1, 33), (1, 33)), "b"), mSPO_AST.Int(Span((1, 36), (1, 36)), 2))
								]
							)
						),
						mSPO_AST.AreEqual
					);
				}
			)
		]
	);
}
