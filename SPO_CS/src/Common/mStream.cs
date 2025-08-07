// IMPORT mStd
// IMPORT mMaybe
// IMPORT mLazy
// IMPORT mRef

//#define TAIL_RECURSIVE

public static class
mStream {
	[DebuggerTypeProxy(typeof(tStream<>.tDebuggerProxy))]
	public struct
	tStream<t> {
		internal mRef.tRef<(t Head, mLazy.tLazy<tStream<t>> Tail)> _HeadTail;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tStream(
			t aHead,
			mLazy.tLazy<tStream<t>> aTail
		) {
			this._HeadTail = mRef.Ref((aHead, aTail));
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public
		tStream(
		) {
			this._HeadTail = mStd.cEmpty;
		}
		
		[Pure, DebuggerHidden]
		public readonly tBool
		Equals(
			tStream<t> a
		) => this.Match(
			[DebuggerHidden] () => a._HeadTail.IsEmpty(),
			[DebuggerHidden] (aHead1, aTail1) => a.Match(
				[DebuggerHidden] () => false,
				[DebuggerHidden] (aHead2, aTail2) => Equals(aHead1, aHead2) && Equals(aTail1, aTail2)
			)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override readonly tBool
		Equals(
			tUnknown a
		) => this.Equals((tStream<t>)a);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tStream<t>(
			mStd.tEmpty _
		) => Stream<t>();
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override tText
		ToString(
		) => this.Reduce(
			new System.Text.StringBuilder().AppendLine("("),
			[DebuggerHidden] (aSB, a) => aSB.Append("  ").AppendLine(a.ToString())
		).AppendLine(
			")"
		).ToString();
		
		private readonly struct
		tDebuggerProxy(tStream<t> aStream) {
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden), DebuggerHidden]
			public t[] Text {
				get {
					var Count = aStream.Take(100).Count();
					return aStream.Take(100).MapWithIndex(
						[DebuggerHidden] (aIndex, aItem) => (Index: aIndex, Value: aItem)
					).Reduce(
						new t[Count],
						[DebuggerHidden] (aArray, a) => {
							aArray[a.Index] = a.Value;
							return aArray;
						}
					);
				}
			}
		}
	}
	
	[method: Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public ref struct
	tStreamIterator<t>(
		tStream<t> aStream
	) {
		private tStream<t> _Curr = mStd.cEmpty;
		private tStream<t> _Next = aStream;
		
		public void
		Reset(
		) => throw new System.NotImplementedException();
		
		public readonly ref t Current => ref this._Curr._HeadTail._Box!._Value.Head;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		MoveNext(
		) {
			if (this._Next.Is(out _, out var Tail)) {
				this._Curr = this._Next;
				this._Next = Tail;
				return true;
			} else {
				return false;
			}
		}
		
		public readonly void
		Dispose(
		) {
		}
	}
	
	public static tBool
	IsRefEqual<t>(
		this tStream<t> a1,
		tStream<t> a2
	) => a1._HeadTail.IsRefEqual(a2._HeadTail);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStreamIterator<t>
	GetEnumerator<t>(
		this tStream<t> aStream
	) => new(aStream);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
	) => new();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
		t aHead,
		mStd.tFunc<tStream<t>> aTailFunc
	) => new(
		aHead,
		mLazy.Lazy(aTailFunc)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
		t aHead,
		tStream<t> aTail
	) => new tStream<t>(
		aHead,
		aTail
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
		System.ReadOnlySpan<t> aStream
	) {
		var Result = Stream<t>();
		for (var I = aStream.Length; I --> 0;) {
			Result = Stream(aStream[I], Result);
		}
		return Result;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
		System.Span<t> aStream
	) {
		var Result = Stream<t>();
		for (var I = aStream.Length; I --> 0;) {
			Result = Stream(aStream[I], Result);
		}
		return Result;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	AsStream<t>(
		this t[] a
	) => System.MemoryExtensions.AsSpan(a).AsStream();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	AsStream<t>(
		this System.Span<t> a
	) => Stream(a);
	
	[Pure, DebuggerHidden]
	public static tStream<tNat32>
	NatStartWith(
		tNat32 aStart
	) => Stream(aStart, () => NatStartWith(aStart + 1));
	
	[Pure, DebuggerHidden]
	public static tStream<tInt32>
	Int(
		tInt32 aStart
	) => Stream(aStart, () => Int(aStart + 1));
	
	public static tBool
	Eq<t>(
		this tStream<t> a1,
		tStream<t> a2,
		mStd.tFunc<t, t, tBool> aEq
	) => a1.Match(
		[DebuggerHidden] () => a2.IsEmpty(),
		[DebuggerHidden] (aHead1, aTail1) => a2.Match(
			[DebuggerHidden] () => false,
			[DebuggerHidden] (aHead2, aTail2) => aEq(aHead1, aHead2) && Eq(aTail1, aTail2, aEq)
		)
	);
	
	public static mStd.tFunc<tStream<t>, tStream<t>, tBool>
	Eq<t>(
		mStd.tFunc<t, t, tBool> aEq
	) => [DebuggerHidden] (a1, a2) => a1.Eq(a2, aEq);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>(
		this tStream<tIn> aStream,
		mStd.tFunc<tOut> aOnNone,
		mStd.tFunc<tIn, tStream<tIn>, tOut> aOnAny
	) => (
		aStream.Is(out var Head, out var Tail)
		? aOnAny(Head, Tail)
		: aOnNone()
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>(
		this tStream<tIn> aStream,
		mStd.tFunc<tIn, tStream<tIn>, tOut> aOnAny,
		mStd.tFunc<tOut> aOnNone
	) => aStream.Match(aOnNone, aOnAny);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Is<t>(
		[NotNullWhen(true)] this tStream<t> aStream,
		out t aHead,
		out tStream<t> aTail
	) {
		if (aStream._HeadTail.Deref.IsSome(out var aHeadTail)) {
			aHead = aHeadTail.Head;
			aTail = aHeadTail.Tail.Value;
			return true;
		} else {
			aHead = default!;
			aTail = mStd.cEmpty;
			return false;
		}
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Concat<t>(
		tStream<t> a1,
		tStream<t> a2
	) => (
		a1.Is(out var Head, out var Tail)
		? Stream(Head, () => Concat(Tail, a2))
		: a2
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Flatt<t>(
		System.Span<tStream<t>> a
	) => Stream(a).Flatt();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Flatt<t>(
		this tStream<tStream<t>> a
	) => a.Reduce(
		default(tStream<t>),
		(aRes, a) => Concat(aRes, a)
	);
	
	[Pure, DebuggerHidden]
	[return: NotNullIfNotNull(nameof(aStream))]
	public static tStream<tRes>
	Map<tRes, tElem>(
		this tStream<tElem> aStream,
		mStd.tFunc<tElem, tRes> aMapFunc
	) => (
		aStream.Is(out var Head, out var Tail)
		? Stream(aMapFunc(Head), [DebuggerHidden] () => Tail.Map(aMapFunc))
		: mStd.cEmpty
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<tRes>
	MapWithIndex<tRes, tElem>(
		this tStream<tElem> aStream,
		mStd.tFunc<tNat32, tElem, tRes> aMapFunc
	) => aStream.MapWithIndex().Map([DebuggerHidden] (a) => aMapFunc(a.Index, a.Item));
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<(tNat32 Index, t Item)>
	MapWithIndex<t>(
		this tStream<t> aStream
	) => ZipShort(NatStartWith(0), aStream);
	
	[Pure, DebuggerHidden]
	public static tRes
	Reduce<tRes, tElem>(
		this tStream<tElem> aStream,
		tRes aInitialAggregate,
		mStd.tFunc<tRes, tElem, tRes> aAggregatorFunc
	#if TAIL_RECURSIVE
	) => (
		aStream.Match(out var Head, out var Tail)
		? Tail.Reduce(aAggregatorFunc(aInitialAggregate, Head), aAggregatorFunc)
		: aInitialAggregate
	);
	#else
	) {
		var Result = aInitialAggregate;
		foreach (var Item in aStream) {
			Result = aAggregatorFunc(Result, Item);
		}
		return Result;
	}
	#endif
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tNat32
	Count<t>(
		this tStream<t> aStream
	) => aStream.Reduce(0u, (a, _) => a + 1);
	
	[Pure, DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryReduce<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, t, t> aAggregatorFunc
	) => (
		aStream.Is(out var Head, out var Tail)
		? Tail.Reduce(Head, aAggregatorFunc)
		: mStd.cEmpty
	);
	
	private sealed class tGenComp<t>(mStd.tFunc<t, t, tInt32> aComp) : System.Collections.Generic.IComparer<t> {
		public System.Int32 Compare(t? a1, t? a2) => aComp(a1, a2);
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	DontRepeat<t>(
		this tStream<t> aStream
	) {
		if (aStream.Is(out var First, out var Tail)) {
			while (Tail.Is(out var Next, out var NextTail)) {
				if (!Equals(First, Next)) {
					return Stream(First, () => Tail.DontRepeat());
				}
				Tail = NextTail;
			}
			return Stream([First]);
		} else {
			return mStd.cEmpty;
		}
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Sort<t>(
		this tStream<t> aStream
	) where t : System.IComparable<t> {
		var Res = new t[aStream.Count()];
		var I = 0;
		foreach (var Item in aStream) {
			Res[I] = Item;
			I += 1;
		}
		System.Array.Sort(Res);
		return Stream(System.MemoryExtensions.AsSpan(Res));
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Sort<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, t, tInt32> aCompare
	) {
		var Res = new t[aStream.Count()];
		var I = 0;
		foreach (var Item in aStream) {
			Res[I] = Item;
			I += 1;
		}
		System.Array.Sort(
			Res,
			System.Collections.Generic.Comparer<t>.Create(
				(a1, a2) => aCompare(a1, a2)
			)
		);
		return Stream(System.MemoryExtensions.AsSpan(Res));
	}
	
	[Pure, DebuggerHidden]
	public static t
	Join<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, t, t> aAggregatorFunc,
		t aDefault
	) => (
		aStream.Is(out var Head, out var Tail)
		? Tail.Reduce(Head, aAggregatorFunc)
		: aDefault
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Take<t>(
		this tStream<t> aStream,
		tNat32 aCount
	) => (
		(aCount > 0 && aStream.Is(out var Head, out var Tail))
		? Stream(Head, () => Tail.Take(aCount - 1))
		: mStd.cEmpty
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	TakeWhile<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aCond
	) => (
		aStream.Is(out var Head, out var Tail) && aCond(Head)
		? Stream(Head, () => Tail.TakeWhile(aCond))
		: mStd.cEmpty
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	TakeUntil<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aCond
	) => (
		aStream.Is(out var Head, out var Tail) && !aCond(Head)
		? Stream(Head, () => Tail.TakeWhile(aCond))
		: mStd.cEmpty
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Skip<t>(
		this tStream<t> aStream,
		tNat32 aCount
	) {
		#if TAIL_RECURSIVE
		mAssert.IsTrue(aCount >= 0);
		return (
			(aCount > 0 && aStream.Match(out var Head, out var Tail))
			? Tail.Skip(aCount - 1)
			: aStream
		);
		#else
		while (aCount --> 0 && aStream.Is(out var _, out aStream)) { }
		return aStream;
		#endif
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	SkipUntil<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aCond
	) {
		while (aStream.Is(out var Head, out aStream)) {
			if (aCond(Head)) {
				return Stream(Head, aStream);
			}
		}
		return mStd.cEmpty;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	SkipWhile<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aCond
	) => aStream.SkipUntil([DebuggerHidden] (a) => !aCond(a));
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Every<t>(
		this tStream<t> aStream,
		tNat32 aCount
	) {
		if (aCount is 0) {
			return mStd.cEmpty;
		}
		
		return (
			aStream.Is(out var Head, out var Tail)
			? Stream(Head, () => Tail.Skip(aCount - 1).Every(aCount))
			: mStd.cEmpty
		);
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Where<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aPredicate
	) {
		#if TAIL_RECURSIVE
		return (
			!aStream.Match(out var Head, out var Tail) ? Stream<t>() :
			aPredicate(Head) ? Stream(Head, [DebuggerHidden] () => Tail.Where(aPredicate)) :
			Tail.Where(aPredicate)
		);
		#else
		while (aStream.Is(out var Head, out aStream)) {
			if (aPredicate(Head)) {
				return Stream(Head, [DebuggerHidden] () => aStream.Where(aPredicate));
			}
		}
		return mStd.cEmpty;
		#endif
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	IsEmpty<t>(
		[NotNullWhen(false)] this tStream<t> aStream
	) => aStream._HeadTail.IsEmpty();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryFirst<t>(
		this tStream<t> aStream
	) => aStream.Is(out var Head, out _) ? Head : mStd.cEmpty;
	
	[Pure, DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryLast<t>(
		this tStream<t> aStream
	) {
		var Result = mMaybe.None<t>();
		foreach (var Item in aStream) {
			Result = Item;
		}
		return Result;
	}
	
	[Pure, DebuggerHidden]
	public static tBool
	Any(
		this tStream<tBool> aStream
	) {
		#if TAIL_RECURSIVE
		return aStream.Match(out var Head, out var Tail) && (Head || Tail.Any());
		#else
		foreach (var Item in aStream) {
			if (Item) {
				return true;
			}
		}
		return false;
		#endif
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryGet<t>(
		this tStream<t> aStream,
		tNat32 aIndex
	) => aStream.Skip(aIndex).TryFirst();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Any<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aPrefix
	) => aStream.Map(aPrefix).Any();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	All(
		this tStream<tBool> aStream
	) => !aStream.Map(_ => !_).Any();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	All<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, tBool> aPrefix
	) => aStream.Map(aPrefix).All();
	
	[Pure, DebuggerHidden]
	public static tStream<t>
	Reverse<t>(
		this tStream<t> aStream
	) {
		var Result = Stream<t>([]);
		foreach (var Item in aStream) {
			Result = Stream(Item, Result);
		}
		return Result;
	}
	
	[Pure, DebuggerHidden]
	public static tStream<(t1 _1, t2 _2)>
	ZipShort<t1, t2>(
		tStream<t1> a1,
		tStream<t2> a2
	) => (
		a1.Is(out var Head1, out var Tail1) &&
		a2.Is(out var Head2, out var Tail2)
	)
	? Stream((Head1, Head2), () => ZipShort(Tail1, Tail2))
	: mStd.cEmpty;
	
	[Pure, DebuggerHidden]
	public static tStream<(mMaybe.tMaybe<t1> _1, mMaybe.tMaybe<t2> _2)>
	ZipExtend<t1, t2>(
		tStream<t1> a1,
		tStream<t2> a2
	) {
		if (a1.IsEmpty() && a2.IsEmpty()) {
			return mStd.cEmpty;
		}
		
		var MaybeHead1 = a1.Is(out var Head1, out var Tail1) ? mMaybe.Some(Head1) : mStd.cEmpty;
		var MaybeHead2 = a2.Is(out var Head2, out var Tail2) ? mMaybe.Some(Head2) : mStd.cEmpty;
		return Stream((MaybeHead1, MaybeHead2), () => ZipExtend(Tail1, Tail2));
	}
}
