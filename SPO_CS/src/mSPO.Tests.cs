// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT mIL_Parser.Tests
// IMPORT mVM_Type.Tests
// IMPORT mVM.Tests
// IMPORT mIL_GenerateOpcodes.Tests
// IMPORT mSPO_AST_Types.Tests
// IMPORT mSPO_Parser.Tests
// IMPORT mSPO2IL.Tests
// IMPORT mSPO_Interpreter.Tests
// IMPORT mTokenizer.Tests
// IMPORT mStdLib.Tests

public static class
mSPO_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"mSPO",
		[
			mIL_GenerateOpcodes_Tests.Tests,
			mIL_Parser_Tests.Tests,
			mSPO_AST_Types_Tests.Tests,
			mSPO_Interpreter_Tests.Tests,
			mSPO_Parser_Tests.Tests,
			mSPO2IL_Tests.Tests,
			mStdLib_Tests.Tests,
			mTokenizer_Tests.Tests,
			mVM_Type_Tests.Tests,
			mVM_Tests.Tests,
		]
	);
}