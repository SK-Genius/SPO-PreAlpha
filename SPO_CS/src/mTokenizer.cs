//IMPORT mTextParser.cs
//IMPORT mError.cs
//IMPORT mAssert.cs

#nullable enable

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class
mTokenizer {
	
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tText, tError>, tText> Text = mTextParser.GetToken;
	
	public static readonly mParserGen.tParser<tPos, tChar, tChar, tError> _ = CharIn(" \t\r");
	public static readonly mParserGen.tParser<tPos, tChar, mStream.tStream<tChar>?, tError> __ = _[0..];
	
	public static readonly tText SpacialChars = "#$§€\".:,;()[]{} \t\n\r";
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Digit = CharInRange('0', '9')
	.Modify(aChar => aChar - '0')
	.SetName(nameof(Digit));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	Nat = mParserGen.Seq(
		Digit,
		(-Char('_')[0..] + Digit)[0..]
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
	Id = (CharNotIn(SpacialChars).Modify(aChar => "" + aChar) | Text("..."))[1..]
	.Modify(aTextList => aTextList.Join((a1, a2) => a1 + a2, ""));
	
	public enum
	tTokenType {
		Number,
		Text,
		Id,
		SpecialId,
		SpecialToken,
	}
	
	public struct
	tToken {
		public tTokenType Type;
		public tText Text;
		public tSpan Span;
		
		public override tText
		ToString(
		) => $"'{this.Text}'::{this.Type}@({this.Span.Start.Row}:{this.Span.Start.Col}..{this.Span.End.Row}:{this.Span.End.Col})";
	}
	
	public static readonly mParserGen.tParser<tPos, tChar, tToken, tError>
	Token = mParserGen.OneOf(
		mParserGen.Seq(Char('"'), CharNotIn("\"")[0..], Char('"'))
		.ModifyS((aSpan, _, aChars, __) => new tToken { Type = tTokenType.Text, Text = aChars.Reduce("", (aText, aChar) => aText + aChar), Span = aSpan })
		.SetName(nameof(tTokenType.Text)),
		
		Text("=>")
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken)),
		
		Number
		.ModifyS((aSpan, aInt) => new tToken { Type = tTokenType.Number, Text = "" + aInt, Span = aSpan })
		.SetName(nameof(tTokenType.Number)),
		
		Id
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Id, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Id)),
		
		CharIn("#§").__(Id)
		.ModifyS((aSpan, aChar, aText) => new tToken { Type = tTokenType.SpecialId, Text = aChar + aText, Span = aSpan })
		.SetName(nameof(tTokenType.Id)),
		
		Text("..")
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken)),
		
		CharIn(".,:;()[]{}€\n").Modify(aChar => "" + aChar)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken))
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mStream.tStream<tToken>?, tError>
	Tokenizer = (Token +-__)[0..];
	
	public static tOut
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aText,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var Tokens = Tokenizer.ParseText(aText, aId, aDebugStream).Result;
		var MaybeResult = aParser.StartParse(Tokens.Map(a => (a.Span, a)), aDebugStream);
		var Result = MaybeResult.ElseThrow(a => a.ToText(aText.Split('\n')));
		
		if (!Result.RemainingStream.IsEmpty()) {
			var Row = Result.RemainingStream.TryFirst().Then(a => a.Span.Start.Row).ElseThrow();
			var Col = Result.RemainingStream.TryFirst().Then(a => a.Span.Start.Col).ElseThrow();
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
			throw mError.Error(
				$"""
				({Row}, {Col}): expected end of text
				{Line}
				{MarkerLine}^
				
				"""
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
	NLs_Token = -(NL_Token | SpaceToken)[1..]
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
	IdToken = mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Id,
		a => (a.Span.Start, "expect Idifier"),
		mTextParser.ComparePos
	).SetDebugName(nameof(IdToken));
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	Token_(
		tText aText
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.Id && a.Text == aText,
		a => (a.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(Token)}('{aText}')");
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialToken(
		tText aText
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialToken && a.Text == aText,
		a => (a.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(SpecialToken)}('{aText}')");
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialId(
		tChar aPrefix
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialId && a.Text.StartsWith("" + aPrefix),
		a => (a.Span.Start, $"expect '{aPrefix}...'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(SpecialToken)}('{aPrefix}...')");
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialId(
		tChar aPrefix,
		tText aId
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		a => a.Type == tTokenType.SpecialId && a.Text == "" + aPrefix + aId,
		a => (a.Span.Start, $"expect '{aPrefix}{aId}'"),
		mTextParser.ComparePos
	).SetDebugName($"{nameof(SpecialToken)}('{aPrefix}{aId}')");
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	KeyWord(
		tText aId
	) => SpecialId('§', aId);
	
	public static mStd.tFunc<tRes, tSpan>
	X<tRes>(
		mStd.tFunc<tRes, tSpan>aFunc
	) => aSpan => aFunc(aSpan);
	
	public static mStd.tFunc<tRes, tSpan, tToken>
	X<tRes>(
		mStd.tFunc<tRes, tSpan, tText> aFunc
	) => (aSpan, a1) => aFunc(aSpan, a1.Text);
	
	public static mStd.tFunc<tRes, tSpan, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, tSpan, tText, tText> aFunc
	) => (aSpan, a1, a2) => aFunc(aSpan, a1.Text, a2.Text);
	
	public static mStd.tFunc<tRes, tSpan, tToken, tToken, tToken>
	X<tRes>(
		mStd.tFunc<tRes, tSpan, tText, tText, tText> aFunc
	) => (aSpan, a1, a2, a3) => aFunc(aSpan, a1.Text, a2.Text, a3.Text);
}
