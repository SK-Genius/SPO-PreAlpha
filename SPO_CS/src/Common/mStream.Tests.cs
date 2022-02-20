//IMPORT mTest.cs
//IMPORT mStream.cs
//IMPORT mAssert.cs

#nullable enable

public static class
mStream_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mStream),
		mTest.Test(
			"tStream.Equals()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream<tInt32>(), mStream.Stream<tInt32>());
				mAssert.AreEquals(mStream.Stream(1), mStream.Stream(1));
				mAssert.AreNotEquals(mStream.Stream(1), mStream.Stream<tInt32>());
				mAssert.AreNotEquals(mStream.Stream(1), mStream.Stream(2));
				mAssert.AreNotEquals(mStream.Stream(1), mStream.Stream(1, 2));
				mAssert.AreEquals(
					mStream.Nat(1).Take(4),
					mStream.Stream(1, 2, 3, 4)
				);
			}
		),
		mTest.Test(
			"Concat()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Concat(mStream.Stream(1, 2), mStream.Stream(3, 4)), mStream.Stream(1, 2, 3, 4));
				mAssert.AreEquals(mStream.Concat(mStream.Stream(1, 2), mStream.Stream<tInt32>()), mStream.Stream(1, 2));
				mAssert.AreEquals(mStream.Concat(mStream.Stream<tInt32>(), mStream.Stream(3, 4)), mStream.Stream(3, 4));
				mAssert.AreEquals(mStream.Concat(mStream.Stream<tInt32>(), mStream.Stream<tInt32>()), mStream.Stream<tInt32>());
			}
		),
		mTest.Test(
			"Map()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream(1, 2, 3, 4).Map(a => a*a), mStream.Stream(1, 4, 9, 16));
				mAssert.AreEquals(mStream.Stream<tInt32>().Map(a => a*a), mStream.Stream<tInt32>());
			}
		),
		mTest.Test(
			"MapWithIndex()",
			aStreamOut => {
				mAssert.AreEquals(
					mStream.Stream(1, 2, 3, 4).MapWithIndex((i, a) => (i, a*a)),
					mStream.Stream((0, 1), (1, 4), (2, 9), (3, 16))
				);
			}
		),
		mTest.Test(
			"Reduce()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream(1, 2, 3, 4).Reduce(0, (a1, a2) => a1+a2), 10);
				mAssert.AreEquals(mStream.Stream(1).Reduce(0, (a1, a2) => a1+a2), 1);
				mAssert.AreEquals(mStream.Stream<tInt32>().Reduce(0, (a1, a2) => a1+a2), 0);
			}
		),
		mTest.Test(
			"Join()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream("a", "b", "c", "d").Join((a1, a2) => $"{a1},{a2}", ""), "a,b,c,d");
				mAssert.AreEquals(mStream.Stream("a").Join((a1, a2) => $"{a1},{a2}", ""), "a");
				mAssert.AreEquals(mStream.Stream<tText>().Join((a1, a2) => $"{a1},{a2}", ""), "");
			}
		),
		mTest.Test(
			"Take()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream(1, 2, 3, 4).Take(3), mStream.Stream(1, 2, 3));
				mAssert.AreEquals(mStream.Stream(1, 2, 3).Take(4), mStream.Stream(1, 2, 3));
				mAssert.AreEquals(mStream.Stream<tInt32>().Take(4), mStream.Stream<tInt32>());
				mAssert.AreEquals(mStream.Stream(1, 2, 3).Take(0), mStream.Stream<tInt32>());
				mAssert.AreEquals(mStream.Stream(1, 2, 3).Take(-1), mStream.Stream<tInt32>());
			}
		),
		mTest.Test(
			"Skip()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream(1, 2, 3, 4).Skip(3), mStream.Stream(4));
				mAssert.AreEquals(mStream.Stream(1, 2, 3).Skip(4), mStream.Stream<tInt32>());
				mAssert.AreEquals(mStream.Stream<tInt32>().Skip(4), mStream.Stream<tInt32>());
				mAssert.AreEquals(mStream.Stream(1, 2, 3).Skip(0), mStream.Stream(1, 2, 3));
			}
		),
		mTest.Test(
			"IsEmpty()",
			aStreamOut => {
				mAssert.IsTrue(mStream.Stream<tInt32>().IsEmpty());
				mAssert.IsFalse(mStream.Stream(1).IsEmpty());
				mAssert.IsFalse(mStream.Stream(1, 2).IsEmpty());
			}
		),
		mTest.Test(
			"Any()",
			aStreamOut => {
				mAssert.IsFalse(mStream.Stream<tBool>().Any());
				mAssert.IsFalse(mStream.Stream(false).Any());
				mAssert.IsTrue(mStream.Stream(true).Any());
				mAssert.IsFalse(mStream.Stream(false, false, false).Any());
				mAssert.IsTrue(mStream.Stream(true, true, true).Any());
				mAssert.IsTrue(mStream.Stream(false, false, true, false).Any());
				mAssert.IsTrue(mStream.Stream(1, 2, 3, 4).Map(a => a == 2).Any());
				mAssert.IsFalse(mStream.Stream(1, 3, 4).Map(a => a == 2).Any());
			}
		),
		mTest.Test(
			"Every()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream(1, 2, 3, 4, 5).Every(2), mStream.Stream(1, 3, 5));
				mAssert.AreEquals(mStream.Stream(1, 2).Every(2), mStream.Stream(1));
				mAssert.AreEquals(mStream.Stream<tInt32>().Every(2), mStream.Stream<tInt32>());
				mAssert.AreEquals(mStream.Stream(1, 2, 3).Every(1), mStream.Stream(1, 2, 3));
			}
		),
		mTest.Test(
			"foreach",
			sStreamOut => {
				var Sum = 0;
				foreach (var Value in mStream.Nat(3).Take(3)) {
					Sum += Value;
				}
				mAssert.AreEquals(Sum, 3 + 4 + 5);
			}
		)
	);
}
