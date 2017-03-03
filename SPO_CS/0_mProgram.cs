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
	public static void
	Main(
		params tText[] aArgs
	//================================================================================
	) {
		var Args = mList.List<tText>();
		foreach (var Arg in aArgs) {
			Args = mList.Concat(Args, mList.List(Arg));
		}
		mTest.Tests(
			mStd.Tuple(nameof(mStd), mStd.Test),
//			mStd.Tuple(nameof(mTest), mTest.Test),
			mStd.Tuple(nameof(mList), mList.Test),
			mStd.Tuple(nameof(mMap), mMap.Test),
//			mStd.Tuple(nameof(mMath), mMath.Test),
			mStd.Tuple(nameof(mArrayList), mArrayList.Test),
			mStd.Tuple(nameof(mParserGen), mParserGen.Test),
			mStd.Tuple(nameof(mTextParser), mTextParser.Test),
			mStd.Tuple(nameof(mIL_AST), mIL_AST.Test),
			mStd.Tuple(nameof(mIL_Parser), mIL_Parser.Test),
			mStd.Tuple(nameof(mIL_VM), mIL_VM.Test),
			mStd.Tuple(nameof(mIL_Interpreter), mIL_Interpreter.Test),
			mStd.Tuple(nameof(mSPO_AST), mSPO_AST.Test),
			mStd.Tuple(nameof(mSPO_Parser), mSPO_Parser.Test),
			mStd.Tuple(nameof(mSPO2IL), mSPO2IL.Test),
			mStd.Tuple(nameof(mSPO_Interpreter), mSPO_Interpreter.Test)
		)(
			aLine => {
				System.Console.WriteLine(aLine);
				System.Diagnostics.Debug.WriteLine(aLine);
			},
			Args
		);
	}
}
