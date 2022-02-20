#nullable enable

[DebuggerStepThrough]
public static class
mTreeMap {
	
	[DebuggerTypeProxy(typeof(mTreeMap.tTree<,>.tDebuggerProxy))]
	public readonly struct
	tTree<tKey, tValue> {
		internal readonly mStd.tFunc<tInt32, tKey, tKey> KeyCompare;
		internal readonly tNode<tKey, tValue>? Root;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal tTree(
			mStd.tFunc<tInt32, tKey, tKey> aKeyCompare,
			tNode<tKey, tValue>? aRoot
		) {
			this.KeyCompare = aKeyCompare;
			this.Root = aRoot;
		}
		
		private struct tDebuggerProxy {
			private readonly tTree<tKey, tValue> _Tree;
			public tDebuggerProxy(tTree<tKey, tValue> a) { this._Tree = a; }
			
			[Pure]
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public (tKey Key, tValue Value)[] List => this._Tree.ToStream().Take(100).ToArrayList().ToArray();
		}
	}
	
	internal class
	tNode<tKey, tValue> {
		internal tKey Key = default!;
		internal tValue Value = default!;
		internal tInt32 Deep;
		internal tNode<tKey, tValue>? SubTree1;
		internal tNode<tKey, tValue>? SubTree2;
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tInt32
	Deep<tKey, tValue>(
		this tNode<tKey, tValue>? a
	) {
		return a?.Deep ?? 0;
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tInt32
	Deep<tKey, tValue>(
		this tTree<tKey, tValue> a
	) {
		return a.Root.Deep();
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tTree<tKey, tValue>
	Tree<tKey, tValue>(
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare,
		params (tKey Key, tValue Value)[] aItems
	) {
		var Tree = new tTree<tKey, tValue>(aKeyCompare, default);
		foreach (var (Key, Value) in aItems) {
			Tree = Tree.Set(Key, Value);
		}
		return Tree;
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tTree<tKey, tValue>
	Set<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey,
		tValue aValue
	) => new(
		aTree.KeyCompare,
		aTree.Root.Add(aKey, aValue, aTree.KeyCompare)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>
	Add<tKey, tValue>(
		this tNode<tKey, tValue>? aNode,
		tKey aKey,
		tValue aValue,
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare
	) {
		if (aNode is null) {
			return Node(aKey, aValue, null, null);
		}
		
		return aKeyCompare(aNode.Key, aKey) switch {
			0 => Node(
				aKey,
				aValue,
				aNode.SubTree1,
				aNode.SubTree2
			),
			1 => Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1.Add(aKey, aValue, aKeyCompare),
				aNode.SubTree2
			).Balance(),
			-1 => Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1,
				aNode.SubTree2.Add(aKey, aValue, aKeyCompare)
			).Balance(),
			_ => throw mError.Error("impossible"),
		};
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static mMaybe.tMaybe<tValue>
	TryGet<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey
	) => aTree.Root.TryGet(aKey, aTree.KeyCompare);
	
	//public static tValue
	//ForceGet<tKey, tValue>(
	//	this tTree<tKey, tValue> aTree,
	//	tKey aKey
	//) => aTree.TryGet(aKey).ElseThrow("unknown key: " + aKey);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static mMaybe.tMaybe<tValue>
	TryGet<tKey, tValue>(
		this tNode<tKey, tValue>? aNode,
		tKey aKey,
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare
	) {
		if (aNode is null) { return mStd.cEmpty; }
		var Comp = aKeyCompare(aKey, aNode.Key);
		return (
			(Comp == 0) ? mMaybe.Some(aNode.Value) :
			(Comp > 0) ? aNode.SubTree2.TryGet(aKey, aKeyCompare) :
			aNode.SubTree1.TryGet(aKey, aKeyCompare)
		);
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tTree<tKey, tValue>
	Remove<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey
	) => new(
		aTree.KeyCompare,
		aTree.Root!.Remove(aKey, aTree.KeyCompare)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>?
	Remove<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		tKey aKey,
		mStd.tFunc<tInt32, tKey, tKey> aKeyCompare
	) {
		mAssert.IsFalse(aNode is null);
		
		var SubTree1 = aNode.SubTree1;
		var SubTree2 = aNode.SubTree2;
		var Key = aNode.Key;
		var Value = aNode.Value;
		
		if (SubTree1 is null && SubTree2 is null) {
			mAssert.AreEquals(aKey, Key);
			return null;
		}
		
		switch (aKeyCompare(aKey, Key)) {
			case 0: {
				if (SubTree1.Deep() > SubTree2.Deep()) {
					SubTree1 = SubTree1!.RemoveMax(out Key, out Value);
				} else {
					SubTree2 = SubTree2!.RemoveMin(out Key, out Value);
				}
				break;
			}
			case -1: {
				SubTree1 = aNode.SubTree1!.Remove(aKey, aKeyCompare);
				break;
			}
			case 1: {
				SubTree2 = aNode.SubTree2!.Remove(aKey, aKeyCompare);
				break;
			}
		}
		
		return Node(Key, Value, SubTree1, SubTree2).Balance();
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>?
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>?
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>
	RotateRight<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		return Node(
			aNode.SubTree1!.Key,
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>
	RotateLeft<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		return Node(
			aNode.SubTree2!.Key,
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>
	Balance<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		var Diff = aNode.SubTree1.Deep() - aNode.SubTree2.Deep();
		if (Diff.Abs() <= 1) {
			return aNode;
		}
		
		return Diff.Sign() switch
		{
			-1 => (
				(aNode.SubTree2!.SubTree1.Deep() > aNode.SubTree2.SubTree2.Deep())
				? Node(
					aNode.Key,
					aNode.Value,
					aNode.SubTree1,
					aNode.SubTree2.RotateRight()
				)
				: aNode
			).RotateLeft(),
			1 => (
				(aNode.SubTree1!.SubTree2.Deep() > aNode.SubTree1.SubTree1.Deep())
				? Node(
					aNode.Key,
					aNode.Value,
					aNode.SubTree1.RotateLeft(),
					aNode.SubTree2
				)
				: aNode
			).RotateRight(),
			_ => aNode,
		};
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static tNode<tKey, tValue>
	Node<tKey, tValue>(
		tKey aKey,
		tValue aValue,
		tNode<tKey, tValue>? aSubTree1,
		tNode<tKey, tValue>? aSubTree2
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static mStream.tStream<(tKey Key, tValue Value)>?
	ToStream<tKey, tValue>(
		this tTree<tKey, tValue> a
	) => a.Root.ToStream();
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static mStream.tStream<(tKey Key, tValue Value)>?
	ToStream<tKey, tValue>(
		this tNode<tKey, tValue>? a
	) => (a is null)
	? mStream.Stream<(tKey Key, tValue Value)>()
	: mStream.Concat(
		a.SubTree1.ToStream(),
		mStream.Concat(
			mStream.Stream((a.Key, a.Value)),
			a.SubTree2.ToStream()
		)
	);
}
