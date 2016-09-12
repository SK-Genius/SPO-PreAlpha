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
	
	public static tSPO_Parser _ = CharIn(" \t");
	public static tSPO_Parser __ = _[0, null];
	public static tSPO_Parser NL = Char('\n');
	
	public static mStd.tFunc<tSPO_Parser, tText> TOKEN = a => Token(a) + -__;
	
	public static tSPO_Parser _STRING_ = (-Char('"') +NotChar('"')[0, null] -Char('"'))
		.Modify_(aChars => aChars.Reduce("", (tText aText, tChar aChar) => aText + aChar))
		.Modify(mSPO_AST.Text);
	
	public static tSPO_Parser _DIGIT_ = CharInRange('0', '9')
		.Modify((tChar aChar) => (int)aChar - (int)'0');
	
	public static tSPO_Parser _NAT_ = (_DIGIT_ + (_DIGIT_ | -Char('_'))[0, null])
		.Modify_(a => a.Reduce(0, (tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit));
	
	public static tSPO_Parser _POSITIV_ = Char('+')
		.Modify(() => +1);
	
	public static tSPO_Parser _NEGATIV_ = Char('-')
		.Modify(() => -1);
	
	public static tSPO_Parser _SIGNUM_ = _POSITIV_ | _NEGATIV_;
	
	public static tSPO_Parser _INT_ = (_SIGNUM_ + _NAT_)
		.Modify((int aSig, int aAbs) => aSig * aAbs);
	
	public static tSPO_Parser _NUM_ = ( _INT_ | _NAT_ )
		.Modify(mSPO_AST.Number);
	
	public static tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static tSPO_Parser IDENT = ( ( CharNotIn(SpazialChars).Modify((tChar aChar) => aChar.ToString()) | Token("...") )[1, null] -__ )
		.Modify_(aChars => aChars.Reduce("", (tText a1, tText a2) => a1 + a2))
		.Modify(mSPO_AST.Ident);
	
	public static tSPO_Parser LITERAL = ( (_NUM_ | _STRING_) -__ );
	
	public static mStd.tFunc<tSPO_Parser, tSPO_Parser> C = aParser => -TOKEN("(") +aParser -__ -TOKEN(")");
	
	public static tSPO_Parser MATCH = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>();
	
	public static tSPO_Parser ELEMENT = mParserGen.UndefParser<mStd.tTuple<tChar, mStd.tAction<tText>>>();
	
	public static tSPO_Parser ASSIGNMENT = ( +MATCH -TOKEN("=") +ELEMENT )
		.Modify(mSPO_AST.Assignment);
	
	public static tSPO_Parser COMMAND = ASSIGNMENT; // TODO: Macros, VarDef, MethodCall, Streaming, ...
	
	public static tSPO_Parser BLOCK = (-TOKEN("{") -(TOKEN(";")|NL)[0, 1] +COMMAND +(-(TOKEN(";")|NL) +COMMAND)[0, null] -(TOKEN(";")|NL)[0, 1] -TOKEN("}"))
		.Modify_(a => mParserGen.ResultList(a._Value.Map(mStd.To<mSPO_AST.tCommandNode>)));
	
	public static tSPO_Parser TUPLE = C( +ELEMENT +( -(-TOKEN(",")|(-NL -__)) +ELEMENT )[1, null] )
		.Modify_((mParserGen.tResultList a) => mParserGen.ResultList(mSPO_AST.Tuple(a._Value.Map(mStd.To<mSPO_AST.tExpressionNode>))));
	
	public static tSPO_Parser CALL1 = C( -TOKEN(".") +IDENT +( +ELEMENT + IDENT )[0, null] + ELEMENT[0, 1] )
		.Modify_(
			aList => {
				var Last = (aList._Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "..." : "";
				return (
					mParserGen.ResultList(
						mSPO_AST.Call(
							mSPO_AST.Ident(aList._Value.Every(2).Map(a => a.To<mSPO_AST.tIdentNode>()._Name).Join((a1, a2) => a1+"..."+a2)+Last),
							mSPO_AST.Tuple(aList._Value.Skip(1).Every(2).Map(mStd.To<mSPO_AST.tExpressionNode>))
						)
					)
				);
			}
		);
	
	private const tText DotsText = "...";
	private const tText EmptyText = "...";


	public static tSPO_Parser CALL2 = C( +ELEMENT -TOKEN(".") +IDENT +( +ELEMENT +IDENT )[0, null] +ELEMENT[0, 1] )
		.Modify_(
			aList => {
				var Last = (aList._Value.Reduce(0, (a, a_) => a + 1) % 2 == 0) ? "" : "...";
				return (
					mParserGen.ResultList(
						mSPO_AST.Call(
							mSPO_AST.Ident("..."+aList._Value.Skip(1).Every(2).Map(a => a.To<mSPO_AST.tIdentNode>()._Name).Join((a1, a2) => a1+"..."+a2)+Last),
							mSPO_AST.Tuple(aList._Value.Every(2).Map(mStd.To<mSPO_AST.tExpressionNode>))
						)
					)
				);
			}
		);
	
	public static tSPO_Parser CALL = CALL1 | CALL2;
	
	public static tSPO_Parser LAMBDA = C( +MATCH -TOKEN("=>") +BLOCK )
		.Modify(mSPO_AST.Lambda);
	
	public static tSPO_Parser MODUL = (ASSIGNMENT +(-NL +ASSIGNMENT)[0, null])
		.Modify_(a => mParserGen.ResultList(a.Map(mStd.To<mSPO_AST.tAssignmantNode>)));
	
	static mSPO_Parser() {
		MATCH.Def(
			C( +(MATCH|LITERAL|IDENT) +( -TOKEN(",") +(MATCH|LITERAL|IDENT) )[0, null] ) .Modify_(
				a => mParserGen.ResultList(mSPO_AST.Match(a._Value.Map(mStd.To<mSPO_AST.tMatchItemNode>)))
			)
		);
		
		ELEMENT.Def( LITERAL | IDENT | C( +ELEMENT ) | TUPLE | CALL | LAMBDA );
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
	
	// TODO: test
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"Atoms",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(  _NUM_.Parse("+1_234..."), mParserGen.ResultList(mSPO_AST.Number(1234)));
					mStd.AssertEq(LITERAL.Parse("+1_234..."), mParserGen.ResultList(mSPO_AST.Number(1234)));
					mStd.AssertEq(ELEMENT.Parse("+1_234..."), mParserGen.ResultList(mSPO_AST.Number(1234)));
					
					mStd.AssertEq(_STRING_.Parse("\"BLA\"..."), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					mStd.AssertEq(LITERAL.Parse("\"BLA\"..."), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					mStd.AssertEq(ELEMENT.Parse("\"BLA\"..."), mParserGen.ResultList(mSPO_AST.Text("BLA")));
					
					mStd.AssertEq(ELEMENT.Parse("BLA ..."),  mParserGen.ResultList(mSPO_AST.Ident("BLA")));
					return true;
				}
			)
		),
		mStd.Tuple(
			"Tuple",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(
						ELEMENT.Parse("(+1_234, \"BLA\")..."),
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
						MATCH.Parse("(12)"),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mList.List<mSPO_AST.tMatchItemNode>(
									mSPO_AST.Number(12)
								)
							)
						)
					);
					mStd.AssertEq(
						MATCH.Parse("(x)"),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mList.List<mSPO_AST.tMatchItemNode>(
									mSPO_AST.Ident("x")
								)
							)
						)
					);
					mStd.AssertEq(
						MATCH.Parse("(12, x)..."),
						mParserGen.ResultList(
							mSPO_AST.Match(
								mList.List<mSPO_AST.tMatchItemNode>(
									mSPO_AST.Number(12),
									mSPO_AST.Ident("x")
								)
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
						ELEMENT.Parse("(x .* x)"),
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
						ELEMENT.Parse("(.sin x)"),
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
			"???",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					// TODO
					#if true
					mStd.AssertEq(
						ELEMENT.Parse("((x) => { (a) = (x .* x) })..."),
						mParserGen.ResultList(
							mSPO_AST.Lambda(
								mSPO_AST.Match(
									mList.List<mSPO_AST.tMatchItemNode>(
										mSPO_AST.Ident("x")
									)
								),
								mList.List<mSPO_AST.tCommandNode>(
									mSPO_AST.Assignment(
										mSPO_AST.Match(
											mList.List<mSPO_AST.tMatchItemNode>(
												mSPO_AST.Ident("a")
											)
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
							)
						)
					);
					
					mStd.AssertEq(
						ELEMENT.Parse("(2 .< (4 .+ 3) < 3)..."),
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
					#endif
					//TODO: parse Module !!!
					return true;
				}
			)
		)
	);
	
	#endregion
}