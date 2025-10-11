// IMPORT mStd
// IMPORT mStream

using tError = System.String;

public static class
mTextStream {
	public struct
	tPos {
		public tText Id;
		public tNat32 Row;
		public tNat32 Col;
		
		public override readonly tText
		ToString(
		) => $"{this.Id}({this.Row}:{this.Col})";
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tPos
	Pos(
		tText aId,
		tNat32 aRow,
		tNat32 aCol
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
		var Line = aSrcLines.Length > 0 && aError.Pos.Row > 0
			? aSrcLines[aError.Pos.Row - 1]
			: "";
		
		var MarkerLine = mStream.Stream(
			System.MemoryExtensions.AsSpan(Line)
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
			{aError.Pos.Id}:{aError.Pos.Row} ERROR: {aError.Message}
			{aError.Pos.Row}: {Line}
			{new tText(' ', ("" + aError.Pos.Row).Length + 2)}{MarkerLine}^
			
			"""
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	ToText(
		this mStream.tStream<(tPos Pos, tError Message)> aErrors,
		tText[] aSrcLines
	//) => aErrors.Reduce("", (aOut, aError) => aOut + "\n" + aError.ToText(aSrcLines));
	) => aErrors.GroupAndSortBy(
		_ => _.Pos.Id,
		tText.CompareOrdinal
	).ToStream(
	).Map(
		aFile => "" + aFile.Key + aFile.Value.GroupAndSortBy(
			_ => _.Pos.Row,
			(a1, a2) => ((tInt32)a1 - (tInt32)a2).Sign()
		).ToStream(
		).TryLast(
		).Then(
			aRow => $":{aRow.Key} ERROR " + aRow.Value.GroupAndSortBy(
				_ => _.Pos.Col,
				(a1, a2) => ((tInt32)a1 - (tInt32)a2).Sign()
			).ToStream(
			).Map(
				aCol => $"""
				
				{aRow.Key}: {aSrcLines[((tInt32)aRow.Key - 1).Clamp(0, aSrcLines.Length - 1)].TrimEnd().Replace('\t', ' ')}
				{" ┌" + new tText('─', ("" + aRow.Key).Length + (tInt32)aCol.Key - 1)}^
				
				""" + aCol.Value.Sort(
					(a1, a2) => tText.CompareOrdinal(a1.Message, a2.Message)
				).DontRepeat(
				).Map(
					_ => $" ├ {_.Message}\n"
				).Join(
					(a1, a2) => a1 + a2,
					""
				)
			).TryLast(
			)
		).ElseUse(
			""
		)
	).Join(
		(a1, a2) => a1 + a2,
		""
	);
	
	public static tBool
	Eq(
		tPos a1,
		tPos a2
	) => a1.Id == a2.Id && a1.Row == a2.Row && a1.Col == a2.Col;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStream.tStream<(tPos Pos, tChar Char)>
	ToStream(
		this tText aText,
		tText aId
	) {
		var Col = (tNat32?)1;
		var Row = (tNat32?)1;
		
		return mStream.Stream(
			System.MemoryExtensions.AsSpan(aText)
		).Where(
			_ => _ != '\r'
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
