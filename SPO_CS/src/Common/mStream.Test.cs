//IMPORT mTest.cs
//IMPORT mStream.cs
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
mStream_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStream),
		mTest.Test(
			"tStream.Equals()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream<tInt32>(), mStream.Stream<tInt32>());
				mAssert.AssertEq(mStream.Stream(1), mStream.Stream(1));
				mAssert.AssertNotEq(mStream.Stream(1), mStream.Stream<tInt32>());
				mAssert.AssertNotEq(mStream.Stream(1), mStream.Stream(2));
				mAssert.AssertNotEq(mStream.Stream(1), mStream.Stream(1, 2));
				mAssert.AssertEq(
					mStream.Nat(1).Take(4),
					mStream.Stream(1, 2, 3, 4)
				);
			}
		),
		mTest.Test(
			"Concat()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Concat(mStream.Stream(1, 2), mStream.Stream(3, 4)), mStream.Stream(1, 2, 3, 4));
				mAssert.AssertEq(mStream.Concat(mStream.Stream(1, 2), mStream.Stream<tInt32>()), mStream.Stream(1, 2));
				mAssert.AssertEq(mStream.Concat(mStream.Stream<tInt32>(), mStream.Stream(3, 4)), mStream.Stream(3, 4));
				mAssert.AssertEq(mStream.Concat(mStream.Stream<tInt32>(), mStream.Stream<tInt32>()), mStream.Stream<tInt32>());
			}
		),
		mTest.Test(
			"Map()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream(1, 2, 3, 4).Map(a => a*a), mStream.Stream(1, 4, 9, 16));
				mAssert.AssertEq(mStream.Stream<tInt32>().Map(a => a*a), mStream.Stream<tInt32>());
			}
		),
		mTest.Test(
			"MapWithIndex()",
			aStreamOut => {
				mAssert.AssertEq(
					mStream.Stream(1, 2, 3, 4).MapWithIndex((i, a) => (i, a*a)),
					mStream.Stream((0, 1), (1, 4), (2, 9), (3, 16))
				);
			}
		),
		mTest.Test(
			"Reduce()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream(1, 2, 3, 4).Reduce(0, (a1, a2) => a1+a2), 10);
				mAssert.AssertEq(mStream.Stream(1).Reduce(0, (a1, a2) => a1+a2), 1);
				mAssert.AssertEq(mStream.Stream<tInt32>().Reduce(0, (a1, a2) => a1+a2), 0);
			}
		),
		mTest.Test(
			"Join()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream("a", "b", "c", "d").Join((a1, a2) => $"{a1},{a2}", ""), "a,b,c,d");
				mAssert.AssertEq(mStream.Stream("a").Join((a1, a2) => $"{a1},{a2}", ""), "a");
				mAssert.AssertEq(mStream.Stream<tText>().Join((a1, a2) => $"{a1},{a2}", ""), "");
			}
		),
		mTest.Test(
			"Take()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream(1, 2, 3, 4).Take(3), mStream.Stream(1, 2, 3));
				mAssert.AssertEq(mStream.Stream(1, 2, 3).Take(4), mStream.Stream(1, 2, 3));
				mAssert.AssertEq(mStream.Stream<tInt32>().Take(4), mStream.Stream<tInt32>());
				mAssert.AssertEq(mStream.Stream(1, 2, 3).Take(0), mStream.Stream<tInt32>());
				mAssert.AssertEq(mStream.Stream(1, 2, 3).Take(-1), mStream.Stream<tInt32>());
			}
		),
		mTest.Test(
			"Skip()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream(1, 2, 3, 4).Skip(3), mStream.Stream(4));
				mAssert.AssertEq(mStream.Stream(1, 2, 3).Skip(4), mStream.Stream<tInt32>());
				mAssert.AssertEq(mStream.Stream<tInt32>().Skip(4), mStream.Stream<tInt32>());
				mAssert.AssertEq(mStream.Stream(1, 2, 3).Skip(0), mStream.Stream(1, 2, 3));
			}
		),
		mTest.Test(
			"IsEmpty()",
			aStreamOut => {
				mAssert.Assert(mStream.Stream<tInt32>().IsEmpty());
				mAssert.AssertNot(mStream.Stream(1).IsEmpty());
				mAssert.AssertNot(mStream.Stream(1, 2).IsEmpty());
				
				mAssert.AssertNot(mStream.Stream<tInt32>() == new mStream.tStream<tInt32>());
			}
		),
		mTest.Test(
			"Any()",
			aStreamOut => {
				mAssert.AssertNot(mStream.Stream<tBool>().Any());
				mAssert.AssertNot(mStream.Stream(false).Any());
				mAssert.Assert(mStream.Stream(true).Any());
				mAssert.AssertNot(mStream.Stream(false, false, false).Any());
				mAssert.Assert(mStream.Stream(true, true, true).Any());
				mAssert.Assert(mStream.Stream(false, false, true, false).Any());
				mAssert.Assert(mStream.Stream(1, 2, 3, 4).Map(a => a == 2).Any());
				mAssert.AssertNot(mStream.Stream(1, 3, 4).Map(a => a == 2).Any());
			}
		),
		mTest.Test(
			"Every()",
			aStreamOut => {
				mAssert.AssertEq(mStream.Stream(1, 2, 3, 4, 5).Every(2), mStream.Stream(1, 3, 5));
				mAssert.AssertEq(mStream.Stream(1, 2).Every(2), mStream.Stream(1));
				mAssert.AssertEq(mStream.Stream<tInt32>().Every(2), mStream.Stream<tInt32>());
				mAssert.AssertEq(mStream.Stream(1, 2, 3).Every(1), mStream.Stream(1, 2, 3));
			}
		)
	);
	
	#if NUNIT
	[xTestCase("tStream.Equals()")]
	[xTestCase("Concat()")]
	[xTestCase("Map()")]
	[xTestCase("MapWithIndex()")]
	[xTestCase("Reduce()")]
	[xTestCase("Join()")]
	[xTestCase("Take()")]
	[xTestCase("Skip()")]
	[xTestCase("IsEmpty()")]
	[xTestCase("Any()")]
	[xTestCase("Every()")]
	public static void _(tText a) {
		mAssert.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
