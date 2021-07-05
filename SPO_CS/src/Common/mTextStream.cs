//IMPORT mStream.cs

#nullable enable

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

using tError = System.String;

[System.Diagnostics.DebuggerStepThrough]
public static class
mTextStream {
	
	public struct
	tPos {
		public tText Ident;
		public tInt32 Row;
		public tInt32 Col;
		
		public override tText
		ToString(
		) => $"{this.Ident}({this.Row}:{this.Col})";
	}
	
	public static tPos
	Pos(
		tText aIdent,
		tInt32 aRow,
		tInt32 aCol
	) => new tPos {
		Ident = aIdent,
		Row = aRow,
		Col = aCol
	};
	
	public static tText
	ToText(
		this (tPos Pos, tError Message) aError,
		tText[] aSrcLines
	) {
		var Line = aSrcLines[aError.Pos.Row-1];
		var MarkerLine = mStream.Stream(
			Line.ToCharArray()
		).Take(
			aError.Pos.Col - 1
		).Map(
			aChar => aChar == '\t' ? '\t' : ' '
		).Reduce(
			"",
			(aString, aChar) => aString + aChar
		);
		return (
			$"{aError.Pos.Ident}({aError.Pos.Row}, {aError.Pos.Col}) ERROR: {aError.Message}\n" +
			$"{Line}\n" +
			$"{MarkerLine}^\n"
		);
	}
	
	public static tText
	ToText(
		this mStream.tStream<(tPos Pos, tError Message)>? aErrors,
		tText[] aSrcLines
	) => aErrors.Reverse(
	).Map(
		_ => _.ToText(aSrcLines)
	).Reduce(
		"",
		(a1, a2) => a1 + "\n" + a2
	);
	
	public static mStream.tStream<(tPos Pos, tChar Char)>?
	ToStream(
		this tText aText,
		tText aIdent
	) {
		var Col = (int?)1;
		var Row = (int?)1;
		
		return mStream.Stream(
			aText.ToCharArray()
		).Where(
			_ => _ != '\r'
		).Map(
			aChar => {
				var Result = (Pos(aIdent, Row.Value, Col.Value), aChar);
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
