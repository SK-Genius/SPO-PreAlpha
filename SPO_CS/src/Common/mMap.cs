//IMPORT mStream.cs
//IMPORT mDebug.cs

#nullable enable

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

public static class
mMap {
	
	public readonly struct
	tMap<tKey, tValue> {
		internal readonly mStream.tStream<(tKey, tValue)>? _KeyValuePairs;
		internal readonly mStd.tFunc<tBool, tKey, tKey> _EqualsFunc;
		
		internal tMap(
			mStream.tStream<(tKey, tValue)>? aKeyValuePairs,
			mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
		) {
			_KeyValuePairs = aKeyValuePairs;
			_EqualsFunc = aEqualsFunc;
		}
	}
	
	public static tMap<tKey, tValue>
	Map<tKey, tValue>(
		mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
	) => new tMap<tKey, tValue>(
		aKeyValuePairs: mStream.Stream<(tKey, tValue)>(),
		aEqualsFunc: aEqualsFunc
	);
	
	public static mMaybe.tMaybe<tValue>
	Get<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	) {
		var RestList = aMap._KeyValuePairs;
		while (RestList.Match(out var KeyValuePair, out RestList)) {
			var (Key, Value) = KeyValuePair;
			if (aMap._EqualsFunc(Key, aKey)) {
				return mMaybe.Some(Value);
			}
		}
		return mStd.cEmpty;
	}
	
	public static tValue
	ForceGet<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	) => aMap.Get(aKey).ElseThrow("impossible");
	
	public static tMap<tKey, tValue>
	Remove<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	) => new tMap<tKey, tValue>(
		aEqualsFunc: aMap._EqualsFunc,
		aKeyValuePairs: aMap._KeyValuePairs.Where(
			((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
		)
	);
	
	public static tMap<tKey, tValue>
	Set<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey,
		tValue aValue
	) => new tMap<tKey, tValue>(
		aEqualsFunc: aMap._EqualsFunc,
		aKeyValuePairs: mStream.Stream(
			(aKey, aValue),
			aMap._KeyValuePairs.Where(
				((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
			)
		)
	);
}
