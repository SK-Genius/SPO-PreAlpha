using tToken = mTokenizer.tToken;
using tTokenType = mTokenizer.tTokenType;

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class
mSPO_Parser {
	private static readonly mParserGen.tParser<tPos, tToken, mStd.tEmpty, tError>
	NLs_Token = mTokenizer.NLs_Token;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	TextToken = mTokenizer.TextToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NumberToken = mTokenizer.NumberToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	IdToken = mTokenizer.IdToken;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	SpecialToken = mTokenizer.SpecialToken;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tChar>
	SpecialId = mTokenizer.SpecialId;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tChar, tText>
	SpecialId_ = mTokenizer.SpecialId;
	
	private static readonly mStd.tFunc<mParserGen.tParser<tPos, tToken, tToken, tError>, tText>
	Token = mTokenizer.Token_;
	
	private static mParserGen.tParser<tPos, tToken, tToken, tError>
	KeyWord(
		tText aId
	) => SpecialId_('§', aId);
	
	public static mParserGen.tParser<tPos, tToken, tOut, tError>
	SetName<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aName
	) => aParser.AddError(
		a => (a.Span.Start, $"invalid {aName}")
	)
	.SetDebugName(aName);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tEmptyNode<tSpan>, tError>
	Empty = (-SpecialToken("(") -SpecialToken(")"))
	.ModifyS(mSPO_AST.Empty)
	.SetName(nameof(Empty));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIgnoreMatchNode<tSpan>, tError>
	IgnoreMatch = IdToken
	.Assert(a => a.Type == tTokenType.Id && a.Text == "_", a => (a.Span.Start, "expected _"))
	.ModifyS(mSPO_AST.IgnoreMatch)
	.SetName(nameof(IgnoreMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIdNode<tSpan>, tError>
	Id = IdToken
	.ModifyS(mTokenizer.X(mSPO_AST.Id))
	.SetName(nameof(Id));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIntNode<tSpan>, tError>
	Number = NumberToken
	.Modify(a => tInt32.Parse(a.Text))
	.ModifyS(mSPO_AST.Int)
	.SetName(nameof(Number));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTextNode<tSpan>, tError>
	Text = TextToken
	.ModifyS(mTokenizer.X(mSPO_AST.Text))
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
	
	public static mParserGen.tParser<tPos, tToken, tOut, tError>
	C<tOut>(
		mParserGen.tParser<tPos, tToken, tOut, tError> aParser
	) => (
		(-SpecialToken("(") + (-NLs_Token + (aParser + (-NLs_Token -SpecialToken(")"))))) |
		(-SpecialToken("(") +(aParser + -SpecialToken(")")))
	);
	
	public static mParserGen.tParser<tPos, tToken, tOut, tError>
	E<tOut>(
		mParserGen.tParser<tPos, tToken, tOut, tError> aParser
	) => (
		(-SpecialToken("[") + (-NLs_Token + (aParser + (-NLs_Token -SpecialToken("]"))))) |
		(-SpecialToken("[") +(aParser + -SpecialToken("]")))
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	ExpressionInCall = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(ExpressionInCall));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Expression = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(Expression));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>
	UnTypedMatch = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(UnTypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>
	TypedMatch = mParserGen.Seq(UnTypedMatch.Cast<mSPO_AST.tMatchItemNode<tSpan>>(), (-SpecialToken("€") +Expression).Modify(mMaybe.Some))
	.ModifyS(mSPO_AST.Match)
	.SetName(nameof(TypedMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchNode<tSpan>, tError>
	Match = (TypedMatch | UnTypedMatch)
	.SetName(nameof(Match));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	PipeToRight = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(PipeToRight));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	PipeToLeft = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(PipeToLeft));

	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	PipeExpression = PipeToRight | PipeToLeft;
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tDefNode<tSpan>, tError>
	Def = mParserGen.Seq(Match, Token("="), PipeExpression | Expression)
	.Modify((aMatch, _, aExpression) => (aMatch, aExpression))
	.ModifyS(mSPO_AST.Def)
	.SetName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tReturnIfNode<tSpan>, tError>
	ReturnIf = mParserGen.Seq(KeyWord("RETURN"), Expression, Token("IF"), Expression)
	.Modify((_, aResult, _, aCond) => (aCond, aResult))
	.ModifyS(mSPO_AST.ReturnIf)
	.SetName(nameof(ReturnIf));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tReturnIfNode<tSpan>, tError>
	Return = (-KeyWord("RETURN") +(PipeExpression | Expression))
	.ModifyS((aSpan, a) => mSPO_AST.ReturnIf(aSpan, mSPO_AST.True(aSpan), a))
	.SetName(nameof(Return));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCommandNode<tSpan>, tError>
	Command = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tCommandNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mSPO_AST.tCommandNode<tSpan>>?, tError>
	Commands = Command[0..]
	.SetName(nameof(Commands));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tBlockNode<tSpan>, tError>
	Block = mParserGen.Seq(SpecialToken("{"), NLs_Token, Commands, SpecialToken("}"))
	.Modify((_, _, aCommands, _) => aCommands)
	.ModifyS(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Tuple = C( mParserGen.Seq(PipeExpression | ExpressionInCall, ((-SpecialToken(",") | -NLs_Token) +(PipeExpression | ExpressionInCall))[1..]) )
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.Tuple)
	.SetName(nameof(Tuple));
	
	public static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdNode<tSpan> Id, mStream.tStream<tChild>? Childs), tError>
	Infix<tChild>(
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	) => (
		mParserGen.Seq(mParserGen.Seq(aChildParser, Id)[0..], aChildParser[0..1])
		.ModifyS(
			(aSpan, aList, aLastChild) => (
				mSPO_AST.Id(
					aSpan,
					aList.Map(
						a => a.Item2.Id[1..]
					).Reduce(
						"",
						(a1, a2) => $"{a1}...{a2}"
					) + (aLastChild.IsEmpty() ? "" : "...")
				),
				mStream.Concat(aList.Map(a => a.Item1), aLastChild)
			)
		)
	);
	
	public static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdNode<tSpan> Id, mStream.tStream<tChild>? Childs), tError>
	InfixPrefix<tChild>(
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	) => (
		mParserGen.Seq(SpecialId('#'), Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstId, aInfix) => (
				Id: mSPO_AST.Id(aSpan, aFirstId.Text[1..] + aInfix.Id.Id[1..]),
				Childs: aInfix.Childs
			)
		)
	) | (
		mParserGen.Seq(aChildParser, SpecialId('#'), Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstChild, aFirstId, aInfix) => (
				Id: mSPO_AST.Id(aSpan, "..." + aFirstId.Text[1..] + aInfix.Id.Id[1..]),
				Childs: mStream.Stream(aFirstChild, aInfix.Childs)
			)
		)
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCallNode<tSpan>, tError>
	Call = (
		(
			(
				mParserGen.Seq(
					SpecialToken("."), Id, Infix(ExpressionInCall)
				).ModifyS(
					(aSpan, _, aFirst, aInfix) => (
						Id: mSPO_AST.Id(
							mSpan.Span(
								mTextStream.Pos(aSpan.Start.Id, aSpan.Start.Row, aSpan.Start.Col + 1),
								aSpan.End
							),
							aFirst.Id[1..] + aInfix.Id.Id[1..]
						),
						Childs: aInfix.Childs
					)
				)
			) | (
				mParserGen.Seq(ExpressionInCall, SpecialToken("."), Id, Infix(ExpressionInCall))
				.ModifyS(
					(aSpan, aFirstChild, _, aFirst, aInfix) => (
						Id: mSPO_AST.Id(aSpan, "..." + aFirst.Id[1..] + aInfix.Id.Id[1..]),
						Childs: mStream.Stream(aFirstChild, aInfix.Childs)
					)
				)
			)
		).ModifyS(
			(aSpan, a) => mSPO_AST.Call(aSpan, a.Id, mSPO_AST.Tuple(aSpan, a.Childs))
		) | (
			-SpecialToken(".") +(ExpressionInCall +ExpressionInCall).ModifyS(mSPO_AST.Call)
		)
	)
	.SetName(nameof(Call));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPrefixNode<tSpan>, tError>
	Prefix = InfixPrefix(ExpressionInCall)
	.ModifyS((aSpan, aId, aChilds) => mSPO_AST.Prefix(aSpan, aId, mSPO_AST.Tuple(aSpan, aChilds)))
	.SetName(nameof(Prefix));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchFreeIdNode<tSpan>, tError>
	MatchFreeId = (-KeyWord("DEF") +Id)
	.Modify(a => a.Id[1..])
	.ModifyS(mSPO_AST.MatchFreeId)
	.SetName(nameof(MatchFreeId));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchPrefixNode<tSpan>, tError>
	MatchPrefix = C( InfixPrefix(UnTypedMatch) )
	.ModifyS(
		(aSpan, aId, aChilds) => mSPO_AST.MatchPrefix(
			aSpan,
			aId,
			mSPO_AST.Match(aSpan, mSPO_AST.MatchTuple(aSpan, aChilds), mStd.cEmpty)
		)
	)
	.SetName(nameof(MatchPrefix));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecordNode<tSpan>, tError>
	Record = (
		mParserGen.Seq(
			SpecialToken("{") +-NLs_Token[0..1],
			mParserGen.Seq(
				Id,
				SpecialToken(":"),
				Expression
			).Modify((aId, _, aExpression) => (Key: aId, Value: aExpression)),
			mParserGen.Seq(
				-SpecialToken(",") | -NLs_Token,
				Id,
				SpecialToken(":"),
				Expression
			).Modify((_, aId, _, aExpression) => (Key: aId, Value: aExpression))[0..],
			-NLs_Token[0..1] +SpecialToken("}")
		)
		.Modify((_, aHead, aTail, _) => mStream.Stream(aHead, aTail))
		.ModifyS(mSPO_AST.Record) |
		mParserGen.Seq(
			SpecialToken("{"),
			NLs_Token[0..1],
			SpecialToken("}")
		)
		.ModifyS((aSpan, _) => mSPO_AST.Record(aSpan, null))
	)
	.SetName(nameof(Record));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchRecordNode<tSpan>, tError>
	MatchRecord = mParserGen.Seq(
		SpecialToken("{") +-NLs_Token[0..1],
		mParserGen.Seq(
			Id,
			SpecialToken(":"),
			Match
		).Modify((aId, _, aExpression) => (Key: aId, Match: aExpression)),
		mParserGen.Seq(
			-SpecialToken(",") | -NLs_Token,
			Id,
			SpecialToken(":"),
			Match
		).Modify((_, aId, _, aExpression) => (Key: aId, Match: aExpression))[0..],
		-NLs_Token[0..1] +SpecialToken("}")
	)
	.Modify((_, aHead, aTail, _) => mStream.Stream(aHead, aTail))
	.ModifyS(mSPO_AST.MatchRecord)
	.SetName(nameof(Record));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMatchGuardNode<tSpan>, tError>
	MatchGuard = C( mParserGen.Seq(Match, Token("&"), Expression) )
	.Modify((a1, _ , a2) => (a1, a2))
	.ModifyS(mSPO_AST.MatchGuard)
	.SetName(nameof(MatchGuard));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>
	Type = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>(mTextParser.ComparePos)
	.SetName(nameof(Type));
	
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
	PrefixType = E(InfixPrefix(Type))
	.ModifyS(mSPO_AST.PrefixType)
	.SetName(nameof(PrefixType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTupleTypeNode<tSpan>, tError>
	TupleType = E(
		mParserGen.Seq(
			Type,
			((-SpecialToken(",") | -NLs_Token) +Type)[1..]
		)
	)
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.TupleType)
	.SetName(nameof(TupleType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tSetTypeNode<tSpan>, tError>
	SetType = E(
		mParserGen.Seq(
			-Token("|")[0..1] +Type,
			(-(NLs_Token[0..1] +-Token("|") +-NLs_Token[0..1]) +Type)[1..] +-Token("|")[0..1]
		)
	)
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.SetType)
	.SetName(nameof(SetType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLambdaTypeNode<tSpan>, tError>
	LambdaType = E(
		mParserGen.Seq(
			Type,
			SpecialToken(":"),
			Type,
			SpecialToken("=>"),
			Type
		).Modify((a1, _, a2, _, a3) => (a1, a2, a3)) |
		mParserGen.Seq(
			Type,
			SpecialToken("=>"),
			Type
		).ModifyS(
			(aSpan, a2, _, a3) => (
				a1: (mSPO_AST.tTypeNode<tSpan>)mSPO_AST.EmptyType(mSpan.Span(aSpan.Start)),
				a2,
				a3
			)
		)
	)
	.ModifyS(mSPO_AST.LambdaType)
	.SetName(nameof(LambdaType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecursiveTypeNode<tSpan>, tError>
	RecursiveType = E(
		mParserGen.Seq(
			KeyWord("RECURSIVE"),
			Id,
			Type
		)
	)
	.Modify((_, aId, aExpression) => (aId, aExpression))
	.ModifyS(mSPO_AST.RecursiveType)
	.SetName(nameof(RecursiveType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tInterfaceTypeNode<tSpan>, tError>
	InterfaceType = E(
		mParserGen.Seq(
			KeyWord("INTERFACE"),
			Id,
			Type
		)
	)
	.Modify((_, aId, aExpression) => (aId, aExpression))
	.ModifyS(mSPO_AST.InterfaceType)
	.SetName(nameof(InterfaceType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tGenericTypeNode<tSpan>, tError>
	GenericType = E(
		mParserGen.Seq(
			KeyWord("GENERIC"),
			Id,
			Type
		)
	)
	.Modify((_, aId, aExpression) => (aId, aExpression))
	.ModifyS(mSPO_AST.GenericType)
	.SetName(nameof(GenericType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLambdaNode<tSpan>, tError>
	Lambda = mParserGen.Seq(
		mParserGen.Seq(Match, -Token("<=>"))[0..1].Modify(a => a.TryFirst().ThenTry(a_ => mMaybe.Some(a_.Item1))),
		Match,
		-SpecialToken("=>"),
		Expression
	)
	.Modify((aStaticMatch, aMatch, _, aExpression) => (aStaticMatch, aMatch, aExpression))
	.ModifyS(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodNode<tSpan>, tError>
	Method = mParserGen.Seq(Match, SpecialToken(":"), Match, Block)
	.Modify((aObjMatch, _, aArgMatch, aBlock) => (aObjMatch, aArgMatch, aBlock))
	.ModifyS(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecLambdaItemNode<tSpan>, tError>
	RecLambdaItem = mParserGen.Seq(MatchFreeId, Token("="), Lambda | C( Lambda ))
	.Modify((aId, _, aLambda) => (aId, aLambda))
	.ModifyS(mSPO_AST.RecLambdaItem)
	.SetName(nameof(RecLambdaItem));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecLambdasNode<tSpan>, tError>
	RecLambda = mParserGen.Seq(
		KeyWord("RECURSIVE"),
		SpecialToken("{"),
		NLs_Token,
		(RecLambdaItem +-NLs_Token)[1..],
		SpecialToken("}")
	).Modify((_, _, _, a, _) => a)
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
			Expression,
			NLs_Token
		).Modify((aCond, _, aRes, _) => (aCond, aRes))[0..],
		SpecialToken("}")
	)
	.Modify((_, _, _, a, _) => a)
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
			NLs_Token
		).Modify((aMatch, _, aExpression, _) => (aMatch, aExpression))[0..],
		SpecialToken("}")
	)
	.Modify((_, aExpression, _, aBranches, _) => (aExpression, aBranches))
	.ModifyS(mSPO_AST.IfMatch)
	.SetName(nameof(IfMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodCallNode<tSpan>, tError>
	MethodCall = mParserGen.Seq(
		Id,
		Infix(ExpressionInCall),
		(-SpecialToken("=>") +Match)[0..1].Modify(aMatches => aMatches.TryFirst())
	)
	.ModifyS(
		(aSpan, aFirst, aInfix, aMaybeOut) => mSPO_AST.MethodCall(
			aSpan,
			mSPO_AST.Id(aSpan, aFirst.Id[1..] + aInfix.Id.Id[1..]),
			mSPO_AST.Tuple(aSpan, aInfix.Childs),
			aMaybeOut
		)
	)
	.SetName(nameof(MethodCall));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mSPO_AST.tMethodCallNode<tSpan>>?, tError>
	MethodCalls = mParserGen.Seq(
		MethodCall,
		((-SpecialToken(",")|-NLs_Token) +MethodCall)[0..],
		NLs_Token[0..1],
		SpecialToken(".")
	)
	.Modify((aFirst, aRest, _, _) => mStream.Stream(aFirst, aRest))
	.SetName(nameof(MethodCalls));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tDefVarNode<tSpan>, tError>
	DefVar = mParserGen.Seq(
		KeyWord("VAR"),
		Id,
		SpecialToken(":") +(-NLs_Token[0..1] -Token("=")),
		Expression,
		(
			((-SpecialToken(",") | -NLs_Token) +MethodCalls) |
			SpecialToken(".").Modify(a => mStream.Stream<mSPO_AST.tMethodCallNode<tSpan>>())
		)
	)
	.Modify((_, aId, _, aFirst, aRest) => (aId, aFirst, aRest))
	.ModifyS(mSPO_AST.DefVar)
	.SetName(nameof(DefVar));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tVarToValNode<tSpan>, tError>
	VarToVal = (-KeyWord("TO_VAL") +Expression)
	.ModifyS(mSPO_AST.VarToVal)
	.SetName(nameof(VarToVal));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodCallsNode<tSpan>, tError>
	MethodCallStatment = mParserGen.Seq(
		ExpressionInCall,
		SpecialToken(":"),
		NLs_Token[0..1],
		MethodCalls
	)
	.Modify((aObj, _, _, aMethodCalls) => (aObj, aMethodCalls))
	.ModifyS(mSPO_AST.MethodCallStatement)
	.SetName(nameof(MethodCallStatment));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tImportNode<tSpan>, tError>
	Import = (-KeyWord("IMPORT") +(Match +-NLs_Token))
	.ModifyS(mSPO_AST.Import)
	.SetName(nameof(Import));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExportNode<tSpan>, tError>
	Export = (-KeyWord("EXPORT") +Expression)
	.ModifyS(mSPO_AST.Export)
	.SetName(nameof(Export));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tModuleNode<tSpan>, tError>
	Module = mParserGen.Seq(
		NLs_Token[0..1],
		Import,
		Commands,
		Export,
		NLs_Token[0..1]
	)
	.Modify((_, aImport, aCommands, aExports, _) => (aImport, aCommands, aExports))
	.ModifyS(mSPO_AST.Module)
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		Type.Def(
			mParserGen.OneOf(
				Id.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				EmptyType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				BoolType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				IntType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				TypeType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				PrefixType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				TupleType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				SetType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				LambdaType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				RecursiveType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				InterfaceType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				GenericType.Cast<mSPO_AST.tTypeNode<tSpan>>()
			)
		);
		
		UnTypedMatch.Def(
			mParserGen.OneOf(
				MatchFreeId.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				C( mParserGen.Seq(Match, ((-SpecialToken(",") | -NLs_Token) +Match)[0..]) )
					.Modify(mStream.Stream)
					.ModifyS(mSPO_AST.MatchTuple)
					.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				IgnoreMatch.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				MatchPrefix.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				MatchRecord.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				MatchGuard.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				Literal.Cast<mSPO_AST.tMatchItemNode<tSpan>>(),
				Id.Cast<mSPO_AST.tMatchItemNode<tSpan>>()
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
				Record.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				VarToVal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				C( PipeExpression | Expression ).Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Literal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Id.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Type.Cast<mSPO_AST.tExpressionNode<tSpan>>()
			)
		);
		
		ExpressionInCall.Def(
			mParserGen.OneOf(
				Block.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Tuple.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Record.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				C( PipeExpression | Expression ).Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Literal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Id.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				Type.Cast<mSPO_AST.tExpressionNode<tSpan>>()
			)
		);
		
		PipeToRight.Def(
			(Expression +-KeyWord(">") +(PipeToRight | Expression))
			.ModifyS(mSPO_AST.PipeToRight)
		);
		
		PipeToLeft.Def(
			(Expression +-KeyWord("<") +(PipeToLeft | Expression))
			.ModifyS(mSPO_AST.PipeToLeft)
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
			) +- NLs_Token
		);
	}
}
