public static class
mAny {
	public readonly struct
	tAny {
		internal readonly object? _Value;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tAny(object? aValue) {
			this._Value = aValue;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Equals(
			tAny a
		) => (
			this._Value is not null &&
			this._Value.Equals(a._Value)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override tBool
		Equals(
			object? a
		) => a is tAny X && this.Equals(X);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAny
	Any<t>(
		t a
	) => new(a);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tBool
	Is<t>(
		this tAny a,
		out t aValue
	) {
#if DEBUG
		if (typeof(t) == typeof(tAny)) {
			throw mError.Error("");
		}
#endif
		mAssert.IsNotNull(a._Value);
		
		if (a._Value is t Value) {
			aValue = Value;
			return true;
		} else {
			aValue = default!;
			return false;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tBool
	Is(
		this tAny a
	) => a._Value is null;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	To<t>(
		this tAny a
	) => (
		a.Is(out t Result)
		? Result
		: throw mError.Error($"To: {typeof(t).FullName} <- {a}")
	);
}
