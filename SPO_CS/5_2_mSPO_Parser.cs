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
	public static readonly mStd.tFunc<tSPO_Parser, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<tSPO_Parser, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<tSPO_Parser, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<tSPO_Parser, tText> Text = mTextParser.GetToken;
	
	public static readonly tSPO_Parser
	_ = CharIn(" \t")
	.SetName(nameof(_));
	
	public static readonly tSPO_Parser
	__ = _[0, null]
	.SetName(nameof(__));
	
	public static readonly tSPO_Parser
	NL = (Char('\n') -__)[0, null]
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
	
	public static tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
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
	Match = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(Match));
	
	public static readonly tSPO_Parser
	ExpressionInCall = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(ExpressionInCall));
	
	public static readonly tSPO_Parser
	Expression = mParserGen.UndefParser<mTextParser.tPosChar, mTextParser.tError>()
	.SetName(nameof(Expression));
	
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
	Commands = (+Command +Command[0, null])[0, 1]
	.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mSPO_AST.tCommandNode>)))
	.SetName(nameof(Commands));
	
	public static readonly tSPO_Parser
	Block = (-Token("{") +(-NL +Commands -Token("}")).OrFail())
	.Modify(mSPO_AST.Block)
	.SetName(nameof(Block));
	
	public static readonly tSPO_Parser
	Tuple = C( +Expression +( -(-Token(",")|-NL) +Expression)[1, null] )
	.ModifyList(
		a => mParserGen.ResultList(
			mSPO_AST.Tuple(a.Value.Map(mStd.To<mSPO_AST.tExpressionNode>))
		)
	)
	.SetName(nameof(Tuple));
	
	//================================================================================
	public static tSPO_Parser
	Infix(
		tText aPrefix,
		tSPO_Parser aChildParser
	//================================================================================
	) {
		return (
			(-Token(aPrefix) +Ident +( +aChildParser + Ident )[0, null] + aChildParser[0, 1])
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
		) | (
			(+aChildParser -Token(aPrefix) +Ident +( +aChildParser +Ident )[0, null] +aChildParser[0, 1])
			.ModifyList(
				aList => {
					var Last = (aList.Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "" : "...";
					return (
						mParserGen.ResultList(
							(
								mSPO_AST.Ident(
									"..."+aList.Value.Skip(1).Every(2).Map(
										a => a.To<mSPO_AST.tIdentNode>().Name.Substring(1)
									).Join(
										(a1, a2) => $"{a1}...{a2}"
									)+Last
								),
								aList.Value.Every(2)
							)
						)
					);
				}
			)
		);
	}
	
	public static readonly tSPO_Parser
	Call = (
		Infix(".", ExpressionInCall).Modify(
			((mSPO_AST.tIdentNode, tResults) aPair) => {
				var (Ident, ChildList) = aPair;
				return mSPO_AST.Call(
					Ident,
					mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>))
				);
			}
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
	MatchPrefix = C( Infix("#", Match) )
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
	MatchGuard = C( +Match -Token("|") +Expression.OrFail() )
	.Modify(mSPO_AST.MatchGuard)
	.SetName(nameof(MatchGuard));
	
	public static readonly tSPO_Parser
	Lambda = (+Match -Token("=>") +Expression.OrFail())
	.Modify(mSPO_AST.Lambda)
	.SetName(nameof(Lambda));
	
	public static readonly tSPO_Parser
	Method = (+Match -Token(":") +Match +Block)
	.Modify(mSPO_AST.Method)
	.SetName(nameof(Method));
	
	public static readonly tSPO_Parser
	RecLambdaItem = (-__ +Ident -Token("=") +(Lambda | C( Lambda )) -NL)
	.Modify(mSPO_AST.RecLambdaItem)
	.SetName(nameof(RecLambdaItem));
	
	public static readonly tSPO_Parser
	RecLambda = (
		(-Token("§RECURSIV") -Token("{") -NL +(RecLambdaItem[1, null] -Token("}")).OrFail()) |
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
		-Token("§IF") +Expression -Token("MATCH")
		+(
			-Token("{") -NL
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
			
			mStd.Assert(Head.Match(out mSPO_AST.tExpressionNode Expression));
			
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
		+Infix("", Expression)
		+(-Token("=>") -Token("§DEF") +Match)[0, 1].ModifyList(
			a => mParserGen.ResultList(
				a.Value.IsEmpty()
				? null
				: (mSPO_AST.tMatchNode?)mStd.To<mSPO_AST.tMatchNode>(a.Value.First())
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
	MethodCalls = (+MethodCall +(-(-Token(",") | -NL) +MethodCall)[0, null] -NL[0, 1] -Token("."))
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
		Match.Def(
			(Literal|Ident|MatchPrefix|MatchGuard).Modify(
				(mSPO_AST.tMatchItemNode aMatch) => mSPO_AST.Match(aMatch, null)
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
			) |
			C(+Match -Token("€") +Expression).Modify(mSPO_AST.Match) // TODO: BUG - infinit recursion (maybe solved ???)
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
			C( Expression ) |
			Literal |
			Ident
		);
		
		ExpressionInCall.Def(
			Block |
			C( Lambda ) |
			Tuple |
			C(
				Call |
				Prefix  |
				ExpressionInCall
			) |
			Literal |
			Ident
		);
		
		// TODO: Macros, Streaming, Block, ...
		Command.Def(
			(Def -NL) |
			(DefVar -NL) |
			(MethodCallStatment -NL) |
			(RecLambda -NL) |
			(ReturnIf -NL) |
			(Return -NL)
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
			"Match",
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
			"Call",
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
		)
	);
	
	#endregion
}