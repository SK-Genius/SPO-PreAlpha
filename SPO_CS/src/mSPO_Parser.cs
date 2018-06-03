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

using tPos = mTextParser.tPos;
using tSpan = mStd.tSpan<mTextParser.tPos>;
using tSPO_Parser = mParserGen.tParser<mTextParser.tPos, System.Char, mTextParser.tError>;

public static class mSPO_Parser {
	public static readonly mStd.tFunc<tSPO_Parser, tChar> Char = a => mTextParser.GetChar(a);
	public static readonly mStd.tFunc<tSPO_Parser, tChar> NotChar = a => mTextParser.GetNotChar(a);
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharIn = a => mTextParser.GetCharIn(a);
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharNotIn = a => mTextParser.GetCharNotIn(a);
	public static readonly mStd.tFunc<tSPO_Parser, tChar, tChar> CharInRange = (a1, a2) => mTextParser.GetCharInRange(a1, a2);
	public static readonly mStd.tFunc<tSPO_Parser, tText> Text = a => mTextParser.GetToken(a);
	
	public static readonly tSPO_Parser
	_ = CharIn(" \t")
	.SetName(nameof(_));
	
	public static readonly tSPO_Parser
	__ = _[0, null]
	.SetName(nameof(__));
	
	public static readonly tSPO_Parser
	NL = (-__ +Char('\n') -__)[1, null]
	.SetName(nameof(NL));
	
	public static readonly mStd.tFunc<tSPO_Parser, tText>
	Token = a => (Text(a) + -__)
	.SetName("\"" + a + "\"");
	
	public static readonly tSPO_Parser
	String = (-Char('"') +NotChar('"')[0, null] -Char('"'))
	.ModifyList(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar))
	.Modify((mStd.tFunc<mSPO_AST.tTextNode<tPos>, tSpan, tText>)mSPO_AST.Text<tPos>)
	.SetName(nameof(String));
	
	public static readonly tSPO_Parser
	Digit = CharInRange('0', '9')
	.Modify((tSpan aSpan, tChar aChar) => (int)aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly tSPO_Parser
	Nat = (Digit + (Digit | -Char('_'))[0, null])
	.ModifyList(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit))
	.SetName(nameof(Nat));
	
	public static readonly tSPO_Parser
	PosSignum = Char('+')
	.Modify((tSpan aSpan) => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly tSPO_Parser
	NegSignum = Char('-')
	.Modify((tSpan aSpan) => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly tSPO_Parser
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly tSPO_Parser
	Int = (Signum + Nat)
	.Modify((tSpan aSpan, int aSig, int aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly tSPO_Parser
	Num = (Int | Nat)
	.Modify((tSpan aSpan, tInt32 a) => mSPO_AST.Number(aSpan, a))
	.SetName(nameof(Num));
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly tSPO_Parser
	Empty = (-Token("()"))
	.Modify(mSPO_AST.Empty)
	.SetName(nameof(Empty));
	
	public static readonly tSPO_Parser
	Ident = ( ( CharNotIn(SpazialChars).Modify((tSpan aSpan, tChar aChar) => aChar.ToString()) | Text("...") )[1, null] -__ )
	.ModifyList(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
	.Assert((tText aText) => aText != "=>")
	.Modify((tSpan aSpan, tText a) => mSPO_AST.Ident(aSpan, a))
	.SetName(nameof(Ident));
	
	public static readonly tSPO_Parser
	Literal = ( (Empty | Num | String) -__ )
	.SetName(nameof(Literal));
	
	//================================================================================
	public static tSPO_Parser
	C(
		tSPO_Parser aParser
	//================================================================================
	) => (
		(-Token("(") -NL +aParser -NL -Token(")")) |
		(-Token("(") +aParser -Token(")"))
	);
	
	public static readonly tSPO_Parser
	ExpressionInCall = mParserGen.UndefParser<tPos, tChar, mTextParser.tError>()
	.SetName(nameof(ExpressionInCall));
	
	public static readonly tSPO_Parser
	Expression = mParserGen.UndefParser<tPos, tChar, mTextParser.tError>()
	.SetName(nameof(Expression));
	
	public static readonly tSPO_Parser
	UnTypedMatch = mParserGen.UndefParser<tPos, tChar, mTextParser.tError>()
	.SetName(nameof(UnTypedMatch));
	
	public static readonly tSPO_Parser
	TypedMatch = (+UnTypedMatch -Token("€") +Expression.OrFail())
	.Modify((tSpan aSpan, mSPO_AST.tMatchNode<tPos> a1, mSPO_AST.tExpressionNode<tPos> a2) => mSPO_AST.Match(aSpan, a1, a2))
	.SetName(nameof(TypedMatch));
	
	public static readonly tSPO_Parser
	Match = (+TypedMatch | +UnTypedMatch)
	.SetName(nameof(Match));
	
	public static readonly tSPO_Parser
	Def = (-Token("§DEF") +(+Match -Token("=") +Expression).OrFail())
	.Modify(mSPO_AST.Def_<tPos>())
	.SetName(nameof(Def));
	
	public static readonly tSPO_Parser
	ReturnIf = (-Token("§RETURN") +Expression.OrFail() -Token("IF") +Expression.OrFail())
	.Modify(mSPO_AST.ReturnIf_<tPos>())
	.SetName(nameof(ReturnIf));
	
	public static readonly tSPO_Parser
	Return = (-Token("§RETURN") +Expression.OrFail())
	.Modify((tSpan aSpan, mSPO_AST.tExpressionNode<tPos> a) => mSPO_AST.ReturnIf(aSpan, a, mSPO_AST.True(aSpan)))
	.SetName(nameof(Return));
	
	public static readonly tSPO_Parser
	Command = mParserGen.UndefParser<tPos, tChar, mTextParser.tError>()
	.SetName(nameof(Command));
	
	public static readonly tSPO_Parser
	Commands = Command[0, null]
	.ModifyList(a => mParserGen.ResultList(a.Span, a.Map(mStd.To<mSPO_AST.tCommandNode<tPos>>)))
	.SetName(nameof(Commands));
	
	public static readonly tSPO_Parser
	Block = (-Token("{") +(-NL +Commands -Token("}")).OrFail())
	.Modify(mSPO_AST.Block_<tPos>())
	.SetName(nameof(Block));
	
	public static readonly tSPO_Parser
	Tuple = C( +Expression +( -(Token(",")|NL) +Expression)[1, null] )
	.ModifyList(
		a => mParserGen.ResultList(
			a.Span,
			mSPO_AST.Tuple(a.Span, a.Value.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>))
		)
	)
	.SetName(nameof(Tuple));
	
	//================================================================================
	public static tSPO_Parser
	Infix(
		tSPO_Parser aChildParser
	//================================================================================
	) => (
		(+Ident +(( +aChildParser + Ident )[0, null] + aChildParser[0, 1]).OrFail())
		.ModifyList(
			aList => {
				var Last = (aList.Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "..." : "";
				return (
					mParserGen.ResultList(
						aList.Span,
						(
							mSPO_AST.Ident(
								aList.Span,
								aList.Value.Every(2).Map(
									a => a.To<mSPO_AST.tIdentNode<tPos>>().Name.Substring(1)
								).Join(
									(a1, a2) => $"{a1}...{a2}"
								)+Last
							),
							aList.Value.Skip(1).Every(2)
						)
					)
				);
			}
		)
	);
	
	//================================================================================
	public static tSPO_Parser
	Infix(
		tText aPrefix,
		tSPO_Parser aChildParser
	//================================================================================
	) => (
		-Token(aPrefix) +Infix(aChildParser)
	) | (
		(+aChildParser -Token(aPrefix) +Infix(aChildParser))
		.ModifyList(
			a => {
				a.GetHeadTail(out var FirstChild, out var Rest);
				mStd.Assert(Rest.Value.First().Match(out (mSPO_AST.tIdentNode<tPos> Ident, tResults ChildList) Infix));
				return mParserGen.ResultList(
					a.Span,
					(
						mSPO_AST.Ident(a.Span, "..." + Infix.Ident.Name.Substring(1)),
						mList.Concat(mList.List(FirstChild), Infix.ChildList)
					)
				);
			}
		)
	);
	
	public static readonly tSPO_Parser
	Call = (
		Infix(".", ExpressionInCall).Modify(
			(tSpan aSpan, (mSPO_AST.tIdentNode<tPos> Ident, tResults Childs) a) =>  mSPO_AST.Call(
				aSpan,
				a.Ident,
				mSPO_AST.Tuple(aSpan, a.Childs.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>))
			)
		) |
		(-Token(".") +ExpressionInCall +ExpressionInCall).Modify(mSPO_AST.Call_<tPos>())
	)
	.SetName(nameof(Call));
	
	public static readonly tSPO_Parser
	Prefix = Infix("#", ExpressionInCall)
	.Modify(
		(tSpan aSpan, (mSPO_AST.tIdentNode<tPos> Ident, tResults Childs) a) => {
			return mSPO_AST.Prefix(
				aSpan,
				a.Ident,
				mSPO_AST.Tuple(aSpan, a.Childs.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>))
			);
		}
	)
	.SetName(nameof(Prefix));
	
	public static readonly tSPO_Parser
	MatchPrefix = C( Infix("#", UnTypedMatch) )
	.Modify(
		(tSpan aSpan, (mSPO_AST.tIdentNode<tPos> Ident, tResults Childs) a) => {
			return mSPO_AST.MatchPrefix(
				aSpan,
				a.Ident,
				mSPO_AST.Match(
					aSpan,
					mSPO_AST.MatchTuple(aSpan, a.Childs.Map(mStd.To<mSPO_AST.tMatchNode<tPos>>)),
					null
				)
			);
		}
	)
	.SetName(nameof(MatchPrefix));
	
	public static readonly tSPO_Parser
	MatchGuard = C( +UnTypedMatch -Token("|") +Expression.OrFail() )
	.Modify(mSPO_AST.MatchGuard_<tPos>())
	.SetName(nameof(MatchGuard));
	
	public static readonly tSPO_Parser
	EmptyType = (-Token("[]"))
	.Modify(mSPO_AST.EmptyType_<tPos>())
	.SetName(nameof(EmptyType));
	
	public static readonly tSPO_Parser
	BoolType = (-Token("§BOOL"))
	.Modify(mSPO_AST.BoolType_<tPos>())
	.SetName(nameof(BoolType));
	
	public static readonly tSPO_Parser
	IntType = (-Token("§INT"))
	.Modify(mSPO_AST.IntType_<tPos>())
	.SetName(nameof(IntType));
	
	public static readonly tSPO_Parser
	TypeType = (-Token("[[]]"))
	.Modify(mSPO_AST.TypeType_<tPos>())
	.SetName(nameof(TypeType));
	
	public static readonly tSPO_Parser
	PrefixType = (-Token("[") +Infix("#", Expression) -Token("]"))
	.Modify(
		(tSpan aSpan, (mSPO_AST.tIdentNode<tPos> Ident, tResults Childs) a) => {
			return mSPO_AST.PrefixType(
				aSpan,
				a.Ident,
				a.Childs.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>)
			);
		}
	)
	.SetName(nameof(PrefixType));
	
	public static readonly tSPO_Parser
	TupleType = (-Token("[") -NL[0, 1] +Expression +((-Token(",") | -NL) +Expression)[1, null] -NL[0, 1] -Token("]"))
	.ModifyList(
		a => mParserGen.ResultList(
			a.Span,
			mSPO_AST.TupleType(a.Span, a.Value.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>))
		)
	)
	.SetName(nameof(TupleType));
	
	public static readonly tSPO_Parser
	SetType = (-Token("[") -NL[0, 1] +Expression +(-Token("|") -NL[0, 1] +Expression)[1, null] -Token("|")[0, 1] -NL[0, 1] -Token("]"))
	.ModifyList(
		a => mParserGen.ResultList(
			a.Span,
			mSPO_AST.SetType(a.Span, a.Value.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>))
		)
	)
	.SetName(nameof(SetType));
	
	public static readonly tSPO_Parser
	LambdaType = (-Token("[") +Expression -Token("=>") +Expression -Token("]"))
	.Modify(mSPO_AST.LambdaType_<tPos>())
	.SetName(nameof(LambdaType));
	
	public static readonly tSPO_Parser
	RecursiveType = (-Token("[") -Token("§RECURSIV") +Ident +Expression -Token("]"))
	.Modify(mSPO_AST.RecursiveType_<tPos>())
	.SetName(nameof(RecursiveType));
	
	public static readonly tSPO_Parser
	InterfaceType = (-Token("[") -Token("§INTERFACE") +Ident +Expression -Token("]"))
	.Modify(mSPO_AST.InterfaceType_<tPos>())
	.SetName(nameof(InterfaceType));
	
	public static readonly tSPO_Parser
	GenericType = (-Token("[") -Token("§GENERIC") +Ident +Expression -Token("]"))
	.Modify(mSPO_AST.GenericType_<tPos>())
	.SetName(nameof(GenericType));
	
	public static readonly tSPO_Parser
	Type = (
		EmptyType |
		BoolType |
		IntType |
		TypeType |
		PrefixType |
		TupleType |
		SetType |
		LambdaType |
		RecursiveType |
		InterfaceType |
		GenericType
	)
	.SetName(nameof(Type));
	
	public static readonly tSPO_Parser
	Lambda = (+Match -Token("=>") +Expression.OrFail())
	.Modify(mSPO_AST.Lambda_<tPos>())
	.SetName(nameof(Lambda));
	
	public static readonly tSPO_Parser
	Method = (+Match -Token(":") +Match +Block)
	.Modify(mSPO_AST.Method_<tPos>())
	.SetName(nameof(Method));
	
	public static readonly tSPO_Parser
	RecLambdaItem = (-__ +Ident -Token("=") +(Lambda | C( Lambda )))
	.Modify(mSPO_AST.RecLambdaItem_<tPos>())
	.SetName(nameof(RecLambdaItem));
	
	public static readonly tSPO_Parser
	RecLambda = (
		(-Token("§RECURSIV") -Token("{") -NL +((+RecLambdaItem -NL)[1, null] -Token("}")).OrFail()) |
		(-Token("§RECURSIV") +RecLambdaItem.OrFail())
	)
	.ModifyList(
		aList => mParserGen.ResultList(
			aList.Span,
			mSPO_AST.RecLambdas(aList.Span, aList.Value.Map(mStd.To<mSPO_AST.tRecLambdaItemNode<tPos>>))
		)
	)
	.SetName(nameof(RecLambda));
	
	public static readonly tSPO_Parser
	If = (
		-Token("§IF") -Token("{") -NL +(
			(
				-__ +Expression -Token("=>") +(Expression -NL).OrFail()
			).Modify(
				(tSpan aSpan, mSPO_AST.tExpressionNode<tPos> a1, mSPO_AST.tExpressionNode<tPos> a2) => (a1, a2)
			)[0, null] -Token("}")
		).OrFail()
	)
	.ModifyList(
		aList => mParserGen.ResultList(
			aList.Span,
			mSPO_AST.If(
				aList.Span,
				aList.Map(mStd.To<(mSPO_AST.tExpressionNode<tPos>, mSPO_AST.tExpressionNode<tPos>)>)
			)
		)
	)
	.SetName(nameof(If));
	
	public static readonly tSPO_Parser
	IfMatch = (
		-Token("§IF")
		+(
			+Expression -Token("MATCH") -Token("{") -NL
			+(
				-__ +Match +(-Token("=>") +Expression -NL).OrFail()
			)
			.Modify(
				(tSpan aSpan, mSPO_AST.tMatchNode<tPos> a1, mSPO_AST.tExpressionNode<tPos> a2) => (a1, a2)
			)[0, null]
			-Token("}")
		).OrFail()
	)
	.ModifyList(
		aList => {
			aList.GetHeadTail(out var Head, out var Tail);
			
			mDebug.Assert(Head.Match(out mSPO_AST.tExpressionNode<tPos> Expression));
			
			return mParserGen.ResultList(
				aList.Span,
				mSPO_AST.IfMatch(
					aList.Span,
					Expression,
					Tail.Map(mStd.To<(mSPO_AST.tMatchNode<tPos>, mSPO_AST.tExpressionNode<tPos>)>)
				)
			);
		}
	)
	.SetName(nameof(IfMatch));
	
	public static readonly tSPO_Parser
	MethodCall = (
		+Infix(Expression)
		+(-Token("=>") -Token("§DEF") +Match)[0, 1].ModifyList(
			a => mParserGen.ResultList(
				a.Span,
				a.Value?.First().To<mSPO_AST.tMatchNode<tPos>?>()
			)
		)
	)
	.Modify(
		(tSpan aSpan, (mSPO_AST.tIdentNode<tPos> Ident, tResults Childs) a, mSPO_AST.tMatchNode<tPos>? aMaybeOut) => {
			return mSPO_AST.MethodCall(
				aSpan,
				a.Ident,
				mSPO_AST.Tuple(aSpan, a.Childs.Map(mStd.To<mSPO_AST.tExpressionNode<tPos>>)),
				aMaybeOut
			);
		}
	)
	.SetName(nameof(MethodCall));
	
	public static readonly tSPO_Parser
	MethodCalls = (+MethodCall +(-(Token(",")|NL) +MethodCall)[0, null] -NL[0, 1] -Token("."))
	.ModifyList(
		aList => mParserGen.ResultList(
			aList.Span,
			aList.Map(mStd.To<mSPO_AST.tMethodCallNode<tPos>>)
		)
	)
	.SetName(nameof(MethodCalls));
	
	public static readonly tSPO_Parser
	DefVar = (
		-Token("§VAR") +(
			Ident -Token(":") -NL[0, 1] -Token("=") +Expression +(
				-(-Token(",") | -NL) +MethodCalls |
				-Token(".")
			).Flat()
		).OrFail()
	)
	.Modify(mSPO_AST.DefVar_<tPos>())
	.SetName(nameof(DefVar));
	
	public static readonly tSPO_Parser
	VarToVal = (+Ident -Token(":") -Token("=>"))
	.Modify(mSPO_AST.VarToVal_<tPos>())
	.SetName(nameof(VarToVal));
	
	public static readonly tSPO_Parser
	MethodCallStatment = (+(ExpressionInCall -Token(":")) -NL[0, 1] +MethodCalls.OrFail())
	.Modify(mSPO_AST.MethodCallStatment_<tPos>())
	.SetName(nameof(MethodCallStatment));
	
	public static readonly tSPO_Parser
	Import = (-Token("§IMPORT") +(Match -NL).OrFail())
	.Modify(mSPO_AST.Import_<tPos>())
	.SetName(nameof(Import));
	
	public static readonly tSPO_Parser
	Export = ( -Token("§EXPORT") +Expression.OrFail())
	.Modify(mSPO_AST.Export_<tPos>())
	.SetName(nameof(Export));
	
	public static readonly tSPO_Parser
	Module = ( -NL[0, 1] +Import +Commands +Export -NL[0, 1])
	.Modify(mSPO_AST.Module_<tPos>())
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		UnTypedMatch.Def(
			C(+Match +(-(-Token(",") | -NL) +Match)[0, null]).ModifyList(
				a => mParserGen.ResultList(
					a.Span,
					mSPO_AST.Match(
						a.Span,
						mSPO_AST.MatchTuple(
							a.Span,
							a.Value.Map(mStd.To<mSPO_AST.tMatchNode<tPos>>)
						),
						null
					)
				)
			) |
			(Literal|Ident|MatchPrefix|MatchGuard).Modify(
				mSPO_AST.UnTypedMatch_<tPos>()
			)
		);
		
		Expression.Def(
			If |
			IfMatch |
			Block |
			Lambda |
			Method |
			Call |
			Tuple |
			Prefix |
			VarToVal |
			C( Expression ) |
			Literal |
			Ident |
			Type
		);
		
		ExpressionInCall.Def(
			Block |
			Tuple |
			C( Expression ) |
			Literal |
			Ident |
			Type
		);
		
		// TODO: Macros, Streaming, Block, ...
		Command.Def(
			(
				Def |
				DefVar |
				MethodCallStatment |
				RecLambda |
				ReturnIf |
				Return
			) -NL.OrFail()
		);
	}
	
	#region TEST
	
	//================================================================================
	private static tSpan Span(
		(tInt32 Row, tInt32 Col) aStart,
		(tInt32 Row, tInt32 Col) aEnd
	//================================================================================
	) => new tSpan {
		Start = {
			Row = aStart.Row,
			Col = aStart.Col
		},
		End = {
			Row = aEnd.Row,
			Col = aEnd.Col
		}
	};
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Parser),
		mTest.Test(
			"Atoms",
			aStreamOut => {
				mStd.AssertEq(
					Num.ParseText("+1_234", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 6)), mSPO_AST.Number(Span((1, 1), (1, 6)), 1234))
				);
				mStd.AssertEq(
					Literal.ParseText("+1_234", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 6)), mSPO_AST.Number(Span((1, 1), (1, 6)), 1234))
				);
				mStd.AssertEq(
					ExpressionInCall.ParseText("+1_234", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 6)), mSPO_AST.Number(Span((1, 1), (1, 6)), 1234))
				);
				
				mStd.AssertEq(
					String.ParseText("\"BLA\"", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 5)), mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"))
				);
				mStd.AssertEq(
					Literal.ParseText("\"BLA\"", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 5)), mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"))
				);
				mStd.AssertEq(
					ExpressionInCall.ParseText("\"BLA\"", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 5)), mSPO_AST.Text(Span((1, 1), (1, 5)), "BLA"))
				);
				
				mStd.AssertEq(
					ExpressionInCall.ParseText("BLA", aStreamOut),
					mParserGen.ResultList(Span((1, 1), (1, 3)), mSPO_AST.Ident(Span((1, 1), (1, 3)), "BLA"))
				);
			}
		),
		mTest.Test(
			"Tuple",
			aStreamOut => {
				mStd.AssertEq(
					ExpressionInCall.ParseText("(+1_234, \"BLA\")", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 15)),
						mSPO_AST.Tuple(
							Span((1, 1), (1, 15)),
							mList.List<mSPO_AST.tExpressionNode<tPos>>(
								mSPO_AST.Number(Span((1, 2), (1, 7)), 1234),
								mSPO_AST.Text(Span((1, 10), (1, 14)), "BLA")
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Match1",
			aStreamOut => {
				mStd.AssertEq(
					Match.ParseText("12", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 2)),
						mSPO_AST.Match(
							Span((1, 1), (1, 2)),
							mSPO_AST.Number(Span((1, 1), (1, 2)), 12),
							null
						)
					)
				);
				mStd.AssertEq(
					Match.ParseText("x", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 1)),
						mSPO_AST.Match(
							Span((1, 1), (1, 1)),
							mSPO_AST.Ident(Span((1, 1), (1, 1)), "x"),
							null
						)
					)
				);
				mStd.AssertEq(
					Match.ParseText("(12, x)", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 7)), 
						mSPO_AST.Match(
							Span((1, 1), (1, 7)), 
							mSPO_AST.MatchTuple(
								Span((1, 1), (1, 7)), 
								mList.List(
									mSPO_AST.Match(Span((1, 2), (1, 3)), mSPO_AST.Number(Span((1, 2), (1, 3)), 12), null),
									mSPO_AST.Match(Span((1, 6), (1, 6)), mSPO_AST.Ident(Span((1, 6), (1, 6)), "x"), null)
								)
							),
							null
						)
					)
				);
			}
		),
		mTest.Test(
			"FunctionCall",
			aStreamOut => {
				mStd.AssertEq(
					Expression.ParseText("x .* x", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 6)),
						mSPO_AST.Call(
							Span((1, 1), (1, 6)),
							mSPO_AST.Ident(Span((1, 1), (1, 6)), "...*..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 6)),
								mList.List<mSPO_AST.tExpressionNode<tPos>>(
									mSPO_AST.Ident(Span((1, 1), (1, 2)), "x"), // TODO: Span isn't correct ((1, 1), (1, 1))
									mSPO_AST.Ident(Span((1, 6), (1, 6)), "x")
								)
							)
						)
					)
				);
				mStd.AssertEq(
					Expression.ParseText(".sin x", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 6)),
						mSPO_AST.Call(
							Span((1, 1), (1, 6)),
							mSPO_AST.Ident(Span((1, 2), (1, 6)), "sin..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 6)),
								mList.List<mSPO_AST.tExpressionNode<tPos>>(
									mSPO_AST.Ident(Span((1, 6), (1, 6)), "x")
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Lambda",
			aStreamOut => {
				mStd.AssertEq(
					Expression.ParseText("x => x .* x", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 11)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 11)),
							mSPO_AST.Match(
								Span((1, 1), (1, 2)),
								mSPO_AST.Ident(Span((1, 1), (1, 2)), "x"),
								null
							),
							mSPO_AST.Call(
								Span((1, 6), (1, 11)), 
								mSPO_AST.Ident(Span((1, 6), (1, 11)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 6), (1, 11)), 
									mList.List<mSPO_AST.tExpressionNode<tPos>>(
										mSPO_AST.Ident(Span((1, 6), (1, 7)), "x"),
										mSPO_AST.Ident(Span((1, 11), (1, 11)), "x")
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"TypedMatch",
			aStreamOut => {
				mStd.AssertEq(
					Expression.ParseText("(x € MyType) => x .* x", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 22)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 22)),
							mSPO_AST.Match(
								Span((1, 1), (1, 13)),
								mSPO_AST.Match(
									Span((1, 2), (1, 11)),
									mSPO_AST.Match(
										Span((1, 2), (1, 3)),
										mSPO_AST.Ident(Span((1, 2), (1, 3)), "x"),
										null
									),
									mSPO_AST.Ident(Span((1, 6), (1, 11)), "MyType")
								),
								null
							),
							mSPO_AST.Call(
								Span((1, 17), (1, 22)),
								mSPO_AST.Ident(Span((1, 17), (1, 22)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 17), (1, 22)),
									mList.List<mSPO_AST.tExpressionNode<tPos>>(
										mSPO_AST.Ident(Span((1, 17), (1, 18)), "x"),
										mSPO_AST.Ident(Span((1, 22), (1, 22)), "x")
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Expression",
			aStreamOut => {
				mStd.AssertEq(
					Expression.ParseText("2 .< (4 .+ 3) < 3", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 17)),
						mSPO_AST.Call(
							Span((1, 1), (1, 17)),
							mSPO_AST.Ident(Span((1, 1), (1, 17)), "...<...<..."),
							mSPO_AST.Tuple(
								Span((1, 1), (1, 17)),
								mList.List<mSPO_AST.tExpressionNode<tPos>>(
									mSPO_AST.Number(Span((1, 1), (1, 1)), 2),
									mSPO_AST.Call(
										Span((1, 7), (1, 12)),
										mSPO_AST.Ident(Span((1, 7), (1, 12)), "...+..."),
										mSPO_AST.Tuple(
											Span((1, 7), (1, 12)),
											mList.List<mSPO_AST.tExpressionNode<tPos>>(
												mSPO_AST.Number(Span((1, 7), (1, 7)), 4),
												mSPO_AST.Number(Span((1, 12), (1, 12)), 3)
											)
										)
									),
									mSPO_AST.Number(Span((1, 17), (1, 17)), 3)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"NestedMatch",
			aStreamOut => {
				mStd.AssertEq(
					Expression.ParseText("(a, b, (x, y, z)) => a .* z", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 27)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 27)),
							mSPO_AST.Match(
								Span((1, 1), (1, 18)),
								mSPO_AST.MatchTuple(
									Span((1, 1), (1, 18)),
									mList.List(
										mSPO_AST.Match(
											Span((1, 2), (1, 2)),
											mSPO_AST.Ident(Span((1, 2), (1, 2)), "a"),
											null
										),
										mSPO_AST.Match(
											Span((1, 5), (1, 5)),
											mSPO_AST.Ident(Span((1, 5), (1, 5)), "b"),
											null
										),
										mSPO_AST.Match(
											Span((1, 8), (1, 16)),
											mSPO_AST.MatchTuple(
												Span((1, 8), (1, 16)),
												mList.List(
													mSPO_AST.Match(
														Span((1, 9), (1, 9)),
														mSPO_AST.Ident(Span((1, 9), (1, 9)), "x"),
														null
													),
													mSPO_AST.Match(
														Span((1, 12), (1, 12)),
														mSPO_AST.Ident(Span((1, 12), (1, 12)), "y"),
														null
													),
													mSPO_AST.Match(
														Span((1, 15), (1, 15)),
														mSPO_AST.Ident(Span((1, 15), (1, 15)), "z"),
														null
													)
												)
											),
											null
										)
									)
								),
								null
							),
							mSPO_AST.Call(
								Span((1, 22), (1, 27)),
								mSPO_AST.Ident(Span((1, 22), (1, 27)), "...*..."),
								mSPO_AST.Tuple(
									Span((1, 22), (1, 27)),
									mList.List<mSPO_AST.tExpressionNode<tPos>>(
										mSPO_AST.Ident(Span((1, 22), (1, 23)), "a"),
										mSPO_AST.Ident(Span((1, 27), (1, 27)), "z")
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"PrefixMatch",
			aStreamOut => {
				mStd.AssertEq(
					Expression.ParseText("(1 #* a) => a", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 13)),
						mSPO_AST.Lambda(
							Span((1, 1), (1, 13)),
							mSPO_AST.Match(
								Span((1, 1), (1, 9)),
								mSPO_AST.MatchPrefix(
									Span((1, 1), (1, 9)),
									mSPO_AST.Ident(Span((1, 2), (1, 7)), "...*..."),
									mSPO_AST.Match(
										Span((1, 1), (1, 9)), 
										mSPO_AST.MatchTuple(
											Span((1, 1), (1, 9)),
											mList.List(
												mSPO_AST.Match(
													Span((1, 2), (1, 3)),
													mSPO_AST.Number(Span((1, 2), (1, 2)), 1),
													null
												),
												mSPO_AST.Match(
													Span((1, 7), (1, 7)),
													mSPO_AST.Ident(Span((1, 7), (1, 7)), "a"),
													null
												)
											)
										),
										null
									)
								),
								null
							),
							mSPO_AST.Ident(Span((1, 13), (1, 13)), "a")
						)
					)
				);
			}
		),
		mTest.Test(
			"MethodCall",
			aStreamOut => {
				mStd.AssertEq(
					Command.ParseText("o := ((o :=>) .+ i) .\n", aStreamOut),
					mParserGen.ResultList(
						Span((1, 1), (1, 22)),
						mSPO_AST.MethodCallStatment(
							Span((1, 1), (1, 21)),
							mSPO_AST.Ident(Span((1, 1), (1, 2)), "o"),
							mList.List(
								mSPO_AST.MethodCall(
									Span((1, 4), (1, 20)),
									mSPO_AST.Ident(Span((1, 4), (1, 20)), "=..."),
									mSPO_AST.Call(
										Span((1, 7), (1, 18)),
										mSPO_AST.Ident(Span((1, 7), (1, 18)), "...+..."),
										mSPO_AST.Tuple(
											Span((1, 7), (1, 18)), 
											mList.List<mSPO_AST.tExpressionNode<tPos>>(
												mSPO_AST.VarToVal(
													Span((1, 8), (1, 12)),
													mSPO_AST.Ident(Span((1, 8), (1, 9)), "o")
												),
												mSPO_AST.Ident(Span((1, 18), (1, 18)), "i")
											)
										)
									),
									null
								)
							)
						)
					)
				);
			}
		)
	);
	
	#endregion
}