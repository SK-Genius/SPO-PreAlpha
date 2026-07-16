#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mError.cs
#:ref mAssert.cs

public static class
mAny {
	public readonly struct
	tAny {
		internal readonly tUnknown? _Value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tAny(tUnknown? aValue) {
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
			tUnknown? a
		) => a is tAny X && this.Equals(X);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAny
	Any<t>(
		t a
	) => new(a);
	
	extension (tAny a) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Is<t>(
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
		public tBool
		Is(
		) => a._Value is null;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		To<t>(
		) => (
			a.Is(out t Result)
			? Result
			: throw mError.Error($"To: {typeof(t).FullName} <- {a}")
		);
	}
}
