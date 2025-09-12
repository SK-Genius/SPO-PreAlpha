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
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Clamp(
		this tInt32 a,
		tInt32 aMin,
		tInt32 aMax
	) => Min(Max(aMin, a), aMax);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tNat32
	Clamp(
		this tNat32 a,
		tNat32 aMin,
		tNat32 aMax
	) => Min(Max(aMin, a), aMax);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Abs(
		this tInt32 a
	) => a > 0 ? a : -a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Sign(
		this tInt32 a
	) => (
		a < 0 ? -1 :
		a == 0 ? 0 :
		1
	);
}
