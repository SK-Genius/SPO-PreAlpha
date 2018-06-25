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

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class mRunTests {
	static mTest.tTest Test = mTest.Tests(
		"All",
		mStd_Test.Test,
		mList_Test.Test,
		mMap_Test.Test,
//		mMath_Test.Test,
		mArrayList_Test.Test,
		mParserGen_Test.Test,
		mTextParser.Test,
		mVM_Test.Test,
		mVM_Type_Test.Test,
		mVM_Data_Test.Test,
		mIL_AST_Test.Test,
		mIL_Tokenizer_Test.Test,
		mIL_Parser_Test.Test,
		mIL_Interpreter_Test.Test,
		mSPO_AST_Test.Test,
		mSPO_Parser_Test.Test,
		mSPO2IL_Test.Test,
		mSPO_Interpreter_Test.Test,
		mStdLib_Test.Test
	);
	
	//================================================================================
	public static mTest.tResult
	SelfTests(
		mList.tList<tText> aFilters,
		mStd.tAction<tText> aDebugOut
	//================================================================================
	) => Test.Run(aDebugOut, aFilters);
	
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
	
	[xTrait("all", "true")]
	[xArg("")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
