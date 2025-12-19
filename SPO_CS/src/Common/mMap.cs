// IMPORT mStd
// IMPORT mStream
// IMPORT mMaybe

public static class
mMap {
	public readonly struct
	tMap<tKey, tValue> {
		internal readonly mStream.tStream<(tKey, tValue)> _KeyValuePairs;
		internal readonly mStd.tFunc<tKey, tKey, tBool> _EqualsFunc;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tMap(
			mStream.tStream<(tKey, tValue)> aKeyValuePairs,
			mStd.tFunc<tKey, tKey, tBool> aEqualsFunc
		) {
			this._KeyValuePairs = aKeyValuePairs;
			this._EqualsFunc = aEqualsFunc;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tMap<tKey, tValue>
	Map<tKey, tValue>(
		mStd.tFunc<tKey, tKey, tBool> aEqualsFunc
	) => new(
		aKeyValuePairs: mStd.cEmpty,
		aEqualsFunc: aEqualsFunc
	);
	
	extension<tKey, tValue> (tMap<tKey, tValue> aMap) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public mMaybe.tMaybe<tValue>
		TryGet(
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
		public tMap<tKey, tValue>
		Remove(
			tKey aKey
		) => new(
			aEqualsFunc: aMap._EqualsFunc,
			aKeyValuePairs: aMap._KeyValuePairs.Where(
				[DebuggerHidden] ((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
			)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tMap<tKey, tValue>
		Set(
			tKey aKey,
			tValue aValue
		) => new(
			aEqualsFunc: aMap._EqualsFunc,
			aKeyValuePairs: mStream.Stream(
				(aKey, aValue),
				aMap._KeyValuePairs.Where(
					[DebuggerHidden] ((tKey Key, tValue) a) => !aMap._EqualsFunc(a.Key, aKey)
				)
			)
		);
	}
}
