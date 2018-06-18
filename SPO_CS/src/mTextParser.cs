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
	
	public struct tError {
		public tPos Pos;
		public tText Message;
	}
	
	//================================================================================
	public static mList.tList<(mStd.tSpan<tPos> Span, tChar Char)>
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
					var Result = (
						new mStd.tSpan<tPos> {
							Start = {
								Col = Col.Value,
								Row = Row.Value
							},
							End = {
								Col = Col.Value,
								Row = Row.Value
							}
						},
						aChar
					);
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
	public static (mStd.tSpan<tPos> Span, tOut Result)
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tChar, tOut, tError> aParser,
		tText aText,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Stream = TextToStream(aText);
		var MaybeResult = aParser.StartParse(Stream, aDebugStream);
		mStd.Assert(
			MaybeResult.Match(out var Result, out var ErrorList),
			#if true
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
			#else
			""
			#endif
		);
		
		if (!Result.RestStream.IsEmpty()) {
			var Line = Result.RestStream.First().Span.Start.Row;
			throw mStd.Error(
				$"({Line}, 1): expected end of text\n" +
				$"{aText.Split('\n')[Line-1]}\n" +
				$"^"
			);
		}
		return Result.Result;
	}
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => a == aRefChar,
		a => new tError{ Pos = a.Span.Start, Message = $"expect {aRefChar}" }
	)
	.SetDebugName("'", aRefChar.ToString(), "'");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetNotChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => a != aRefChar,
		a => new tError{ Pos = a.Span.Start, Message = $"expect not {aRefChar}" }
	)
	.SetDebugName("'^", aRefChar.ToString(), "'");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetCharIn(
		tText aRefChars
	//================================================================================
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => {
			foreach (var RefChar in aRefChars) {
				if (a == RefChar) {
					return true;
				}
			}
			return false;
		},
		a => new tError{ Pos = a.Span.Start, Message = $"expect one of [{aRefChars}]" }
	)
	.SetDebugName("[", aRefChars, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetCharNotIn(
		tText aRefChars
	//================================================================================
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => {
			foreach (var RefChar in aRefChars) {
				if (a == RefChar) {
					return false;
				}
			}
			return true;
		},
		a => new tError{ Pos = a.Span.Start, Message = $"expect non of [{aRefChars}]" }
	)
	.SetDebugName("[^", aRefChars, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetCharInRange(
		tChar aMinChar,
		tChar aMaxChar
	//================================================================================
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => aMinChar <= a && a <= aMaxChar,
		a => new tError{ Pos = a.Span.Start, Message = $"expect one in [{aMinChar}...{aMaxChar}]" }
	)
	.SetDebugName("[", aMinChar, "..", aMaxChar, "]");
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tText, tError>
	GetToken(
		tText aToken
	//================================================================================
	) {
		mStd.AssertNotEq(aToken.Length, 0);
		
		var Parser = -GetChar(aToken[0]);
		foreach (var Char in aToken.Substring(1)) {
			Parser = -Parser._(GetChar(Char));
		}
		return Parser
		.Modify(aSpan => aToken)
		.AddError(
			a => new tError {
				Pos = a.Span.Start,
				Message = $"expect '{aToken}'"
			}
		)
		.SetDebugName("\"", aToken, "\"");
	}
	
	//================================================================================
	public static mParserGen.tParser<tPos, tChar, tOut, tError>
	SetName<tOut>(
		this mParserGen.tParser<tPos, tChar, tOut, tError> aParser,
		tText aName
	//================================================================================
	) => aParser.AddError(
		a => new tError{
			Pos = a.Span.Start,
			Message = $"invalid {aName}"
		}
	)
	.SetDebugName(aName);
}
