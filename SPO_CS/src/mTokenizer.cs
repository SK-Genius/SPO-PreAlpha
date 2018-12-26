//IMPORT mTextParser.cs

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

using tPos = mTextStream.tPos;
using tSpan = mStd.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class mTokenizer {
	
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tText, tError>, tText> Text = mTextParser.GetToken;
	
	public static readonly mParserGen.tParser<tPos, tChar, tChar, tError> _ = CharIn(" \t\r");
	public static readonly mParserGen.tParser<tPos, tChar, mStream.tStream<tChar>, tError> __ = _[0, null];
	
	public static readonly tText SpazialChars = "#$§€\".:,;()[]{} \t\n\r";
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Digit = CharInRange('0', '9')
	.Modify(aChar => aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Nat = mParserGen.Seq(
		Digit,
		(-Char('_')[0, null] + Digit)[0, null]
	).Modify(
		(aFirst, aRest) => aRest.Reduce(
			aFirst,
			(aNumber, aDigit) => 10 * aNumber + aDigit
		)
	).SetName(nameof(Nat));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	PosSignum = (-Char('+'))
	.Modify(aSpan => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	NegSignum = (-Char('-'))
	.Modify((aSpan) => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Int = mParserGen.Seq(Signum, Nat)
	.Modify((aSig, aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Number = (Int | Nat)
	.SetName(nameof(Number));
	
	public static readonly mParserGen.tParser<tPos, tChar, tText, tError>
	Ident = (CharNotIn(SpazialChars).Modify(aChar => "" + aChar) | Text("..."))[1, null]
	.Modify(aTextList => aTextList.Join((a1, a2) => a1 + a2));
	
	public enum tTokenType {
		Number,
		Text,
		Ident,
		SpecialIdent,
		SpecialToken,
	}
	
	public struct tToken {
		public tTokenType Type;
		public tText Text;
		public tSpan Span;
		
		override public tText ToString() => $"'{this.Text}'::{this.Type}@({this.Span.Start.Row}:{this.Span.Start.Col}..{this.Span.End.Row}:{this.Span.End.Col})";
	}
	
	public static readonly mParserGen.tParser<tPos, tChar, tToken, tError>
	Token = mParserGen.OneOf(
		mParserGen.Seq(Char('"'), CharNotIn("\"")[0, null], Char('"'))
		.ModifyS((aSpan, _, aChars, __) => new tToken { Type = tTokenType.Text, Text = aChars.Reduce("", (aText, aChar) => aText + aChar), Span = aSpan })
		.SetName(nameof(tTokenType.Text)),
		
		Text("=>")
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken)),
		
		Number
		.ModifyS((aSpan, aInt) => new tToken { Type = tTokenType.Number, Text = "" + aInt, Span = aSpan })
		.SetName(nameof(tTokenType.Number)),
		
		Ident
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Ident, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Ident)),
		
		CharIn("#§").__(Ident)
		.ModifyS((aSpan, aChar, aText) => new tToken { Type = tTokenType.SpecialIdent, Text = aChar + aText, Span = aSpan })
		.SetName(nameof(tTokenType.Ident)),
		
		Text("..")
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken)),
		
		CharIn(".,:;()[]{}€\n").Modify(aChar => "" + aChar)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken))
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mStream.tStream<tToken>, tError>
	Tokenizer = (Token +-__)[0, null];
	
	//================================================================================
	public static tOut
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aText,
		tText aIdent,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	//================================================================================
	) {
		var Tokens = Tokenizer.ParseText(aText, aIdent, aDebugStream).Result;
		var MaybeResult = aParser.StartParse(Tokens.Map(a => (a.Span, a)), aDebugStream);
		mStd.Assert(
			MaybeResult.Match(out var Result, out var ErrorList),
			ErrorList.ToText(aText.Split('\n'))
		);
		
		if (!Result.RestStream.IsEmpty()) {
			var Row = Result.RestStream.First().Span.Start.Row;
			var Col = Result.RestStream.First().Span.Start.Col;
			var Line = aText.Split('\n')[Row];
			var MarkerLine = mStream.Stream(
				Line.ToCharArray()
			).Take(
				Col - 1
			).Map(
				aChar => aChar == '\t' ? '\t' : ' '
			).Reduce(
				"",
				(aString, aChar) => aString + aChar
			);
			throw mStd.Error(
				$"({Row}, {Col}): expected end of text\n" +
				$"{Line}\n" +
				$"{MarkerLine}^\n"
			);
		}
		return Result.Result.Value;
	}
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	SpaceToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && (a.Text == " " || a.Text == "\t"),
		a => (a.Span.Start, "expect space"),
		mTextParser.ComparePos
	).SetDebugName(nameof(SpaceToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL_Token = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == "\n",
		a => (a.Span.Start, "expect line break"),
		mTextParser.ComparePos
	).SetDebugName(nameof(NL_Token));
	
	public static readonly mParserGen.tParser<tPos, tToken, mStd.tEmpty, tError>
	NLs_Token = -(NL_Token | SpaceToken)[1, null]
	.SetDebugName(nameof(NLs_Token));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	TextToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Text,
		a => (a.Span.Start, "expect '\"'"),
		mTextParser.ComparePos
	).SetDebugName(nameof(TextToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NumberToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Number,
		a => (a.Span.Start, "expect number"),
		mTextParser.ComparePos
	).SetDebugName(nameof(NumberToken));
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	IdentToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Ident,
		a => (a.Span.Start, "expect identifier"),
		mTextParser.ComparePos
	).SetDebugName(nameof(IdentToken));
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	Token_(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Ident && a.Text == aText,
		a => (a.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(Token)}('{aText}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialToken(
		tText aText
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == aText,
		a => (a.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(SpecialToken)}('{aText}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialIdent(
		tChar aPrefix
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialIdent && a.Text.StartsWith("" + aPrefix),
		a => (a.Span.Start, $"expect '{aPrefix}...'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(SpecialToken)}('{aPrefix}...')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialIdent(
		tChar aPrefix,
		tText aIdent
	//================================================================================
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialIdent && a.Text == "" + aPrefix + aIdent,
		a => (a.Span.Start, $"expect '{aPrefix}{aIdent}'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(SpecialToken)}('{aPrefix}{aIdent}')");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	KeyWord(
		tText aIdent
	//================================================================================
	) => SpecialIdent('§', aIdent);
	
	//================================================================================
	public static mStd.tFunc<tRes, tSpan>
	X<tRes>(
		mStd.tFunc<tRes, tSpan>aFunc
	//================================================================================
	) => aSpan => aFunc(aSpan);
	
	//================================================================================
	public static mStd.tFunc<tRes, tSpan, tToken>
	X<tRes>(
		mStd.tFunc<tRes, tSpan, tText> aFunc
	//================================================================================
	) => (aSpan, a1) => aFunc(aSpan, a1.Text);
	
	//================================================================================
	public static mStd.tFunc<tRes, tSpan, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, tSpan, tText, tText> aFunc
	//================================================================================
	) => (aSpan, a1, a2) => aFunc(aSpan, a1.Text, a2.Text);
	
	//================================================================================
	public static mStd.tFunc<tRes, tSpan, tToken, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, tSpan, tText, tText, tText> aFunc
	//================================================================================
	) => (aSpan, a1, a2, a3) => aFunc(aSpan, a1.Text, a2.Text, a3.Text);
}
