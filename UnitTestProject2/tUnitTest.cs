using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class tUnitTest {
	private static void Run(mStd.tFunc<mTest.tResult, mStd.tAction<string>, mList.tList<string>> aTest) {
		aTest(line => System.Diagnostics.Debug.WriteLine(line), mList.List<string>());
	}
	
	[TestMethod] public void Test_1_1_mStd()             => Run(mStd.Test);
//	[TestMethod] public void Test_1_1_mTest()            => Run(mTest.Test);
	[TestMethod] public void Test_1_2_mList()            => Run(mList.Test);
	[TestMethod] public void Test_1_3_mMap()             => Run(mMap.Test);
//	[TestMethod] public void Test_1_4_mMath()            => Run(mMath.Test);
	[TestMethod] public void Test_1_5_mArrayList()       => Run(mArrayList.Test);
	[TestMethod] public void Test_2_mParserGen()         => Run(mParserGen.Test);
	[TestMethod] public void Test_3_mTextParser()        => Run(mTextParser.Test);
	[TestMethod] public void Test_4_1_mIL_AST()          => Run(mIL_AST.Test);
	[TestMethod] public void Test_4_2_mIL_Parser()       => Run(mIL_Parser.Test);
	[TestMethod] public void Test_4_3_mIL_VM()           => Run(mIL_VM.Test);
	[TestMethod] public void Test_4_3_mIL_Interpreter()  => Run(mIL_Interpreter.Test);
	[TestMethod] public void Test_5_1_mSPO_AST()         => Run(mSPO_AST.Test);
	[TestMethod] public void Test_5_2_mSPO_Parser()      => Run(mSPO_Parser.Test);
	[TestMethod] public void Test_5_3_mSPO2IL()          => Run(mSPO2IL.Test);
	[TestMethod] public void Test_5_4_mSPO_Interpreter() => Run(mSPO_Interpreter.Test);
}