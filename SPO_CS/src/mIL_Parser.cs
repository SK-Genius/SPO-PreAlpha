//IMPORT mTokenizer.cs
//IMPORT mIL_AST.cs

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

using tToken = mTokenizer.tToken;

using tPos = mTextStream.tPos;
using tSpan = mStd.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class
mIL_Parser {
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL = mTokenizer.NL_Token;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	QuotedString  = mTokenizer.TextToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Number = mTokenizer.NumberToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Ident = mTokenizer.IdentToken;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	SpecialToken = mTokenizer.SpecialToken;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tChar>
	SpecialIdent = mTokenizer.SpecialIdent;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	Token = mTokenizer.Token_;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	KeyWord = mTokenizer.KeyWord;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Prefix = SpecialIdent('#').Modify(
		_ => new mTokenizer.tToken{
			Span = _.Span,
			Type = _.Type,
			Text = _.Text.Substring(1)
		}
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mIL_AST.tCommandNode<tSpan>, tError>
	Command = (
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Number)
		.Modify((aIdent, _, aNumber) => (aIdent, aNumber))
		.ModifyS(mTokenizer.X(mIL_AST.CreateInt))
		.SetDebugName(nameof(mIL_AST.CreateInt)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_BOOL"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsBool))
		.SetDebugName(nameof(mIL_AST.IsBool)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Ident, -Token("&") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.And))
		.SetDebugName(nameof(mIL_AST.And)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Ident, -Token("|") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Or))
		.SetDebugName(nameof(mIL_AST.Or)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Ident, -Token("^") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.XOr))
		.SetDebugName(nameof(mIL_AST.XOr)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_INT"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsInt))
		.SetDebugName(nameof(mIL_AST.IsInt)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("==") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsAreEq))
		.SetDebugName(nameof(mIL_AST.IntsAreEq)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("<=>") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsComp))
		.SetDebugName(nameof(mIL_AST.IntsComp)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("+") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsAdd))
		.SetDebugName(nameof(mIL_AST.IntsAdd)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("-") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsSub))
		.SetDebugName(nameof(mIL_AST.IntsSub)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("*") +Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsMul))
		.SetDebugName(nameof(mIL_AST.IntsMul)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_PAIR"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsPair))
		.SetDebugName(nameof(mIL_AST.IsPair)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Ident, -SpecialToken(",") +Ident)
		.Modify((a1, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CreatePair))
		.SetDebugName(nameof(mIL_AST.CreatePair)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -KeyWord("1ST") +Ident)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.GetFirst))
		.SetDebugName(nameof(mIL_AST.GetFirst)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -KeyWord("2ND") +Ident)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.GetSecond))
		.SetDebugName(nameof(mIL_AST.GetSecond)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_PREFIX"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsPrefix))
		.SetDebugName(nameof(mIL_AST.IsPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Token("+"), Prefix, Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.AddPrefix))
		.SetDebugName(nameof(mIL_AST.AddPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Token("-"), Prefix, Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.SubPrefix))
		.SetDebugName(nameof(mIL_AST.SubPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Token("?"), Prefix, Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.HasPrefix))
		.SetDebugName(nameof(mIL_AST.HasPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_RECORD"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsRecord))
		.SetDebugName(nameof(mIL_AST.IsRecord)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -SpecialToken("{"), Ident, -SpecialToken("}") -Token("+"), Ident)
		.Modify((a1, _, a2, __, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.ExtendRec))
		.SetDebugName(nameof(mIL_AST.ExtendRec)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -SpecialToken("{"), Ident, (-SpecialToken("}") -Token("/")))
		.Modify((a1, _, a2, __) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.DivideRec))
		.SetDebugName(nameof(mIL_AST.DivideRec)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("."), Ident, Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CallFunc))
		.SetDebugName(nameof(mIL_AST.CallFunc)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -KeyWord("OBJ") -SpecialToken(":"), Ident, Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CallProc))
		.SetDebugName(nameof(mIL_AST.CallProc)) |
		
		mParserGen.Seq(-KeyWord("VAR"), Ident, Token("<-"), Ident)
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarSet))
		.SetDebugName(nameof(mIL_AST.VarSet)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("VAR"), Ident, Token("->"))
		.Modify((a1, _, __, a2, ___) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarGet))
		.SetDebugName(nameof(mIL_AST.VarGet)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_VAR"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsVar))
		.SetDebugName(nameof(mIL_AST.IsVar)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("VAR"), Ident)
		.Modify((a1, _, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarDef))
		.SetDebugName(nameof(mIL_AST.VarDef)) |
		
		mParserGen.Seq(KeyWord("RETURN"), Ident, Token("IF"), Ident)
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.ReturnIf))
		.SetDebugName(nameof(mIL_AST.ReturnIf)) |
		
		mParserGen.Seq(KeyWord("REPEAT"), Ident, Token("IF"), Ident)
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.RepeatIf))
		.SetDebugName(nameof(mIL_AST.RepeatIf)) |
		
		mParserGen.Seq(KeyWord("ASSERT"), Ident, SpecialToken("=>"), Ident)
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Assert))
		.SetDebugName(nameof(mIL_AST.Assert)) |
		
		mParserGen.Seq(Ident, SpecialToken("=>"), Ident, SpecialToken(":"), Ident)
		.Modify((a1, _, a2, __, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Proof))
		.SetDebugName(nameof(mIL_AST.Proof)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Ident)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Alias))
		.SetDebugName(nameof(mIL_AST.Alias)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -KeyWord("IS_TYPE"), Ident)
		.Modify((aIdent1, _, aIdent2) => (aIdent1, aIdent2))
		.ModifyS(mTokenizer.X(mIL_AST.IsType))
		.SetDebugName(nameof(mIL_AST.IsType)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -Token("&") +(Ident +-SpecialToken("]")))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeCond))
		.SetDebugName(nameof(mIL_AST.TypeCond)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeFunc))
		.SetDebugName(nameof(mIL_AST.TypeFunc)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -SpecialToken(":") +(Ident +-SpecialToken("]")))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeMethod))
		.SetDebugName(nameof(mIL_AST.TypeMethod)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -SpecialToken(",") +(Ident +-SpecialToken("]")))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypePair))
		.SetDebugName(nameof(mIL_AST.TypePair)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Prefix, (Ident +-SpecialToken("]")))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypePrefix))
		.SetDebugName(nameof(mIL_AST.TypePrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("=") -SpecialToken("[") -SpecialToken("{"), Ident +-SpecialToken("}"), -Token("+") +Ident +-SpecialToken("]"))
		.Modify((a1, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeRecord))
		.SetDebugName(nameof(mIL_AST.TypeRecord)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -Token("|") +Ident +-SpecialToken("]"))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeSet))
		.SetDebugName(nameof(mIL_AST.TypeSet)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), -KeyWord("VAR") +Ident +-SpecialToken("]"))
		.Modify((a1, _, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.TypeVar))
		.SetDebugName(nameof(mIL_AST.TypeVar)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("REC"), Ident, -SpecialToken("=>") +Ident +-SpecialToken("]"))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeRecursive))
		.SetDebugName(nameof(mIL_AST.TypeRecursive)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("ANY"), Ident, -SpecialToken("=>") +Ident +-SpecialToken("]"))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeInterface))
		.SetDebugName(nameof(mIL_AST.TypeInterface)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("ALL"), Ident, -SpecialToken("=>") +Ident +-SpecialToken("]"))
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeGeneric))
		.SetDebugName(nameof(mIL_AST.TypeGeneric))
	)
	.SetDebugName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mIL_AST.tCommandNode<tSpan>>, tError>
	Block = (Command +-NL)[1, null]
	.SetDebugName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tToken, (tText, mStream.tStream<mIL_AST.tCommandNode<tSpan>>), tError>
	Def = mParserGen.Seq(KeyWord("DEF"), Ident, NL, Block)
	.Modify((_, aID, __, aBlock) => (aID.Text, aBlock))
	.SetDebugName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<(tText, mStream.tStream<mIL_AST.tCommandNode<tSpan>>)>, tError>
	Module = Def[1, null]
	.SetDebugName(nameof(Module));
}
