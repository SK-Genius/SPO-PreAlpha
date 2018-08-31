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
using tSpan =mStd.tSpan<mTextStream.tPos>;
using tError = mTextStream.tError;

public static class mIL_Parser {
	
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
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Ident, -Token("&") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.And))
		.SetDebugName(nameof(mIL_AST.And)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Ident, -Token("|") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Or))
		.SetDebugName(nameof(mIL_AST.Or)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Ident, -Token("^") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.XOr))
		.SetDebugName(nameof(mIL_AST.XOr)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("==") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsAreEq))
		.SetDebugName(nameof(mIL_AST.IntsAreEq)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("<=>") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsComp))
		.SetDebugName(nameof(mIL_AST.IntsComp)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("+") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsAdd))
		.SetDebugName(nameof(mIL_AST.IntsAdd)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("-") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsSub))
		.SetDebugName(nameof(mIL_AST.IntsSub)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("INT"), Ident, -Token("*") +Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsMul))
		.SetDebugName(nameof(mIL_AST.IntsMul)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Ident, -SpecialToken(",") +Ident.OrFail())
		.Modify((a1, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CreatePair))
		.SetDebugName(nameof(mIL_AST.CreatePair)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -KeyWord("1ST") +Ident.OrFail())
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.GetFirst))
		.SetDebugName(nameof(mIL_AST.GetFirst)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -KeyWord("2ND") +Ident.OrFail())
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.GetSecond))
		.SetDebugName(nameof(mIL_AST.GetSecond)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Token("+"), Prefix.OrFail(), Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.AddPrefix))
		.SetDebugName(nameof(mIL_AST.AddPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Token("-"), Prefix.OrFail(), Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.SubPrefix))
		.SetDebugName(nameof(mIL_AST.SubPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Token("?"), Prefix.OrFail(), Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.HasPrefix))
		.SetDebugName(nameof(mIL_AST.HasPrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("."), Ident, Ident)
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Call))
		.SetDebugName(nameof(mIL_AST.Call)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -KeyWord("OBJ") -SpecialToken(":").OrFail(), Ident.OrFail(), Ident.OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Exec))
		.SetDebugName(nameof(mIL_AST.Exec)) |
		
		(-KeyWord("PUSH") +Ident.OrFail())
		.ModifyS(mTokenizer.X(mIL_AST.Push))
		.SetDebugName(nameof(mIL_AST.Push)) |
		
		(-KeyWord("POP"))
		.ModifyS(mTokenizer.X(mIL_AST.Pop))
		.SetDebugName(nameof(mIL_AST.Pop)) |
		
		mParserGen.Seq(-KeyWord("VAR"), Ident.OrFail(), Token("<-").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarSet))
		.SetDebugName(nameof(mIL_AST.VarSet)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("VAR"), Ident, Token("->"))
		.Modify((a1, _, __, a2, ___) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarGet))
		.SetDebugName(nameof(mIL_AST.VarGet)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), KeyWord("VAR"), Ident.OrFail())
		.Modify((a1, _, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarDef))
		.SetDebugName(nameof(mIL_AST.VarDef)) |
		
		mParserGen.Seq(KeyWord("RETURN"), Ident.OrFail(), Token("IF").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.ReturnIf))
		.SetDebugName(nameof(mIL_AST.ReturnIf)) |
		
		mParserGen.Seq(KeyWord("REPEAT"), Ident.OrFail(), Token("IF").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.RepeatIf))
		.SetDebugName(nameof(mIL_AST.RepeatIf)) |
		
		mParserGen.Seq(KeyWord("ASSERT"), Ident.OrFail(), SpecialToken("=>").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Assert))
		.SetDebugName(nameof(mIL_AST.Assert)) |
		
		mParserGen.Seq(Ident, SpecialToken("=>"), Ident, SpecialToken(":"), Ident.OrFail())
		.Modify((a1, _, a2, __, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Proof))
		.SetDebugName(nameof(mIL_AST.Proof)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), Ident)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Alias))
		.SetDebugName(nameof(mIL_AST.Alias)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -Token("&") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeCond))
		.SetDebugName(nameof(mIL_AST.TypeCond)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeFunc))
		.SetDebugName(nameof(mIL_AST.TypeFunc)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -SpecialToken(":") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeMethod))
		.SetDebugName(nameof(mIL_AST.TypeMethod)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -SpecialToken(",") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypePair))
		.SetDebugName(nameof(mIL_AST.TypePair)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Prefix, (Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypePrefix))
		.SetDebugName(nameof(mIL_AST.TypePrefix)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), Ident, -Token("|") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeSet))
		.SetDebugName(nameof(mIL_AST.TypeSet)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), SpecialToken("["), -KeyWord("VAR") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.TypeVar))
		.SetDebugName(nameof(mIL_AST.TypeVar)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("REC"), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeRecursive))
		.SetDebugName(nameof(mIL_AST.TypeRecursive)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("ANY"), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeInterface))
		.SetDebugName(nameof(mIL_AST.TypeInterface)) |
		
		mParserGen.Seq(Ident, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("ALL"), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
		.Modify((a1, _, __, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeGeneric))
		.SetDebugName(nameof(mIL_AST.TypeGeneric)) |
		
		mParserGen.Seq(KeyWord("TYPE_OF"), Ident.OrFail(), Token("IS").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.TypeIs))
		.SetDebugName(nameof(mIL_AST.TypeIs))
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
