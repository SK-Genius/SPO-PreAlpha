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
		mVM_Type.tType aExpected
	) {
		mAssert.IsFalse(aFound.IsSubType(aExpected).Match(out _, out var Error));
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
	Tests = mTest.Tests(
		nameof(mVM_Type),
		[
			mTest.Test(
				"Generic inference preserves EMPTY alternatives and distinguishes missing types",
				aDebug => {
					var Parameter = mVM_Type.Free("t", mVM_Type.Type());
					var Function = mVM_Type.Generic(
						Parameter,
						mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Pair(Parameter, Parameter), Parameter)
					);
					
					foreach (
						var Argument in mStream.Stream(
							mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int()),
							mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Empty())
						)
					) {
						var Result = mVM_Type.Infer(
							Function,
							mVM_Type.Empty(),
							Argument,
							__ => {}
						).AssertNotError(
							__ => __
						);
						mAssert.IsTrue(Result.SameType(mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Int())));
					}
					
					mMaybe.tMaybe<mVM_Type.tType> Missing = default;
					var Empty = mMaybe.Some(mVM_Type.Empty());
					mAssert.IsTrue(mVM_Type.Union(Missing, Missing).IsNone());
					mAssert.IsTrue(mVM_Type.Union(Empty, Missing).AssertNotEmpty().IsEmpty());
					mAssert.IsTrue(mVM_Type.Union(Missing, Empty).AssertNotEmpty().IsEmpty());
				}
			),
			mTest.Test(
				"Type equality has one meaning for records, binders and free parameters",
				__ => {
					static void Check(
						mVM_Type.tType? aLeft,
						mVM_Type.tType? aRight,
						tBool aEqual
					) {
						mAssert.AreEquals(aLeft == aRight, aEqual);
						mAssert.AreEquals(aRight == aLeft, aEqual);
						mAssert.AreEquals(aLeft != aRight, !aEqual);
						mAssert.AreEquals(tUnknown.Equals(aLeft, aRight), aEqual);
						mAssert.AreEquals(aLeft.SameType(aRight), aEqual);
						if (aEqual && aLeft is not null && aRight is not null) {
							mAssert.AreEquals(aLeft.GetHashCode(), aRight.GetHashCode());
						}
					}
					var Record = mVM_Type.Record([("Number", mVM_Type.Int())]);
					Check(Record, mVM_Type.Record([("Number", mVM_Type.Int())]), true);
					Check(Record, mVM_Type.Record([("Number", mVM_Type.Bool())]), false);
					Check(Record, mVM_Type.Record([("Other", mVM_Type.Int())]), false);
					Check(
						Record,
						mVM_Type.Record([("Number", mVM_Type.Int()), ("Extra", mVM_Type.Bool())]),
						false
					);
					Check(
						mVM_Type.Set(mVM_Type.Int(), mVM_Type.Bool()),
						mVM_Type.Set(mVM_Type.False(), mVM_Type.Set(mVM_Type.Int(), mVM_Type.True())),
						true
					);
					var Binder = mVM_Type.Free("t", mVM_Type.Type());
					var Renamed = mVM_Type.Free("other", mVM_Type.Type());
					Check(mVM_Type.Set(mVM_Type.Int(), mVM_Type.Int()), mVM_Type.Int(), true);
					Check(
						mVM_Type.Set(Binder, mVM_Type.Int()).Substitute(Binder, mVM_Type.Int()),
						mVM_Type.Int(),
						true
					);
					Check(Binder, Binder, true);
					Check(Binder, mVM_Type.Free("t", mVM_Type.Type()), false);
					Check(
						mVM_Type.Abstract("Head", mVM_Type.Type()),
						mVM_Type.Abstract("Head", mVM_Type.Type()),
						false
					);
					Check(
						mVM_Type.Generic(Binder, mVM_Type.Pair(Binder, mVM_Type.Int())),
						mVM_Type.Generic(Renamed, mVM_Type.Pair(Renamed, mVM_Type.Int())),
						true
					);
					Check(
						mVM_Type.Recursive(Binder, mVM_Type.Pair(Binder, mVM_Type.Int())),
						mVM_Type.Recursive(Renamed, mVM_Type.Pair(Renamed, mVM_Type.Int())),
						true
					);
					Check(
						mVM_Type.Sig(Binder, Binder),
						mVM_Type.Sig(Renamed, Renamed),
						true
					);
					Check(null, null, true);
					Check(Record, null, false);
					mAssert.IsFalse(Record.Equals("not a type"));
				}
			),
			mTest.Test(
				"Procedure arguments are contravariant",
				__ => {
					var Wide = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Bool(), mVM_Type.Int());
					var Narrow = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.False(), mVM_Type.Int());
					mAssert.IsTrue(Wide.IsSubType(Narrow).Match(out _, out _));
					mAssert.IsFalse(Narrow.IsSubType(Wide).Match(out _, out _));
				}
			),
			mTest.Test(
				"Invariant type applications honor bindings already inferred from other operands",
				aDebug => {
					var Constructor = mVM_Type.Abstract(
						"Box",
						mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type())
					);
					var Parameter = mVM_Type.Free("Value", mVM_Type.Type());
					var Unknown = mVM_Type.Free("Unknown", mVM_Type.Type());
					var Function = mVM_Type.Generic(
						Parameter,
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Pair(
								Constructor.ApplyType(Parameter),
								mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), Parameter)
							),
							Parameter
						)
					);
					foreach (var ValueType in mStream.Stream(Unknown, mVM_Type.Var(mVM_Type.Int()))) {
						var Result = mVM_Type.Infer(
							Function,
							mVM_Type.Empty(),
							mVM_Type.Pair(
								Constructor.ApplyType(ValueType),
								mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int())
							),
							__ => {}
						);
						if (mStd.RefEq(ValueType, Unknown)) {
							mAssert.IsTrue(Result.AssertNotError(__ => __).IsInt());
						} else {
							mAssert.IsFalse(Result.Match(out _, out _));
						}
					}
				}
			),
			mTest.Test(
				"Free aliases use all known bindings without creating reciprocal cycles",
				aDebug => {
					foreach (
						var ValueType in mStream.Stream(
							mVM_Type.Int(),
							mVM_Type.Var(mVM_Type.Int()),
							mVM_Type.Any()
						)
					) {
						var First = mVM_Type.Free("First", mVM_Type.Type());
						var Second = mVM_Type.Free("Second", mVM_Type.Type());
						foreach (var (Left, Right) in mStream.Stream((First, Second), (Second, First))) {
							var Aliases = Left.IsSubType(Right).AssertNotError(__ => __);
							Aliases = Right.IsSubType(Left, Aliases).AssertNotError(__ => __);
							var Bound = ValueType.IsSubType(Right, Aliases).AssertNotError(__ => __);
							mAssert.IsTrue(Left.ApplyMappings(Bound).SameType(ValueType));
							mAssert.IsTrue(Right.ApplyMappings(Bound).SameType(ValueType));
							mAssert.IsFalse(Left.ApplyMappings(Aliases).IsImmutable);
							mAssert.IsTrue(First.IsFree());
							mAssert.IsTrue(Second.IsFree());
						}
					}
				}
			),
			mTest.Test(
				"IsImmutable distinguishes values from callable signatures",
				__ => {
					mAssert.IsTrue(mVM_Type.Type().IsImmutable);
					mAssert.IsTrue(
						mVM_Type.Proc(
							mVM_Type.Var(mVM_Type.Int()),
							mVM_Type.Any(),
							mVM_Type.Var(mVM_Type.Int())
						).IsImmutable
					);
					mAssert.IsFalse(mVM_Type.Any().IsImmutable);
					mAssert.IsFalse(mVM_Type.Var(mVM_Type.Int()).IsImmutable);
					mAssert.IsFalse(mVM_Type.Ref(mVM_Type.Int()).IsImmutable);
					mAssert.IsFalse(mVM_Type.Record([("Value", mVM_Type.Int())]).IsImmutable);
				}
			),
			mTest.Test(
				"IsImmutable is transitive through tuples, unions and SIG payloads",
				__ => {
					var Head = mVM_Type.Free("t", mVM_Type.Type());
					var Immutable = mVM_Type.Pair(mVM_Type.Bool(), mVM_Type.Prefix("Value", mVM_Type.Int()));
					var Mutable = mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Prefix("Value", mVM_Type.Var(mVM_Type.Int())));
					mAssert.IsTrue(Immutable.IsImmutable);
					mAssert.IsFalse(Mutable.IsImmutable);
					mAssert.IsTrue(mVM_Type.Sig(Head, Immutable).IsImmutable);
					mAssert.IsFalse(mVM_Type.Sig(Head, Mutable).IsImmutable);
					mAssert.IsFalse(mVM_Type.Set(Immutable, Mutable).IsImmutable);
					mAssert.IsFalse(mVM_Type.Set(Immutable, mVM_Type.Any()).IsImmutable);
				}
			),
			mTest.Test(
				"IsImmutable is recomputed by generic substitution",
				__ => {
					var Head = mVM_Type.Free("t", mVM_Type.Type());
					var Constructor = mVM_Type.Generic(Head, mVM_Type.Pair(mVM_Type.Int(), Head));
					mAssert.IsFalse(Constructor.IsImmutable);
					mAssert.IsTrue(Constructor.ApplyType(mVM_Type.Int()).IsImmutable);
					mAssert.IsFalse(Constructor.ApplyType(mVM_Type.Var(mVM_Type.Int())).IsImmutable);
					mAssert.IsFalse(Constructor.IsImmutable);
					mAssert.IsTrue(mVM_Type.Generic(Head, mVM_Type.Proc(mVM_Type.Empty(), Head, Head)).IsImmutable);
					mAssert.IsFalse(mVM_Type.Interface(Head, Head).IsImmutable);
					// The hidden receiver may be mutable even when the method table is immutable.
					mAssert.IsFalse(mVM_Type.Interface(Head, mVM_Type.Int()).IsImmutable);
				}
			),
			mTest.Test(
				"IsImmutable resolves recursive structure without hiding mutable payloads",
				__ => {
					var Head = mVM_Type.Free("List", mVM_Type.Type());
					var Item = mVM_Type.Free("Item", mVM_Type.Type());
					var List = mVM_Type.Recursive(Head, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Head, Item)));
					mAssert.IsFalse(List.IsImmutable);
					mAssert.IsTrue(List.Substitute(Item, mVM_Type.Int()).IsImmutable);
					mAssert.IsFalse(List.Substitute(Item, mVM_Type.Var(mVM_Type.Int())).IsImmutable);
					mAssert.IsTrue(mVM_Type.Text().IsImmutable);
				}
			),
			mTest.Tests(
				"Type error output",
				[
					mTest.Test(
						"Nested differences retain their path in a single comparison",
						aDebug => {
							static mVM_Type.tType Type(mVM_Type.tType aValue) => mVM_Type.Proc(
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
							aDebug(Error);
							mAssert.AreEquals(Error.Split("found:").Length, 2);
							mAssert.AreEquals(Error.Split("expected:").Length, 2);
							mAssert.IsFalse(Error.Contains("in:") || Error.Contains("Ignored"));
							mAssert.IsTrue(Error.Contains("Value : [ ..., §TRUE ]"));
							mAssert.IsTrue(Error.Contains("Value : [ ..., §INT ]"));
							mAssert.IsTrue(Error.Contains("-> ..."));
						}
					),
					mTest.Test(
						"Different prefix names remain visible around common payloads",
						aDebug => mAssert.AreEquals(
							TypeError(mVM_Type.Prefix("Left", mVM_Type.Int()), mVM_Type.Prefix("Right", mVM_Type.Int())),
							"found:\n  [ #Left ... ]\nexpected:\n  [ #Right ... ]"
						)
					),
					mTest.Test(
						"Missing fields retain their names and types",
						aDebug => mAssert.AreEquals(
							TypeError(
								mVM_Type.Record([("Common", mVM_Type.Int())]),
								mVM_Type.Record([("Common", mVM_Type.Int()), ("Missing", mVM_Type.True())])
							),
							"missing field 'Missing'\nfound:\n  [{ ... }]\nexpected:\n  [{ ...,  Missing : §TRUE }]"
						)
					),
					mTest.Test(
						"Generic omissions align bound parameters by identity",
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
					mTest.Test(
						"Recursive omissions retain the data shape without unfolding it",
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
						Free.ApplyMappings(Updated).SameType(mVM_Type.Set(mVM_Type.Int(), mVM_Type.False()))
					);
				}
			),
			mTest.Test(
				"ApplyMappings uses free type instances",
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
			mTest.Tests(
				"Pair projection",
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
							var Type = mSPO_Parser.Type.TryParseText(a.Type, "Pair", __ => aDebugStream(__())).ElseThrow()
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var _1 = mSPO_Parser.Type.TryParseText(a._1, "FirstPart", __ => aDebugStream(__())).ElseThrow()
							.AsVM_Type(mStd.cEmpty)
							.AssertNotError(__ => __.ToText());
							
							var _2 = mSPO_Parser.Type.TryParseText(a._2, "SecondPart", __ => aDebugStream(__())).ElseThrow()
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
			mTest.Test(
				"Pair projection fails for non pairs",
				aDebugStream => {
					mAssert.IsFalse(mVM_Type.Int().TryProjectPair(out _, out _));
				}
			),
			mTest.Test(
				"SplitBy expands recursive types",
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
				a => mTest.Test(
					a.Expr + " => " + a.Type,
					aStreamOut => {
					var AST =  mSPO_Parser.Expression.TryParseText(
						a.Expr,
						"",
						__ => aStreamOut(__())
					).ElseThrow();
					
					var Type = AST.UpdateTypes(
						cTestScope,
						new()
					).AssertNotError(
						__ => __.ToText()
					).Type;
					
					var VM_Type = mSPO_Parser.Type.TryParseText(
						a.Type,
						"",
						__ => { aStreamOut(__()); }
					).ElseThrow().DesugarType(
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