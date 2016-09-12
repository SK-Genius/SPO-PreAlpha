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
	Map<
		tKey,
		tValue
	>(
		mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
	//================================================================================
	){
		return new tMap<tKey, tValue>{
			_KeyValuePairs = mList.List<mStd.tTuple<tKey, tValue>>(),
			_EqualsFunc = aEqualsFunc
		};
	}
	
	//================================================================================
	public static tBool
	TryGet<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey,
		out tValue aValue
	//================================================================================
	) {
		var RestList = aMap._KeyValuePairs;
		mStd.tTuple<tKey, tValue> KeyValuePair;
		while (RestList.MATCH(out KeyValuePair, out RestList)) {
			tKey Key;
			mStd.Assert(KeyValuePair.MATCH(out Key, out aValue));
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
		tValue Result;
		mStd.Assert(aMap.TryGet(aKey, out Result));
		return Result;
	}
	
	//================================================================================
	public static tMap<tKey, tValue>
	Remove<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	//================================================================================
	) {
		return new tMap<tKey, tValue>{
			_EqualsFunc = aMap._EqualsFunc,
			_KeyValuePairs = aMap._KeyValuePairs.Where(
				aKeyValuePair => {
					tKey Key;
					tValue Value;
					mStd.Assert(aKeyValuePair.MATCH(out Key, out Value));
					return aMap._EqualsFunc(Key, aKey);
				}
			)
		};
	}
	
	//================================================================================
	public static tMap<tKey, tValue>
	Set<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey,
		tValue aValue
	//================================================================================
	) {
		return new tMap<tKey, tValue>{
			_EqualsFunc = aMap._EqualsFunc,
			_KeyValuePairs = mList.List(
				mStd.Tuple(aKey, aValue),
				aMap._KeyValuePairs.Where(
					aKeyValuePair => {
						tKey Key;
						tValue Value;
						mStd.Assert(aKeyValuePair.MATCH(out Key, out Value));
						return !aMap._EqualsFunc(Key, aKey);
					}
				)
			)
		};
	}
	
	#region TEST
	
	// TODO
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"tMap.ToString()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					return true;
				}
			)
		)
	);
	
	#endregion
}