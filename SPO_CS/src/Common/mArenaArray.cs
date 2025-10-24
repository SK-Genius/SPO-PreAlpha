// IMPORT mStd
// IMPORT mArena
// IMPORT mArenaRef

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
					this._Offset + Start * sizeof(t),
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
					this._Offset + Index * sizeof(t)
				);
			}
		}
	}
	
	extension (mArena.tArena aArena) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public unsafe tArenaArray<t>
		NewArray<t>(
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
		public unsafe tArenaArray<t>
		NewArray<t>(
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
	}
		
	public struct
	tIterator<t> where t : unmanaged {
		internal tArenaArray<t> _Span;
		internal tInt32 _Index;
		
		public readonly unsafe mArenaRef.tArenaRef<t>
		Current {
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
			get => new (
				this._Span._Offset + this._Index * sizeof(t)
			);
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		MoveNext(
		) {
			this._Index += 1;
			return this._Index <= this._Span._Count;
		}
	}
	
	extension<t> (in tArenaArray<t> aSpan) where t : unmanaged{
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tIterator<t>
		GetEnumerator(
		) => new () {
			_Span = aSpan,
			_Index = -1,
		};
	}
}