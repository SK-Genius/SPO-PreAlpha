//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mAssert.cs
//IMPORT mAny.cs

#nullable enable

public static class
mAny_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mAny),
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
