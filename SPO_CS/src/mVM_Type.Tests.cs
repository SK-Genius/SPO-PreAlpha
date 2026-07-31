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
			mTest.Test("ApplyMappings uses free type instances",
				aDebugStream => {
					var Outer = mVM_Type.Free("t");
					var Gen = mVM_Type.Generic(
						mVM_Type.Free("t").Def(out var Bound),
						mVM_Type.Proc(mVM_Type.Empty(), Outer, Bound)
					);
					
					mAssert.IsFalse(ReferenceEquals(Outer, Bound));
					
					var Mappings = mVM_Type.Int(
					).IsSubType(
						Outer,
						mStd.cEmpty
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
					
					var OtherBound = mVM_Type.Free("other");
					
					mVM_Type.Generic(Bound, Bound).IsSubType(
						mVM_Type.Generic(OtherBound, OtherBound),
						mStd.cEmpty
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
					var Head = mVM_Type.Free("RecursiveSplit");
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
						cTestScope
					).AssertNotError(__ => __.ToText());
					
					var Type_ = mSPO_Parser.Type.ParseText(
						a.Type,
						"",
						__ => { aStreamOut(__()); }
					).DesugarType(
					).AsVM_Type(
						cTestScope
					).AssertNotError(__ => __.ToText());
					
					Type.IsSubType(Type_, mStd.cEmpty)
					.AssertNotError(_ => Type.ToText() + " != " + Type_.ToText());
					
					if (a.Expr is "§TRUE") {
						mAssert.AreEquals(Type, mVM_Type.True());
						mAssert.IsFalse(Type_.IsSubType(Type, mStd.cEmpty).Match(out _, out _));
					} else if (a.Expr is "§FALSE") {
						mAssert.AreEquals(Type, mVM_Type.False());
						mAssert.IsFalse(Type_.IsSubType(Type, mStd.cEmpty).Match(out _, out _));
					} else {
						Type_.IsSubType(Type, mStd.cEmpty)
						.AssertNotError(_ => Type.ToText() + " != " + Type_.ToText());
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
