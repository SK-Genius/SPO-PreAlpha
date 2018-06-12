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

using tIL_Parser = mParserGen.tParser<mTextParser.tPos, mIL_Tokenizer.tToken, mTextParser.tError>;
using tPos = mTextParser.tPos;

public static class  mIL_Parser {
	public static readonly tIL_Parser
	NL = mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.SpecialToken && a.Text == "\n",
		a => new mTextParser.tError {
			Message = "expect end of line",
			Pos = a.Item1.Start
		}
	).SetDebugName(nameof(NL));
	
	public static readonly tIL_Parser
	QuotedString  = mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.Text,
		a => new mTextParser.tError {
			Message = "expect '\"'",
			Pos = a.Item1.Start
		}
	).SetDebugName(nameof(QuotedString));
	
	public static readonly tIL_Parser
	Number = mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.Number,
		a => new mTextParser.tError {
			Message = "expect number",
			Pos = a.Item1.Start
		}
	).SetDebugName(nameof(Number));
	
	public static readonly tIL_Parser
	Ident = mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.Ident,
		a => new mTextParser.tError {
			Message = "expect identifier",
			Pos = a.Item1.Start
		}
	).SetDebugName(nameof(Ident));
	
	public static readonly tIL_Parser
	Prefix = mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.Prefix,
		a => new mTextParser.tError {
			Message = "expect identifier",
			Pos = a.Item1.Start
		}
	).SetDebugName(nameof(Prefix));
	
	//================================================================================
	public static tIL_Parser
	Token(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.Ident && a.Text == aText,
		a => new mTextParser.tError {
			Message = "expect identifier",
			Pos = a.Item1.Start
		}
	).SetDebugName($"{nameof(Token)}('{aText}')");
	
	//================================================================================
	public static tIL_Parser
	SpecialToken(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.SpecialToken && a.Text == aText,
		a => new mTextParser.tError {
			Message = "expect identifier",
			Pos = a.Item1.Start
		}
	).SetDebugName($"{nameof(SpecialToken)}('{aText}')");
	
	//================================================================================
	public static tIL_Parser
	KeyWord(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, mIL_Tokenizer.tToken, mTextParser.tError>(
		a => a.Type == mIL_Tokenizer.tTokenType.KeyWord && a.Text == aText,
		a => new mTextParser.tError {
			Message = "expect identifier",
			Pos = a.Item1.Start
		}
	).SetDebugName($"{nameof(KeyWord)}('{aText}')");
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>> aFunc
	//================================================================================
	) => (
		mStd.tSpan<tPos> aSpan
	) => aFunc(aSpan);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, mIL_Tokenizer.tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText> aFunc
	//================================================================================
	) => (
		mStd.tSpan<tPos> aSpan,
		mIL_Tokenizer.tToken a1
	) => aFunc(aSpan, a1.Text);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, mIL_Tokenizer.tToken, mIL_Tokenizer.tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText, tText> aFunc
	//================================================================================
	) => (
		mStd.tSpan<tPos> aSpan,
		mIL_Tokenizer.tToken a1,
		mIL_Tokenizer.tToken a2
	) => aFunc(aSpan, a1.Text, a2.Text);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, mIL_Tokenizer.tToken, mIL_Tokenizer.tToken, mIL_Tokenizer.tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText, tText, tText> aFunc
	//================================================================================
	) => (
		mStd.tSpan<tPos> aSpan,
		mIL_Tokenizer.tToken a1,
		mIL_Tokenizer.tToken a2,
		mIL_Tokenizer.tToken a3
	) => aFunc(aSpan, a1.Text, a2.Text, a3.Text);
	
	public static readonly tIL_Parser
	Command = (
		(+Ident -SpecialToken(":=") +Number)
		.Modify(X(mIL_AST.CreateInt))
		.SetDebugName(nameof(mIL_AST.CreateInt)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("BOOL") +Ident -Token("&") +Ident.OrFail())
		.Modify(X(mIL_AST.And))
		.SetDebugName(nameof(mIL_AST.And)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("BOOL") +Ident -Token("|") +Ident.OrFail())
		.Modify(X(mIL_AST.Or))
		.SetDebugName(nameof(mIL_AST.Or)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("BOOL") +Ident -Token("^") +Ident.OrFail())
		.Modify(X(mIL_AST.XOr))
		.SetDebugName(nameof(mIL_AST.XOr)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("INT") +Ident -Token("==") +Ident.OrFail())
		.Modify(X(mIL_AST.IntsAreEq))
		.SetDebugName(nameof(mIL_AST.IntsAreEq)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("INT") +Ident -Token("<=>") +Ident.OrFail())
		.Modify(X(mIL_AST.IntsComp))
		.SetDebugName(nameof(mIL_AST.IntsComp)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("INT") +Ident -Token("+") +Ident.OrFail())
		.Modify(X(mIL_AST.IntsAdd))
		.SetDebugName(nameof(mIL_AST.IntsAdd)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("INT") +Ident -Token("-") +Ident.OrFail())
		.Modify(X(mIL_AST.IntsSub))
		.SetDebugName(nameof(mIL_AST.IntsSub)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("INT") +Ident -Token("*") +Ident.OrFail())
		.Modify(X(mIL_AST.IntsMul))
		.SetDebugName(nameof(mIL_AST.IntsMul)) |
		
		(+Ident -SpecialToken(":=") +Ident -SpecialToken(",") +Ident.OrFail())
		.Modify(X(mIL_AST.CreatePair))
		.SetDebugName(nameof(mIL_AST.CreatePair)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("1ST") +Ident.OrFail())
		.Modify(X(mIL_AST.GetFirst))
		.SetDebugName(nameof(mIL_AST.GetFirst)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("2ND") +Ident.OrFail())
		.Modify(X(mIL_AST.GetSecond))
		.SetDebugName(nameof(mIL_AST.GetSecond)) |
		
		(+Ident -SpecialToken(":=") -Token("+") +(+Prefix +Ident).OrFail())
		.Modify(X(mIL_AST.AddPrefix))
		.SetDebugName(nameof(mIL_AST.AddPrefix)) |
		
		(+Ident -SpecialToken(":=") -Token("-") +(+Prefix +Ident).OrFail())
		.Modify(X(mIL_AST.SubPrefix))
		.SetDebugName(nameof(mIL_AST.SubPrefix)) |
		
		(+Ident -SpecialToken(":=") -Token("?") +(+Prefix +Ident).OrFail())
		.Modify(X(mIL_AST.HasPrefix))
		.SetDebugName(nameof(mIL_AST.HasPrefix)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken(".") +Ident +Ident)
		.Modify(X(mIL_AST.Call))
		.SetDebugName(nameof(mIL_AST.Call)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("OBJ") -SpecialToken(":") +(+Ident +Ident).OrFail())
		.Modify(X(mIL_AST.Exec))
		.SetDebugName(nameof(mIL_AST.Exec)) |
		
		(-KeyWord("PUSH") +Ident.OrFail())
		.Modify(X(mIL_AST.Push))
		.SetDebugName(nameof(mIL_AST.Push)) |
		
		(-KeyWord("POP"))
		.Modify(X(mIL_AST.Pop))
		.SetDebugName(nameof(mIL_AST.Pop)) |
		
		(-KeyWord("VAR") +(+Ident -Token("<-") +Ident).OrFail())
		.Modify(X(mIL_AST.VarSet))
		.SetDebugName(nameof(mIL_AST.VarSet)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("VAR") +Ident -Token("->"))
		.Modify(X(mIL_AST.VarGet))
		.SetDebugName(nameof(mIL_AST.VarGet)) |
		
		(+Ident -SpecialToken(":=") -KeyWord("VAR") +Ident.OrFail())
		.Modify(X(mIL_AST.VarDef))
		.SetDebugName(nameof(mIL_AST.VarDef)) |
		
		(-KeyWord("RETURN") +(+Ident -Token("IF") +Ident).OrFail())
		.Modify(X(mIL_AST.ReturnIf))
		.SetDebugName(nameof(mIL_AST.ReturnIf)) |
		
		(-KeyWord("REPEAT") +(Ident -Token("IF") +Ident).OrFail())
		.Modify(X(mIL_AST.RepeatIf))
		.SetDebugName(nameof(mIL_AST.RepeatIf)) |
		
		(-KeyWord("ASSERT") +(+Ident -Token("=>") +Ident).OrFail())
		.Modify(X(mIL_AST.Assert))
		.SetDebugName(nameof(mIL_AST.Assert)) |
		
		(+Ident -Token("=>") +(+Ident -SpecialToken(":") +Ident).OrFail())
		.Modify(X(mIL_AST.Proof))
		.SetDebugName(nameof(mIL_AST.Proof)) |
		
		(+Ident -SpecialToken(":=") +Ident)
		.Modify(X(mIL_AST.Alias))
		.SetDebugName(nameof(mIL_AST.Alias)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") +Ident -Token("&") +(+Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeCond))
		.SetDebugName(nameof(mIL_AST.TypeCond)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") +Ident -Token("=>") +(+Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeFunc))
		.SetDebugName(nameof(mIL_AST.TypeFunc)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") +Ident -SpecialToken(":") +(+Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeMethod))
		.SetDebugName(nameof(mIL_AST.TypeMethod)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") +Ident -SpecialToken(",") +(+Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypePair))
		.SetDebugName(nameof(mIL_AST.TypePair)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") +Prefix +(+Ident-SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypePrefix))
		.SetDebugName(nameof(mIL_AST.TypePrefix)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") +Ident -Token("|") +(+Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeSet))
		.SetDebugName(nameof(mIL_AST.TypeSet)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") -KeyWord("VAR") +(+Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeVar))
		.SetDebugName(nameof(mIL_AST.TypeVar)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") -KeyWord("REC") +(+Ident -Token("=>") +Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeRecursive))
		.SetDebugName(nameof(mIL_AST.TypeRecursive)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") -KeyWord("ANY") +(+Ident -Token("=>") +Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeInterface))
		.SetDebugName(nameof(mIL_AST.TypeInterface)) |
		
		(+Ident -SpecialToken(":=") -SpecialToken("[") -KeyWord("ALL") +(+Ident -Token("=>") +Ident -SpecialToken("]")).OrFail())
		.Modify(X(mIL_AST.TypeGeneric))
		.SetDebugName(nameof(mIL_AST.TypeGeneric)) |
		
		(-KeyWord("TYPE_OF") +(+Ident -Token("IS") +Ident).OrFail())
		.Modify(X(mIL_AST.TypeIs))
		.SetDebugName(nameof(mIL_AST.TypeIs))
	)
	.SetDebugName(nameof(Command));
	
	public static readonly tIL_Parser
	Block = (+Command -NL)[1, null]
	.ModifyList(a => mParserGen.ResultList(a.Span, a.Map(mStd.To<mIL_AST.tCommandNode<tPos>>)))
	.SetDebugName(nameof(Block));
	
	public static readonly tIL_Parser
	Def = (-KeyWord("DEF") +Ident -NL +Block)
	.Modify((mStd.tSpan<tPos> aSpan, mIL_Tokenizer.tToken aID, mList.tList<mIL_AST.tCommandNode<tPos>> aBlock) => (aID.Text, aBlock))
	.SetDebugName(nameof(Def));
	
	public static readonly tIL_Parser
	Module = Def[1, null]
	.ModifyList(a => mParserGen.ResultList(a.Span, a.Map(mStd.To<(tText, mList.tList<mIL_AST.tCommandNode<tPos>>)>)))
	.SetDebugName(nameof(Module));
	
	//================================================================================
	public static mParserGen.tResultList<mTextParser.tPos>
	ParseText(
		this tIL_Parser aParser,
		tText aText,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Tokens = mIL_Tokenizer.Tokenizer.ParseText(aText, aDebugStream).Value.Map(mStd.To<mIL_Tokenizer.tToken>);
		var MaybeResult = aParser.StartParse(Tokens.Map(a => (a.Span, a)), aDebugStream);
		mStd.Assert(
			MaybeResult.Match(out var Result, out var ErrorList),
			#if true
			ErrorList.Reduce(
				mList.List<mTextParser.tError>(),
				(aOldList, aNew) => mList.List(
					aNew,
					aOldList.Where(
						aOld => (
							aOld.Pos.Row > aNew.Pos.Row ||
							(aOld.Pos.Row == aNew.Pos.Row && aOld.Pos.Col >= aNew.Pos.Col)
						)
					)
				)
			).Map(
			#else
			ErrorList.Map(
			#endif
				aError => {
					var Line = aText.Split('\n')[aError.Pos.Row-1];
					var MarkerLine = mTextParser.TextToStream(
						Line
					).Take(
						aError.Pos.Col - 1
					).Map(
						aSymbol => aSymbol.Item2 == '\t' ? '\t' : ' '
					).Reduce(
						"",
						(aString, aChar) => aString + aChar
					);
					return (
						$"({aError.Pos.Row}, {aError.Pos.Col}): {aError.Message}\n" +
						$"{Line}\n" +
						$"{MarkerLine}^\n"
					);
				}
			).Reduce("", (a1, a2) => a1 + "\n" + a2)
		);
		
		var (ResultList, Rest) = Result;
		if (!Rest.IsEmpty()) {
			var Line = Rest.First().Item1.Start.Row;
			throw mStd.Error(
				$"({Line}, 1): expected end of text\n" +
				$"{aText.Split('\n')[Line-1]}\n" +
				$"^"
			);
		}
		return ResultList;
	}
}
