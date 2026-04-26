// IMPORT Common/mStd
// IMPORT Common/mSpan
// IMPORT Common/mTest
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT Common/mAssert
// IMPORT Common/mTextParser
// IMPORT Common/mArrayList
// IMPORT Common/mParserGen
// IMPORT mIL_AST
// IMPORT mIL_Parser
// IMPORT mTokenizer


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
			("a := [b, c]", mIL_AST.TypePair(Span((1, 1), (1, 11)), "a", "b", "c")),
			("a := [#b c]", mIL_AST.TypePrefix(Span((1, 1), (1, 11)), "a", "b", "c")),
			("a := [{b} + c]", mIL_AST.TypeRecord(Span((1, 1), (1, 14)), "a", "b", "c")),
			("a := [b => c]", mIL_AST.TypeFunc(Span((1, 1), (1, 13)), "a", "b", "c")),
			("a := [b : c]", mIL_AST.TypeMethod(Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := [b | c]", mIL_AST.TypeSet(Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := [b & c]", mIL_AST.TypeCond(Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := [§VAR b]", mIL_AST.TypeVar(Span((1, 1), (1, 13)), "a", "b")),
			("a := [§REC b => c]", mIL_AST.TypeRecursive(Span((1, 1), (1, 18)), "a", "b", "c")),
			("a := [§ANY b => c]", mIL_AST.TypeInterface(Span((1, 1), (1, 18)), "a", "b", "c")),
			("a := [§ALL b => c]", mIL_AST.TypeGeneric(Span((1, 1), (1, 18)), "a", "b", "c")),
			("a := [.b c]", mIL_AST.TypeGenericApply(Span((1, 1), (1, 11)), "a", "b", "c")),
			
			("a := b", mIL_AST.Alias(Span((1, 1), (1, 6)), "a", "b")),
			("a := 1", mIL_AST.CreateInt(Span((1, 1), (1, 6)), "a", "1")),
			("a := §INT b == c", mIL_AST.IntsAreEq(Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := §INT b <=> c", mIL_AST.IntsComp(Span((1, 1), (1, 17)), "a", "b", "c")),
			("a := §INT b + c", mIL_AST.IntsAdd(Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §INT b - c", mIL_AST.IntsSub(Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §INT b * c", mIL_AST.IntsMul(Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §INT b / c", mIL_AST.IntsDiv(Span((1, 1), (1, 15)), "a", "b", "c")),
			("a := §BOOL b & c", mIL_AST.And(Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := §BOOL b | c", mIL_AST.Or(Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := §BOOL b ^ c", mIL_AST.XOr(Span((1, 1), (1, 16)), "a", "b", "c")),
			("a := b, c", mIL_AST.CreatePair(Span((1, 1), (1, 9)), "a", "b", "c")),
			("a := §1ST b", mIL_AST.GetFirst(Span((1, 1), (1, 11)), "a", "b")),
			("a := §2ND b", mIL_AST.GetSecond(Span((1, 1), (1, 11)), "a", "b")),
			("a := +#b c", mIL_AST.AddPrefix(Span((1, 1), (1, 10)), "a", "b", "c")),
			("a := -#b c", mIL_AST.SubPrefix(Span((1, 1), (1, 10)), "a", "b", "c")),
			("a := {b} + c", mIL_AST.AddField(Span((1, 1), (1, 12)), "a", "b", "c")),
			("a := {b} #c", mIL_AST.GetField(Span((1, 1), (1, 11)), "a", "b", "c")),
			("a := .b c", mIL_AST.CallFunc(Span((1, 1), (1, 9)), "a", "b", "c")),
			("a := §OBJ:b c", mIL_AST.CallProc(Span((1, 1), (1, 13)), "a", "b", "c")),
			("a := §VAR b", mIL_AST.VarDef(Span((1, 1), (1, 11)), "a", "b")),
			("a := §VAR b ->", mIL_AST.VarGet(Span((1, 1), (1, 14)), "a", "b")),
			("§REC a := .b c", mIL_AST.DefRecProcs(Span((1, 1), (1, 14)), "a", "b", "c")),
			
			("§VAR a <- b", mIL_AST.VarSet(Span((1, 1), (1, 11)), "a", "b")),
			("§RETURN a IF b", mIL_AST.ReturnIf(Span((1, 1), (1, 14)), "b", "a")),
			("§RETURN a IF_NOT_EMPTY", mIL_AST.ReturnIfNotEmpty(Span((1, 1), (1, 22)), "a")),
			("a := §TRY b AS_BOOL", mIL_AST.TryAsBool(Span((1, 1), (1, 19)), "a", "b")),
			("a := §TRY b AS_INT", mIL_AST.TryAsInt(Span((1, 1), (1, 18)), "a", "b")),
			("a := §TRY b AS_TYPE", mIL_AST.TryAsType(Span((1, 1), (1, 19)), "a", "b")),
			("a := §TRY b AS_PAIR", mIL_AST.TryAsPair(Span((1, 1), (1, 19)), "a", "b")),
			("a := §TRY b AS_RECORD", mIL_AST.TryAsRecord(Span((1, 1), (1, 21)), "a", "b")),
			("a := §TRY b AS_VAR", mIL_AST.TryAsVar(Span((1, 1), (1, 18)), "a", "b")),
			("a := §TRY b AS_REF", mIL_AST.TryAsRef(Span((1, 1), (1, 18)), "a", "b")),
			("a := §TRY_REMOVE #c FROM b", mIL_AST.TryRemovePrefixFrom(Span((1, 1), (1, 26)), "a", "b", "c")),
			("§ASSERT a => b", mIL_AST.Assert(Span((1, 1), (1, 14)), "a", "b")),
		}.AsStream(
		).Map(
			aTestCase => mStream.Stream(
				mTest.Test($"{nameof(mTextParser.ParseText)} {aTestCase.Command.NodeType}: {aTestCase.Expr}",
					aStreamOut => {
						mAssert.AreEquals(
							mIL_Parser.Command.ParseText(aTestCase.Expr, "", __ => aStreamOut(__())),
							aTestCase.Command,
							mIL_AST.Eq_(mSpan.Eq_<tPos>(mTextStream.Eq))
						);
					}
				),
				mTest.Test($"{nameof(mIL_AST.ToText)} {aTestCase.Command.NodeType}: {aTestCase.Expr}",
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
