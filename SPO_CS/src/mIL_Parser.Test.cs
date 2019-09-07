//IMPORT mTest.cs
//IMPORT mIL_Parser.cs

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

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class
mIL_Parser_Test {
	
	private static tSpan
	Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	) => new tSpan {
		Start = {
			Ident = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		End = {
			Ident = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	};
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_Parser),
		mTest.Test(
			"Commands",
			aStreamOut => {
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §IS_BOOL b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IsBool, Span((1, 1), (1, 15)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §IS_INT b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IsInt, Span((1, 1), (1, 14)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §IS_PREFIX b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IsPrefix, Span((1, 1), (1, 17)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §IS_PAIR b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IsPair, Span((1, 1), (1, 15)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §IS_RECORD b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IsRecord, Span((1, 1), (1, 17)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §IS_TYPE b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IsType, Span((1, 1), (1, 15)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := b, c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pair, Span((1, 1), (1, 9)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §INT b == c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAreEq, Span((1, 1), (1, 16)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §INT b <=> c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsComp, Span((1, 1), (1, 17)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §INT b + c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAdd, Span((1, 1), (1, 15)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §INT b - c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsSub, Span((1, 1), (1, 15)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §INT b * c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsMul, Span((1, 1), (1, 15)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §BOOL b & c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolAnd, Span((1, 1), (1, 16)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §BOOL b | c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolOr, Span((1, 1), (1, 16)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §BOOL b ^ c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolXOr, Span((1, 1), (1, 16)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §1ST b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.First, Span((1, 1), (1, 11)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §2ND b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Second, Span((1, 1), (1, 11)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := +#b c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.AddPrefix, Span((1, 1), (1, 10)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := -#b c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.SubPrefix, Span((1, 1), (1, 10)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := ?#b c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.HasPrefix, Span((1, 1), (1, 10)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := {b} + c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ExtendRec, Span((1, 1), (1, 12)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := {b} /", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.DivideRec, Span((1, 1), (1, 10)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := .b c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.CallFunc, Span((1, 1), (1, 9)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("§RETURN a IF b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIf, Span((1, 1), (1, 14)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("§REPEAT a IF b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.RepeatIf, Span((1, 1), (1, 14)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("§ASSERT a => b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Assert, Span((1, 1), (1, 14)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §VAR b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarDef, Span((1, 1), (1, 11)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("§VAR a <- b", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarSet, Span((1, 1), (1, 11)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §VAR b ->", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarGet, Span((1, 1), (1, 14)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := §OBJ:b c", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.CallProc, Span((1, 1), (1, 13)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [b & c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeCond, Span((1, 1), (1, 12)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [b => c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeFunc, Span((1, 1), (1, 13)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [b : c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeMethod, Span((1, 1), (1, 12)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [b, c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePair, Span((1, 1), (1, 11)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [#b c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePrefix, Span((1, 1), (1, 11)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [{b} + c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeRec, Span((1, 1), (1, 14)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [b | c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeSet, Span((1, 1), (1, 12)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [§VAR b]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeVar, Span((1, 1), (1, 13)), "a", "b")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [§REC b => c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeRecursive, Span((1, 1), (1, 18)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [§ANY b => c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeInterface, Span((1, 1), (1, 18)), "a", "b", "c")
				);
				mAssert.AreEquals(
					mIL_Parser.Command.ParseText("a := [§ALL b => c]", "", a => aStreamOut(a())),
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeGeneric, Span((1, 1), (1, 18)), "a", "b", "c")
				);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("Commands")]
	public static void _(tText a) {
		mAssert.AreEquals(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
