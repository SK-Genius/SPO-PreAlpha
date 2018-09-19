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

static class mProgram {
	static void Main() {
		try {
			var DebugOut = mStd.Action((tText a) => System.Console.WriteLine(a));
			
			var StdLib = mStdLib.GetImportData();
			
			var StackModule = mSPO_Interpreter.Run(
				System.IO.File.ReadAllText(@"src\Stack.spo"),
				mVM_Data.Empty(),
				DebugOut
			);
			
			var StackTestModule = mSPO_Interpreter.Run(
				System.IO.File.ReadAllText(@"src\StackTest.spo"),
				mVM_Data.Tuple(StdLib, StackModule),
				DebugOut
			);
			
			mStd.Assert(StackTestModule.MatchBool(out var IsOK));
			mStd.Assert(IsOK);
			
			DebugOut("OK");
		} catch (mStd.tError<mStd.tEmpty> Error) {
			System.Console.WriteLine(Error.Message);
			System.Environment.Exit(-1);
		}
	}
}
