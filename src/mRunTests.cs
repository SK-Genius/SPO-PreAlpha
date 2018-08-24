//IMPORT mArrayList_Test.cs
//IMPORT mIL_AST_Test.cs
//IMPORT mIL_Interpreter_Test.cs
//IMPORT mIL_Parser_Test.cs
//IMPORT mList_Test.cs
//IMPORT mMap_Test.cs
//IMPORT mParserGen_Test.cs
//IMPORT mSPO_AST_Test.cs
//IMPORT mSPO_Interpreter_Test.cs
//IMPORT mSPO_Parser_Test.cs
//IMPORT mSPO2IL_Test.cs
//IMPORT mStd_Test.cs
//IMPORT mStdLib_Test.cs
//IMPORT mTextParser_Test.cs
//IMPORT mTextStream_Test.cs
//IMPORT mTokenizer_Test.cs
//IMPORT mVM_Test.cs
//IMPORT mVM_Data_Test.cs
//IMPORT mVM_Type_Test.cs

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

#if NUNIT
using xTestCase = NUnit.Framework.TestCaseAttribute;
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCategory = NUnit.Framework.CategoryAttribute;

[xTestClass]
#endif
public static class mRunTests {
	
	static mTest.tTest Test = mTest.Tests(
		"All",
		mStd_Test.Test,
		mList_Test.Test,
		mMap_Test.Test,
//		mMath_Test.Test,
		mArrayList_Test.Test,
		mParserGen_Test.Test,
		mTextParser_Test.Test,
		mVM_Test.Test,
		mVM_Type_Test.Test,
		mVM_Data_Test.Test,
		mIL_AST_Test.Test,
		mTokenizer_Test.Test,
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
	
	#if NUNIT
	[xTestCategory("all")]
	[xTestCase("")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
