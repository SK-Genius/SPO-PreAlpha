#:include Common/mStd.cs
#:include Common/mTest.cs
#:include mIL_Parser.Tests.cs
#:include mVM_Type.Tests.cs
#:include mVM.Tests.cs
#:include mIL_GenerateOpcodes.Tests.cs
#:include mSPO_AST_Types.Tests.cs
#:include mSPO_Parser.Tests.cs
#:include mSPO2IL.Tests.cs
#:include mTokenizer.Tests.cs
#:include mRegression.Tests.cs
#:include mModule.Tests.cs

public static class
mSPO_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"mSPO",
		[
			mTokenizer_Tests.Tests,
			mIL_Parser_Tests.Tests,
			mIL_GenerateOpcodes_Tests.Tests,
			mVM_Type_Tests.Tests,
			mVM_Tests.Tests,
			mSPO_AST_Types_Tests.Tests,
			mSPO_Parser_Tests.Tests,
			mSPO2IL_Tests.Tests,
			mRegression_Tests.Tests,
			mModule_Tests.Tests,
		]
	);
}
