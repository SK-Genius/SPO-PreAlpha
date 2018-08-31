//IMPORT mTest.cs
//IMPORT mTextParser.cs

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
public static class mTextParser_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mTextParser),
		mTest.Test(
			"TODO",
			aStreamOut => {
			}
		)
	);
	
	#if NUNIT
	[xTestCase("TODO")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
