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
public static class mList_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mList),
		mTest.Test(
			"tList.Equals()",
			aStreamOut => {
				mStd.AssertEq(mList.List<tInt32>(), mList.List<tInt32>());
				mStd.AssertEq(mList.List(1), mList.List(1));
				mStd.AssertNotEq(mList.List(1), mList.List<tInt32>());
				mStd.AssertNotEq(mList.List(1), mList.List(2));
				mStd.AssertNotEq(mList.List(1), mList.List(1, 2));
				mStd.AssertEq(
					mList.Nat(1).Take(4),
					mList.List(1, 2, 3, 4)
				);
			}
		),
		mTest.Test(
			"Concat()",
			aStreamOut => {
				mStd.AssertEq(mList.Concat(mList.List(1, 2), mList.List(3, 4)), mList.List(1, 2, 3, 4));
				mStd.AssertEq(mList.Concat(mList.List(1, 2), mList.List<tInt32>()), mList.List(1, 2));
				mStd.AssertEq(mList.Concat(mList.List<tInt32>(), mList.List(3, 4)), mList.List(3, 4));
				mStd.AssertEq(mList.Concat(mList.List<tInt32>(), mList.List<tInt32>()), mList.List<tInt32>());
			}
		),
		mTest.Test(
			"Map()",
			aStreamOut => {
				mStd.AssertEq(mList.List(1, 2, 3, 4).Map(a => a*a), mList.List(1, 4, 9, 16));
				mStd.AssertEq(mList.List<tInt32>().Map(a => a*a), mList.List<tInt32>());
			}
		),
		mTest.Test(
			"MapWithIndex()",
			aStreamOut => {
				mStd.AssertEq(
					mList.List(1, 2, 3, 4).MapWithIndex((i, a) => (i, a*a)),
					mList.List((0, 1), (1, 4), (2, 9), (3, 16))
				);
			}
		),
		mTest.Test(
			"Reduce()",
			aStreamOut => {
				mStd.AssertEq(mList.List(1, 2, 3, 4).Reduce(0, (a1, a2) => a1+a2), 10);
				mStd.AssertEq(mList.List(1).Reduce(0, (a1, a2) => a1+a2), 1);
				mStd.AssertEq(mList.List<tInt32>().Reduce(0, (a1, a2) => a1+a2), 0);
			}
		),
		mTest.Test(
			"Join()",
			aStreamOut => {
				mStd.AssertEq(mList.List("a", "b", "c", "d").Join((a1, a2) => $"{a1},{a2}"), "a,b,c,d");
				mStd.AssertEq(mList.List("a").Join((a1, a2) => $"{a1},{a2}"), "a");
				mStd.AssertEq(mList.List<tText>().Join((a1, a2) => $"{a1},{a2}"), "");
			}
		),
		mTest.Test(
			"Take()",
			aStreamOut => {
				mStd.AssertEq(mList.List(1, 2, 3, 4).Take(3), mList.List(1, 2, 3));
				mStd.AssertEq(mList.List(1, 2, 3).Take(4), mList.List(1, 2, 3));
				mStd.AssertEq(mList.List<tInt32>().Take(4), mList.List<tInt32>());
				mStd.AssertEq(mList.List(1, 2, 3).Take(0), mList.List<tInt32>());
				mStd.AssertEq(mList.List(1, 2, 3).Take(-1), mList.List<tInt32>());
			}
		),
		mTest.Test(
			"Skip()",
			aStreamOut => {
				mStd.AssertEq(mList.List(1, 2, 3, 4).Skip(3), mList.List(4));
				mStd.AssertEq(mList.List(1, 2, 3).Skip(4), mList.List<tInt32>());
				mStd.AssertEq(mList.List<tInt32>().Skip(4), mList.List<tInt32>());
				mStd.AssertEq(mList.List(1, 2, 3).Skip(0), mList.List(1, 2, 3));
			}
		),
		mTest.Test(
			"IsEmpty()",
			aStreamOut => {
				mStd.Assert(mList.List<tInt32>().IsEmpty());
				mStd.AssertNot(mList.List(1).IsEmpty());
				mStd.AssertNot(mList.List(1, 2).IsEmpty());
			
				mStd.AssertNot(mList.List<tInt32>() == new mList.tList<int>());
			}
		),
		mTest.Test(
			"Any()",
			aStreamOut => {
				mStd.AssertNot(mList.List<tBool>().Any());
				mStd.AssertNot(mList.List(false).Any());
				mStd.Assert(mList.List(true).Any());
				mStd.AssertNot(mList.List(false, false, false).Any());
				mStd.Assert(mList.List(true, true, true).Any());
				mStd.Assert(mList.List(false, false, true, false).Any());
				mStd.Assert(mList.List(1, 2, 3, 4).Map(a => a == 2).Any());
				mStd.AssertNot(mList.List(1, 3, 4).Map(a => a == 2).Any());
			}
		),
		mTest.Test(
			"Every()",
			aStreamOut => {
				mStd.AssertEq(mList.List(1, 2, 3, 4, 5).Every(2), mList.List(1, 3, 5));
				mStd.AssertEq(mList.List(1, 2).Every(2), mList.List(1));
				mStd.AssertEq(mList.List<tInt32>().Every(2), mList.List<tInt32>());
				mStd.AssertEq(mList.List(1, 2, 3).Every(1), mList.List(1, 2, 3));
			}
		)
	);
	
	[xTestCase("tList.Equals()")]
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
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
