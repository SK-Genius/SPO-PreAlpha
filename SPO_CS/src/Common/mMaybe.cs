public static class
mMaybe {
	
	public readonly struct
	tMaybe<t> {
		
		internal readonly tBool _HasValue;
		internal readonly t _Value;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
		
		public override string
		ToString(
		) => this.Match(
			aOnNone: () => "-",
			aOnSome: _ => "" + _
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
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	IsSome<t>(
		this tMaybe<t> a,
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
	public static tBool
	IsNone<t>(
		this tMaybe<t> a
	) => !a._HasValue;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>( // nice but slow
		this tMaybe<tIn> a,
		mStd.tFunc<tOut> aOnNone,
		mStd.tFunc<tOut, tIn> aOnSome
	) => (
		a.IsSome(out var Value)
		? aOnSome(Value)
		: aOnNone()
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMaybe<tOut>
	ThenDo<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tOut, tIn> aMap
	) => a.IsSome(out var Value) ? Some(aMap(Value)) : mStd.cEmpty;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMaybe<tOut>
	ThenTry<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tMaybe<tOut>, tIn> aMap
	) => a.IsSome(out var Value) ? aMap(Value) : mStd.cEmpty;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Else<tOut>(
		this tMaybe<tOut> a,
		tOut aFallback
	) => a.IsSome(out var Value) ? Value : aFallback;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	ElseDo<tOut>(
		this tMaybe<tOut> a,
		mStd.tFunc<tOut> aOnNone
	) => a.IsSome(out var Value) ? Value : aOnNone();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Else<tOut>(
		this tMaybe<tOut> a,
		mLazy.tLazy<tOut> aFallback
	) => a.IsSome(out var Value) ? Value : aFallback.Value;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMaybe<tOut>
	ElseTry<tOut>(
		this tMaybe<tOut> a,
		mLazy.tLazy<tMaybe<tOut>> aFallback
	) => a.IsSome(out _) ? a : aFallback.Value;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		mStd.tFunc<tText> aOnThrow
	) => a.IsSome(out var Value) ? Value : throw mError.Error(aOnThrow());
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		[CallerLineNumber]tInt32 aLine = 0,
		[CallerMemberName]tText aCaller = "",
		[CallerFilePath]tText aFile = "",
		[CallerArgumentExpression("a")] string aExpr = ""
	) => a.IsSome(out var Value) ? Value : throw mError.Error($"Error in '{aCaller}' ({aFile}:{aLine}): '{aExpr}' should not empty");
}
