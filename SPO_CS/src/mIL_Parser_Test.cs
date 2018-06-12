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

using tIL_Parser = mParserGen.tParser<mTextParser.tPos, mIL_Tokenizer.tToken, mTextParser.tError>;
using tPos = mTextParser.tPos;

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class  mIL_Parser_Test {
	//================================================================================
	private static mStd.tSpan<tPos> Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	//================================================================================
	) => new mStd.tSpan<tPos> {
		Start = {
			Row = aStart.Row,
			Col = aStart.Col
		},
		End = {
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	};
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_Parser),
		mTest.Test(
			"Commands",
			aDebugStream => {
				//================================================================================
				void AssertParsedCommand(
					string aText,
					mIL_AST.tCommandNode<tPos> aCommandNode,
					mStd.tAction<string> aDebugStream_
				//================================================================================
				) => mStd.AssertEq(
					mIL_Parser.Command.ParseText(aText, aDebugStream_),
					mParserGen.ResultList(aCommandNode.Span, aCommandNode)
				);
				
				AssertParsedCommand(
					"a := b, c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pair, Span((1, 1), (1, 9)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b == c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAreEq, Span((1, 1), (1, 16)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b <=> c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsComp, Span((1, 1), (1, 17)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b + c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAdd, Span((1, 1), (1, 15)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b - c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsSub, Span((1, 1), (1, 15)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b * c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsMul, Span((1, 1), (1, 15)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §BOOL b & c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolAnd, Span((1, 1), (1, 16)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §BOOL b | c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolOr, Span((1, 1), (1, 16)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §BOOL b ^ c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolXOr, Span((1, 1), (1, 16)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §1ST b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.First, Span((1, 1), (1, 11)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §2ND b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Second, Span((1, 1), (1, 11)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := +#b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.AddPrefix, Span((1, 1), (1, 10)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := -#b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.SubPrefix, Span((1, 1), (1, 10)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := ?#b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.HasPrefix, Span((1, 1), (1, 10)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := .b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Call, Span((1, 1), (1, 9)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"§RETURN a IF b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIf, Span((1, 1), (1, 14)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§REPEAT a IF b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.RepeatIf, Span((1, 1), (1, 14)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§ASSERT a => b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Assert, Span((1, 1), (1, 14)), "a", "b"),
					aDebugStream
				);
				
				AssertParsedCommand(
					"§PUSH a",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Push, Span((1, 1), (1, 7)), "a"),
					aDebugStream
				);
				AssertParsedCommand(
					"§POP",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pop, Span((1, 1), (1, 4))),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §VAR b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarDef, Span((1, 1), (1, 11)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§VAR a <- b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarSet, Span((1, 1), (1, 11)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §VAR b ->",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarGet, Span((1, 1), (1, 14)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §OBJ:b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Exec, Span((1, 1), (1, 13)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b & c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeCond, Span((1, 1), (1, 12)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b => c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeFunc, Span((1, 1), (1, 13)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b : c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeMethod, Span((1, 1), (1, 12)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b, c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePair, Span((1, 1), (1, 11)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [#b c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePrefix, Span((1, 1), (1, 11)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b | c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeSet, Span((1, 1), (1, 12)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [§VAR b]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeVar, Span((1, 1), (1, 13)), "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [§REC b => c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeRecursive, Span((1, 1), (1, 18)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [§ANY b => c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeInterface, Span((1, 1), (1, 18)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [§ALL b => c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeGeneric, Span((1, 1), (1, 18)), "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"§TYPE_OF a IS b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeIs, Span((1, 1), (1, 15)), "a", "b"),
					aDebugStream
				);
			}
		)
	);
	
	[xArg("Commands")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
