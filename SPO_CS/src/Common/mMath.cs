#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs

public static class
mMath {
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Min(
		tInt32 a1,
		tInt32 a2
	) => (a1 < a2) ? a1 : a2;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tNat32
	Min(
		tNat32 a1,
		tNat32 a2
	) => (a1 < a2) ? a1 : a2;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Max(
		tInt32 a1,
		tInt32 a2
	) => (a1 > a2) ? a1 : a2;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tNat32
	Max(
		tNat32 a1,
		tNat32 a2
	) => (a1 > a2) ? a1 : a2;
	
	extension (tNat32 a) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tNat32
		Clamp(
			tNat32 aMin,
			tNat32 aMax
		) => Min(Max(aMin, a), aMax);
	}
	
	extension (tInt32 a) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tInt32
		Clamp(
			tInt32 aMin,
			tInt32 aMax
		) => Min(Max(aMin, a), aMax);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tInt32
		Abs(
		) => a > 0 ? a : -a;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tInt32
		Sign(
		) => (
			a < 0 ? -1 :
			a == 0 ? 0 :
			1
		);
	}
}
