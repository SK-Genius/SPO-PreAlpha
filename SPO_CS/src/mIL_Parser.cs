﻿//IMPORT mTokenizer.cs
//IMPORT mIL_AST.cs

#nullable enable

using tToken = mTokenizer.tToken;

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class
mIL_Parser {
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL = mTokenizer.NL_Token;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	QuotedString = mTokenizer.TextToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Number = mTokenizer.NumberToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Id = mTokenizer.IdToken;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	SpecialToken = mTokenizer.SpecialToken;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tChar>
	SpecialId = mTokenizer.SpecialId;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	Token = mTokenizer.Token_;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	KeyWord = mTokenizer.KeyWord;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Prefix = SpecialId('#').Modify(
		a => new tToken{
			Span = a.Span,
			Type = a.Type,
			Text = a.Text[1..]
		}
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mIL_AST.tCommandNode<tSpan>, tError>
	Command = mParserGen.OneOf(
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), Number)
		.Modify((aId, _, aNumber) => (aId, aNumber))
		.ModifyS(mTokenizer.X(mIL_AST.CreateInt))
		.SetDebugName(nameof(mIL_AST.CreateInt)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_BOOL"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsBool))
		.SetDebugName(nameof(mIL_AST.IsBool)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Id, -Token("&") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.And))
		.SetDebugName(nameof(mIL_AST.And)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Id, -Token("|") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Or))
		.SetDebugName(nameof(mIL_AST.Or)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("BOOL"), Id, -Token("^") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.XOr))
		.SetDebugName(nameof(mIL_AST.XOr)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_INT"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsInt))
		.SetDebugName(nameof(mIL_AST.IsInt)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("INT"), Id, -Token("==") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsAreEq))
		.SetDebugName(nameof(mIL_AST.IntsAreEq)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("INT"), Id, -Token("<=>") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsComp))
		.SetDebugName(nameof(mIL_AST.IntsComp)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("INT"), Id, -Token("+") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsAdd))
		.SetDebugName(nameof(mIL_AST.IntsAdd)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("INT"), Id, -Token("-") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsSub))
		.SetDebugName(nameof(mIL_AST.IntsSub)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("INT"), Id, -Token("*") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsMul))
		.SetDebugName(nameof(mIL_AST.IntsMul)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("INT"), Id, -Token("/") +Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.IntsDiv))
		.SetDebugName(nameof(mIL_AST.IntsDiv)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_PAIR"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsPair))
		.SetDebugName(nameof(mIL_AST.IsPair)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), Id, -SpecialToken(",") +Id)
		.Modify((a1, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CreatePair))
		.SetDebugName(nameof(mIL_AST.CreatePair)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), -KeyWord("1ST") +Id)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.GetFirst))
		.SetDebugName(nameof(mIL_AST.GetFirst)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), -KeyWord("2ND") +Id)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.GetSecond))
		.SetDebugName(nameof(mIL_AST.GetSecond)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_PREFIX"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsPrefix))
		.SetDebugName(nameof(mIL_AST.IsPrefix)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), Token("+"), Prefix, Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.AddPrefix))
		.SetDebugName(nameof(mIL_AST.AddPrefix)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), Token("-"), Prefix, Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.SubPrefix))
		.SetDebugName(nameof(mIL_AST.SubPrefix)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), Token("?"), Prefix, Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.HasPrefix))
		.SetDebugName(nameof(mIL_AST.HasPrefix)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_RECORD"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsRecord))
		.SetDebugName(nameof(mIL_AST.IsRecord)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -SpecialToken("{"), Id, -SpecialToken("}") -Token("+"), Id)
		.Modify((a1, _, a2, _, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.ExtendRec))
		.SetDebugName(nameof(mIL_AST.ExtendRec)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -SpecialToken("{"), Id, (-SpecialToken("}") -Token("/")))
		.Modify((a1, _, a2, _) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.DivideRec))
		.SetDebugName(nameof(mIL_AST.DivideRec)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("."), Id, Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CallFunc))
		.SetDebugName(nameof(mIL_AST.CallFunc)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), -KeyWord("OBJ") -SpecialToken(":"), Id, Id)
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.CallProc))
		.SetDebugName(nameof(mIL_AST.CallProc)),
		
		mParserGen.Seq(-KeyWord("VAR"), Id, Token("<-"), Id)
		.Modify((_, a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarSet))
		.SetDebugName(nameof(mIL_AST.VarSet)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("VAR"), Id, Token("->"))
		.Modify((a1, _, _, a2, _) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarGet))
		.SetDebugName(nameof(mIL_AST.VarGet)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_VAR"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsVar))
		.SetDebugName(nameof(mIL_AST.IsVar)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), KeyWord("VAR"), Id)
		.Modify((a1, _, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarDef))
		.SetDebugName(nameof(mIL_AST.VarDef)),
		
		mParserGen.Seq(KeyWord("RETURN"), Id, Token("IF"), Id)
		.Modify((_, a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.ReturnIf))
		.SetDebugName(nameof(mIL_AST.ReturnIf)),
		
		mParserGen.Seq(KeyWord("REPEAT"), Id, Token("IF"), Id)
		.Modify((_, a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.RepeatIf))
		.SetDebugName(nameof(mIL_AST.RepeatIf)),
		
		mParserGen.Seq(KeyWord("ASSERT"), Id, SpecialToken("=>"), Id)
		.Modify((_, a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Assert))
		.SetDebugName(nameof(mIL_AST.Assert)),
		
		mParserGen.Seq(Id, SpecialToken("=>"), Id, SpecialToken(":"), Id)
		.Modify((a1, _, a2, _, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.Proof))
		.SetDebugName(nameof(mIL_AST.Proof)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), Id)
		.Modify((a1, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Alias))
		.SetDebugName(nameof(mIL_AST.Alias)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -KeyWord("IS_TYPE"), Id)
		.Modify((aId1, _, aId2) => (aId1, aId2))
		.ModifyS(mTokenizer.X(mIL_AST.IsType))
		.SetDebugName(nameof(mIL_AST.IsType)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), Id, -Token("&") +(Id +-SpecialToken("]")))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeCond))
		.SetDebugName(nameof(mIL_AST.TypeCond)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), Id, -SpecialToken("=>") +(Id +-SpecialToken("]")))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeFunc))
		.SetDebugName(nameof(mIL_AST.TypeFunc)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), Id, -SpecialToken(":") +(Id +-SpecialToken("]")))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeMethod))
		.SetDebugName(nameof(mIL_AST.TypeMethod)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), Id, -SpecialToken(",") +(Id +-SpecialToken("]")))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypePair))
		.SetDebugName(nameof(mIL_AST.TypePair)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), Prefix, (Id +-SpecialToken("]")))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypePrefix))
		.SetDebugName(nameof(mIL_AST.TypePrefix)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("=") -SpecialToken("[") -SpecialToken("{"), Id +-SpecialToken("}"), -Token("+") +Id +-SpecialToken("]"))
		.Modify((a1, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeRecord))
		.SetDebugName(nameof(mIL_AST.TypeRecord)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), Id, -Token("|") +Id +-SpecialToken("]"))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeSet))
		.SetDebugName(nameof(mIL_AST.TypeSet)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), -KeyWord("VAR") +Id +-SpecialToken("]"))
		.Modify((a1, _, _, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.TypeVar))
		.SetDebugName(nameof(mIL_AST.TypeVar)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), SpecialToken("["), -KeyWord("FREE") -SpecialToken("]"))
		.Modify((a, _, _, _) => a)
		.ModifyS(mTokenizer.X(mIL_AST.TypeFree))
		.SetDebugName(nameof(mIL_AST.TypeFree)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("REC"), Id, -SpecialToken("=>") +Id +-SpecialToken("]"))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeRecursive))
		.SetDebugName(nameof(mIL_AST.TypeRecursive)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("ANY"), Id, -SpecialToken("=>") +Id +-SpecialToken("]"))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeInterface))
		.SetDebugName(nameof(mIL_AST.TypeInterface)),
		
		mParserGen.Seq(Id, -SpecialToken(":") -Token("="), -SpecialToken("[") -KeyWord("ALL"), Id, -SpecialToken("=>") +Id +-SpecialToken("]"))
		.Modify((a1, _, _, a2, a3) => (a1, a2, a3))
		.ModifyS(mTokenizer.X(mIL_AST.TypeGeneric))
		.SetDebugName(nameof(mIL_AST.TypeGeneric))
	)
	.SetDebugName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mIL_AST.tCommandNode<tSpan>>?, tError>
	Block = (Command +-NL)[1..]
	.SetDebugName(nameof(Block));

	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mIL_AST.tCommandNode<tSpan>>?, tError>
	Types = (-KeyWord("TYPES") -NL +Block)
	.SetDebugName(nameof(Types));

	public static readonly mParserGen.tParser<tPos, tToken, mIL_AST.tDef<tSpan>, tError>
	Def = mParserGen.Seq(-KeyWord("DEF") +Id, -SpecialToken("€") + Id +-NL, Block)
	.Modify((a1, a2, a3) => mIL_AST.Def(a1.Text, a2.Text, a3))
	.SetDebugName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tToken, mIL_AST.tModule<tSpan>, tError>
	Module = mParserGen.Seq(Types, Def[1..])
	.Modify((a1, a2) => mIL_AST.Module(a1, a2))
	.SetDebugName(nameof(Module));
}
