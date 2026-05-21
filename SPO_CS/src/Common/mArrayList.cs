#:include mStd.cs
#:include mStream.cs
#:include mMath.cs
#:include mAssert.cs

public static class
mArrayList {
	public sealed class
	tArrayList<t> {
		internal tNat32 _CurrSize;
		public t[] _Items = []; // TODO: internal
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Equals(
			tArrayList<t> a
		) {
			if (this._CurrSize != a._CurrSize) {
				return false;
			}
			
			for (var Index = 0; Index < this._CurrSize; Index += 1) {
				if (!Equals(this._Items[Index], a._Items[Index])) {
					return false;
				}
			}
			return true;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override tBool
		Equals(
			tUnknown? a
		) => this.Equals((tArrayList<t>)a!);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArrayList<t>
	List<t>(
		params System.Span<t> aArray
	) => new() {
		_CurrSize = (tNat32)aArray.Length,
		_Items = aArray.ToArray()
	};
	
	extension<t> (tArrayList<t> aList) {
		[Pure, DebuggerHidden]
		public tNat32
		Size => aList._CurrSize;
		
		[Pure, DebuggerHidden]
		public tBool
		IsEmpty => aList.Size == 0;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		private void
		Resize(
		) {
			var NewArray = new t[mMath.Max(8, 3 * (aList._CurrSize >> 1))];
			System.Array.Copy(aList._Items, NewArray, aList._CurrSize);
			aList._Items = NewArray;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tArrayList<t>
		Push(
			t aNewItem
		) {
			if (aList._CurrSize == aList._Items.Length) {
				aList.Resize();
			}
			aList._Items[aList._CurrSize] = aNewItem;
			aList._CurrSize += 1;
			return aList;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tArrayList<t>
		Push(
			params System.Span<t> aNewItems
		) {
			foreach (var NewItem in aNewItems) {
				aList = aList.Push(NewItem);
			}
			return aList;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		Pop(
		) {
			var Item = aList._Items[aList._CurrSize - 1];
			aList._CurrSize -= 1;
			if (aList._CurrSize < (aList._Items.Length >> 1)) {
				aList.Resize();
			}
			return Item;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tArrayList<t>
		Pop(
			out t aItem
		) {
			aItem = aList.Pop();
			return aList;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public t
		Get(
			tNat32 aIndex
		) {
			mAssert.IsTrue(aIndex < aList._CurrSize);
			return aList._Items[aIndex];
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		Set(
			tNat32 aIndex,
			t aValue
		) {
			aList._Items[aIndex] = aValue;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		Update(
			tNat32 aIndex,
			mStd.tFunc<t, t> aOnUpdate
		) {
			ref var Item = ref aList._Items[aIndex];
			Item = aOnUpdate(Item);
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArrayList<t>
	Concat<t>(
		tArrayList<t> a1,
		tArrayList<t> a2
	) {
		var Array = new t[a1._CurrSize + a2._CurrSize];
		System.Array.Copy(a1._Items, Array, a1._CurrSize);
		System.Array.Copy(a2._Items, 0, Array, a1._CurrSize, a2._CurrSize);
		return new tArrayList<t> {
			_CurrSize = (tNat32)Array.Length,
			_Items = Array,
		};
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStream.tStream<t>
	ToLazyList<t>(
		this tArrayList<t> aList
	) => aList.ToStream(0);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStream.tStream<t>
	ToStream<t>(
		this tArrayList<t> aList,
		tInt32 aStartIndex
	) => (
		(aStartIndex >= aList._CurrSize)
		? mStd.cEmpty
		: mStream.Stream(
			aList._Items[aStartIndex],
			() => aList.ToStream(aStartIndex + 1)
		)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStream.tStream<t>
	ToStream<t>(
		this tArrayList<t> aList
	) => aList.ToStream(0);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tArrayList<t>
	ToArrayList<t>(
		this mStream.tStream<t> aList
	) => aList.Reduce(List<t>(), Push);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t[]
	ToArray<t>(
		this tArrayList<t> a
	) {
		var Count = a._CurrSize;
		var Res = new t[Count];
		while (Count --> 0) {
			Res[Count] = a._Items[Count];
		}
		return Res;
	}
}
