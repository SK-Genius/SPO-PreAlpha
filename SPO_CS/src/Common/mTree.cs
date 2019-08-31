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

public static class
mTree {
	
	[System.Diagnostics.DebuggerTypeProxy(typeof(mTree.tTree<,>.tDebuggerProxy))]
	public class
	tTree<tKey, tValue> {
		internal mStd.tFunc<tInt32, tKey, tKey> KeyCompare;
		internal tNode<tKey, tValue> Root;

		private struct tDebuggerProxy {
			private readonly tTree<tKey, tValue> _Tree;
			public tDebuggerProxy(tTree<tKey, tValue> a) { this._Tree = a; }
			[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
			public (tKey Key, tValue Value)[] List => this._Tree.ToStream().Take(100).ToArrayList().ToArray();
		}
	}
	
	internal class
	tNode<tKey, tValue> {
		internal tKey Key;
		internal tValue Value;
		internal tInt32 Deep;
		internal tNode<tKey, tValue> SubTree1;
		internal tNode<tKey, tValue> SubTree2;
	}
	
	internal static tInt32
	Deep<tKey, tValue>(
		this tNode<tKey, tValue> a
	) {
		return a?.Deep ?? 0;
	}
	
	internal static tInt32
	Deep<tKey, tValue>(
		this tTree<tKey, tValue> a
	) {
		return a.Root.Deep();
	}
	
	public static tTree<tKey, tValue>
	Tree<tKey, tValue>(
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare,
		params (tKey Key, tValue Value)[] aItems
	) {
		var Tree = new tTree<tKey, tValue> {
			KeyCompare = aKeyCompare,
		};
		foreach (var Item in aItems) {
			Tree = Tree.Set(Item.Key, Item.Value);
		}
		return Tree;
	}
	
	public static tTree<tKey, tValue>
	Set<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey,
		tValue aValue
	) {
		return new tTree<tKey, tValue> {
			KeyCompare = aTree.KeyCompare,
			Root = aTree.Root.Add(aKey, aValue, aTree.KeyCompare),
		};
	}
	
	internal static tNode<tKey, tValue>
	Add<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		tKey aKey,
		tValue aValue,
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare
	) {
		if (aNode is null) {
			return Node(aKey, aValue, null, null);
		}
		
		switch (aKeyCompare(aNode.Key, aKey)) {
			case 0: {
				return Node(
					aKey,
					aValue,
					aNode.SubTree1,
					aNode.SubTree2
				);
			}
			case 1: {
				return Node(
					aNode.Key,
					aNode.Value,
					aNode.SubTree1.Add(aKey, aValue, aKeyCompare),
					aNode.SubTree2
				).Balance();
			}
			case -1: {
				return Node(
					aNode.Key,
					aNode.Value,
					aNode.SubTree1,
					aNode.SubTree2.Add(aKey, aValue, aKeyCompare)
				).Balance();
			}
			default: {
				throw mError.Error("impossible");
			}
		}
	}
	
	public static tValue
	ForceGet<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey
	) {
		return aTree.Root.Get(aKey, aTree.KeyCompare);
	}
	
	internal static tValue
	Get<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		tKey aKey,
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare
	) {
		mAssert.NotNull(aNode, () => "unknown key: " + aKey);
		return mStd.Switch(
			aKeyCompare(aKey, aNode.Key),
			(0, _ => aNode.Value),
			(1, _ => aNode.SubTree2.Get(aKey, aKeyCompare)),
			(-1, _ => aNode.SubTree1.Get(aKey, aKeyCompare))
		);
	}
	
	public static tTree<tKey, tValue>
	Remove<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey
	) {
		return new tTree<tKey, tValue> {
			Root = aTree.Root.Remove(aKey, aTree.KeyCompare),
			KeyCompare = aTree.KeyCompare,
		};
	}
	
	internal static tNode<tKey, tValue>
	Remove<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		tKey aKey,
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare
	) {
		mAssert.NotEquals(aNode, null);
		
		var SubTree1 = aNode.SubTree1;
		var SubTree2 = aNode.SubTree2;
		var Key = aNode.Key;
		var Value = aNode.Value;
		
		if (SubTree1 is null && SubTree2 is null) {
			mAssert.Equals(aKey, Key);
			return null;
		}
		
		switch (aKeyCompare(aKey, Key)) {
			case 0: {
				
				if (SubTree1.Deep() > SubTree2.Deep()) {
					SubTree1 = SubTree1.RemoveMax(out Key, out Value);
				} else {
					SubTree2 = SubTree2.RemoveMin(out Key, out Value);
				}
				break;
			}
			case -1: {
				SubTree1 = aNode.SubTree1.Remove(aKey, aKeyCompare);
				break;
			}
			case 1: {
				SubTree2 = aNode.SubTree2.Remove(aKey, aKeyCompare);
				break;
			}
		}
		
		return Node(Key, Value, SubTree1, SubTree2).Balance();
	}
	
	internal static tNode<tKey, tValue>
	RemoveMin<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		out tKey aKey,
		out tValue aValue
	) {
		if (aNode.SubTree1 is null) {
			aKey = aNode.Key;
			aValue = aNode.Value;
			return aNode.SubTree2;
		} else {
			var SubTree1 = aNode.SubTree1.RemoveMin(
				out aKey,
				out aValue
			);
			return Node(
				aNode.Key,
				aNode.Value,
				SubTree1,
				aNode.SubTree2
			);
		}
	}
	
	internal static tNode<tKey, tValue>
	RemoveMax<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		out tKey aKey,
		out tValue aValue
	) {
		if (aNode.SubTree2 is null) {
			aKey = aNode.Key;
			aValue = aNode.Value;
			return aNode.SubTree1;
		} else {
			var SubTree2 = aNode.SubTree2.RemoveMax(
				out aKey,
				out aValue
			);
			return Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1,
				SubTree2
			);
		}
	}
	
	internal static tNode<tKey, tValue>
	RotateRight<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		return Node(
			aNode.SubTree1.Key,
			aNode.SubTree1.Value,
			aNode.SubTree1.SubTree1,
			Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1.SubTree2,
				aNode.SubTree2
			)
		);
	}
	
	internal static tNode<tKey, tValue>
	RotateLeft<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		return Node(
			aNode.SubTree2.Key,
			aNode.SubTree2.Value,
			Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1,
				aNode.SubTree2.SubTree1
			),
			aNode.SubTree2.SubTree2
		);
	}
	
	internal static tNode<tKey, tValue>
	Balance<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		var Diff = aNode.SubTree1.Deep() - aNode.SubTree2.Deep();
		if (mMath.Abs(Diff) <= 1) {
			return aNode;
		}
		
		var Result = aNode;
		switch (mMath.Sign(Diff)) {
			case -1: {
				if (Result.SubTree2.SubTree1.Deep() > Result.SubTree2.SubTree2.Deep()) {
					Result = Node(
						Result.Key,
						Result.Value,
						Result.SubTree1,
						Result.SubTree2.RotateRight()
					);
				}
				Result = Result.RotateLeft();
				break;
			}
			case 1: {
				if (Result.SubTree1.SubTree2.Deep() > Result.SubTree1.SubTree1.Deep()) {
					Result = Node(
						Result.Key,
						Result.Value,
						Result.SubTree1.RotateLeft(),
						Result.SubTree2
					);
				}
				Result = Result.RotateRight();
				break;
			}
		}
		return Result;
	}
	
	internal static tNode<tKey, tValue>
	Node<tKey, tValue>(
		tKey aKey,
		tValue aValue,
		tNode<tKey, tValue> aSubTree1,
		tNode<tKey, tValue> aSubTree2
	) {
		return new tNode<tKey, tValue> {
			Key = aKey,
			Value = aValue,
			SubTree1 = aSubTree1,
			SubTree2 = aSubTree2,
			Deep = 1 + mMath.Max(
				aSubTree1.Deep(),
				aSubTree2.Deep()
			),
		};
	}

	public static mStream.tStream<(tKey Key, tValue Value)>
	ToStream<tKey, tValue>(
		this tTree<tKey, tValue> a
	) => a.Root.ToStream();
	
	private static mStream.tStream<(tKey Key, tValue Value)>
	ToStream<tKey, tValue>(
		this tNode<tKey, tValue> a
	) {
		if (a is null) {
			return mStream.Stream<(tKey Key, tValue Value)>(); 
		}
		return mStream.Concat(
			a.SubTree1.ToStream(),
			mStream.Concat(
				mStream.Stream((a.Key, a.Value)),
				a.SubTree2.ToStream()
			)
		);
	}
}
