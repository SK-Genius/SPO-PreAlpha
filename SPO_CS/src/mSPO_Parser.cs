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

using tSPO_Parser = mParserGen.tParser<mTextParser.tPosChar, mTextParser.tError>;

public static class mSPO_Parser {
	private static readonly mStd.tFunc<tChar, mTextParser.tPosChar> SelectChar = a => a.Char;
	private static readonly mStd.tFunc<tText, mTextParser.tPosText> SelectText = a => a.Text;
	
	public static readonly mStd.tFunc<tSPO_Parser, tChar> Char = a => mTextParser.GetChar(a).Modify(SelectChar);
	public static readonly mStd.tFunc<tSPO_Parser, tChar> NotChar = a => mTextParser.GetNotChar(a).Modify(SelectChar);
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharIn = a => mTextParser.GetCharIn(a).Modify(SelectChar);
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharNotIn = a => mTextParser.GetCharNotIn(a).Modify(SelectChar);
	public static readonly mStd.tFunc<tSPO_Parser, tChar, tChar> CharInRange = (a1, a2) => mTextParser.GetCharInRange(a1, a2).Modify(SelectChar);
	public static readonly mStd.tFunc<tSPO_Parser, tText> Text = a => mTextParser.GetToken(a).Modify(SelectText);
	
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
	.Modify(mSPO_AST.Text)
	.SetName(nameof(String));
	
	public static readonly tSPO_Parser
	Digit = CharInRange('0', '9')
	.Modify((tChar aChar) => (int)aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly tSPO_Parser
	Nat = (Digit + (Digit | -Char('_'))[0, null])
	.ModifyList(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit))
	.SetName(nameof(Nat));
	
	public static readonly tSPO_Parser
	PosSignum = Char('+')
	.Modify(() => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly tSPO_Parser
	NegSignum = Char('-')
	.Modify(() => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly tSPO_Parser
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly tSPO_Parser
	Int = (Signum + Nat)
	.Modify((int aSig, int aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly tSPO_Parser
	Num = ( Int | Nat )
	.Modify(mSPO_AST.Number)
	.SetName(nameof(Num));
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly tSPO_Parser
	Empty = (-Token("()"))
	.Modify(mSPO_AST.Empty)
	.SetName(nameof(Empty));
	
	public static readonly tSPO_Parser
	Ident = ( ( CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) | Text("...") )[1, null] -__ )
	.ModifyList(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
	.Assert((tText aText) => aText != "=>")
	.Modify(mSPO_AST.Ident)
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
	ExpressionInCall = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(ExpressionInCall));
	
	public static readonly tSPO_Parser
	Expression = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(Expression));
	
	public static readonly tSPO_Parser
	UnTypedMatch = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(UnTypedMatch));
	
	public static readonly tSPO_Parser
	TypedMatch = (+UnTypedMatch -Token("€") +Expression.OrFail())
	.Modify(mSPO_AST.Match)
	.SetName(nameof(TypedMatch));
	
	public static readonly tSPO_Parser
	Match = (+TypedMatch | +UnTypedMatch)
	.SetName(nameof(Match));
	
	public static readonly tSPO_Parser
	Def = (-Token("§DEF") +(+Match -Token("=") +Expression).OrFail())
	.Modify(mSPO_AST.Def)
	.SetName(nameof(Def));
	
	public static readonly tSPO_Parser
	ReturnIf = (-Token("§RETURN") +Expression.OrFail() -Token("IF") +Expression.OrFail())
	.Modify(mSPO_AST.ReturnIf)
	.SetName(nameof(ReturnIf));
	
	public static readonly tSPO_Parser
	Return = (-Token("§RETURN") +Expression.OrFail())
	.Modify((mSPO_AST.tExpressionNode a) => mSPO_AST.ReturnIf(a, mSPO_AST.True()))
	.SetName(nameof(Return));
	
	public static readonly tSPO_Parser
	Command = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(Command));
	
	public static readonly tSPO_Parser
	Commands = Command[0, null]
	.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mSPO_AST.tCommandNode>)))
	.SetName(nameof(Commands));
	
	public static readonly tSPO_Parser
	Block = (-Token("{") +(-NL +Commands -Token("}")).OrFail())
	.Modify(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly tSPO_Parser
	Tuple = C( +Expression +( -(Token(",")|NL) +Expression)[1, null] )
	.ModifyList(
		a => mParserGen.ResultList(
			mSPO_AST.Tuple(a.Value.Map(mStd.To<mSPO_AST.tExpressionNode>))
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
						(
							mSPO_AST.Ident(
								aList.Value.Every(2).Map(
									a => a.To<mSPO_AST.tIdentNode>().Name.Substring(1)
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
				mStd.Assert(Rest.Value.First().Match(out (mSPO_AST.tIdentNode Ident, tResults ChildList) Infix));
				return mParserGen.ResultList(
					(
						new mSPO_AST.tIdentNode { Name = "_..." + Infix.Ident.Name.Substring(1) },
						mList.Concat(mList.List(FirstChild), Infix.ChildList)
					)
				);
			}
		)
	);
	
	public static readonly tSPO_Parser
	Call = (
		Infix(".", ExpressionInCall).Modify(
			((mSPO_AST.tIdentNode Ident, tResults Childs) a) =>  mSPO_AST.Call(
				a.Ident,
				mSPO_AST.Tuple(a.Childs.Map(mStd.To<mSPO_AST.tExpressionNode>))
			)
		) |
		(-Token(".") +ExpressionInCall +ExpressionInCall).Modify(mSPO_AST.Call)
	)
	.SetName(nameof(Call));
	
	public static readonly tSPO_Parser
	Prefix = Infix("#", ExpressionInCall)
	.Modify(
		((mSPO_AST.tIdentNode, tResults) aPair) => {
			var (Ident, ChildList) = aPair;
			return mSPO_AST.Prefix(
				Ident,
				mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>))
			);
		}
	)
	.SetName(nameof(Prefix));
	
	public static readonly tSPO_Parser
	MatchPrefix = C( Infix("#", UnTypedMatch) )
	.Modify(
		((mSPO_AST.tIdentNode, tResults) aPair) => {
			var (Ident, ChildList) = aPair;
			return mSPO_AST.MatchPrefix(
				Ident,
				mSPO_AST.Match(
					mSPO_AST.MatchTuple(ChildList.Map(mStd.To<mSPO_AST.tMatchNode>)),
					null
				)
			);
		}
	)
	.SetName(nameof(MatchPrefix));
	
	public static readonly tSPO_Parser
	MatchGuard = C( +UnTypedMatch -Token("|") +Expression.OrFail() )
	.Modify(mSPO_AST.MatchGuard)
	.SetName(nameof(MatchGuard));
	
	public static readonly tSPO_Parser
	EmptyType = Token("[]")
	.Modify((tText _) => mSPO_AST.EmptyType())
	.SetName(nameof(EmptyType));
	
	public static readonly tSPO_Parser
	BoolType = Token("§BOOL")
	.Modify((tText _) => mSPO_AST.BoolType())
	.SetName(nameof(BoolType));
	
	public static readonly tSPO_Parser
	IntType = Token("§INT")
	.Modify((tText _) => mSPO_AST.IntType())
	.SetName(nameof(IntType));
	
	public static readonly tSPO_Parser
	TypeType = Token("[[]]")
	.Modify((tText _) => mSPO_AST.TypeType())
	.SetName(nameof(TypeType));
	
	public static readonly tSPO_Parser
	PairType = (-Token("[") +Expression -Token(",") +Expression -Token("]"))
	.Modify(mSPO_AST.PairType)
	.SetName(nameof(PairType));
	
	public static readonly tSPO_Parser
	LambdaType = (-Token("[") +Expression -Token("=>") +Expression -Token("]"))
	.Modify(mSPO_AST.LambdaType)
	.SetName(nameof(LambdaType));
	
	public static readonly tSPO_Parser
	Type = (EmptyType | BoolType | IntType | TypeType | PairType | LambdaType)
	.SetName(nameof(Type));
	
	public static readonly tSPO_Parser
	Lambda = (+Match -Token("=>") +Expression.OrFail())
	.Modify(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly tSPO_Parser
	Method = (+Match -Token(":") +Match +Block)
	.Modify(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly tSPO_Parser
	RecLambdaItem = (-__ +Ident -Token("=") +(Lambda | C( Lambda )))
	.Modify(mSPO_AST.RecLambdaItem)
	.SetName(nameof(RecLambdaItem));
	
	public static readonly tSPO_Parser
	RecLambda = (
		(-Token("§RECURSIV") -Token("{") -NL +((+RecLambdaItem -NL)[1, null] -Token("}")).OrFail()) |
		(-Token("§RECURSIV") +RecLambdaItem.OrFail())
	)
	.ModifyList(
		aList => mParserGen.ResultList(
			mSPO_AST.RecLambdas(aList.Value.Map(mStd.To<mSPO_AST.tRecLambdaItemNode>))
		)
	)
	.SetName(nameof(RecLambda));
	
	public static readonly tSPO_Parser
	If = (
		-Token("§IF") -Token("{") -NL +(
			(
				-__ +Expression -Token("=>") +(Expression -NL).OrFail()
			).Modify(
				(mSPO_AST.tExpressionNode a1, mSPO_AST.tExpressionNode a2) => (a1, a2)
			)[0, null] -Token("}")
		).OrFail()
	)
	.ModifyList(
		aList => mParserGen.ResultList(
			mSPO_AST.If(
				aList.Map(mStd.To<(mSPO_AST.tExpressionNode, mSPO_AST.tExpressionNode)>)
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
				(mSPO_AST.tMatchNode a1, mSPO_AST.tExpressionNode a2) => (a1, a2)
			)[0, null]
			-Token("}")
		).OrFail()
	)
	.ModifyList(
		aList => {
			aList.GetHeadTail(out var Head, out var Tail);
			
			mDebug.Assert(Head.Match(out mSPO_AST.tExpressionNode Expression));
			
			return mParserGen.ResultList(
				mSPO_AST.IfMatch(
					Expression,
					Tail.Map(mStd.To<(mSPO_AST.tMatchNode, mSPO_AST.tExpressionNode)>)
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
				a.Value?.First().To<mSPO_AST.tMatchNode?>()
			)
		)
	)
	.Modify(
		((mSPO_AST.tIdentNode, tResults) aPair, mSPO_AST.tMatchNode? aMaybeOut) => {
			var (Ident, ChildList) = aPair;
			return mSPO_AST.MethodCall(
				Ident,
				mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>)),
				aMaybeOut
			);
		}
	)
	.SetName(nameof(MethodCall));
	
	public static readonly tSPO_Parser
	MethodCalls = (+MethodCall +(-(Token(",")|NL) +MethodCall)[0, null] -NL[0, 1] -Token("."))
	.ModifyList(
		aList => mParserGen.ResultList(
			aList.Map(mStd.To<mSPO_AST.tMethodCallNode>)
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
	.Modify(mSPO_AST.DefVar)
	.SetName(nameof(DefVar));
	
	public static readonly tSPO_Parser
	VarToVal = (+Ident -Token(":") -Token("=>"))
	.Modify(mSPO_AST.VarToVal)
	.SetName(nameof(VarToVal));
	
	public static readonly tSPO_Parser
	MethodCallStatment = (+(ExpressionInCall -Token(":")) -NL[0, 1] +MethodCalls.OrFail())
	.Modify(mSPO_AST.MethodCallStatment)
	.SetName(nameof(MethodCallStatment));
	
	public static readonly tSPO_Parser
	Import = (-Token("§IMPORT") +(Match -NL).OrFail())
	.Modify(mSPO_AST.Import)
	.SetName(nameof(Import));
	
	public static readonly tSPO_Parser
	Export = ( -Token("§EXPORT") +Expression.OrFail())
	.Modify(mSPO_AST.Export)
	.SetName(nameof(Export));
	
	public static readonly tSPO_Parser
	Module = ( -NL[0, 1] +Import +Commands +Export -NL[0, 1])
	.Modify(mSPO_AST.Module)
	.SetName(nameof(Module));
	
	static mSPO_Parser() {
		UnTypedMatch.Def(
			(Literal|Ident|MatchPrefix|MatchGuard).Modify(
				mSPO_AST.UnTypedMatch
			) |
			C(+Match +(-(-Token(",") | -NL) +Match)[0, null]).ModifyList(
				a => mParserGen.ResultList(
					mSPO_AST.Match(
						mSPO_AST.MatchTuple(
							a.Value.Map(mStd.To<mSPO_AST.tMatchNode>)
						),
						null
					)
				)
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
			Literal |
			Ident |
			Tuple |
			C( Expression )
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
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Parser),
		mTest.Test(
			"Atoms",
			aStreamOut => {
				mStd.AssertEq(
					Num.ParseText("+1_234", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Number(1234))
				);
				mStd.AssertEq(
					Literal.ParseText("+1_234", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Number(1234))
				);
				mStd.AssertEq(
					ExpressionInCall.ParseText("+1_234", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Number(1234))
				);
				
				mStd.AssertEq(
					String.ParseText("\"BLA\"", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Text("BLA"))
				);
				mStd.AssertEq(
					Literal.ParseText("\"BLA\"", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Text("BLA"))
				);
				mStd.AssertEq(
					ExpressionInCall.ParseText("\"BLA\"", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Text("BLA"))
				);
				
				mStd.AssertEq(
					ExpressionInCall.ParseText("BLA", aStreamOut),
					mParserGen.ResultList(mSPO_AST.Ident("BLA"))
				);
			}
		),
		mTest.Test(
			"Tuple",
			aStreamOut => {
				mStd.AssertEq(
					ExpressionInCall.ParseText("(+1_234, \"BLA\")", aStreamOut),
					mParserGen.ResultList(
						mSPO_AST.Tuple(
							mList.List<mSPO_AST.tExpressionNode>(
								mSPO_AST.Number(1234),
								mSPO_AST.Text("BLA")
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
						mSPO_AST.Match(
							mSPO_AST.Number(12),
							null
						)
					)
				);
				mStd.AssertEq(
					Match.ParseText("x", aStreamOut),
					mParserGen.ResultList(
						mSPO_AST.Match(
							mSPO_AST.Ident("x"),
							null
						)
					)
				);
				mStd.AssertEq(
					Match.ParseText("(12, x)", aStreamOut),
					mParserGen.ResultList(
						mSPO_AST.Match(
							mSPO_AST.MatchTuple(
								mList.List(
									mSPO_AST.Match(mSPO_AST.Number(12), null),
									mSPO_AST.Match(mSPO_AST.Ident("x"), null)
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
						mSPO_AST.Call(
							mSPO_AST.Ident("...*..."),
							mSPO_AST.Tuple(
								mList.List<mSPO_AST.tExpressionNode>(
									mSPO_AST.Ident("x"),
									mSPO_AST.Ident("x")
								)
							)
						)
					)
				);
				mStd.AssertEq(
					Expression.ParseText(".sin x", aStreamOut),
					mParserGen.ResultList(
						mSPO_AST.Call(
							mSPO_AST.Ident("sin..."),
							mSPO_AST.Tuple(
								mList.List<mSPO_AST.tExpressionNode>(
									mSPO_AST.Ident("x")
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
						mSPO_AST.Lambda(
							mSPO_AST.Match(
								mSPO_AST.Ident("x"),
								null
							),
							mSPO_AST.Call(
								mSPO_AST.Ident("...*..."),
								mSPO_AST.Tuple(
									mList.List<mSPO_AST.tExpressionNode>(
										mSPO_AST.Ident("x"),
										mSPO_AST.Ident("x")
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
						mSPO_AST.Lambda(
							mSPO_AST.Match(
								mSPO_AST.Match(
									mSPO_AST.Match(
										mSPO_AST.Ident("x"),
										null
									),
									mSPO_AST.Ident("MyType")
								),
								null
							),
							mSPO_AST.Call(
								mSPO_AST.Ident("...*..."),
								mSPO_AST.Tuple(
									mList.List<mSPO_AST.tExpressionNode>(
										mSPO_AST.Ident("x"),
										mSPO_AST.Ident("x")
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
						mSPO_AST.Call(
							mSPO_AST.Ident("...<...<..."),
							mSPO_AST.Tuple(
								mList.List<mSPO_AST.tExpressionNode>(
									mSPO_AST.Number(2),
									mSPO_AST.Call(
										mSPO_AST.Ident("...+..."),
										mSPO_AST.Tuple(
											mList.List<mSPO_AST.tExpressionNode>(
												mSPO_AST.Number(4),
												mSPO_AST.Number(3)
											)
										)
									),
									mSPO_AST.Number(3)
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
						mSPO_AST.Lambda(
							mSPO_AST.Match(
								mSPO_AST.MatchTuple(
									mList.List(
										mSPO_AST.Match(
											mSPO_AST.Ident("a"),
											null
										),
										mSPO_AST.Match(
											mSPO_AST.Ident("b"),
											null
										),
										mSPO_AST.Match(
											mSPO_AST.MatchTuple(
												mList.List(
													mSPO_AST.Match(
														mSPO_AST.Ident("x"),
														null
													),
													mSPO_AST.Match(
														mSPO_AST.Ident("y"),
														null
													),
													mSPO_AST.Match(
														mSPO_AST.Ident("z"),
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
								mSPO_AST.Ident("...*..."),
								mSPO_AST.Tuple(
									mList.List<mSPO_AST.tExpressionNode>(
										mSPO_AST.Ident("a"),
										mSPO_AST.Ident("z")
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
						mSPO_AST.Lambda(
							mSPO_AST.Match(
								mSPO_AST.MatchPrefix(
									mSPO_AST.Ident("...*..."),
									mSPO_AST.Match(
										mSPO_AST.MatchTuple(
											mList.List(
												mSPO_AST.Match(
													mSPO_AST.Number(1),
													null
												),
												mSPO_AST.Match(
													mSPO_AST.Ident("a"),
													null
												)
											)
										),
										null
									)
								),
								null
							),
							mSPO_AST.Ident("a")
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
						mSPO_AST.MethodCallStatment(
							mSPO_AST.Ident("o"),
							mList.List(
								mSPO_AST.MethodCall(
									mSPO_AST.Ident("=..."),
									mSPO_AST.Call(
										mSPO_AST.Ident("...+..."),
										mSPO_AST.Tuple(
											mList.List<mSPO_AST.tExpressionNode>(
												mSPO_AST.VarToVal(
													mSPO_AST.Ident("o")
												),
												mSPO_AST.Ident("i")
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