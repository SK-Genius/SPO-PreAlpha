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

public static class  mIL_Tokenizer {
	
	public static readonly mStd.tFunc<mParserGen.tParser<mTextStream.tPos, tChar, tChar, mTextStream.tError>, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextStream.tPos, tChar, tChar, mTextStream.tError>, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextStream.tPos, tChar, tChar, mTextStream.tError>, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextStream.tPos, tChar, tChar, mTextStream.tError>, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextStream.tPos, tChar, tChar, mTextStream.tError>, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<mParserGen.tParser<mTextStream.tPos, tChar, tText, mTextStream.tError>, tText> Text = mTextParser.GetToken;
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tChar, mTextStream.tError> _ = CharIn(" \t\r");
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, mList.tList<tChar>, mTextStream.tError> __ = _[0, null];
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	Digit = CharInRange('0', '9')
	.Modify(aChar => aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	Nat = mParserGen.Seq(
		Digit,
		(-Char('_')[0, null] + Digit)[0, null]
	).Modify(
		(aFirst, aRest) => aRest.Reduce(
			aFirst,
			(aNumber, aDigit) => 10 * aNumber + aDigit
		)
	).SetName(nameof(Nat));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	PosSignum = (-Char('+'))
	.Modify(aSpan => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	NegSignum = (-Char('-'))
	.Modify((aSpan) => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	Int = mParserGen.Seq(Signum, Nat)
	.Modify((aSig, aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tInt32, mTextStream.tError>
	Number = (Int | Nat)
	.SetName(nameof(Number));
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tText, mTextStream.tError>
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
		public mStd.tSpan<mTextStream.tPos> Span;
		
		override public tText ToString() => $"'{this.Text}'::{this.Type}@({this.Span.Start.Row}:{this.Span.Start.Col}..{this.Span.End.Row}:{this.Span.End.Col})";
	}
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, tToken, mTextStream.tError>
	Token = (
		mParserGen.Seq(Char('"'), CharNotIn("\"")[0, null], Char('"'))
		.ModifyS((aSpan, _, aChars, __) => new tToken { Type = tTokenType.Text, Text = aChars.Reduce("", (aText, aChar) => aText + aChar), Span = aSpan })
		.SetName(nameof(tTokenType.Text)) |
		
		Number
		.ModifyS((aSpan, aInt) => new tToken { Type = tTokenType.Number, Text = "" + aInt, Span = aSpan })
		.SetName(nameof(tTokenType.Number)) |
		
		Ident
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Ident, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Ident)) |
		
		(-Char('§') +Ident)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.KeyWord, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.KeyWord)) |
		
		(-Char('#') +Ident)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.Prefix, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Prefix)) |
		
		(
			Text(":=") |
			CharIn(".,:;()[]{}€\n").Modify(aChar => "" + aChar)
		)
		.ModifyS((aSpan, aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
	);
	
	public static readonly mParserGen.tParser<mTextStream.tPos, tChar, mList.tList<tToken>, mTextStream.tError>
	Tokenizer = (Token +-__)[0, null];
}
