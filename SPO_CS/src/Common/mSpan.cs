#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs

public static class
mSpan {
	public readonly struct
	tSpan<tPos> {
		public readonly tPos Start;
		public readonly tPos End;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tSpan(
			tPos aStart,
			tPos aEnd
		) {
			this.Start = aStart;
			this.End = aEnd;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override tText
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
	
	public static tBool
	Eq<tPos>(
		tSpan<tPos> a1,
		tSpan<tPos> a2,
		mStd.tFunc<tPos, tPos, tBool> aEqPos
	) => aEqPos(a1.Start, a2.Start) && aEqPos(a1.End, a2.End);
	
	public static mStd.tFunc<tSpan<tPos>, tSpan<tPos>, tBool>
	Eq_<tPos>(
		mStd.tFunc<tPos, tPos, tBool> aEqPos
	) => (
		a1,
		a2
	) => Eq(a1, a2, aEqPos);
}
