public static class
mArena {
	
	public sealed class
	tArena {
		internal tCPtr _Buffer;
		internal tInt32 _BufferSize;
		internal tInt32 _NextOffset;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArena
	NewArena(
		tInt32 aBufferSize
	) => new tArena {
		_Buffer = Marshal.AllocHGlobal(aBufferSize),
		_BufferSize = aBufferSize,
		_NextOffset = 0,
	};
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArena
	New(
		tInt32 aBufferSize
	) => new tArena {
		_Buffer = Marshal.AllocHGlobal(aBufferSize),
		_BufferSize = aBufferSize,
		_NextOffset = 0,
	};
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArena
	TempArena(
		this tArena aArena
	) => new tArena {
		_Buffer = aArena._Buffer + aArena._NextOffset,
		_BufferSize = aArena._BufferSize - aArena._NextOffset,
		_NextOffset = 0,
	};
	
}