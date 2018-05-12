﻿using tBool = System.Boolean;

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
	
	public struct tPosText {
		public tPos Pos;
		public tText Text;
	}
	
	public struct tPosInt {
		public tPos Pos;
		public tInt32 Int;
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
		var Col = (int?)1;
		var Row = (int?)1;
		
		return mList.List(a.ToCharArray())
			.Where(_ => _ != '\r')
			.Map(
				aChar => {
					var Result = new tPosChar {
						Char = aChar,
						Pos = {
							Col = Col.Value,
							Row = Row.Value
						}
					};
					if (aChar == '\n') {
						Col = 1;
						Row += 1;
					} else {
						Col += 1;
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
					var MarkerLine = TextToStream(
						Line
					).Take(
						aError.Pos.Col - 1
					).Map(
						aSymbol => aSymbol.Char == '\t' ? '\t' : ' '
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
		
		var (ResultList, Rest) = Result;
		if (!Rest.IsEmpty()) {
			var Line = Rest.First().Pos.Row;
			throw mStd.Error(
				$"({Line}, 1): expected end of text\n" +
				$"{aText.Split('\n')[Line-1]}\n" +
				$"^"
			);
		}
		return ResultList;
	}
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser<tPosChar, tError>(
		a => a.Char == aRefChar,
		a => new tError{ Pos = a.Pos, Message = $"expect {aRefChar}" }
	)
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
	.SetDebugName("[", aMinChar, "..", aMaxChar, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	GetToken(
		tText aToken
	//================================================================================
	) {
		mStd.AssertNotEq(aToken.Length, 0);
		
		var Parser = mParserGen.EmptyParser<tPosChar, tError>();
		foreach (var Char in aToken) {
			Parser += GetChar(Char);
		}
		return Parser
		.ModifyList(
			a => mParserGen.ResultList(
				new tPosText {
					Pos = a.Value.First().To<tPosChar>().Pos,
					Text = aToken
				}
			)
		)
		.AddError(
			a => new tError {
				Pos = a.Pos,
				Message = $"expect '{aToken}'"
			}
		)
		.SetDebugName("\"", aToken, "\"");
	}
	
	//================================================================================
	public static mParserGen.tParser<tPosChar, tError>
	SetName(
		this mParserGen.tParser<tPosChar, tError> aParser,
		tText aName
	//================================================================================
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