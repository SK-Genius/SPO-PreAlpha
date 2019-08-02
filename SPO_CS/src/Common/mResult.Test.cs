//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mResult.cs
//IMPORT mAssert.cs

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
mResult_Test {

	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mResult),
		mTest.Test(
			"tResult.Equals()",
			aStreamOut => {
				mAssert.AssertEq<mResult.tResult<tInt32, tText>>(mResult.OK(1), mResult.OK(1));
				mAssert.AssertEq<mResult.tResult<tText, tText>>(mResult.OK("1"), mResult.OK("1"));
				mAssert.AssertEq<mResult.tResult<tInt32, tText>>(mResult.Fail("Bla"), mResult.Fail("Bla"));
			}
		)
	);

#if NUNIT
	[xTestCase("tResult.Equals()")]
	public static void _(tText a) {
		mAssert.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
#endif
}
