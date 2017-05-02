﻿using tBool = System.Boolean;

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
	public static mStd.tFunc<tSPO_Parser, tChar> Char = mTextParser.GetChar;
	public static mStd.tFunc<tSPO_Parser, tChar> NotChar = mTextParser.GetNotChar;
	public static mStd.tFunc<tSPO_Parser, tText> CharIn = mTextParser.GetCharIn;
	public static mStd.tFunc<tSPO_Parser, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static mStd.tFunc<tSPO_Parser, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static mStd.tFunc<tSPO_Parser, tText> Token = mTextParser.GetToken;
	
	public static tSPO_Parser _ = CharIn(" \t\n")
		.SetDebugName(nameof(_));
	
	public static tSPO_Parser __ = _[0, null]
		.SetDebugName(nameof(__));
	
	public static tSPO_Parser NL = Char('\n')
		.SetDebugName(nameof(NL));
	
	public static mStd.tFunc<tSPO_Parser, tText> TOKEN = a => (Token(a) + -__)
		.SetDebugName("\"", a, "\"");
	
	public static tSPO_Parser _STRING_ = (-Char('"') +NotChar('"')[0, null] -Char('"'))
		.ModifyList(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar))
		.Modify(mSPO_AST.Text)
		.SetDebugName(nameof(_STRING_));
	
	public static tSPO_Parser _DIGIT_ = CharInRange('0', '9')
		.Modify((tChar aChar) => (int)aChar - (int)'0')
		.SetDebugName(nameof(_DIGIT_));
	
	public static tSPO_Parser _NAT_ = (_DIGIT_ + (_DIGIT_ | -Char('_'))[0, null])
		.ModifyList(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit))
		.SetDebugName(nameof(_NAT_));
	
	public static tSPO_Parser _POSITIV_ = Char('+')
		.Modify(() => +1)
		.SetDebugName(nameof(_POSITIV_));
	
	public static tSPO_Parser _NEGATIV_ = Char('-')
		.Modify(() => -1)
		.SetDebugName(nameof(_NEGATIV_));
	
	public static tSPO_Parser _SIGNUM_ = (_POSITIV_ | _NEGATIV_)
		.SetDebugName(nameof(_SIGNUM_));
	
	public static tSPO_Parser _INT_ = (_SIGNUM_ + _NAT_)
		.Modify((int aSig, int aAbs) => aSig * aAbs)
		.SetDebugName(nameof(_INT_));
	
	public static tSPO_Parser _NUM_ = ( _INT_ | _NAT_ )
		.Modify(mSPO_AST.Number)
		.SetDebugName(nameof(_NUM_));
	
	public static tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static tSPO_Parser EMPTY = (-TOKEN("()"))
		.Modify(mSPO_AST.Empty)
		.SetDebugName(nameof(EMPTY));
	
	public static tSPO_Parser IDENT = ( ( CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) | Token("...") )[1, null] -__ )
		.ModifyList(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
		.Modify(mSPO_AST.Ident)
		.SetDebugName(nameof(IDENT));
	
	public static tSPO_Parser LITERAL = ( (EMPTY | _NUM_ | _STRING_) -__ )
		.SetDebugName(nameof(LITERAL));
	
	public static mStd.tFunc<tSPO_Parser, tSPO_Parser> C = aParser => -TOKEN("(") +aParser -__ -TOKEN(")");
	
	public static tSPO_Parser MATCH = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(MATCH));
	
	public static tSPO_Parser EXPRESSION_IN_CALL = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(EXPRESSION_IN_CALL));
	
	public static tSPO_Parser EXPRESSION = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(EXPRESSION));
	
	public static tSPO_Parser ASSIGNMENT = ( +MATCH -TOKEN(":=") +EXPRESSION )
		.Modify(mSPO_AST.Assignment)
		.SetDebugName(nameof(ASSIGNMENT));
	
	public static tSPO_Parser RETURN_IF = (-TOKEN("§RETURN") +EXPRESSION -TOKEN("IF") +EXPRESSION)
		.Modify(mSPO_AST.ReturnIf)
		.SetDebugName(nameof(RETURN_IF));
	
	public static tSPO_Parser RETURN = (-TOKEN("§RETURN") +EXPRESSION)
		.Modify((mSPO_AST.tExpressionNode a) => mSPO_AST.ReturnIf(a, mSPO_AST.True()))
		.SetDebugName(nameof(RETURN));
	
	public static tSPO_Parser COMMAND = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(COMMAND));
	
	public static tSPO_Parser COMMANDS = (+COMMAND +(-(TOKEN(";")|NL) +COMMAND)[0, null])[0, 1]
		.ModifyList(a => mParserGen.ResultList(a.Map(mStd.To<mSPO_AST.tCommandNode>)))
		.SetDebugName(nameof(COMMANDS));
	
	public static tSPO_Parser BLOCK = (-TOKEN("{") -(TOKEN(";")|NL)[0, 1] +COMMANDS -(TOKEN(";")|NL)[0, 1] -TOKEN("}"))
		.Modify(mSPO_AST.Block)
		.SetDebugName(nameof(BLOCK));
	
	public static tSPO_Parser TUPLE = C( +EXPRESSION +( -(-TOKEN(",")|(-NL -__)) +EXPRESSION )[1, null] )
		.ModifyList((mParserGen.tResultList a) => mParserGen.ResultList(mSPO_AST.Tuple(a._Value.Map(mStd.To<mSPO_AST.tExpressionNode>))))
		.SetDebugName(nameof(TUPLE));
	
	public static tSPO_Parser Infix(tText aPrefix, tSPO_Parser aChildParser) {
		return (
			(-TOKEN(aPrefix) +IDENT +( +aChildParser + IDENT )[0, null] + aChildParser[0, 1])
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
			(+aChildParser -TOKEN(aPrefix) +IDENT +( +aChildParser +IDENT )[0, null] +aChildParser[0, 1])
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
	
	public static tSPO_Parser CALL = (
		Infix(".", EXPRESSION_IN_CALL).Modify(
			(mStd.tTuple<mSPO_AST.tIdentNode, tResults> aPair) => {
				mSPO_AST.tIdentNode Ident;
				tResults ChildList;
				aPair.MATCH(out Ident, out ChildList);
				return mSPO_AST.Call(
					Ident,
					mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>))
				);
			}
		) |
		(-TOKEN(".") +EXPRESSION_IN_CALL +EXPRESSION_IN_CALL).Modify(mSPO_AST.Call)
	).SetDebugName(nameof(CALL));
	
	public static tSPO_Parser PREFIX = Infix("#", EXPRESSION_IN_CALL)
		.Modify(
			(mStd.tTuple<mSPO_AST.tIdentNode, tResults> aPair) => {
				mSPO_AST.tIdentNode Ident;
				tResults ChildList;
				aPair.MATCH(out Ident, out ChildList);
				return mSPO_AST.Prefix(
					Ident,
					mSPO_AST.Tuple(ChildList.Map(mStd.To<mSPO_AST.tExpressionNode>))
				);
			}
		)
		.SetDebugName(nameof(PREFIX));
	
	public static tSPO_Parser MATCH_PREFIX = C( Infix("#", MATCH) )
		.Modify(
			(mStd.tTuple<mSPO_AST.tIdentNode, tResults> aPair) => {
				mSPO_AST.tIdentNode Ident;
				tResults ChildList;
				aPair.MATCH(out Ident, out ChildList);
				return mSPO_AST.MatchPrefix(
					Ident,
					mSPO_AST.Match(
						mSPO_AST.MatchTuple(ChildList.Map(mStd.To<mSPO_AST.tMatchNode>)),
						null
					)
				);
			}
		)
		.SetDebugName(nameof(MATCH_PREFIX));
	
	public static tSPO_Parser MATCH_GUARD = C( +MATCH -TOKEN("|") +EXPRESSION )
		.Modify(mSPO_AST.MatchGuard)
		.SetDebugName(MATCH_GUARD);

	public static tSPO_Parser LAMBDA = (+MATCH -TOKEN("=>") +EXPRESSION)
		.Modify(mSPO_AST.Lambda)
		.SetDebugName(nameof(LAMBDA));
	
	public static tSPO_Parser REC_LAMBDAS = (-TOKEN("§RECURSIV {") +(+IDENT -TOKEN(":=") +(LAMBDA | C( LAMBDA ))).Modify(mSPO_AST.RecLambdaItem)[1, null] -TOKEN("}"))
		.ModifyList(
			aList => mParserGen.ResultList(
				mSPO_AST.RecLambdas(aList._Value.Map(mStd.To<mSPO_AST.tRecLambdaItemNode>))
			)
		)
		.SetDebugName(nameof(REC_LAMBDAS));
	
	public static tSPO_Parser IF = (
		-TOKEN("§IF") -TOKEN("{") +(
			+EXPRESSION -TOKEN("=>") +EXPRESSION -TOKEN(";")
		)
		.Modify((mSPO_AST.tExpressionNode a1, mSPO_AST.tExpressionNode a2) => mStd.Tuple(a1, a2))
		[0, null] -TOKEN("}")
	)
		.ModifyList(
			aList => mParserGen.ResultList(
				mSPO_AST.If(
					aList.Map(mStd.To<mStd.tTuple<mSPO_AST.tExpressionNode, mSPO_AST.tExpressionNode>>)
				)
			)
		)
		.SetDebugName(nameof(IF));
	
	public static tSPO_Parser IF_MATCH = (
		-TOKEN("§IF") +EXPRESSION -TOKEN("MATCH") -TOKEN("{") +(
			+MATCH -TOKEN("=>") +EXPRESSION -TOKEN(";")
		)
		.Modify((mSPO_AST.tMatchNode a1, mSPO_AST.tExpressionNode a2) => mStd.Tuple(a1, a2))
		[0, null] -TOKEN("}")
	)
		.ModifyList(
			aList => {
				mStd.tAny Head;
				mParserGen.tResultList Tail;
				aList.GetHeadTail(out Head, out Tail);
				
				mSPO_AST.tExpressionNode Expression;
				Head.MATCH(out Expression);
				
				return mParserGen.ResultList(
					mSPO_AST.IfMatch(
						Expression,
						Tail.Map(mStd.To<mStd.tTuple<mSPO_AST.tMatchNode, mSPO_AST.tExpressionNode>>)
					)
				);
			}
		)
		.SetDebugName(nameof(IF_MATCH));
	
	public static tSPO_Parser IMPORT = (-TOKEN("§IMPORT") +MATCH)
		.Modify(mSPO_AST.Import)
		.SetDebugName(nameof(IMPORT));
	
	public static tSPO_Parser EXPORT = ( -TOKEN("§EXPORT") +EXPRESSION )
		.Modify(mSPO_AST.Export);
	
	public static tSPO_Parser MODULE = ( +IMPORT +COMMANDS +EXPORT )
		.Modify(mSPO_AST.Module)
		.SetDebugName(nameof(MODULE));
	
	static mSPO_Parser() {
		MATCH.Def(
			(LITERAL|IDENT|MATCH_PREFIX|MATCH_GUARD).Modify(
				(mSPO_AST.tMatchItemNode Match) => mSPO_AST.Match(Match, null)
			) |
			C(+MATCH +(-TOKEN(",") +MATCH)[0, null]).ModifyList(
				(mParserGen.tResultList a) => mParserGen.ResultList(
					mSPO_AST.Match(
						mSPO_AST.MatchTuple(
							a._Value.Map(mStd.To<mSPO_AST.tMatchNode>)
						),
						null
					)
				)
			) |
			C(+MATCH -TOKEN("€") +EXPRESSION).Modify(mSPO_AST.Match) // TODO: infinit recursion
		);
		
		EXPRESSION.Def(
			IF_MATCH |
			IF |
			BLOCK |
			LAMBDA |
			CALL |
			TUPLE |
			PREFIX |
			C( EXPRESSION ) |
			LITERAL |
			IDENT
		);
		
		EXPRESSION_IN_CALL.Def(
			BLOCK |
			C( LAMBDA ) |
			TUPLE |
			C(
				CALL |
				PREFIX  |
				EXPRESSION_IN_CALL
			) |
			LITERAL |
			IDENT
		);
		
		COMMAND.Def(ASSIGNMENT | REC_LAMBDAS | RETURN_IF | RETURN );
		// TODO: Macros, VarDef, MethodCall, Streaming, Block, ...
	}
	
	#region TEST
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"Atoms",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(_NUM_.ParseText("+1_234", aStreamOut), mParserGen.ResultList(mSPO_AST.Number(1234)));
					mStd.AssertEq(LITERAL.ParseText("+1_234", aStreamOut), mParserGen.ResultList(mSPO_AST.Number(1234)));
					mStd.AssertEq(EXPRESSION_IN_CALL.ParseText("+1_234", aStreamOut), mParserGen.ResultList(mSPO_AST.Number(1234)));
					
					mStd.AssertEq(_STRING_.ParseText("\"BLA\"", aStreamOut), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					mStd.AssertEq(LITERAL.ParseText("\"BLA\"", aStreamOut), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					mStd.AssertEq(EXPRESSION_IN_CALL.ParseText("\"BLA\"", aStreamOut), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					
					mStd.AssertEq(EXPRESSION_IN_CALL.ParseText("BLA", aStreamOut),  mParserGen.ResultList(mSPO_AST.Ident("BLA")));
					return true;
				}
			)
		),
		mStd.Tuple(
			"Tuple",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						EXPRESSION_IN_CALL.ParseText("(+1_234, \"BLA\")", aStreamOut),
						mParserGen.ResultList(
							mSPO_AST.Tuple(
								mList.List<mSPO_AST.tExpressionNode>(
									mSPO_AST.Number(1234),
									mSPO_AST.Text("BLA")
								)
							)
						)
					);
					return true;
				}
			)
		),
		mStd.Tuple(
			"Match",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						MATCH.ParseText("12", aStreamOut),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mSPO_AST.Number(12),
								null
							)
						)
					);
					mStd.AssertEq(
						MATCH.ParseText("x", aStreamOut),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mSPO_AST.Ident("x"),
								null
							)
						)
					);
					mStd.AssertEq(
						MATCH.ParseText("(12, x)", aStreamOut),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mSPO_AST.MatchTuple(
									mList.List<mSPO_AST.tMatchNode>(
										mSPO_AST.Match(mSPO_AST.Number(12), null),
										mSPO_AST.Match(mSPO_AST.Ident("x"), null)
									)
								),
								null
							)
						)
					);
					return true;
				}
			)
		),
		mStd.Tuple(
			"Call",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						EXPRESSION.ParseText("x .* x", aStreamOut),
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
						EXPRESSION.ParseText(".sin x", aStreamOut),
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
					return true;
				}
			)
		),
		mStd.Tuple(
			"Lambda",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						EXPRESSION.ParseText("x => x .* x", aStreamOut),
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
						EXPRESSION.ParseText("2 .< (4 .+ 3) < 3", aStreamOut),
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
					return true;
				}
			)
		),
		mStd.Tuple(
			"NestedMatch",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						EXPRESSION.ParseText("(a, b, (x, y, z)) => a .* z", aStreamOut),
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
					return true;
				}
			)
		),
		mStd.Tuple(
			"PrefixMatch",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						EXPRESSION.ParseText("(1 #* a) => a", aStreamOut),
						mParserGen.ResultList(
							mSPO_AST.Lambda(
								mSPO_AST.Match(
									mSPO_AST.MatchPrefix(
										mSPO_AST.Ident("...*..."),
										mSPO_AST.Match(
											mSPO_AST.MatchTuple(
												mList.List<mSPO_AST.tMatchNode>(
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
					return true;
				}
			)
		)
	);
	
	#endregion
}