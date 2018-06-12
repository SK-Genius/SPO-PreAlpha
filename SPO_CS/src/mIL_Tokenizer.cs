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

using tIL_Tokenizer = mParserGen.tParser<mTextParser.tPos, System.Char, mTextParser.tError>;

public static class  mIL_Tokenizer {
	public static readonly mStd.tFunc<tIL_Tokenizer, tChar> Char = mTextParser.GetChar;
	public static readonly mStd.tFunc<tIL_Tokenizer, tChar> NotChar = mTextParser.GetNotChar;
	public static readonly mStd.tFunc<tIL_Tokenizer, tText> CharIn = mTextParser.GetCharIn;
	public static readonly mStd.tFunc<tIL_Tokenizer, tText> CharNotIn = mTextParser.GetCharNotIn;
	public static readonly mStd.tFunc<tIL_Tokenizer, tChar, tChar> CharInRange = mTextParser.GetCharInRange;
	public static readonly mStd.tFunc<tIL_Tokenizer, tText> Text = mTextParser.GetToken;
	
	public static readonly tIL_Tokenizer _ = CharIn(" \t\r");
	public static readonly tIL_Tokenizer __ = _[0, null];
	
	public static readonly tText SpazialChars = "#$§\".:,;()[]{} \t\n\r";
	
	public static readonly tIL_Tokenizer
	Digit = CharInRange('0', '9')
	.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tChar aChar) => aChar - (int)'0')
	.SetName(nameof(Digit));
	
	public static readonly tIL_Tokenizer
	Nat = (Digit + (Digit | -Char('_'))[0, null])
	.ModifyList(a => a.Reduce((tInt32 aNumber, tInt32 aDigit) => 10*aNumber+aDigit))
	.SetName(nameof(Nat));
	
	public static readonly tIL_Tokenizer
	PosSignum = (-Char('+'))
	.Modify((mStd.tSpan<mTextParser.tPos> aSpan) => +1)
	.SetName(nameof(PosSignum));
	
	public static readonly tIL_Tokenizer
	NegSignum = (-Char('-'))
	.Modify((mStd.tSpan<mTextParser.tPos> aSpan) => -1)
	.SetName(nameof(NegSignum));
	
	public static readonly tIL_Tokenizer
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly tIL_Tokenizer
	Int = (Signum + Nat)
	.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tInt32 aSig, tInt32 aAbs) => aSig * aAbs)
	.SetName(nameof(Int));
	
	public static readonly tIL_Tokenizer
	Number = (Int | Nat)
	.SetName(nameof(Number));

	public static readonly tIL_Tokenizer
	Ident = (CharNotIn(SpazialChars).Modify((mStd.tSpan<mTextParser.tPos> aSpan, tChar aChar) => aChar.ToString()) | Text("..."))[1, null]
	.ModifyList(aTextList => aTextList.Reduce((tText a1, tText a2) => a1 + a2));
	
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
	
	public static readonly tIL_Tokenizer
	Token = (
		(-Char('"') + CharNotIn("\"") -Char('"'))
		.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tText aText) => new tToken { Type = tTokenType.Text, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Text)) |
		
		Number
		.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tInt32 aInt) => new tToken { Type = tTokenType.Number, Text = aInt.ToString(), Span = aSpan })
		.SetName(nameof(tTokenType.Number)) |
		
		Ident
		.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tText aText) => new tToken { Type = tTokenType.Ident, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Ident)) |
		
		(-Char('§') + Ident)
		.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tText aText) => new tToken { Type = tTokenType.KeyWord, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.KeyWord)) |
		
		(-Char('#') + Ident)
		.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tText aText) => new tToken { Type = tTokenType.Prefix, Text = aText, Span = aSpan })
		.SetName(nameof(tTokenType.Prefix)) |
		
		(
			Text(":=") |
			CharIn(".,:;()[]{}€\n").Modify((mStd.tSpan<mTextParser.tPos> aSpan, tChar aChar) => aChar.ToString())
		)
		.Modify((mStd.tSpan<mTextParser.tPos> aSpan, tText aText) => new tToken { Type = tTokenType.SpecialToken, Text = aText, Span = aSpan })
	);
	
	public static readonly tIL_Tokenizer
	Tokenizer = (Token -__)[0, null];
}
