//IMPORT mTest.cs
//IMPORT mVM_Data.cs

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
public static class mVM_Data_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM_Data),
		mTest.Test(
			"TODO: ExternDef",
			aDebugStream => {
				// TODO: Tests
			}
		)
	);
	
	#if NUNIT
	[xTestCase("TODO: ExternDef")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
