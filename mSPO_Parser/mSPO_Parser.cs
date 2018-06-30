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

using tPos = mTextParser.tPos;
using tSpan = mStd.tSpan<mTextParser.tPos>;
using tError = mTextParser.tError;

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
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<tChar>, tError>
	NL = (-__)._(Char('\n'))._(-__)[1, null]
	.SetName(nameof(NL));
	
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tText, tError>, tText>
	Token = a => (Text(a))._(-__)
	.SetName("\"" + a + "\"");
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tTextNode<tSpan>, tError>
	String = (-Char('"'))._(NotChar('"')[0, null])._(-Char('"'))
	.Modify(aChars => aChars.Reduce("", (aText, aChar) => aText + aChar))
	.ModifyS(mSPO_AST.Text)
	.SetName(nameof(String));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Digit = CharInRange('0', '9')
	.Modify(aChar => (int)aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Nat = (Digit)._((-Char('_')[0, null])._(Digit)[0, null])
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
	Int = (Signum)._(Nat)
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
	Ident = ( ( CharNotIn(SpazialChars).ModifyS((aSpan, aChar) => "" + aChar) | Text("...") )[1, null] )._( -__ )
	.Modify(aChars => aChars.Join((a1, a2) => a1 + a2))
	.Assert(
		aText => aText != "=>" && aText != "_",
		a => new tError { Pos = a.Span.Start, Message = $"invalid identifyer '{a.Value}'" }
	)
	.ModifyS(mSPO_AST.Ident)
	.SetName(nameof(Ident));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tLiteralNode<tSpan>, tError>
	Literal = (
		Empty.Modify(a => (mSPO_AST.tLiteralNode<tSpan>)a)
		.Or(Num)
		.Or(String)
	)._(
		-__
	)
	.SetName(nameof(Literal));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tOut, tError>
	C<tOut>(
		mParserGen.tParser<tPos, tChar, tOut, tError> aParser
	//================================================================================
	) => (
		(-Token("("))._(-NL)._(aParser)._(-NL)._(-Token(")")) |
		(-Token("("))._(aParser)._(-Token(")"))
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
	TypedMatch = (UnTypedMatch.ModifyS((_, a) => (mSPO_AST.tMatchItemNode<tSpan>)a))._(-Token("€"))._(Expression.OrFail())
	.ModifyS(mSPO_AST.Match)
	.SetName(nameof(TypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMatchNode<tSpan>, tError>
	Match = (TypedMatch | UnTypedMatch)
	.SetName(nameof(Match));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tDefNode<tSpan>, tError>
	Def = (-Token("§DEF"))._((Match)._(-Token("="))._(Expression).OrFail())
	.ModifyS(mSPO_AST.Def)
	.SetName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tReturnIfNode<tSpan>, tError>
	ReturnIf = (-Token("§RETURN"))._(Expression.OrFail())._(-Token("IF"))._(Expression.OrFail())
	.ModifyS(mSPO_AST.ReturnIf)
	.SetName(nameof(ReturnIf));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tReturnIfNode<tSpan>, tError>
	Return = (-Token("§RETURN"))._(Expression.OrFail())
	.ModifyS((aSpan, a) => mSPO_AST.ReturnIf(aSpan, a, mSPO_AST.True(aSpan)))
	.SetName(nameof(Return));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tCommandNode<tSpan>, tError>
	Command = mParserGen.UndefParser<tPos, tChar, mSPO_AST.tCommandNode<tSpan>, tError>()
	.SetName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<mSPO_AST.tCommandNode<tSpan>>, tError>
	Commands = Command[0, null]
	.SetName(nameof(Commands));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tBlockNode<tSpan>, tError>
	Block = (-Token("{"))._((-NL)._(Commands)._(-Token("}")).OrFail())
	.ModifyS(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>
	Tuple = C( (ExpressionInCall)._((-Token(",") | -NL)._(ExpressionInCall)[1, null]) )
	.Modify(mList.List)
	.ModifyS(mSPO_AST.Tuple)
	.SetName(nameof(Tuple));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, (mSPO_AST.tIdentNode<tSpan> Ident, mList.tList<tChild> Childs), tError>
	Infix<tChild>(
		mParserGen.tParser<tPos, tChar, tChild, tError> aChildParser
	//================================================================================
	) => (
		(Ident)._(((aChildParser)._(Ident)[0, null])._(aChildParser[0, 1]).OrFail())
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
		(-Token(aPrefix))._(Infix(aChildParser))
	) | (
		(aChildParser)._(-Token(aPrefix))._(Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstChild, aIdent, aLastChilds) => (
				mSPO_AST.Ident(aSpan, "..." + aIdent.Name.Substring(1)),
				mList.List(aFirstChild, aLastChilds)
			)
		)
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tCallNode<tSpan>, tError>
	Call = (
		Infix(".", ExpressionInCall).ModifyS(
			(aSpan, a) => mSPO_AST.Call(aSpan, a.Ident, mSPO_AST.Tuple(aSpan, a.Childs))
		) |
		(-Token("."))._(ExpressionInCall)._(ExpressionInCall).ModifyS(mSPO_AST.Call)
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
	MatchGuard = C( (UnTypedMatch)._(-Token("|"))._(Expression.OrFail()) )
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
	PrefixType = (-Token("["))._(Infix("#", Expression))._(-Token("]"))
	.ModifyS(mSPO_AST.PrefixType)
	.SetName(nameof(PrefixType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tTupleTypeNode<tSpan>, tError>
	TupleType = (-Token("["))._(-NL[0, 1])._(Expression)._(
		(-Token(",") | -NL)._(Expression)[1, null]
	)._(-NL[0, 1])._(-Token("]"))
	.Modify(mList.List)
	.ModifyS(mSPO_AST.TupleType)
	.SetName(nameof(TupleType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tSetTypeNode<tSpan>, tError>
	SetType = (-Token("["))._(-NL[0, 1])._(Expression)._(
		(-Token("|"))._(-NL[0, 1])._(Expression)[1, null]
	)._(-Token("|")[0, 1])._(-NL[0, 1])._(-Token("]"))
	.Modify(mList.List)
	.ModifyS(mSPO_AST.SetType)
	.SetName(nameof(SetType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tLambdaTypeNode<tSpan>, tError>
	LambdaType = (-Token("["))._(Expression)._(-Token("=>"))._(Expression)._(-Token("]"))
	.ModifyS(mSPO_AST.LambdaType)
	.SetName(nameof(LambdaType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tRecursiveTypeNode<tSpan>, tError>
	RecursiveType = (-Token("["))._(-Token("§RECURSIV"))._(Ident)._(Expression)._(-Token("]"))
	.ModifyS(mSPO_AST.RecursiveType)
	.SetName(nameof(RecursiveType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tInterfaceTypeNode<tSpan>, tError>
	InterfaceType = (-Token("["))._(-Token("§INTERFACE"))._(Ident)._(Expression)._(-Token("]"))
	.ModifyS(mSPO_AST.InterfaceType)
	.SetName(nameof(InterfaceType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tGenericTypeNode<tSpan>, tError>
	GenericType = (-Token("["))._(-Token("§GENERIC"))._(Ident)._(Expression)._(-Token("]"))
	.ModifyS(mSPO_AST.GenericType)
	.SetName(nameof(GenericType));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExpressionNode<tSpan>, tError>
	Type = (
		EmptyType.Modify(a => (mSPO_AST.tExpressionNode<tSpan>)a)
		.Or(BoolType)
		.Or(IntType)
		.Or(TypeType)
		.Or(PrefixType)
		.Or(TupleType)
		.Or(SetType)
		.Or(LambdaType)
		.Or(RecursiveType)
		.Or(InterfaceType)
		.Or(GenericType)
	)
	.SetName(nameof(Type));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tLambdaNode<tSpan>, tError>
	Lambda = (Match)._(-Token("=>"))._(Expression.OrFail())
	.ModifyS(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMethodNode<tSpan>, tError>
	Method = (Match)._(-Token(":"))._(Match)._(Block)
	.ModifyS(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tRecLambdaItemNode<tSpan>, tError>
	RecLambdaItem = (-__)._(Ident)._(-Token("="))._((Lambda | C( Lambda )))
	.ModifyS(mSPO_AST.RecLambdaItem)
	.SetName(nameof(RecLambdaItem));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tRecLambdasNode<tSpan>, tError>
	RecLambda = (
		(-Token("§RECURSIV"))._(-Token("{"))._(-NL)._(((RecLambdaItem)._(-NL)[1, null])._(-Token("}")).OrFail()) |
		(-Token("§RECURSIV"))._(RecLambdaItem[1, 1].OrFail())
	)
	.ModifyS(mSPO_AST.RecLambdas)
	.SetName(nameof(RecLambda));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIfNode<tSpan>, tError>
	If = (-Token("§IF"))._(-Token("{"))._(-NL)._(
		(
			(-__)._(Expression)._(-Token("=>"))._((Expression)._(-NL).OrFail())[0, null]
		)._(-Token("}")).OrFail()
	)
	.ModifyS(mSPO_AST.If)
	.SetName(nameof(If));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tIfMatchNode<tSpan>, tError>
	IfMatch = (-Token("§IF"))._(
		(Expression)._(-Token("MATCH"))._(-Token("{"))._(-NL)._(
			(-__)._(Match)._(
				(-Token("=>"))._(Expression)._(-NL).OrFail()
			)[0, null]
		)._(-Token("}")).OrFail()
	)
	.ModifyS(mSPO_AST.IfMatch)
	.SetName(nameof(IfMatch));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMethodCallNode<tSpan>, tError>
	MethodCall = (Infix(ExpressionInCall))._(
		(-Token("=>"))._(-Token("§DEF"))._(Match)[0, 1]
		.Modify(aMatches => aMatches?.First())
	)
	.ModifyS(
		(aSpan, aIdent, aChilds, aMaybeOut) => mSPO_AST.MethodCall(
			aSpan,
			aIdent,
			mSPO_AST.Tuple(aSpan, aChilds),
			aMaybeOut
		)
	)
	.SetName(nameof(MethodCall));
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<mSPO_AST.tMethodCallNode<tSpan>>, tError>
	MethodCalls = (MethodCall)._((-Token(",")|-NL)._(MethodCall)[0, null])._(-NL[0, 1])._(-Token("."))
	.Modify(mList.List)
	.SetName(nameof(MethodCalls));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tDefVarNode<tSpan>, tError>
	DefVar = (-Token("§VAR"))._(
		(Ident)._(-Token(":"))._(-NL[0, 1])._(-Token("="))._(Expression)._(
			(-Token(",") | -NL)._(MethodCalls) |
			Token(".").Modify(a => mList.List<mSPO_AST.tMethodCallNode<tSpan>>())
		).OrFail()
	)
	.ModifyS(mSPO_AST.DefVar)
	.SetName(nameof(DefVar));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tVarToValNode<tSpan>, tError>
	VarToVal = (-Token("§TO_VAL"))._(Expression)
	.ModifyS(mSPO_AST.VarToVal)
	.SetName(nameof(VarToVal));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tMethodCallsNode<tSpan>, tError>
	MethodCallStatment = (ExpressionInCall)._(-Token(":"))._(-NL[0, 1])._(MethodCalls.OrFail())
	.ModifyS(mSPO_AST.MethodCallStatment)
	.SetName(nameof(MethodCallStatment));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tImportNode<tSpan>, tError>
	Import = (-Token("§IMPORT"))._((Match)._(-NL).OrFail())
	.ModifyS(mSPO_AST.Import)
	.SetName(nameof(Import));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tExportNode<tSpan>, tError>
	Export = (-Token("§EXPORT"))._(Expression.OrFail())
	.ModifyS(mSPO_AST.Export)
	.SetName(nameof(Export));
	
	public static readonly mParserGen.tParser<tPos, tChar, mSPO_AST.tModuleNode<tSpan>, tError>
	Module = (-NL[0, 1])._(Import)._(Commands)._(Export)._(-NL[0, 1])
	.ModifyS(mSPO_AST.Module)
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		UnTypedMatch.Def(
			(
				C( (Match)._(((-Token(",")).Or(-NL))._(Match)[0, null]) )
				.Modify(mList.List)
				.ModifyS(mSPO_AST.MatchTuple)
			).Or(
				IgnoreMatch.Modify(a => (mSPO_AST.tMatchItemNode<tSpan>)a)
				.Or(Literal)
				.Or(Ident)
				.Or(MatchPrefix)
				.Or(MatchGuard)
			).ModifyS(mSPO_AST.UnTypedMatch)
		);
		
		Expression.Def(
			If.Modify(a => (mSPO_AST.tExpressionNode<tSpan>)a)
			.Or(IfMatch)
			.Or(Block)
			.Or(Lambda)
			.Or(Method)
			.Or(Call)
			.Or(Tuple)
			.Or(Prefix)
			.Or(VarToVal)
			.Or(C( Expression ))
			.Or(Literal)
			.Or(Ident)
			.Or(Type)
		);
		
		ExpressionInCall.Def(
			Block.Modify(a => (mSPO_AST.tExpressionNode<tSpan>)a)
			.Or(Tuple)
			.Or(C( Expression ))
			.Or(Literal)
			.Or(Ident)
			.Or(Type)
		);
		
		// TODO: Macros, Streaming, Block, ...
		Command.Def(
			Def.Modify(a => (mSPO_AST.tCommandNode<tSpan>)a)
			.Or(DefVar)
			.Or(MethodCallStatment)
			.Or(RecLambda)
			.Or(ReturnIf)
			.Or(Return)
			._(-NL.OrFail())
		);
	}
}
