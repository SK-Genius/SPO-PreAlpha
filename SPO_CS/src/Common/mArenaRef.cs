#:include mStd.cs
#:include mArena.cs

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
	
	extension (mArena.tArena aArena) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public unsafe tArenaRef<t>
		Alloc<t>(
			in t aValue
		)  where t : unmanaged {
			var Des = (t*)(aArena._Buffer + aArena._NextOffset);
			*Des = aValue;
			var Ref = new tArenaRef<t>(aArena._NextOffset);
			aArena._NextOffset += sizeof(t);
			return Ref;
		}
	}
	
	extension<t> (in tArenaRef<t> aRef) where t : unmanaged {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public unsafe void
		DeRef(
			mArena.tArena aArena,
			out t aValue
		) {
			var Src = (t*)(aArena._Buffer + aRef._Offset);
			aValue = *Src;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public unsafe t
		DeRef(
			mArena.tArena aArena
		) {
			aRef.DeRef(aArena, out var Result);
			return Result;
		}
	}
}
