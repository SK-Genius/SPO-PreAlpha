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
public static class mArrayList_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mArrayList),
		mTest.Test(
			"tArrayList.IsEmpty(...)",
			aStreamOut => {
				mStd.Assert(mArrayList.List<tInt32>().IsEmpty());
				mStd.AssertNot(mArrayList.List(1).IsEmpty());
			}
		),
		mTest.Test(
			"tArrayList.Equals(...)",
			aStreamOut => {
				mStd.AssertEq(mArrayList.List<tInt32>(), mArrayList.List<tInt32>());
				mStd.AssertNotEq(mArrayList.List<tInt32>(), mArrayList.List(1));
				mStd.AssertEq(mArrayList.List(1), mArrayList.List(1));
				mStd.AssertNotEq(mArrayList.List(1), mArrayList.List(2));
				mStd.AssertNotEq(mArrayList.List(1), mArrayList.List(1, 2));
				mStd.AssertEq(mArrayList.List(3, 32, 5), mArrayList.List(3, 32, 5));
			}
		),
		mTest.Test(
			"tArrayList.ToArrayList()",
			aStreamOut => {
				mStd.AssertEq(mList.List(1, 2, 3).ToArrayList(), mArrayList.List(1, 2, 3));
				mStd.AssertEq(mList.List<tInt32>().ToArrayList(), mArrayList.List<tInt32>());
			}
		),
		mTest.Test(
			"tArrayList.ToLasyList()",
			aStreamOut => {
				mStd.AssertEq(mArrayList.List<tInt32>().ToLasyList(), mList.List<tInt32>());
				mStd.AssertEq(mArrayList.List(1).ToLasyList(), mList.List(1));
				mStd.AssertEq(mArrayList.List(1, 2, 3).ToLasyList(), mList.List(1, 2, 3));
			}
		),
		mTest.Test(
			"tArrayList.Push(...)",
			aStreamOut => {
				mStd.AssertEq(mArrayList.List<tInt32>().Push(1).Push(2), mArrayList.List(1, 2));
				mStd.AssertEq(mArrayList.List(1, 2).Push(3).Push(4), mArrayList.List(1, 2, 3, 4));
				mStd.AssertEq(mArrayList.List(1, 2, 3, 4, 5, 6, 7, 8).Push(9), mArrayList.List(1, 2, 3, 4, 5, 6, 7, 8, 9));
			}
		),
		mTest.Test(
			"tArrayList.Pop()",
			aStreamOut => {
				{
					var L = mArrayList.List(1, 2, 3);
					mStd.AssertEq(L.Pop(), 3);
					mStd.AssertEq(L.Pop(), 2);
					mStd.AssertEq(L, mArrayList.List(1));
				}
				{
					var L = mArrayList.List(1, 2, 3);
					mStd.AssertEq(L.Pop(out var X).Pop(out var Y), mArrayList.List(1));
					mStd.AssertEq(X, 3);
					mStd.AssertEq(Y, 2);
					mStd.AssertEq(L.Pop(out X), mArrayList.List<tInt32>());
					mStd.AssertEq(X, 1);
				}
			}
		),
		mTest.Test(
			"tArrayList.Resize(...)",
			aStreamOut => {
				var L = mArrayList.List(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
				var Slots = 12;
				mStd.AssertEq(L._Items.Length, Slots);
				
				L.Push(13);
				Slots += Slots / 2;
				mStd.AssertEq(L._Items.Length, Slots);
				
				tInt32 _;
				while (L.Size() > Slots/2) {
					L.Pop(out _);
				}
				mStd.AssertEq(L._Items.Length, Slots);
				
				L.Pop(out _);
				Slots = L.Size() * 3 / 2;
				mStd.AssertEq(L._Items.Length, Slots);
			}
		),
		mTest.Test(
			"tArrayList.Get(...)",
			aStreamOut => {
				mStd.AssertEq(mArrayList.List(10, 11, 12).Get(1), 11);
			}
		),
		mTest.Test(
			"tArrayList.Set(...)",
			aStreamOut => {
				var L = mArrayList.List(10, 11, 12, 13);
				L.Set(1, 21);
				mStd.AssertEq(L, mArrayList.List(10, 21, 12, 13));
			}
		),
		mTest.Test(
			"mArrayList.Concat(...)",
			aStreamOut => {
				mStd.AssertEq(mArrayList.Concat(mArrayList.List<tInt32>(), mArrayList.List<tInt32>()), mArrayList.List<tInt32>());
				mStd.AssertEq(mArrayList.Concat(mArrayList.List(1, 2), mArrayList.List<tInt32>()), mArrayList.List(1, 2));
				mStd.AssertEq(mArrayList.Concat(mArrayList.List<tInt32>(), mArrayList.List(1, 2)), mArrayList.List(1, 2));
				mStd.AssertEq(mArrayList.Concat(mArrayList.List(1, 2), mArrayList.List(3, 4, 5)), mArrayList.List(1, 2, 3, 4, 5));
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
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
