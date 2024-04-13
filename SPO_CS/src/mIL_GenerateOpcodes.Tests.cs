using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mIL_GenerateOpcodes_Tests {
	
	private static tText
	SpanToText(
		tSpan a
	) => $"{a.Start.Id}({a.Start.Row}:{a.Start.Col} .. {a.End.Row}:{a.End.Col})";
	
	public static (mStream.tStream<mVM_Data.tProcDef<tSpan>>? Defs, mTreeMap.tTree<tText, tNat32> DefLookup)
	CompileModule(
		tText aSourceCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => mIL_GenerateOpcodes.GenerateOpcodes(
		mIL_Parser.Module.ParseText(aSourceCode, aId, aTrace),
		aTrace
	);
	
	private static mVM_Data.tData
	Add (
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
	Sub (
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
	Mul (
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
					a => aDebugStream(a())
				);
				
				#if MY_TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
					);
				#else
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> _) => {}
					);
				#endif
				
				var Proc = DefLookup.TryGet("...++").ThenTry(a => Defs.TryGet(a)).ElseThrow();
				var Res = mVM_Data.Empty();
				mVM.Run<tSpan>(
					mVM_Data.Proc(Proc, mVM_Data.Empty()),
					mVM_Data.Empty(),
					mVM_Data.Int(5),
					Res,
					SpanToText,
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
						->pre->pre := [EMPTY => pre->pre]
					§DEF ...++ € ->pre->pre
						_1 := 1
						arg := -#PRE ARG
						inc := §INT arg + _1
						res := +#PRE inc
						§RETURN res IF TRUE
					
					""",
					"",
					a => aDebugStream(a())
				);
				
				#if MY_TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Proc = DefLookup.TryGet("...++").ThenTry(Defs.TryGet).ElseThrow();
				var Env = mVM_Data.ExternDef(Add);
				var Res = mVM_Data.Empty();
				mVM.Run<tSpan>(
					mVM_Data.Proc(Proc, Env),
					mVM_Data.Empty(),
					mVM_Data.Prefix("PRE", mVM_Data.Int(12)),
					Res,
					SpanToText,
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
						IntInt := [INT, INT]
						IntInt->Bool := [IntInt => BOOL]
						Env := [EMPTY => IntInt->Bool]
						Int->Bool := [INT => BOOL]
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
					a => aDebugStream(a())
				);
				
				var Proc = DefLookup.TryGet("...=1").ThenTry(Defs.TryGet).ElseThrow();
				var Env = mVM_Data.ExternDef(Eq);
				var Res = mVM_Data.Empty();
				
				#if MY_TRACE
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
					CallStack = CallStack_.Step(a => ""+a);
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
							SpanToText,
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
						IntInt := [INT, INT]
						IntInt->Int := [IntInt => INT]
						_IntInt->Int := [EMPTY => IntInt->Int]
						IntInt->Bool := [IntInt => BOOL]
						_IntInt->Bool := [EMPTY => IntInt->Bool]
						Env1 := [EMPTY, _IntInt->Int]
						Env2 := [Env1, _IntInt->Int]
						Env3 := [Env2, _IntInt->Int]
						Env4 := [Env3, _IntInt->Bool]
						->Int := [EMPTY => INT]
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
					a => aDebugStream(a())
				);
				
				var Proc1 = DefLookup.TryGet("bla").ThenTry(Defs.TryGet).ElseThrow();
				var Proc2 = DefLookup.TryGet("bla2").ThenTry(Defs.TryGet).ElseThrow();
				
				var Env = mVM_Data.Tuple(
					mVM_Data.ExternDef(Add),
					mVM_Data.ExternDef(Sub),
					mVM_Data.ExternDef(Mul),
					mVM_Data.ExternDef(Eq)
				);
				#if MY_TRACE
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
						SpanToText,
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
						SpanToText,
						TraceOut
					);
					mAssert.AreEquals(Res, mVM_Data.Int(12));
				}
			}
		)
	);
}
