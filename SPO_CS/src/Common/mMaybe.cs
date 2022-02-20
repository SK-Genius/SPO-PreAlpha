//IMPORT mStd.cs
//IMPORT mError.cs
//IMPORT mLazy.cs

#nullable enable

[DebuggerStepThrough]
public static class
mMaybe {
	
	[DebuggerStepThrough]
	[DebuggerDisplay("{_HasValue ? \"Some: \"+_Value : \"None\"}")]
	public readonly struct
	tMaybe<t> {
		
		internal readonly tBool _HasValue;
		internal readonly t _Value;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal tMaybe(
			tBool aHasValue,
			t aValue
		) {
			this._HasValue = aHasValue;
			this._Value = aValue;
		}
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static
		implicit operator tMaybe<t>(
			mStd.tEmpty _
		) => new(false, default!);
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static
		implicit operator tMaybe<t>(
			t a
		) => Some(a);
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tMaybe<t>
	Some<t>(
		t a
	) => new(true, a);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tMaybe<t>
	None<t>(
	) => mStd.cEmpty;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tBool
	IsNone<t>(
		this tMaybe<t> a
	) => !a._HasValue;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tOut
	Match<tIn, tOut>( // nice but slow
		this tMaybe<tIn> a,
		mStd.tFunc<tOut, tIn> Some,
		mStd.tFunc<tOut> None
	) => (
		a.IsSome(out var Value)
		? Some(Value)
		: None()
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tMaybe<tOut>
	Then<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tOut, tIn> aMap
	) => a.IsSome(out var Value) ? Some(aMap(Value)) : mStd.cEmpty;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tMaybe<tOut>
	ThenTry<tIn, tOut>(
		this tMaybe<tIn> a,
		mStd.tFunc<tMaybe<tOut>, tIn> aMap
	) => a.IsSome(out var Value) ? aMap(Value) : mStd.cEmpty;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tOut
	Else<tOut>(
		this tMaybe<tOut> a,
		mStd.tFunc<tOut> aElse
	) => a.IsSome(out var Value) ? Value : aElse();
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tOut
	Else<tOut>(
		this tMaybe<tOut> a,
		mLazy.tLazy<tOut> aElse
	) => a.IsSome(out var Value) ? Value : aElse.Value;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tOut
	ElseDefault<tOut>(
		this tMaybe<tOut> a
	) => a.IsSome(out var Value) ? Value : default;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tMaybe<tOut>
	ElseTry<tOut>(
		this tMaybe<tOut> a,
		mLazy.tLazy<tMaybe<tOut>> aElse
	) => a.IsSome(out var _) ? a : aElse.Value;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		tText aError
	) => a.IsSome(out var Value) ? Value : throw mError.Error(aError);
}
