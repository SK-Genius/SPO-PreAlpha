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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// This file only exist for the codecoverage tools (for example OpenCover / AxoCover)

static class mTestHelper {
	//================================================================================
	internal static void
	MagicRun(
		mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> aTest,
		[CallerMemberName] tText aFilter = ""
	//================================================================================
	) {
		mStd.AssertEq(
			aTest(
				line => Debug.WriteLine(line),
				mList.List(aFilter.Replace('_', '.'))
			),
			mTest.tResult.OK
		);
	}
}

[TestClass]public class Test_All {
	[TestMethod] public void All() => mStd.AssertEq(
		mProgram.SelfTests(mList.List(""), System.Console.WriteLine),
		mTest.tResult.OK
	);
}

[TestClass]public class Test_1_1_mStd {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mStd.Test;
	
	[TestMethod] public void tMaybe_ToString() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tMaybe_Equals() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tVar_ToString() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tVar_Equals() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tTuple_ToString() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tTuple_Equals() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_1_1_mTest {
}

[TestClass]public class Test_1_2_mList {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mList.Test;
	
	[TestMethod] public void tList_ToString() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tList_Equals() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Concat() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Map() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Reduce() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Join() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Take() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Skip() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void IsEmpty() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Any() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Every() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_1_3_mMap {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mMap.Test;
	
	[TestMethod] public void tMap_ToString() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_1_4_mMath {
}

[TestClass]public class Test_1_5_mArrayList {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mArrayList.Test;
	
	[TestMethod] public void tArrayList_IsEmpty() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tArrayList_ToLasyList() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tArrayList_Push() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tArrayList_Pop() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tArrayList_Get() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void tArrayList_Set() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void mArrayList_Concat() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_2_mParserGen {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mParserGen.Test;
	
	[TestMethod] public void AtomParser() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void _Plus_() => mTestHelper.MagicRun(Tests, "...+...");
	[TestMethod] public void _Minus_() => mTestHelper.MagicRun(Tests, "...-...");
	[TestMethod] public void Minus_() => mTestHelper.MagicRun(Tests, "-...");
	[TestMethod] public void NStar_() => mTestHelper.MagicRun(Tests, "n*...");
	[TestMethod] public void MinusNStar() => mTestHelper.MagicRun(Tests, "-n*...");
	[TestMethod] public void _StarN() => mTestHelper.MagicRun(Tests, "...*n");
	[TestMethod] public void _Or_() => mTestHelper.MagicRun(Tests, "...|...");
	[TestMethod] public void MinMax() => mTestHelper.MagicRun(Tests, "...[m, n]");
	[TestMethod] public void Min() => mTestHelper.MagicRun(Tests, "...[n, null]");
	[TestMethod] public void Modify() => mTestHelper.MagicRun(Tests, "....ModifyList(...=>...)");
	[TestMethod] public void ModifyReduce() => mTestHelper.MagicRun(Tests, "....ModifyList(a => a.Reduce(...))");
	[TestMethod] public void Not_() => mTestHelper.MagicRun(Tests, "~...");
	[TestMethod] public void EvalMathExp() => mTestHelper.MagicRun(Tests, "Eval('MathExpr')");
}

[TestClass]public class Test_3_mTextParser {
}

[TestClass]public class Test_4_1_mIL_AST {
}

[TestClass]public class Test_4_2_mIL_Parser {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mIL_Parser.Test;
	
	[TestMethod] public void SubParser() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Commands() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_4_3_mIL_VM {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mIL_VM.Test;
	
	[TestMethod] public void ExternDef() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void InternDef() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_4_3_mIL_Interpreter {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mIL_Interpreter.Test;
	
	[TestMethod] public void Call() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Prefix() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Assert() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void ParseModule() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_5_1_mSPO_AST {
}

[TestClass]public class Test_5_2_mSPO_Parser {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mSPO_Parser.Test;
	
	[TestMethod] public void Atoms() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Tuple() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Match() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Call() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Lambda() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void NestedMatch() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void PrefixMatch() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_5_3_mSPO2IL {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mSPO2IL.Test;
	
	[TestMethod] public void MapExpresion() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapAssignment1() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapAssignmentMatch() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MatchTuple() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapMatchPrefix() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapLambda1() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapLambda2() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapNestedMatch() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void MapModule() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_5_4_mSPO_Interpreter {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mSPO_Interpreter.Test;
	
	[TestMethod] public void Run1() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Run2() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void Run3() => mTestHelper.MagicRun(Tests);
}

[TestClass]public class Test_6_mStdLib {
	static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Tests = mStdLib.Test;
	
	[TestMethod] public void Fib() => mTestHelper.MagicRun(Tests);
	[TestMethod] public void IfThenElse() => mTestHelper.MagicRun(Tests);
}