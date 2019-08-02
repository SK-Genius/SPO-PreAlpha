//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mAssert.cs
//IMPORT mAny.cs

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

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class
mAny_Test {

	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tAny.Equals()",
			aStreamOut => {
				mAssert.AssertEq(mAny.Any(1), mAny.Any(1));
				mAssert.AssertEq(mAny.Any("1"), mAny.Any("1"));
				mAssert.AssertNotEq(mAny.Any(1), mAny.Any(2));
				mAssert.AssertNotEq(mAny.Any(1), mAny.Any(false));
				mAssert.AssertNotEq(mAny.Any("1"), mAny.Any("2"));
				mAssert.AssertNotEq(mAny.Any("1"), mAny.Any(1));
			}
		)
	);

#if NUNIT
	[xTestCase("tAny.Equals()")]
	public static void _(tText a) {
		mAssert.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
#endif
}
