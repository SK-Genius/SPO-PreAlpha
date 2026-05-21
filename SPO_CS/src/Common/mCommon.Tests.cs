#:include mStd.cs
#:include mTest.cs
#:include mAny.Tests.cs
#:include mFS.Tests.cs
#:include mLazy.Tests.cs
#:include mMaybe.Tests.cs
#:include mResult.Tests.cs
#:include mStream.Tests.cs
#:include mMap.Tests.cs
#:include mTreeMap.Tests.cs
#:include mArrayList.Tests.cs
#:include mParserGen.Tests.cs
#:include mTextParser.Tests.cs

public static class
mCommon_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"mCommon",
		[
			mAny_Tests.Tests,
			mMaybe_Tests.Tests,
			mResult_Tests.Tests,
			mLazy_Tests.Tests,
			mStream_Tests.Tests,
			mArrayList_Tests.Tests,
			mMap_Tests.Tests,
			mTreeMap_Tests.Tests,
			mParserGen_Tests.Tests,
			mFS_Tests.Tests,
			mTextParser_Tests.Tests,
		]
	);
}