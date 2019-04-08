//IMPORT mTest.cs
//IMPORT mMap.cs

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
mMap_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mMap),
		mTest.Test(
			"tMap.ForceGet",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AssertEq(TextToInt.ForceGet("one"), 1);
				mAssert.AssertEq(TextToInt.ForceGet("two"), 2);
				mAssert.AssertError(() => TextToInt.ForceGet("zero"));
			}
		),
		mTest.Test(
			"tMap.Get",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AssertEq(TextToInt.Get("one"), 1);
				mAssert.AssertEq(TextToInt.Get("two"), 2);
				mAssert.AssertEq(TextToInt.Get("zero"), mStd.cEmpty);
			}
		),
		mTest.Test(
			"tMap.Remove",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mAssert.AssertEq(TextToInt.Get("one"), 1);
				mAssert.AssertEq(TextToInt.Get("two"), 2);
				TextToInt = TextToInt.Remove("one");
				mAssert.AssertEq(TextToInt.Get("one"), mStd.cEmpty);
				mAssert.AssertEq(TextToInt.Get("two"), 2);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("tMap.ForceGet")]
	[xTestCase("tMap.Get")]
	[xTestCase("tMap.Remove")]
	public static void _(tText a) {
		mAssert.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
