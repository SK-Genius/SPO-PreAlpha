#nullable enable

public static class
mTreeMap_Tests {
	private static readonly mStd.tFunc<tInt32, tInt32, tInt32>
	Int32Compare = (a1, a2) => mMath.Sign(a1 - a2);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mTokenizer),
		mTest.Test(
			"Create",
			aDebugStream => {
				{
					var Tree = mTreeMap.Tree<tInt32, tText>(Int32Compare);
					mAssert.AreEquals(Tree.Deep(), 0);
					mAssert.AreEquals(Tree.TryGet(0), mStd.cEmpty);
				}
				{
					var Tree = mTreeMap.Tree(
						Int32Compare,
						(1, "bla")
					);
					mAssert.AreEquals(Tree.Deep(), 1);
					mAssert.AreEquals(Tree.TryGet(1), "bla");
					mAssert.AreEquals(Tree.TryGet(0), mStd.cEmpty);
				}
				{
					var Tree = mTreeMap.Tree(
						Int32Compare,
						(1, "bla"),
						(2, "blub")
					);
					mAssert.AreEquals(Tree.Deep(), 2);
					mAssert.AreEquals(Tree.TryGet(0), mStd.cEmpty);
					mAssert.AreEquals(Tree.TryGet(1), "bla");
					mAssert.AreEquals(Tree.TryGet(2), "blub");
				}
			}
		),
		mTest.Test(
			"Remove",
			aDebugStream => {
				{
					var Tree = mTreeMap.Tree<tInt32, tText>(Int32Compare)
						.Set(1, "bla")
						.Set(3, "foo")
						.Set(2, "blub")
						.Remove(3);
					mAssert.AreEquals(Tree.TryGet(0), mStd.cEmpty);
					mAssert.AreEquals(Tree.TryGet(1), "bla");
					mAssert.AreEquals(Tree.TryGet(2), "blub");
					mAssert.AreEquals(Tree.TryGet(3), mStd.cEmpty);
				}
			}
		),
		mTest.Test(
			"Big",
			aDebugStream => {
				{
					var Tree = mTreeMap.Tree<tInt32, tText>(Int32Compare)
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
					mAssert.AreEquals(Tree.TryGet(0), "_0");
					mAssert.AreEquals(Tree.TryGet(1), "_1");
					mAssert.AreEquals(Tree.TryGet(2), "_2");
					mAssert.AreEquals(Tree.TryGet(3), "_3");
					mAssert.AreEquals(Tree.TryGet(4), "_4");
					mAssert.AreEquals(Tree.TryGet(5), "_5");
					mAssert.AreEquals(Tree.TryGet(6), "_6");
					mAssert.AreEquals(Tree.TryGet(7), "_7");
					mAssert.AreEquals(Tree.TryGet(8), "_8");
					mAssert.AreEquals(Tree.TryGet(9), "_9");
					mAssert.AreEquals(Tree.TryGet(20), mStd.cEmpty);
					
					Tree = Tree.Remove(3);
					Tree = Tree.Remove(7);
					Tree = Tree.Remove(1);
					Tree = Tree.Remove(9);
					Tree = Tree.Remove(5);
					
					mAssert.AreEquals(Tree.TryGet(0), "_0");
					mAssert.AreEquals(Tree.TryGet(2), "_2");
					mAssert.AreEquals(Tree.TryGet(4), "_4");
					mAssert.AreEquals(Tree.TryGet(6), "_6");
					mAssert.AreEquals(Tree.TryGet(8), "_8");
				}
			}
		)
	);
}
