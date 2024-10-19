public static class
mArenaRef {
	
	public readonly struct
	tArenaRef<t> where t : unmanaged {
		internal readonly tInt32 _Offset;
		
		internal
		tArenaRef(
			tInt32 aOffset
		) {
			_Offset = aOffset;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe tArenaRef<t>
	Alloc<t>(
		this mArena.tArena aArena,
		in t aValue
	) where t : unmanaged {
		var Des = (t*)(aArena._Buffer + aArena._NextOffset);
		*Des = aValue;
		var Ref = new tArenaRef<t>(aArena._NextOffset);
		aArena._NextOffset += sizeof(t);
		return Ref;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe void
	DeRef<t>(
		this in tArenaRef<t> aRef,
		mArena.tArena aArena,
		out t aValue
	) where t : unmanaged {
		var Src = (t*)(aArena._Buffer + aRef._Offset);
		aValue = *Src;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe t
	DeRef<t>(
		this in tArenaRef<t> aRef,
		mArena.tArena aArena
	) where t : unmanaged {
		aRef.DeRef(aArena, out var Result);
		return Result;
	}
	
}
