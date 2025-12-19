// IMPORT mStd
// IMPORT mError

public static class
mMaybe {
	public readonly struct
	tMaybe<t> {
		internal readonly tBool _HasValue;
		internal readonly t _Value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tMaybe(
			tBool aHasValue,
			t aValue
		) {
			this._HasValue = aHasValue;
			this._Value = aValue;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tMaybe<t>(
			mStd.tEmpty _
		) => new(false, default!);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tMaybe<t>(
			t a
		) => Some(a);
		
		public override tText
		ToString(
		) => this.Match(
			() => "-",
			_ => "" + _
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMaybe<t>
	Some<t>(
		t a
	) => new(true, a);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMaybe<t>
	None<t>(
	) => mStd.cEmpty;
	
	extension<t> (tMaybe<t> a) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		IsSome(
			out t aValue
		) {
			if (a._HasValue) {
				aValue = a._Value;
				return true;
			} else {
				aValue = default!;
				return false;
			}
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		IsNone(
		) => !a._HasValue;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tOut
		Match<tOut>( // nice but slow
			mStd.tFunc<tOut> aOnNone,
			mStd.tFunc<t, tOut> aOnSome
		) => (
			a.IsSome(out var Value)
			? aOnSome(Value)
			: aOnNone()
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tOut
		Match<tOut>( // nice but slow
			mStd.tFunc<t, tOut> aOnSome,
			mStd.tFunc<tOut> aOnNone
		) => (
		a.IsSome(out var Value)
			? aOnSome(Value)
			: aOnNone()
		);
		
		public tBool
		Eq(
			tMaybe<t> a2,
			mStd.tFunc<t, t, tBool> aEq
		) => a.Match(
			[DebuggerHidden] () => a2.IsNone(),
			[DebuggerHidden] (_1) => a2.Match(
				[DebuggerHidden] () => false,
				[DebuggerHidden] (_2) => aEq(_1, _2)
			)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tMaybe<tOut>
		Then<tOut>(
			mStd.tFunc<t, tOut> aMap
		) => a.IsSome(out var Value) ? Some(aMap(Value)) : mStd.cEmpty;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tMaybe<tOut>
		ThenTry<tOut>(
			mStd.tFunc<t, tMaybe<tOut>> aMap
		) => a.IsSome(out var Value) ? aMap(Value) : mStd.cEmpty;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		ElseUse(
			t aFallback
		) => a.IsSome(out var Value) ? Value : aFallback;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		Else(
			mStd.tFunc<t> aOnNone
		) => a.IsSome(out var Value) ? Value : aOnNone();
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tMaybe<t>
		ElseTry(
			mStd.tFunc<tMaybe<t>> aOnNone
		) => a.IsSome(out _) ? a : aOnNone();
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		AssertNotEmpty(
			mStd.tFunc<tText> aOnThrow
		) => a.IsSome(out var Value) ? Value : throw mError.Error(aOnThrow());
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		AssertNotEmpty(
			[CallerArgumentExpression(nameof(a))] tText aExpr = "",
			[CallerMemberName] tText aCaller = "",
			[CallerFilePath] tText aFile = "",
			[CallerLineNumber] tInt32 aLine = 0
		) => (
			a.IsSome(out var Value)
			? Value
			: throw mError.Error($"Error in '{aCaller}' ( {aFile}:{aLine} ): '{aExpr}' should not empty")
		);
	}
	
	public static mStd.tFunc<tMaybe<t>, tMaybe<t>, tBool>
	Eq<t>(
		mStd.tFunc<t, t, tBool> aEq
	) => [DebuggerHidden] (a1, a2) => a1.Eq(a2, aEq);
}
