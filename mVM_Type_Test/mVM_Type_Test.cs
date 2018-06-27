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

using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
public static class mVM_Type_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM_Type),
		mTest.Test(
			"BoolBool",
			aDebugStream => {
				mVM_Type.Unify(mVM_Type.Bool(), mVM_Type.Bool(), aDebugStream);
			}
		),
		mTest.Test(
			"BoolInt",
			aDebugStream => {
				mStd.AssertError(() => mVM_Type.Unify(mVM_Type.Bool(), mVM_Type.Int(), aDebugStream));
			}
		)
	);
	
	[xTestCase("BoolBool")]
	[xTestCase("BoolInt")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
