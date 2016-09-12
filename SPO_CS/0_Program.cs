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

public static class Program {
	
	//================================================================================
	public static void
	Main(
		tText[] aArgs
	//================================================================================
	) {
		var Args = mList.List<tText>();
		foreach (var Arg in aArgs) {
			Args = mList.Concat(Args, mList.List(Arg));
		}
		mTest.Tests(
			mStd.Tuple("mStd", mStd.Test),
			mStd.Tuple("mList", mList.Test),
//			mStd.Tuple("mTest", mTest.Test),
			mStd.Tuple("mParserGen", mParserGen.Test),
			mStd.Tuple("mTextParser", mTextParser.Test),
			mStd.Tuple("mIL_AST", mIL_AST.Test),
			mStd.Tuple("mIL_Parser", mIL_Parser.Test),
			mStd.Tuple("mIL_VM", mIL_VM.Test),
			mStd.Tuple("mIL_Interpreter", mIL_Interpreter.Test),
			mStd.Tuple("mSPO_AST", mSPO_AST.Test),
			mStd.Tuple("mSPO_Parser", mSPO_Parser.Test)
		)(
			aLine => System.Console.WriteLine(aLine),
			Args
		);
		
#		if DEBUG && false
			System.Console.ReadKey();
#		endif
	}
	
}
