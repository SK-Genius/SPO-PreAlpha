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
	
	public static mStd.tTuple<mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>>, tFailInfo>
	TextStream(
		mStd.tFunc<mStd.tMaybe<tChar>> aStream
	) {
		var TextParserInfo = new tFailInfo {_Line = 1, _Coll = 1};
		var LineIter = (tInt32?)1;
		var CollIter = (tInt32?)0;
		return mStd.Tuple(
			mList.LasyList(
				() => {
					// fixiere iterator
					var MaybeCurrChar = aStream();
					tChar CurrChar;
					if (MaybeCurrChar.MATCH(out CurrChar)) {
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
	) {
		return mParserGen.AtomParser(
			(tChar aChar, mStd.tAction<tText> aSendErrorMessage) => {
				aSendErrorMessage("char = "+aRefChar);
				return aChar == aRefChar;
			}
		).Modify(Modifyer);
	}
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetNotChar(
		tChar aRefChar
	//================================================================================
	) {
		return mParserGen.AtomParser(
			(tChar aChar, mStd.tAction<tText> aSendErrorMessage) => {
				aSendErrorMessage("char != "+aRefChar);
				return aChar != aRefChar;
			}
		).Modify(Modifyer);
	}
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetCharIn(
		tText aRefChars
	//================================================================================
	) {
		return mParserGen.AtomParser(
			(tChar aChar, mStd.tAction<tText> aSendErrorMessage) => {
				aSendErrorMessage("char is one of "+aRefChars);
				foreach (var RefChar in aRefChars) {
					if (aChar == RefChar) {
						return true;
					}
				}
				return false;
			}
		).Modify(Modifyer);
	}
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetCharNotIn(
		tText aRefChars
	//================================================================================
	) {
		return mParserGen.AtomParser(
			(tChar aChar, mStd.tAction<tText> aSendErrorMessage) => {
				aSendErrorMessage("char is NOT one of "+aRefChars);
				foreach (var RefChar in aRefChars) {
					if (aChar == RefChar) {
						return false;
					}
				}
				return true;
			}
		).Modify(Modifyer);
	}
	
	//================================================================================
	public static mParserGen.tParser<mStd.tTuple<tChar, mStd.tAction<tText>>>
	GetCharInRange(
		tChar aMinChar,
		tChar aMaxChar
	//================================================================================
	) {
		return mParserGen.AtomParser(
			(tChar aChar, mStd.tAction<tText> aSendErrorMessage) => {
				aSendErrorMessage("char in "+aMinChar+".."+aMaxChar);
				return aMinChar <= aChar && aChar <= aMaxChar;
			}
		).Modify(Modifyer);
	}
	
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
		return Parser.Modify_(a => mParserGen.ResultList(aToken));
	}
	
	#region Test
	
	// TODO: test
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"GetChar",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					return true;
				}
			)
		)
	);
	
	#endregion
	
}