using System;

public static class
mArenaArray {
	
	public readonly struct
	tArenaArray<t> where t : unmanaged {
		internal readonly tInt32 _Offset;
		internal readonly tInt32 _Count;
		
		internal
		tArenaArray(
			tInt32 aOffset,
			tInt32 aCount
		) {
			_Offset = aOffset;
			_Count = aCount;
		}
		
		public unsafe tArenaArray<t>
		this[
			System.Range aRange
		] {
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
			get {
				var Start = aRange.Start.IsFromEnd ? _Count - aRange.Start.Value : aRange.Start.Value;
				var End = aRange.End.IsFromEnd ? _Count - aRange.End.Value : aRange.End.Value;
				return new tArenaArray<t>(
					_Offset + Start * sizeof(t),
					End - Start
				);
			}
		}
		
		public unsafe mArenaRef.tArenaRef<t>
		this[
			System.Index aIndex
		] {
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
			get {
				var Index = aIndex.IsFromEnd ? _Count - aIndex.Value : aIndex.Value;
				if (0 < Index || Index <= _Count) {
					throw new System.IndexOutOfRangeException();
				}
				return new mArenaRef.tArenaRef<t>(
					_Offset + Index * sizeof(t)
				);
			}
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe tArenaArray<t>
	NewArray<t>(
		this mArena.tArena aArena,
		tInt32 aCount,
		in t aValue
	) where t : unmanaged {
		var Array = new tArenaArray<t>(aArena._NextOffset, aCount);
		while (aCount --> 0) {
			var Des = (t*)(aArena._NextOffset);
			*Des = aValue;
			aArena._NextOffset += sizeof(t);
		}
		return Array;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static unsafe tArenaArray<t>
	NewArray<t>(
		this mArena.tArena aArena,
		params System.Span<t> aValues
	) where t : unmanaged {
		var Array = new tArenaArray<t>(aArena._NextOffset, aValues.Length);
		foreach (var Value in aValues) {
			var Des = (t*)(aArena._Buffer + aArena._NextOffset);
			*Des = Value;
			aArena._NextOffset += sizeof(t);
		}
		return Array;
	}
	
	public struct
	tIterator<t> where t : unmanaged {
		internal tArenaArray<t> _Span;
		internal tInt32 _Index;
		
		public unsafe mArenaRef.tArenaRef<t>
		Current {
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
			get => new mArenaRef.tArenaRef<t>(
				_Span._Offset + _Index * sizeof(t)
			);
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		MoveNext(
		) {
			_Index += 1;
			return _Index <= _Span._Count;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tIterator<t>
	GetEnumerator<t>(
		this in tArenaArray<t> aSpan
	) where t : unmanaged
	=> new tIterator<t> {
		_Span = aSpan,
		_Index = -1,
	};
	
}