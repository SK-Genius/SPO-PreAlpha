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

#nullable enable

return mTest.Tests(
	"All",
	mAny_Tests.Tests,
	mMaybe_Tests.Tests,
	mResult_Tests.Tests,
	mStream_Tests.Tests,
	mMap_Tests.Tests,
	mTreeMap_Tests.Tests,
//	mMath_Test.Test,
	mArrayList_Tests.Tests,
	mParserGen_Tests.Tests,
//	mTextParser_Test.Test,
	mVM_Tests.Tests,
	mVM_Type_Tests.Tests,
//	mVM_Data_Test.Test,
//	mIL_AST_Test.Test,
	mTokenizer_Tests.Tests,
	mIL_Parser_Tests.Tests,
	mIL_GenerateOpcodes_Tests.Tests,
//	mSPO_AST_Test.Test,
	mSPO_AST_Types_Tests.Tests,
	mSPO_Parser_Tests.Tests,
	mSPO2IL_Tests.Tests,
	mSPO_Interpreter_Tests.Tests,
	mStdLib_Tests.Tests
).Run(
	a => {
		System.Console.WriteLine(a.Replace("\t", "  "));
		System.Console.Out.Flush();
	},
	mStream.Stream(args)
).Result == mTest.tResult.Fail ? -1 : 0;
