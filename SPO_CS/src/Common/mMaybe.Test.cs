//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mResult.cs
//IMPORT mAssert.cs
//IMPORT mMaybe.cs

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
mMaybe_Test {

	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tMaybe.Equals()",
			aStreamOut => {
				mAssert.Equals(mMaybe.Some(1), mMaybe.Some(1));
				mAssert.NotEquals(mMaybe.Some("1"), mMaybe.Some("2"));
				mAssert.Equals<mMaybe.tMaybe<tInt32>>(mStd.cEmpty, mStd.cEmpty);
				mAssert.NotEquals(mStd.cEmpty, mMaybe.Some(3));
				mAssert.Equals<mMaybe.tMaybe<tInt32>>(default, mStd.cEmpty);
			}
		)
	);
	
#if NUNIT
	[xTestCase("tMaybe.Equals()")]
	public static void _(tText a) {
		mAssert.Equals(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
#endif
}
