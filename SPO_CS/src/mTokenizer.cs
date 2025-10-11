// IMPORT Common/mStd
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT Common/mTextParser
// IMPORT Common/mMaybe
// IMPORT Common/mSpan
// IMPORT Common/mResult
// IMPORT Common/mParserGen
// IMPORT Common/mError
// IMPORT Common/mParserGen

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
	
	public static readonly tText SpecialChars = "#$§€\".:,;()[]{} \t\n\r";
	
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
						-Char('"') -Char('\n'),
						(-CharIn(" \t\r") -Char('|') +(~Char('\n')).Modify(
							_ => _.Item1.Reduce("", (aLine, aChar) => aLine + aChar)
						))[0..].Modify(
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
						(aSpan, _, aChars, __) => new tToken { Type = tTokenType.Text, Text = aChars.Reduce("", (aText, aChar) => aText + aChar), Span = aSpan }
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
			
			CharIn("#§").__(Id)
			.ModifyS((aSpan, aChar, aText) => new tToken { Type = tTokenType.SpecialId, Text = aChar + aText, Span = aSpan })
			.SetName(nameof(tTokenType.SpecialId)),
			
			Text("..")
			.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
			.SetName(nameof(tTokenType.SpecialToken)),
			
			CharIn(".,:;()[]{}?€\n").Modify(aChar => "" + aChar)
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
		var MaybeResult = aParser.StartParse(Tokens.Map(_ => (_.Span, _)), aDebugStream);
		var Lines = aText.Split("\n");
		var Result = MaybeResult.ElseThrow(
			_ => _.ToText(Lines)
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
		_ => _.Type is tTokenType.SpecialToken && (_.Text == " " || _.Text == "\t"),
		_ => (_.Span.Start, "expect space"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(SpaceToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NL_Token = mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.SpecialToken && _.Text == "\n",
		_ => (_.Span.Start, "expect line break"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(NL_Token)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, mStd.tEmpty, tError>
	NLs_Token = -(NL_Token | SpaceToken)[1..]
	.SetDebugName([nameof(NLs_Token)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	CharToken = mParserGen.AtomParser<tPos, tToken, tError>(
		_ => (
			_.Type is tTokenType.SpecialId &&
			_.Text.Length is 2 &&
			_.Text[0] is '§' &&
			(
				(_.Text[1] is >= 'A' and <= 'Z') ||
				(_.Text[1] is >= 'a' and <= 'z') ||
				(_.Text[1] is >= '0' and <= '9')
			)
		),
		_ => (_.Span.Start, "expect char literal"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(CharToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	TextToken = mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.Text,
		_ => (_.Span.Start, "expect '\"'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(TextToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	NumberToken = mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.Number,
		_ => (_.Span.Start, "expect number"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(NumberToken)]);
	
	public static readonly mParserGen.tParser<tPos, tToken, tToken, tError>
	IdToken = mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.Id,
		_ => (_.Span.Start, "expect Identifier"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([nameof(IdToken)]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	Token_(
		tText aText
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.Id && _.Text == aText,
		_ => (_.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(Token)}('{aText}')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialToken(
		tText aText
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.SpecialToken && _.Text == aText,
		_ => (_.Span.Start, $"expect '{aText}'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(SpecialToken)}('{aText}')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialId(
		tChar aPrefix
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.SpecialId && _.Text.StartsWith("" + aPrefix),
		_ => (_.Span.Start, $"expect '{aPrefix}...'"),
		mTextParser.ComparePos,
		mTextParser.AreErrorsEqual
	).SetDebugName([$"{nameof(SpecialId)}('{aPrefix}...')"]);
	
	public static mParserGen.tParser<tPos, tToken, tToken, tError>
	SpecialId(
		tChar aPrefix,
		tText aId
	) => mParserGen.AtomParser<tPos, tToken, tError>(
		_ => _.Type is tTokenType.SpecialId && _.Text == "" + aPrefix + aId,
		_ => (_.Span.Start, $"expect '{aPrefix}{aId}'"),
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
