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

public static class mArrayList {
	
	public class tArrayList<t> {
		internal tInt32 _CurrSize;
		internal t[]    _Items;
		
		//================================================================================
		public tBool 
		Equals(
			tArrayList<t> a
		//================================================================================
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
		
		//================================================================================
		override public tBool 
		Equals(
			object a
		//================================================================================
		) { return this.Equals((tArrayList<t>)a); }
		
		//================================================================================
		public static tBool 
		operator==(
			tArrayList<t> a1,
			tArrayList<t> a2
		//================================================================================
		) { return a1.IsNull() ? a2.IsNull() : a1.Equals(a2); }
		
		//================================================================================
		public static tBool 
		operator!=(
			tArrayList<t> a1,
			tArrayList<t> a2
		//================================================================================
		) { return !(a1 == a2); }
	}
	
	//================================================================================
	public static tArrayList<t>
	List<t>(
		params t[] aArray
	//================================================================================
	) {
		return new tArrayList<t> {
			_CurrSize = aArray.Length,
			_Items = (t[])aArray.Clone()
		};
	}
	
	//================================================================================
	public static tBool
	IsEmpty<t>(
		this tArrayList<t> aList 
	//================================================================================
	) {
		return aList._CurrSize == 0;
	}
	
	//================================================================================
	private static void
	Resize<t>(
		this tArrayList<t> aList
	//================================================================================
	) {
		var NewArray = new t[mMath.Max(8, 3 * (aList._CurrSize >> 1))];
		System.Array.Copy(aList._Items, NewArray, aList._CurrSize);
		aList._Items = NewArray;
	}
	
	//================================================================================
	public static tArrayList<t>
	Push<t>(
		this tArrayList<t> aList,
		t                  aNewItem
	//================================================================================
	) {
		if (aList._CurrSize == aList._Items.Length) {
			aList.Resize();
		}
		aList._Items[aList._CurrSize] = aNewItem;
		aList._CurrSize += 1;
		return aList;
	}
	
	//================================================================================
	public static t
	Pop<t>(
		this tArrayList<t> aList 
	//================================================================================
	) {
		var Item = aList._Items[aList._CurrSize - 1];
		aList._CurrSize -= 1;
		if (aList._CurrSize >> 1 < aList._Items.Length) {
			aList.Resize();
		}
		return Item;
	}
	
	//================================================================================
	public static tArrayList<t>
	Pop<t>(
		this tArrayList<t> aList,
		out t aItem
	//================================================================================
	) {
		aItem = aList.Pop();
		return aList;
	}
	
	//================================================================================
	public static t
	Get<t>(
		tArrayList<t> aList,
		tInt32        aIndex
	//================================================================================
	) {
		return aList._Items[aIndex];
	}
	
	//================================================================================
	public static void
	Set<t>(
		tArrayList<t> aList,
		tInt32        aIndex,
		t             aValue
	//================================================================================
	) {
		aList._Items[aIndex] = aValue;
	}
	
	//================================================================================
	public static tArrayList<t>
	Concat<t>(
		tArrayList<t> a1,
		tArrayList<t> a2
	//================================================================================
	) {
		var Array = new t[a1._CurrSize + a2._CurrSize];
		System.Array.Copy(a1._Items, Array, a1._CurrSize);
		System.Array.Copy(a2._Items, 0, Array, a1._CurrSize, a2._CurrSize);
		return new tArrayList<t> {
			_CurrSize = Array.Length,
			_Items = Array
		};
		
	}
	
	//================================================================================
	public static mList.tList<t>
	ToLasyList<t>(
		this tArrayList<t> aList
	//================================================================================
	) {
		int? Index = 0;
		return mList.LasyList<t>(
			() => {
				if (Index >= aList._CurrSize) {
					return mStd.Fail<t>();
				}
				
				var Item = aList._Items[Index.Value];
				Index += 1;
				return mStd.OK(Item);
			}
		);
	}
	
	#region TEST
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"tArrayList.IsEmpty(...)",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.Assert(List<tInt32>().IsEmpty());
					mStd.AssertNot(List(1).IsEmpty());
					return true;
				}
			)
		),
		mStd.Tuple(
			"tArrayList.ToLasyList(...)",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List<tInt32>().ToLasyList(), mList.List<tInt32>());
					mStd.AssertEq(List(1).ToLasyList(), mList.List(1));
					mStd.AssertEq(List(1, 2, 3).ToLasyList(), mList.List(1, 2, 3));
					return true;
				}
			)
		),
		mStd.Tuple(
			"tArrayList.Push(...)",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List<tInt32>().Push(1).Push(2), List(1, 2));
					mStd.AssertEq(List(1, 2).Push(3).Push(4), List(1, 2, 3, 4));
					mStd.AssertEq(List(1, 2, 3, 4, 5, 6, 7, 8).Push(9), List(1, 2, 3, 4, 5, 6, 7, 8, 9));
					return true;
				}
			)
		),
		mStd.Tuple(
			"tArrayList.Pop(...)",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					{
						var L = List(1, 2, 3);
						mStd.AssertEq(L.Pop(), 3);
						mStd.AssertEq(L.Pop(), 2);
						mStd.AssertEq(L, List(1));
					}
					{
						var L = List(1, 2, 3);
						tInt32 X;
						tInt32 Y;
						mStd.AssertEq(L.Pop(out X).Pop(out Y), List(1));
						mStd.AssertEq(X, 3);
						mStd.AssertEq(Y, 2);
						mStd.AssertEq(L.Pop(out X), List<tInt32>());
						mStd.AssertEq(X, 1);
					}
					return true;
				}
			)
		),
		mStd.Tuple(
			"mArrayList.Concat(...)",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(Concat(List<tInt32>(), List<tInt32>()), List<tInt32>());
					mStd.AssertEq(Concat(List(1, 2), List<tInt32>()), List(1, 2));
					mStd.AssertEq(Concat(List<tInt32>(), List(1, 2)), List(1, 2));
					mStd.AssertEq(Concat(List(1, 2), List(3, 4, 5)), List(1, 2, 3, 4, 5));
					return true;
				}
			)
		)
	);
	
	#endregion
}