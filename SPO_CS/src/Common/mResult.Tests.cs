#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mTest.cs
#:ref mAssert.cs
#:ref mResult.cs

public static class
mResult_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mResult),
		[
			mTest.Test("tResult.Equals()",
				aStreamOut => {
					mAssert.AreEquals<mResult.tResult<tInt32, tText>>(mResult.OK(1), 1);
					mAssert.AreEquals<mResult.tResult<tText, tText>>(mResult.OK("1"), "1");
					mAssert.AreEquals<mResult.tResult<tInt32, tText>>(mResult.Fail("Bla"), mResult.Fail("Bla"));
				}
			)
		]
	);
}
