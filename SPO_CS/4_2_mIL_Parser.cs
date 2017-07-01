﻿using tBool = System.Boolean;

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

using tResults = mList.tList<mStd.tAny>;

using tIL_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

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
	
	public static readonly tIL_Parser QuotedString = (-Char('"') +NotChar('"')[0, null] -Char('"'))
		.ModifyList(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar)); 
	
	public static readonly tIL_Parser Digit = CharInRange('0', '9')
		.Modify((tChar aChar) => (int)aChar - (int)'0');
	
	public static readonly tIL_Parser Nat = (Digit + (Digit | -Char('_'))[0, null])
		.ModifyList(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit));
	
	public static readonly tIL_Parser PosSignum = Char('+')
		.Modify(() => +1);
	
	public static readonly tIL_Parser NegSignum = Char('-')
		.Modify(() => -1);
	
	public static readonly tIL_Parser Signum = PosSignum | NegSignum;
	
 	public static readonly tIL_Parser Int = (Signum + Nat)
		.Modify((int Sig, int Abs) => Sig * Abs);
	
	public static readonly tIL_Parser Number = Int | Nat;
	
	public static readonly tIL_Parser NumberText = Number
		.Modify((tInt32 a) => a.ToString());
	
	public static tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly tIL_Parser Ident = (
		(
			CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) |
			Text("...")
		)[1, null] -__
	).ModifyList(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2));
	
	public static readonly tIL_Parser Literal = (Number | QuotedString) -__;
	
	public static readonly tIL_Parser Command = (
		(+Ident -Token(":=") +NumberText)
			.Modify(mIL_AST.CreateInt) |
		
		(+Ident -Token(":=") -Token("§BOOL") +Ident -Token("&") +Ident)
			.Modify(mIL_AST.And) |
		
		(+Ident -Token(":=") -Token("§BOOL") +Ident -Token("|") +Ident)
			.Modify(mIL_AST.Or) |
		
		(+Ident -Token(":=") -Token("§BOOL") +Ident -Token("^") +Ident)
			.Modify(mIL_AST.XOr) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("==") +Ident)
			.Modify(mIL_AST.IntsAreEq) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("<=>") +Ident)
			.Modify(mIL_AST.IntsComp) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("+") +Ident)
			.Modify(mIL_AST.IntsAdd) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("-") +Ident)
			.Modify(mIL_AST.IntsSub) |
		
		(+Ident -Token(":=") -Token("§INT") +Ident -Token("*") +Ident)
			.Modify(mIL_AST.IntsMul) |
		
		(+Ident -Token(":=") +Ident -Token(",") +Ident)
			.Modify(mIL_AST.CreatePair) |
		
		(+Ident -Token(":=") -Token("§1ST") +Ident)
			.Modify(mIL_AST.GetFirst) |
		
		(+Ident -Token(":=") -Token("§2ND") +Ident)
			.Modify(mIL_AST.GetSecond) |
		
		(+Ident -Token(":=") -Token("+") +Ident +Ident)
			.Modify(mIL_AST.AddPrefix) |
		
		(+Ident -Token(":=") -Token("-") +Ident +Ident)
			.Modify(mIL_AST.SubPrefix) |
		
		(+Ident -Token(":=") -Token("?") +Ident +Ident)
			.Modify(mIL_AST.HasPrefix) |
		
		(+Ident -Token(":=") +Ident +Ident)
			.Modify(mIL_AST.Call) |
		
		(+Ident -Token(":=") -Token("§OBJ") +Ident +Ident)
			.Modify(mIL_AST.Exec) |
		
		(+Ident -Token(":=") +Ident)
			.Modify(mIL_AST.Alias) |
		
		(-Token("§PUSH") +Ident)
			.Modify(mIL_AST.Push) |
		
		(-Token("§POP"))
			.Modify(mIL_AST.Pop) |
		
		(-Token("§RETURN") +Ident -Token("IF") +Ident)
			.Modify(mIL_AST.ReturnIf) |
		
		(-Token("§REPEAT") +Ident -Token("IF") +Ident)
			.Modify(mIL_AST.RepeatIf) |
		
		(-Token("§ASSERT") +Ident -Token("=>") +Ident)
			.Modify(mIL_AST.Assert) |
		
		(+Ident -Token("=>") +Ident -Token(":") +Ident)
			.Modify(mIL_AST.Proof)
	);
	
	public static readonly tIL_Parser Block = (-_ +Command -NL)[1, null]
		.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mIL_AST.tCommandNode>)));
	
	public static readonly tIL_Parser Def = (-Text("DEF") -__ +Ident -NL +Block)
		.Modify((tText aID, mList.tList<mIL_AST.tCommandNode> aBlock) => mStd.Tuple(aID, aBlock));
	
	public static readonly tIL_Parser Module = Def[1, null]
		.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mStd.tTuple<tText, mList.tList<mIL_AST.tCommandNode>>>)));
	
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
				mStd.AssertEq(Command.ParseText("a := b, c", aDebugStream),         mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pair,      "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §INT b == c", aDebugStream),  mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAreEq, "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §INT b <=> c", aDebugStream), mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsComp,  "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §INT b + c", aDebugStream),   mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsAdd,   "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §INT b - c", aDebugStream),   mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsSub,   "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §INT b * c", aDebugStream),   mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.IntsMul,   "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §BOOL b & c", aDebugStream),  mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.And,       "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §BOOL b | c", aDebugStream),  mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Or,        "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §BOOL b ^ c", aDebugStream),  mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.XOr,       "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := §1ST b", aDebugStream),       mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.First,     "a", "b")));
				mStd.AssertEq(Command.ParseText("a := §2ND b", aDebugStream),       mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Second,    "a", "b")));
				mStd.AssertEq(Command.ParseText("a := +b c", aDebugStream),         mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.AddPrefix, "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := -b c", aDebugStream),         mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.SubPrefix, "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := ?b c", aDebugStream),         mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.HasPrefix, "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("a := b c", aDebugStream),          mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Call,      "a", "b", "c")));
				mStd.AssertEq(Command.ParseText("§RETURN a IF b", aDebugStream),    mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIf,  "a", "b")));
				mStd.AssertEq(Command.ParseText("§REPEAT a IF b", aDebugStream),    mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.RepeatIf,  "a", "b")));
				mStd.AssertEq(Command.ParseText("§ASSERT a => b", aDebugStream),    mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Assert,    "a", "b")));
					
				mStd.AssertEq(Command.ParseText("§PUSH a", aDebugStream),           mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Push,      "a")));
				mStd.AssertEq(Command.ParseText("§POP", aDebugStream),              mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pop)));
				mStd.AssertEq(Command.ParseText("a := §OBJ b c", aDebugStream),     mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Exec,      "a", "b", "c")));
			}
		)
	);
	
	#endregion
}
