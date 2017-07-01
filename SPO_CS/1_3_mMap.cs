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

public static class mMap {
	public struct tMap<tKey, tValue> {
		internal mList.tList<mStd.tTuple<tKey, tValue>> _KeyValuePairs;
		internal mStd.tFunc<tBool, tKey, tKey> _EqualsFunc;
	}
	
	//================================================================================
	public static tMap<tKey, tValue>
	Map<tKey, tValue>(
		mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
	//================================================================================
	) => new tMap<tKey, tValue>{
		_KeyValuePairs = mList.List<mStd.tTuple<tKey, tValue>>(),
		_EqualsFunc = aEqualsFunc
	};
	
	//================================================================================
	public static tBool
	TryGet<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey,
		out tValue aValue
	//================================================================================
	) {
		var RestList = aMap._KeyValuePairs;
		while (RestList.Match(out var KeyValuePair, out RestList)) {
			KeyValuePair.Match(out var Key, out aValue);
			if (aMap._EqualsFunc(Key, aKey)) {
				return true;
			}
		}
		aValue = default(tValue);
		return false;
	}
	
	//================================================================================
	public static tValue
	Get<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	//================================================================================
	) {
		mStd.Assert(aMap.TryGet(aKey, out var Result));
		return Result;
	}
	
	//================================================================================
	public static tMap<tKey, tValue>
	Remove<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	//================================================================================
	) => new tMap<tKey, tValue>{
		_EqualsFunc = aMap._EqualsFunc,
		_KeyValuePairs = aMap._KeyValuePairs.Where(
			aKeyValuePair => {
				aKeyValuePair.Match(out tKey Key, out tValue _);
				return !aMap._EqualsFunc(Key, aKey);
			}
		)
	};
	
	//================================================================================
	public static tMap<tKey, tValue>
	Set<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey,
		tValue aValue
	//================================================================================
	) => new tMap<tKey, tValue>{
		_EqualsFunc = aMap._EqualsFunc,
		_KeyValuePairs = mList.List(
			mStd.Tuple(aKey, aValue),
			aMap._KeyValuePairs.Where(
				aKeyValuePair => {
					aKeyValuePair.Match(out tKey Key, out tValue _);
					return !aMap._EqualsFunc(Key, aKey);
				}
			)
		)
	};
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mMap),
		mTest.Test(
			"tMap.Get",
			aStreamOut => {
				var TextToInt = Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mStd.AssertEq(TextToInt.Get("one"), 1);
				mStd.AssertEq(TextToInt.Get("two"), 2);
			}
		),
		mTest.Test(
			"tMap.TryGet",
			aStreamOut => {
				var TextToInt = Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				tInt32 Num;
				mStd.Assert(TextToInt.TryGet("one", out Num));
				mStd.AssertEq(Num, 1);
				mStd.Assert(TextToInt.TryGet("two", out Num));
				mStd.AssertEq(Num, 2);
				mStd.AssertNot(TextToInt.TryGet("zero", out Num));
			}
		),
		mTest.Test(
			"tMap.Remove",
			aStreamOut => {
				var TextToInt = Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				tInt32 Num;
				mStd.Assert(TextToInt.TryGet("one", out Num));
				mStd.AssertEq(Num, 1);
				mStd.Assert(TextToInt.TryGet("two", out Num));
				mStd.AssertEq(Num, 2);
				TextToInt = TextToInt.Remove("one");
				mStd.AssertNot(TextToInt.TryGet("one", out Num));
				mStd.Assert(TextToInt.TryGet("two", out Num));
				mStd.AssertEq(Num, 2);
			}
		)
	);
	
	#endregion
}