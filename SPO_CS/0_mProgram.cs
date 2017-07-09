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

public static class mProgram {
	//================================================================================
	public static mTest.tResult
	SelfTests(
		mList.tList<tText> aFilters,
		mStd.tAction<tText> aDebugOut
	//================================================================================
	) => mTest.Tests(
		"All",
		mStd.Test,
//		mTest.Test,
		mList.Test,
		mMap.Test,
//		mMath.Test,
		mArrayList.Test,
		mParserGen.Test,
		mTextParser.Test,
		mIL_AST.Test,
		mIL_Parser.Test,
		mIL_VM.Test,
		mIL_Interpreter.Test,
		mSPO_AST.Test,
		mSPO_Parser.Test,
		mSPO2IL.Test,
		mSPO_Interpreter.Test,
		mStdLib.Test
	).Run(
		aDebugOut,
		aFilters
	);
	
	//================================================================================
	public static void
	Main(
		params tText[] aArgs
	//================================================================================
	) {
		var Args = mList.List<tText>();
		foreach (var Arg in aArgs) {
			Args = mList.Concat(Args, mList.List(Arg));
		}
		if (
			SelfTests(
				Args,
				aLine => {
					System.Console.WriteLine(aLine);
					System.Diagnostics.Debug.WriteLine(aLine);
				}
			) == mTest.tResult.Fail
		) {
			System.Environment.Exit(-1);
		}
	}
}
