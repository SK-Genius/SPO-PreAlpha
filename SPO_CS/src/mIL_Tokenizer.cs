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

using tPosChar = mTextParser.tPosChar;
using tPosText = mTextParser.tPosText;
using tPosInt = mTextParser.tPosInt;

using tIL_Tokenizer = mParserGen.tParser<mTextParser.tPosChar, mTextParser.tError>;

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
	.Modify((tPosChar aChar) => new tPosInt{ Pos = aChar.Pos, Int = (int)aChar.Char - (int)'0' })
	.SetName(nameof(Digit));
	
	public static readonly tIL_Tokenizer
	Nat = (Digit + (Digit | -Char('_'))[0, null])
	.ModifyList(a => a.Reduce((tPosInt aNumber, tPosInt aDigit) => new tPosInt{ Pos = aNumber.Pos, Int = 10*aNumber.Int+aDigit.Int }))
	.SetName(nameof(Nat));
	
	public static readonly tIL_Tokenizer
	PosSignum = Char('+')
	.Modify((tPosChar a) => new tPosInt{ Pos = a.Pos, Int = +1 })
	.SetName(nameof(PosSignum));
	
	public static readonly tIL_Tokenizer
	NegSignum = Char('-')
	.Modify((tPosChar a) => new tPosInt{ Pos = a.Pos, Int = -1 })
	.SetName(nameof(NegSignum));
	
	public static readonly tIL_Tokenizer
	Signum = (PosSignum | NegSignum)
	.SetName(nameof(Signum));
	
	public static readonly tIL_Tokenizer
	Int = (Signum + Nat)
	.Modify((tPosInt aSig, tPosInt aAbs) => new tPosInt{ Pos = aSig.Pos, Int = aSig.Int * aAbs.Int })
	.SetName(nameof(Int));
	
	public static readonly tIL_Tokenizer
	Number = ( Int | Nat )
	.SetName(nameof(Number));

	public static readonly tIL_Tokenizer
	Ident = (CharNotIn(SpazialChars).Modify((tPosChar a) => new tPosText { Text = a.Char.ToString(), Pos = a.Pos }) | Text("..."))[1, null]
	.ModifyList(aTextList => aTextList.Reduce((tPosText a1, tPosText a2) => new tPosText{ Text = a1.Text + a2.Text, Pos = a1.Pos }));
	
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
		public mTextParser.tPos Pos;
		
		override public tText ToString() => $"'{Text}'::{Type}@({Pos.Row}, {Pos.Col})";
	}
	
	public static readonly tIL_Tokenizer
	Token = (
		(-Char('"') + CharNotIn("\"") -Char('"'))
		.Modify((tPosText a) => new tToken { Type = tTokenType.Text, Text = a.Text, Pos = a.Pos })
		.SetName(nameof(tTokenType.Text)) |
		
		Number
		.Modify((tPosInt a) => new tToken { Type = tTokenType.Number, Text = a.Int.ToString(), Pos = a.Pos })
		.SetName(nameof(tTokenType.Number)) |
		
		Ident
		.Modify((tPosText a) => new tToken { Type = tTokenType.Ident, Text = a.Text, Pos = a.Pos })
		.SetName(nameof(tTokenType.Ident)) |
		
		(-Char('§') + Ident)
		.Modify((tPosText a) => new tToken { Type = tTokenType.KeyWord, Text = a.Text, Pos = a.Pos })
		.SetName(nameof(tTokenType.KeyWord)) |
		
		(-Char('#') + Ident)
		.Modify((tPosText a) => new tToken { Type = tTokenType.Prefix, Text = a.Text, Pos = a.Pos })
		.SetName(nameof(tTokenType.Prefix)) |
		
		(
			Text(":=") |
			CharIn(".,:;()[]{}€\n").Modify((tPosChar a) => new tPosText { Text = a.Char.ToString(), Pos = a.Pos })
		)
		.Modify((tPosText a) => new tToken { Type = tTokenType.SpecialToken, Text = a.Text, Pos = a.Pos })
	);
	
	public static readonly tIL_Tokenizer
	Tokenizer = (Token -__)[0, null];
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_Tokenizer),
		mTest.Test(
			"TODO",
			aDebugStream => {
				var TokenList = Tokenizer.ParseText(
					"a := §INT b <=> c \n a := [#b c]",
					aDebugStream
				).Value.Map(mStd.To<tToken>);
				mStd.AssertEq(
					TokenList,
					mList.List(
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 1 }, Text = "a", Type = tTokenType.Ident },
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 3 }, Text = ":=", Type = tTokenType.SpecialToken },
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 7 }, Text = "INT", Type = tTokenType.KeyWord },
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 11 }, Text = "b", Type = tTokenType.Ident },
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 13 }, Text = "<=>", Type = tTokenType.Ident },
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 17 }, Text = "c", Type = tTokenType.Ident },
						new tToken{ Pos = new mTextParser.tPos{ Row = 1, Col = 19 }, Text = "\n", Type = tTokenType.SpecialToken },
						
						new tToken{ Pos = new mTextParser.tPos{ Row = 2, Col = 2 }, Text = "a", Type = tTokenType.Ident },
						new tToken{ Pos = new mTextParser.tPos{ Row = 2, Col = 4 }, Text = ":=", Type = tTokenType.SpecialToken },
						new tToken{ Pos = new mTextParser.tPos{ Row = 2, Col = 7 }, Text = "[", Type = tTokenType.SpecialToken },
						new tToken{ Pos = new mTextParser.tPos{ Row = 2, Col = 9 }, Text = "b", Type = tTokenType.Prefix },
						new tToken{ Pos = new mTextParser.tPos{ Row = 2, Col = 11 }, Text = "c", Type = tTokenType.Ident },
						new tToken{ Pos = new mTextParser.tPos{ Row = 2, Col = 12 }, Text = "]", Type = tTokenType.SpecialToken }
					)
				);
			}
		)
	);
	
	#endregion
}
