//IMPORT mStream.cs
//IMPORT mDebug.cs

#nullable enable

public static class
mMap {
	
	public readonly struct
	tMap<tKey, tValue> {
		internal readonly mStream.tStream<(tKey, tValue)>? _KeyValuePairs;
		internal readonly mStd.tFunc<tBool, tKey, tKey> _EqualsFunc;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tMap(
			mStream.tStream<(tKey, tValue)>? aKeyValuePairs,
			mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
		) {
			this._KeyValuePairs = aKeyValuePairs;
			this._EqualsFunc = aEqualsFunc;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMap<tKey, tValue>
	Map<tKey, tValue>(
		mStd.tFunc<tBool, tKey, tKey> aEqualsFunc
	) => new(
		aKeyValuePairs: mStream.Stream<(tKey, tValue)>(),
		aEqualsFunc: aEqualsFunc
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mMaybe.tMaybe<tValue>
	TryGet<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	) {
		foreach (var (Key, Value) in aMap._KeyValuePairs) {
			if (aMap._EqualsFunc(Key, aKey)) {
				return Value;
			}
		}
		return mStd.cEmpty;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMap<tKey, tValue>
	Remove<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey
	) => new(
		aEqualsFunc: aMap._EqualsFunc,
		aKeyValuePairs: aMap._KeyValuePairs.Where(
			[DebuggerHidden]((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
		)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMap<tKey, tValue>
	Set<tKey, tValue>(
		this tMap<tKey, tValue> aMap,
		tKey aKey,
		tValue aValue
	) => new(
		aEqualsFunc: aMap._EqualsFunc,
		aKeyValuePairs: mStream.Stream(
			(aKey, aValue),
			aMap._KeyValuePairs.Where(
				[DebuggerHidden]((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
			)
		)
	);
}
