#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
#:ref Common/mParserGen.cs
#:ref mVM_Type.cs
#:ref mTokenizer.cs
#:ref mSPO_AST.cs
#:ref mSPO_AST_Types.cs
#:ref mSPO_Parser.cs
#:ref mSPO_Desugar.cs

public static class
mSPO_AST_Types_Tests {
	#if true
	
	private const tInt32 cNoPos = 1;
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_AST_Types),
		[
			mTest.Test("BOOL desugars to singleton types",
				aDebugStream => {
					var Type = mSPO_Parser.Type.ParseText(
						"§BOOL",
						"",
						__ => aDebugStream(__())
					).DesugarType();
					mAssert.IsTrue(Type is mSPO_AST.tSetTypeNode<mSpan.tSpan<mTextStream.tPos>>);
					var DesugaredType = Type.AsVM_Type(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
					var ExpandedType = mSPO_Parser.Type.ParseText(
						"[§TRUE | §FALSE]",
						"",
						__ => aDebugStream(__())
					).AsVM_Type(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
					DesugaredType.IsSubType(ExpandedType, mStd.cEmpty).AssertNotError(__ => __);
					ExpandedType.IsSubType(DesugaredType, mStd.cEmpty).AssertNotError(__ => __);
				}
			),
			mTest.Test("Literals",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Int(cNoPos, 1).UpdateTypes(mStd.cEmpty),
						mVM_Type.Int()
					);
					mAssert.AreEquals(
						mSPO_AST.False(cNoPos).UpdateTypes(mStd.cEmpty),
						mVM_Type.False()
					);
				}
			),
			mTest.Test("Tuple",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Tuple(
							cNoPos,
							[
								mSPO_AST.Int(cNoPos, 1),
								mSPO_AST.True(cNoPos)
							]
						).UpdateTypes(mStd.cEmpty),
						mVM_Type.Tuple(
							[mVM_Type.Int(), mVM_Type.True()]
						)
					);
					mAssert.AreEquals(
						mSPO_AST.False(cNoPos).UpdateTypes(mStd.cEmpty),
						mVM_Type.False()
					);
				}
			),
			mTest.Test("Is",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Is(
							cNoPos,
							mSPO_AST.Int(cNoPos, 1),
							mSPO_AST.Pattern(
								cNoPos,
								mSPO_AST.Int(cNoPos, 1),
								mStd.cEmpty
							)
						).UpdateTypes(
							mStd.cEmpty
						),
						mVM_Type.Bool()
					);
				}
			),
			mTest.Test("Match cases consume the remaining union type",
				aDebugStream => {
					var Match = (mSPO_AST.tIfMatchNode<mSpan.tSpan<mTextStream.tPos>>)mSPO_Parser.Expression.ParseText(
						"""
						§IF X MATCH {
							() : 0
							§DEF N : N
						}
						""",
						"",
						__ => aDebugStream(__())
					);
					mAssert.AreEquals(
						Match.UpdateTypes(
							mStream.Stream(
								mSPO_AST_Types.ScopeItem(
									"_X",
									mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Int())
								)
							)
						),
						mVM_Type.Int()
					);
					mAssert.AreEquals(
						Match.Cases.Skip(1).TryFirst().AssertNotEmpty().Pattern.TypeAnnotation.AssertNotEmpty(),
						mVM_Type.Int()
					);
				}
			),
			mTest.Test("Match must be exhaustive",
				aDebugStream => {
					var Match = mSPO_Parser.Expression.ParseText(
						"""
						§IF X MATCH {
							() : 0
						}
						""",
						"",
						__ => aDebugStream(__())
					);
					mAssert.IsFalse(
						Match.UpdateTypes(
							mStream.Stream(
								mSPO_AST_Types.ScopeItem(
									"_X",
									mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Int())
								)
							)
						).Match(out _, out _)
					);
				}
			),
			mTest.Tests("Split match type",
				mStream.Stream(
					[
						(mStd.File(), mStd.LineNr(), "[[[] | §INT]; §TRUE]", "((); _)", "[[]; §TRUE]", "[§INT; §TRUE]"),
						(mStd.File(), mStd.LineNr(), "[#Some §INT | #None []]", "(#Some _)", "[#Some §INT]", "[#None []]"),
						(mStd.File(), mStd.LineNr(), "§INT", "1", "§INT", "§INT"),
						(mStd.File(), mStd.LineNr(), "§INT", "(_ & §TRUE)", "§INT", "§INT"),
						(mStd.File(), mStd.LineNr(), "[< Field: [§INT | []] >]", "{ Missing: _ }", "", "[< Field: [§INT | []] >]"),
						(
							mStd.File(),
							mStd.LineNr(),
							"[§RECURSIVE RecursiveMatch [[]| [RecursiveMatch; §INT]]]",
							"(_; _)",
							"[[§RECURSIVE RecursiveMatch [[]| [RecursiveMatch; §INT]]]; §INT]",
							"[]"
						),
					]
				).Map(
					((tText File, tInt32 LineNr, tText Type, tText Pattern, tText PatternTypeUsed, tText RemainingType) a) => mTest.Test($"{a.Type} : {a.Pattern}",
						aDebugStream => {
							var Type = mSPO_Parser.Type.ParseText(a.Type, "", __ => aDebugStream(__()))
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var PatternType = mSPO_Parser.Pattern.ParseText(a.Pattern, "", __ => aDebugStream(__()));
							
							var PatternTypeUsed = a.PatternTypeUsed == ""
							? mMaybe.None<mVM_Type.tType>()
							: mSPO_Parser.Type.ParseText(a.PatternTypeUsed, "", __ => aDebugStream(__()))
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var RemainingType = a.RemainingType == ""
							? mMaybe.None<mVM_Type.tType>()
							: mSPO_Parser.Type.ParseText(a.RemainingType, "", __ => aDebugStream(__()))
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var Coverage = Type.SplitForPatternType(PatternType);
							
							mAssert.IsTrue(
								Coverage.Matched.Eq(PatternTypeUsed, (a, b) => a == b),
								() => $"matched '{Coverage.Matched}' instead of '{PatternTypeUsed}'"
							);
							
							mAssert.IsTrue(
								Coverage.Remaining.Eq(RemainingType, (a, b) => a == b),
								() => $"remaining '{Coverage.Remaining}' instead of '{RemainingType}'"
							);
						},
						a.File,
						a.LineNr
					)
				).ToArrayList().ToArray()
			),
			mTest.Test("Lambda",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Lambda(
							cNoPos,
							mStd.cEmpty,
							mSPO_AST.Pattern(
								cNoPos,
								mSPO_AST.PrefixPattern(
									cNoPos,
									"_Bla...",
									mSPO_AST.Pattern(
										cNoPos,
										mSPO_AST.FreeIdPattern(cNoPos, "a"),
										mMaybe.Some(
											(mSPO_AST.tExpressionNode<tInt32>)mSPO_AST.SetType(
												cNoPos,
												mStream.Stream<mSPO_AST.tTypeNode<tInt32>>(
													mSPO_AST.False(cNoPos),
													mSPO_AST.True(cNoPos)
												)
											)
										)
									)
								),
								mStd.cEmpty
							),
							mSPO_AST.Id(cNoPos, "a")
						).UpdateTypes(mStd.cEmpty),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix("_Bla...", mVM_Type.Bool()),
							mVM_Type.Bool()
						)
					);
					
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							"(#Bla (§DEF a € §BOOL)) => a",
							"",
							__ => { aDebugStream(__()); }
						).DesugarExpression(
						).AssertNotError(
							__ => __.ErrorText
						).UpdateTypes(mStd.cEmpty),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix("_Bla...", mVM_Type.Bool()),
							mVM_Type.Bool()
						)
					);
					
					mAssert.AreEquals(
						mSPO_AST.False(cNoPos).UpdateTypes(mStd.cEmpty),
						mVM_Type.False()
					);
				}
			),
			mTest.Tests("Types",
				mStream.Stream<(tText FileLine, tText Code, mVM_Type.tType Type)>(
					[
						(
							mStd.FileLine(),
							"()",
							mVM_Type.Empty()
						),
						(
							mStd.FileLine(),
							"§TRUE",
							mVM_Type.True()
						),
						(
							mStd.FileLine(),
							"§FALSE",
							mVM_Type.False()
						),
						(
							mStd.FileLine(),
							"0",
							mVM_Type.Int()
						),
						(
							mStd.FileLine(),
							"(1, §TRUE)",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.True()])
						),
						(
							mStd.FileLine(),
							"(1, (2, 3), 4)",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]), mVM_Type.Int()])
						),
						(
							mStd.FileLine(),
							"((1, 2), 3)",
							mVM_Type.Tuple([mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]), mVM_Type.Int()])
						),
						(
							mStd.FileLine(),
							"(1, (2, 3))",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()])])
						),
						(
							mStd.FileLine(),
							"#Bla 1",
							mVM_Type.Prefix("_Bla...", mVM_Type.Int())
						),
						(
							mStd.FileLine(),
							"{A: 1, B: 2, C: 3}",
							mVM_Type.Record([("_A", mVM_Type.Int()), ("_B", mVM_Type.Int()), ("_C", mVM_Type.Int())])
						),
					]
				).Map(
					a => {
						var Groups = System.Text.RegularExpressions.Regex.Match(a.FileLine, @"^(.*)\:(\d+)$").Groups;
						var FilePath = Groups[1].Value;
						var LineNr = tInt32.Parse(Groups[2].Value);
						
						return mTest.Test(a.Code,
							aDebugStream => {
								mAssert.AreEquals(
									mSPO_Parser.Expression.ParseText(
										a.Code,
										"",
										__ => { aDebugStream(__()); }
									).UpdateTypes(mStd.cEmpty),
									a.Type
								);
							},
							FilePath,
							LineNr
						);
					}
				).ToArrayList(
				).ToArray(
				)
			),
		]
	);
	
	#endif
}
