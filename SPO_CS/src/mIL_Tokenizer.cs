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

public static class  mIL_Tokenizer {
	public static readonly mStd.tFunc<mParserGen.tParser<mTextParser.tPos, tChar, tChar, mTextParser.tError>, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextParser.tPos, tChar, tChar, mTextParser.tError>, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextParser.tPos, tChar, tChar, mTextParser.tError>, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextParser.tPos, tChar, tChar, mTextParser.tError>, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextParser.tPos, tChar, tChar, mTextParser.tError>, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextParser.tPos, tChar, tText, mTextParser.tError>, tText> Text = mTextParser.GetToken;
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tChar, mTextParser.tError> _ = CharIn(" \t\r");
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, mList.tList<tChar>, mTextParser.tError> __ = _[0, null];
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	Digit = CharInRange('0', '9')
	.ModifyS((aSpan, aChar) => aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	Nat = Digit._(
		(-(Char('_')[0, null]))._(Digit)[0, null]
	).ModifyS(
		(aSpan, aFirst, aRest) => aRest.Reduce(
			aFirst,
			(aNumber, aDigit) => 10 * aNumber + aDigit
		)
	).SetName(nameof(Nat));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	PosSignum = (-Char('+'))
	.Modify(aSpan => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	NegSignum = (-Char('-'))
	.Modify((aSpan) => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	Int = (Signum)._(Nat)
	.ModifyS((aSpan, aSig, aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tInt32, mTextParser.tError>
	Number = (Int | Nat)
	.SetName(nameof(Number));
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tText, mTextParser.tError>
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
		public mStd.tSpan<mTextParser.tPos> Span;
		
		override public tText ToString() => $"'{Text}'::{Type}@({Span.Start.Row}:{Span.Start.Col}..{Span.End.Row}:{Span.End.Col})";
	}
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, tToken, mTextParser.tError>
	Token = (
		(-Char('"'))._(CharNotIn("\"")[0, null])._(-Char('"'))
		.ModifyS((aSpan, aChars) => new tToken { Type = tTokenType.Text, Text = aChars.Reduce("", (aText, aChar) => aText + aChar), Span = aSpan })
		.SetName(nameof(tTokenType.Text)) |
		
		Number
		.ModifyS((aSpan, aInt) => new tToken { Type = tTokenType.Number, Text = aInt.ToString(), Span = aSpan })
		.SetName(nameof(tTokenType.Number)) |
		
		Ident
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Ident, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Ident)) |
		
		(-Char('§'))._(Ident)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.KeyWord, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.KeyWord)) |
		
		(-Char('#'))._(Ident)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Prefix, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Prefix)) |
		
		(
			Text(":=") |
			CharIn(".,:;()[]{}€\n").ModifyS((aSpan, aChar) => aChar.ToString())
		)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
	);
	
	public static readonly mParserGen.tParser<mTextParser.tPos, tChar, mList.tList<tToken>, mTextParser.tError>
	Tokenizer = Token._(-__)[0, null];
}
