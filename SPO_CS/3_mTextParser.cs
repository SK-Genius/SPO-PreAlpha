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

public static class mTextParser {
	
	public struct tPos {
		public tInt32 Row;
		public tInt32 Col;
	}
	
	public struct tPosChar {
		public tPos Pos;
		public tChar Char;
	}
	
	public struct tError {
		public tPos Pos;
		public tText Message;
	}
	
	//================================================================================
	public static mList.tList<tPosChar>
	TextToStream(
		tText a
	//================================================================================
	) {
		var I = (int?)0;
		var Col = (int?)1;
		var Row = (int?)1;
		
		return mList.LasyList(
			() => {
				mStd.tMaybe<tPosChar, mStd.tVoid> Result;
				if (I < a.Length) {
					var Char = a[I.Value];
					if (Char == '\r') {
						I += 1;
						Char = a[I.Value];
					}
					Result = mStd.OK(
						new tPosChar {
							Char = Char,
							Pos = {
								Col = Col.Value,
								Row = Row.Value
							}
						}
					);
					I += 1;
					Col += 1;
					if (Char == '\n') {
						Col = 1;
						Row += 1;
					}
				} else {
					Result = mStd.Fail();
				}
				return Result;
			}
		);
	}
	
	//================================================================================
	public static mParserGen.tResultList
	ParseText(
		this mParserGen.tParser<tPosChar, tError> aParser,
		tText aText,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Stream = TextToStream(aText);
		var MaybeResult = aParser.StartParse(Stream, aDebugStream);
		if (!MaybeResult.Match(out var Result, out var ErrorList)) {
			mStd.Assert(
				false,
				ErrorList.Map(
					a => {
						var Line = aText.Split('\n')[a.Pos.Row-1];
						var MarkerLine = TextToStream(
							Line
						).Take(
							a.Pos.Col - 1
						).Map(
							aSymbol => aSymbol.Char == '\t' ? '\t' : ' '
						).Reduce(
							"",
							(aString, aChar) => aString + aChar
						);
						return (
							$"({a.Pos.Row}, {a.Pos.Col}): {a.Message}\n" +
							$"{Line}\n" +
							$"{MarkerLine}^\n"
						);
					}
				).Reduce("", (a1, a2) => a1 + "\n" + a2)
			);
		}
		var (ResultList, Rest) = Result;
		mStd.AssertEq(Rest, mList.List<tPosChar>());
		return ResultList;
	}
	
	//================================================================================
	private static mStd.tFunc<tChar, tPosChar>
	Modifyer = (
		tPosChar a
	//================================================================================
	) => a.Char;
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser<tPosChar, tError>(
		a => a.Char == aRefChar,
		a => new tError{ Pos = a.Pos, Message = $"expect {aRefChar}" }
	)
	.Modify(Modifyer)
	.SetDebugName("'", aRefChar.ToString(), "'");
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetNotChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser<tPosChar, tError>(
		a => a.Char != aRefChar,
		a => new tError{ Pos = a.Pos, Message = $"expect not {aRefChar}" }
	)
	.Modify(Modifyer)
	.SetDebugName("'^", aRefChar.ToString(), "'");
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetCharIn(
		tText aRefChars
	//================================================================================
	) => mParserGen.AtomParser<tPosChar, tError>(
		a => {
			foreach (var RefChar in aRefChars) {
				if (a.Char == RefChar) {
					return true;
				}
			}
			return false;
		},
		a => new tError{ Pos = a.Pos, Message = $"expect one of [{aRefChars}]" }
	)
	.Modify(Modifyer)
	.SetDebugName("[", aRefChars, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetCharNotIn(
		tText aRefChars
	//================================================================================
	) => mParserGen.AtomParser<tPosChar, tError>(
		a => {
			foreach (var RefChar in aRefChars) {
				if (a.Char == RefChar) {
					return false;
				}
			}
			return true;
		},
		a => new tError{ Pos = a.Pos, Message = $"expect non of [{aRefChars}]" }
	)
	.Modify(Modifyer)
	.SetDebugName("[^", aRefChars, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetCharInRange(
		tChar aMinChar,
		tChar aMaxChar
	//================================================================================
	) => mParserGen.AtomParser<tPosChar, tError>(
		a => aMinChar <= a.Char && a.Char <= aMaxChar,
		a => new tError{ Pos = a.Pos, Message = $"expect one in [{aMinChar}...{aMaxChar}]" }
	)
	.Modify(Modifyer)
	.SetDebugName("[", aMinChar, "..", aMaxChar, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetToken(
		tText aToken
	//================================================================================
	) {
		var Parser = mParserGen.EmptyParser<tPosChar, tError>();
		foreach (var Char in aToken) {
			Parser += GetChar(Char);
		}
		return Parser
		.ModifyList(a => mParserGen.ResultList(aToken))
		.AddError(
			a => new tError {
				Pos = a.Pos,
				Message = $"expect '{aToken}'"
			}
		)
		.SetDebugName("\"", aToken, "\"");
	}
	
	public static mParserGen.tParser<tPosChar, tError>
	SetName(
		this mParserGen.tParser<tPosChar, tError> aParser,
		tText aName
	) => aParser.AddError(
		a => new tError{
			Pos = a.Pos,
			Message = $"invalid {aName}"
		}
	)
	.SetDebugName(aName);
	
	#region Test
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mTextParser),
		mTest.Test(
			"TODO",
			aStreamOut => {
			}
		)
	);
	
	#endregion
}