//IMPORT mTest.cs
//IMPORT mStd.cs
//IMPORT mResult.cs
//IMPORT mAssert.cs
//IMPORT mMaybe.cs

#nullable enable

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

public static class
mMaybe_Tests {

	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tMaybe.Equals()",
			aStreamOut => {
				mAssert.AreEquals(mMaybe.Some(1), mMaybe.Some(1));
				mAssert.AreNotEquals(mMaybe.Some("1"), mMaybe.Some("2"));
				mAssert.AreEquals<mMaybe.tMaybe<tInt32>>(mStd.cEmpty, mStd.cEmpty);
				mAssert.AreNotEquals(mStd.cEmpty, mMaybe.Some(3));
				mAssert.AreEquals<mMaybe.tMaybe<tInt32>>(default, mStd.cEmpty);
			}
		)
	);
}
