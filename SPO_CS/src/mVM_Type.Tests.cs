//IMPORT mTest.cs
//IMPORT mVM_Type.cs

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
mVM_Type_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM_Type),
		mTest.Test(
			"BoolBool",
			aDebugStream => {
				mAssert.IsTrue(mVM_Type.Unify(mVM_Type.Bool(), mVM_Type.Bool(), a => aDebugStream(a())));
			}
		),
		mTest.Test(
			"BoolInt",
			aDebugStream => {
				mAssert.IsFalse(mVM_Type.Unify(mVM_Type.Bool(), mVM_Type.Int(), a => aDebugStream(a())));
			}
		)
	);
}
