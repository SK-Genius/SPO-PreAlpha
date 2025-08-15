// IMPORT mStd
// IMPORT mTest
// IMPORT mAny.Tests
// IMPORT mFS.Tests
// IMPORT mMaybe.Tests
// IMPORT mResult.Tests
// IMPORT mStream.Tests
// IMPORT mMap.Tests
// IMPORT mTreeMap.Tests
// IMPORT mArrayList.Tests
// IMPORT mParserGen.Tests
// IMPORT mLazy.Tests

public static class
mCommon_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"mCommon",
		[
			mAny_Tests.Tests,
			mMaybe_Tests.Tests,
			mResult_Tests.Tests,
			mStream_Tests.Tests,
			mArrayList_Tests.Tests,
			mMap_Tests.Tests,
			mTreeMap_Tests.Tests,
			mParserGen_Tests.Tests,
			mLazy_Tests.Tests,
			mFS_Tests.Tests,
			mTextParser_Tests.Tests,
		]
	);
}