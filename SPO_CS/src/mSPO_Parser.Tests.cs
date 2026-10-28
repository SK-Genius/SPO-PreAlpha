#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mFS.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mSpan.cs
#:ref Common/mStream.cs
#:ref Common/mTreeMap.cs
#:ref Common/mMath.cs
#:ref Common/mTextStream.cs
#:ref Common/mParserGen.cs
#:ref mTokenizer.cs
#:ref mSPO_AST.cs
#:ref mSPO_Desugar.cs
#:ref mSPO_Parser.cs
#:ref mIL_Parser.cs

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
			mTest.Test("LOAD",
				aDebug => {
					var Folder = mFS.CWD() / "SignatureTests";
					var Source = Folder.GetFile("Test.SPO").Path.ToText();
					
					var Files = mTreeMap.Tree<tText, tText>((a, b) => tText.CompareOrdinal(a, b).Sign(), [])
					.Set(Folder.GetFile("Int.SIG").Path.ToText(), "\n§INT\n")
					.Set(Folder.GetFile("nested/Pair.SIG").Path.ToText(), "[[§LOAD \"../Int.SIG\"], [§LOAD \"../Int.SIG\"]]\n");
					
					var FileContext = new mTokenizer.tFileContext(aFile => Files.TryGet(aFile.Path.ToText()).AssertNotEmpty());
					
					var Type = mSPO_Parser.Type.TryParseText("[§LOAD \"nested/Pair.SIG\"]", Source, __ => aDebug(__()), FileContext).ElseThrow();
					
					mAssert.IsTrue(Type is mSPO_AST.tTupleTypeNode<tSpan> Tuple && Tuple.ItemTypes.All(__ => __ is mSPO_AST.tIntTypeNode<tSpan>));
					mAssert.IsTrue(mSPO_Parser.Type.TryParseText("[\n§LOAD \"Int.SIG\"\n]", Source, _ => {}, FileContext).ElseThrow() is mSPO_AST.tIntTypeNode<tSpan>);
					

				}
			),
			mTest.Test("LOAD paths",
				aDebug => {
					var File = mFS.CWD().GetFile("Signature Tests/Int #1.sig");
					var FileContext = new mTokenizer.tFileContext(aFile => {
						mAssert.IsTrue(aFile.IsSameFile(File));
						return "§INT";
					});
					foreach (var Source in new[] { "", "Test.SPO", mFS.CWD().GetFile("Test.SPO").Path.ToText() }) {
						foreach (var Target in new[] { "Signature Tests/Int #1.sig", File.Path.ToText() }) {
							var Type = mSPO_Parser.Type.TryParseText($"[§LOAD \"{Target}\"]", Source, _ => {}, FileContext).ElseThrow();
							mAssert.IsTrue(Type is mSPO_AST.tIntTypeNode<tSpan>);
							mAssert.AreEquals(Type.Pos.Start.Id, File.Path.ToText());
						}
					}

					foreach (var Target in System.OperatingSystem.IsWindows() ? new[] { "./Int #1.sig", "./INT #1.SIG" } : new[] { "./Int #1.sig" }) {
						var Result = mSPO_Parser.Type.TryParseText(
							$"[§LOAD \"{Target}\"]", File.Path.ToText(), _ => {}, FileContext
						);
						mAssert.IsFalse(Result.Match(out _, out var Error));
						mAssert.IsTrue(Error.Contains("recursive §LOAD"));
					}
				}
			),
			mTest.Test("Atoms",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Number.TryParseText("+1_234", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Literal.TryParseText("+1_234", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.TryParseText("+1_234", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Int(Span((1, 1), (1, 6)), 1234),
						mSPO_AST.AreEqual
					);
					
					mAssert.AreEquals(
						mSPO_Parser.Text.TryParseText("\"BLA\"", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Literal.TryParseText("\"BLA\"", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.TryParseText("\"BLA\"", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"),
						mSPO_AST.AreEqual
					);
					
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.TryParseText("BLA", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Id(Span((1, 1), (1, 3)), "BLA"),
						mSPO_AST.AreEqual
					);
				}
			),
			mTest.Test("Tuple",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.ExpressionInCall.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(+1_234, "BLA")
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							"""
							(1; "BLA")
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Pattern.TryParseText("(Xs; X)", "", __ => { aStreamOut(__()); }).ElseThrow(),
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
						mSPO_Parser.Pattern.TryParseText("12", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Int(Span((1, 1), (1, 2)), 12),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Pattern.TryParseText("x", "", __ => { aStreamOut(__()); }).ElseThrow(),
						mSPO_AST.Id(Span((1, 1), (1, 1)), "x"),
						mSPO_AST.AreEqual
					);
					mAssert.AreEquals(
						mSPO_Parser.Pattern.TryParseText("(12, x)", "", __ => { aStreamOut(__()); }).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							"""
							X §IS 12
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							x .* x
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							.sin x
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							x => x .* x
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
			mTest.Tests("Unnamed lambda arguments",
				mStream.Stream(
					[
						(
							mStd.File(),
							mStd.LineNr(),
							"=> 2 .* ...",
							"a0 => 2 .* a0"
						),
						(
							mStd.File(),
							mStd.LineNr(),
							"=> ... .+ ...",
							"(a0, a1) => a0 .+ a1"
						),
						(
							mStd.File(),
							mStd.LineNr(),
							"=> ((=> ...), ...)",
							"a0 => ((a0 => a0), a0)"
						),
						(
							mStd.File(),
							mStd.LineNr(),
							"=> (..., (=> ...), ...)",
							"(a0, a1) => (a0, (a0 => a0), a1)"
						),
					]
				).Map(
					((tText File, tInt32 LineNr, tText Expr1, tText Expr2) a) => mTest.Test(a.Expr1,
						aStreamOut => {
							static mSPO_AST.tExpressionNode<tSpan>
							ParseAndDesugar(
								tText aText,
								mStd.tAction<tText> aStreamOut
							) => mSPO_Parser.Expression.TryParseText(
								aText,
								"",
								__ => aStreamOut(__())
							).ElseThrow().DesugarExpression(
							).AssertNotError(
								__ => __.ErrorText
							);
							
							var Expected = ParseAndDesugar(a.Expr2, aStreamOut).ToText();
							for (var Index = 0; Index <= 9; Index += 1) {
								Expected = Expected.Replace($"_a{Index}", $"a{Index}");
							}
							mAssert.AreEquals(ParseAndDesugar(a.Expr1, aStreamOut).ToText(), Expected);
						},
						a.File,
						a.LineNr
					)
				).ToArrayList().ToArray()
			),
			mTest.Test("TypedMatch",
				aStreamOut => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(x € MyType) => x .* x
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							2 .< (4 .+ 3) < 3
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(a, b, (x, y, z)) => a .* z
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Expression.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							(1 #* a) => a
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Command.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							o := ((§TO_VAL o) .+ i) .
							
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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
						mSPO_Parser.Command.TryParseText(
							//        1         2         3         4         5        6          7         8
							//2345678901234567890123456789012345678901234567890123456789012345678901234567890
							"""
							{a: §DEF x, b: §DEF y} = {a: 1, b: 2}
							
							""",
							"",
							__ => { aStreamOut(__()); }
						).ElseThrow(),
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