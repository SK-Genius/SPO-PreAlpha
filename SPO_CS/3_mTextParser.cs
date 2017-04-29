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
	
	//================================================================================
	public static mStd.tFunc<mStd.tMaybe<tChar>>
	TextToStream(
		tText a
	//================================================================================
	) {
		var I = (int?)0;
		return () => {
			mStd.tMaybe<tChar> Result;
			if (I < a.Length) {
				Result = mStd.OK(a[I.Value]);
				I += 1;
			} else {
				Result = mStd.Fail<tChar>();
			}
			return Result;
		};
	}
	
	public class tFailInfo {
		internal tInt32 _Line;
		internal tInt32 _Coll;
		internal tText _ErrorMessage;
	}
	
	//================================================================================
	public static mParserGen.tResultList
	ParseText(
		this mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>> aParser,
		tText aText,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var Text = TextStream(mTextParser.TextToStream(aText), aDebugStream);
		Text.MATCH(out var List, out var Info);
		var MaybeResult = aParser.StartParse(List, aDebugStream);
		mStd.Assert(
			MaybeResult.MATCH(out var Result),
			$"({Info._Line}, {Info._Coll}): {Info._ErrorMessage}"
		);
		Result.MATCH(out var ResultList, out var Rest);
		mStd.AssertEq(Rest, mList.List<mStd.tTuple<tChar, mStd.tAction<tText>>>());
		return ResultList;
	}
	
	//================================================================================
	public static mStd.tTuple<mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>>, tFailInfo>
	TextStream(
		mStd.tFunc<mStd.tMaybe<tChar>> aStream,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		var TextParserInfo = new tFailInfo {_Line = 1, _Coll = 1};
		var LineIter = (tInt32?)1;
		var CollIter = (tInt32?)0;
		return mStd.Tuple(
			mList.LasyList(
				() => {
					// fixiere iterator
					var MaybeCurrChar = aStream();
					if (MaybeCurrChar.MATCH(out var CurrChar)) {
						tText OutChar;
						switch (CurrChar) {
							case '\n': { OutChar = @"\n"; } break;
							case '\t': { OutChar = @"\t"; } break;
							default: { OutChar = CurrChar.ToString(); } break;
						}
						#if TRACE
							aDebugStream($"### ({LineIter},{CollIter+1}) -> '{OutChar}'");
						#endif
						
						if (CurrChar == '\n') {
							LineIter += 1;
							CollIter = 1;
						} else if (CurrChar != '\r') {
							CollIter += 1;
						}
						
						var CurrLine = LineIter.Value;
						var CurrColl = CollIter.Value;
						
						return mStd.OK(
							mStd.Tuple(
								CurrChar,
								mStd.Action(
									(tText aErrorMessage) => {
										if (
											CurrLine > TextParserInfo._Line || (
												CurrLine == TextParserInfo._Line &&
												CurrColl > TextParserInfo._Coll
											)
										) {
											TextParserInfo._Line = CurrLine;
											TextParserInfo._Coll = CurrColl;
											TextParserInfo._ErrorMessage = aErrorMessage;
										} else if (
											CurrLine == TextParserInfo._Line &&
											CurrColl == TextParserInfo._Coll
										) {
											TextParserInfo._ErrorMessage += "\n" + aErrorMessage;
										}
									}
								)
							)
						);
					} else {
						return mStd.Fail<mStd.tTuple<tChar, mStd.tAction<tText>>>();
					}
				}
			),
			TextParserInfo
		);
	}
	
	//================================================================================
	private static mStd.tFunc<tChar, mStd.tTuple<tChar, mStd.tAction<tText>>>
	Modifyer = (
		a
	//================================================================================
	) => {
		tChar Char;
		mStd.tAction<tText> _;
		a.MATCH(out Char, out _);
		return Char;
	};
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser(
		(mStd.tTuple<tChar, mStd.tAction<tText>> a) => {
			a.MATCH(out var Char, out var SendErrorMessage);
			SendErrorMessage($"char = {aRefChar}");
			return Char == aRefChar;
		}
	)
	.Modify(Modifyer)
	.SetDebugName("'", aRefChar.ToString(), "'");
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetNotChar(
		tChar aRefChar
	//================================================================================
	) => mParserGen.AtomParser(
		(mStd.tTuple<tChar, mStd.tAction<tText>> a) => {
			a.MATCH(out var Char, out var SendErrorMessage);
			SendErrorMessage($"char != {aRefChar}");
			return Char != aRefChar;
		}
	)
	.Modify(Modifyer)
	.SetDebugName("'^", aRefChar.ToString(), "'");
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetCharIn(
		tText aRefChars
	//================================================================================
	) => mParserGen.AtomParser(
		(mStd.tTuple<tChar, mStd.tAction<tText>> a) => {
			a.MATCH(out var Char, out var SendErrorMessage);
			SendErrorMessage($"char is one of {aRefChars}");
			foreach (var RefChar in aRefChars) {
				if (Char == RefChar) {
					return true;
				}
			}
			return false;
		}
	)
	.Modify(Modifyer)
	.SetDebugName("[", aRefChars, "]");
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetCharNotIn(
		tText aRefChars
	//================================================================================
	) => mParserGen.AtomParser(
		(mStd.tTuple<tChar, mStd.tAction<tText>> a) => {
			a.MATCH(out var Char, out var SendErrorMessage);
			SendErrorMessage($"char is NOT one of {aRefChars}");
			foreach (var RefChar in aRefChars) {
				if (Char == RefChar) {
					return false;
				}
			}
			return true;
		}
	)
	.Modify(Modifyer)
	.SetDebugName("[^", aRefChars, "]");
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetCharInRange(
		tChar aMinChar,
		tChar aMaxChar
	//================================================================================
	) => mParserGen.AtomParser(
		(mStd.tTuple<tChar, mStd.tAction<tText>> a) => {
			a.MATCH(out var Char, out var SendErrorMessage);
			SendErrorMessage($"char in {aMinChar}..{aMaxChar}");
			return aMinChar <= Char && Char <= aMaxChar;
		}
	)
	.Modify(Modifyer)
	.SetDebugName("[", aMinChar, "..", aMaxChar, "]");
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetToken(
		tText aToken
	//================================================================================
	) {
		var Parser = mParserGen.EmptyParser<mStd.tTuple<tChar, mStd.tAction<tText>>>();
		foreach (var Char in aToken) {
			Parser += GetChar(Char);
		}
		return Parser
		.ModifyList(a => mParserGen.ResultList(aToken))
		.SetDebugName("\"", aToken, "\"");
	}
	
	#region Test
	
	// TODO: add tests
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>>
	Test = mTest.Tests(
		mStd.Tuple(
			"TODO",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => true
			)
		)
	);
	
	#endregion
	
}