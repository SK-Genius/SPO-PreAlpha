//IMPORT mStream.cs
//IMPORT mMath.cs
//IMPORT mDebug.cs

using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(nameof(mArrayList)+"_Test")]

public static class
mArrayList {
	
	public sealed class
	tArrayList<t> {
		internal tInt32 _CurrSize;
		internal t[] _Items;
		
		public tBool 
		Equals(
			tArrayList<t> a
		) {
			if (this._CurrSize != a._CurrSize) {
				return false;
			}
			
			for (var Index = 0; Index < this._CurrSize; Index += 1) {
				if (!this._Items[Index].Equals(a._Items[Index])) {
					return false;
				}
			}
			return true;
		}
		
		override public tBool
		Equals(
			object a
		) => this.Equals((tArrayList<t>)a);
	}
	
	public static tArrayList<t>
	List<t>(
		params t[] aArray
	) => new tArrayList<t> {
		_CurrSize = aArray.Length,
		_Items = (t[])aArray.Clone()
	};
	
	public static tInt32
	Size<t>(
		this tArrayList<t> aList 
	) => aList._CurrSize;
	
	public static tBool
	IsEmpty<t>(
		this tArrayList<t> aList 
	) => aList.Size() == 0;
	
	private static void
	Resize<t>(
		this tArrayList<t> aList
	) {
		var NewArray = new t[mMath.Max(8, 3 * (aList._CurrSize >> 1))];
		System.Array.Copy(aList._Items, NewArray, aList._CurrSize);
		aList._Items = NewArray;
	}
	
	public static tArrayList<t>
	Push<t>(
		this tArrayList<t> aList,
		t aNewItem
	) {
		if (aList._CurrSize == aList._Items.Length) {
			aList.Resize();
		}
		aList._Items[aList._CurrSize] = aNewItem;
		aList._CurrSize += 1;
		return aList;
	}
	
	public static tArrayList<t>
	Push<t>(
		this tArrayList<t> aList,
		params t[] aNewItems
	) {
		foreach (var NewItem in aNewItems) {
			aList = aList.Push(NewItem);
		}
		return aList;
	}
	
	public static t
	Pop<t>(
		this tArrayList<t> aList 
	) {
		var Item = aList._Items[aList._CurrSize - 1];
		aList._CurrSize -= 1;
		if (aList._CurrSize < (aList._Items.Length >> 1)) {
			aList.Resize();
		}
		return Item;
	}
	
	public static tArrayList<t>
	Pop<t>(
		this tArrayList<t> aList,
		out t aItem
	) {
		aItem = aList.Pop();
		return aList;
	}
	
	public static t
	Get<t>(
		this tArrayList<t> aList,
		tInt32 aIndex
	) {
		mDebug.Assert(aIndex < aList._CurrSize);
		return aList._Items[aIndex];
	}
	
	public static void
	Set<t>(
		this tArrayList<t> aList,
		tInt32 aIndex,
		t aValue
	) {
		aList._Items[aIndex] = aValue;
	}
	
	public static tArrayList<t>
	Concat<t>(
		tArrayList<t> a1,
		tArrayList<t> a2
	) {
		var Array = new t[a1._CurrSize + a2._CurrSize];
		System.Array.Copy(a1._Items, Array, a1._CurrSize);
		System.Array.Copy(a2._Items, 0, Array, a1._CurrSize, a2._CurrSize);
		return new tArrayList<t> {
			_CurrSize = Array.Length,
			_Items = Array
		};
	}
	
	public static mStream.tStream<t>
	ToLasyList<t>(
		this tArrayList<t> aList,
		tInt32 aStartIndex
	) => (
		(aStartIndex >= aList._CurrSize)
		? mStream.Stream<t>()
		: mStream.Stream(
			aList._Items[aStartIndex],
			() => aList.ToLasyList(aStartIndex + 1)
		)
	);
	
	public static mStream.tStream<t>
	ToStream<t>(
		this tArrayList<t> aList
	) => aList.ToLasyList(0);
	
	public static tArrayList<t>
	ToArrayList<t>(
		this mStream.tStream<t> aList
	) => aList.Reduce(List<t>(), Push);
	
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
