using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mIL_Parser_Tests {
	
	private static tSpan
	Span(
		(tNat32 Row, tNat32 Col) aStart,
		(tNat32 Row, tNat32 Col) aEnd
	) => mSpan.Span(
		new tPos {
			Id = "",
			Row = aStart.Row,
			Col = aStart.Col
		},
		new tPos {
			Id = "",
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mIL_Parser),
		new (tText Expr, mIL_AST.tCommandNode<tSpan> Command)[] {
			("a := b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Alias, Span((1, 1), (1, 6)), "a", "b")),
			("a := 1", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Int, Span((1, 1), (1, 6)), "a", "1")),
			("a := §INT b == c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAreEq, Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := §INT b <=> c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsComp, Span((1, 1), (1, 17)), "a", "b", "c")),
			("a := §INT b + c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAdd, Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §INT b - c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsSub, Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §INT b * c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsMul, Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §INT b / c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsDiv, Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §BOOL b & c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolAnd, Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := §BOOL b | c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolOr, Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := §BOOL b ^ c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolXOr, Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := b, c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pair, Span((1, 1), (1, 9)), "a", "b", "c")),
			("a := §1ST b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.First, Span((1, 1), (1, 11)), "a", "b")),
			("a := §2ND b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Second, Span((1, 1), (1, 11)), "a", "b")),
			("a := +#b c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ApplyPrefix, Span((1, 1), (1, 10)), "a", "b", "c")),
			("a := -#b c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.RemovePrefix, Span((1, 1), (1, 10)), "a", "b", "c")),
			("a := {b} + c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ExtendRec, Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := {b} /", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.DivideRec, Span((1, 1), (1, 10)), "a", "b")),
			("a := .b c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.CallFunc, Span((1, 1), (1, 9)), "a", "b", "c")),
			("a := §OBJ:b c", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.CallProc, Span((1, 1), (1, 13)), "a", "b", "c")),
			("a := §VAR b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarDef, Span((1, 1), (1, 11)), "a", "b")),
			("a := §VAR b ->", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarGet, Span((1, 1), (1, 14)), "a", "b")),
			
			("a := [b, c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePair, Span((1, 1), (1, 11)), "a", "b", "c")),
			("a := [#b c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePrefix, Span((1, 1), (1, 11)), "a", "b", "c")),
			("a := [{b} + c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeRecord, Span((1, 1), (1, 14)), "a", "b", "c")),
			("a := [b => c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeFunc, Span((1, 1), (1, 13)), "a", "b", "c")),
			("a := [b : c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeMethod, Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := [b | c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeSet, Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := [b & c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeCond, Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := [§VAR b]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeVar, Span((1, 1), (1, 13)), "a", "b")),
			("a := [§REC b => c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeRecursive, Span((1, 1), (1, 18)), "a", "b", "c")),
			("a := [§ANY b => c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeInterface, Span((1, 1), (1, 18)), "a", "b", "c")),
			("a := [§ALL b => c]", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeGeneric, Span((1, 1), (1, 18)), "a", "b", "c")),
			
			("§VAR a <- b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarSet, Span((1, 1), (1, 11)), "a", "b")),
			("§RETURN a IF b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIf, Span((1, 1), (1, 14)), "b", "a")),
			("§RETURN a IF_NOT_EMPTY", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIfNotEmpty, Span((1, 1), (1, 22)), "EMPTY", "a")),
			("a := §TRY b AS_BOOL", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsBool, Span((1, 1), (1, 19)), "a", "b")),
			("a := §TRY b AS_INT", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsInt, Span((1, 1), (1, 18)), "a", "b")),
			("a := §TRY b AS_TYPE", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsType, Span((1, 1), (1, 19)), "a", "b")),
			("a := §TRY b AS_PAIR", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsPair, Span((1, 1), (1, 19)), "a", "b")),
			("a := §TRY b AS_RECORD", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsRecord, Span((1, 1), (1, 21)), "a", "b")),
			("a := §TRY b AS_VAR", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsVar, Span((1, 1), (1, 18)), "a", "b")),
			("a := §TRY b AS_REF", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryAsRef, Span((1, 1), (1, 18)), "a", "b")),
			("a := §TRY_REMOVE #c FROM b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TryRemovePrefixFrom, Span((1, 1), (1, 26)), "a", "b", "c")),
			("§ASSERT a => b", mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Assert, Span((1, 1), (1, 14)), "a", "b")),
		}.AsStream(
		).Map(
			aTestCase => mStream.Stream(
 				mTest.Test(
					$"{nameof(mTextParser.ParseText)} {aTestCase.Command.NodeType}: {aTestCase.Expr}",
					aStreamOut => {
						mAssert.AreEquals(
							mIL_Parser.Command.ParseText(aTestCase.Expr, "", _ => aStreamOut(_())),
							aTestCase.Command
						);
					}
				),
				mTest.Test(
					$"{nameof(mIL_AST.ToText)} {aTestCase.Command.NodeType}: {aTestCase.Expr}",
					aStreamOut => {
						mAssert.AreEquals(
							aTestCase.Command.ToText(),
							aTestCase.Expr
						);
					}
				)
			)
		).Flatt(
		).ToArrayList(
		).ToArray(
		)
	);
}
