using System.Collections.Generic;

public static class
mArenaStack {
	
	public readonly struct
	tArenaStack<t> where t : unmanaged {
		internal readonly t _Head;
		internal readonly mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> _Tail;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tArenaStack(
			t aHead,
			mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> aTail
		) {
			_Head = aHead;
			_Tail = aTail;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>>
	EmptyStack<t>(
	) where t : unmanaged
	=> mArenaMaybeRef.NoneRef<tArenaStack<t>>();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>>
	Push<t>(
		this mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> aStack,
		in mArena.tArena aArena,
		t aValue
	) where t : unmanaged
	=> aArena.Alloc(
		new tArenaStack<t>(aValue, aStack)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static (t Head, mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> Tail)
	Pop<t>(
		this mArenaRef.tArenaRef<tArenaStack<t>> aStack,
		mArena.tArena aArena
	) where t : unmanaged {
		aStack.DeRef(aArena, out var Stack);
		return (
			Head: Stack._Head,
			Tail: Stack._Tail
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mMaybe.tMaybe<
		(t Head, mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> Tail)
	>
	TryPop<t>(
		this mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> aStack,
		mArena.tArena aArena
	) where t : unmanaged
	=> aStack.Match(
		aOnSomeRef: _ => mMaybe.Some(_.Pop(aArena)),
		aOnNoneRef: () => mMaybe.None<
			(t Head, mArenaMaybeRef.tArenaMaybeRef<tArenaStack<t>> Tail)
		>()
	);
	
}
	