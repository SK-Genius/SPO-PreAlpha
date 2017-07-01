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

using tSPO_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

public static class  mSPO_Parser {
	public static readonly mStd.tFunc<tSPO_Parser, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<tSPO_Parser, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<tSPO_Parser, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<tSPO_Parser, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<tSPO_Parser, tText> Text = mTextParser.GetToken;
	
	public static readonly tSPO_Parser _ = CharIn(" \t")
		.SetDebugName(nameof(_));
	
	public static readonly tSPO_Parser __ = _[0, null]
		.SetDebugName(nameof(__));
	
	public static readonly tSPO_Parser NL = (Char('\n') -__)[0, null]
		.SetDebugName(nameof(NL));
	
	public static readonly mStd.tFunc<tSPO_Parser, tText> Token = a => (Text(a) + -__)
		.SetDebugName("\"", a, "\"");
	
	public static readonly tSPO_Parser String = (-Char('"') +NotChar('"')[0, null] -Char('"'))
		.ModifyList(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar))
		.Modify(mSPO_AST.Text)
		.SetDebugName(nameof(String));
	
	public static readonly tSPO_Parser Digit = CharInRange('0', '9')
		.Modify((tChar aChar) => (int)aChar - (int)'0')
		.SetDebugName(nameof(Digit));
	
	public static readonly tSPO_Parser Nat = (Digit + (Digit | -Char('_'))[0, null])
		.ModifyList(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit))
		.SetDebugName(nameof(Nat));
	
	public static readonly tSPO_Parser PosSignum = Char('+')
		.Modify(() => +1)
		.SetDebugName(nameof(PosSignum));
	
	public static readonly tSPO_Parser NegSignum = Char('-')
		.Modify(() => -1)
		.SetDebugName(nameof(NegSignum));
	
	public static readonly tSPO_Parser Signum = (PosSignum | NegSignum)
		.SetDebugName(nameof(Signum));
	
	public static readonly tSPO_Parser Int = (Signum + Nat)
		.Modify((int aSig, int aAbs) => aSig * aAbs)
		.SetDebugName(nameof(Int));
	
	public static readonly tSPO_Parser Num = ( Int | Nat )
		.Modify(mSPO_AST.Number)
		.SetDebugName(nameof(Num));
	
	public static tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly tSPO_Parser Empty = (-Token("()"))
		.Modify(mSPO_AST.Empty)
		.SetDebugName(nameof(Empty));
	
	public static readonly tSPO_Parser Ident = ( ( CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) | Text("...") )[1, null] -__ )
		.ModifyList(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
		.Modify(mSPO_AST.Ident)
		.SetDebugName(nameof(Ident));
	
	public static readonly tSPO_Parser Literal = ( (Empty | Num | String) -__ )
		.SetDebugName(nameof(Literal));
	
	public static mStd.tFunc<tSPO_Parser, tSPO_Parser> C = aParser => (
		(-Token("(") -NL +aParser -NL -Token(")")) |
		(-Token("(") +aParser -Token(")"))
	);
	
	public static readonly tSPO_Parser Match = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(Match));
	
	public static readonly tSPO_Parser ExpressionInCall = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(ExpressionInCall));
	
	public static readonly tSPO_Parser Expression = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(Expression));
	
	public static readonly tSPO_Parser Assignment = ( +Match -Token(":=") +Expression )
		.Modify(mSPO_AST.Assignment)
		.SetDebugName(nameof(Assignment));
	
	public static readonly tSPO_Parser ReturnIf = (-Token("§RETURN") +Expression -Token("IF") +Expression)
		.Modify(mSPO_AST.ReturnIf)
		.SetDebugName(nameof(ReturnIf));
	
	public static readonly tSPO_Parser Return = (-Token("§RETURN") +Expression)
		.Modify((mSPO_AST.tExpressionNode a) => mSPO_AST.ReturnIf(a, mSPO_AST.True()))
		.SetDebugName(nameof(Return));
	
	public static readonly tSPO_Parser Command = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(Command));
	
	public static readonly tSPO_Parser Commands = (+Command +Command[0, null])[0, 1]
		.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mSPO_AST.tCommandNode>)))
		.SetDebugName(nameof(Commands));
	
	public static readonly tSPO_Parser Bolck = (-Token("{") -NL +Commands -Token("}"))
		.Modify(mSPO_AST.Block)
		.SetDebugName(nameof(Bolck));
	
	public static readonly tSPO_Parser Tuple = C( +Expression +( -(-Token(",")|-NL) +Expression )[1, null] )
		.ModifyList(
			a => mParserGen.ResultList(mSPO_AST.Tuple(a._Value.Map(mStd.To<mSPO_AST.tExpressionNode>))))
		.SetDebugName(nameof(Tuple));
	
	public static tSPO_Parser Infix(tText aPrefix, tSPO_Parser aChildParser) {
		return (
			(-Token(aPrefix) +Ident +( +aChildParser + Ident )[0, null] + aChildParser[0, 1])
			.ModifyList(
				aList => {
					var Last = (aList._Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "..." : "";
					return (
						mParserGen.ResultList(
							mStd.Tuple(
								mSPO_AST.Ident(aList._Value.Every(2).Map(a => a.To<mSPO_AST.tIdentNode>()._Name.Substring(1)).Join((a1, a2) => $"{a1}...{a2}")+Last),
								aList._Value.Skip(1).Every(2)
							)
						)
					);
				}
			)
		) | (
			(+aChildParser -Token(aPrefix) +Ident +( +aChildParser +Ident )[0, null] +aChildParser[0, 1])
			.ModifyList(
				aList => {
					var Last = (aList._Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "" : "...";
					return (
						mParserGen.ResultList(
							mStd.Tuple(
								mSPO_AST.Ident("..."+aList._Value.Skip(1).Every(2).Map(a => a.To<mSPO_AST.tIdentNode>()._Name.Substring(1)).Join((a1, a2) => a1+"..."+a2)+Last),
								aList._Value.Every(2)
							)
						)
					);
				}
			)
		);
	}
	
	public static readonly tSPO_Parser Call = (
		Infix(".", ExpressionInCall).Modify(
			(mStd.tTuple<mSPO_AST.tIdentNode, tResults> aPair) => {
				mSPO_AST.tIdentNode Ident;
				tResults ChildList;
				aPair.Match(out Ident, out ChildList);
				return mSPO_AST.Call(
					Ident,
					mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>))
				);
			}
		) |
		(-Token(".") +ExpressionInCall +ExpressionInCall).Modify(mSPO_AST.Call)
	).SetDebugName(nameof(Call));
	
	public static readonly tSPO_Parser Prefix = Infix("#", ExpressionInCall)
		.Modify(
			(mStd.tTuple<mSPO_AST.tIdentNode, tResults> aPair) => {
				mSPO_AST.tIdentNode Ident;
				tResults ChildList;
				aPair.Match(out Ident, out ChildList);
				return mSPO_AST.Prefix(
					Ident,
					mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>))
				);
			}
		)
		.SetDebugName(nameof(Prefix));
	
	public static readonly tSPO_Parser MatchPrefix = C( Infix("#", Match) )
		.Modify(
			(mStd.tTuple<mSPO_AST.tIdentNode, tResults> aPair) => {
				mSPO_AST.tIdentNode Ident;
				tResults ChildList;
				aPair.Match(out Ident, out ChildList);
				return mSPO_AST.MatchPrefix(
					Ident,
					mSPO_AST.Match(
						mSPO_AST.MatchTuple(ChildList.Map(mStd.To<mSPO_AST.tMatchNode>)),
						null
					)
				);
			}
		)
		.SetDebugName(nameof(MatchPrefix));
	
	public static readonly tSPO_Parser MatchGuard = C( +Match -Token("|") +Expression )
		.Modify(mSPO_AST.MatchGuard)
		.SetDebugName(nameof(MatchGuard));

	public static readonly tSPO_Parser Lambda = (+Match -Token("=>") +Expression)
		.Modify(mSPO_AST.Lambda)
		.SetDebugName(nameof(Lambda));
	
	public static readonly tSPO_Parser RecLambdaItem = (-__ +Ident -Token(":=") +(Lambda | C( Lambda )) -NL)
		.Modify(mSPO_AST.RecLambdaItem)
		.SetDebugName(nameof(RecLambdaItem));

	public static readonly tSPO_Parser RecLambda = (
		(-Token("§RECURSIV") -Token("{") -NL +RecLambdaItem[1, null] -Token("}")) |
		(-Token("§RECURSIV") +RecLambdaItem)
	)
		.ModifyList(
			aList => mParserGen.ResultList(
				mSPO_AST.RecLambdas(aList._Value.Map(mStd.To<mSPO_AST.tRecLambdaItemNode>))
			)
		)
		.SetDebugName(nameof(RecLambda));
	
	public static readonly tSPO_Parser If = (
		-Token("§IF") -Token("{") -NL +(
			-__ +Expression -Token("=>") +Expression -NL
		).Modify(
			(mSPO_AST.tExpressionNode a1, mSPO_AST.tExpressionNode a2) => mStd.Tuple(a1, a2)
		)[0, null] -Token("}")
	)
		.ModifyList(
			aList => mParserGen.ResultList(
				mSPO_AST.If(
					aList.Map(mStd.To<mStd.tTuple<mSPO_AST.tExpressionNode, mSPO_AST.tExpressionNode>>)
				)
			)
		)
		.SetDebugName(nameof(If));
	
	public static readonly tSPO_Parser IfMatch = (
		-Token("§IF") +Expression -Token("MATCH") -Token("{") -NL +(
			-__ +Match -Token("=>") +Expression -NL
		)
		.Modify((mSPO_AST.tMatchNode a1, mSPO_AST.tExpressionNode a2) => mStd.Tuple(a1, a2))
		[0, null] -Token("}")
	)
		.ModifyList(
			aList => {
				mStd.tAny Head;
				mParserGen.tResultList Tail;
				aList.GetHeadTail(out Head, out Tail);
				
				mSPO_AST.tExpressionNode Expression;
				Head.Match(out Expression);
				
				return mParserGen.ResultList(
					mSPO_AST.IfMatch(
						Expression,
						Tail.Map(mStd.To<mStd.tTuple<mSPO_AST.tMatchNode, mSPO_AST.tExpressionNode>>)
					)
				);
			}
		)
		.SetDebugName(nameof(IfMatch));
	
	public static readonly tSPO_Parser Import = (-Token("§IMPORT") +Match -NL)
		.Modify(mSPO_AST.Import)
		.SetDebugName(nameof(Import));
	
	public static readonly tSPO_Parser Export = ( -Token("§EXPORT") +Expression)
		.Modify(mSPO_AST.Export);
	
	public static readonly tSPO_Parser Module = ( +Import +Commands +Export -NL[0, 1])
		.Modify(mSPO_AST.Module)
		.SetDebugName(nameof(Module));
	
	static mSPO_Parser() {
		Match.Def(
			(Literal|Ident|MatchPrefix|MatchGuard).Modify(
				(mSPO_AST.tMatchItemNode aMatch) => mSPO_AST.Match(aMatch, null)
			) |
			C(+Match +(-(-Token(",") | -NL) +Match)[0, null]).ModifyList(
				a => mParserGen.ResultList(
					mSPO_AST.Match(
						mSPO_AST.MatchTuple(
							a._Value.Map(mStd.To<mSPO_AST.tMatchNode>)
						),
						null
					)
				)
			) |
			C(+Match -Token("€") +Expression).Modify(mSPO_AST.Match) // TODO: infinit recursion
		);
		
		Expression.Def(
			IfMatch |
			If |
			Bolck |
			Lambda |
			Call |
			Tuple |
			Prefix |
			C( Expression ) |
			Literal |
			Ident
		);
		
		ExpressionInCall.Def(
			Bolck |
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
		
		Command.Def((Assignment -NL) | (RecLambda -NL) | (ReturnIf -NL) | (Return -NL) );
		// TODO: Macros, VarDef, MethodCall, Streaming, Block, ...
	}
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Parser),
		mTest.Test(
			"Atoms",
			aStreamOut => {
				mStd.AssertEq(Num.ParseText("+1_234", aStreamOut), mParserGen.ResultList(mSPO_AST.Number(1234)));
				mStd.AssertEq(Literal.ParseText("+1_234", aStreamOut), mParserGen.ResultList(mSPO_AST.Number(1234)));
				mStd.AssertEq(ExpressionInCall.ParseText("+1_234", aStreamOut), mParserGen.ResultList(mSPO_AST.Number(1234)));
					
				mStd.AssertEq(String.ParseText("\"BLA\"", aStreamOut), mParserGen.ResultList(mSPO_AST.Text("BLA")));
				mStd.AssertEq(Literal.ParseText("\"BLA\"", aStreamOut), mParserGen.ResultList(mSPO_AST.Text("BLA")));
				mStd.AssertEq(ExpressionInCall.ParseText("\"BLA\"", aStreamOut), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					
				mStd.AssertEq(ExpressionInCall.ParseText("BLA", aStreamOut),  mParserGen.ResultList(mSPO_AST.Ident("BLA")));
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