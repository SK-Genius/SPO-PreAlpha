#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mAssert.cs
#:ref mError.cs
#:ref mMaybe.cs
#:ref mRef.cs
#:ref mStream.cs
#:ref mMath.cs
#:ref mArrayList.cs

public static class
mTreeMap {
	[DebuggerTypeProxy(typeof(tTree<,>.tDebuggerProxy))]
	public readonly struct
	tTree<tKey, tValue> {
		internal readonly mStd.tFunc<tKey, tKey, tInt32> KeyCompare;
		internal readonly mRef.tRef<tNode<tKey, tValue>> Root;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tTree(
			mStd.tFunc<tKey, tKey, tInt32> aKeyCompare,
			mRef.tRef<tNode<tKey, tValue>> aRoot
		) {
			this.KeyCompare = aKeyCompare;
			this.Root = aRoot;
		}
		
		private readonly struct
		tDebuggerProxy(
			tTree<tKey, tValue> aTree
		) {
			[Pure]
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden), DebuggerHidden]
			public (tKey Key, tValue Value)[]
			List => aTree.ToStream().Take(100).ToArrayList().ToArray();
		}
	}
	
	internal struct
	tNode<tKey, tValue> {
		internal tKey Key;
		internal tValue Value;
		internal tInt32 Deep;
		internal mRef.tRef<tNode<tKey, tValue>> SubTree1;
		internal mRef.tRef<tNode<tKey, tValue>> SubTree2;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tInt32
	Deep<tKey, tValue>(
		this mRef.tRef<tNode<tKey, tValue>> a
	) => a.Is(out var Node) ? Node.Deep : 0;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Deep<tKey, tValue>(
		this tTree<tKey, tValue> a
	) => a.Root.Deep();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tTree<tKey, tValue>
	Tree<tKey, tValue>(
		mStd.tFunc<tKey, tKey, tInt32> aKeyCompare,
		System.Span<(tKey Key, tValue Value)> aItems
	) => aItems.AsStream(
	).Reduce(
		new tTree<tKey, tValue>(aKeyCompare, default),
		(Tree, _) => Tree.Set(_.Key, _.Value)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tTree<tKey, tValue>
	Set<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey,
		tValue aValue
	) => new(
		aTree.KeyCompare,
		aTree.Root.Add(aKey, aValue, aTree.KeyCompare)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tNode<tKey, tValue>
	Add<tKey, tValue>(
		this mRef.tRef<tNode<tKey, tValue>> aNode,
		tKey aKey,
		tValue aValue,
		mStd.tFunc<tKey, tKey, tInt32> aKeyCompare
	) => aNode.Is(out var Node_)
	? aKeyCompare(Node_.Key, aKey) switch {
		0 => Node(
			aKey,
			aValue,
			Node_.SubTree1,
			Node_.SubTree2
		),
		1 => Node(
			Node_.Key,
			Node_.Value,
			Node_.SubTree1.Add(aKey, aValue, aKeyCompare),
			Node_.SubTree2
		).Balance(),
		-1 => Node(
			Node_.Key,
			Node_.Value,
			Node_.SubTree1,
			Node_.SubTree2.Add(aKey, aValue, aKeyCompare)
		).Balance(),
		_ => throw mError.Error("impossible"),
	}
	: Node(aKey, aValue, mStd.cEmpty, mStd.cEmpty);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static mMaybe.tMaybe<tValue>
	TryGet<tKey, tValue>(
		this mRef.tRef<tNode<tKey, tValue>> aNode,
		tKey aKey,
		mStd.tFunc<tKey, tKey, tInt32> aKeyCompare
	) => aNode.Is(out var Node)
	? aKeyCompare(aKey, Node.Key) switch {
		0 => Node.Value,
		>0 => Node.SubTree2.TryGet(aKey, aKeyCompare),
		_ => Node.SubTree1.TryGet(aKey, aKeyCompare)
	}
	: mStd.cEmpty;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tTree<tKey, tValue>
	Remove<tKey, tValue>(
		this tTree<tKey, tValue> aTree,
		tKey aKey
	) => new(
		aTree.KeyCompare,
		aTree.Root.Remove(aKey, aTree.KeyCompare)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static mRef.tRef<tNode<tKey, tValue>>
	Remove<tKey, tValue>(
		this mRef.tRef<tNode<tKey, tValue>> aNode,
		tKey aKey,
		mStd.tFunc<tKey, tKey, tInt32> aKeyCompare
	) {
		if (aNode.Is(out var Node)) {
			return Node.Remove(aKey, aKeyCompare);
		} else {
			return mStd.cEmpty;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static mRef.tRef<tNode<tKey, tValue>>
	Remove<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		tKey aKey,
		mStd.tFunc<tKey, tKey, tInt32> aKeyCompare
	) {
		var Key = aNode.Key;
		var Value = aNode.Value;
		
		var SubTree1 = aNode.SubTree1;
		var SubTree2 = aNode.SubTree2;
		
		if (aNode.SubTree1.IsEmpty() || aNode.SubTree2.IsEmpty()) {
			mAssert.AreEquals(aKey, Key);
			return mStd.cEmpty;
		}
		
		switch (aKeyCompare(aKey, Key)) {
			case 0: {
				if (aNode.SubTree1.Deep() > aNode.SubTree2.Deep()) {
					mAssert.IsTrue(SubTree1.Is(out var SubTree1_));
					SubTree1 = SubTree1_.RemoveMax(out Key, out Value);
				} else {
					mAssert.IsTrue(SubTree2.Is(out var SubTree2_));
					SubTree2 = SubTree2_.RemoveMin(out Key, out Value);
				}
				break;
			}
			case -1: {
				SubTree1 = SubTree1.Remove(aKey, aKeyCompare);
				break;
			}
			case 1: {
				SubTree2 = SubTree2.Remove(aKey, aKeyCompare);
				break;
			}
			default: {
				throw mError.Error("impossible");
			}
		}
		
		return Node(Key, Value, SubTree1, SubTree2).Balance();
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static mRef.tRef<tNode<tKey, tValue>>
	RemoveMin<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		out tKey aKey,
		out tValue aValue
	) {
		if (aNode.SubTree1.Is(out var SubTree1)) {
			return Node(
				aNode.Key,
				aNode.Value,
				SubTree1.RemoveMin(
					out aKey,
					out aValue
				),
				aNode.SubTree2
			);
		} else {
			aKey = aNode.Key;
			aValue = aNode.Value;
			return aNode.SubTree2;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static mRef.tRef<tNode<tKey, tValue>>
	RemoveMax<tKey, tValue>(
		this tNode<tKey, tValue> aNode,
		out tKey aKey,
		out tValue aValue
	) {
		if (aNode.SubTree2.Is(out var SubTree2)) {
			return Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1,
				SubTree2.RemoveMax(
					out aKey,
					out aValue
				)
			);
		} else {
			aKey = aNode.Key;
			aValue = aNode.Value;
			return aNode.SubTree1;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tNode<tKey, tValue>
	RotateRight<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		mAssert.IsTrue(aNode.SubTree1.Is(out var SubTree1));
		
		return Node(
			SubTree1.Key,
			SubTree1.Value,
			SubTree1.SubTree1,
			Node(
				aNode.Key,
				aNode.Value,
				SubTree1.SubTree2,
				aNode.SubTree2
			)
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tNode<tKey, tValue>
	RotateLeft<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		mAssert.IsTrue(aNode.SubTree2.Is(out var SubTree2));
		
		return Node(
			SubTree2!.Key,
			SubTree2.Value,
			Node(
				aNode.Key,
				aNode.Value,
				aNode.SubTree1,
				SubTree2.SubTree1
			),
			SubTree2.SubTree2
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tNode<tKey, tValue>
	Balance<tKey, tValue>(
		this tNode<tKey, tValue> aNode
	) {
		var Diff = aNode.SubTree1.Deep() - aNode.SubTree2.Deep();
		if (Diff.Abs() <= 1) {
			return aNode;
		}
		
		
		switch (Diff.Sign()) {
			case -1: {
				mAssert.IsTrue(aNode.SubTree2.Is(out var SubTree2));
				return (
					(SubTree2.SubTree1.Deep() > SubTree2.SubTree2.Deep())
					? Node(
						aNode.Key,
						aNode.Value,
						aNode.SubTree1,
						SubTree2.RotateRight()
					)
					: aNode
				).RotateLeft();
			}
			case 1: {
				mAssert.IsTrue(aNode.SubTree1.Is(out var SubTree1));
				return (
					(SubTree1.SubTree2.Deep() > SubTree1.SubTree1.Deep())
					? Node(
						aNode.Key,
						aNode.Value,
						SubTree1.RotateLeft(),
						aNode.SubTree2
					)
					: aNode
				).RotateRight();
			}
			default: {
				return aNode;
			}
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	internal static tNode<tKey, tValue>
	Node<tKey, tValue>(
		tKey aKey,
		tValue aValue,
		mRef.tRef<tNode<tKey, tValue>> aSubTree1,
		mRef.tRef<tNode<tKey, tValue>> aSubTree2
	) => new () {
		Key = aKey,
		Value = aValue,
		SubTree1 = aSubTree1,
		SubTree2 = aSubTree2,
		Deep = 1 + mMath.Max(
			aSubTree1.Deep(),
			aSubTree2.Deep()
		),
	};
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mStream.tStream<(tKey Key, tValue Value)>
	ToStream<tKey, tValue>(
		this tTree<tKey, tValue> a
	) => a.Root.ToStream();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static mStream.tStream<(tKey Key, tValue Value)>
	ToStream<tKey, tValue>(
		this mRef.tRef<tNode<tKey, tValue>> a
	) => (
		a.Is(out var Node)
		? mStream.Concat(
			Node.SubTree1.ToStream(),
			mStream.Concat(
				mStream.Stream((Node.Key, Node.Value)),
				Node.SubTree2.ToStream()
			)
		)
		: mStd.cEmpty
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tTree<tGroup, mStream.tStream<t>>
	GroupAndSortBy<t, tGroup>(
		this mStream.tStream<t> aStream,
		mStd.tFunc<t, tGroup> aDefineGroup,
		mStd.tFunc<tGroup, tGroup, tInt32> aCompGroup
	) => aStream.Reduce(
		Tree<tGroup, mStream.tStream<t>>(aCompGroup, []),
		(aTree, aItem) => mStd.With(
			aDefineGroup(aItem),
			aGroup => aTree.TryGet(
				aGroup
			).Match(
				__ => aTree.Set(aGroup, mStream.Stream(aItem, __)),
				() => aTree.Set(aGroup, mStream.Stream(aItem))
			)
		)
	);
}
