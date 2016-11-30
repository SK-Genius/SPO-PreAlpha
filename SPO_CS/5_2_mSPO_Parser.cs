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
	public static mStd.tFunc<tSPO_Parser, tChar> Char = mTextParser.GetChar;
	public static mStd.tFunc<tSPO_Parser, tChar> NotChar = mTextParser.GetNotChar;
	public static mStd.tFunc<tSPO_Parser, tText> CharIn = mTextParser.GetCharIn;
	public static mStd.tFunc<tSPO_Parser, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static mStd.tFunc<tSPO_Parser, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static mStd.tFunc<tSPO_Parser, tText> Token = mTextParser.GetToken;
	
	public static tSPO_Parser _ = CharIn(" \t\n")
		.SetDebugName("_");

	public static tSPO_Parser __ = _[0, null]
		.SetDebugName("__");

	public static tSPO_Parser NL = Char('\n')
		.SetDebugName("NL");
	
	public static mStd.tFunc<tSPO_Parser, tText> TOKEN = a => Token(a) + -__;
	
	public static tSPO_Parser _STRING_ = (-Char('"') +NotChar('"')[0, null] -Char('"'))
		.Modify_(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar))
		.Modify(mSPO_AST.Text)
		.SetDebugName(nameof(_STRING_));
	
	public static tSPO_Parser _DIGIT_ = CharInRange('0', '9')
		.Modify((tChar aChar) => (int)aChar - (int)'0')
		.SetDebugName(nameof(_DIGIT_));
	
	public static tSPO_Parser _NAT_ = (_DIGIT_ + (_DIGIT_ | -Char('_'))[0, null])
		.Modify_(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit))
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
	
	public static tSPO_Parser IDENT = ( ( CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) | Token("...") )[1, null] -__ )
		.Modify_(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
		.Modify(mSPO_AST.Ident)
		.SetDebugName(nameof(IDENT));
	
	public static tSPO_Parser LITERAL = ( (_NUM_ | _STRING_) -__ )
		.SetDebugName(nameof(LITERAL));
	
	public static mStd.tFunc<tSPO_Parser, tSPO_Parser> C = aParser => -TOKEN("(") +aParser -__ -TOKEN(")")
		.SetDebugName(nameof(C));
	
	public static tSPO_Parser MATCH = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(MATCH));
	
	public static tSPO_Parser EXPRESSION = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>()
		.SetDebugName(nameof(EXPRESSION));
	
	public static tSPO_Parser ASSIGNMENT = ( +MATCH -TOKEN(":=") +EXPRESSION )
		.Modify(mSPO_AST.Assignment)
		.SetDebugName(nameof(ASSIGNMENT));
	
	public static tSPO_Parser COMMAND = ASSIGNMENT // TODO: Macros, VarDef, MethodCall, Streaming, Block, ...
		.SetDebugName(nameof(COMMAND));
	
	public static tSPO_Parser COMMANDS = (+COMMAND +(-(TOKEN(";")|NL) +COMMAND)[0, null])
		.Modify_(a => mParserGen.ResultList(a.Map(mStd.To<mSPO_AST.tCommandNode>)))
		.SetDebugName(nameof(COMMANDS));
	
	public static tSPO_Parser BLOCK = (-TOKEN("{") -(TOKEN(";")|NL)[0, 1] +COMMANDS -(TOKEN(";")|NL)[0, 1] -TOKEN("}"))
		.Modify(mSPO_AST.Block)
		.SetDebugName(nameof(BLOCK));
	
	public static tSPO_Parser TUPLE = C( +EXPRESSION +( -(-TOKEN(",")|(-NL -__)) +EXPRESSION )[1, null] )
		.Modify_((mParserGen.tResultList a) => mParserGen.ResultList(mSPO_AST.Tuple(a._Value.Map(mStd.To<mSPO_AST.tExpressionNode>))))
		.SetDebugName(nameof(TUPLE));
	
	public static tSPO_Parser CALL1 = C( -TOKEN(".") +IDENT +( +EXPRESSION + IDENT )[0, null] + EXPRESSION[0, 1] )
		.Modify_(
			aList => {
				var Last = (aList._Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "..." : "";
				return (
					mParserGen.ResultList(
						mSPO_AST.Call(
							mSPO_AST.Ident(aList._Value.Every(2).Map(a => a.To<mSPO_AST.tIdentNode>()._Name.Substring(1)).Join((a1, a2) => a1+"..."+a2)+Last),
							mSPO_AST.Tuple(aList._Value.Skip(1).Every(2).Map(mStd.To<mSPO_AST.tExpressionNode>))
						)
					)
				);
			}
		)
		.SetDebugName(nameof(CALL1));
	
	private const tText DotsText = "...";
	private const tText EmptyText = "...";


	public static tSPO_Parser CALL2 = C( +EXPRESSION -TOKEN(".") +IDENT +( +EXPRESSION +IDENT )[0, null] +EXPRESSION[0, 1] )
		.Modify_(
			aList => {
				var Last = (aList._Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "" : "...";
				return (
					mParserGen.ResultList(
						mSPO_AST.Call(
							mSPO_AST.Ident("..."+aList._Value.Skip(1).Every(2).Map(a => a.To<mSPO_AST.tIdentNode>()._Name.Substring(1)).Join((a1, a2) => a1+"..."+a2)+Last),
							mSPO_AST.Tuple(aList._Value.Every(2).Map(mStd.To<mSPO_AST.tExpressionNode>))
						)
					)
				);
			}
		)
		.SetDebugName(nameof(CALL2));
	
	public static tSPO_Parser CALL = (CALL1 | CALL2)
		.SetDebugName(nameof(CALL));
	
	public static tSPO_Parser PREFIX = C( -TOKEN("#") +IDENT +EXPRESSION )
		.Modify(mSPO_AST.Prefix)
		.SetDebugName(nameof(PREFIX));
	
	public static tSPO_Parser MATCH_PREFIX = C( -TOKEN("#") +IDENT +MATCH )
		.Modify(mSPO_AST.MatchPrefix)
		.SetDebugName(nameof(MATCH_PREFIX));
	
	public static tSPO_Parser LAMBDA = C( +MATCH -TOKEN("=>") +EXPRESSION )
		.Modify(mSPO_AST.Lambda)
		.SetDebugName(nameof(LAMBDA));
	
	public static tSPO_Parser IMPORT = ( -TOKEN("§IMPORT") +MATCH )
		.Modify(mSPO_AST.Import)
		.SetDebugName(nameof(IMPORT));
	
//	public static tSPO_Parser EXPORT = ( -TOKEN("§EXPORT") +C() )
//		.Modify(mSPO_AST.Export);
	
//	public static tSPO_Parser MODULE = ( +IMPORT +COMMANDS +EXPORT )
	public static tSPO_Parser MODULE = ( +IMPORT +COMMANDS )
		.Modify(mSPO_AST.Module)
		.SetDebugName(nameof(MODULE));
	
	static mSPO_Parser() {
		MATCH.Def(
			(LITERAL|IDENT|MATCH_PREFIX).Modify((mSPO_AST.tMatchItemNode Match) => mSPO_AST.Match(Match, null)) |
			C(+MATCH +(-TOKEN(",") +MATCH)[0, null]).Modify_(
				(mParserGen.tResultList a) => mParserGen.ResultList(
					mSPO_AST.Match(
						mSPO_AST.MatchTuple(
							a._Value.Map(mStd.To<mSPO_AST.tMatchNode>)
						),
						null
					)
				)
			) |
			(+MATCH -TOKEN("€") +EXPRESSION).Modify(mSPO_AST.Match)
		);
		
		EXPRESSION.Def( LITERAL | IDENT | C( +EXPRESSION ) | TUPLE | CALL | PREFIX | LAMBDA | BLOCK );
	}
	
	#region TEST
	
	private static mParserGen.tResultList Parse(
		this tSPO_Parser aParser,
		tText aText
	) {
		mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> List;
		mTextParser.tFailInfo Info;
		mStd.tTuple<mParserGen.tResultList, mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>>> Result;
		mParserGen.tResultList ResultList;
		mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> Rest;
		
		var Text1 = mTextParser.TextStream(mTextParser.TextToStream(aText));
		mStd.Assert(Text1.MATCH(out List, out Info));
		var MaybeResult1 = aParser.Parse(List);
		mStd.Assert(MaybeResult1.MATCH(out Result), "("+Info._Line+", "+Info._Coll+"): "+Info._ErrorMessage);
		mStd.Assert(Result.MATCH(out ResultList, out Rest));
		return ResultList;
	}
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"Atoms",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(  _NUM_.Parse("+1_234..."), mParserGen.ResultList(mSPO_AST.Number(1234)));
					mStd.AssertEq(LITERAL.Parse("+1_234..."), mParserGen.ResultList(mSPO_AST.Number(1234)));
					mStd.AssertEq(EXPRESSION.Parse("+1_234..."), mParserGen.ResultList(mSPO_AST.Number(1234)));
					
					mStd.AssertEq(_STRING_.Parse("\"BLA\"..."), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					mStd.AssertEq(LITERAL.Parse("\"BLA\"..."), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					mStd.AssertEq(EXPRESSION.Parse("\"BLA\"..."), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					
					mStd.AssertEq(EXPRESSION.Parse("BLA ..."),  mParserGen.ResultList(mSPO_AST.Ident("BLA")));
					return true;
				}
			)
		),
		mStd.Tuple(
			"Tuple",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						EXPRESSION.Parse("(+1_234, \"BLA\")..."),
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
						MATCH.Parse("12"),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mSPO_AST.Number(12),
								null
							)
						)
					);
					mStd.AssertEq(
						MATCH.Parse("(x)"),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mSPO_AST.Ident("x"),
								null
							)
						)
					);
					mStd.AssertEq(
						MATCH.Parse("(12, x)..."),
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
						EXPRESSION.Parse("(x .* x)"),
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
						EXPRESSION.Parse("(.sin x)"),
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
						EXPRESSION.Parse("((x) => (x .* x))..."),
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
						EXPRESSION.Parse("(2 .< (4 .+ 3) < 3)..."),
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
					//TODO: parse Module !!!
					return true;
				}
			)
		)
	);
	
	#endregion
}