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

public static class mRunTests {
	//================================================================================
	public static mTest.tResult
	SelfTests(
		mList.tList<tText> aFilters,
		mStd.tAction<tText> aDebugOut
	//================================================================================
	) => mTest.Tests(
		"All",
		mTest.Test_,
		mMap.Test,
//		mMath.Test,
		mArrayList.Test,
		mParserGen.Test,
		mTextParser.Test,
		mVM.Test,
		mVM_Type.Test,
		mVM_Data.Test,
		mIL_AST.Test,
		mIL_Tokenizer.Test,
		mIL_Parser.Test,
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
		if (
			SelfTests(
				mList.List(aArgs),
				_ => { System.Console.WriteLine(_); System.Console.Out.Flush(); }
			) == mTest.tResult.Fail
		) {
			System.Environment.Exit(-1);
		}
	}
}
