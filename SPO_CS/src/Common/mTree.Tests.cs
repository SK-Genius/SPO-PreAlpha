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
mTree_Tests {
	private static readonly mStd.tFunc<tInt32, tInt32, tInt32>
	Int32Compare = (a1, a2) => mMath.Sign(a1 - a2);

	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mTokenizer),
		mTest.Test(
			"Create",
			aDebugStream => {
				{
					var Tree = mTree.Tree<tInt32, tText>(Int32Compare);
					mAssert.AreEquals(Tree.Deep(), 0);
					mAssert.ThrowsError(() => { Tree.ForceGet(0); });
				}
				{
					var Tree = mTree.Tree(
						Int32Compare,
						(1, "bla")
					);
					mAssert.AreEquals(Tree.Deep(), 1);
					mAssert.AreEquals(Tree.ForceGet(1), "bla");
					mAssert.ThrowsError(() => { Tree.ForceGet(0); });
				}
				{
					var Tree = mTree.Tree(
						Int32Compare,
						(1, "bla"),
						(2, "blub")
					);
					mAssert.AreEquals(Tree.Deep(), 2);
					mAssert.ThrowsError(() => { Tree.ForceGet(0); });
					mAssert.AreEquals(Tree.ForceGet(1), "bla");
					mAssert.AreEquals(Tree.ForceGet(2), "blub");
				}
			}
		),
		mTest.Test(
			"Remove",
			aDebugStream => {
				{
					var Tree = mTree.Tree<tInt32, tText>(Int32Compare)
						.Set(1, "bla")
						.Set(3, "foo")
						.Set(2, "blub")
						.Remove(3);
					mAssert.ThrowsError(() => { Tree.ForceGet(0); });
					mAssert.AreEquals(Tree.ForceGet(1), "bla");
					mAssert.AreEquals(Tree.ForceGet(2), "blub");
					mAssert.ThrowsError(() => { Tree.ForceGet(3); });
				}
			}
		),
		mTest.Test(
			"Big",
			aDebugStream => {
				{
					var Tree = mTree.Tree<tInt32, tText>(Int32Compare)
						.Set(0, "_0")
						.Set(1, "_1")
						.Set(2, "_2")
						.Set(3, "_3")
						.Set(4, "_4")
						.Set(5, "_5")
						.Set(6, "_6")
						.Set(7, "_7")
						.Set(8, "_8")
						.Set(9, "_9");
					mAssert.AreEquals(Tree.ForceGet(0), "_0");
					mAssert.AreEquals(Tree.ForceGet(1), "_1");
					mAssert.AreEquals(Tree.ForceGet(2), "_2");
					mAssert.AreEquals(Tree.ForceGet(3), "_3");
					mAssert.AreEquals(Tree.ForceGet(4), "_4");
					mAssert.AreEquals(Tree.ForceGet(5), "_5");
					mAssert.AreEquals(Tree.ForceGet(6), "_6");
					mAssert.AreEquals(Tree.ForceGet(7), "_7");
					mAssert.AreEquals(Tree.ForceGet(8), "_8");
					mAssert.AreEquals(Tree.ForceGet(9), "_9");
					mAssert.ThrowsError(() => { Tree.ForceGet(20); });

					Tree = Tree.Remove(3);
					Tree = Tree.Remove(7);
					Tree = Tree.Remove(1);
					Tree = Tree.Remove(9);
					Tree = Tree.Remove(5);

					mAssert.AreEquals(Tree.ForceGet(0), "_0");
					mAssert.AreEquals(Tree.ForceGet(2), "_2");
					mAssert.AreEquals(Tree.ForceGet(4), "_4");
					mAssert.AreEquals(Tree.ForceGet(6), "_6");
					mAssert.AreEquals(Tree.ForceGet(8), "_8");
				}
			}
		)
	);

#if NUNIT
	[xTestCase("Create")]
	[xTestCase("Remove")]
	[xTestCase("Big")]
	public static void _(tText a) {
		mAssert.AreEquals(
			Tests.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
