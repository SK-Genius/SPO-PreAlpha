#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref mIL_GenerateOpcodes.Tests.cs
#:ref mIL_Parser.Tests.cs
#:ref mModule.Tests.cs
#:ref mE2E.Tests.cs
#:ref mSPO_AST_Types.Tests.cs
#:ref mSPO_Parser.Tests.cs
#:ref mSPO2IL.Tests.cs
#:ref mTokenizer.Tests.cs
#:ref mVM_Type.Tests.cs
#:ref mVM.Tests.cs

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
			mE2E_Tests.Tests,
			mModule_Tests.Tests,
		]
	);
}
