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
using tError = mTextStream.tError;

public static class mTokenizer {
	
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tChar, tError>, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<mParserGen.tParser<tPos, tChar, tText, tError>, tText> Text = mTextParser.GetToken;
	
	public static readonly mParserGen.tParser<tPos, tChar, tChar, tError> _ = CharIn(" \t\r");
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<tChar>, tError> __ = _[0, null];
	
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
		KeyWord,
		Prefix,
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
		
		Text("..")
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken)),
		
		(-Char('§') +Ident)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.KeyWord, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.KeyWord)),
		
		CharIn(".,:;()[]{}€#\n").Modify(aChar => "" + aChar)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.SpecialToken))
	);
	
	public static readonly mParserGen.tParser<tPos, tChar, mList.tList<tToken>, tError>
	Tokenizer = (Token +-__)[0, null];
	
	//================================================================================
	public static tOut
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tToken, tOut, tError> aParser,
		tText aText,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Tokens = Tokenizer.ParseText(aText, aDebugStream).Result;
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
					var MarkerLine = mTextStream.TextToStream(
						Line
					).Map(
						_ => (mStd.Span(_.Pos), _.Char)
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
