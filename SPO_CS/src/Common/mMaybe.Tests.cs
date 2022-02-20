//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mResult.cs
//IMPORT mAssert.cs
//IMPORT mMaybe.cs

#nullable enable

public static class
mMaybe_Tests {

	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tMaybe.Equals()",
			aStreamOut => {
				mAssert.AreEquals(mMaybe.Some(1), 1);
				mAssert.AreNotEquals(mMaybe.Some("1"), "2");
				mAssert.AreEquals<mMaybe.tMaybe<tInt32>>(mStd.cEmpty, mStd.cEmpty);
				mAssert.AreNotEquals(mStd.cEmpty, mMaybe.Some(3));
				mAssert.AreEquals<mMaybe.tMaybe<tInt32>>(default, mStd.cEmpty);
			}
		)
	);
}
