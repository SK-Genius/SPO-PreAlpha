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

using tIL_Parser = mParserGen.tParser<mTextParser.tPosChar, mTextParser.tError>;

public static class  mIL_Parser {
	public static readonly mStd.tFunc<tIL_Parser, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<tIL_Parser, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<tIL_Parser, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<tIL_Parser, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<tIL_Parser, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<tIL_Parser, tText> Text = mTextParser.GetToken;
	
	public static readonly tIL_Parser _ = CharIn(" \t");
	public static readonly tIL_Parser __ = _[0, null];
	public static readonly tIL_Parser NL = Char('\n');
	
	public static readonly mStd.tFunc<tIL_Parser, tText> Token = a => Text(a) + -__;
	
	public static readonly tIL_Parser
	QuotedString = (-Char('"') +NotChar('"')[0, null] -Char('"'))
	.ModifyList(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar))
	.SetName(nameof(QuotedString));
	
	public static readonly tIL_Parser
	Digit = CharInRange('0', '9')
	.Modify((tChar aChar) => (int)aChar - (int)'0');
	
	public static readonly tIL_Parser
	Nat = (Digit + (Digit | -Char('_'))[0, null])
	.ModifyList(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit));
	
	public static readonly tIL_Parser
	PosSignum = Char('+')
	.Modify(() => +1);
	
	public static readonly tIL_Parser
	NegSignum = Char('-')
	.Modify(() => -1);
	
	public static readonly tIL_Parser
	Signum = PosSignum | NegSignum;
	
	public static readonly tIL_Parser
	Int = (Signum + Nat)
	.Modify((int Sig, int Abs) => Sig * Abs);
	
	public static readonly tIL_Parser
	Number = (Int | Nat)
	.SetName(nameof(Number));
	
	public static readonly tIL_Parser
	NumberText = Number
	.Modify((tInt32 a) => a.ToString());
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly tIL_Parser
	Ident = (
		(
			CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) |
			Text("...")
		)[1, null] -__
	)
	.ModifyList(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
	.SetName(nameof(Ident));
	
	public static readonly tIL_Parser
	Literal = (Number | QuotedString) -__;
	
	public static readonly tIL_Parser
	Command = (
		(+Ident -Token(":=") +NumberText)
		.Modify(mIL_AST.CreateInt) |
		
		(+Ident -Token(":=") -Token("§BOOL") +Ident -Token("&") +Ident.OrFail())
		.Modify(mIL_AST.And) |
		
		(+Ident -Token(":=") -Token("§BOOL") +Ident -Token("|") +Ident.OrFail())
		.Modify(mIL_AST.Or) |
		
		(+Ident -Token(":=") -Token("§BOOL") +Ident -Token("^") +Ident.OrFail())
		.Modify(mIL_AST.XOr) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("==") +Ident.OrFail())
		.Modify(mIL_AST.IntsAreEq) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("<=>") +Ident.OrFail())
		.Modify(mIL_AST.IntsComp) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("+") +Ident.OrFail())
		.Modify(mIL_AST.IntsAdd) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("-") +Ident.OrFail())
		.Modify(mIL_AST.IntsSub) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("*") +Ident.OrFail())
		.Modify(mIL_AST.IntsMul) |
		
		(+Ident -Token(":=") +Ident -Token(",") +Ident.OrFail())
		.Modify(mIL_AST.CreatePair) |
		
		(+Ident -Token(":=") -Token("§1ST") +Ident.OrFail())
		.Modify(mIL_AST.GetFirst) |
		
		(+Ident -Token(":=") -Token("§2ND") +Ident.OrFail())
		.Modify(mIL_AST.GetSecond) |
		
		(+Ident -Token(":=") -Token("+") +(+Ident +Ident).OrFail())
		.Modify(mIL_AST.AddPrefix) |
		
		(+Ident -Token(":=") -Token("-") +(+Ident +Ident).OrFail())
		.Modify(mIL_AST.SubPrefix) |
		
		(+Ident -Token(":=") -Token("?") +(+Ident +Ident).OrFail())
		.Modify(mIL_AST.HasPrefix) |
		
		(+Ident -Token(":=") -Token(".") +Ident +Ident)
		.Modify(mIL_AST.Call) |
		
		(+Ident -Token(":=") -Token("§OBJ") -Token(":") +(+Ident +Ident).OrFail())
		.Modify(mIL_AST.Exec) |
		
		(-Token("§PUSH") +Ident.OrFail())
		.Modify(mIL_AST.Push) |
		
		(-Token("§POP"))
		.Modify(mIL_AST.Pop) |
		
		(-Token("§VAR") +(+Ident -Token("<-") +Ident).OrFail())
		.Modify(mIL_AST.VarSet) |
		
		(+Ident -Token(":=") -Token("§VAR") +Ident -Token("->"))
		.Modify(mIL_AST.VarGet) |
		
		(+Ident -Token(":=") -Token("§VAR") +Ident.OrFail())
		.Modify(mIL_AST.VarDef) |
		
		(-Token("§RETURN") +(+Ident -Token("IF") +Ident).OrFail())
		.Modify(mIL_AST.ReturnIf) |
		
		(-Token("§REPEAT") +(Ident -Token("IF") +Ident).OrFail())
		.Modify(mIL_AST.RepeatIf) |
		
		(-Token("§ASSERT") +(+Ident -Token("=>") +Ident).OrFail())
		.Modify(mIL_AST.Assert) |
		
		(+Ident -Token("=>") +(+Ident -Token(":") +Ident).OrFail())
		.Modify(mIL_AST.Proof) |
		
		(+Ident -Token(":=") +Ident)
		.Modify(mIL_AST.Alias) |
		
		(+Ident -Token(":=") -Token("[") +Ident -Token("&") +(+Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypeCond) |
		
		(+Ident -Token(":=") -Token("[") +Ident -Token("=>") +(+Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypeFunc) |
		
		(+Ident -Token(":=") -Token("[") +Ident -Token(":") +(+Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypeMethod) |
		
		(+Ident -Token(":=") -Token("[") +Ident -Token(",") +(+Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypePair) |
		
		(+Ident -Token(":=") -Token("[") -Token("#") +(+Ident +Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypePrefix) |
		
		(+Ident -Token(":=") -Token("[") +Ident -Token("|") +(+Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypeSet) |
		
		(+Ident -Token(":=") -Token("[") -Token("§VAR") +(+Ident -Token("]")).OrFail())
		.Modify(mIL_AST.TypeVar) |
		
		(-Token("§TYPE_OF") +(+Ident -Token("IS") +Ident).OrFail())
		.Modify(mIL_AST.TypeIs)
	)
	.SetName(nameof(Command));
	
	public static readonly tIL_Parser
	Block = (-_ +Command -NL)[1, null]
	.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mIL_AST.tCommandNode>)))
	.SetName(nameof(Block));
	
	public static readonly tIL_Parser
	Def = (-Text("DEF") -__ +Ident -NL +Block)
	.Modify((tText aID, mList.tList<mIL_AST.tCommandNode> aBlock) => (aID, aBlock))
	.SetName(nameof(Def));
	
	public static readonly tIL_Parser
	Module = Def[1, null]
	.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<(tText, mList.tList<mIL_AST.tCommandNode>)>)))
	.SetName(nameof(Module));
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_Parser),
		mTest.Test(
			"SubParser",
			aDebugStream => {
				mStd.AssertEq(Int.ParseText("+1_234", aDebugStream), mParserGen.ResultList(1234));
				mStd.AssertEq(QuotedString.ParseText("\"BLA\"", aDebugStream), mParserGen.ResultList("BLA"));
				mStd.AssertEq(Ident.ParseText("BLA...", aDebugStream), mParserGen.ResultList("BLA..."));
			}
		),
		mTest.Test(
			"Commands",
			aDebugStream => {
				//================================================================================
				void AssertParsedCommand(
					string aText,
					mIL_AST.tCommandNode aCommandNode,
					mStd.tAction<string> aDebugStream_
				//================================================================================
				) => mStd.AssertEq(
					Command.ParseText(aText, aDebugStream_),
					mParserGen.ResultList(aCommandNode)
				);
				
				AssertParsedCommand(
					"a := b, c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pair, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b == c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAreEq, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b <=> c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsComp, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b + c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAdd, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b - c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsSub, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §INT b * c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsMul, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §BOOL b & c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolAnd, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §BOOL b | c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolOr, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §BOOL b ^ c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.BoolXOr, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §1ST b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.First, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §2ND b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Second, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := +b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.AddPrefix, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := -b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.SubPrefix, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := ?b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.HasPrefix, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := .b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Call, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"§RETURN a IF b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIf, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§REPEAT a IF b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.RepeatIf, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§ASSERT a => b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Assert, "a", "b"),
					aDebugStream
				);
				
				AssertParsedCommand(
					"§PUSH a",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Push, "a"),
					aDebugStream
				);
				AssertParsedCommand(
					"§POP",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pop),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §VAR b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarDef, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§VAR a <- b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarSet, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §VAR b ->",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.VarGet, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := §OBJ:b c",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Exec, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b & c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeCond, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b => c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeFunc, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b : c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeMethod, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b, c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePair, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [#b c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypePrefix, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [b | c]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeSet, "a", "b", "c"),
					aDebugStream
				);
				AssertParsedCommand(
					"a := [§VAR b]",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeVar, "a", "b"),
					aDebugStream
				);
				AssertParsedCommand(
					"§TYPE_OF a IS b",
					mIL_AST.CommandNode(mIL_AST.tCommandNodeType.TypeIs, "a", "b"),
					aDebugStream
				);
			}
		)
	);
	
	#endregion
}
