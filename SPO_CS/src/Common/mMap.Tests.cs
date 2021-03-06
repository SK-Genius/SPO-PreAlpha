﻿//IMPORT mTest.cs
//IMPORT mMap.cs

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
mMap_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mMap),
		mTest.Test(
			"tMap.ForceGet",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AreEquals(TextToInt.ForceGet("one"), 1);
				mAssert.AreEquals(TextToInt.ForceGet("two"), 2);
				mAssert.ThrowsError(() => TextToInt.ForceGet("zero"));
			}
		),
		mTest.Test(
			"tMap.Get",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AreEquals(TextToInt.Get("one"), mMaybe.Some(1));
				mAssert.AreEquals(TextToInt.Get("two"), mMaybe.Some(2));
				mAssert.AreEquals(TextToInt.Get("zero"), mStd.cEmpty);
			}
		),
		mTest.Test(
			"tMap.Remove",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AreEquals(TextToInt.Get("one"), mMaybe.Some(1));
				mAssert.AreEquals(TextToInt.Get("two"), mMaybe.Some(2));
				TextToInt = TextToInt.Remove("one");
				mAssert.AreEquals(TextToInt.Get("one"), mStd.cEmpty);
				mAssert.AreEquals(TextToInt.Get("two"), mMaybe.Some(2));
			}
		)
	);
}
