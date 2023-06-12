#nullable enable

public static class
mMath {
	
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
