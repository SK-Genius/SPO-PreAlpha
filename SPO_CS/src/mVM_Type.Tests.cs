#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mStream.cs
#:ref Common/mSpan.cs
#:ref Common/mResult.cs
#:ref Common/mTextStream.cs
#:ref Common/mArrayList.cs
#:ref Common/mParserGen.cs
#:ref mTokenizer.cs
#:ref mVM_Type.cs
#:ref mSPO_AST_Types.cs
#:ref mSPO_AST.cs
#:ref mSPO_Parser.cs
#:ref mSPO_Desugar.cs

public static class
mVM_Type_Tests {
	private static tText
	Id(
		tText aId
	) => "_" + aId;
	
	private static tText
	TypeError(
		mVM_Type.tType aFound,
		mVM_Type.tType aExpected,
		tBool aHighlightDifferences = false
	) {
		mAssert.IsFalse(aFound.IsSubType(aExpected, aHighlightDifferences: aHighlightDifferences).Match(out _, out var Error));
		return Error!;
	}
	
	private static readonly mStream.tStream<mSPO_AST_Types.tScopeItem> cTestScope = mStream.Stream(
		mSPO_AST_Types.ScopeItem(
			"_...+...",
			mVM_Type.Proc(
				mVM_Type.Empty(),
				mVM_Type.Tuple(
					[mVM_Type.Int(), mVM_Type.Int()]
				),
				mVM_Type.Int()
			)
		)
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(nameof(mVM_Type),
		[
			mTest.Tests("Type error output",
				[
					mTest.Test("Nested differences retain their path in a single comparison",
						aDebug => {
							mVM_Type.tType Type(mVM_Type.tType aValue) => mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Tuple(
									[
										mVM_Type.Int(),
										mVM_Type.Record(
											[
												("Ignored", mVM_Type.Int()),
												("Value", mVM_Type.Tuple([mVM_Type.Int(), aValue]))
											]
										)
									]
								),
								mVM_Type.Int()
							);
							var Error = TypeError(Type(mVM_Type.True()), Type(mVM_Type.Int()));
							var Highlighted = TypeError(Type(mVM_Type.True()), Type(mVM_Type.Int()), true);
							aDebug(Highlighted);
							mAssert.AreEquals(Error.Split("found:").Length, 2);
							mAssert.AreEquals(Error.Split("expected:").Length, 2);
							mAssert.IsFalse(Error.Contains("in:") || Error.Contains("Ignored"));
							mAssert.IsTrue(Error.Contains("Value : [ ..., §TRUE ]"));
							mAssert.IsTrue(Error.Contains("Value : [ ..., §INT ]"));
							mAssert.IsTrue(Error.Contains("-> ..."));
							mAssert.AreEquals(
								Highlighted,
								Error.Replace("§TRUE", "\x1b[31m§TRUE\x1b[0m").Replace("§INT", "\x1b[31m§INT\x1b[0m")
							);
						}
					),
					mTest.Test("Type errors highlight differences by default",
						aDebug => {
							mAssert.IsFalse(mVM_Type.True().IsSubType(mVM_Type.Int()).Match(out _, out var Error));
							mAssert.AreEquals(Error, "found:\n  \x1b[31m§TRUE\x1b[0m\nexpected:\n  \x1b[31m§INT\x1b[0m");
							mAssert.AreEquals(mVM_Type.True().ToText(), "§TRUE");
						}
					),
					mTest.Test("Only different prefix names are highlighted around common payloads",
						aDebug => mAssert.AreEquals(
							TypeError(mVM_Type.Prefix("Left", mVM_Type.Int()), mVM_Type.Prefix("Right", mVM_Type.Int()), true),
							"found:\n  [ \x1b[31m#Left\x1b[0m ... ]\nexpected:\n  [ \x1b[31m#Right\x1b[0m ... ]"
						)
					),
					mTest.Test("Missing fields are highlighted including their names",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Record([("Common", mVM_Type.Int())]),
								mVM_Type.Record([("Common", mVM_Type.Int()), ("Missing", mVM_Type.True())]),
								true
							),
							"missing field 'Missing'\nfound:\n  [{ ... }]\nexpected:\n  [{ ...,  \x1b[31mMissing : §TRUE\x1b[0m }]"
						)
					),
					mTest.Test("Color escapes do not affect line wrapping",
						aDebug => {
							var Found = mVM_Type.Tuple(
								mStream.Nat32StartWith(0).Take(7).Map(_ => mVM_Type.True())
							);
							var Expected = mVM_Type.Tuple(
								mStream.Nat32StartWith(0).Take(7).Map(_ => mVM_Type.Int())
							);
							var Error = TypeError(Found, Expected, true);
							mAssert.AreEquals(Error.Split('\n').Length, 4);
							mAssert.AreEquals(Error.Replace("\x1b[31m", "").Replace("\x1b[0m", ""), TypeError(Found, Expected));
						}
					),
					mTest.Test("Tuple omissions preserve element positions",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Tuple([mVM_Type.True(), mVM_Type.Int(), mVM_Type.True(), mVM_Type.Int()]),
								mVM_Type.Tuple([mVM_Type.True(), mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int()])
							),
							"found:\n  [ ..., ..., §TRUE, ... ]\nexpected:\n  [ ..., ..., §INT, ... ]"
						)
					),
					mTest.Test("Different tuple arities keep corresponding elements aligned from the left",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.True()]),
								mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int(), mVM_Type.True()])
							),
							"found:\n  [ ..., §TRUE ]\nexpected:\n  [ ..., §INT, §TRUE ]"
						)
					),
					mTest.Test("All differing record fields are visible together",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Record(
									[("A", mVM_Type.True()), ("B", mVM_Type.Int()), ("C", mVM_Type.Int())]
								),
								mVM_Type.Record(
									[("A", mVM_Type.Int()), ("B", mVM_Type.True()), ("C", mVM_Type.Int())]
								)
							),
							"found:\n  [{ A : §TRUE,  B : §INT,  ... }]\nexpected:\n  [{ A : §INT,  B : §TRUE,  ... }]"
						)
					),
					mTest.Test("Missing record fields remain visible",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Record([("Common", mVM_Type.Int())]),
								mVM_Type.Record([("Common", mVM_Type.Int()), ("Missing", mVM_Type.True())])
							),
							"missing field 'Missing'\nfound:\n  [{ ... }]\nexpected:\n  [{ ...,  Missing : §TRUE }]"
						)
					),
					mTest.Test("Union omissions match members independently of order and grouping",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Set(mVM_Type.True(), mVM_Type.Set(mVM_Type.Int(), mVM_Type.Empty())),
								mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Set(mVM_Type.False(), mVM_Type.Int()))
							),
							"found:\n  [ §TRUE | ... ]\nexpected:\n  [ ... | §FALSE ]"
						)
					),
					mTest.Test("Different prefixes remain visible around equal payloads",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Prefix("Left", mVM_Type.Int()),
								mVM_Type.Prefix("Right", mVM_Type.Int())
							),
							"found:\n  [ #Left ... ]\nexpected:\n  [ #Right ... ]"
						)
					),
					mTest.Test("Generic omissions align bound parameters by identity",
						aDebug => {
							var Left = mVM_Type.Free("t", mVM_Type.Type());
							var Right = mVM_Type.Free("u", mVM_Type.Type());
							mAssert.AreEquals(
								TypeError(
									mVM_Type.Generic(Left, mVM_Type.Proc(mVM_Type.Empty(), Left, mVM_Type.True())),
									mVM_Type.Generic(Right, mVM_Type.Proc(mVM_Type.Empty(), Right, mVM_Type.Int()))
								),
								"found:\n  [ §ALL ... => [ ... -> §TRUE ] ]\nexpected:\n  [ §ALL ... => [ ... -> §INT ] ]"
							);
						}
					),
					mTest.Test("Recursive omissions retain the data shape without unfolding it",
						aDebug => {
							var Left = mVM_Type.Free("List", mVM_Type.Type());
							var Right = mVM_Type.Free("Other", mVM_Type.Type());
							mAssert.AreEquals(
								TypeError(
									mVM_Type.Recursive(
										Left, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Left, mVM_Type.True()))
									),
									mVM_Type.Recursive(
										Right, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Right, mVM_Type.Int()))
									)
								),
								"found:\n  [ §RECURSIVE ... = [ ... | [ ...; §TRUE ] ] ]\n" +
								"expected:\n  [ §RECURSIVE ... = [ ... | [ ...; §INT ] ] ]"
							);
						}
					)
				]
			),
			mTest.Test(
				"SIG contracts compare bound constructors independently of parameter names",
				aDebug => {
					var Kind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					var F = mVM_Type.Free("F", Kind);
					var G = mVM_Type.Free("G", Kind);
					var Contract = mVM_Type.Sig(F, F.ApplyType(mVM_Type.Int()));
					var Renamed = mVM_Type.Sig(G, G.ApplyType(mVM_Type.Int()));
					mAssert.IsTrue(Contract.SameType(Renamed));
					mAssert.IsTrue(
						Contract.IsSubType(Renamed).AssertNotError(__ => __).ToStream().IsEmpty()
					);
					mAssert.IsFalse(
						Contract.IsSubType(
							mVM_Type.Sig(G, G.ApplyType(mVM_Type.Bool()))
						).Match(out _, out _)
					);
				}
			),
			mTest.Test(
				"Repeated free bindings replace the mapping and preserve previous snapshots",
				aDebug => {
					var Free = mVM_Type.Free("t", mVM_Type.Type());
					var First = mVM_Type.Int().IsSubType(Free).AssertNotError(__ => __);
					var Updated = mVM_Type.False().IsSubType(Free, First).AssertNotError(__ => __);
					mAssert.AreEquals(Updated.ToStream().Count(), 1u);
					mAssert.IsTrue(Free.ApplyMappings(First).IsInt());
					mAssert.IsTrue(
						Free.ApplyMappings(Updated).SameType(mVM_Type.Union(mVM_Type.Int(), mVM_Type.False()))
					);
				}
			),
			mTest.Test("ApplyMappings uses free type instances",
				aDebugStream => {
					var Outer = mVM_Type.Free("t", mVM_Type.Type());
					var Gen = mVM_Type.Generic(
						mVM_Type.Free("t", mVM_Type.Type()).Def(out var Bound),
						mVM_Type.Proc(mVM_Type.Empty(), Outer, Bound)
					);
					
					mAssert.IsFalse(ReferenceEquals(Outer, Bound));
					
					var Mappings = mVM_Type.Int(
					).IsSubType(
						Outer
					).AssertNotError(__ => __);
					
					Mappings = mVM_Type.False(
					).IsSubType(
						Bound,
						Mappings
					).AssertNotError(__ => __);
					
					mAssert.AreEquals(
						Gen.ApplyMappings(Mappings),
						mVM_Type.Generic(
							Bound,
							mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), Bound)
						)
					);
					
					var OtherBound = mVM_Type.Free("other", mVM_Type.Type());
					
					mVM_Type.Generic(Bound, Bound).IsSubType(
						mVM_Type.Generic(OtherBound, OtherBound)
					).AssertNotError(__ => __);
				}
			),
			mTest.Tests("Pair projection",
				mStream.Stream(
					[
						(
							mStd.File(),
							mStd.LineNr(),
							"[§INT; §TRUE]",
							"§INT",
							"§TRUE"
						),
						(
							mStd.File(),
							mStd.LineNr(),
							"[[§INT; §TRUE] | [[]; §FALSE]]",
							"[§INT | []]",
							"[§TRUE | §FALSE]"
						),
						(
							mStd.File(),
							mStd.LineNr(),
							"[§RECURSIVE RecursivePair [[] | [RecursivePair; §INT]]]",
							"[§RECURSIVE RecursivePair [[] | [RecursivePair; §INT]]]",
							"§INT"
						),
					]
				).Map(
					((tText File, tInt32 LineNr, tText Type, tText _1, tText _2) a) => mTest.Test(
						a.Type,
						aDebugStream => {
							var Type = mSPO_Parser.Type.ParseText(a.Type, "Pair", __ => aDebugStream(__()))
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var _1 = mSPO_Parser.Type.ParseText(a._1, "FirstPart", __ => aDebugStream(__()))
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var _2 = mSPO_Parser.Type.ParseText(a._2, "SecondPart", __ => aDebugStream(__()))
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							mAssert.IsTrue(Type.TryProjectPair(out var First, out var Second));
							mAssert.AreEquals(First, _1);
							mAssert.AreEquals(Second, _2);
						},
						a.File,
						a.LineNr
					)
				).ToArrayList().ToArray()
			),
			mTest.Test("Pair projection fails for non pairs",
				aDebugStream => {
					mAssert.IsFalse(mVM_Type.Int().TryProjectPair(out _, out _));
				}
			),
			mTest.Test("SplitBy expands recursive types",
				aDebugStream => {
					var Head = mVM_Type.Free("RecursiveSplit", mVM_Type.Type());
					var Recursive = mVM_Type.Recursive(
						Head,
						mVM_Type.Set(
							mVM_Type.Empty(),
							mVM_Type.Pair(Head, mVM_Type.Int())
						)
					);
					var Split = Recursive.SplitBy(__ => __.IsPair(out _, out _));
					mAssert.AreEquals(
						Split.Matched.AssertNotEmpty(),
						mVM_Type.Pair(Recursive, mVM_Type.Int())
					);
					mAssert.AreEquals(Split.Remainder.AssertNotEmpty(), mVM_Type.Empty());
				}
			),
			..mStream.Stream<(tText File, tInt32 LineNr, tText Expr, tText Type)>(
			[
				(mStd.File(), mStd.LineNr(), "()", "[]"),
				(mStd.File(), mStd.LineNr(), "§TRUE", "§BOOL"),
				(mStd.File(), mStd.LineNr(), "§FALSE", "§BOOL"),
				(mStd.File(), mStd.LineNr(), "1", "§INT"),
				(mStd.File(), mStd.LineNr(), "...+...", "[[§INT, §INT] => §INT]"),
				(mStd.File(), mStd.LineNr(), "1 .+ 1", "§INT"),
				(mStd.File(), mStd.LineNr(), "a € §INT => a .+ a", "[§INT => §INT]"),
				(mStd.File(), mStd.LineNr(), ".((a1 € §INT, a2 € §INT, a3 € §INT) => (a1 .+ a2) .+ a3)(1, 2, 3)", "§INT"),
				(
					mStd.File(),
					mStd.LineNr(),
					"""
					§IF (1, 2) MATCH {
						(1, 1) : 1
						(1, _) : 2
						(2, §DEF a) : a
						_ : 0
					}
					""",
					"§INT"
				),
				(
					mStd.File(),
					mStd.LineNr(),
					"""
					§IF 1 MATCH {
						1 : 1
						_ : ()
					}
					""",
					"[§INT | []]"
				),
				(
					mStd.File(),
					mStd.LineNr(),
					"""
					§IF 1 MATCH {
						1 : 1
						_ : ()
					}
					""",
					"[[] | §INT]"
				)
			]
		).Map(
			a => mTest.Test(a.Expr + " => " + a.Type,
				aStreamOut => {
					var AST =  mSPO_Parser.Expression.ParseText(
						a.Expr,
						"",
						__ => aStreamOut(__())
					);
					
					var Type = AST.UpdateTypes(
						cTestScope,
						new()
					).AssertNotError(
						__ => __.ToText()
					).Type;
					
					var VM_Type = mSPO_Parser.Type.ParseText(
						a.Type,
						"",
						__ => { aStreamOut(__()); }
					).DesugarType(
					).AsVM_Type(
						cTestScope
					).AssertNotError(
						__ => __.ToText()
					);
					
					Type.IsSubType(
						VM_Type
					).AssertNotError(
						_ => Type.ToText() + " != " + VM_Type.ToText()
					);
					
					if (a.Expr is "§TRUE") {
						mAssert.AreEquals(Type, mVM_Type.True());
						mAssert.IsFalse(VM_Type.IsSubType(Type).Match(out _, out _));
					} else if (a.Expr is "§FALSE") {
						mAssert.AreEquals(Type, mVM_Type.False());
						mAssert.IsFalse(VM_Type.IsSubType(Type).Match(out _, out _));
					} else {
						VM_Type.IsSubType(
							Type
						).AssertNotError(
							_ => Type.ToText() + " != " + VM_Type.ToText()
						);
					}
				},
				a.File,
				a.LineNr
			)
			).ToArrayList(
			).ToArray(
			)
		]
	);
}
