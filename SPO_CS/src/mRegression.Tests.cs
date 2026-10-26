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
#:ref Common/mArrayList.cs
#:ref Common/mTreeMap.cs
#:ref Common/mTextStream.cs
#:ref Common/mSpan.cs
#:ref mSPO_AST.cs
#:ref mSPO_Parser.cs
#:ref mSPO_Interpreter.cs
#:ref mVM_Type.cs
#:ref mVM_Data.cs
#:ref mVM.cs
#:ref mIL_Parser.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mRegression_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"Regressions",
		[
			mTest.Test(
				"Empty prefix expressions distinguish omitted and explicit arguments",
				aDebug => {
					foreach (var Parser in new[] { mSPO_Parser.Expression, mSPO_Parser.ExpressionInCall }) {
						foreach (
							var (Source, Name) in new[] {
								("#Prefix", "_Prefix"),
								("(#Prefix)", "_Prefix"),
								("(#Prefix ())", "_Prefix...")
							}
						) {
							var Prefix = (mSPO_AST.tPrefixNode<tSpan>)Parser.TryParseText(
								Source,
								"",
								__ => aDebug(__())
							).ElseThrow();
							
							mAssert.AreEquals(Prefix.Prefix, Name);
							mAssert.IsTrue(Prefix.Element is mSPO_AST.tEmptyNode<tSpan>);
						}
					}
				}
			),
			mTest.Test(
				"Empty prefix types distinguish omitted and explicit arguments",
				aDebug => {
					foreach (var (Source, Name) in new[] { ("[#Prefix]", "_Prefix"), ("[#Prefix []]", "_Prefix...") }) {
						var Prefix = (mSPO_AST.tPrefixTypeNode<tSpan>)mSPO_Parser.Type.TryParseText(
							Source, "", __ => aDebug(__())
						).ElseThrow();
						
						mAssert.AreEquals(Prefix.Prefix.Id, Name);
						mAssert.IsTrue(Prefix.Expressions.All(__ => __ is mSPO_AST.tEmptyTypeNode<tSpan>));
					}
				}
			),
			mTest.Test(
				"Empty prefix patterns distinguish omitted and explicit arguments",
				aDebug => {
					foreach (
						var (Source, Name) in new[] {
							("#Prefix", "_Prefix"),
							("(#Prefix)", "_Prefix"),
							("(#Prefix ())", "_Prefix...")
						}
					) {
						var Prefix = (mSPO_AST.tPrefixPatternNode<tSpan>)mSPO_Parser.Pattern.TryParseText(
							Source, "", __ => aDebug(__())
						).ElseThrow();
						mAssert.AreEquals(Prefix.Prefix, Name);
						mAssert.IsTrue(Prefix.Pattern is mSPO_AST.tEmptyNode<tSpan>);
					}
				}
			),
			mTest.Test(
				"Empty function calls distinguish omitted and explicit arguments",
				aDebug => {
					foreach (var (Source, Name) in new[] { (".F", "_F"), (".F ()", "_F...") }) {
						var Call = mSPO_Parser.Call.TryParseText(Source, "", __ => aDebug(__())).ElseThrow();
						mAssert.AreEquals(((mSPO_AST.tIdNode<tSpan>)Call.Func).Id, Name);
						mAssert.IsTrue(Call.Arg is mSPO_AST.tEmptyNode<tSpan>);
					}
				}
			),
			mTest.Test(
				"Empty method calls distinguish omitted and explicit arguments",
				aDebug => {
					foreach (var (Source, Name) in new[] { ("M", "_M"), ("M ()", "_M...") }) {
						var Call = mSPO_Parser.MethodCall.TryParseText(Source, "", __ => aDebug(__())).ElseThrow();
						mAssert.AreEquals(Call.Method.Id, Name);
						mAssert.IsTrue(Call.Argument is mSPO_AST.tEmptyNode<tSpan>);
					}
				}
			),
			mTest.Test(
				"Multiline empty parentheses work as expressions and patterns",
				aDebug => {
					foreach (var Source in new[] { "()", "(\n)", "(\n\n\t)" }) {
						foreach (var Parser in new[] { mSPO_Parser.Expression, mSPO_Parser.ExpressionInCall }) {
							var Value = Parser.TryParseText(Source, "", __ => aDebug(__())).ElseThrow();
							mAssert.IsTrue(Value is mSPO_AST.tEmptyNode<tSpan>);
						}
						var Pattern = mSPO_Parser.Pattern.TryParseText(Source, "", __ => aDebug(__())).ElseThrow();
						mAssert.IsTrue(Pattern is mSPO_AST.tEmptyNode<tSpan>);
					}
				}
			),
			mTest.Test(
				"Method expression bodies retain their expression node",
				aDebug => {
					var Method = mSPO_Parser.Method.TryParseText(
						"(§VAR aSelf € §INT) : () => §TO_VAL aSelf",
						"",
						__ => aDebug(__())
					).ElseThrow();
					
					mAssert.IsTrue((mSPO_AST.tExpressionNode<tSpan>)Method.Body is mSPO_AST.tVarToValNode<tSpan>);
				}
			),
			mTest.Test(
				"Method expression bodies preserve opaque argument access checks",
				aDebug => {
					const tText cSource = """
					§DEF Read... = (
						§VAR aSelf € §INT
					) : (
						§DEF aValue € [§VAR §INT]
					) => §TO_VAL aValue
					
					§EXPORT 1
					""";
					
					var Result = mSPO_Interpreter.Run(
						cSource,
						"",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						__ => aDebug(__())
					);
					
					mAssert.IsFalse(Result.Match(out _, out var Error));
					mAssert.IsTrue(Error.Contains("opaque mutable access is not allowed"));
				}
			),
			mTest.Test(
				"Recursive definitions accept method blocks and expressions with optional parentheses",
				aDebug => {
					foreach (var Body in new[] { "{\n§RETURN §TO_VAL aSelf\n}", "=> §TO_VAL aSelf" }) {
						foreach (var Parenthesized in new[] { false, true }) {
							var Method = "(§VAR aSelf € §INT) : () " + Body;
							var Source = "§DEF Read = " + (Parenthesized ? "(" + Method + ")" : Method);
							var Item = mSPO_Parser.RecProcItem.TryParseText(
								Source,
								"",
								__ => aDebug(__())
							).ElseThrow();
							
							mSPO_AST.tExpressionNode<tSpan> Definition = Item.Proc;
							mAssert.IsTrue(Definition is mSPO_AST.tMethodNode<tSpan>);
						}
					}
				}
			),
			mTest.Test(
				"Recursive definitions reject non-callable expressions",
				aDebug => mAssert.IsFalse(
					mSPO_Parser.RecProcItem.TryParseText(
						"§DEF Value = 1",
						"",
						__ => aDebug(__())
					).Match(
						out _,
						out _
					)
				)
			),
			mTest.Tests(
				"Recursive methods enforce receiver and mutable access restrictions",
				mStream.Stream<(tText Name, tText Source, tText Error)>(
					(
						"Opaque argument",
						"""
						§RECURSIVE {
							§DEF Read... = (
								§VAR aSelf € §INT
							) : (
								§DEF aValue € [§VAR §INT]
							) => §TO_VAL aValue
						}
						
						§EXPORT 1
						""",
						"opaque mutable access is not allowed"
					),
					(
						"Mutable capture",
						"""
						§VAR Cell := 7 .
						
						§RECURSIVE {
							§DEF Read = (
								§VAR aSelf € §INT
							) : () => §TO_VAL Cell
						}
						
						§EXPORT 1
						""",
						"mutable closure environment is not allowed"
					),
					(
						"Wrong receiver type",
						"""
						§RECURSIVE {
							§DEF Read = (
								§VAR aSelf € §INT
							) : () => §TO_VAL aSelf
						}
						
						§VAR Cell :
							= §TRUE
							Read => §DEF Value
						.
						
						§EXPORT Value
						""",
						"INT"
					)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						aDebug => {
							mSPO_Parser.Module.TryParseText(aCase.Source, "", __ => aDebug(__())).ElseThrow();
							
							var Result = mSPO_Interpreter.Run(
								aCase.Source,
								"",
								(mVM_Data.Empty(), mVM_Type.Empty()),
								__ => aDebug(__())
							);
							
							mAssert.IsFalse(Result.Match(out _, out var Error));
							mAssert.IsTrue(Error.Contains(aCase.Error));
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Tests(
				"IF skips unselected result expressions",
				mStream.Stream<(tText Name, tText Expression, mVM_Data.tData Expected)>(
					(
						"False case with fallback",
						"""
						§IF {
							§FALSE: .Probe
							§TRUE: 7
						}
						""",
						mVM_Data.Int(7)
					),
					(
						"False case without fallback",
						"""
						§IF {
							§FALSE: .Probe
						}
						""",
						mVM_Data.Empty()
					),
					(
						"Selected empty result",
						"""
						§IF {
							§TRUE: ()
							§TRUE: .Probe
						}
						""",
						mVM_Data.Empty()
					)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						aDebug => {
							var CallCount = 0;
							var Import = mVM_Data.ExternProc(
								(aEnv, aObj, aArg, aTrace) => {
									CallCount++;
									return mVM_Data.Int(99);
								},
								mVM_Data.Empty()
							);
							var ImportType = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Int());
							var Result = mSPO_Interpreter.Run(
								$"""
								§IMPORT (§DEF Probe € [[] => §INT])
								§EXPORT {aCase.Expression}
								""",
								"",
								(Import, ImportType),
								__ => aDebug(__())
							).ElseThrow();
							
							mAssert.AreEquals(CallCount, 0);
							mAssert.AreEquals(Result.Data.ToText(1000), aCase.Expected.ToText(1000));
						}
					)
				).ToArrayList(
				).ToArray()
			),
			mTest.Test(
				"Generic methods accept single and multiple type parameters with either body form",
				aDebug => {
					foreach (var Parameters in new[] { "(§DEF t)", "(§DEF t € §TYPE)", "(§DEF t, §DEF u)" }) {
						foreach (var Body in new[] { "=> aValue", "{\n§RETURN aValue\n}" }) {
							var Source = Parameters + " <=> (§VAR aSelf € §INT) : (§DEF aValue € t) " + Body;
							mSPO_Parser.Method.TryParseText(Source, "", __ => aDebug(__())).ElseThrow();
						}
					}
				}
			),
			mTest.Tests(
				"Generic methods preserve type constraints and parameter scope",
				mStream.Stream<(tText Name, tText Source, tText Error)>(
					(
						"Receiver and argument use the same type parameter",
						"""
						§DEF Same... = (
							§DEF t
						) <=> (
							§VAR aSelf € t
						) : (
							§DEF aValue € t
						) => aValue
						
						§VAR Cell :
							= 7
							Same §TRUE => §DEF Result
						.
						
						§EXPORT Result
						""",
						"INT"
					),
					(
						"Type parameters do not escape the method",
						"""
						§DEF Same... = (
							§DEF t
						) <=> (
							§VAR aSelf € §INT
						) : (
							§DEF aValue € t
						) => aValue
						
						§DEF Outside € t = 7
						
						§EXPORT Outside
						""",
						"_t"
					)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						aDebug => {
							mSPO_Parser.Module.TryParseText(aCase.Source, "", __ => aDebug(__())).ElseThrow();
							var Result = mSPO_Interpreter.Run(
								aCase.Source,
								"",
								(mVM_Data.Empty(), mVM_Type.Empty()),
								__ => aDebug(__())
							);
							mAssert.IsFalse(Result.Match(out _, out var Error));
							mAssert.IsTrue(Error.Contains(aCase.Error));
						}
					)
				).ToArrayList(
				).ToArray()
			),
			mTest.Tests(
				"Removing from a two entry tree preserves the other entry and the original tree",
				mStream.Stream(0, 1, 2, 3).Map(
					aKey => mTest.Test(
						"Remove " + aKey,
						__ => {
							var Tree = mTreeMap.Tree<tInt32, tText>(
								(aLeft, aRight) => aLeft.CompareTo(aRight),
								[(1, "one"), (2, "two")]
							);
							
							var Removed = Tree.Remove(aKey);
							
							mAssert.AreEquals(Removed.TryGet(1).IsNone(), aKey == 1);
							mAssert.AreEquals(Removed.TryGet(2).IsNone(), aKey == 2);
							
							if (aKey != 1) {
								mAssert.AreEquals(Removed.TryGet(1).AssertNotEmpty(), "one");
							}
							
							if (aKey != 2) {
								mAssert.AreEquals(Removed.TryGet(2).AssertNotEmpty(), "two");
							}
							
							mAssert.AreEquals(Tree.TryGet(1).AssertNotEmpty(), "one");
							mAssert.AreEquals(Tree.TryGet(2).AssertNotEmpty(), "two");
						}
					)
				).ToArrayList(
				).ToArray()
			),
			mTest.Test(
				"Distinct record field names do not collide at runtime",
				__ => {
					mAssert.IsFalse("_DefaultStyle".PrefixHash() == "_OldStyle".PrefixHash());
					var Record = mVM_Data.Record([
						("_DefaultStyle", mVM_Data.Int(1)),
						("_OldStyle", mVM_Data.Int(2))
					]);
					mAssert.IsTrue(Record.IsRecord(out var Fields));
					mAssert.AreEquals(Fields.TryGet("_DefaultStyle".PrefixHash()).AssertNotEmpty(), mVM_Data.Int(1));
					mAssert.AreEquals(Fields.TryGet("_OldStyle".PrefixHash()).AssertNotEmpty(), mVM_Data.Int(2));
				}
			),
			mTest.Tests(
				"Type equality includes record field names and types",
				mStream.Stream(
					mVM_Type.Record([("Number", mVM_Type.Bool())]),
					mVM_Type.Record([("Other", mVM_Type.Int())]),
					mVM_Type.Record([("Number", mVM_Type.Int()), ("Extra", mVM_Type.Bool())])
				).Map(
					aOther => mTest.Test(
						aOther.ToText(),
						__ => {
							var Record = mVM_Type.Record([("Number", mVM_Type.Int())]);
							mAssert.IsFalse(Record == aOther);
							mAssert.IsFalse(aOther == Record);
							mAssert.IsFalse(Record.Equals(aOther));
							mAssert.IsFalse(Record.SameType(aOther));
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Test(
				"Free type parameters with the same spelling retain their identity",
				__ => {
					var First = mVM_Type.Free("t", mVM_Type.Type());
					var Second = mVM_Type.Free("t", mVM_Type.Type());
					mAssert.IsFalse(First == Second);
					mAssert.IsFalse(First.Equals(Second));
					mAssert.IsFalse(First.SameType(Second));
				}
			),
			mTest.Tests(
				"Bound type parameter names do not affect equality",
				mStream.Stream<(tText Name, mStd.tFunc<mVM_Type.tType, mVM_Type.tType> Bind)>(
					("Generic", aHead => mVM_Type.Generic(aHead, mVM_Type.Pair(aHead, mVM_Type.Int()))),
					("Recursive", aHead => mVM_Type.Recursive(aHead, mVM_Type.Pair(aHead, mVM_Type.Int()))),
					("SIG", aHead => mVM_Type.Sig(aHead, aHead))
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						__ => {
							var Left = aCase.Bind(mVM_Type.Free("t", mVM_Type.Type()));
							var Right = aCase.Bind(mVM_Type.Free("other", mVM_Type.Type()));
							mAssert.IsTrue(Left == Right);
							mAssert.IsTrue(Right == Left);
							mAssert.IsTrue(Left.Equals(Right));
							mAssert.IsTrue(Left.SameType(Right));
							mAssert.AreEquals(Left.GetHashCode(), Right.GetHashCode());
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Test(
				"Union equality ignores member order and duplicate members",
				__ => {
					var Left = mVM_Type.Set(mVM_Type.Int(), mVM_Type.Bool());
					var Right = mVM_Type.Set(mVM_Type.False(), mVM_Type.Set(mVM_Type.Int(), mVM_Type.True()));
					mAssert.IsTrue(Left == Right);
					mAssert.IsTrue(Left.SameType(Right));
					mAssert.IsTrue(mVM_Type.Set(mVM_Type.Int(), mVM_Type.Int()) == mVM_Type.Int());
				}
			),
			mTest.Test(
				"Type equality with an unrelated object returns false",
				__ => mAssert.IsFalse(mVM_Type.Int().Equals("not a type"))
			),
			mTest.Tests(
				"Generic inference retains EMPTY alternatives in either argument order",
				mStream.Stream(false, true).Map(
					aEmptyFirst => mTest.Test(
						aEmptyFirst ? "EMPTY then INT" : "INT then EMPTY",
						aDebug => {
							var Parameter = mVM_Type.Free("t", mVM_Type.Type());
							var Function = mVM_Type.Generic(
								Parameter,
								mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Pair(Parameter, Parameter), Parameter)
							);
							var Argument = (
								aEmptyFirst
								? mVM_Type.Pair(mVM_Type.Empty(), mVM_Type.Int())
								: mVM_Type.Pair(mVM_Type.Int(), mVM_Type.Empty())
							);
							var Result = mVM_Type.Infer(Function, mVM_Type.Empty(), Argument, __ => {}).ElseThrow();
							mAssert.IsTrue(Result.SameType(mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Int())));
						}
					)
				).ToArrayList().ToArray()
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
			mTest.Tests(
				"Invariant type applications respect bindings inferred from other arguments",
				mStream.Stream(false, true).Map(
					aMutable => mTest.Test(
						aMutable ? "Reject conflicting mutable payload" : "Resolve unknown payload",
						aDebug => {
							var Constructor = mVM_Type.Abstract(
								"Box", mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type())
							);
							var Parameter = mVM_Type.Free("Value", mVM_Type.Type());
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
							var Payload = aMutable ? mVM_Type.Var(mVM_Type.Int()) : mVM_Type.Free("Unknown", mVM_Type.Type());
							var Result = mVM_Type.Infer(
								Function,
								mVM_Type.Empty(),
								mVM_Type.Pair(
									Constructor.ApplyType(Payload),
									mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int())
								),
								__ => {}
							);
							
							if (aMutable) {
								mAssert.IsFalse(Result.Match(out _, out _));
							} else {
								mAssert.IsTrue(Result.ElseThrow().IsInt());
							}
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Tests(
				"Free aliases resolve transitively in either binding order",
				mStream.Stream(false, true).Map(
					aReverse => mTest.Tests(
						aReverse ? "Second first" : "First first",
						mStream.Stream(mVM_Type.Int(), mVM_Type.Var(mVM_Type.Int()), mVM_Type.Any()).Map(
							aValueType => mTest.Test(
								aValueType.ToText(),
								__ => {
									var First = mVM_Type.Free("First", mVM_Type.Type());
									var Second = mVM_Type.Free("Second", mVM_Type.Type());
									var Left = aReverse ? Second : First;
									var Right = aReverse ? First : Second;
									var Aliases = Left.IsSubType(Right).ElseThrow();
									Aliases = Right.IsSubType(Left, Aliases).ElseThrow();
									var Bound = aValueType.IsSubType(Right, Aliases).ElseThrow();
									mAssert.IsTrue(Left.ApplyMappings(Bound).SameType(aValueType));
									mAssert.IsTrue(Right.ApplyMappings(Bound).SameType(aValueType));
									mAssert.AreEquals(First.Kind, mVM_Type.tKind.Free);
									mAssert.AreEquals(Second.Kind, mVM_Type.tKind.Free);
								}
							)
						).ToArrayList().ToArray()
					)
				).ToArrayList().ToArray()
			),
			mTest.Tests(
				"Module imports enforce immutability on actual values",
				mStream.Stream<(tText Name, mVM_Data.tData Value, tBool Denied)>(
					("Direct cell", mVM_Data.Var(mVM_Data.Int(7)), true),
					("Cell inside record", mVM_Data.Record([("Cell", mVM_Data.Var(mVM_Data.Int(7)))]), true),
					("Cell inside tuple", mVM_Data.Pair(mVM_Data.Empty(), mVM_Data.Var(mVM_Data.Int(7))), true),
					("Immutable record", mVM_Data.Record([("Value", mVM_Data.Int(7))]), false)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						aDebug => {
							const tText cSource = """
							§TYPES
								F := [ANY => INT]
								D := [EMPTY_TYPE => F]
							§DEF main € D
								§RETURN ONE IF TRUE
							""";
							
							var Module = mIL_Parser.Module.TryParseText(cSource + "\n", "", __ => aDebug(__())).ElseThrow();
							
							var Result = mVM.TryRun(
								Module,
								(aCase.Value, mVM_Type.Any()),
								mTextParser.ToText,
								__ => aDebug(__())
							);
							
							if (aCase.Denied) {
								mAssert.IsFalse(Result.Match(out _, out var Error));
								mAssert.AreEquals(Error, "mutable module import is not allowed");
							} else {
								mAssert.AreEquals(Result.ElseThrow().Data, mVM_Data.Int(1));
							}
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Test(
				"MATCH captures immutable external callables",
				aDebug => {
					const tText cSource = """
						§IMPORT (§DEF Apply... € [§INT => §INT])
						§EXPORT §IF 7 MATCH {
							0: 0
							§DEF Item: .Apply Item
						}
						""";
					var Import = mVM_Data.ExternProc((aEnv, aObj, aArg, aTrace) => aArg, mVM_Data.Empty());
					var Type = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int());
					var Result = mSPO_Interpreter.Run(cSource, "", (Import, Type), __ => aDebug(__())).ElseThrow();
					mAssert.AreEquals(Result.Data, mVM_Data.Int(7));
				}
			)
		]
	);
}