//IMPORT mTest.cs
//IMPORT mVM_Type.cs

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
mVM_Type_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM_Type),
		mTest.Test(
			"BoolBool",
			aDebugStream => {
				mVM_Type.Unify(mVM_Type.Bool(), mVM_Type.Bool(), a => aDebugStream(a()));
			}
		),
		mTest.Test(
			"BoolInt",
			aDebugStream => {
				mStd.AssertError(() => mVM_Type.Unify(mVM_Type.Bool(), mVM_Type.Int(), a => aDebugStream(a())));
			}
		)
	);
	
	#if NUNIT
	[xTestCase("BoolBool")]
	[xTestCase("BoolInt")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
