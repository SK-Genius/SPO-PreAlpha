//IMPORT Common/mAny.Test.cs
//IMPORT Common/mArrayList.Test.cs
//IMPORT Common/mStream.Test.cs
//IMPORT Common/mMap.Test.cs
//IMPORT Common/mMaybe.Test.cs
//IMPORT Common/mParserGen.Test.cs
//IMPORT Common/mResult.Test.cs
//IMPORT mTokenizer.Test.cs
//IMPORT mIL_Interpreter.Test.cs
//IMPORT mIL_Parser.Test.cs
//IMPORT mSPO_AST_Types.Test.cs
//IMPORT mSPO_Interpreter.Test.cs
//IMPORT mSPO_Parser.Test.cs
//IMPORT mSPO2IL.Test.cs
//IMPORT mStdLib.Test.cs
//IMPORT mVM.Test.cs
//IMPORT mVM_Type.Test.cs

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
public static class
mRunTests {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		"All",
		mAny_Test.Test,
		mMaybe_Test.Test,
		mResult_Test.Test,
		mStream_Test.Test,
		mMap_Test.Test,
		mTree_Tests.Test,
//		mMath_Test.Test,
		mArrayList_Test.Test,
		mParserGen_Test.Test,
//		mTextParser_Test.Test,
		mVM_Test.Test,
		mVM_Type_Test.Test,
//		mVM_Data_Test.Test,
//		mIL_AST_Test.Test,
		mTokenizer_Test.Test,
		mIL_Parser_Test.Test,
		mIL_Interpreter_Test.Test,
//		mSPO_AST_Test.Test,
		mSPO_AST_Types_Test.Test,
		mSPO_Parser_Test.Test,
		mSPO2IL_Test.Test,
		mSPO_Interpreter_Test.Test,
		mStdLib_Test.Test
	);
	
	public static mTest.tResult
	SelfTests(
		mStream.tStream<tText> aFilters,
		mStd.tAction<tText> aDebugOut
	) => Test.Run(aDebugOut, aFilters);
	
	public static int
	Main(
		params tText[] aArgs
	) {
		return SelfTests(
			mStream.Stream(aArgs),
			_ => {
				System.Console.WriteLine(_);
				System.Console.Out.Flush();
			}
		) == mTest.tResult.Fail
		? 0
		: -1;
	}
	
	#if NUNIT
	[xTestCategory("all")]
	[xTestCase("")]
	public static void _(tText a) {
		mAssert.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
