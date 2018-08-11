//IMPORT mSPO_AST.cs
//IMPORT mTextParser.cs

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

using tPos = mTextStream.tPos;
using tSpan = mStd.tSpan<mTextStream.tPos>;
using tError = mTextStream.tError;

public static class mSPO_Parser {
	
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> Char = a => mTextParser.GetChar(a);
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> NotChar = a => mTextParser.GetNotChar(a);
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharIn = a => mTextParser.GetCharIn(a);
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharNotIn = a => mTextParser.GetCharNotIn(a);
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar, tChar> CharInRange = (a1, a2) => mTextParser.GetCharInRange(a1, a2);
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tText, tError>, tText> Text = a => mTextParser.GetToken(a);
	
	public static readonly mParserGen.tParser<tPos, tChar, tChar, tError>
	_ = CharIn(" \t")
	.SetName(nameof(_));
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<tChar>, tError>
	__ = _[0, null]
	.SetName(nameof(__));
	
	public static readonly mParserGen.tParser<tPos, tChar, mStd.tEmpty, tError>
	NL = -mParserGen.Seq(__, Char('\n'), __)[1, null]
	.SetName(nameof(NL));
	
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tText, tError>, tText>
	Token = a => (Text(a) + -__)
	.SetName("\"" + a + "\"");
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tTextNode<tSpan>, tError>
	String = mParserGen.Seq(Char('"'), NotChar('"')[0, null], Char('"'))
	.Modify((_, aChars, __) => aChars.Reduce("", (aText, aChar) => aText + aChar))
	.ModifyS(mSPO_AST.Text)
	.SetName(nameof(String));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Digit = CharInRange('0', '9')
	.Modify(aChar => (int)aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Nat = mParserGen.Seq(Digit, (-Char('_')[0, null] +Digit)[0, null])
	.Modify((aFirst, aRest) => aRest.Reduce(aFirst, (aNumber, aDigit) => 10*aNumber+aDigit))
	.SetName(nameof(Nat));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	PosSignum = Char('+')
	.Modify(_ => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	NegSignum = Char('-')
	.Modify(_ => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Int = mParserGen.Seq(Signum, Nat)
	.Modify((aSig, aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tNumberNode<tSpan>, tError>
	Num = (Int | Nat)
	.ModifyS(mSPO_AST.Number)
	.SetName(nameof(Num));
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tEmptyNode<tSpan>, tError>
	Empty = (-Token("()"))
	.ModifyS(mSPO_AST.Empty)
	.SetName(nameof(Empty));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIgnoreMatchNode<tSpan>, tError>
	IgnoreMatch = (-Token("_"))
	.ModifyS(mSPO_AST.IgnoreMatch)
	.SetName(nameof(IgnoreMatch));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIdentNode<tSpan>, tError>
	Ident = ( ( CharNotIn(SpazialChars).Modify(aChar => "" + aChar) | Text("...") )[1, null] +-__ )
	.Modify(aChars => aChars.Join((a1, a2) => a1 + a2))
	.Assert(
		aText => aText != "=>" && aText != "_",
		a => new tError { Pos = a.Span.Start, Message = $"invalid identifyer '{a.Value}'" }
	)
	.ModifyS(mSPO_AST.Ident)
	.SetName(nameof(Ident));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tLiteralNode<tSpan>, tError>
	Literal = (
		mParserGen.OneOf(
			Empty.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			Num.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			String.Cast<mSPO_AST.tLiteralNode<tSpan>>()
		) +-__
	)
	.SetName(nameof(Literal));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tOut, tError>
	C<tOut>(
		mParserGen.tParser<tPos, tChar, tOut, tError> aParser
	//================================================================================
	) => (
		(-Token("(") + (-NL + (aParser + (-NL -Token(")"))))) |
		(-Token("(") +(aParser + -Token(")")))
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>
	ExpressionInCall = mParserGen.UndefParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>()
	.SetName(nameof(ExpressionInCall));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>
	Expression = mParserGen.UndefParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>()
	.SetName(nameof(Expression));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMatchNode<tSpan>, tError>
	UnTypedMatch = mParserGen.UndefParser<tPos, tChar, mSPO_AST.tMatchNode<tSpan>, tError>()
	.SetName(nameof(UnTypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMatchNode<tSpan>, tError>
	TypedMatch = mParserGen.Seq(UnTypedMatch.Cast<mSPO_AST.tMatchItemNode<tSpan>>(), (-Token("€") +Expression.OrFail()))
	.ModifyS(mSPO_AST.Match)
	.SetName(nameof(TypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMatchNode<tSpan>, tError>
	Match = (TypedMatch | UnTypedMatch)
	.SetName(nameof(Match));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tDefNode<tSpan>, tError>
	Def = mParserGen.Seq(Token("§DEF"), Match.OrFail(), Token("=").OrFail(), Expression.OrFail())
	.Modify((_, aMatch, __, aExpression) => (aMatch, aExpression))
	.ModifyS(mSPO_AST.Def)
	.SetName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tReturnIfNode<tSpan>, tError>
	ReturnIf = mParserGen.Seq(Token("§RETURN"), Expression.OrFail(), Token("IF"), Expression.OrFail())
	.Modify((_, aResult, __, aCond) => (aResult, aCond))
	.ModifyS(mSPO_AST.ReturnIf)
	.SetName(nameof(ReturnIf));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tReturnIfNode<tSpan>, tError>
	Return = (-Token("§RETURN") +Expression.OrFail())
	.ModifyS((aSpan, a) => mSPO_AST.ReturnIf(aSpan, a, mSPO_AST.True(aSpan)))
	.SetName(nameof(Return));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tCommandNode<tSpan>, tError>
	Command = mParserGen.UndefParser<tPos, tChar, mSPO_AST.tCommandNode<tSpan>, tError>()
	.SetName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<mSPO_AST.tCommandNode<tSpan>>, tError>
	Commands = Command[0, null]
	.SetName(nameof(Commands));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tBlockNode<tSpan>, tError>
	Block = mParserGen.Seq(Token("{"), NL.OrFail(), Commands.OrFail(), Token("}").OrFail())
	.Modify((_, __, aCommands, ___) => aCommands)
	.ModifyS(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>
	Tuple = C( mParserGen.Seq(ExpressionInCall, ((-Token(",") | -NL) +ExpressionInCall)[1, null]) )
	.Modify(mList.List)
	.ModifyS(mSPO_AST.Tuple)
	.SetName(nameof(Tuple));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, (mSPO_AST.tIdentNode<tSpan> Ident, mList.tList<tChild> Childs), tError>
	Infix<tChild>(
		mParserGen.tParser<tPos, tChar, tChild, tError> aChildParser
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
	public static mParserGen.tParser<tPos, tChar, (mSPO_AST.tIdentNode<tSpan> Ident, mList.tList<tChild> Childs), tError>
	Infix<tChild>(
		tText aPrefix,
		mParserGen.tParser<tPos, tChar, tChild, tError> aChildParser
	//================================================================================
	) => (
		(-Token(aPrefix) +Infix(aChildParser))
	) | (
		mParserGen.Seq(aChildParser, Token(aPrefix), Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstChild, _, aInfix) => (
				Ident: mSPO_AST.Ident(aSpan, "..." + aInfix.Ident.Name.Substring(1)),
				Childs: mList.List(aFirstChild, aInfix.Childs)
			)
		)
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tCallNode<tSpan>, tError>
	Call = (
		Infix(".", ExpressionInCall).ModifyS(
			(aSpan, a) => mSPO_AST.Call(aSpan, a.Ident, mSPO_AST.Tuple(aSpan, a.Childs))
		) | (
			-Token(".") +(ExpressionInCall +ExpressionInCall).ModifyS(mSPO_AST.Call)
		)
	)
	.SetName(nameof(Call));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tPrefixNode<tSpan>, tError>
	Prefix = Infix("#", ExpressionInCall)
	.ModifyS((aSpan, aIdent, aChilds) => mSPO_AST.Prefix(aSpan, aIdent, mSPO_AST.Tuple(aSpan, aChilds)))
	.SetName(nameof(Prefix));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMatchPrefixNode<tSpan>, tError>
	MatchPrefix = C( Infix("#", UnTypedMatch) )
	.ModifyS(
		(aSpan, aIdent, aChilds) => mSPO_AST.MatchPrefix(
			aSpan,
			aIdent,
			mSPO_AST.Match(aSpan, mSPO_AST.MatchTuple(aSpan, aChilds), null)
		)
	)
	.SetName(nameof(MatchPrefix));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMatchGuardNode<tSpan>, tError>
	MatchGuard = C( mParserGen.Seq(UnTypedMatch, Token("|"), Expression.OrFail()) )
	.Modify((a1, _ , a2) => (a1, a2))
	.ModifyS(mSPO_AST.MatchGuard)
	.SetName(nameof(MatchGuard));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tEmptyTypeNode<tSpan>, tError>
	EmptyType = (-Token("[]"))
	.ModifyS(mSPO_AST.EmptyType)
	.SetName(nameof(EmptyType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tBoolTypeNode<tSpan>, tError>
	BoolType = (-Token("§BOOL"))
	.ModifyS(mSPO_AST.BoolType)
	.SetName(nameof(BoolType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIntTypeNode<tSpan>, tError>
	IntType = (-Token("§INT"))
	.ModifyS(mSPO_AST.IntType)
	.SetName(nameof(IntType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tTypeTypeNode<tSpan>, tError>
	TypeType = (-Token("[[]]"))
	.ModifyS(mSPO_AST.TypeType)
	.SetName(nameof(TypeType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tPrefixTypeNode<tSpan>, tError>
	PrefixType = mParserGen.Seq(Token("["), Infix("#", Expression), Token("]").OrFail())
	.Modify((_, aInfix, __) => aInfix)
	.ModifyS(mSPO_AST.PrefixType)
	.SetName(nameof(PrefixType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tTupleTypeNode<tSpan>, tError>
	TupleType = mParserGen.Seq(
		Token("["),
		NL[0, 1],
		Expression,
		((-Token(",") | -NL) +Expression.OrFail())[1, null],
		-NL[0, 1] +Token("]").OrFail()
	)
	.Modify((_, __, aFirst, aRest, ___) => mList.List(aFirst, aRest))
	.ModifyS(mSPO_AST.TupleType)
	.SetName(nameof(TupleType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tSetTypeNode<tSpan>, tError>
	SetType = mParserGen.Seq(
		Token("["),
		NL[0, 1],
		Expression +-Token("|"),
		(-NL[0, 1] +Expression)[1, null].OrFail(),
		Token("|")[0, 1] +-(-NL[0, 1] +Token("]").OrFail())
	)
	.Modify((_, __, aFirst, aRest, ___) => mList.List(aFirst, aRest))
	.ModifyS(mSPO_AST.SetType)
	.SetName(nameof(SetType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tLambdaTypeNode<tSpan>, tError>
	LambdaType = mParserGen.Seq(
		Token("["),
		Expression,
		Token("=>"),
		Expression.OrFail(),
		Token("]").OrFail()
	)
	.Modify((_, a1, __, a2, ___) => (a1, a2))
	.ModifyS(mSPO_AST.LambdaType)
	.SetName(nameof(LambdaType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tRecursiveTypeNode<tSpan>, tError>
	RecursiveType = mParserGen.Seq(
		Token("["),
		Token("§RECURSIV"),
		Ident.OrFail(),
		Expression.OrFail(),
		Token("]").OrFail()
	)
	.Modify((_, __, aIdent, aExpression, ___) => (aIdent, aExpression))
	.ModifyS(mSPO_AST.RecursiveType)
	.SetName(nameof(RecursiveType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tInterfaceTypeNode<tSpan>, tError>
	InterfaceType = mParserGen.Seq(
		Token("["),
		Token("§INTERFACE"),
		Ident.OrFail(),
		Expression.OrFail(),
		Token("]").OrFail()
	)
	.Modify((_, __, aIdent, aExpression, ___) => (aIdent, aExpression))
	.ModifyS(mSPO_AST.InterfaceType)
	.SetName(nameof(InterfaceType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tGenericTypeNode<tSpan>, tError>
	GenericType = mParserGen.Seq(
		Token("["),
		Token("§GENERIC"),
		Ident,
		Expression,
		Token("]").OrFail()
	)
	.Modify((_, __, aIdent, aExpression, ___) => (aIdent, aExpression))
	.ModifyS(mSPO_AST.GenericType)
	.SetName(nameof(GenericType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>
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
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tLambdaNode<tSpan>, tError>
	Lambda = mParserGen.Seq(Match, Token("=>"), Expression.OrFail())
	.Modify((aMatch, _, aExpression) => (aMatch, aExpression))
	.ModifyS(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMethodNode<tSpan>, tError>
	Method = mParserGen.Seq(Match, Token(":"), Match.OrFail(), Block.OrFail())
	.Modify((aObjMatch, _, aArgMatch, aBlock) => (aObjMatch, aArgMatch, aBlock))
	.ModifyS(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tRecLambdaItemNode<tSpan>, tError>
	RecLambdaItem = mParserGen.Seq(__, Ident, Token("="), Lambda | C( Lambda ))
	.Modify((_, aIdent, __, aLambda) => (aIdent, aLambda))
	.ModifyS(mSPO_AST.RecLambdaItem)
	.SetName(nameof(RecLambdaItem));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tRecLambdasNode<tSpan>, tError>
	RecLambda = (
		mParserGen.Seq(
			Token("§RECURSIV"),
			Token("{"),
			NL,
			(RecLambdaItem +-NL)[1, null],
			Token("}").OrFail()
		).Modify((_, __, ___, a, ____) => a) |
		(-Token("§RECURSIV") +RecLambdaItem[1, 1].OrFail())
	)
	.ModifyS(mSPO_AST.RecLambdas)
	.SetName(nameof(RecLambda));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIfNode<tSpan>, tError>
	If = mParserGen.Seq(
		Token("§IF"),
		Token("{"),
		NL,
		mParserGen.Seq(
			__,
			Expression,
			Token("=>"),
			Expression.OrFail(),
			NL.OrFail()
		).Modify((_, aCond, __, aRes, ___) => (aCond, aRes))[0, null],
		Token("}").OrFail()
	)
	.Modify((_, __, ___, a, ____) => a)
	.ModifyS(mSPO_AST.If)
	.SetName(nameof(If));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIfMatchNode<tSpan>, tError>
	IfMatch = mParserGen.Seq(
		Token("§IF"),
		Expression,
		Token("MATCH") +-(Token("{") +-NL),
		mParserGen.Seq(
			__,
			Match,
			Token("=>"),
			Expression,
			NL.OrFail()
		).Modify((_, aMatch, __, aExpression, ___) => (aMatch, aExpression))[0, null],
		Token("}").OrFail()
	)
	.Modify((_, aExpression, __, aBranches, ___) => (aExpression, aBranches))
	.ModifyS(mSPO_AST.IfMatch)
	.SetName(nameof(IfMatch));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMethodCallNode<tSpan>, tError>
	MethodCall = mParserGen.Seq(
		Infix(ExpressionInCall),
		(-Token("=>") +(-Token("§DEF") +Match))[0, 1].Modify(aMatches => aMatches?.First())
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
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<mSPO_AST.tMethodCallNode<tSpan>>, tError>
	MethodCalls = mParserGen.Seq(
		MethodCall,
		((-Token(",")|-NL) +MethodCall)[0, null],
		NL[0, 1],
		Token(".").OrFail()
	)
	.Modify((aFirst, aRest, _, __) => mList.List(aFirst, aRest))
	.SetName(nameof(MethodCalls));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tDefVarNode<tSpan>, tError>
	DefVar = mParserGen.Seq(
		Token("§VAR"),
		Ident,
		Token(":") +(-NL[0, 1] -Token("=")),
		Expression.OrFail(),
		(
			((-Token(",") | -NL) +MethodCalls) |
			Token(".").OrFail().Modify(a => mList.List<mSPO_AST.tMethodCallNode<tSpan>>())
		)
	)
	.Modify((_, aIdent, __, aFirst, aRest) => (aIdent, aFirst, aRest))
	.ModifyS(mSPO_AST.DefVar)
	.SetName(nameof(DefVar));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tVarToValNode<tSpan>, tError>
	VarToVal = (-Token("§TO_VAL") +Expression.OrFail())
	.ModifyS(mSPO_AST.VarToVal)
	.SetName(nameof(VarToVal));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMethodCallsNode<tSpan>, tError>
	MethodCallStatment = mParserGen.Seq(
		ExpressionInCall,
		Token(":"),
		NL[0, 1],
		MethodCalls.OrFail()
	)
	.Modify((aObj, _, __, aMethodCalls) => (aObj, aMethodCalls))
	.ModifyS(mSPO_AST.MethodCallStatment)
	.SetName(nameof(MethodCallStatment));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tImportNode<tSpan>, tError>
	Import = (-Token("§IMPORT") +(Match +-NL).OrFail())
	.ModifyS(mSPO_AST.Import)
	.SetName(nameof(Import));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExportNode<tSpan>, tError>
	Export = (-Token("§EXPORT") +Expression.OrFail())
	.ModifyS(mSPO_AST.Export)
	.SetName(nameof(Export));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tModuleNode<tSpan>, tError>
	Module = mParserGen.Seq(
		NL[0, 1],
		Import,
		Commands,
		Export,
		NL[0, 1]
	)
	.Modify((_, aImport, aCommands, aExports, __) => (aImport, aCommands, aExports))
	.ModifyS(mSPO_AST.Module)
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		UnTypedMatch.Def(
			mParserGen.OneOf(
				C( mParserGen.Seq(Match, ((-Token(",") | (-NL)) +Match)[0, null]) )
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
			) +- NL.OrFail()
		);
	}
}
