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

using tResults = mList.tList<mStd.tAny>;

using tIL_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

public static class  mIL_Parser {
	public static mStd.tFunc<tIL_Parser, tChar> Char = mTextParser.GetChar;
	public static mStd.tFunc<tIL_Parser, tChar> NotChar = mTextParser.GetNotChar;
	public static mStd.tFunc<tIL_Parser, tText> CharIn = mTextParser.GetCharIn;
	public static mStd.tFunc<tIL_Parser, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static mStd.tFunc<tIL_Parser, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static mStd.tFunc<tIL_Parser, tText> Token = mTextParser.GetToken;
	
	public static tIL_Parser _ = CharIn(" \t");
	public static tIL_Parser __ = _[0, null];
	public static tIL_Parser NL = Char('\n');
	
	public static mStd.tFunc<tIL_Parser, tText> TOKEN = a => Token(a) + -__;
	
	public static tIL_Parser _STRING_ = (-Char('"') +NotChar('"')[0, null] -Char('"'))
		.Modify_(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar)); 
	
	public static tIL_Parser _DIGIT_ = CharInRange('0', '9')
		.Modify((tChar aChar) => (int)aChar - (int)'0');
	
	public static tIL_Parser _NAT_ = (_DIGIT_ + (_DIGIT_ | -Char('_'))[0, null])
		.Modify_(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit));
	
	public static tIL_Parser _POSITIV_ = Char('+')
		.Modify(() => +1);
	
	public static tIL_Parser _NEGATIV_ = Char('-')
		.Modify(() => -1);
	
	public static tIL_Parser _SIGNUM_ = _POSITIV_ | _NEGATIV_;
	
 	public static tIL_Parser _INT_ = (_SIGNUM_ + _NAT_)
		.Modify((int Sig, int Abs) => Sig * Abs);
	
	public static tIL_Parser _NUM_ = _INT_ | _NAT_;
	
	public static tIL_Parser NUM = _NUM_.Modify((tInt32 a) => a.ToString());
	
	public static tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static tIL_Parser IDENT = ( ( CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) | Token("...") )[1, null] -__ )
		.Modify_(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2));
	
	public static tIL_Parser LITERAL = (_NUM_ | _STRING_) -__;
	
	public static tIL_Parser COMMAND = (
		(+IDENT -TOKEN(":=") +NUM)                         .Modify(mIL_AST.CreateInt) |
		(+IDENT -TOKEN(":=") +IDENT -TOKEN(",") +IDENT)    .Modify(mIL_AST.CreatePair) |
		(+IDENT -TOKEN(":=") -TOKEN("§1st") +IDENT)        .Modify(mIL_AST.GetFirst) |
		(+IDENT -TOKEN(":=") -TOKEN("§2nd") +IDENT)        .Modify(mIL_AST.GetSecond) |
		(+IDENT -TOKEN(":=") -TOKEN("+") +IDENT +IDENT)    .Modify(mIL_AST.AddPrefix) |
		(+IDENT -TOKEN(":=") -TOKEN("-") +IDENT +IDENT)    .Modify(mIL_AST.SubPrefix) |
		(+IDENT -TOKEN(":=") +IDENT +IDENT)                .Modify(mIL_AST.Call) |
		(+IDENT -TOKEN(":=") +IDENT)                       .Modify(mIL_AST.Alias) |
		(-TOKEN("§PUSH") +IDENT)                           .Modify(mIL_AST.Push) |
		(-TOKEN("§POP"))                                   .Modify(mIL_AST.Pop) |
		(-TOKEN("§RETURN") +IDENT -TOKEN("IF") +IDENT)     .Modify(mIL_AST.ReturnIf) |
		(-TOKEN("§REPEAT") +IDENT -TOKEN("IF") +IDENT)     .Modify(mIL_AST.RepeatIf) |
		(-TOKEN("§ASSERT") +IDENT -TOKEN("=>") +IDENT)     .Modify(mIL_AST.Assert) |
		(+IDENT -TOKEN("=>") +IDENT -TOKEN(":") +IDENT)    .Modify(mIL_AST.Proof)
	);
	
	public static tIL_Parser BLOCK = (-_ +COMMAND -NL)[1, null]
		.Modify_(a => mParserGen.ResultList(a.Map(mStd.To<mIL_AST.tCommandNode>)));
	
	public static tIL_Parser DEF = (-Token("DEF") -__ +IDENT -NL +BLOCK)
		.Modify((tText aID, mList.tList<mIL_AST.tCommandNode> aBlock) => mStd.Tuple(aID, aBlock));
	
	public static tIL_Parser MODULE = DEF[1, null]
		.Modify_(a => mParserGen.ResultList(a.Map(mStd.To<mStd.tTuple<tText, mList.tList<mIL_AST.tCommandNode>>>)));
	
	#region TEST
	
	private static mParserGen.tResultList Parse(
		this tIL_Parser aParser,
		tText aText
	) {
		mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> List;
		mTextParser.tFailInfo Info;
		mStd.tTuple<mParserGen.tResultList, mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>>> Result;
		mParserGen.tResultList ResultList;
		mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> Rest;
		
		var Text1 = mTextParser.TextStream(mTextParser.TextToStream(aText));
		mStd.Assert(Text1.MATCH(out List, out Info));
		var MaybeResult1 = aParser.Parse(List);
		mStd.Assert(MaybeResult1.MATCH(out Result), "("+Info._Line+", "+Info._Coll+"): "+Info._ErrorMessage);
		mStd.Assert(Result.MATCH(out ResultList, out Rest));
		return ResultList;
	}
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"SubParser",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					mStd.AssertEq(_INT_.Parse("+1_234..."), mParserGen.ResultList(1234));
					mStd.AssertEq(_STRING_.Parse("\"BLA\"..."), mParserGen.ResultList("BLA"));
					mStd.AssertEq(IDENT.Parse("BLA... ..."), mParserGen.ResultList("BLA..."));
					return true;
				}
			)
		),
		mStd.Tuple(
			"Commands",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					mStd.AssertEq(COMMAND.Parse("a := b, c ..."),      mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pair,      "a", "b", "c")));
					mStd.AssertEq(COMMAND.Parse("a := §1st b ..."),    mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.First,     "a", "b")));
					mStd.AssertEq(COMMAND.Parse("a := §2nd b ..."),    mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Second,    "a", "b")));
					mStd.AssertEq(COMMAND.Parse("a := +b c ..."),      mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.AddPrefix, "a", "b", "c")));
					mStd.AssertEq(COMMAND.Parse("a := -b c ..."),      mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.SubPrefix, "a", "b", "c")));
					mStd.AssertEq(COMMAND.Parse("§PUSH a ..."),        mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Push,      "a")));
					mStd.AssertEq(COMMAND.Parse("§POP ..."),           mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Pop)));
					mStd.AssertEq(COMMAND.Parse("a := b c ..."),       mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Call,      "a", "b", "c")));
					mStd.AssertEq(COMMAND.Parse("§RETURN a IF b ..."), mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.ReturnIf,  "a", "b")));
					mStd.AssertEq(COMMAND.Parse("§REPEAT a IF b ..."), mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.RepeatIf,  "a", "b")));
					mStd.AssertEq(COMMAND.Parse("§ASSERT a => b ..."), mParserGen.ResultList(mIL_AST.CommandNode(mIL_AST.tCommandNodeType.Assert,    "a", "b")));
					return true;
				}
			)
		)
	);
	
	#endregion
	
}
