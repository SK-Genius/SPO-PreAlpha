//IMPORT Common/mAny.Tests.cs
//IMPORT Common/mArrayList.Tests.cs
//IMPORT Common/mStream.Tests.cs
//IMPORT Common/mMap.Tests.cs
//IMPORT Common/mMaybe.Tests.cs
//IMPORT Common/mParserGen.Tests.cs
//IMPORT Common/mResult.Tests.cs
//IMPORT mTokenizer.Tests.cs
//IMPORT mIL_Interpreter.Tests.cs
//IMPORT mIL_Parser.Tests.cs
//IMPORT mSPO_AST_Types.Tests.cs
//IMPORT mSPO_Interpreter.Tests.cs
//IMPORT mSPO_Parser.Tests.cs
//IMPORT mSPO2IL.Tests.cs
//IMPORT mStdLib.Tests.cs
//IMPORT mVM.Tests.cs
//IMPORT mVM_Type.Tests.cs

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
		mAny_Tests.Tests,
		mMaybe_Tests.Tests,
		mResult_Tests.Tests,
		mStream_Tests.Tests,
		mMap_Tests.Tests,
		mTreeMap_Tests.Tests,
//		mMath_Test.Test,
		mArrayList_Tests.Tests,
		mParserGen_Tests.Tests,
//		mTextParser_Test.Test,
		mVM_Tests.Tests,
		mVM_Type_Tests.Tests,
//		mVM_Data_Test.Test,
//		mIL_AST_Test.Test,
		mTokenizer_Tests.Tests,
		mIL_Parser_Tests.Tests,
		mIL_Interpreter_Tests.Tests,
//		mSPO_AST_Test.Test,
		mSPO_AST_Types_Tests.Tests,
		mSPO_Parser_Tests.Tests,
		mSPO2IL_Tests.Tests,
		mSPO_Interpreter_Tests.Tests,
		mStdLib_Tests.Tests
	);
	
	public static int
	Main(
		params tText[] aArgs
	) {
		return Test.Run(
			_ => {
				System.Console.WriteLine(_);
				System.Console.Out.Flush();
			},
			mStream.Stream(aArgs)
		).Result == mTest.tResult.Fail
		? 0
		: -1;
	}
	
	#if NUNIT
	[xTestCategory("all")]
	[xTestCase("")]
	public static void _(tText a) {
		mAssert.AreEquals(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)).Result,
			mTest.tResult.OK
		);
	}
	#endif
}
