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
			mTest.Test("Generic values and signatures use the same type abstraction",
				aDebug => {
					var Expression = mSPO_Parser.Expression.ParseText(
						"[§GENERIC t [t => t]]",
						"",
						__ => aDebug(__())
					);
					var Kind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					var Value = Expression.AsVM_Value(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
					mAssert.IsTrue(Value.KindType().SameType(Kind));
					var Signature = Expression.AsVM_Type(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
					mAssert.IsTrue(Value.SameType(Signature));
					mAssert.IsFalse(mVM_Type.Free("x").IsSubType(Value, mStd.cEmpty).Match(out _, out _));
					mAssert.IsFalse(Value.IsSubType(mVM_Type.Free("x"), mStd.cEmpty).Match(out _, out _));
					mAssert.IsTrue(Signature.KindType().SameType(Kind));
					mAssert.IsTrue(Expression.TypeAnnotation.AssertNotEmpty().SameType(Kind));
					mAssert.IsFalse(Kind.IsSubType(mVM_Type.Type(), mStd.cEmpty).Match(out _, out _));
					var Scope = mStream.Stream(mSPO_AST_Types.ScopeItem("_T", Kind, Value));
					var Alias = mSPO_Parser.Expression.ParseText("T", "", __ => aDebug(__()));
					mAssert.IsTrue(Alias.AsVM_Type(Scope).AssertNotError(__ => __.ErrorText).SameType(Value));
					var Applied = mSPO_Parser.Expression.ParseText("[.T §INT]", "", __ => aDebug(__()))
						.AsVM_Type(Scope).AssertNotError(__ => __.ErrorText);
					mAssert.IsTrue(Applied.SameType(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int())));
					var Mono = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int());
					mAssert.IsFalse(Mono.IsSubType(Signature, mStd.cEmpty).Match(out _, out _));
					var A = mVM_Type.Free("a");
					var B = mVM_Type.Free("b");
					var Body = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Pair(A, B), A);
					var AB = mVM_Type.Generic(A, mVM_Type.Generic(B, Body));
					var BA = mVM_Type.Generic(B, mVM_Type.Generic(A, Body));
					mAssert.IsFalse(AB.SameType(BA));
					mAssert.IsTrue(AB.IsSubType(BA, mStd.cEmpty).Match(out _, out _));
				}
			),
			mTest.Test("Curried type abstractions retain their function kind until fully applied",
				aDebug => {
					var Expression = mSPO_Parser.Expression.ParseText(
						"[§GENERIC a [§GENERIC b [a => b]]]",
						"",
						__ => aDebug(__())
					);
					var Value = Expression.AsVM_Value(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
					var FunctionKind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					mAssert.IsTrue(Value.KindType().SameType(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), FunctionKind)));
					var Partial = Value.ApplyType(mVM_Type.Int());
					mAssert.IsTrue(Partial.KindType().SameType(FunctionKind));
					var Applied = Partial.ApplyType(mVM_Type.Bool());
					mAssert.IsTrue(Applied.KindType().IsType());
					mAssert.IsTrue(Applied.SameType(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Bool())));
					mAssert.ThrowsError(() => { Value.ApplyType(Partial); });
				}
			),
			mTest.Test("SIG type function bindings cannot be inferred as types",
				aDebug => {
					var Kind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					var F = mVM_Type.SigHead("F", Kind);
					mAssert.IsFalse(Kind.IsSubType(mVM_Type.Type(), mStd.cEmpty).Match(out _, out _));
					mAssert.IsFalse(F.IsSubType(mVM_Type.Int(), mStd.cEmpty).Match(out _, out _));
					mAssert.IsFalse(mVM_Type.Free("t").IsSubType(F, mStd.cEmpty).Match(out _, out _));
					var Application = F.ApplyType(mVM_Type.Int());
					mAssert.IsTrue(Application.KindType().IsType());
					var Parameter = mVM_Type.Free("t");
					var Identity = mVM_Type.Generic(Parameter, Parameter);
					mAssert.IsTrue(Application.Substitute(F, Identity).IsInt());
					mAssert.ThrowsError(() => { mVM_Type.Sig(F, F); });
				}
			),
			mTest.Tests("SIG rejects type functions in type positions",
				mStream.Stream(
					"[§SIG_WITH F € [§TYPE => §TYPE] IN F]",
					"[§SIG_WITH F € [§TYPE => §TYPE] IN [< Value: F >]]",
					"[§SIG_WITH F € [§TYPE => §TYPE] IN [F => §INT]]",
					"[§SIG_WITH F € [§TYPE => §TYPE] IN [§INT => F]]",
					"[§SIG_WITH F € [§TYPE => [§TYPE => §TYPE]] IN [.F §INT]]"
				).Map(Source => mTest.Test(Source,
					aDebug => {
						var Expression = mSPO_Parser.Expression.ParseText(Source, "", __ => aDebug(__()));
						mAssert.IsFalse(Expression.UpdateTypes(mStd.cEmpty).Match(out _, out _));
					}
				)).ToArrayList().ToArray()
			),
			mTest.Test("SIG heads and type literals have type TYPE",
				aDebug => {
					var Literal = mSPO_Parser.Expression.ParseText("§INT", "", __ => aDebug(__()));
					var Type = Literal.UpdateTypes(mStd.cEmpty).AssertNotError(__ => __.ErrorText);
					mAssert.IsTrue(Type.IsType() && Type.Refs.Length == 0);
					mAssert.IsTrue(Literal.AsVM_Type(mStd.cEmpty).AssertNotError(__ => __.ErrorText).IsInt());
					mAssert.IsTrue(Literal.TypeAnnotation.AssertNotEmpty().IsType());
					var Pattern = mSPO_Parser.Pattern.ParseText(
						"§SIG [§SIG_WITH t € §TYPE IN t] WITH §DEF Head € §TYPE IN §DEF Body",
						"",
						__ => aDebug(__())
					);
					var Scope = mSPO_AST_Types.UpdatePatternTypes(
						Pattern, mStd.cEmpty, mSPO_AST_Types.tTypeRelation.Equal, mStd.cEmpty
					).AssertNotError(__ => __.ErrorText).Scope;
					var Head = Scope.Where(__ => __.Id == "_Head").TryFirst().AssertNotEmpty();
					var Body = Scope.Where(__ => __.Id == "_Body").TryFirst().AssertNotEmpty();
					mAssert.IsTrue(Head.Type.IsType() && Head.Type.Refs.Length == 0);
					mAssert.IsTrue(Body.Type.SameType(Head.TypeValue.AssertNotEmpty()));
				}
			),
			mTest.Tests("SIG rejects invalid packages",
				mStream.Stream(
					"§SIG [§SIG_WITH t € §TYPE IN t] WITH §INT IN §TRUE",
					"§SIG [§SIG_WITH F € [§TYPE => §TYPE] IN []] WITH §INT IN ()",
					"§SIG [§SIG_WITH t € §TYPE IN t] WITH [< Value: §INT >] IN { Value: §TRUE }",
					"""
					§IF (§SIG [§SIG_WITH t € §TYPE IN t] WITH §INT IN 7) MATCH {
						§SIG [§SIG_WITH t € §TYPE IN t] WITH §DEF Head € §TYPE IN §DEF Body : .((§DEF N € §INT) => N) Body
					}
					"""
				).Map(
					Source => mTest.Test(Source,
						aDebug => {
							var Expression = mSPO_Parser.Expression.ParseText(Source, "", __ => aDebug(__()))
							.DesugarExpression().AssertNotError(__ => __.ErrorText);
							mAssert.IsFalse(Expression.UpdateTypes(mStd.cEmpty).Match(out _, out _));
						}
					)
				).ToArrayList().ToArray()
			),
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
			mTest.Test("Higher-order arguments reach a recursive fixed point",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							"""
							.F (
								1
								(§DEF a => a)
								(§DEF a1 => (§DEF a2 => a1))
							)
							""",
							"",
							__ => aDebugStream(__())
						).UpdateTypes(
							mStream.Stream(
								mSPO_AST_Types.ScopeItem(
									"_F...",
									mSPO_Parser.Type.ParseText(
										"""
										[
											§GENERIC tIn [
												§GENERIC tMiddle [
													§GENERIC tOut [
														[
															tIn
															[tIn => tMiddle]
															[
																tIn => [tMiddle => tOut]
															]
														] => tOut
													]
												]
											]
										]
										""",
										"",
										__ => aDebugStream(__())
									).AsVM_Type(mStd.cEmpty).AssertNotError(__ => __.ErrorText)
								)
							)
						),
						mVM_Type.Int()
					);
				}
			),
			mTest.Test("Higher-order method arguments are retried",
				aDebugStream => {
					var Scope = mSPO_AST_Types.UpdateMethodCallTypes(
						mSPO_Parser.MethodCall.ParseText(
							"""
							F (
								1
								(§DEF a => a)
							) => §DEF result
							""",
							"",
							__ => aDebugStream(__())
						),
						mStream.Stream(
							mSPO_AST_Types.ScopeItem(
								"_F...",
								mSPO_Parser.Type.ParseText(
									"[§GENERIC tIn [§GENERIC tOut [[tIn, [tIn => tOut]] => tOut]]]",
									"",
									__ => aDebugStream(__())
								).AsVM_Type(mStd.cEmpty).AssertNotError(__ => __.ErrorText)
							)
						)
					).AssertNotError(__ => __.ErrorText);
					mAssert.AreEquals(
						Scope.Where(__ => __.Id == "_result").TryFirst().AssertNotEmpty().Type,
						mVM_Type.Int()
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
