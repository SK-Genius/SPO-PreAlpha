public static class
mVM_Tests {
	
	private static
	mVM_Data.tData
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
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM),
		mTest.Test("ExternDef",
			aDebugStream => {
				#if MY_TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc1 = new mVM_Data.tProcDef<mStd.tEmpty>(
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Int(),
							mVM_Type.Int()
						)
					)
				);
				var Reg1 = Proc1.Pair(mStd.cEmpty, mVM_Data.cOneReg, mVM_Data.cOneReg);
				var Reg2 = Proc1.Call(mStd.cEmpty, mVM_Data.cEnvReg, mVM_Data.cEmptyReg);
				var Reg3 = Proc1.Call(mStd.cEmpty, Reg2, Reg1);
				Proc1.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, Reg3);
				
				var Res = mVM_Data.Empty();
				mVM.Run<mStd.tEmpty>(
					mVM_Data.Proc(Proc1, Env),
					mVM_Data.Empty(),
					mVM_Data.Empty(),
					Res,
					_ => "" + _,
					TraceOut
				);
				mAssert.AreEquals(Res, mVM_Data.Int(2));
			}
		),
		mTest.Test("InternDef",
			aDebugStream => {
				#if MY_TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc2 = new mVM_Data.tProcDef<mStd.tEmpty>(
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Tuple(mVM_Type.Int(), mVM_Type.Int()),
							mVM_Type.Int()
						),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Int(),
							mVM_Type.Int()
						)
					)
				);
				Proc2.ReturnIf(
					mStd.cEmpty,
					mVM_Data.cTrueReg,
					Proc2.Call(
						mStd.cEmpty,
						Proc2.Call(
							mStd.cEmpty,
							mVM_Data.cEnvReg,
							mVM_Data.cEmptyReg
						),
						Proc2.Pair(
							mStd.cEmpty,
							mVM_Data.cOneReg,
							mVM_Data.cOneReg
						)
					)
				);
				
				var Res = mVM_Data.Empty();
				mVM.Run<mStd.tEmpty>(mVM_Data.Proc(Proc2, Env), mVM_Data.Empty(), mVM_Data.Empty(), Res, a=>""+a, TraceOut);
				mAssert.AreEquals(Res, mVM_Data.Int(2));
			}
		)
	);
}
