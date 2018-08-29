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
	Command = mParserGen.OneOf(
		mParserGen.If(
			Ident,
			a1 => mParserGen.If(
				-SpecialToken(":") -Token("="),
				_ => mParserGen.OneOf(
					Number
					.ModifySS((aSpan, aNumber) => (mStd.Merge(a1.Span, aSpan), (a1, aNumber)))
					.ModifyS(mTokenizer.X(mIL_AST.CreateInt))
					.SetDebugName(nameof(mIL_AST.CreateInt)),
					
					mParserGen.If(
						-KeyWord("BOOL") +Ident,
						a2 => mParserGen.OneOf(
							(-Token("&") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.And))
							.SetDebugName(nameof(mIL_AST.And)),
							
							(-Token("|") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.Or))
							.SetDebugName(nameof(mIL_AST.Or)),
							
							(-Token("^") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.XOr))
							.SetDebugName(nameof(mIL_AST.XOr))
						)
					),
					
					mParserGen.If(
						-KeyWord("INT") +Ident,
						a2 => mParserGen.OneOf(
							(-Token("==") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.IntsAreEq))
							.SetDebugName(nameof(mIL_AST.IntsAreEq)),
							
							(-Token("<=>") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.IntsComp))
							.SetDebugName(nameof(mIL_AST.IntsComp)),
							
							(-Token("+") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.IntsAdd))
							.SetDebugName(nameof(mIL_AST.IntsAdd)),
							
							(-Token("-") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.IntsSub))
							.SetDebugName(nameof(mIL_AST.IntsSub)),
							
							(-Token("*") +Ident.OrFail())
							.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.IntsMul))
							.SetDebugName(nameof(mIL_AST.IntsMul))
						)
					),
					
					mParserGen.Seq(Ident, -SpecialToken(",") +Ident.OrFail())
					.ModifySS((aSpan, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
					.ModifyS(mTokenizer.X(mIL_AST.CreatePair))
					.SetDebugName(nameof(mIL_AST.CreatePair)),
					
					(-KeyWord("1ST") +Ident.OrFail())
					.ModifySS((aSpan, a2) => (mStd.Merge(a1.Span, aSpan), (a1, a2)))
					.ModifyS(mTokenizer.X(mIL_AST.GetFirst))
					.SetDebugName(nameof(mIL_AST.GetFirst)),
					
					(-KeyWord("2ND") +Ident.OrFail())
					.ModifySS((aSpan, a2) => (mStd.Merge(a1.Span, aSpan), (a1, a2)))
					.ModifyS(mTokenizer.X(mIL_AST.GetSecond))
					.SetDebugName(nameof(mIL_AST.GetSecond)),
					
					mParserGen.Seq(Token("+"), Prefix.OrFail(), Ident.OrFail())
					.ModifySS((aSpan, __, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
					.ModifyS(mTokenizer.X(mIL_AST.AddPrefix))
					.SetDebugName(nameof(mIL_AST.AddPrefix)),
					
					mParserGen.Seq(Token("-"), Prefix.OrFail(), Ident.OrFail())
					.ModifySS((aSpan, __, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
					.ModifyS(mTokenizer.X(mIL_AST.SubPrefix))
					.SetDebugName(nameof(mIL_AST.SubPrefix)),
					
					mParserGen.Seq(Token("?"), Prefix.OrFail(), Ident.OrFail())
					.ModifySS((aSpan, __, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
					.ModifyS(mTokenizer.X(mIL_AST.HasPrefix))
					.SetDebugName(nameof(mIL_AST.HasPrefix)),
					
					mParserGen.Seq(SpecialToken("."), Ident, Ident)
					.ModifySS((aSpan, __, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
					.ModifyS(mTokenizer.X(mIL_AST.Call))
					.SetDebugName(nameof(mIL_AST.Call)),
					
					mParserGen.Seq(-KeyWord("OBJ") -SpecialToken(":").OrFail(), Ident.OrFail(), Ident.OrFail())
					.ModifySS((aSpan, __, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
					.ModifyS(mTokenizer.X(mIL_AST.Exec))
					.SetDebugName(nameof(mIL_AST.Exec)),
					
					mParserGen.Seq(KeyWord("VAR"), Ident, Token("->"))
					.ModifySS((aSpan, __, a2, ___) => (mStd.Merge(a1.Span, aSpan), (a1, a2)))
					.ModifyS(mTokenizer.X(mIL_AST.VarGet))
					.SetDebugName(nameof(mIL_AST.VarGet)),
					
					mParserGen.Seq(KeyWord("VAR"), Ident.OrFail())
					.ModifySS((aSpan, __, a2) => (mStd.Merge(a1.Span, aSpan), (a1, a2)))
					.ModifyS(mTokenizer.X(mIL_AST.VarDef))
					.SetDebugName(nameof(mIL_AST.VarDef)),
					
					Ident
					.ModifySS((aSpan, a2) => (mStd.Merge(a1.Span, aSpan), (a1, a2)))
					.ModifyS(mTokenizer.X(mIL_AST.Alias))
					.SetDebugName(nameof(mIL_AST.Alias)),
					
					mParserGen.If(
						SpecialToken("["),
						__ => mParserGen.OneOf(
							mParserGen.If(
								Ident,
								a2 => mParserGen.OneOf(
									(-Token("&") +(Ident +-SpecialToken("]")).OrFail())
									.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
									.ModifyS(mTokenizer.X(mIL_AST.TypeCond))
									.SetDebugName(nameof(mIL_AST.TypeCond)),
									
									(-SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
									.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
									.ModifyS(mTokenizer.X(mIL_AST.TypeFunc))
									.SetDebugName(nameof(mIL_AST.TypeFunc)),
									
									(-SpecialToken(":") +(Ident +-SpecialToken("]")).OrFail())
									.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
									.ModifyS(mTokenizer.X(mIL_AST.TypeMethod))
									.SetDebugName(nameof(mIL_AST.TypeMethod)),
									
									(-SpecialToken(",") +(Ident +-SpecialToken("]")).OrFail())
									.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
									.ModifyS(mTokenizer.X(mIL_AST.TypePair))
									.SetDebugName(nameof(mIL_AST.TypePair)),
									
									(-Token("|") +(Ident +-SpecialToken("]")).OrFail())
									.ModifySS((aSpan, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
									.ModifyS(mTokenizer.X(mIL_AST.TypeSet))
									.SetDebugName(nameof(mIL_AST.TypeSet))
								)
							),
							
							mParserGen.Seq(Prefix, (Ident +-SpecialToken("]")).OrFail())
							.ModifySS((aSpan, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.TypePrefix))
							.SetDebugName(nameof(mIL_AST.TypePrefix)),
							
							mParserGen.Seq(KeyWord("VAR"), (Ident +-SpecialToken("]")).OrFail())
							.ModifySS((aSpan, ___, a2) => (mStd.Merge(a1.Span, aSpan), (a1, a2)))
							.ModifyS(mTokenizer.X(mIL_AST.TypeVar))
							.SetDebugName(nameof(mIL_AST.TypeVar)),
							
							mParserGen.Seq(KeyWord("REC"), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
							.ModifySS((aSpan, ___, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.TypeRecursive))
							.SetDebugName(nameof(mIL_AST.TypeRecursive)),
							
							mParserGen.Seq(KeyWord("ANY"), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
							.ModifySS((aSpan, ___, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.TypeInterface))
							.SetDebugName(nameof(mIL_AST.TypeInterface)),
							
							mParserGen.Seq(KeyWord("ALL"), Ident, -SpecialToken("=>") +(Ident +-SpecialToken("]")).OrFail())
							.ModifySS((aSpan, ___, a2, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
							.ModifyS(mTokenizer.X(mIL_AST.TypeGeneric))
							.SetDebugName(nameof(mIL_AST.TypeGeneric))
						)
					)
				)
			) |
			mParserGen.Seq(SpecialToken("=>"), Ident, SpecialToken(":"), Ident.OrFail())
			.ModifySS((aSpan, _, a2, __, a3) => (mStd.Merge(a1.Span, aSpan), (a1, a2, a3)))
			.ModifyS(mTokenizer.X(mIL_AST.Proof))
			.SetDebugName(nameof(mIL_AST.Proof))
		),
		
		(-KeyWord("PUSH") +Ident.OrFail())
		.ModifyS(mTokenizer.X(mIL_AST.Push))
		.SetDebugName(nameof(mIL_AST.Push)),
		
		(-KeyWord("POP"))
		.ModifyS(mTokenizer.X(mIL_AST.Pop))
		.SetDebugName(nameof(mIL_AST.Pop)),
		
		mParserGen.Seq(-KeyWord("VAR"), Ident.OrFail(), Token("<-").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.VarSet))
		.SetDebugName(nameof(mIL_AST.VarSet)),
		
		mParserGen.Seq(KeyWord("RETURN"), Ident.OrFail(), Token("IF").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.ReturnIf))
		.SetDebugName(nameof(mIL_AST.ReturnIf)),
		
		mParserGen.Seq(KeyWord("REPEAT"), Ident.OrFail(), Token("IF").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.RepeatIf))
		.SetDebugName(nameof(mIL_AST.RepeatIf)),
		
		mParserGen.Seq(KeyWord("ASSERT"), Ident.OrFail(), SpecialToken("=>").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.Assert))
		.SetDebugName(nameof(mIL_AST.Assert)),
		
		mParserGen.Seq(KeyWord("TYPE_OF"), Ident.OrFail(), Token("IS").OrFail(), Ident.OrFail())
		.Modify((_, a1, __, a2) => (a1, a2))
		.ModifyS(mTokenizer.X(mIL_AST.TypeIs))
		.SetDebugName(nameof(mIL_AST.TypeIs))
	)
	.SetDebugName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mList.tList<mIL_AST.tCommandNode<tSpan>>, tError>
	Block = (Command +-NL)[1, null]
	.SetDebugName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tToken, (tText, mList.tList<mIL_AST.tCommandNode<tSpan>>), tError>
	Def = mParserGen.Seq(KeyWord("DEF"), Ident, NL, Block)
	.Modify((_, aID, __, aBlock) => (aID.Text, aBlock))
	.SetDebugName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tToken, mList.tList<(tText, mList.tList<mIL_AST.tCommandNode<tSpan>>)>, tError>
	Module = Def[1, null]
	.SetDebugName(nameof(Module));
}
