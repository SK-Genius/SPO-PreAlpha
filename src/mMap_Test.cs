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
public static class mMap_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mMap),
		mTest.Test(
			"tMap.Get",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mStd.AssertEq(TextToInt.Get("one"), 1);
				mStd.AssertEq(TextToInt.Get("two"), 2);
			}
		),
		mTest.Test(
			"tMap.TryGet",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mStd.Assert(TextToInt.TryGet("one", out var Num));
				mStd.AssertEq(Num, 1);
				mStd.Assert(TextToInt.TryGet("two", out Num));
				mStd.AssertEq(Num, 2);
				mStd.AssertNot(TextToInt.TryGet("zero", out Num));
			}
		),
		mTest.Test(
			"tMap.Remove",
			aStreamOut => {
				var TextToInt = mMap.Map<tText, tInt32>((a1, a2) => a1 == a2)
				.Set("one", 1)
				.Set("two", 2);
				mStd.Assert(TextToInt.TryGet("one", out var Num));
				mStd.AssertEq(Num, 1);
				mStd.Assert(TextToInt.TryGet("two", out Num));
				mStd.AssertEq(Num, 2);
				TextToInt = TextToInt.Remove("one");
				mStd.AssertNot(TextToInt.TryGet("one", out Num));
				mStd.Assert(TextToInt.TryGet("two", out Num));
				mStd.AssertEq(Num, 2);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("tMap.Get")]
	[xTestCase("tMap.TryGet")]
	[xTestCase("tMap.Remove")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
