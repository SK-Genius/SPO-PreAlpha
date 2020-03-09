//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mAssert.cs
//IMPORT mAny.cs

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
mAny_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tAny.Equals()",
			aStreamOut => {
				mAssert.AreEquals(mAny.Any(1), mAny.Any(1));
				mAssert.AreEquals(mAny.Any("1"), mAny.Any("1"));
				mAssert.AreNotEquals(mAny.Any(1), mAny.Any(2));
				mAssert.AreNotEquals(mAny.Any(1), mAny.Any(false));
				mAssert.AreNotEquals(mAny.Any("1"), mAny.Any("2"));
				mAssert.AreNotEquals(mAny.Any("1"), mAny.Any(1));
			}
		)
	);
}
