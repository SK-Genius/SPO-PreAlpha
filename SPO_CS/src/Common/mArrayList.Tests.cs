//IMPORT mTest.cs
//IMPORT mArrayList.cs

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
mArrayList_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mArrayList),
		mTest.Test(
			"tArrayList.IsEmpty(...)",
			aStreamOut => {
				mAssert.IsTrue(mArrayList.List<tInt32>().IsEmpty());
				mAssert.IsFalse(mArrayList.List(1).IsEmpty());
			}
		),
		mTest.Test(
			"tArrayList.Equals(...)",
			aStreamOut => {
				mAssert.AreEquals(mArrayList.List<tInt32>(), mArrayList.List<tInt32>());
				mAssert.AreNotEquals(mArrayList.List<tInt32>(), mArrayList.List(1));
				mAssert.AreEquals(mArrayList.List(1), mArrayList.List(1));
				mAssert.AreNotEquals(mArrayList.List(1), mArrayList.List(2));
				mAssert.AreNotEquals(mArrayList.List(1), mArrayList.List(1, 2));
				mAssert.AreEquals(mArrayList.List(3, 32, 5), mArrayList.List(3, 32, 5));
			}
		),
		mTest.Test(
			"tArrayList.ToArrayList()",
			aStreamOut => {
				mAssert.AreEquals(mStream.Stream(1, 2, 3).ToArrayList(), mArrayList.List(1, 2, 3));
				mAssert.AreEquals(mStream.Stream<tInt32>().ToArrayList(), mArrayList.List<tInt32>());
			}
		),
		mTest.Test(
			"tArrayList.ToLasyList()",
			aStreamOut => {
				mAssert.AreEquals(mArrayList.List<tInt32>().ToStream(), mStream.Stream<tInt32>());
				mAssert.AreEquals(mArrayList.List(1).ToStream(), mStream.Stream(1));
				mAssert.AreEquals(mArrayList.List(1, 2, 3).ToStream(), mStream.Stream(1, 2, 3));
			}
		),
		mTest.Test(
			"tArrayList.Push(...)",
			aStreamOut => {
				mAssert.AreEquals(mArrayList.List<tInt32>().Push(1).Push(2), mArrayList.List(1, 2));
				mAssert.AreEquals(mArrayList.List(1, 2).Push(3).Push(4), mArrayList.List(1, 2, 3, 4));
				mAssert.AreEquals(mArrayList.List(1, 2, 3, 4, 5, 6, 7, 8).Push(9), mArrayList.List(1, 2, 3, 4, 5, 6, 7, 8, 9));
			}
		),
		mTest.Test(
			"tArrayList.Pop()",
			aStreamOut => {
				{
					var L = mArrayList.List(1, 2, 3);
					mAssert.AreEquals(L.Pop(), 3);
					mAssert.AreEquals(L.Pop(), 2);
					mAssert.AreEquals(L, mArrayList.List(1));
				}
				{
					var L = mArrayList.List(1, 2, 3);
					mAssert.AreEquals(L.Pop(out var X).Pop(out var Y), mArrayList.List(1));
					mAssert.AreEquals(X, 3);
					mAssert.AreEquals(Y, 2);
					mAssert.AreEquals(L.Pop(out X), mArrayList.List<tInt32>());
					mAssert.AreEquals(X, 1);
				}
			}
		),
		mTest.Test(
			"tArrayList.Resize(...)",
			aStreamOut => {
				var L = mArrayList.List(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
				var Slots = 12;
				mAssert.AreEquals(L._Items.Length, Slots);
				
				L.Push(13);
				Slots += Slots / 2;
				mAssert.AreEquals(L._Items.Length, Slots);
				
				tInt32 _;
				while (L.Size() > Slots/2) {
					L.Pop(out _);
				}
				mAssert.AreEquals(L._Items.Length, Slots);
				
				L.Pop(out _);
				Slots = L.Size() * 3 / 2;
				mAssert.AreEquals(L._Items.Length, Slots);
			}
		),
		mTest.Test(
			"tArrayList.Get(...)",
			aStreamOut => {
				mAssert.AreEquals(mArrayList.List(10, 11, 12).Get(1), 11);
			}
		),
		mTest.Test(
			"tArrayList.Set(...)",
			aStreamOut => {
				var L = mArrayList.List(10, 11, 12, 13);
				L.Set(1, 21);
				mAssert.AreEquals(L, mArrayList.List(10, 21, 12, 13));
			}
		),
		mTest.Test(
			"mArrayList.Concat(...)",
			aStreamOut => {
				mAssert.AreEquals(mArrayList.Concat(mArrayList.List<tInt32>(), mArrayList.List<tInt32>()), mArrayList.List<tInt32>());
				mAssert.AreEquals(mArrayList.Concat(mArrayList.List(1, 2), mArrayList.List<tInt32>()), mArrayList.List(1, 2));
				mAssert.AreEquals(mArrayList.Concat(mArrayList.List<tInt32>(), mArrayList.List(1, 2)), mArrayList.List(1, 2));
				mAssert.AreEquals(mArrayList.Concat(mArrayList.List(1, 2), mArrayList.List(3, 4, 5)), mArrayList.List(1, 2, 3, 4, 5));
			}
		)
	);
	
	#if NUNIT
	[xTestCase("tArrayList.IsEmpty(...)")]
	[xTestCase("tArrayList.Equals(...)")]
	[xTestCase("tArrayList.ToArrayList()")]
	[xTestCase("tArrayList.ToLasyList()")]
	[xTestCase("tArrayList.Push(...)")]
	[xTestCase("tArrayList.Pop()")]
	[xTestCase("tArrayList.Resize(...)")]
	[xTestCase("tArrayList.Get(...)")]
	[xTestCase("tArrayList.Set(...)")]
	[xTestCase("mArrayList.Concat(...)")]
	public static void _(tText a) {
		mAssert.AreEquals(
			Tests.Run(System.Console.WriteLine, mStream.Stream(a)).Result,
			mTest.tResult.OK
		);
	}
	#endif
}
