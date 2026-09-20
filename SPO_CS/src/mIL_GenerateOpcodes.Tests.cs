#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mSpan.cs
#:ref Common/mMaybe.cs
#:ref Common/mStream.cs
#:ref Common/mTreeMap.cs
#:ref Common/mAssert.cs
#:ref Common/mTest.cs
#:ref Common/mTextStream.cs
#:ref Common/mParserGen.cs
#:ref mTokenizer.cs
#:ref mIL_GenerateOpcodes.cs
#:ref mIL_AST.cs
#:ref mIL_Parser.cs
#:ref mVM_Data.cs
#:ref mVM.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mIL_GenerateOpcodes_Tests {
	public static (mStream.tStream<mVM_Data.tProcDef<tSpan>> Defs, mTreeMap.tTree<tText, tNat32> DefLookup)
	CompileModule(
		tText aSourceCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => mIL_GenerateOpcodes.GenerateOpcodes(
		mIL_Parser.Module.ParseText(aSourceCode, aId, aTrace),
		aTrace
	);
	
	private static mVM_Data.tData
	Add(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aArg.IsPair(out var Arg1, out var Arg2));
		mAssert.IsTrue(Arg1.IsInt(out var IntArg1));
		mAssert.IsTrue(Arg2.IsInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 + IntArg2);
	}
	
	private static mVM_Data.tData
	Sub(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aArg.IsPair(out var Arg1, out var Arg2));
		mAssert.IsTrue(Arg1.IsInt(out var IntArg1));
		mAssert.IsTrue(Arg2.IsInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 - IntArg2);
	}
	
	private static mVM_Data.tData
	Mul(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aArg.IsPair(out var Arg1, out var Arg2));
		mAssert.IsTrue(Arg1.IsInt(out var IntArg1));
		mAssert.IsTrue(Arg2.IsInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 * IntArg2);
	}
	
	private static mVM_Data.tData
	Eq(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aArg.IsPair(out var Arg1, out var Arg2));
		mAssert.IsTrue(Arg1.IsInt(out var IntArg1));
		mAssert.IsTrue(Arg2.IsInt(out var IntArg2));
		return mVM_Data.Bool(IntArg1.Equals(IntArg2));
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mIL_GenerateOpcodes),
		[
			mTest.Test("ALL is callable and describes generic function signatures",
				aDebug => {
					const tText Source = """
					§TYPES
						t := [§FREE]
						Body := [t => t]
						Identity := [§ALL t => Body]
						IdDef := [EMPTY_TYPE => Identity]
						Func := [EMPTY_TYPE => TYPE]
						Def := [IdDef => Func]
					§DEF identity € IdDef
						§RETURN ARG IF TRUE
					§DEF main € Def
						id := .ENV EMPTY
						seven := 7
						value := .id seven
						flag := .id TRUE
						t := [§FREE]
						Body := [t => t]
						T := [§ALL t => Body]
						result := .T INT_TYPE
						§RETURN result IF TRUE
					""";
					var Module = CompileModule(Source + "\n", "", __ => aDebug(__()));
					var IdDef = Module.Defs.TryFirst().AssertNotEmpty();
					var Def = Module.Defs.TryGet(Module.DefLookup.TryGet("main").AssertNotEmpty()).AssertNotEmpty();
					var Kind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					mAssert.IsTrue(IdDef.DefType.Refs[2].KindType().SameType(Kind));
					mAssert.IsTrue(Def.Types.Get(Def._LastReg - 1).SameType(Kind));
					mAssert.IsTrue(Def.Types.Get(Def._LastReg).IsType());
					var Result = mVM_Data.Empty();
					mVM.Run<tSpan>(
						mVM_Data.Proc(Def, mVM_Data.Def(IdDef)),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						Result,
						mTextParser.ToText,
						__ => aDebug(__())
					);
					mAssert.IsTrue(Result.TypeValue().SameType(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int())));
					mAssert.ThrowsError(() => {
						CompileModule(Source.Replace("[§ALL t => Body]", "[§GENERIC t => Body]") + "\n", "", _ => { });
					});
					mAssert.ThrowsError(() => {
						CompileModule(Source.Replace("[§ALL t => Body]", "[§ALL INT => Body]") + "\n", "", _ => { });
					});
				}
			),
			mTest.Test("SIG type constructors are callable functions",
				aDebug => {
					const tText Source = """
					§TYPES
						Func := [EMPTY_TYPE => TYPE]
						Def := [EMPTY_TYPE => Func]
					§DEF main € Def
						t := [§FREE]
						F := [§ALL t => t]
						result := .F INT_TYPE
						§RETURN result IF TRUE
					""";
					var Def = CompileModule(Source + "\n", "", __ => aDebug(__())).Defs.TryFirst().AssertNotEmpty();
					var Result = mVM_Data.Empty();
					mVM.Run<tSpan>(
						mVM_Data.Proc(Def, mVM_Data.Empty()),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						Result,
						mTextParser.ToText,
						__ => aDebug(__())
					);
					mAssert.IsTrue(Result.TypeValue().IsInt());
					mAssert.ThrowsError(() => {
						CompileModule(Source.Replace("§RETURN result", "§RETURN F") + "\n", "", _ => { });
					});
					mAssert.ThrowsError(() => {
						CompileModule(Source.Replace("[§FREE]", "[§FREE € Type_TYPE]") + "\n", "", _ => { });
					});
				}
			),
			mTest.Test("SIG parameter declarations cannot escape as values",
				aDebug => {
					const tText Source = """
					§TYPES
						Func := [EMPTY_TYPE => TYPE]
						Def := [EMPTY_TYPE => Func]
					§DEF main € Def
						F := [§SIG_HEAD Type_TYPE]
						Contract := [§SIG_WITH F IN F]
						§RETURN Contract IF TRUE
					
					""";
					CompileModule(Source, "", __ => aDebug(__()));
					mAssert.ThrowsError(() => {
						CompileModule(Source.Replace("§RETURN Contract", "§RETURN F"), "", _ => { });
					});
				}
			),
			mTest.Test("SIG uses type values while head registers have type TYPE",
				aDebug => {
					const tText Source = """
					§TYPES
						Func := [EMPTY_TYPE => TYPE]
						Def := [EMPTY_TYPE => Func]
					§DEF main € Def
						t := [§SIG_HEAD Type_TYPE]
						contract := [§SIG_WITH t IN t]
						body := 7
						payload := INT_TYPE, body
						package := §SIG contract WITH payload
						head := §SIG_HEAD package
						§RETURN head IF TRUE
					""";
					var Def = CompileModule(Source + "\n", "", __ => aDebug(__())).Defs.TryFirst().AssertNotEmpty();
					mAssert.IsTrue(Def.Types.Get(mVM_Data.cIntTypeReg).IsType());
					mAssert.IsTrue(Def.Types.Get(Def._LastReg).IsType());
					var Result = mVM_Data.Empty();
					mVM.Run<tSpan>(
						mVM_Data.Proc(Def, mVM_Data.Empty()),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						Result,
						mTextParser.ToText,
						__ => aDebug(__())
					);
					mAssert.IsTrue(Result.TypeValue().IsInt());
					mAssert.ThrowsError(
						() => { CompileModule(Source.Replace("body := 7", "body := TRUE") + "\n", "", _ => { }); }
					);
				}
			),
			mTest.Test("Call",
				aDebugStream => {
					var (Defs, DefLookup) = CompileModule(
						"""
						§TYPES
							Int->Int := [INT => INT]
							Env->Int->Int := [Int->Int => Int->Int]
						§DEF ...++ € Env->Int->Int
							_1 := 1
							res := §INT ARG + _1
							§RETURN res IF TRUE
						
						""",
						"",
						__ => aDebugStream(__())
					);
					
					#if MY_TRACE_IL
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
						);
					#else
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> _) => {}
						);
					#endif
					
					var Proc = DefLookup.TryGet("...++").ThenTry(__ => Defs.TryGet(__)).AssertNotEmpty();
					var Res = mVM_Data.Empty();
					mVM.Run<tSpan>(
						mVM_Data.Proc(Proc, mVM_Data.Empty()),
						mVM_Data.Empty(),
						mVM_Data.Int(5),
						Res,
						mTextParser.ToText,
						TraceOut
					);
					mAssert.AreEquals(Res, mVM_Data.Int(6));
				}
			),
			mTest.Test("Prefix",
				aDebugStream => {
					var (Defs, DefLookup) = CompileModule(
						"""
						§TYPES
							pre := [#PRE INT]
							pre->pre := [pre => pre]
							->pre->pre := [EMPTY_TYPE => pre->pre]
						§DEF ...++ € ->pre->pre
							_1 := 1
							arg := -#PRE ARG
							inc := §INT arg + _1
							res := +#PRE inc
							§RETURN res IF TRUE
						
						""",
						"",
						__ => aDebugStream(__())
					);
					
					#if MY_TRACE_IL
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
						);
					#else
						var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
					#endif
					
					var Proc = DefLookup.TryGet("...++").ThenTry(__ => Defs.TryGet(__)).AssertNotEmpty();
					var Env = mVM_Data.ExternDef(Add);
					var Res = mVM_Data.Empty();
					mVM.Run<tSpan>(
						mVM_Data.Proc(Proc, Env),
						mVM_Data.Empty(),
						mVM_Data.Prefix("PRE", mVM_Data.Int(12)),
						Res,
						mTextParser.ToText,
						TraceOut
					);
					mAssert.AreEquals(Res, mVM_Data.Prefix("PRE", mVM_Data.Int(13)));
				}
			),
			mTest.Test("Assert",
				aDebugStream => {
					var (Defs, DefLookup) = CompileModule(
						"""
						§TYPES
							Bool := [TRUE | FALSE]
							IntInt := [INT, INT]
							IntInt->Bool := [IntInt => Bool]
							Env := [EMPTY_TYPE => IntInt->Bool]
							Int->Bool := [INT => Bool]
							Env-->Int->Bool := [Env => Int->Bool]
						§DEF ...=1 € Env-->Int->Bool
							...=...? := . ENV EMPTY
							_1 := 1
							args := ARG, _1
							arg_eq_1? := . ...=...? args
							§ASSERT TRUE => arg_eq_1?
							§RETURN arg_eq_1? IF TRUE
						
						""",
						"",
						__ => aDebugStream(__())
					);
					
					var Proc = DefLookup.TryGet("...=1").ThenTry(__ => Defs.TryGet(__)).AssertNotEmpty();
					var Env = mVM_Data.ExternDef(Eq);
					var Res = mVM_Data.Empty();
					
					#if MY_TRACE_IL
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
						);
					#else
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> _) => {}
						);
					#endif
					
					var CallStack = mVM.NewCallStack(
						mStd.cEmpty,
						Proc,
						Env,
						mVM_Data.Empty(),
						mVM_Data.Int(1),
						Res,
						TraceOut
					);
					while (CallStack.IsSome(out var CallStack_)) {
						CallStack = CallStack_.Step(__ => "" + __);
					}
					mAssert.AreEquals(Res, mVM_Data.Bool(true));
					mAssert.ThrowsError(
						() => {
							Res = mVM_Data.Empty();
							mVM.Run<tSpan>(
								mVM_Data.Proc(Proc, Env),
								mVM_Data.Empty(),
								mVM_Data.Int(2),
								Res,
								mTextParser.ToText,
								TraceOut
							);
						}
					);
				}
			),
			mTest.Test("ParseModule",
				aDebugStream => { // 
					var (Defs, DefLookup) = CompileModule(
						"""
						§TYPES
							Bool := [TRUE | FALSE]
							IntInt := [INT, INT]
							IntInt->Int := [IntInt => INT]
							_IntInt->Int := [EMPTY_TYPE => IntInt->Int]
							IntInt->Bool := [IntInt => Bool]
							_IntInt->Bool := [EMPTY_TYPE => IntInt->Bool]
							Env1 := [EMPTY_TYPE, _IntInt->Int]
							Env2 := [Env1, _IntInt->Int]
							Env3 := [Env2, _IntInt->Int]
							Env4 := [Env3, _IntInt->Bool]
							->Int := [EMPTY_TYPE => INT]
							tBla := [Env4 => ->Int]
						§DEF bla € tBla
							_1 := 1
							env4 := §1ST ENV
							env3 := §1ST env4
							env2 := §1ST env3
							add_ := §2ND env2
							add := .add_ EMPTY
							p := _1, _1
							r := .add p
							§RETURN r IF TRUE
						§DEF bla2 € tBla
							_1 := 1
							eq_    := §2ND ENV
							env4   := §1ST ENV
							mul_   := §2ND env4
							env3   := §1ST env4
							sub_   := §2ND env3
							env2   := §1ST env3
							add_   := §2ND env2
							env1   := §1ST env2
							add := .add_ EMPTY
							sub := .sub_ EMPTY
							mul := .mul_ EMPTY
							_1_1 := _1, _1
							_2   := .add _1_1
							_2_1 := _2, _1
							_3   := .add _2_1
							_2_2 := _2, _2
							_4   := .add _2_2
							_3_4 := _3, _4
							_12  := .mul _3_4
							§RETURN _12 IF TRUE
							
						""",
						"",
						__ => aDebugStream(__())
					);
					
					var Proc1 = DefLookup.TryGet("bla").ThenTry(__ => Defs.TryGet(__)).AssertNotEmpty();
					var Proc2 = DefLookup.TryGet("bla2").ThenTry(__ => Defs.TryGet(__)).AssertNotEmpty();
					
					var Env = mVM_Data.Tuple(
						[
							mVM_Data.ExternDef(Add),
							mVM_Data.ExternDef(Sub),
							mVM_Data.ExternDef(Mul),
							mVM_Data.ExternDef(Eq)
						]
					);
					#if MY_TRACE_IL
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
						);
					#else
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> _) => {}
						);
					#endif
					{
						var Res = mVM_Data.Empty();
						mVM.Run<tSpan>(
							mVM_Data.Proc(Proc1, Env),
							mVM_Data.Empty(),
							mVM_Data.Empty(),
							Res,
							mTextParser.ToText,
							TraceOut
						);
						mAssert.AreEquals(Res, mVM_Data.Int(2));
					}
					{
						var Res = mVM_Data.Empty();
						mVM.Run<tSpan>(
							mVM_Data.Proc(Proc2, Env),
							mVM_Data.Empty(),
							mVM_Data.Empty(),
							Res,
							mTextParser.ToText,
							TraceOut
						);
						mAssert.AreEquals(Res, mVM_Data.Int(12));
					}
				}
			)
		]
	);
}
