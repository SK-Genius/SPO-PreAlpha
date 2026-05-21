#:include Common/mStd.cs
#:include Common/mParserGen.cs
#:include Common/mTextStream.cs
#:include Common/mTextParser.cs
#:include Common/mSpan.cs
#:include Common/mMaybe.cs
#:include Common/mStream.cs
#:include mTokenizer.cs
#:include mVM_Type.cs
#:include mIL_AST.cs
#:include mSPO_AST.cs
#:include mSPO2IL.cs
#:include mSPO_Desugar.cs

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
	CharToken = mTokenizer.CharToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	TextToken = mTokenizer.TextToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NumberToken = mTokenizer.NumberToken;
	
	private static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	IdToken = mTokenizer.IdToken;
	
	private static readonly mStd.tFunc<tText, mParserGen.tParser<tPos, tToken, tToken, tError>>
	SpecialToken = mTokenizer.SpecialToken;
	
	private static readonly mStd.tFunc<tChar, mParserGen.tParser<tPos, tToken, tToken, tError>>
	SpecialId = mTokenizer.SpecialId;
	
	private static readonly mStd.tFunc<tChar, tText, mParserGen.tParser<tPos, tToken, tToken, tError>>
	SpecialId_ = mTokenizer.SpecialId;
	
	private static readonly mStd.tFunc<tText, mParserGen.tParser<tPos, tToken, tToken, tError>>
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
		__ => (__.Span.Start, $"invalid {aName}")
	)
	.SetDebugName([aName]);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tEmptyNode<tSpan>, tError>
	Empty = (-SpecialToken("(") -SpecialToken(")"))
	.ModifyS(mSPO_AST.Empty)
	.SetName(nameof(Empty));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIgnorePatternNode<tSpan>, tError>
	IgnorePattern = IdToken
	.Assert(__ => __.Type == tTokenType.Id && __.Text == "_", __ => (__.Span.Start, "expect _"))
	.ModifyS(mSPO_AST.IgnorePattern)
	.SetName(nameof(IgnorePattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIdNode<tSpan>, tError>
	Id = IdToken
	.ModifyS(mTokenizer.X(mSPO_AST.Id))
	.SetName(nameof(Id));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIntNode<tSpan>, tError>
	Number = NumberToken
	.Modify(__ => tInt32.Parse(__.Text))
	.ModifyS(mSPO_AST.Int)
	.SetName(nameof(Number));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTrueNode<tSpan>, tError>
	True = KeyWord("TRUE")
	.ModifyS(mSPO_AST.True)
	.SetName(nameof(True));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tFalseNode<tSpan>, tError>
	False = KeyWord("FALSE")
	.ModifyS(mSPO_AST.False)
	.SetName(nameof(False));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCharNode<tSpan>, tError>
	Char = CharToken
	.Modify(__ => __.Text[1])
	.ModifyS(mSPO_AST.Char)
	.SetName(nameof(Char));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTextNode<tSpan>, tError>
	Text = TextToken
	.ModifyS(mTokenizer.X(mSPO_AST.Text))
	.SetName(nameof(Text));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLiteralNode<tSpan>, tError>
	Literal = mParserGen.OneOf(
		[
			Empty.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			True.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			False.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			Number.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			Char.Cast<mSPO_AST.tLiteralNode<tSpan>>(),
			Text.Cast<mSPO_AST.tLiteralNode<tSpan>>()
		]
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
	ExpressionInCall = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(ExpressionInCall));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Expression = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(Expression));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPatternNode<tSpan>, tError>
	UnTypedPattern = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tPatternNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(UnTypedPattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypedPatternNode<tSpan>, tError>
	TypedPattern = mParserGen.Seq(UnTypedPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(), (-SpecialToken("€") +Expression).Modify(mMaybe.Some))
	.ModifyS(mSPO_AST.Pattern)
	.SetName(nameof(TypedPattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPatternNode<tSpan>, tError>
	Pattern = (TypedPattern.Modify(__ => (mSPO_AST.tPatternNode<tSpan>)__) | UnTypedPattern)
	.SetName(nameof(Pattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	PipeToRight = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(PipeToRight));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	PipeToLeft = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(PipeToLeft));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	PipeExpression = PipeToLeft | PipeToRight;
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tDefNode<tSpan>, tError>
	Def = mParserGen.Seq(Pattern, Token("="), PipeExpression | Expression)
	.Modify((aPattern, _, aExpression) => (aPattern, aExpression))
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
	Command = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tCommandNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mSPO_AST.tCommandNode<tSpan>>, tError>
	Commands = Command[0..]
	.SetName(nameof(Commands));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tBlockNode<tSpan>, tError>
	Block = mParserGen.Seq(SpecialToken("{"), NLs_Token, Commands, SpecialToken("}"))
	.Modify((_, _, aCommands, _) => aCommands)
	.ModifyS(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExpressionNode<tSpan>, tError>
	Tuple = C( mParserGen.Seq(PipeExpression | Expression, ((-SpecialToken(",") | -NLs_Token) +(PipeExpression | Expression))[1..]) )
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.Tuple)
	.SetName(nameof(Tuple));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPatternNode<tSpan>, tError>
	TuplePattern = C( mParserGen.Seq(Pattern, ((-SpecialToken(",") | -NLs_Token) +Pattern)[0..]) )
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.TuplePattern)
	.SetName(nameof(TuplePattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPairNode<tSpan>, tError>
	Pair = C(
		mParserGen.Seq(
			PipeExpression | Expression,
			-NLs_Token[0..1],
			-SpecialToken(";"),
			-NLs_Token[0..1],
			PipeExpression | Expression
		)
	)
	.Modify((aTail, _, _, _, aHead) => (aTail, aHead))
	.ModifyS((aSpan, aTail, aHead) => mSPO_AST.Pair(aSpan, aTail, aHead))
	.SetName(nameof(Pair));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPairPatternNode<tSpan>, tError>
	PairPattern = C(
		mParserGen.Seq(
			Pattern,
			-NLs_Token[0..1],
			-SpecialToken(";"),
			-NLs_Token[0..1],
			Pattern
		)
	)
	.Modify((aTail, _, _, _, aHead) => (Tail: aTail, Head: aHead))
	.ModifyS((aSpan, aPair) => mSPO_AST.PairPattern(aSpan, aPair.Tail, aPair.Head))
	.SetName(nameof(PairPattern));
	
	private static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdNode<tSpan> Id, mStream.tStream<tChild> Children), tError>
	Infix<tChild>(
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	) => (
		mParserGen.Seq(mParserGen.Seq(aChildParser, Id)[0..], aChildParser[0..1])
		.ModifyS(
			(aSpan, aList, aLastChild) => (
				mSPO_AST.Id(
					aSpan,
					aList.Map(
						__ => __.Item2.Id[1..]
					).Reduce(
						"",
						(a1, a2) => $"{a1}...{a2}"
					) + (aLastChild.IsEmpty() ? "" : "...")
				),
				mStream.Concat(aList.Map(__ => __.Item1), aLastChild)
			)
		)
	);
	
	private static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdNode<tSpan> Id, mStream.tStream<tChild> Children), tError>
	InfixCall<tChild>(
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	) => (
		mParserGen.Seq(
			SpecialToken("."), Id, Infix(aChildParser)
		).ModifyS(
			(aSpan, _, aFirstId, aInfix) => (
				Id: mSPO_AST.Id(aSpan, aFirstId.Id[1..] + aInfix.Id.Id[1..]),
				Children: aInfix.Children
			)
		)
	) | (
		mParserGen.Seq(aChildParser, SpecialToken("."), Id, Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstChild, _, aFirstId, aInfix) => (
				Id: mSPO_AST.Id(aSpan, "..." + aFirstId.Id[1..] + aInfix.Id.Id[1..]),
				Children: mStream.Stream(aFirstChild, aInfix.Children)
			)
		)
	) | (
		(-SpecialToken(".") +Id)
		.ModifyS(
			(aSpan, aId) => (
				Id: mSPO_AST.Id(aSpan, aId.Id[1..]),
				Children: mStream.Stream<tChild>([])
			)
		)
	);
	
	private static mParserGen.tParser<tPos, tToken, (mSPO_AST.tIdNode<tSpan> Id, mStream.tStream<tChild> Children), tError>
	InfixPrefix<tChild>(
		mParserGen.tParser<tPos, tToken, tChild, tError> aChildParser
	) => (
		mParserGen.Seq(SpecialId('#'), Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstId, aInfix) => (
				Id: mSPO_AST.Id(aSpan, aFirstId.Text[1..] + aInfix.Id.Id[1..]),
				Children: aInfix.Children
			)
		)
	) | (
		mParserGen.Seq(aChildParser, SpecialId('#'), Infix(aChildParser))
		.ModifyS(
			(aSpan, aFirstChild, aFirstId, aInfix) => (
				Id: mSPO_AST.Id(aSpan, "..." + aFirstId.Text[1..] + aInfix.Id.Id[1..]),
				Children: mStream.Stream(aFirstChild, aInfix.Children)
			)
		)
	) | (
		SpecialId('#')
		.ModifyS(
			(aSpan, aId) => (
				Id: mSPO_AST.Id(aSpan, aId.Text[1..]),
				Children: mStream.Stream<tChild>([])
			)
		)
	);
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCallNode<tSpan>, tError>
	Call = mParserGen.OneOf(
		[
			InfixCall(ExpressionInCall).Modify((aId, aArgs) => ((mSPO_AST.tExpressionNode<tSpan>)aId, aArgs)),
			mParserGen.Seq(
				-SpecialToken(".") +C(Expression), Expression.Modify(__ => mStream.Stream(__))
			)
		]
	)
	.ModifyS((aSpan, aId, aArgs) => mSPO_AST.Call(aSpan, aId, mSPO_AST.Tuple(aSpan, aArgs)))
	.SetName(nameof(Call));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPrefixNode<tSpan>, tError>
	Prefix = InfixPrefix(ExpressionInCall)
	.ModifyS((aSpan, aId, aChildren) => mSPO_AST.Prefix(aSpan, aId.Id, mSPO_AST.Tuple(aSpan, aChildren)))
	.SetName(nameof(Prefix));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tFreeIdPatternNode<tSpan>, tError>
	FreeIdPattern = (-KeyWord("DEF") +Id)
	.Modify(__ => __.Id[1..])
	.ModifyS(mSPO_AST.FreeIdPattern)
	.SetName(nameof(FreeIdPattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tVarPatternNode<tSpan>, tError>
	VarPattern = (-KeyWord("VAR") +Id)
	.Modify(__ => __.Id[1..])
	.ModifyS(mSPO_AST.VarPattern)
	.SetName(nameof(VarPattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPrefixPatternNode<tSpan>, tError>
	PrefixPattern = C( InfixPrefix(Pattern) )
	.ModifyS(
		(aSpan, aId, aChildren) => mSPO_AST.PrefixPattern(
			aSpan,
			aId.Id,
			aChildren.Any(_ => true) ? mSPO_AST.TuplePattern(aSpan, aChildren) : mSPO_AST.Empty(aSpan)
		)
	)
	.SetName(nameof(PrefixPattern));
	
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
		.ModifyS((aSpan, _) => mSPO_AST.Record(aSpan, []))
	)
	.SetName(nameof(Record));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecordPatternNode<tSpan>, tError>
	RecordPattern = (
		mParserGen.Seq(
			SpecialToken("{") +-NLs_Token[0..1],
			mParserGen.Seq(
				Id,
				SpecialToken(":"),
				Pattern
			).Modify((aId, _, aExpression) => (Key: aId, Pattern: aExpression)),
			mParserGen.Seq(
				-SpecialToken(",") | -NLs_Token,
				Id,
				SpecialToken(":"),
				Pattern
			).Modify((_, aId, _, aExpression) => (Key: aId, Pattern: aExpression))[0..],
			-NLs_Token[0..1] +SpecialToken("}")
		)
		.Modify((_, aHead, aTail, _) => mStream.Stream(aHead, aTail))
		.ModifyS(mSPO_AST.RecordPattern) |
		mParserGen.Seq(
			SpecialToken("{"),
			NLs_Token[0..1],
			SpecialToken("}")
		)
		.ModifyS((aSpan, _) => mSPO_AST.RecordPattern(aSpan, mStd.cEmpty))
	)
	.SetName(nameof(Record));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tGuardPatternNode<tSpan>, tError>
	GuardPattern = C( mParserGen.Seq(Pattern, Token("&"), Expression) )
	.Modify((a1, _ , a2) => (a1, a2))
	.ModifyS(mSPO_AST.GuardPattern)
	.SetName(nameof(GuardPattern));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>
	TypeInSet = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(TypeInSet));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>
	TypeInTuple = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
	.SetName(nameof(TypeInTuple));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>
	Type = mParserGen.UndefParser<tPos, tToken, mSPO_AST.tTypeNode<tSpan>, tError>(mTextParser.ComparePos, mTextParser.AreErrorsEqual)
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
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tCharTypeNode<tSpan>, tError>
	CharType = (-KeyWord("CHAR"))
	.ModifyS(mSPO_AST.CharType)
	.SetName(nameof(CharType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTextTypeNode<tSpan>, tError>
	TextType = (-KeyWord("TEXT"))
	.ModifyS(mSPO_AST.TextType)
	.SetName(nameof(TextType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tAnyTypeNode<tSpan>, tError>
	AnyType = (-KeyWord("ANY"))
	.ModifyS(mSPO_AST.AnyType)
	.SetName(nameof(TypeType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTypeTypeNode<tSpan>, tError>
	TypeType = (-KeyWord("TYPE"))
	.ModifyS(mSPO_AST.TypeType)
	.SetName(nameof(TypeType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPrefixTypeNode<tSpan>, tError>
	PrefixType = InfixPrefix(Type)
	.ModifyS(mSPO_AST.PrefixType)
	.SetName(nameof(PrefixType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tVarTypeNode<tSpan>, tError>
	VarType = (-KeyWord("VAR") +Type)
	.ModifyS(mSPO_AST.VarType)
	.SetName(nameof(VarType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tTupleTypeNode<tSpan>, tError>
	TupleType = E(
		mParserGen.Seq(
			TypeInTuple,
			((-SpecialToken(",") | -NLs_Token) +TypeInTuple)[1..]
		)
	)
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.TupleType)
	.SetName(nameof(TupleType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tPairTypeNode<tSpan>, tError>
	PairType = E(
		mParserGen.Seq(
			Type,
			-NLs_Token[0..1],
			-SpecialToken(";"),
			-NLs_Token[0..1],
			Type
		)
	)
	.Modify((aTail, _, _, _, aHead) => (Tail: aTail, Head: aHead))
	.ModifyS((aSpan, aPair) => mSPO_AST.PairType(aSpan, aPair.Tail, aPair.Head))
	.SetName(nameof(PairType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tSetTypeNode<tSpan>, tError>
	SetType = mParserGen.Seq(
		TypeInSet,
		(-(NLs_Token[0..1] +-SpecialToken("|") +-NLs_Token[0..1]) +TypeInSet)[1..]
	)
	.Modify(mStream.Stream)
	.ModifyS(mSPO_AST.SetType)
	.SetName(nameof(SetType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLambdaTypeNode<tSpan>, tError>
	LambdaType = (
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
		) |
		mParserGen.Seq(
			SpecialToken("=>"),
			Type
		).ModifyS(
			(aSpan, _, a3) => (
				a1: (mSPO_AST.tTypeNode<tSpan>)mSPO_AST.EmptyType(mSpan.Span(aSpan.Start)),
				a2: (mSPO_AST.tTypeNode<tSpan>)mSPO_AST.EmptyType(mSpan.Span(aSpan.Start)),
				a3
			)
		)
	)
	.ModifyS(mSPO_AST.LambdaType)
	.SetName(nameof(LambdaType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecursiveTypeNode<tSpan>, tError>
	RecursiveType = (
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
	InterfaceType = (
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
	GenericType = (
		mParserGen.Seq(
			KeyWord("GENERIC"),
			Id,
			Type
		)
	)
	.Modify((_, aId, aExpression) => (aId, aExpression))
	.ModifyS(mSPO_AST.GenericType)
	.SetName(nameof(GenericType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tGenericApplyTypeNode<tSpan>, tError>
	GenericApplyType = mParserGen.OneOf(
		[
			InfixCall(Type).Modify((aId, aTypes) => ((mSPO_AST.tTypeNode<tSpan>)aId, aTypes)),
			mParserGen.Seq(
				-SpecialToken(".") +C(Type), Type.Modify(__ => mStream.Stream(__))
			)
		]
	)
	.ModifyS((aSpan, aGenericType, aTypes) => mSPO_AST.GenericApplyType(aSpan, aGenericType, mSPO_AST.TupleType(aSpan, aTypes)))
	.SetName(nameof(GenericApplyType));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tLambdaNode<tSpan>, tError>
	Lambda = mParserGen.Seq(
		mParserGen.Seq(Pattern, -Token("<=>"))[0..1].Modify(a => a.TryFirst().ThenTry(__ => mMaybe.Some(__.Item1))),
		Pattern,
		-SpecialToken("=>"),
		Expression
	)
	.Modify((aStaticPattern, aPattern, _, aExpression) => (aStaticPattern, aPattern, aExpression))
	.ModifyS(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodNode<tSpan>, tError>
	Method = mParserGen.Seq(Pattern, SpecialToken(":"), Pattern, Block)
	.Modify((aObjPattern, _, aArgPattern, aBlock) => (aObjPattern, aArgPattern, aBlock))
	.ModifyS(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tRecLambdaItemNode<tSpan>, tError>
	RecLambdaItem = mParserGen.Seq(FreeIdPattern, Token("="), Lambda | C( Lambda ))
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
			SpecialToken(":"),
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
			Pattern,
			SpecialToken(":"),
			Expression,
			NLs_Token
		).Modify((aPattern, _, aExpression, _) => (aPattern, aExpression))[0..],
		SpecialToken("}")
	)
	.Modify((_, aExpression, _, aBranches, _) => (aExpression, aBranches))
	.ModifyS(mSPO_AST.IfMatch)
	.SetName(nameof(IfMatch));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tIsNode<tSpan>, tError>
	Is = mParserGen.Seq(
		ExpressionInCall,
		KeyWord("IS"),
		Pattern
	)
	.Modify((aValue, _, aPattern) => (aValue, aPattern))
	.ModifyS(mSPO_AST.Is)
	.SetName(nameof(Is));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tMethodCallNode<tSpan>, tError>
	MethodCall = mParserGen.Seq(
		Id,
		Infix(ExpressionInCall),
		(-SpecialToken("=>") +Pattern)[0..1].Modify(aPatterns => aPatterns.TryFirst())
	)
	.ModifyS(
		(aSpan, aFirst, aInfix, aMaybeOut) => mSPO_AST.MethodCall(
			aSpan,
			mSPO_AST.Id(aSpan, aFirst.Id[1..] + aInfix.Id.Id[1..]),
			mSPO_AST.Tuple(aSpan, aInfix.Children),
			aMaybeOut
		)
	)
	.SetName(nameof(MethodCall));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStream.tStream<mSPO_AST.tMethodCallNode<tSpan>>, tError>
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
			SpecialToken(".").Modify(_ => mStream.Stream<mSPO_AST.tMethodCallNode<tSpan>>([]))
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
	MethodCallStatement = mParserGen.Seq(
		ExpressionInCall,
		SpecialToken(":"),
		NLs_Token[0..1],
		MethodCalls
	)
	.Modify((aObj, _, _, aMethodCalls) => (aObj, aMethodCalls))
	.ModifyS(mSPO_AST.MethodCallStatement)
	.SetName(nameof(MethodCallStatement));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tImportNode<tSpan>, tError>
	Import = (-KeyWord("IMPORT") +(Pattern +-NLs_Token))
	.ModifyS(mSPO_AST.Import)
	.SetName(nameof(Import));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tExportNode<tSpan>, tError>
	Export = (-KeyWord("EXPORT") +Expression)
	.ModifyS(mSPO_AST.Export)
	.SetName(nameof(Export));
	
	public static readonly mParserGen.tParser<tPos, tToken, mSPO_AST.tModuleNode<tSpan>, tError>
	Module = mParserGen.Seq(
		NLs_Token[0..1],
		Import[0..1],
		Commands,
		Export,
		NLs_Token[0..1]
	)
	.Modify(
		(_, aImport, aCommands, aExports, _) => (
			aImport.Match(
				() => mSPO_AST.Import(
					default,
					mSPO_AST.UnTypedPattern(
						default,
						mSPO_AST.RecordPattern<tSpan>(default, mStd.cEmpty)
					)
				),
				(aHead, aTail) => aHead
			),
			aCommands,
			aExports
		)
	)
	.ModifyS(mSPO_AST.Module)
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		TypeInSet.Def(
			mParserGen.OneOf(
				[
					PrefixType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					VarType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					LambdaType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					RecursiveType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					InterfaceType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					GenericType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					GenericApplyType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					Type.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				]
			)
		);
		
		TypeInTuple.Def(
			mParserGen.OneOf(
				[
					SetType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					TypeInSet.Cast<mSPO_AST.tTypeNode<tSpan>>(),
				]
			)
		);
		
		Type.Def(
			mParserGen.OneOf(
				[
					Id.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					EmptyType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					BoolType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					IntType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					CharType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					TextType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					AnyType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					TypeType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					PairType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					TupleType.Cast<mSPO_AST.tTypeNode<tSpan>>(),
					E( TypeInTuple ).Cast<mSPO_AST.tTypeNode<tSpan>>(),
					//C( PipeExpression | Expression ).Cast<mSPO_AST.tTypeNode<tSpan>>(),
				]
			)
		);
		
		UnTypedPattern.Def(
			mParserGen.OneOf(
				[
					FreeIdPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					VarPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					TuplePattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					PairPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					IgnorePattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					PrefixPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					RecordPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					GuardPattern.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					Literal.Cast<mSPO_AST.tPatternNode<tSpan>>(),
					Id.Cast<mSPO_AST.tPatternNode<tSpan>>(),
				]
			)
		);
		
		Expression.Def(
			mParserGen.OneOf(
				[
					If.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					IfMatch.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Is.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Lambda.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Method.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Call.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Prefix.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					VarToVal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Block.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Tuple.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Pair.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Record.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					C( PipeExpression | Expression ).Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Literal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Id.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Type.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				]
			)
		);
		
		ExpressionInCall.Def(
			mParserGen.OneOf(
				[
					Block.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Tuple.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Pair.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Record.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					C( PipeExpression | Expression ).Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Literal.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Id.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
					Type.Cast<mSPO_AST.tExpressionNode<tSpan>>(),
				]
			)
		);
		
		PipeToRight.Def(
			mParserGen.Seq(Expression, (-KeyWord(">") +Expression)[1..])
			.ModifyS(mSPO_AST.PipeToRight)
		);
		
		PipeToLeft.Def(
			mParserGen.Seq((Expression +-KeyWord("<"))[1..], Expression)
			.Modify((aPipe, aHead) => (aPipe.Reverse(), aHead))
			.ModifyS(mSPO_AST.PipeToLeft)
		);
		
		// TODO: Macros, Streaming, Block, ...
		Command.Def(
			mParserGen.OneOf(
				[
					Def.Cast<mSPO_AST.tCommandNode<tSpan>>(),
					DefVar.Cast<mSPO_AST.tCommandNode<tSpan>>(),
					MethodCallStatement.Cast<mSPO_AST.tCommandNode<tSpan>>(),
					RecLambda.Cast<mSPO_AST.tCommandNode<tSpan>>(),
					ReturnIf.Cast<mSPO_AST.tCommandNode<tSpan>>(),
					Return.Cast<mSPO_AST.tCommandNode<tSpan>>()
				]
			) +- NLs_Token
		);
	}
	
	public static tText
	ToText(
		this (mSpan.tSpan<mTextStream.tPos> Pos, tText ErrorText) a
	) => $"{mTextParser.ToText(a.Pos)}: {a.ErrorText}";
}


