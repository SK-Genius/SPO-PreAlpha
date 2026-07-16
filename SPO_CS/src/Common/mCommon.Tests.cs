#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mTest.cs
#:ref mAny.Tests.cs
#:ref mFS.Tests.cs
#:ref mLazy.Tests.cs
#:ref mMaybe.Tests.cs
#:ref mResult.Tests.cs
#:ref mStream.Tests.cs
#:ref mMap.Tests.cs
#:ref mTreeMap.Tests.cs
#:ref mArrayList.Tests.cs
#:ref mParserGen.Tests.cs
#:ref mTextParser.Tests.cs

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