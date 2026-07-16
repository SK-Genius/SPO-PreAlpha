#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs

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
	) => new () {
		_Buffer = Marshal.AllocHGlobal(aBufferSize),
		_BufferSize = aBufferSize,
		_NextOffset = 0,
	};
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArena
	New(
		tInt32 aBufferSize
	) => new () {
		_Buffer = Marshal.AllocHGlobal(aBufferSize),
		_BufferSize = aBufferSize,
		_NextOffset = 0,
	};
	
	extension (tArena aArena) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tArena
		TempArena(
		) => new () {
			_Buffer = aArena._Buffer + aArena._NextOffset,
			_BufferSize = aArena._BufferSize - aArena._NextOffset,
			_NextOffset = 0,
		};
	}
}