#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mStream.cs
#:ref Common/mTextStream.cs
#:ref Common/mTextParser.cs
#:ref Common/mMaybe.cs
#:ref Common/mSpan.cs
#:ref Common/mResult.cs
#:ref Common/mParserGen.cs
#:ref Common/mError.cs

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class
mTokenizer {
	public static readonly mStd.tFunc<tChar, mParserGen.tParser<tPos, tChar, tChar, tError>> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<tChar, mParserGen.tParser<tPos, tChar, tChar, tError>> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<tText, mParserGen.tParser<tPos, tChar, tChar, tError>> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<tText, mParserGen.tParser<tPos, tChar, tChar, tError>> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<tChar, tChar, mParserGen.tParser<tPos, tChar, tChar, tError>> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<tText, mParserGen.tParser<tPos, tChar, tText, tError>> Text = mTextParser.GetToken;
	
	public static readonly mParserGen.tParser<tPos, tChar, tChar, tError> _ = CharIn(" \t\r");
	public static readonly mParserGen.tParser<tPos, tChar, mStream.tStream<tChar>, tError> __ = _[0..];
	
	public static readonly tText PrefixChars = "#§";
	public static readonly tText ReservedChars = "€\".:,;|()[]{} \t\n\r";
	public static readonly tText SpecialChars = PrefixChars + ReservedChars;
	
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
	.Modify(_ => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly mParserGen.tParser<tPos, tChar, tInt32, tError>
	NegSignum = (-Char('-'))
	.Modify(_ => -1)
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
	Id = (CharNotIn(SpecialChars).Modify(aChar => "" + aChar) | Text("..."))[1..]
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
		
		public override readonly tText
		ToString(
		) => $"'{this.Text}'::{this.Type}@({mTextParser.ToText(this.Span)})";
	}
	
	public static readonly mParserGen.tParser<tPos, tChar, tToken, tError>
	Token = mParserGen.OneOf(
		[
			(
				(
					mParserGen.Seq(
						-Char('"') -Char('\r')[0..1] -Char('\n'),
						(
							mParserGen.Seq(
								-__ -Char('|'),
								CharNotIn("\r\n")[0..].Modify(
									aChars => aChars.Reduce("", (aLine, aChar) => aLine + aChar)
								),
								-Char('\r')[0..1] -Char('\n')
							).Modify((_, aLine, __) => aLine)
						)[0..].Modify(
							aLines => aLines.Join((aLines, aLine) => aLines + '\n' + aLine, "")
						),
						-__ -Char('"')
					)
					.ModifyS((aSpan, _, aLines, _) => new tToken { Type = tTokenType.Text, Text = aLines, Span = aSpan })
				) | (
					mParserGen.Seq(
						Char('"'),
						CharNotIn("\"")[0..],
						Char('"')
					).ModifyS(
						(aSpan, _, aChars, __) => new tToken {
							Type = tTokenType.Text,
							Text = aChars.Reduce("", (aText, aChar) => aText + aChar),
							Span = aSpan
						}
					)
				)
			).SetName(nameof(tTokenType.Text)),
			
			Text("=>")
			.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
			.SetName(nameof(tTokenType.SpecialToken)),
			
			Number
			.ModifyS((aSpan, aInt) => new tToken { Type = tTokenType.Number, Text = "" + aInt, Span = aSpan })
			.SetName(nameof(tTokenType.Number)),
			
			Id
			.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Id, Text = aText, Span = aSpan })
			.SetName(nameof(tTokenType.Id)),
			
			CharIn(PrefixChars).__(Id)
			.ModifyS((aSpan, aChar, aText) => new tToken { Type = tTokenType.SpecialId, Text = aChar + aText, Span = aSpan })
			.SetName(nameof(tTokenType.SpecialId)),
			
			Text("..")
			.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
			.SetName(nameof(tTokenType.SpecialToken)),
			
			CharIn(ReservedChars).Modify(aChar => "" + aChar)
			.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
			.SetName(nameof(tTokenType.SpecialToken))
		]
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mStream.tStream<tToken>, tError>
	Tokenizer = (Token +-__)[0..];
	
	public static tOut
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aText,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var Tokens = Tokenizer.ParseText(aText, aId, aDebugStream).Result;
		var MaybeResult = aParser.StartParse(Tokens.Map(__ => (__.Span, __)), aDebugStream);
		var Lines = aText.Split("\n");
		var Result = MaybeResult.AssertNotError(
			__ => __.ToText(Lines)
		);
		
		if (!Result.RemainingStream.IsEmpty()) {
			var Row = Result.RemainingStream.TryFirst().AssertNotEmpty().Span.Start.Row;
			var Col = Result.RemainingStream.TryFirst().AssertNotEmpty().Span.Start.Col;
			var PrevLine = Row > 2 ? Lines[Row - 2] : "";
			var Line = Lines[Row - 1];
			var MarkerLine = mStream.Stream(
				System.MemoryExtensions.AsSpan(Line)
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
				{aId}:{Row} expected end of text
				{PrevLine}
				{Line}
				{MarkerLine}^
				
				"""
			);
		}
		return Result.Result.Value;
	}
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	SpaceToken = mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.SpecialToken && (__.Text == " " || __.Text == "\t"),
		__ => (__.Span.Start, "expect space"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(SpaceToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL_Token = mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.SpecialToken && __.Text == "\n",
		__ => (__.Span.Start, "expect line break"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(NL_Token)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, mStd.tEmpty, tError>
	NLs_Token = -(NL_Token | SpaceToken)[1..]
	.SetDebugName([nameof(NLs_Token)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	CharToken = mParserGen.AtomParser<tPos, tToken, tError>(
		__ => (
			__.Type is tTokenType.SpecialId &&
			__.Text.Length is 2 &&
			__.Text[0] is '§' &&
			(
				(__.Text[1] is >= 'A' and <= 'Z') ||
				(__.Text[1] is >= 'a' and <= 'z') ||
				(__.Text[1] is >= '0' and <= '9')
			)
		),
		__ => (__.Span.Start, "expect char literal"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(CharToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	TextToken = mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.Text,
		__ => (__.Span.Start, "expect '\"'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(TextToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NumberToken = mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.Number,
		__ => (__.Span.Start, "expect number"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(NumberToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	IdToken = mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.Id,
		__ => (__.Span.Start, "expect Identifier"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(IdToken)]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	Token_(
		tText aText
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.Id && __.Text == aText,
		__ => (__.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(Token)}('{aText}')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialToken(
		tText aText
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.SpecialToken && __.Text == aText,
		__ => (__.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(SpecialToken)}('{aText}')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialId(
		tChar aPrefix
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.SpecialId && __.Text.StartsWith("" + aPrefix),
		__ => (__.Span.Start, $"expect '{aPrefix}...'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(SpecialId)}('{aPrefix}...')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialId(
		tChar aPrefix,
		tText aId
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		__ => __.Type is tTokenType.SpecialId && __.Text == "" + aPrefix + aId,
		__ => (__.Span.Start, $"expect '{aPrefix}{aId}'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(SpecialId)}('{aPrefix}{aId}')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	KeyWord(
		tText aId
	) => SpecialId('§', aId);
	
	public static mStd.tFunc<tSpan, tRes>
	X<tRes>(
		mStd.tFunc<tSpan, tRes> aFunc
	) => aSpan => aFunc(aSpan);
	
	public static mStd.tFunc<tSpan, tToken, tRes>
	X<tRes>(
		mStd.tFunc<tSpan, tText, tRes> aFunc
	) => (aSpan, a1) => aFunc(aSpan, a1.Text);
	
	public static mStd.tFunc<tSpan, tToken, tToken, tRes>
	X<tRes>(
		mStd.tFunc<tSpan, tText, tText, tRes> aFunc
	) => (aSpan, a1, a2) => aFunc(aSpan, a1.Text, a2.Text);
	
	public static mStd.tFunc<tSpan, tToken, tToken, tToken, tRes>
	X<tRes>(
		mStd.tFunc<tSpan, tText, tText, tText, tRes> aFunc
	) => (aSpan, a1, a2, a3) => aFunc(aSpan, a1.Text, a2.Text, a3.Text);
}
