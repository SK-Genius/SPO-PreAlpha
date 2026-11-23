#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
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
				"Recursive text extensions are proved without binding recursive heads",
				aDebug => {
					var Text = mVM_Type.Text();
					var Head = mVM_Type.Free("Extended", mVM_Type.Type());
					var Extended = mVM_Type.Recursive(Head, mVM_Type.Set(Text, mVM_Type.Pair(Head, mVM_Type.Char())));
					mAssert.IsTrue(Extended.IsSubType(Text).ElseThrow().Deep() == 0);
					mAssert.IsTrue(Text.IsSubType(Extended).ElseThrow().Deep() == 0);
					var OtherHead = mVM_Type.Free("Other", mVM_Type.Type());
					var Booleans = mVM_Type.Recursive(OtherHead, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(OtherHead, mVM_Type.Bool())));
					mAssert.IsTrue(Extended.IsSubType(Booleans).IsFail(out _, out _));
				}
			),
			mTest.Test(
				"A recursive assumption does not hide incompatible fields or unguarded targets",
				aDebug => {
					static mVM_Type.tType Node(mVM_Type.tType aValue) {
						var Head = mVM_Type.Free("Node", mVM_Type.Type());
						return mVM_Type.Recursive(Head, mVM_Type.Record([("Next", Head), ("Value", aValue)]));
					}
					
					mAssert.IsTrue(Node(mVM_Type.Int()).IsSubType(Node(mVM_Type.Bool())).IsFail(out _, out _));
					var Loop = mVM_Type.Free("Loop", mVM_Type.Type());
					mAssert.IsTrue(mVM_Type.Int().IsSubType(mVM_Type.Recursive(Loop, Loop)).IsFail(out _, out _));
				}
			),
			mTest.Test(
				"Fold inference checks both accumulator bounds after choosing its type",
				aDebug => {
					var Result = mVM_Type.Free("Result", mVM_Type.Type());
					var Empty = mVM_Type.Empty();
					var Signature = mVM_Type.Generic(Result, mVM_Type.Proc(Empty, mVM_Type.Tuple([Result, mVM_Type.Proc(Empty, Result, Result)]), Result));
					var Initial = mVM_Type.Record([("Value", mVM_Type.False())]);
					var State = mVM_Type.Record([("Value", mVM_Type.Bool())]);
					var Callback = mVM_Type.Proc(Empty, State, State);
					var Inferred = mVM_Type.InferCall(Signature, Empty, mVM_Type.Tuple([Initial, Callback]), _ => {}).ElseThrow();
					mAssert.IsTrue(Inferred.Result.IsSubType(State).Match(out _, out _));
					mAssert.IsTrue(State.IsSubType(Inferred.Result).Match(out _, out _));
					var Invalid = mVM_Type.Proc(Empty, mVM_Type.Int(), mVM_Type.Bool());
					mAssert.IsTrue(mVM_Type.InferCall(Signature, Empty, mVM_Type.Tuple([mVM_Type.Int(), Invalid]), _ => {}).IsFail(out _, out _));
				}
			),
			mTest.Test(
				"Recursive list refinements retain their closed head",
				aDebug => {
					var Head = mVM_Type.Free("List", mVM_Type.Type());
					var List = mVM_Type.Recursive(Head, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Head, mVM_Type.Int())));
					var Expanded = List.Refs[1].Substitute(Head, List);
					mAssert.IsTrue(List.IsSubType(Expanded).Match(out _, out _));
					mAssert.IsTrue(Expanded.IsSubType(List).Match(out _, out _));
					var One = mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int());
					var Remaining = List.Subtract(One).AssertNotEmpty();
					mAssert.IsTrue(Remaining.IsSubType(List).Match(out _, out _));
					mAssert.IsFalse(One.IsSubType(Remaining).Match(out _, out _));
					mAssert.IsTrue(mVM_Type.Pair(One, mVM_Type.Int()).IsSubType(Remaining).Match(out _, out _));
				}
			),
			mTest.Test(
				"Opening a type constructor preserves opaque argument views",
				aDebug => {
					var Kind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					var Constructor = mVM_Type.Abstract("List", Kind);
					var Parameter = mVM_Type.Free("Element", mVM_Type.Type());
					var Head = mVM_Type.Free("Node", mVM_Type.Type());
					var Known = mVM_Type.Generic(Parameter, mVM_Type.Recursive(Head, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Head, Parameter))));
					var Inner = Constructor.ApplyType(mVM_Type.Int());
					var Nested = Constructor.ApplyType(Inner);
					var Opened = Nested.ResolveWitnesses(new(Constructor, Known, false), false);
					mAssert.IsTrue(Opened.SameType(Known.ApplyType(Inner)));
					mAssert.IsFalse(Opened.SameType(Known.ApplyType(Known.ApplyType(mVM_Type.Int()))));
				}
			),
			mTest.Test(
				"A function argument constrained by BOOL retains both alternatives",
				aDebug => {
					var Parameter = mVM_Type.Free("Value", mVM_Type.Type());
					var Source = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Bool(), mVM_Type.Int());
					var Target = mVM_Type.Proc(mVM_Type.Empty(), Parameter, mVM_Type.Int());
					var Mappings = Source.IsSubType(Target).ElseThrow();
					mAssert.IsTrue(Parameter.ApplyMappings(Mappings).SameType(mVM_Type.Bool()));
				}
			),
			mTest.Test(
				"A record is immutable exactly when its declared fields are immutable",
				aDebug => {
					var Inner = mVM_Type.Record([("Value", mVM_Type.Int())]);
					mAssert.IsTrue(Inner.IsImmutable);
					mAssert.IsTrue(mVM_Type.Record([("Inner", Inner)]).IsImmutable);
					mAssert.IsFalse(mVM_Type.Record([("Cell", mVM_Type.Var(Inner))]).IsImmutable);
				}
			),
			mTest.Tests(
				"Type error output",
				[
					mTest.Test(
						"Nested differences retain their path in a single comparison",
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
										Left,
										mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Left, mVM_Type.True()))
									),
									mVM_Type.Recursive(
										Right,
										mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Right, mVM_Type.Int()))
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
				"Inference rejects an unbound type containing itself",
				aDebug => {
					var Parameter = mVM_Type.Free("t", mVM_Type.Type());
					mAssert.IsFalse(mVM_Type.Pair(mVM_Type.Empty(), Parameter).IsSubType(Parameter).Match(out _, out _));
					mAssert.IsTrue(Parameter.IsSubType(Parameter).Match(out _, out _));
				}
			),
			mTest.Test(
				"Generic calls instantiate their binder separately from caller variables",
				aDebug => {
					var Parameter = mVM_Type.Free("t", mVM_Type.Type());
					var Function = mVM_Type.Generic(Parameter, mVM_Type.Proc(mVM_Type.Empty(), Parameter, Parameter));
					var Argument = mVM_Type.Set(Parameter, mVM_Type.Empty());
					var Bound = mVM_Type.BindCall(Function, mVM_Type.Empty(), Argument, _ => {}).AssertNotError(__ => __);
					mAssert.IsTrue(Bound.Result.SameType(Argument));
					mAssert.IsTrue(Bound.Mappings.TryGet(Parameter).IsNone());
				}
			),
			mTest.Test(
				"Universal signatures keep parameter identities separate",
				aDebug => {
					var A = mVM_Type.Free("a", mVM_Type.Type());
					var B = mVM_Type.Free("b", mVM_Type.Type());
					var First = mVM_Type.Generic(A, mVM_Type.Generic(B, mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Pair(A, B), A)));
					var Second = mVM_Type.Generic(A, mVM_Type.Generic(B, mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Pair(A, B), B)));
					mAssert.IsFalse(First.IsSubType(Second).Match(out _, out _));
					var Identity = mVM_Type.Generic(A, mVM_Type.Proc(mVM_Type.Empty(), A, A));
					var Outer = mVM_Type.Free("outer", mVM_Type.Type());
					mAssert.IsFalse(mVM_Type.Proc(mVM_Type.Empty(), Outer, Outer).IsSubType(Identity).Match(out _, out _));
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