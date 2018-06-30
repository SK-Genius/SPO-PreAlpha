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

using tToken = mIL_Tokenizer.tToken;
using tTokenType = mIL_Tokenizer.tTokenType;

using tPos = mTextParser.tPos;
using tSpan =mStd.tSpan<mTextParser.tPos>;
using tError = mTextParser.tError;

public static class mIL_Parser {
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == "\n",
		a => new tError {
			Message = "expect end of line",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(NL));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	QuotedString  = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Text,
		a => new tError {
			Message = "expect '\"'",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(QuotedString));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Number = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Number,
		a => new tError {
			Message = "expect number",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(Number));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Ident = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Ident,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(Ident));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	Prefix = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Prefix,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName(nameof(Prefix));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	Token(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Ident && a.Text == aText,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName($"{nameof(Token)}('{aText}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialToken(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == aText,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName($"{nameof(SpecialToken)}('{aText}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	KeyWord(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.KeyWord && a.Text == aText,
		a => new tError {
			Message = "expect identifier",
			Pos = a.Span.Start
		}
	).SetDebugName($"{nameof(KeyWord)}('{aText}')");
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>> aFunc
	//================================================================================
	) => aSpan => aFunc(aSpan);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText> aFunc
	//================================================================================
	) => (aSpan, a1) => aFunc(aSpan, a1.Text);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText, tText> aFunc
	//================================================================================
	) => (aSpan, a1, a2) => aFunc(aSpan, a1.Text, a2.Text);
	
	//================================================================================
	private static mStd.tFunc<tRes, mStd.tSpan<tPos>, tToken, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, mStd.tSpan<tPos>, tText, tText, tText> aFunc
	//================================================================================
	) => (aSpan, a1, a2, a3) => aFunc(aSpan, a1.Text, a2.Text, a3.Text);
	
	public static readonly mParserGen.tParser<tPos, tToken, mIL_AST.tCommandNode<tSpan>, tError>
	Command = (
		(Ident)._( -SpecialToken(":="))._(Number)
		.ModifyS(X(mIL_AST.CreateInt))
		.SetDebugName(nameof(mIL_AST.CreateInt)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("BOOL"))._(Ident)._(-Token("&"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.And))
		.SetDebugName(nameof(mIL_AST.And)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("BOOL"))._(Ident)._(-Token("|"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.Or))
		.SetDebugName(nameof(mIL_AST.Or)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("BOOL"))._(Ident)._(-Token("^"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.XOr))
		.SetDebugName(nameof(mIL_AST.XOr)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("INT"))._(Ident)._(-Token("=="))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.IntsAreEq))
		.SetDebugName(nameof(mIL_AST.IntsAreEq)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("INT"))._(Ident)._(-Token("<=>"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.IntsComp))
		.SetDebugName(nameof(mIL_AST.IntsComp)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("INT"))._(Ident)._(-Token("+"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.IntsAdd))
		.SetDebugName(nameof(mIL_AST.IntsAdd)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("INT"))._(Ident)._(-Token("-"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.IntsSub))
		.SetDebugName(nameof(mIL_AST.IntsSub)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("INT"))._(Ident)._(-Token("*"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.IntsMul))
		.SetDebugName(nameof(mIL_AST.IntsMul)) |
		
		(Ident)._(-SpecialToken(":="))._(Ident)._(-SpecialToken(","))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.CreatePair))
		.SetDebugName(nameof(mIL_AST.CreatePair)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("1ST"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.GetFirst))
		.SetDebugName(nameof(mIL_AST.GetFirst)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("2ND"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.GetSecond))
		.SetDebugName(nameof(mIL_AST.GetSecond)) |
		
		(Ident)._(-SpecialToken(":="))._(-Token("+"))._((Prefix)._(Ident).OrFail())
		.ModifyS(X(mIL_AST.AddPrefix))
		.SetDebugName(nameof(mIL_AST.AddPrefix)) |
		
		(Ident)._(-SpecialToken(":="))._(-Token("-"))._((Prefix)._(Ident).OrFail())
		.ModifyS(X(mIL_AST.SubPrefix))
		.SetDebugName(nameof(mIL_AST.SubPrefix)) |
		
		(Ident)._(-SpecialToken(":="))._(-Token("?"))._((Prefix)._(Ident).OrFail())
		.ModifyS(X(mIL_AST.HasPrefix))
		.SetDebugName(nameof(mIL_AST.HasPrefix)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("."))._(Ident)._(Ident)
		.ModifyS(X(mIL_AST.Call))
		.SetDebugName(nameof(mIL_AST.Call)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("OBJ"))._(-SpecialToken(":"))._((Ident)._(Ident).OrFail())
		.ModifyS(X(mIL_AST.Exec))
		.SetDebugName(nameof(mIL_AST.Exec)) |
		
		(-KeyWord("PUSH"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.Push))
		.SetDebugName(nameof(mIL_AST.Push)) |
		
		(-KeyWord("POP"))
		.ModifyS(X(mIL_AST.Pop))
		.SetDebugName(nameof(mIL_AST.Pop)) |
		
		(-KeyWord("VAR"))._((Ident)._(-Token("<-"))._(Ident).OrFail())
		.ModifyS(X(mIL_AST.VarSet))
		.SetDebugName(nameof(mIL_AST.VarSet)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("VAR"))._(Ident)._(-Token("->"))
		.ModifyS(X(mIL_AST.VarGet))
		.SetDebugName(nameof(mIL_AST.VarGet)) |
		
		(Ident)._(-SpecialToken(":="))._(-KeyWord("VAR"))._(Ident.OrFail())
		.ModifyS(X(mIL_AST.VarDef))
		.SetDebugName(nameof(mIL_AST.VarDef)) |
		
		(-KeyWord("RETURN"))._((Ident)._(-Token("IF"))._(Ident).OrFail())
		.ModifyS(X(mIL_AST.ReturnIf))
		.SetDebugName(nameof(mIL_AST.ReturnIf)) |
		
		(-KeyWord("REPEAT"))._((Ident)._(-Token("IF"))._(Ident).OrFail())
		.ModifyS(X(mIL_AST.RepeatIf))
		.SetDebugName(nameof(mIL_AST.RepeatIf)) |
		
		(-KeyWord("ASSERT"))._((Ident)._(-Token("=>"))._(Ident).OrFail())
		.ModifyS(X(mIL_AST.Assert))
		.SetDebugName(nameof(mIL_AST.Assert)) |
		
		(Ident)._(-Token("=>"))._((Ident)._(-SpecialToken(":"))._(Ident).OrFail())
		.ModifyS(X(mIL_AST.Proof))
		.SetDebugName(nameof(mIL_AST.Proof)) |
		
		(Ident)._(-SpecialToken(":="))._(Ident)
		.ModifyS(X(mIL_AST.Alias))
		.SetDebugName(nameof(mIL_AST.Alias)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(Ident)._(-Token("&"))._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeCond))
		.SetDebugName(nameof(mIL_AST.TypeCond)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(Ident)._(-Token("=>"))._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeFunc))
		.SetDebugName(nameof(mIL_AST.TypeFunc)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(Ident)._(-SpecialToken(":"))._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeMethod))
		.SetDebugName(nameof(mIL_AST.TypeMethod)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(Ident)._(-SpecialToken(","))._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypePair))
		.SetDebugName(nameof(mIL_AST.TypePair)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(Prefix)._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypePrefix))
		.SetDebugName(nameof(mIL_AST.TypePrefix)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(Ident)._(-Token("|"))._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeSet))
		.SetDebugName(nameof(mIL_AST.TypeSet)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(-KeyWord("VAR"))._((Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeVar))
		.SetDebugName(nameof(mIL_AST.TypeVar)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(-KeyWord("REC"))._((Ident)._(-Token("=>"))._(Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeRecursive))
		.SetDebugName(nameof(mIL_AST.TypeRecursive)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(-KeyWord("ANY"))._((Ident)._(-Token("=>"))._(Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeInterface))
		.SetDebugName(nameof(mIL_AST.TypeInterface)) |
		
		(Ident)._(-SpecialToken(":="))._(-SpecialToken("["))._(-KeyWord("ALL"))._((Ident)._(-Token("=>"))._(Ident)._(-SpecialToken("]")).OrFail())
		.ModifyS(X(mIL_AST.TypeGeneric))
		.SetDebugName(nameof(mIL_AST.TypeGeneric)) |
		
		(-KeyWord("TYPE_OF"))._((Ident)._(-Token("IS"))._(Ident).OrFail())
		.ModifyS(X(mIL_AST.TypeIs))
		.SetDebugName(nameof(mIL_AST.TypeIs))
	)
	.SetDebugName(nameof(Command));
	
	public static readonly mParserGen.tParser<tPos, tToken, mList.tList<mIL_AST.tCommandNode<tSpan>>, tError>
	Block = (Command)._(-NL)[1, null]
	.SetDebugName(nameof(Block));
	
	public static readonly mParserGen.tParser<tPos, tToken, (tText, mList.tList<mIL_AST.tCommandNode<tSpan>>), tError>
	Def = (-KeyWord("DEF"))._(Ident)._(-NL)._(Block)
	.ModifyS((aSpan, aID, aBlock) => (aID.Text, aBlock))
	.SetDebugName(nameof(Def));
	
	public static readonly mParserGen.tParser<tPos, tToken, mList.tList<(tText, mList.tList<mIL_AST.tCommandNode<tSpan>>)>, tError>
	Module = Def[1, null]
	.SetDebugName(nameof(Module));
	
	//================================================================================
	public static tOut
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aText,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Tokens = mIL_Tokenizer.Tokenizer.ParseText(aText, aDebugStream).Result;
		var MaybeResult = aParser.StartParse(Tokens.Map(a => (a.Span, a)), aDebugStream);
		mStd.Assert(
			MaybeResult.Match(out var Result, out var ErrorList),
			#if true
			ErrorList.Reduce(
				mList.List<tError>(),
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
						a => a.Char == '\t' ? '\t' : ' '
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
		
		if (!Result.RestStream.IsEmpty()) {
			var Line = Result.RestStream.First().Span.Start.Row;
			throw mStd.Error(
				$"({Line}, 1): expected end of text\n" +
				$"{aText.Split('\n')[Line-1]}\n" +
				$"^"
			);
		}
		return Result.Result.Value;
	}
}
