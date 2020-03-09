//IMPORT mTest.cs
//IMPORT mVM.cs

#nullable enable

using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

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
		mAssert.IsTrue(aArg.MatchPair(out var Arg1, out var Arg2));
		mAssert.IsTrue(Arg1.MatchInt(out var IntArg1));
		mAssert.IsTrue(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 + IntArg2);
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM),
		mTest.Test(
			"ExternDef",
			aDebugStream => {
				#if MY_TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc1 = new mVM_Data.tProcDef<mStd.tEmpty>();
				var Reg1 = Proc1.Pair(mStd.cEmpty, mVM_Data.tProcDef<mStd.tEmpty>.cOneReg, mVM_Data.tProcDef<mStd.tEmpty>.cOneReg);
				var Reg2 = Proc1.Call(mStd.cEmpty, mVM_Data.tProcDef<mStd.tEmpty>.cEnvReg, mVM_Data.tProcDef<mStd.tEmpty>.cEmptyReg);
				var Reg3 = Proc1.Call(mStd.cEmpty, Reg2, Reg1);
				Proc1.ReturnIf(mStd.cEmpty, mVM_Data.tProcDef<mStd.tEmpty>.cTrueReg, Reg3);
				
				var Res = mVM_Data.Empty();
				mVM.Run<mStd.tEmpty>(mVM_Data.Proc(Proc1, Env), mVM_Data.Empty(), mVM_Data.Empty(), Res, a=>""+a, TraceOut);
				mAssert.AreEquals(Res, mVM_Data.Int(2));
			}
		),
		mTest.Test(
			"InternDef",
			aDebugStream => {
				#if MY_TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc2 = new mVM_Data.tProcDef<mStd.tEmpty>();
				Proc2.ReturnIf(
					mStd.cEmpty,
					mVM_Data.tProcDef<mStd.tEmpty>.cTrueReg,
					Proc2.Call(
						mStd.cEmpty,
						Proc2.Call(
							mStd.cEmpty,
							mVM_Data.tProcDef<mStd.tEmpty>.cEnvReg,
							mVM_Data.tProcDef<mStd.tEmpty>.cEmptyReg
						),
						Proc2.Pair(
							mStd.cEmpty,
							mVM_Data.tProcDef<mStd.tEmpty>.cOneReg,
							mVM_Data.tProcDef<mStd.tEmpty>.cOneReg
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
