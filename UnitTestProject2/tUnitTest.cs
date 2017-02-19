using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class tUnitTest {
	[TestMethod] public void Test_mStd()        { mProgram.Main(nameof(mStd)); }
	[TestMethod] public void Test_mTest()       { mProgram.Main(nameof(mTest)); }
	[TestMethod] public void Test_mList()       { mProgram.Main(nameof(mList)); }
	[TestMethod] public void Test_mMap()        { mProgram.Main(nameof(mMap)); }
	[TestMethod] public void Test_mMath()       { mProgram.Main(nameof(mMath)); }
	[TestMethod] public void Test_mArrayList()  { mProgram.Main(nameof(mArrayList)); }
	[TestMethod] public void Test_mParserGen()  { mProgram.Main(nameof(mParserGen)); }
	[TestMethod] public void Test_mTextParser() { mProgram.Main(nameof(mTextParser)); }
	[TestMethod] public void Test_mIL_AST()     { mProgram.Main(nameof(mIL_AST)); }
	[TestMethod] public void Test_mIL_Parser()  { mProgram.Main(nameof(mIL_Parser)); }
	[TestMethod] public void Test_mIL_VM()      { mProgram.Main(nameof(mIL_VM)); }
	[TestMethod] public void Test_mSPO_AST()    { mProgram.Main(nameof(mSPO_AST)); }
	[TestMethod] public void Test_mSPO_Parser() { mProgram.Main(nameof(mSPO_Parser)); }
	[TestMethod] public void Test_mSPO2IL()     { mProgram.Main(nameof(mSPO2IL)); }
}