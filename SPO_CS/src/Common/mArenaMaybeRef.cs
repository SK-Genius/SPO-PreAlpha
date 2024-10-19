public static class
mArenaMaybeRef {
	
	public readonly struct
	tArenaMaybeRef<t> where t : unmanaged {
		internal readonly tInt32 _Offset;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tArenaMaybeRef(
			tInt32 aOffset
		) {
			_Offset = aOffset;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static implicit
		operator tArenaMaybeRef<t>(
			mArenaRef.tArenaRef<t> aRef
		) => new (aRef._Offset);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe tArenaMaybeRef<t>
	NoneRef<t>(
	) where t : unmanaged
	=> new tArenaMaybeRef<t>(-1);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe tBool
	TryDeRef<t>(
		this in tArenaMaybeRef<t> aRef,
		mArena.tArena aArena,
		out t aValue
	) where t : unmanaged {
		if (aRef._Offset < 0) {
			aValue = default;
			return false;
		} else {
			var Src = (t*)(aArena._Buffer + aRef._Offset);
			aValue = *Src;
			return true;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Match<t>(
		this tArenaMaybeRef<t> aMaybeRef,
		out mArenaRef.tArenaRef<t> aRef
	) where t : unmanaged {
		if (aMaybeRef._Offset >= 0) {
			aRef = new mArenaRef.tArenaRef<t>(aMaybeRef._Offset);
			return true;
		} else {
			aRef = default;
			return false;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>(
		this tArenaMaybeRef<tIn> aMaybeRef,
		mStd.tFunc<tOut, mArenaRef.tArenaRef<tIn>> aOnSomeRef,
		mStd.tFunc<tOut> aOnNoneRef
	) where tIn : unmanaged where tOut : unmanaged
	=> aMaybeRef.Match(out var Ref)
	? aOnSomeRef(Ref)
	: aOnNoneRef();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mMaybe.tMaybe<mArenaRef.tArenaRef<t>>
	AsMaybe<t>(
		this tArenaMaybeRef<t> aMaybeRef
	) where t : unmanaged
	=> aMaybeRef.Match(
		aOnSomeRef: mMaybe.Some,
		aOnNoneRef: mMaybe.None<mArenaRef.tArenaRef<t>>
	);
	
}
