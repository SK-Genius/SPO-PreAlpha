﻿//IMPORT mList.cs

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

public static class mTextStream {
	
	public struct tPos {
		public tInt32 Row;
		public tInt32 Col;
	}
	
	//================================================================================
	public static tPos
	Pos(
		tInt32 aRow,
		tInt32 aCol
	//================================================================================
	) => new tPos {
		Row = aRow,
		Col = aCol
	};
	
	public struct tError {
		public tPos Pos;
		public tText Message;
	}
	
	//================================================================================
	public static tError
	Error(
		tPos aPos,
		tText aMessage
	//================================================================================
	) => new tError {
		Pos = aPos,
		Message = aMessage
	};
	
	//================================================================================
	public static mList.tList<tError>
	Reduce(
		this mList.tList<tError> aErrors
	//================================================================================
	) => aErrors.Reduce(
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
	);
	
	//================================================================================
	public static tText
	ToText(
		this tError aError,
		tText[] aSrcLines
	//================================================================================
	) {
		var Line = aSrcLines[aError.Pos.Row-1];
		var MarkerLine = TextToStream(
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
	
	//================================================================================
	public static tText
	ToText(
		this mList.tList<tError> aErrors,
		tText[] aSrcLines
	//================================================================================
	) => aErrors
		.Reduce()
		.Map(_ => _.ToText(aSrcLines))
		.Reduce("", (a1, a2) => a1 + "\n" + a2);
	
	//================================================================================
	public static mList.tList<(tPos Pos, tChar Char)>
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
					var Result = (Pos(Row.Value, Col.Value), aChar);
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
}
