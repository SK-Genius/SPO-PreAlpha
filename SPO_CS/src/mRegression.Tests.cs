#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
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
#:ref mModule.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mRegression_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"Regressions",
		[
			mTest.Test(
				"Constructed function types resolve captured TYPE parameters before ANY matches",
				_ => {
					var Export = mSPO_Interpreter.Run(
						"""
						§DEF Probe... = (§DEF aType € §TYPE) => {
							§DEF Identity... = (§DEF aValue € aType) => aValue
							§DEF tFunction = [aType => aType]
							§DEF Boxed € §ANY = Identity...
							§RETURN §IF Boxed MATCH {
								§DEF Candidate € tFunction: §TRUE
								_: §FALSE
							}
						}
						§EXPORT .Probe §INT
						""",
						"TYPE function identity",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					mAssert.IsTrue(Export.Data.IsBool(out var Value) && Value);
				}
			),
			mTest.Tests(
				"TYPE-selected structural views keep their representation and static function signatures",
				mStream.Stream(false, true).Map(
					aNative => mTest.Test(
						aNative ? "Native function" : "SPO function",
						_ => {
							var Empty = mVM_Type.Empty();
							var FunctionType = mVM_Type.Proc(Empty, mVM_Type.Any(), mVM_Type.Type());
							var Calls = 0;
							
							var Native = mVM_Data.ExternProc(
								FunctionType,
								(_, _, aArgument, _) => {
									Calls += 1;
									mAssert.IsTrue(aArgument._Value.Is(out mVM_Data.tDynamicValue Value) && Value.ActualType.IsInt());
									return mVM_Data.TypeExpression(mVM_Type.Any());
								},
								mVM_Data.Empty()
							);
							
							var Prefix = aNative
							? "§IMPORT {Public...: §DEF Public... € [§ANY => [§TYPE]]}"
							: "§DEF GetType... = (§DEF t € §TYPE) <=> (§DEF aValue € t) => t\n§DEF Public... = (§DEF aValue € §ANY) => .GetType aValue";
							
							var Import = aNative ? (mVM_Data.Record([("_Public...", Native)]), mVM_Type.Record([("_Public...", FunctionType)])) : (mVM_Data.Empty(), Empty);
							
							var Export = mSPO_Interpreter.Run(
								Prefix + "\n" +
								"""
								§DEF tPackage = [§SIG_WITH t € §TYPE IN t]
								§DEF Seal... = (§DEF aType € §TYPE, §DEF aValue € §ANY) => §IF aValue MATCH {
									§DEF Candidate € aType: §SIG tPackage WITH aType IN Candidate
									_: §SIG tPackage WITH [] IN ()
								}
								§DEF tView = [< Value: §ANY, Invoke...: [§INT => §ANY] >]
								§DEF Package = .Seal (tView, {Value: 7, Invoke...: Public..., Hidden: 9})
								§EXPORT §IF Package MATCH {
									§SIG tPackage WITH tView IN {Value: §DEF Value, Invoke...: §DEF Invoke...}: (
										§IF Value MATCH {
											§DEF Number € §INT: Number
											_: 0
										}
										§IF (.Invoke 7) MATCH {
											§DEF tFound € [§TYPE]: tFound
											_: §INT
										}
									)
									§SIG tPackage WITH _ IN _: (0, §INT)
								}
								""",
								"TYPE-selected representation",
								Import,
								_ => {}
							).ElseThrow();
							
							var Expected = mVM_Data.Tuple([mVM_Data.Int(7), mVM_Data.TypeExpression(mVM_Type.Any())]);
							mAssert.IsTrue(Export.SameVisibleValue((Expected, Export.Type)));
							mAssert.AreEquals(Calls, aNative ? 1 : 0);
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Test(
				"Functions packed with a TYPE parameter retain their explicit witnesses at native entries",
				aDebug => {
					var Factory = mSPO_Interpreter.Run(
						"""
						§DEF tPackage = [§SIG_WITH t € §TYPE IN [t => §BOOL]]
						§EXPORT (§DEF aType € §TYPE) => {
							§DEF Function... = (§DEF aValue € aType) => §TRUE
							§RETURN §SIG tPackage WITH aType IN Function...
						}
						""",
						"Captured TYPE entry",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					var Package = Factory.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.TypeInt(), mVM_Type.Type()), _ => "", mVM.cNoTrace);
					var Function = Package.OpenSig<tSpan>();
					Function.Data.Normalize(Function.Type);
					var Result = Function.Data.Call<tSpan>(mVM_Data.Empty(), mVM_Data.Int(7), _ => "", mVM.cNoTrace);
					mAssert.IsTrue(Result.IsBool(out var Value) && Value);
					mAssert.ThrowsError(() => Function.Data.Call<tSpan>(mVM_Data.Empty(), mVM_Data.Bool(false), _ => "", mVM.cNoTrace));
				}
			),
			mTest.Test(
				"Cached native modules validate payloads before any initializer effects",
				aDebug => {
					static mModule.tModuleSetup Setup(tText aName, tText aSource, mStream.tStream<(tText Key, mModule.tModuleSetup Module)> aDependencies = default) {
						var Module = mModule.ModuleSetup(new mFS.tPath(aName), aDependencies);
						Module.IL = mSPO_Parser.Module.TryParseText(aSource, aName, _ => {}).ElseThrow().Check().ElseThrow();
						Module.Checked = mResult.OK(mStd.cEmpty).WithErrorType<tText>();
						return Module;
					}
					
					var Calls = 0;
					var Native = Setup("Native payload", "§EXPORT {Bad: 7, Run: () => ()}");
					var Function = mVM_Data.ExternProc(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty()), (_, _, _, _) => { Calls += 1; return mVM_Data.Empty(); }, mVM_Data.Empty());
					var Type = mIL_TypeChecker.ModuleSignature(Native.IL!).ModifyError(mIL_TypeChecker.ErrorText).ElseThrow().Export;
					Native.Module = mResult.OK((mVM_Data.Record([("_Bad", mVM_Data.Bool(false)), ("_Run", Function)]), Type)).WithErrorType<tText>();
					
					var Consumer = Setup(
						"Native payload consumer",
						"§IMPORT {Native: {Bad: §DEF Bad € §INT, Run: §DEF Run € [[] => []]}}\n§DEF Done = .Run\n§EXPORT 7",
						mStream.Stream(("Native", Native))
					);
					
					mAssert.ThrowsError(() => Consumer.Init(_ => {}).ElseThrow());
					mAssert.AreEquals(Calls, 0);
				}
			),
			mTest.Test(
				"Generic TYPE results keep the opaque identity of an opened SIG head",
				aDebug => {
					var Export = mSPO_Interpreter.Run(
						"""
						§DEF GetType... = (§DEF t € §TYPE) <=> (§DEF aValue € t) => t
						§DEF tPackage = [§SIG_WITH t € §TYPE IN t]
						§DEF Package = §SIG tPackage WITH §INT IN 7
						§EXPORT §IF Package MATCH {
							§SIG tPackage WITH §DEF tHead IN §DEF Value: {
								§DEF tValue = .GetType Value
								§DEF Boxed € §ANY = Value
								§RETURN {
									Same: §IF Boxed MATCH {
										§DEF Candidate € tValue: §TRUE
										_: §FALSE
									}
									Private: §IF Boxed MATCH {
										§DEF Candidate € §INT: §TRUE
										_: §FALSE
									}
								}
							}
						}
						""",
						"Opaque generic TYPE value",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					mAssert.IsTrue(Export.Data.GetField("_Same".PrefixHash()).IsBool(out var Same) && Same);
					mAssert.IsTrue(Export.Data.GetField("_Private".PrefixHash()).IsBool(out var Private) && !Private);
				}
			),
			mTest.Test(
				"Conversion memoization does not retain native payloads between VM calls",
				aDebug => {
					var Export = mSPO_Interpreter.Run(
						"""
						§DEF tRecord = [<
							Value: §INT
						>]
						§EXPORT (§DEF aValue € tRecord) => {
							§DEF View € [tRecord | []] = aValue
							§RETURN §IF View MATCH {
								{Value: §DEF Number}: Number
								_: 0
							}
						}
						""",
						"Native conversion scope",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					var Type = mVM_Type.Record([("_Value", mVM_Type.Int())]);
					var Data = mVM_Data.Record([("_Value", mVM_Data.Int(7))]);
					
					foreach (var Expected in new[] {7, 9}) {
						Data._Fields = Data._Fields.Set("_Value".PrefixHash(), mVM_Data.Int(Expected));
						var Result = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (Data, Type), _ => "", mVM.cNoTrace);
						mAssert.IsTrue(Result.Data.IsInt(out var Value) && Value == Expected);
					}
				}
			),
			mTest.Test(
				"Specialized generic parameters can be returned as explicit TYPE values",
				aDebug => {
					var Export = mSPO_Interpreter.Run(
						"""
						§DEF GetType... = (§DEF t € §TYPE) <=> (§DEF aValue € t) => t
						§EXPORT {Integer: .GetType 7, Flag: .GetType §TRUE}
						""",
						"Specialized TYPE values",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					mAssert.IsTrue(Export.Data.GetField("_Integer".PrefixHash()).TypeExpressionValue().SameType(mVM_Type.Int()));
					mAssert.IsTrue(Export.Data.GetField("_Flag".PrefixHash()).TypeExpressionValue().SameType(mVM_Type.True()));
					
					mAssert.ThrowsError(
						() => mSPO_Interpreter.Run(
							"§DEF GetType... = (§DEF t € §TYPE) <=> (§DEF aValue € §INT) => t\n§EXPORT .GetType 7",
							"Undetermined TYPE value",
							(mVM_Data.Empty(), mVM_Type.Empty()),
							_ => {}
						).ElseThrow()
					);
				}
			),
			mTest.Test(
				"A structural ANY match accepts new native visible record shapes",
				aDebug => {
					var Export = mSPO_Interpreter.Run(
						"""
						§EXPORT (§DEF aValue € §ANY) => §IF aValue MATCH {
							{Visible: §DEF Number € §INT}: Number
							_: 0
						}
						""",
						"New native ANY shape",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					var Shape = mVM_Type.Record([("_Visible", mVM_Type.Int()), ("_Extra", mVM_Type.Type())]);
					mAssert.IsTrue(Export.Data.IsProc<tSpan>(out var Definition, out _));
					var Compiler = (mIL_Specialize.tContext<tSpan>)Definition.Compilation!;
					var Bodies = Compiler.Bodies.Size;
					var Views = Compiler.Views.Size;
					var Data = mVM_Data.Dynamic(mVM_Data.Record([("_Visible", mVM_Data.Int(7)), ("_Extra", mVM_Data.TypeInt()), ("_Hidden", mVM_Data.Int(9))]), Shape);
					var Result = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (Data, mVM_Type.Any()), _ => "", mVM.cNoTrace);
					mAssert.IsTrue(Result.Data.IsInt(out var Value) && Value == 7);
					mAssert.AreEquals(Compiler.Bodies.Size, Bodies);
					mAssert.AreEquals(Compiler.Views.Size, Views);
					
					var ThroughUnion = mSPO_Interpreter.Run(
						"""
						§DEF tVisible = [<
							Visible: §ANY
						>]
						§EXPORT (§DEF aValue € §ANY) => {
							§RETURN §IF aValue MATCH {
								§DEF View € [tVisible | []]: §IF View MATCH {
									{Visible: §DEF Number € §INT}: Number
									_: 0
								}
								_: 0
							}
						}
						""",
						"New native shape through union",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					var UnionResult = ThroughUnion.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (Data, mVM_Type.Any()), _ => "", mVM.cNoTrace);
					mAssert.IsTrue(UnionResult.Data.IsInt(out var Number) && Number == 7);
				}
			),
			mTest.Test(
				"Import views follow code origins produced by dependency initializers",
				aDebug => {
					static mModule.tModuleSetup Setup(tText aName, tText aSource, mStream.tStream<(tText Key, mModule.tModuleSetup Module)> aDependencies = default) {
						var Module = mModule.ModuleSetup(new mFS.tPath(aName), aDependencies);
						Module.IL = mSPO_Parser.Module.TryParseText(aSource, aName, _ => {}).ElseThrow().Check().ElseThrow();
						Module.Checked = mResult.OK(mStd.cEmpty).WithErrorType<tText>();
						return Module;
					}
					
					var Producer = Setup("Import producer", "§EXPORT {Identity...: (§DEF t € §TYPE) <=> (§DEF aValue € t) => aValue}");
					
					var Consumer = Setup(
						"Import consumer",
						"""
						§IMPORT {Producer: {
							Identity...: §DEF Forward... € [§GENERIC t [t => §ANY]]
						}}
						§DEF Value = .Forward 7
						§EXPORT §IF Value MATCH {
							§DEF Number € §INT: Number
							_: 0
						}
						""",
						mStream.Stream(("Producer", Producer))
					);
					
					var Result = Consumer.Init(_ => {}).ElseThrow();
					mAssert.IsTrue(Result.Data.IsInt(out var Value) && Value == 7);
				}
			),
			mTest.Test(
				"Shared function views keep every code origin during record conversion",
				aDebug => {
					var Ignore = mSPO_Interpreter.Run("§EXPORT () => ()", "Shared producer", (mVM_Data.Empty(), mVM_Type.Empty()), _ => {}).ElseThrow();
					var Import = (mVM_Data.Record([("_Number", mVM_Data.Int(7)), ("_First", Ignore.Data), ("_Second", Ignore.Data)]), mVM_Type.Record([("_Number", mVM_Type.Int()), ("_First", Ignore.Type), ("_Second", Ignore.Type)]));
					
					var Result = mSPO_Interpreter.Run(
						"""
						§IMPORT {
							Number: §DEF Number € §ANY
							First: §DEF First € [[] => []]
							Second: §DEF Second € [[] => []]
						}
						§DEF Result = .Second
						§EXPORT 9
						""",
						"Shared record functions",
						Import,
						_ => {}
					).ElseThrow();
					
					mAssert.IsTrue(Result.Data.IsInt(out var Value) && Value == 9);
				}
			),
			mTest.Test(
				"Native result code origins link before execution and preserve the public signature",
				aDebug => {
					var Empty = mVM_Type.Empty();
					var First = mSPO_Interpreter.Run("§EXPORT (§DEF aValue € §INT) => aValue", "Native result first", (mVM_Data.Empty(), Empty), _ => {}).ElseThrow();
					var Second = mSPO_Interpreter.Run("§EXPORT (§DEF aValue € §INT) => 9", "Native result second", (mVM_Data.Empty(), Empty), _ => {}).ElseThrow();
					var Origins = new[] {First.Data, Second.Data};
					var Calls = 0;
					var Public = mVM_Type.Proc(Empty, mVM_Type.Int(), mVM_Type.Any());
					var Native = mVM_Data.ExternProc(mVM_Type.Proc(Empty, Empty, Public), (_, _, _, _) => Origins[Calls++ % 2], mVM_Data.Empty(), aResultOrigins: Origins);
					var Import = (mVM_Data.Record([("_Get", Native)]), mVM_Type.Record([("_Get", mVM_Type.Proc(Empty, Empty, Public))]));
					
					var Export = mSPO_Interpreter.Run(
						"""
						§IMPORT {Get: §DEF Get € [[] => [§INT => §ANY]]}
						§EXPORT () => {
							§DEF First... = .Get
							§DEF Second... = .Get
							§DEF Boxed € §ANY = First...
							§RETURN {
								First: .First 7
								Second: .Second 7
								Private: §IF Boxed MATCH {
									§DEF Candidate € [§INT => §INT]: §TRUE
									_: §FALSE
								}
							}
						}
						""",
						"Native result consumer",
						Import,
						_ => {}
					).ElseThrow();
					
					mAssert.AreEquals(Calls, 0);
					mAssert.IsTrue(Export.Data.IsProc<tSpan>(out var Definition, out _));
					var Compiler = (mIL_Specialize.tContext<tSpan>)Definition.Compilation!;
					var Bodies = Compiler.Bodies.Size;
					var Views = Compiler.Views.Size;
					var Result = Export.Call<tSpan>((mVM_Data.Empty(), Empty), (mVM_Data.Empty(), Empty), _ => "", mVM.cNoTrace);
					var Expected = mVM_Data.Record([("_First", mVM_Data.Dynamic(mVM_Data.Int(7), mVM_Type.Int())), ("_Second", mVM_Data.Dynamic(mVM_Data.Int(9), mVM_Type.Int())), ("_Private", mVM_Data.Bool(false))]).Normalize(Result.Type);
					mAssert.IsTrue(Result.SameVisibleValue((Expected, Result.Type)));
					mAssert.AreEquals(Calls, 2);
					mAssert.AreEquals(Compiler.Bodies.Size, Bodies);
					mAssert.AreEquals(Compiler.Views.Size, Views);
				}
			),
			mTest.Test(
				"All modules link before the first initializer can produce native effects",
				aDebug => {
					static mModule.tModuleSetup Setup(tText aName, tText aSource, mStream.tStream<(tText Key, mModule.tModuleSetup Module)> aDependencies = default) {
						var Module = mModule.ModuleSetup(new mFS.tPath(aName), aDependencies);
						Module.IL = mSPO_Parser.Module.TryParseText(aSource, aName, _ => {}).ElseThrow().Check().ElseThrow();
						Module.Checked = mResult.OK(mStd.cEmpty).WithErrorType<tText>();
						return Module;
					}
					
					var Calls = 0;
					var Native = Setup("Native", "§EXPORT {Run: () => ()}");
					var Function = mVM_Data.ExternProc(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty()), (_, _, _, _) => { Calls += 1; return mVM_Data.Empty(); }, mVM_Data.Empty());
					Native.Module = mResult.OK((mVM_Data.Record([("_Run", Function)]), mVM_Type.Record([("_Run", mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty()))]))).WithErrorType<tText>();
					
					var Leaf = Setup(
						"Leaf",
						"§IMPORT {Native: {Run: §DEF Run € [[] => []]}}\n§EXPORT .Run",
						mStream.Stream(("Native", Native))
					);
					
					var Root = Setup("Root", "§IMPORT {Leaf: ()}\n§EXPORT (§DEF aValue € §INT) => aValue", mStream.Stream(("Leaf", Leaf)));
					var Missing = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Bool(), mVM_Type.Bool());
					mAssert.ThrowsError(() => Root.Init(_ => {}, mStream.Stream((Missing, mVM_Type.Empty(), mVM_Type.Bool()))));
					mAssert.AreEquals(Calls, 0);
					var Export = Root.Init(_ => {}).ElseThrow();
					mAssert.AreEquals(Calls, 1);
					var Result = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.Int(7), mVM_Type.Int()), _ => "", mVM.cNoTrace);
					mAssert.IsTrue(Result.Data.IsInt(out var Value) && Value == 7);
				}
			),
			mTest.Test(
				"TYPE parameters match new native ANY views without compiling code",
				aDebug => {
					var Export = mSPO_Interpreter.Run(
						"""
						§EXPORT (§DEF aType € §TYPE, §DEF aValue € §ANY) => §IF aValue MATCH {
							§DEF Candidate € aType: §TRUE
							_: §FALSE
						}
						""",
						"Native bound type",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {}
					).ElseThrow();
					
					mAssert.IsTrue(Export.Data.IsProc<tSpan>(out var Definition, out _));
					var Compiler = (mIL_Specialize.tContext<tSpan>)Definition.Compilation!;
					var Bodies = Compiler.Bodies.Size;
					var Views = Compiler.Views.Size;
					var Visible = mVM_Type.Record([("_Visible", mVM_Type.Int())]);
					var Hidden = mVM_Type.Record([("_Hidden", mVM_Type.Int())]);
					var Data = mVM_Data.Dynamic(mVM_Data.Record([("_Visible", mVM_Data.Int(7)), ("_Hidden", mVM_Data.Int(9))]), Visible);
					
					foreach (var (Type, Expected) in mStream.Stream((Visible, true), (Hidden, false))) {
						var Argument = mVM_Data.Tuple([mVM_Data.TypeExpression(Type), Data]);
						var Result = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (Argument, mVM_Type.Tuple([mVM_Type.Type(), mVM_Type.Any()])), _ => "", mVM.cNoTrace);
						mAssert.IsTrue(Result.Data.IsBool(out var Flag) && Flag == Expected);
					}
					
					mAssert.AreEquals(Compiler.Bodies.Size, Bodies);
					mAssert.AreEquals(Compiler.Views.Size, Views);
				}
			),
			mTest.Test(
				"Native generic calls use only specializations declared before module execution",
				aDebug => {
					var Parameter = mVM_Type.Free("Value", mVM_Type.Type());
					var Signature = mVM_Type.Generic(Parameter, mVM_Type.Proc(mVM_Type.Empty(), Parameter, Parameter));
					
					var Export = mSPO_Interpreter.Run(
						"§EXPORT (§DEF t € §TYPE) <=> (§DEF aValue € t) => aValue",
						"Prepared generic entry",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						_ => {},
						mStream.Stream((Signature, mVM_Type.Empty(), mVM_Type.Int()), (Signature, mVM_Type.Empty(), mVM_Type.Bool()))
					).ElseThrow();
					
					mAssert.IsTrue(Export.Data.IsProc<tSpan>(out var Definition, out _));
					var Compiler = (mIL_Specialize.tContext<tSpan>)Definition.Compilation!;
					var Bodies = Compiler.Bodies.Size;
					var Views = Compiler.Views.Size;
					var Integer = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.Int(7), mVM_Type.Int()), _ => "", mVM.cNoTrace);
					var Boolean = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.Bool(true), mVM_Type.Bool()), _ => "", mVM.cNoTrace);
					mAssert.IsTrue(Integer.Data.IsInt(out var Value) && Value == 7);
					mAssert.IsTrue(Boolean.Data.IsBool(out var Flag) && Flag);
					mAssert.AreEquals(Compiler.Bodies.Size, Bodies);
					mAssert.AreEquals(Compiler.Views.Size, Views);
					mAssert.ThrowsError(() => mIL_Specialize.PrepareNativeCall<tSpan>(Export, (mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.Empty(), mVM_Type.Empty())));
					mAssert.ThrowsError(() => Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.Empty(), mVM_Type.Empty()), _ => "", mVM.cNoTrace));
					mAssert.AreEquals(Compiler.Bodies.Size, Bodies);
					mAssert.AreEquals(Compiler.Views.Size, Views);
				}
			),
			mTest.Test(
				"Exported monomorphic functions are prepared before their first native call",
				aDebug => {
					var Export = mSPO_Interpreter.Run(
						"§EXPORT (§DEF aValue € §INT) => aValue",
						"Prepared native entry",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						__ => aDebug(__())
					).ElseThrow();
					
					mAssert.IsTrue(Export.Data.IsProc<tSpan>(out var Definition, out _));
					var Compiler = (mIL_Specialize.tContext<tSpan>)Definition.Compilation!;
					var Bodies = Compiler.Bodies.Size;
					var Views = Compiler.Views.Size;
					var Result = Export.Call<tSpan>((mVM_Data.Empty(), mVM_Type.Empty()), (mVM_Data.Int(7), mVM_Type.Int()), _ => "", mVM.cNoTrace);
					mAssert.IsTrue((Result.Data, Result.Type).SameVisibleValue((mVM_Data.Int(7), mVM_Type.Int())));
					mAssert.AreEquals(Compiler.Bodies.Size, Bodies);
					mAssert.AreEquals(Compiler.Views.Size, Views);
				}
			),
			mTest.Test(
				"Bound interface receivers survive opaque arguments while argument receivers stay opaque",
				aDebug => {
					const tText cSource = """
					§DEF iCell = [
						§INTERFACE t [<
							Read: [t : [] => §INT]
							Write...: [t : [< Value: §INT >] => []]
						>]
					]
					
					§DEF Read = (
						§VAR aCell € §INT
					) : (
					) => §TO_VAL aCell
					
					§DEF Write... = (
						§VAR aCell € §INT
					) : (
						§DEF aPayload € [< Value: §INT >]
					) {
						{Value: §DEF Value} = aPayload
						aCell := Value .
					}
					
					§DEF Exercise... = (
						§DEF aPayload € [< Value: §INT >]
					) => {
						§VAR Cell := 7 .
						§DEF Capability = §INTERFACE iCell WITH Cell IN {
							Read: Read
							Write...: Write...
						}
						
						Capability :
							Write aPayload
							Read => §DEF Value
						.
						
						§RETURN Value
					}
					
					§EXPORT .Exercise {Value: 19}
					""";
					
					var Import = (mVM_Data.Empty(), mVM_Type.Empty());
					
					var Result = mSPO_Interpreter.Run(
						cSource,
						"Bound receiver",
						Import,
						__ => aDebug(__())
					);
					
					mAssert.IsTrue((Result.ElseThrow().Data, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(19), mVM_Type.Int())));
					
					var Opaque = cSource.Replace(
						"§EXPORT .Exercise {Value: 19}",
						"""
						§DEF ReadArgument... = (
							§DEF aCapability € iCell
						) => {
							aCapability : Read => §DEF Value .
							§RETURN Value
						}
						
						§EXPORT ()
						"""
					);
					
					var Denied = mSPO_Interpreter.Run(
						Opaque,
						"Opaque receiver",
						Import,
						__ => aDebug(__())
					);
					
					mAssert.IsFalse(Denied.Match(out _, out var Error));
					mAssert.IsTrue(Error.Contains("opaque mutable access"), Error);
				}
			),
			mTest.Test(
				"Host unpacks SPO tuples in source order",
				aDebug => {
					var Value = mSPO_Interpreter.Run(
						"""
						§EXPORT (
							(11, 22)
							(33, 44, 55)
							(66, 77, 88, 99)
						)
						""",
						"Host tuples",
						(mVM_Data.Empty(), mVM_Type.Empty()),
						aTrace => aDebug(aTrace())
					).ElseThrow(
					).Data;
					
					mAssert.IsTrue(Value.IsTuple(out var Pair, out var Triple, out var Quad));
					mAssert.IsTrue(Pair.IsTuple(out var A, out var B));
					mAssert.IsTrue((A, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(11), mVM_Type.Int())));
					mAssert.IsTrue((B, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(22), mVM_Type.Int())));
					mAssert.IsTrue(Triple.IsTuple(out A, out B, out var C));
					mAssert.IsTrue((A, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(33), mVM_Type.Int())));
					mAssert.IsTrue((B, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(44), mVM_Type.Int())));
					mAssert.IsTrue((C, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(55), mVM_Type.Int())));
					mAssert.IsTrue(Quad.IsTuple(out A, out B, out C, out var D));
					mAssert.IsTrue((A, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(66), mVM_Type.Int())));
					mAssert.IsTrue((B, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(77), mVM_Type.Int())));
					mAssert.IsTrue((C, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(88), mVM_Type.Int())));
					mAssert.IsTrue((D, mVM_Type.Int()).SameVisibleValue((mVM_Data.Int(99), mVM_Type.Int())));
					mAssert.IsFalse(Pair.IsTuple(out _, out _, out _));
					mAssert.IsFalse(Triple.IsTuple(out _, out _));
				}
			),
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
							Source,
							"",
							__ => aDebug(__())
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
							Source,
							"",
							__ => aDebug(__())
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
								mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Int()),
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
							mAssert.IsTrue(Result.SameVisibleValue((aCase.Expected.Normalize(Result.Type), Result.Type)));
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
					
					var Record = mVM_Data.Record(
						[
							("_DefaultStyle", mVM_Data.Int(1)),
							("_OldStyle", mVM_Data.Int(2))
						]
					);
					
					mAssert.IsTrue(Record.IsRecord(out var Fields));
					mAssert.IsTrue(Fields.TryGet("_DefaultStyle".PrefixHash()).AssertNotEmpty().IsInt(out var Default) && Default == 1);
					mAssert.IsTrue(Fields.TryGet("_OldStyle".PrefixHash()).AssertNotEmpty().IsInt(out var Old) && Old == 2);
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
								"Box",
								mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type())
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
					
					var Type = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int());
					var Import = mVM_Data.ExternProc(Type, (aEnv, aObj, aArg, aTrace) => aArg, mVM_Data.Empty());
					var Result = mSPO_Interpreter.Run(cSource, "", (Import, Type), __ => aDebug(__())).ElseThrow();
					mAssert.IsTrue(Result.SameVisibleValue((mVM_Data.Int(7), mVM_Type.Int())));
				}
			)
		]
	);
}