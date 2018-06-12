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

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class mStd_Test {
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tMaybe.Equals()",
			aStreamOut => {
				mStd.AssertEq<mStd.tMaybe<tInt32, tText>>(mStd.OK(1), mStd.OK(1));
				mStd.AssertEq<mStd.tMaybe<tText, tText>>(mStd.OK("1"), mStd.OK("1"));
				mStd.AssertEq<mStd.tMaybe<tInt32, tText>>(mStd.Fail("Bla"), mStd.Fail("Bla"));
			}
		),
		mTest.Test(
			"tVar.Equals()",
			aStreamOut => {
				mStd.AssertEq(mStd.Any(1), mStd.Any(1));
				mStd.AssertEq(mStd.Any("1"), mStd.Any("1"));
				mStd.AssertNotEq(mStd.Any(1), mStd.Any(2));
				mStd.AssertNotEq(mStd.Any(1), mStd.Any(false));
				mStd.AssertNotEq(mStd.Any("1"), mStd.Any("2"));
				mStd.AssertNotEq(mStd.Any("1"), mStd.Any(1));
			}
		)
	);
	
	[xArg("tMaybe.Equals()")]
	[xArg("tVar.Equals()")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
