//IMPORT mStream.cs

#nullable enable

using tError = System.String;

public static class
mTextStream {
	
	public struct
	tPos {
		public tText Id;
		public tInt32 Row;
		public tInt32 Col;
		
		public override tText
		ToString(
		) => $"{this.Id}({this.Row}:{this.Col})";
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tPos
	Pos(
		tText aId,
		tInt32 aRow,
		tInt32 aCol
	) => new() {
		Id = aId,
		Row = aRow,
		Col = aCol
	};
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
			$"""
			{aError.Pos.Id}({aError.Pos.Row}, {aError.Pos.Col}) ERROR: {aError.Message}
			{Line}
			{MarkerLine}^
			
			"""
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	ToText(
		this mStream.tStream<(tPos Pos, tError Message)>? aErrors,
		tText[] aSrcLines
	) => aErrors.Reverse(
	).Map(
		a => a.ToText(aSrcLines)
	).Reduce(
		"",
		(a1, a2) => a1 + "\n" + a2
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStream.tStream<(tPos Pos, tChar Char)>?
	ToStream(
		this tText aText,
		tText aId
	) {
		var Col = (int?)1;
		var Row = (int?)1;
		
		return mStream.Stream(
			aText.ToCharArray()
		).Where(
			a => a != '\r'
		).Map(
			aChar => {
				var Result = (Pos(aId, Row.Value, Col.Value), aChar);
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
