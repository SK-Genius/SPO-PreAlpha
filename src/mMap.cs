//IMPORT mList.cs
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

public static class mMap {
	
	public struct tMap<tKey, tValue> {
		internal mList.tList<(tKey, tValue)> _KeyValuePairs;
		internal mStd.tFunc<tBool, tKey, tKey> _EqualsFunc;
	}
	
	//================================================================================
	public static tMap<tKey, tValue>
	Map<tKey, tValue>(
		mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
	//================================================================================
	) => new tMap<tKey, tValue>{
		_KeyValuePairs = mList.List<(tKey, tValue)>(),
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
			var (Key, aValue_) = KeyValuePair;
			aValue = aValue_;
			if (aMap._EqualsFunc(Key, aKey)) {
				return true;
			}
		}
		aValue = default;
		return false;
	}
	
	//================================================================================
	public static tValue
	Get<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	//================================================================================
	) {
		mDebug.Assert(aMap.TryGet(aKey, out var Result));
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
			((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
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
			(aKey, aValue),
			aMap._KeyValuePairs.Where(
				((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
			)
		)
	};
}
