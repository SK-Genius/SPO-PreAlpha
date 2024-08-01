public static class
mMap_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mMap),
		mTest.Test("tMap.Get",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AreEquals(TextToInt.TryGet("one"), 1);
				mAssert.AreEquals(TextToInt.TryGet("two"), 2);
				mAssert.AreEquals(TextToInt.TryGet("zero"), mStd.cEmpty);
			}
		),
		mTest.Test("tMap.Remove",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AreEquals(TextToInt.TryGet("one"), 1);
				mAssert.AreEquals(TextToInt.TryGet("two"), 2);
				TextToInt = TextToInt.Remove("one");
				mAssert.AreEquals(TextToInt.TryGet("one"), mStd.cEmpty);
				mAssert.AreEquals(TextToInt.TryGet("two"), 2);
			}
		)
	);
}
