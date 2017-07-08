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

using tConsole = System.Console;
using tDateTime = System.DateTime;
using tFile = System.IO.File;
using tStopWatch = System.Diagnostics.Stopwatch;

using xCallerName = System.Runtime.CompilerServices.CallerMemberNameAttribute;
using xTests = Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute;
using xTest = Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute;

// This file only exist for the codecoverage tools (for example OpenCover / AxoCover)

internal static class mTestHelper {
	//================================================================================
	internal static void
	MagicRun(
		mTest.tTest aTest,
		[xCallerName] tText aFilter = ""
	//================================================================================
	) {
		var StopWatch = new tStopWatch();
		StopWatch.Start();
		mStd.AssertEq(
			aTest.Run(
				tConsole.WriteLine,
				mList.List(aFilter.Replace('_', '.'))
			),
			mTest.tResult.OK
		);
		StopWatch.Stop();
		tFile.AppendAllText(
			@"Performanc.tsv",
			$"{tDateTime.Now:yyyy-MM-dd HH:mm:ss}\t{aFilter}\t{(StopWatch.ElapsedTicks+5)/10}�s\n"
		);
	}
}

[xTests] public class Test_All {
	[xTest] public void All() => mStd.AssertEq(
		mProgram.SelfTests(mList.List(""), System.Console.WriteLine),
		mTest.tResult.OK
	);
}

[xTests] public class Test_1_1_mStd {
	private static readonly mTest.tTest Tests = mStd.Test;
	
	[xTest] public void tMaybe_ToString() => mTestHelper.MagicRun(Tests);
	[xTest] public void tMaybe_Equals() => mTestHelper.MagicRun(Tests);
	[xTest] public void tVar_ToString() => mTestHelper.MagicRun(Tests);
	[xTest] public void tVar_Equals() => mTestHelper.MagicRun(Tests);
	[xTest] public void tTuple_ToString() => mTestHelper.MagicRun(Tests);
	[xTest] public void tTuple_Equals() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_1_1_mTest {
}

[xTests] public class Test_1_2_mList {
	private static readonly mTest.tTest Tests = mList.Test;
	
	[xTest] public void tList_ToString() => mTestHelper.MagicRun(Tests);
	[xTest] public void tList_Equals() => mTestHelper.MagicRun(Tests);
	[xTest] public void Concat() => mTestHelper.MagicRun(Tests);
	[xTest] public void Map() => mTestHelper.MagicRun(Tests);
	[xTest] public void Reduce() => mTestHelper.MagicRun(Tests);
	[xTest] public void Join() => mTestHelper.MagicRun(Tests);
	[xTest] public void Take() => mTestHelper.MagicRun(Tests);
	[xTest] public void Skip() => mTestHelper.MagicRun(Tests);
	[xTest] public void IsEmpty() => mTestHelper.MagicRun(Tests);
	[xTest] public void Any() => mTestHelper.MagicRun(Tests);
	[xTest] public void Every() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_1_3_mMap {
	private static readonly mTest.tTest Tests = mMap.Test;
	
	[xTest] public void tMap_Get() => mTestHelper.MagicRun(Tests);
	[xTest] public void tMap_TryGet() => mTestHelper.MagicRun(Tests);
	[xTest] public void tMap_Remove() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_1_4_mMath {
}

[xTests] public class Test_1_5_mArrayList {
	private static readonly mTest.tTest Tests = mArrayList.Test;
	
	[xTest] public void tArrayList_IsEmpty() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_Equals() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_ToArrayList() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_ToLasyList() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_Push() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_Pop() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_Get() => mTestHelper.MagicRun(Tests);
	[xTest] public void tArrayList_Set() => mTestHelper.MagicRun(Tests);
	[xTest] public void mArrayList_Concat() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_2_mParserGen {
	private static readonly mTest.tTest Tests = mParserGen.Test;
	
	[xTest] public void AtomParser() => mTestHelper.MagicRun(Tests);
	[xTest] public void _Plus_() => mTestHelper.MagicRun(Tests, "...+...");
	[xTest] public void _Minus_() => mTestHelper.MagicRun(Tests, "...-...");
	[xTest] public void Minus_() => mTestHelper.MagicRun(Tests, "-...");
	[xTest] public void NStar_() => mTestHelper.MagicRun(Tests, "n*...");
	[xTest] public void MinusNStar() => mTestHelper.MagicRun(Tests, "-n*...");
	[xTest] public void _StarN() => mTestHelper.MagicRun(Tests, "...*n");
	[xTest] public void _Or_() => mTestHelper.MagicRun(Tests, "...|...");
	[xTest] public void MinMax() => mTestHelper.MagicRun(Tests, "...[m, n]");
	[xTest] public void Min() => mTestHelper.MagicRun(Tests, "...[n, null]");
	[xTest] public void Modify() => mTestHelper.MagicRun(Tests, "....ModifyList(...=>...)");
	[xTest] public void ModifyReduce() => mTestHelper.MagicRun(Tests, "....ModifyList(a => a.Reduce(...))");
	[xTest] public void Not_() => mTestHelper.MagicRun(Tests, "~...");
	[xTest] public void EvalMathExp() => mTestHelper.MagicRun(Tests, "Eval('MathExpr')");
}

[xTests] public class Test_3_mTextParser {
}

[xTests] public class Test_4_1_mIL_AST {
}

[xTests] public class Test_4_2_mIL_Parser {
	private static readonly mTest.tTest Tests = mIL_Parser.Test;
	
	[xTest] public void SubParser() => mTestHelper.MagicRun(Tests);
	[xTest] public void Commands() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_4_3_mIL_VM {
	private static readonly mTest.tTest Tests = mIL_VM.Test;
	
	[xTest] public void ExternDef() => mTestHelper.MagicRun(Tests);
	[xTest] public void InternDef() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_4_3_mIL_Interpreter {
	private static readonly mTest.tTest Tests = mIL_Interpreter.Test;
	
	[xTest] public void Call() => mTestHelper.MagicRun(Tests);
	[xTest] public void Prefix() => mTestHelper.MagicRun(Tests);
	[xTest] public void Assert() => mTestHelper.MagicRun(Tests);
	[xTest] public void ParseModule() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_5_1_mSPO_AST {
}

[xTests] public class Test_5_2_mSPO_Parser {
	private static readonly mTest.tTest Tests = mSPO_Parser.Test;
	
	[xTest] public void Atoms() => mTestHelper.MagicRun(Tests);
	[xTest] public void Tuple() => mTestHelper.MagicRun(Tests);
	[xTest] public void Match() => mTestHelper.MagicRun(Tests);
	[xTest] public void Call() => mTestHelper.MagicRun(Tests);
	[xTest] public void Lambda() => mTestHelper.MagicRun(Tests);
	[xTest] public void NestedMatch() => mTestHelper.MagicRun(Tests);
	[xTest] public void PrefixMatch() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_5_3_mSPO2IL {
	private static readonly mTest.tTest Tests = mSPO2IL.Test;
	
	[xTest] public void MapExpresion() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapDef1() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapDefMatch() => mTestHelper.MagicRun(Tests);
	[xTest] public void MatchTuple() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapMatchPrefix() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapLambda1() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapLambda2() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapNestedMatch() => mTestHelper.MagicRun(Tests);
	[xTest] public void MapModule() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_5_4_mSPO_Interpreter {
	private static readonly mTest.tTest Tests = mSPO_Interpreter.Test;
	
	[xTest] public void Run1() => mTestHelper.MagicRun(Tests);
	[xTest] public void Run2() => mTestHelper.MagicRun(Tests);
	[xTest] public void Run3() => mTestHelper.MagicRun(Tests);
}

[xTests] public class Test_6_mStdLib {
	private static readonly mTest.tTest Tests = mStdLib.Test;
	
	[xTest] public void IfThenElse() => mTestHelper.MagicRun(Tests);
	[xTest] public void If2() => mTestHelper.MagicRun(Tests);
	[xTest] public void IfMatch1() => mTestHelper.MagicRun(Tests);
	[xTest] public void IfMatch2() => mTestHelper.MagicRun(Tests);
	[xTest] public void VAR() => mTestHelper.MagicRun(Tests);
}