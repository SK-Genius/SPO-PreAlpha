#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mMaybe.cs
#:ref mStream.cs

public static class
mRecordFields {
	// Sorted storage gives each field set one layout, independent of insertion order.
	public readonly struct
	tFields<tKey, tValue> {
		internal readonly mStd.tFunc<tKey, tKey, tInt32> KeyCompare;
		internal readonly (tKey Key, tValue Value)[] Items;
		
		internal
		tFields(
			mStd.tFunc<tKey, tKey, tInt32> aKeyCompare,
			(tKey Key, tValue Value)[] aItems
		) {
			this.KeyCompare = aKeyCompare;
			this.Items = aItems;
		}
	}
	
	public static tFields<tKey, tValue>
	Empty<tKey, tValue>(
		mStd.tFunc<tKey, tKey, tInt32> aKeyCompare
	) => new(aKeyCompare, []);
	
	private static (tInt32 Index, tBool Found)
	Find<tKey, tValue>(
		this tFields<tKey, tValue> aFields,
		tKey aKey
	) {
		var Lower = 0;
		var Upper = aFields.Items?.Length ?? 0;
		while (Lower < Upper) {
			var Middle = Lower + (Upper - Lower) / 2;
			var Order = aFields.KeyCompare(aFields.Items![Middle].Key, aKey);
			if (Order < 0) {
				Lower = Middle + 1;
			} else if (Order > 0) {
				Upper = Middle;
			} else {
				return (Middle, true);
			}
		}
		return (Lower, false);
	}
	
	public static mMaybe.tMaybe<tValue>
	TryGet<tKey, tValue>(
		this tFields<tKey, tValue> aFields,
		tKey aKey
	) {
		var (Index, Found) = aFields.Find(aKey);
		return Found ? aFields.Items[Index].Value : mStd.cEmpty;
	}
	
	public static tFields<tKey, tValue>
	Set<tKey, tValue>(
		this tFields<tKey, tValue> aFields,
		tKey aKey,
		tValue aValue
	) {
		var (Index, ReplacesField) = aFields.Find(aKey);
		var Items = aFields.Items ?? [];
		var Result = new (tKey Key, tValue Value)[Items.Length + (ReplacesField ? 0 : 1)];
		System.Array.Copy(Items, Result, Index);
		Result[Index] = (aKey, aValue);
		var TailStart = Index + (ReplacesField ? 1 : 0);
		System.Array.Copy(Items, TailStart, Result, Index + 1, Items.Length - TailStart);
		return new(aFields.KeyCompare, Result);
	}
	
	public static mStream.tStream<(tKey Key, tValue Value)>
	ToStream<tKey, tValue>(
		this tFields<tKey, tValue> aFields
	) => (aFields.Items ?? []).AsStream();
	
	public static tBool
	ContentsEqual<tKey, tValue>(
		this tFields<tKey, tValue> aLeft,
		tFields<tKey, tValue> aRight,
		mStd.tFunc<tValue, tValue, tBool> aValuesEqual
	) {
		var Left = aLeft.Items ?? [];
		var Right = aRight.Items ?? [];
		if (Left.Length != Right.Length) {
			return false;
		}
		for (var Index = 0; Index < Left.Length; Index += 1) {
			if (
				aLeft.KeyCompare(Left[Index].Key, Right[Index].Key) != 0 ||
				!aValuesEqual(Left[Index].Value, Right[Index].Value)
			) {
				return false;
			}
		}
		return true;
	}
}
