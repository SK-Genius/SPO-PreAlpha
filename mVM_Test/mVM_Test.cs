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

using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
public static class mVM_Test {
	
	//================================================================================
	private static mVM_Data.tData
	Add (
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 + IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM),
		mTest.Test(
			"ExternDef",
			aDebugStream => {
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc1 = new mVM_Data.tProcDef();
				var r1 = Proc1.Pair(mVM_Data.tProcDef.cOneReg, mVM_Data.tProcDef.cOneReg);
				var r2 = Proc1.Call(mVM_Data.tProcDef.cEnvReg, mVM_Data.tProcDef.cEmptyReg);
				var r3 = Proc1.Call(r2, r1);
				Proc1.ReturnIf(mVM_Data.tProcDef.cTrueReg, r3);
				
				var Res = mVM_Data.Empty();
				mVM.Run(mVM_Data.Proc(Proc1, Env), mVM_Data.Empty(), mVM_Data.Empty(), Res, TraceOut);
				mStd.AssertEq(Res, mVM_Data.Int(2));
			}
		),
		mTest.Test(
			"InternDef",
			aDebugStream => {
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc2 = new mVM_Data.tProcDef();
				Proc2.ReturnIf(
					mVM_Data.tProcDef.cTrueReg,
					Proc2.Call(
						Proc2.Call(
							mVM_Data.tProcDef.cEnvReg,
							mVM_Data.tProcDef.cEmptyReg
						),
						Proc2.Pair(
							mVM_Data.tProcDef.cOneReg,
							mVM_Data.tProcDef.cOneReg
						)
					)
				);
				
				var Res = mVM_Data.Empty();
				mVM.Run(mVM_Data.Proc(Proc2, Env), mVM_Data.Empty(), mVM_Data.Empty(), Res, TraceOut);
				mStd.AssertEq(Res, mVM_Data.Int(2));
			}
		)
	);
	
	[xTestCase("ExternDef")]
	[xTestCase("InternDef")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
