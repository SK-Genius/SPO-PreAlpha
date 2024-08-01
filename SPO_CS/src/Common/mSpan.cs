public static class
mSpan {
	public readonly struct
	tSpan<tPos> {
		public readonly tPos Start;
		public readonly tPos End;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tSpan(
			tPos aStart,
			tPos aEnd
		) {
			this.Start = aStart;
			this.End = aEnd;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override readonly tText
		ToString(
		) => $"{this.Start}..{this.End}";
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tSpan<tPos>
	Span<tPos>(
		tPos aStart,
		tPos aEnd
	) => new(
		aStart: aStart,
		aEnd: aEnd
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tSpan<tPos>
	Span<tPos>(
		tPos aPos
	) => Span(aPos, aPos);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tSpan<tPos>
	Merge<tPos>(
		tSpan<tPos> a1,
		tSpan<tPos> a2
	) => (
		a1.Equals(default(tSpan<tPos>)) ? a2 :
		a2.Equals(default(tSpan<tPos>)) ? a1 :
		Span(a1.Start, a2.End)
	);
}
