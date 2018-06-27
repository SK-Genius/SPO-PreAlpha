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
public static class mSPO_Interpreter_Test {
	
	//================================================================================
	private static mVM_Data.tData
	Mul(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchPair(out var Arg2_, out var _));
		mStd.Assert(Arg2_.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 * IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Interpreter),
		mTest.Test(
			"Run1",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§DEF x... = (a => (k .* a))
							§DEF y = (.x 5)
							
							§EXPORT y
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run2",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§DEF y = (.(a => (k .* a)) 5)
							
							§EXPORT y
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run3",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§EXPORT .(a => (k .* a)) 5
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run4",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
							)
							
							§EXPORT .((a, _, _) => (2 .* a)) (3, 5, 7)
						",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						aDebugStream
					),
					mVM_Data.Int(6)
				);
			}
		),
		mTest.Test(
			"Run5",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
							)
							
							§EXPORT .(
								a => §IF a MATCH {
									(_, a, _) => (a .* a)
									_ => 0
								}
							) (1, 2, 3)
						",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						aDebugStream
					),
					mVM_Data.Int(4)
				);
			}
		)
	);
	
	[xTestCase("Run1")]
	[xTestCase("Run2")]
	[xTestCase("Run3")]
	[xTestCase("Run4")]
	[xTestCase("Run5")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
