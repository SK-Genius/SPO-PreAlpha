public static class
mResult_Tests {

	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mResult),
		mTest.Test(
			"tResult.Equals()",
			aStreamOut => {
				mAssert.AreEquals<mResult.tResult<tInt32, tText>>(mResult.OK(1), mResult.OK(1));
				mAssert.AreEquals<mResult.tResult<tText, tText>>(mResult.OK("1"), mResult.OK("1"));
				mAssert.AreEquals<mResult.tResult<tInt32, tText>>(mResult.Fail("Bla"), mResult.Fail("Bla"));
			}
		)
	);
}
