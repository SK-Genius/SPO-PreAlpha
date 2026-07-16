#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mMaybe.cs
#:ref mArena.cs
#:ref mArenaRef.cs

public static class
mArenaMaybeRef {
	public readonly struct
	tArenaMaybeRef<t> where t : unmanaged {
		internal readonly tInt32 _Offset;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	=> new (-1);
	
	extension<t> (in tArenaMaybeRef<t> aRef) where t : unmanaged {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public unsafe tBool
		TryDeRef(
			mArena.tArena aArena,
			out t aValue
		) {
			if (aRef._Offset < 0) {
				aValue = default;
				return false;
			} else {
				var Src = (t*)(aArena._Buffer + aRef._Offset);
				aValue = *Src;
				return true;
			}
		}
	}
	
	extension<t> (tArenaMaybeRef<t> aMaybeRef) where t : unmanaged {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Match(
			out mArenaRef.tArenaRef<t> aRefOut
		) {
			if (aMaybeRef._Offset >= 0) {
				aRefOut = new mArenaRef.tArenaRef<t>(aMaybeRef._Offset);
				return true;
			} else {
				aRefOut = default;
				return false;
			}
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tOut
		Match<tOut>(
			mStd.tFunc<mArenaRef.tArenaRef<t>, tOut> aOnSomeRef,
			mStd.tFunc<tOut> aOnNoneRef
		) where tOut : unmanaged
		=> aMaybeRef.Match(out var Ref)
		? aOnSomeRef(Ref)
		: aOnNoneRef();
		
		[Pure, DebuggerHidden]
		public mMaybe.tMaybe<mArenaRef.tArenaRef<t>>
		AsMaybe => aMaybeRef.Match(
			aOnSomeRef: mMaybe.Some,
			aOnNoneRef: mMaybe.None<mArenaRef.tArenaRef<t>>
		);
	}
}
