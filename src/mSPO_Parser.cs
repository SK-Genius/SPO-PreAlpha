//IMPORT mSPO_AST.cs
//IMPORT mTextParser.cs
//IMPORT mTokenizer.cs

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
using tTokenType = mTokenizer.tTokenType;

using tPos = mTextStream.tPos;
using tSpan =mStd.tSpan<mTextStream.tPos>;
using tError = mTextStream.tError;

public static class mSPO_Parser {
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	SpaceToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && (a.Text == " " || a.Text == "\t"),
		a => new tError {
			Message = "expect space",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(SpaceToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL_Token = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == "\n",
		a => new tError {
			Message = "expect line break",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(NL_Token));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStd.tEmpty, tError>
	NLs_Token = -(NL_Token | SpaceToken)[1, null]
	.SetDebugName(nameof(NLs_Token));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	TextToken  = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Text,
		a => new tError {
			Message = "expect '\"'",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(TextToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NumberToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Number,
		a => new tError {
			Message = "expect number",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(NumberToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	IdentToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Ident,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(IdentToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	PrefixToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Prefix,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(PrefixToken));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	Token(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Ident && a.Text == aText,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName($"{nameof(Token)}('{aText}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialToken(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == aText,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName($"{nameof(SpecialToken)}('{aText}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	KeyWord(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.KeyWord && a.Text == aText,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName($"{nameof(KeyWord)}('{aText}')");
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>> aFunc
	//================================================================================
	) => aSpan => aFunc(aSpan);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText> aFunc
	//================================================================================
	) => (aSpan, a1) => aFunc(aSpan, a1.Text);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText, tText> aFunc
	//================================================================================
	) => (aSpan, a1, a2) => aFunc(aSpan, a1.Text, a2.Text);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, tToken, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText, tText, tText> aFunc
	//================================================================================
	) => (aSpan, a1, a2, a3) => aFunc(aSpan, a1.Text, a2.Text, a3.Text);
	
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tOut, tError>
	SetName<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aName
	//================================================================================
	) => aParser.AddError(
		a => mTextStream.Error(a.Span.Start, $"invalid {aName}")
	)
	.SetDebugName(aName);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tEmptyNode<tSpan>, tError>
	Empty = (-SpecialToken("(") -SpecialToken(")"))
	.ModifyS(mSPO_AST.Empty)
	.SetName(nameof(Empty));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIgnoreMatchNode<tSpan>, tError>
	IgnoreMatch = IdentToken
	.Assert(a => a.Type == tTokenType.Ident && a.Text == "_", a => mTextStream.Error(a.Span.Start, "expected _"))
	.ModifyS(mSPO_AST.IgnoreMatch)
	.SetName(nameof(IgnoreMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIdentNode<tSpan>, tError>
	Ident = IdentToken
	.ModifyS(X(mSPO_AST.Ident))
	.SetName(nameof(Ident));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tNumberNode<tSpan>, tError>
	Number = NumberToken
	.Modify(a => tInt32.Parse(a.Text))
	.ModifyS(mSPO_AST.Number)
	.SetName(nameof(Number));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTextNode<tSpan>, tError>
	Text = TextToken
	.ModifyS(X(mSPO_AST.Text))
	.SetName(nameof(Text));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLiteralNode<tSpan>, tError>
	Literal = (
		mParserGen.OneOf(
			Empty.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			Number.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			Text.Cast<mSPO_AST.tLiteralNode<tSpan>>()
		)
	)
	.SetName(nameof(Literal));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tOut, tError>
	C<tOut>(
		mParserGen.tParser<tPos, tToken, tOut, tError> aParser
	//================================================================================
	) => (
		(-SpecialToken("(") + (-NLs_Token + (aParser + (-NLs_Token -SpecialToken(")"))))) |
		(-SpecialToken("(") +(aParser + -SpecialToken(")")))
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	ExpressionInCall = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>()
	.SetName(nameof(ExpressionInCall));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Expression = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>()
	.SetName(nameof(Expression));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>
	UnTypedMatch = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>()
	.SetName(nameof(UnTypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>
	TypedMatch = mParserGen.Seq(UnTypedMatch.Cast<mSPO_AST.tMatchItemNode<tSpan>>(), (-SpecialToken("€") +Expression.OrFail()))
	.ModifyS(mSPO_AST.Match)
	.SetName(nameof(TypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>
	Match = (TypedMatch | UnTypedMatch)
	.SetName(nameof(Match));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tDefNode<tSpan>, tError>
	Def = mParserGen.Seq(KeyWord("DEF"), Match.OrFail(), Token("=").OrFail(), Expression.OrFail())
	.Modify((_, aMatch, __, aExpression) => (aMatch, aExpression))
	.ModifyS(mSPO_AST.Def)
	.SetName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tReturnIfNode<tSpan>, tError>
	ReturnIf = mParserGen.Seq(KeyWord("RETURN"), Expression.OrFail(), Token("IF"), Expression.OrFail())
	.Modify((_, aResult, __, aCond) => (aResult, aCond))
	.ModifyS(mSPO_AST.ReturnIf)
	.SetName(nameof(ReturnIf));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tReturnIfNode<tSpan>, tError>
	Return = (-KeyWord("RETURN") +Expression.OrFail())
	.ModifyS((aSpan, a) => mSPO_AST.ReturnIf(aSpan, a, mSPO_AST.True(aSpan)))
	.SetName(nameof(Return));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCommandNode<tSpan>, tError>
	Command = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tCommandNode<tSpan>, tError>()
	.SetName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mList.tList<mSPO_AST.tCommandNode<tSpan>>, tError>
	Commands = Command[0, null]
	.SetName(nameof(Commands));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tBlockNode<tSpan>, tError>
	Block = mParserGen.Seq(SpecialToken("{"), NLs_Token.OrFail(), Commands.OrFail(), SpecialToken("}").OrFail())
	.Modify((_, __, aCommands, ___) => aCommands)
	.ModifyS(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Tuple = C( mParserGen.Seq(ExpressionInCall, ((-SpecialToken(",") | -NLs_Token) +ExpressionInCall)[1, null]) )
	.Modify(mList.List)
	.ModifyS(mSPO_AST.Tuple)
	.SetName(nameof(Tuple));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdentNode<tSpan> Ident, mList.tList<tChild> Childs), tError>
	Infix<tChild>(
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	//================================================================================
	) => (
		mParserGen.Seq(Ident, mParserGen.Seq(aChildParser, Ident)[0, null], aChildParser[0, 1])
		.ModifyS(
			(aSpan, aFirstIdent, aList, aLastChild) => (
				mSPO_AST.Ident(
					aSpan,
					aList.Map(
						a => a.Item2.Name.Substring(1)
					).Reduce(
						aFirstIdent.Name.Substring(1),
						(a1, a2) => $"{a1}...{a2}"
					) + (aLastChild.IsEmpty() ? "" : "...")
				),
				mList.Concat(aList.Map(a => a.Item1), aLastChild)
			)
		)
	);
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdentNode<tSpan> Ident, mList.tList<tChild> Childs), tError>
	Infix<tChild>(
		tText aPrefix,
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	//================================================================================
	) => (
		(-SpecialToken(aPrefix) +Infix(aChildParser))
	) | (
		mParserGen.Seq(aChildParser, SpecialToken(aPrefix), Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstChild, _, aInfix) => (
				Ident: mSPO_AST.Ident(aSpan, "..." + aInfix.Ident.Name.Substring(1)),
				Childs: mList.List(aFirstChild, aInfix.Childs)
			)
		)
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCallNode<tSpan>, tError>
	Call = (
		Infix(".", ExpressionInCall).ModifyS(
			(aSpan, a) => mSPO_AST.Call(aSpan, a.Ident, mSPO_AST.Tuple(aSpan, a.Childs))
		) | (
			-SpecialToken(".") +(ExpressionInCall +ExpressionInCall).ModifyS(mSPO_AST.Call)
		)
	)
	.SetName(nameof(Call));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPrefixNode<tSpan>, tError>
	Prefix = Infix("#", ExpressionInCall)
	.ModifyS((aSpan, aIdent, aChilds) => mSPO_AST.Prefix(aSpan, aIdent, mSPO_AST.Tuple(aSpan, aChilds)))
	.SetName(nameof(Prefix));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchPrefixNode<tSpan>, tError>
	MatchPrefix = C( Infix("#", UnTypedMatch) )
	.ModifyS(
		(aSpan, aIdent, aChilds) => mSPO_AST.MatchPrefix(
			aSpan,
			aIdent,
			mSPO_AST.Match(aSpan, mSPO_AST.MatchTuple(aSpan, aChilds), null)
		)
	)
	.SetName(nameof(MatchPrefix));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchGuardNode<tSpan>, tError>
	MatchGuard = C( mParserGen.Seq(UnTypedMatch, Token("|"), Expression.OrFail()) )
	.Modify((a1, _ , a2) => (a1, a2))
	.ModifyS(mSPO_AST.MatchGuard)
	.SetName(nameof(MatchGuard));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tEmptyTypeNode<tSpan>, tError>
	EmptyType = (-SpecialToken("[") -SpecialToken("]"))
	.ModifyS(mSPO_AST.EmptyType)
	.SetName(nameof(EmptyType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tBoolTypeNode<tSpan>, tError>
	BoolType = (-KeyWord("BOOL"))
	.ModifyS(mSPO_AST.BoolType)
	.SetName(nameof(BoolType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIntTypeNode<tSpan>, tError>
	IntType = (-KeyWord("INT"))
	.ModifyS(mSPO_AST.IntType)
	.SetName(nameof(IntType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypeTypeNode<tSpan>, tError>
	TypeType = (-SpecialToken("[") -SpecialToken("[") -SpecialToken("]") -SpecialToken("]") )
	.ModifyS(mSPO_AST.TypeType)
	.SetName(nameof(TypeType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPrefixTypeNode<tSpan>, tError>
	PrefixType = mParserGen.Seq(SpecialToken("["), Infix("#", Expression), SpecialToken("]").OrFail())
	.Modify((_, aInfix, __) => aInfix)
	.ModifyS(mSPO_AST.PrefixType)
	.SetName(nameof(PrefixType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTupleTypeNode<tSpan>, tError>
	TupleType = mParserGen.Seq(
		SpecialToken("["),
		NLs_Token[0, 1],
		Expression,
		((-SpecialToken(",") | -NLs_Token) +Expression.OrFail())[1, null],
		-NLs_Token[0, 1] +SpecialToken("]").OrFail()
	)
	.Modify((_, __, aFirst, aRest, ___) => mList.List(aFirst, aRest))
	.ModifyS(mSPO_AST.TupleType)
	.SetName(nameof(TupleType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tSetTypeNode<tSpan>, tError>
	SetType = mParserGen.Seq(
		SpecialToken("["),
		NLs_Token[0, 1],
		Expression +-Token("|"),
		(-NLs_Token[0, 1] +Expression)[1, null].OrFail(),
		Token("|")[0, 1] +-(-NLs_Token[0, 1] +SpecialToken("]").OrFail())
	)
	.Modify((_, __, aFirst, aRest, ___) => mList.List(aFirst, aRest))
	.ModifyS(mSPO_AST.SetType)
	.SetName(nameof(SetType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLambdaTypeNode<tSpan>, tError>
	LambdaType = mParserGen.Seq(
		SpecialToken("["),
		Expression,
		SpecialToken("=>"),
		Expression.OrFail(),
		SpecialToken("]").OrFail()
	)
	.Modify((_, a1, __, a2, ___) => (a1, a2))
	.ModifyS(mSPO_AST.LambdaType)
	.SetName(nameof(LambdaType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecursiveTypeNode<tSpan>, tError>
	RecursiveType = mParserGen.Seq(
		SpecialToken("["),
		KeyWord("RECURSIV"),
		Ident.OrFail(),
		Expression.OrFail(),
		SpecialToken("]").OrFail()
	)
	.Modify((_, __, aIdent, aExpression, ___) => (aIdent, aExpression))
	.ModifyS(mSPO_AST.RecursiveType)
	.SetName(nameof(RecursiveType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tInterfaceTypeNode<tSpan>, tError>
	InterfaceType = mParserGen.Seq(
		SpecialToken("["),
		KeyWord("INTERFACE"),
		Ident.OrFail(),
		Expression.OrFail(),
		SpecialToken("]").OrFail()
	)
	.Modify((_, __, aIdent, aExpression, ___) => (aIdent, aExpression))
	.ModifyS(mSPO_AST.InterfaceType)
	.SetName(nameof(InterfaceType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tGenericTypeNode<tSpan>, tError>
	GenericType = mParserGen.Seq(
		SpecialToken("["),
		KeyWord("GENERIC"),
		Ident,
		Expression,
		SpecialToken("]").OrFail()
	)
	.Modify((_, __, aIdent, aExpression, ___) => (aIdent, aExpression))
	.ModifyS(mSPO_AST.GenericType)
	.SetName(nameof(GenericType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Type = mParserGen.OneOf(
		EmptyType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		BoolType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		IntType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		TypeType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		PrefixType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		TupleType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		SetType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		LambdaType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		RecursiveType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		InterfaceType.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
		GenericType.Cast<mSPO_AST.tExpressionNode<tSpan>>()
	)
	.SetName(nameof(Type));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLambdaNode<tSpan>, tError>
	Lambda = mParserGen.Seq(Match, SpecialToken("=>"), Expression.OrFail())
	.Modify((aMatch, _, aExpression) => (aMatch, aExpression))
	.ModifyS(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodNode<tSpan>, tError>
	Method = mParserGen.Seq(Match, SpecialToken(":"), Match.OrFail(), Block.OrFail())
	.Modify((aObjMatch, _, aArgMatch, aBlock) => (aObjMatch, aArgMatch, aBlock))
	.ModifyS(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecLambdaItemNode<tSpan>, tError>
	RecLambdaItem = mParserGen.Seq(Ident, Token("="), Lambda | C( Lambda ))
	.Modify((aIdent, _, aLambda) => (aIdent, aLambda))
	.ModifyS(mSPO_AST.RecLambdaItem)
	.SetName(nameof(RecLambdaItem));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecLambdasNode<tSpan>, tError>
	RecLambda = (
		mParserGen.Seq(
			KeyWord("RECURSIV"),
			SpecialToken("{"),
			NLs_Token,
			(RecLambdaItem +-NLs_Token)[1, null],
			SpecialToken("}").OrFail()
		).Modify((_, __, ___, a, ____) => a) |
		(-KeyWord("RECURSIV") +RecLambdaItem[1, 1].OrFail())
	)
	.ModifyS(mSPO_AST.RecLambdas)
	.SetName(nameof(RecLambda));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIfNode<tSpan>, tError>
	If = mParserGen.Seq(
		KeyWord("IF"),
		SpecialToken("{"),
		NLs_Token,
		mParserGen.Seq(
			Expression,
			SpecialToken("=>"),
			Expression.OrFail(),
			NLs_Token.OrFail()
		).Modify((aCond, _, aRes, __) => (aCond, aRes))[0, null],
		SpecialToken("}").OrFail()
	)
	.Modify((_, __, ___, a, ____) => a)
	.ModifyS(mSPO_AST.If)
	.SetName(nameof(If));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIfMatchNode<tSpan>, tError>
	IfMatch = mParserGen.Seq(
		KeyWord("IF"),
		Expression,
		Token("MATCH") +-(SpecialToken("{") +-NLs_Token),
		mParserGen.Seq(
			Match,
			SpecialToken("=>"),
			Expression,
			NLs_Token.OrFail()
		).Modify((aMatch, _, aExpression, __) => (aMatch, aExpression))[0, null],
		SpecialToken("}").OrFail()
	)
	.Modify((_, aExpression, __, aBranches, ___) => (aExpression, aBranches))
	.ModifyS(mSPO_AST.IfMatch)
	.SetName(nameof(IfMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodCallNode<tSpan>, tError>
	MethodCall = mParserGen.Seq(
		Infix(ExpressionInCall),
		(-SpecialToken("=>") +(-KeyWord("DEF") +Match))[0, 1].Modify(aMatches => aMatches?.First())
	)
	.ModifyS(
		(aSpan, aInfix, aMaybeOut) => mSPO_AST.MethodCall(
			aSpan,
			aInfix.Ident,
			mSPO_AST.Tuple(aSpan, aInfix.Childs),
			aMaybeOut
		)
	)
	.SetName(nameof(MethodCall));
	
	public static readonly mParserGen.tParser<tPos, tToken, mList.tList<mSPO_AST.tMethodCallNode<tSpan>>, tError>
	MethodCalls = mParserGen.Seq(
		MethodCall,
		((-SpecialToken(",")|-NLs_Token) +MethodCall)[0, null],
		NLs_Token[0, 1],
		SpecialToken(".").OrFail()
	)
	.Modify((aFirst, aRest, _, __) => mList.List(aFirst, aRest))
	.SetName(nameof(MethodCalls));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tDefVarNode<tSpan>, tError>
	DefVar = mParserGen.Seq(
		KeyWord("VAR"),
		Ident,
		SpecialToken(":") +(-NLs_Token[0, 1] -Token("=")),
		Expression.OrFail(),
		(
			((-SpecialToken(",") | -NLs_Token) +MethodCalls) |
			SpecialToken(".").OrFail().Modify(a => mList.List<mSPO_AST.tMethodCallNode<tSpan>>())
		)
	)
	.Modify((_, aIdent, __, aFirst, aRest) => (aIdent, aFirst, aRest))
	.ModifyS(mSPO_AST.DefVar)
	.SetName(nameof(DefVar));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tVarToValNode<tSpan>, tError>
	VarToVal = (-KeyWord("TO_VAL") +Expression.OrFail())
	.ModifyS(mSPO_AST.VarToVal)
	.SetName(nameof(VarToVal));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodCallsNode<tSpan>, tError>
	MethodCallStatment = mParserGen.Seq(
		ExpressionInCall,
		SpecialToken(":"),
		NLs_Token[0, 1],
		MethodCalls.OrFail()
	)
	.Modify((aObj, _, __, aMethodCalls) => (aObj, aMethodCalls))
	.ModifyS(mSPO_AST.MethodCallStatment)
	.SetName(nameof(MethodCallStatment));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tImportNode<tSpan>, tError>
	Import = (-KeyWord("IMPORT") +(Match +-NLs_Token).OrFail())
	.ModifyS(mSPO_AST.Import)
	.SetName(nameof(Import));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExportNode<tSpan>, tError>
	Export = (-KeyWord("EXPORT") +Expression.OrFail())
	.ModifyS(mSPO_AST.Export)
	.SetName(nameof(Export));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tModuleNode<tSpan>, tError>
	Module = mParserGen.Seq(
		NLs_Token[0, 1],
		Import,
		Commands,
		Export,
		NLs_Token[0, 1]
	)
	.Modify((_, aImport, aCommands, aExports, __) => (aImport, aCommands, aExports))
	.ModifyS(mSPO_AST.Module)
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		UnTypedMatch.Def(
			mParserGen.OneOf(
				C( mParserGen.Seq(Match, ((-SpecialToken(",") | (-NLs_Token)) +Match)[0, null]) )
					.Modify(mList.List)
					.ModifyS(mSPO_AST.MatchTuple)
					.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				IgnoreMatch.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				Literal.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				Ident.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				MatchPrefix.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				MatchGuard.Cast<mSPO_AST.tMatchItemNode<tSpan>>()
			).ModifyS(mSPO_AST.UnTypedMatch)
		);
		
		Expression.Def(
			mParserGen.OneOf(
				If.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				IfMatch.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Block.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Lambda.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Method.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Call.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Tuple.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Prefix.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				VarToVal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				C( Expression ).Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Literal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Ident.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Type.Cast<mSPO_AST.tExpressionNode<tSpan>>()
			)
		);
		
		ExpressionInCall.Def(
			mParserGen.OneOf(
				Block.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Tuple.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				C( Expression ).Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Literal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Ident.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Type.Cast<mSPO_AST.tExpressionNode<tSpan>>()
			)
		);
		
		// TODO: Macros, Streaming, Block, ...
		Command.Def(
			mParserGen.OneOf(
				Def.Cast<mSPO_AST.tCommandNode<tSpan>>(),
				DefVar.Cast<mSPO_AST.tCommandNode<tSpan>>(),
				MethodCallStatment.Cast<mSPO_AST.tCommandNode<tSpan>>(),
				RecLambda.Cast<mSPO_AST.tCommandNode<tSpan>>(),
				ReturnIf.Cast<mSPO_AST.tCommandNode<tSpan>>(),
				Return.Cast<mSPO_AST.tCommandNode<tSpan>>()
			) +- NLs_Token.OrFail()
		);
	}
}
