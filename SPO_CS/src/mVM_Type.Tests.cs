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
	
	private static void
	AssertEquivalent(
		mVM_Type.tType aType1,
		mVM_Type.tType aType2
	) {
		aType1.IsSubType(aType2).AssertNotError(__ => __);
		aType2.IsSubType(aType1).AssertNotError(__ => __);
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
			mTest.Test("Inference uses type-variable identities",
				aDebugStream => {
					var Outer = mVM_Type.TypeVariable("t");
					var Gen = mVM_Type.Generic(
						mVM_Type.TypeVariable("t").Def(out var Bound),
						mVM_Type.Proc(mVM_Type.Empty(), Outer, Bound)
					);
					
					mAssert.IsFalse(mStd.RefEq(Outer, Bound));
					
					var Mappings = mVM_Type.Int(
					).IsSubTypeOf(
						Outer,
						mVM_Type.NewInferenceState().AddVar(Outer)
					).AssertNotError(__ => __);
					
					Mappings = mVM_Type.False(
					).IsSubTypeOf(
						Bound,
						Mappings.AddVar(Bound)
					).AssertNotError(__ => __);
					
					mAssert.AreEquals(
						Gen.ApplyInference(Mappings),
						mVM_Type.Generic(
							Bound,
							mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), Bound)
						)
					);
					
					var OtherBound = mVM_Type.TypeVariable("other");
					
					mVM_Type.Generic(Bound, Bound).IsSubType(
						mVM_Type.Generic(OtherBound, OtherBound)
					).AssertNotError(__ => __);
				}
			),
			mTest.Test("ApplyInference resolves transitive solutions in any order",
				aDebugStream => {
					var Variable1 = mVM_Type.TypeVariable("variable1");
					var Variable2 = mVM_Type.TypeVariable("variable2");
					var Type = mVM_Type.Pair(Variable1, Variable2);
					var Variable1ToVariable2 = (
						Variable: Variable1,
						Solution: mMaybe.Some(Variable2)
					);
					var Variable2ToInt = (
						Variable: Variable2,
						Solution: mMaybe.Some(mVM_Type.Int())
					);
					var Expected = mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Int());
					
					mAssert.AreEquals(
						Type.ApplyInference(
							mStream.Stream(
								(Variable: Variable1, Solution: mMaybe.None<mVM_Type.tType>())
							)
						),
						Type
					);
					mAssert.AreEquals(
						Type.ApplyInference(
							mStream.Stream(
								Variable1ToVariable2,
								Variable2ToInt
							)
						),
						Expected
					);
					mAssert.AreEquals(
						Type.ApplyInference(
							mStream.Stream(
								Variable2ToInt,
								Variable1ToVariable2
							)
						),
						Expected
					);
				}
			),
			mTest.Test("Constraint and union order only changes representation",
				aDebugStream => {
					var Variable1 = mVM_Type.TypeVariable("left");
					var InitialInference = mVM_Type.NewInferenceState().AddVar(Variable1);
					var Inference1 = InitialInference;
					Inference1 = mVM_Type.Int().IsSubTypeOf(Variable1, Inference1).AssertNotError(__ => __);
					mAssert.IsTrue(InitialInference.TryGetSolution(Variable1).IsNone());
					Inference1 = mVM_Type.Bool().IsSubTypeOf(Variable1, Inference1).AssertNotError(__ => __);
					
					var Variable2 = mVM_Type.TypeVariable("right");
					var Inference2 = mVM_Type.NewInferenceState().AddVar(Variable2);
					Inference2 = mVM_Type.Bool().IsSubTypeOf(Variable2, Inference2).AssertNotError(__ => __);
					Inference2 = mVM_Type.Int().IsSubTypeOf(Variable2, Inference2).AssertNotError(__ => __);
					
					AssertEquivalent(
						Inference1.TryGetSolution(Variable1).AssertNotEmpty(),
						Inference2.TryGetSolution(Variable2).AssertNotEmpty()
					);
					
					var Choice1 = mVM_Type.TypeVariable("choice1");
					var Choice2 = mVM_Type.TypeVariable("choice2");
					
					var ChoiceInference1 = mVM_Type.Int().IsSubTypeOf(
						mVM_Type.Set(mVM_Type.Empty(), Choice1),
						mVM_Type.NewInferenceState().AddVar(Choice1)
					).AssertNotError(__ => __);
					
					var ChoiceInference2 = mVM_Type.Int().IsSubTypeOf(
						mVM_Type.Set(Choice2, mVM_Type.Empty()),
						mVM_Type.NewInferenceState().AddVar(Choice2)
					).AssertNotError(__ => __);
					
					AssertEquivalent(
						ChoiceInference1.TryGetSolution(Choice1).AssertNotEmpty(),
						ChoiceInference2.TryGetSolution(Choice2).AssertNotEmpty()
					);
				}
			),
			mTest.Test("Proc arguments are contravariant",
				aDebugStream => {
					var SubType = mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Any(),
						mVM_Type.True()
					);
					var SupType = mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Int(),
						mVM_Type.Bool()
					);
					SubType.IsSubType(SupType).AssertNotError(__ => __);
					mAssert.IsFalse(SupType.IsSubType(SubType).Match(out _, out _));
					var DifferentObject = mVM_Type.Proc(
						mVM_Type.Int(),
						mVM_Type.Any(),
						mVM_Type.True()
					);
					mAssert.IsFalse(DifferentObject.IsSubType(SubType).Match(out _, out _));
					mAssert.IsFalse(SubType.IsSubType(DifferentObject).Match(out _, out _));
				}
			),
			mTest.Test("Tuple and empty-prefix representation",
				aDebugStream => {
					var Element = mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int());
					mAssert.AreEquals(mVM_Type.Tuple([]), mVM_Type.Empty());
					mAssert.IsTrue(mStd.RefEq(mVM_Type.Tuple([Element]), Element));
					mAssert.AreEquals(
						mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Bool()]),
						mVM_Type.Pair(
							mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int()),
							mVM_Type.Bool()
						)
					);
					mAssert.AreEquals(mVM_Type.Prefix("_P..."), mVM_Type.Prefix("_P...", mVM_Type.Empty()));
					mAssert.IsTrue(
						mVM_Type.Prefix("_P...", mVM_Type.Tuple([Element])).IsPrefix(
							"_P...",
							out var SinglePrefixElement
						)
					);
					mAssert.IsTrue(mStd.RefEq(SinglePrefixElement, Element));
					mAssert.AreEquals(
						mVM_Type.Prefix(
							"_P...",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Bool()])
						),
						mVM_Type.Prefix(
							"_P...",
							mVM_Type.Pair(
								mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int()),
								mVM_Type.Bool()
							)
						)
					);
				}
			),
			mTest.Test("Occurs check rejects infinite inference types",
				aDebugStream => {
					var Variable = mVM_Type.TypeVariable("inferred");
					mAssert.IsFalse(
						mVM_Type.Pair(Variable, mVM_Type.Int()).IsSubTypeOf(
							Variable,
							mVM_Type.NewInferenceState().AddVar(Variable)
						).Match(out _, out _)
					);
				}
			),
			mTest.Test("Union alternatives are checked before rigid type variables",
				aDebugStream => {
					var Rigid = mVM_Type.TypeVariable("rigid");
					var Inferred = mVM_Type.TypeVariable("inferred");
					var Mappings = Rigid.IsSubTypeOf(
						mVM_Type.Set(mVM_Type.Empty(), Inferred),
						mVM_Type.NewInferenceState().AddVar(Inferred)
					).AssertNotError(__ => __);
					
					mAssert.IsTrue(
						Mappings.Any(
							__ => mStd.RefEq(__.Variable, Inferred) &&
								__.Solution.IsSome(out var Solution) &&
								mStd.RefEq(Solution, Rigid)
						)
					);
				}
			),
			mTest.Tests("Union",
				mStream.Stream<(tText File, tInt32 LineNr, tText Type1, tText Type2, tText Expected)>(
					[
						(mStd.File(), mStd.LineNr(), "[]", "§INT", "[[] | §INT]"),
						(mStd.File(), mStd.LineNr(), "§INT", "[]", "[§INT | []]"),
						(mStd.File(), mStd.LineNr(), "[[] | §INT]", "§INT", "[[] | §INT]"),
						(mStd.File(), mStd.LineNr(), "§INT", "[[] | §INT]", "[§INT | []]"),
						(mStd.File(), mStd.LineNr(), "[[] | §INT]", "[]", "[§INT | []]"),
						(mStd.File(), mStd.LineNr(), "[]", "[[] | §INT]", "[[] | §INT]"),
					]
				).Map(
					a => mTest.Test(
						$"[{a.Type1} | {a.Type2}] == {a.Expected}",
						aDebugStream => {
							static mVM_Type.tType
							ParseInWrittenOrder(
								tText aType,
								mStd.tAction<tText> aDebugStream
							) => mSPO_Parser.Type.ParseText(
									aType,
									"",
									__ => aDebugStream(__())
								).AsVM_Type(
									mStd.cEmpty
								).AssertNotError(
									__ => __.ErrorText
								);
							
							mAssert.AreEquals(
								mVM_Type.Union(
									ParseInWrittenOrder(a.Type1, aDebugStream),
									ParseInWrittenOrder(a.Type2, aDebugStream)
								),
								ParseInWrittenOrder(a.Expected, aDebugStream)
							);
						},
						a.File,
						a.LineNr
					)
				).ToArrayList().ToArray()
			),
			mTest.Test("Pair subtyping distributes nested unions",
				aDebugStream => {
					var Type1 = mVM_Type.Empty();
					var Type2 = mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int());
					var Head = mVM_Type.True();
					var PairWithUnion = mVM_Type.Pair(mVM_Type.Set(Type1, Type2), Head);
					var UnionOfPairs = mVM_Type.Set(
						mVM_Type.Pair(Type1, Head),
						mVM_Type.Pair(Type2, Head)
					);
					
					PairWithUnion.IsSubType(UnionOfPairs).AssertNotError(__ => __);
					UnionOfPairs.IsSubType(PairWithUnion).AssertNotError(__ => __);
				}
			),
			mTest.Test("Recursive subtyping is coinductive",
				aDebugStream => {
					var ListHead = mVM_Type.TypeVariable("List");
					var List = mVM_Type.Recursive(
						ListHead,
						mVM_Type.Set(
							mVM_Type.Empty(),
							mVM_Type.Pair(ListHead, mVM_Type.Int())
						)
					);
					var List1Head = mVM_Type.TypeVariable("List1");
					var List1 = mVM_Type.Recursive(
						List1Head,
						mVM_Type.Set(
							mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int()),
							mVM_Type.Pair(List1Head, mVM_Type.Int())
						)
					);
					
					List1.IsSubType(List).AssertNotError(__ => __);
					mAssert.IsFalse(List.IsSubType(List1).Match(out _, out _));
					
					var BoolListHead = mVM_Type.TypeVariable("BoolList");
					var BoolList = mVM_Type.Recursive(
						BoolListHead,
						mVM_Type.Set(
							mVM_Type.Empty(),
							mVM_Type.Pair(BoolListHead, mVM_Type.Bool())
						)
					);
					mAssert.IsFalse(List1.IsSubType(BoolList).Match(out _, out _));
					
					var Element = mVM_Type.TypeVariable("Element");
					var GenericListHead = mVM_Type.TypeVariable("GenericList");
					var GenericList = mVM_Type.Recursive(
						GenericListHead,
						mVM_Type.Set(
							mVM_Type.Empty(),
							mVM_Type.Pair(GenericListHead, Element)
						)
					);
					var Mappings = BoolList.IsSubTypeOf(
						GenericList,
						mVM_Type.NewInferenceState().AddVar(Element)
					).AssertNotError(__ => __);
					mAssert.IsTrue(
						Mappings.Any(
							__ => (
								mStd.RefEq(__.Variable, Element) &&
								__.Solution.IsSome(out var Solution) &&
								Solution == mVM_Type.Bool()
							)
						)
					);
					mVM_Type.Pair(BoolList, mVM_Type.True()).IsSubTypeOf(
						mVM_Type.Pair(GenericList, Element),
						mVM_Type.NewInferenceState().AddVar(Element)
					).AssertNotError(__ => __);
				}
			),
			mTest.Test("Recursive types must be guarded",
				aDebugStream => {
					var Head = mVM_Type.TypeVariable("Unguarded");
					mAssert.ThrowsError(() => mVM_Type.Recursive(Head, Head));
					
					var Parameter = mVM_Type.TypeVariable("T");
					mAssert.ThrowsError(
						() => mVM_Type.Recursive(
							Head,
							mVM_Type.TypeApply(mVM_Type.Generic(Parameter, Parameter), Head)
						)
					);
					mAssert.ThrowsError(
						() => mVM_Type.Recursive(
							Head,
							mVM_Type.TypeApply(
								mVM_Type.Generic(
									Parameter,
									mVM_Type.Set(Parameter, mVM_Type.Prefix("None"))
								),
								Head
							)
						)
					);
					
					mVM_Type.Recursive(
						Head,
						mVM_Type.TypeApply(
							mVM_Type.Generic(Parameter, mVM_Type.Prefix("Box", Parameter)),
							Head
						)
					);
					mVM_Type.Recursive(
						Head,
						mVM_Type.TypeApply(
							mVM_Type.Generic(Parameter, mVM_Type.Pair(mVM_Type.Int(), Parameter)),
							Head
						)
					);
					mVM_Type.Recursive(
						Head,
						mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), Head)
					);
					
					var Outer = mVM_Type.TypeVariable("Outer");
					var Inner = mVM_Type.TypeVariable("Inner");
					mVM_Type.Recursive(
						Outer,
						mVM_Type.Recursive(Inner, mVM_Type.Pair(Inner, Outer))
					);
					mAssert.ThrowsError(
						() => mVM_Type.Recursive(
							Outer,
							mVM_Type.Recursive(
								Inner,
								mVM_Type.Set(mVM_Type.Pair(Inner, mVM_Type.Int()), Outer)
							)
						)
					);
				}
			),
			mTest.Test("Sig contracts are alpha-equivalent",
				aDebugStream => {
					var ConstructorKind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					var F1 = mVM_Type.TypeVariable("F1", ConstructorKind);
					var F2 = mVM_Type.TypeVariable("F2", ConstructorKind);
					mAssert.AreEquals(
						mVM_Type.Sig(F1, mVM_Type.TypeApply(F1, mVM_Type.Int())),
						mVM_Type.Sig(F2, mVM_Type.TypeApply(F2, mVM_Type.Int()))
					);
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
					var Head = mVM_Type.TypeVariable("RecursiveSplit");
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
					
					Type.IsSubType(Type_)
					.AssertNotError(_ => Type.ToText() + " != " + Type_.ToText());
					
					if (a.Expr is "§TRUE") {
						mAssert.AreEquals(Type, mVM_Type.True());
						mAssert.IsFalse(Type_.IsSubType(Type).Match(out _, out _));
					} else if (a.Expr is "§FALSE") {
						mAssert.AreEquals(Type, mVM_Type.False());
						mAssert.IsFalse(Type_.IsSubType(Type).Match(out _, out _));
					} else {
						Type_.IsSubType(Type)
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
