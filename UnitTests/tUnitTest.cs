// This file only exist for the codecoverage tools (for example OpenCover / AxoCover)

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

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

public static class tTests {
	//================================================================================
	internal static void
	Run(
		this mTest.tTest aTest,
		tText aFilter
	//================================================================================
	) {
		mTest.AssertEq(
			aTest.Run(tConsole.WriteLine, mList.List(aFilter)),
			mTest.tResult.OK
		);
	}
	
	[xTrait("all", "true")]
	[xArg("")]
	[xTest] public static void _0_All(tText a) {
		mTest.AssertEq(
			mProgram.SelfTests(mList.List(a), tConsole.WriteLine),
			mTest.tResult.OK
		);
	}
	
	[xArg("tMaybe.ToString()")]
	[xArg("tMaybe.Equals()")]
	[xArg("tVar.ToString()")]
	[xArg("tVar.Equals()")]
	[xTest] public static void _1_1_Std(tText a) => mStd.Test.Run(a);
	
	[xArg("tList.ToString()")]
	[xArg("tList.Equals()")]
	[xArg("Concat()")]
	[xArg("Map()")]
	[xArg("MapWithIndex()")]
	[xArg("Reduce()")]
	[xArg("Join()")]
	[xArg("Take()")]
	[xArg("Skip()")]
	[xArg("IsEmpty()")]
	[xArg("Any()")]
	[xArg("Every()")]
	[xTest] public static void _1_2_List(tText a) => mList.Test.Run(a);
	
	[xArg("tMap.Get")]
	[xArg("tMap.TryGet")]
	[xArg("tMap.Remove")]
	[xTest] public static void _1_3_Map(tText a) => mMap.Test.Run(a);
	
	[xArg("tArrayList.IsEmpty(...)")]
	[xArg("tArrayList.Equals(...)")]
	[xArg("tArrayList.ToArrayList()")]
	[xArg("tArrayList.ToLasyList()")]
	[xArg("tArrayList.Push(...)")]
	[xArg("tArrayList.Pop()")]
	[xArg("tArrayList.Resize(...)")]
	[xArg("tArrayList.Get(...)")]
	[xArg("tArrayList.Set(...)")]
	[xArg("mArrayList.Concat(...)")]
	[xTest] public static void _1_5_ArrayList(tText a) => mArrayList.Test.Run(a);
	
	[xArg("AtomParser")]
	[xArg("...+...")]
	[xArg("...-...")]
	[xArg("-...")]
	[xArg("n*...")]
	[xArg("-n*...")]
	[xArg("...*n")]
	[xArg("...|...")]
	[xArg("...[m, n]")]
	[xArg("...[n, null]")]
	[xArg("....ModifyList(...=>...)")]
	[xArg("....ModifyList(a => a.Reduce(...))")]
	[xArg("~...")]
	[xArg("Eval('MathExpr')")]
	[xTest] public static void _2_1_ParserGen(tText a) => mParserGen.Test.Run(a);
	
	[xArg("TODO")]
	[xTest] public static void _2_2_TextParser(tText a) => mTextParser.Test.Run(a);
	
	[xArg("ExternDef")]
	[xArg("InternDef")]
	[xTest] public static void _3_1_VM(tText a) => mVM.Test.Run(a);
	
	[xArg("BoolBool")]
	[xArg("BoolInt")]
	[xTest] public static void _3_2_VM_Type(tText a) => mVM_Type.Test.Run(a);
	
	[xArg("TODO: ExternDef")]
	[xTest] public static void _3_3_VM_Data(tText a) => mVM_Data.Test.Run(a);
	
	[xArg("TODO")]
	[xTest] public static void _4_1_IL_AST(tText a) => mIL_AST.Test.Run(a);
	
	[xArg("SubParser")]
	[xArg("Commands")]
	[xTest] public static void _4_2_IL_Parser(tText a) => mIL_Parser.Test.Run(a);
	
	[xArg("Call")]
	[xArg("Prefix")]
	[xArg("Assert")]
	[xArg("§TYPE_OF...IS...")]
	[xArg("ParseModule")]
	[xTest] public static void _4_3_IL_Interpreter(tText a) => mIL_Interpreter.Test.Run(a);
	
	[xArg("TODO")]
	[xTest] public static void _5_1_SPO_AST(tText a) => mSPO_AST.Test.Run(a);
	
	[xArg("Atoms")]
	[xArg("Tuple")]
	[xArg("Match1")]
	[xArg("FunctionCall")]
	[xArg("Lambda")]
	[xArg("Expression")]
	[xArg("TypedMatch")]
	[xArg("NestedMatch")]
	[xArg("PrefixMatch")]
	[xArg("MethodCall")]
	[xTest] public static void _5_2_SPO_Parser(tText a) => mSPO_Parser.Test.Run(a);
	
	[xArg("MapExpresion")]
	[xArg("MapDef1")]
	[xArg("MapDefMatch")]
	[xArg("MatchTuple")]
	[xArg("MapMatchPrefix")]
	[xArg("MapLambda1")]
	[xArg("MapLambda2")]
	[xArg("MapNestedMatch")]
	[xArg("MapModule")]
	[xTest] public static void _5_3_SPO2IL(tText a) => mSPO2IL.Test.Run(a);
	
	[xArg("Run1")]
	[xArg("Run2")]
	[xArg("Run3")]
	[xArg("Run4")]
	[xArg("Run5")]
	[xTest] public static void _5_4_SPO_Interpreter(tText a) => mSPO_Interpreter.Test.Run(a);
	
	[xArg("IfThenElse")]
	[xArg("If2")]
	[xArg("IfMatch1")]
	[xArg("IfMatch2")]
	[xArg("VAR")]
	[xArg("Echo")]
	[xTest] public static void _6_StdLib(tText a) => mStdLib.Test.Run(a);
}