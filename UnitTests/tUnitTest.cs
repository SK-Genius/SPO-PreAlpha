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
		mStd.AssertEq(
			aTest.Run(tConsole.WriteLine, mList.List(aFilter)),
			mTest.tResult.OK
		);
	}
	
	[xTrait("all", "true")]
	[xArg("")]
	[xTest] public static void _99_RunTests(tText a) {
		mStd.AssertEq(
			mRunTests.SelfTests(mList.List(a), tConsole.WriteLine),
			mTest.tResult.OK
		);
	}
	
	[xArg("tMaybe.ToString()")]
	[xArg("tMaybe.Equals()")]
	[xArg("tVar.ToString()")]
	[xArg("tVar.Equals()")]
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
	[xTest] public static void Test(tText a) => mTest.Test_.Run(a);

	[xArg("tMap.Get")]
	[xArg("tMap.TryGet")]
	[xArg("tMap.Remove")]
	[xTest] public static void Map(tText a) => mMap.Test.Run(a);
	
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
	[xTest] public static void ArrayList(tText a) => mArrayList.Test.Run(a);
	
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
	[xTest] public static void ParserGen(tText a) => mParserGen.Test.Run(a);
	
	[xArg("TODO")]
	[xTest] public static void TextParser(tText a) => mTextParser.Test.Run(a);
	
	[xArg("ExternDef")]
	[xArg("InternDef")]
	[xTest] public static void VM(tText a) => mVM.Test.Run(a);
	
	[xArg("BoolBool")]
	[xArg("BoolInt")]
	[xTest] public static void VM_Type(tText a) => mVM_Type.Test.Run(a);
	
	[xArg("TODO: ExternDef")]
	[xTest] public static void VM_Data(tText a) => mVM_Data.Test.Run(a);
	
	[xArg("TODO")]
	[xTest] public static void IL_AST(tText a) => mIL_AST.Test.Run(a);
	
	[xArg("SubParser")]
	[xArg("Commands")]
	[xTest] public static void IL_Parser(tText a) => mIL_Parser.Test.Run(a);
	
	[xArg("Call")]
	[xArg("Prefix")]
	[xArg("Assert")]
	[xArg("§TYPE_OF...IS...")]
	[xArg("ParseModule")]
	[xTest] public static void IL_Interpreter(tText a) => mIL_Interpreter.Test.Run(a);
	
	[xArg("TODO")]
	[xTest] public static void SPO_AST(tText a) => mSPO_AST.Test.Run(a);
	
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
	[xTest] public static void SPO_Parser(tText a) => mSPO_Parser.Test.Run(a);
	
	[xArg("MapExpresion")]
	[xArg("MapDef1")]
	[xArg("MapDefMatch")]
	[xArg("MatchTuple")]
	[xArg("MapMatchPrefix")]
	[xArg("MapLambda1")]
	[xArg("MapLambda2")]
	[xArg("MapNestedMatch")]
	[xArg("MapModule")]
	[xTest] public static void SPO2IL(tText a) => mSPO2IL.Test.Run(a);
	
	[xArg("Run1")]
	[xArg("Run2")]
	[xArg("Run3")]
	[xArg("Run4")]
	[xArg("Run5")]
	[xTest] public static void SPO_Interpreter(tText a) => mSPO_Interpreter.Test.Run(a);
	
	[xArg("IfThenElse")]
	[xArg("If2")]
	[xArg("IfMatch1")]
	[xArg("IfMatch2")]
	[xArg("VAR")]
	[xArg("Echo")]
	[xTest] public static void StdLib(tText a) => mStdLib.Test.Run(a);
}