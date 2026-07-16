#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mMaybe.cs
#:ref mLazy.cs
#:ref mRef.cs

//#define TAIL_RECURSIVE

public static class
mStream {
	[DebuggerTypeProxy(typeof(tStream<>.tDebuggerProxy))]
	public struct
	tStream<t> {
		internal mRef.tRef<(t Head, mLazy.tLazy<tStream<t>> Tail)> _HeadTail;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tStream(
			t aHead,
			mLazy.tLazy<tStream<t>> aTail
		) {
			this._HeadTail = mRef.Ref((aHead, aTail));
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
					var LimitedStream = aStream.Take(100);
					var Count = LimitedStream.Count();
					return LimitedStream.MapWithIndex(
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
	
	[method: MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	}
	
	private sealed class
	tGenComp<t>(mStd.tFunc<t, t, tInt32> aComp) : System.Collections.Generic.IComparer<t> {
		public System.Int32 Compare(t? a1, t? a2) => aComp(a1, a2);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
	) => new();
	
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
		t aHead,
		mStd.tFunc<tStream<t>> aTailFunc
	) => new(
		aHead,
		mLazy.Lazy(aTailFunc)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>
	Stream<t>(
		params System.ReadOnlySpan<t> aStream
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
	Stream<t, tAccu>(
		tAccu aInitAccu,
		mStd.tFunc<tAccu, t> aGetHead,
		mStd.tFunc<tAccu, mMaybe.tMaybe<tAccu>> aGetNextAccu
	) => Stream(
		aGetHead(aInitAccu),
		[DebuggerHidden]() => aGetNextAccu(aInitAccu).Match(
			[DebuggerHidden](aNextAccu) => Stream<t, tAccu>(aNextAccu, aGetHead, aGetNextAccu),
			[DebuggerHidden]() => mStd.cEmpty
		)
	);
	
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
	public static tStream<tInt8>
	Int8StartWith(
		tInt8 aStart
	) => Stream(aStart, () => Int8StartWith((tInt8)(aStart + 1)));
	
	[Pure, DebuggerHidden]
	public static tStream<tInt16>
	Int16StartWith(
		tInt16 aStart
	) => Stream(aStart, () => Int16StartWith((tInt16)(aStart + 1)));
	
	[Pure, DebuggerHidden]
	public static tStream<tInt32>
	Int32StartWith(
		tInt32 aStart
	) => Stream(aStart, () => Int32StartWith(aStart + 1));
	
	[Pure, DebuggerHidden]
	public static tStream<tInt64>
	Int64StartWith(
		tInt64 aStart
	) => Stream(aStart, () => Int64StartWith(aStart + 1));
	
	[Pure, DebuggerHidden]
	public static tStream<tNat8>
	Nat8StartWith(
		tNat8 aStart
	) => Stream(aStart, () => Nat8StartWith((tNat8)(aStart + 1)));
	
	[Pure, DebuggerHidden]
	public static tStream<tNat16>
	Nat16StartWith(
		tNat16 aStart
	) => Stream(aStart, () => Nat16StartWith((tNat16)(aStart + 1)));
	
	[Pure, DebuggerHidden]
	public static tStream<tNat32>
	Nat32StartWith(
		tNat32 aStart
	) => Stream(aStart, () => Nat32StartWith(aStart + 1));
	
	[Pure, DebuggerHidden]
	public static tStream<tNat64>
	Nat64StartWith(
		tNat64 aStart
	) => Stream(aStart, () => Nat64StartWith(aStart + 1));
	
	extension<t> (tStream<t> aStream) {
		public tBool
		IsRefEqual(
			tStream<t> a2
		) => aStream._HeadTail.IsRefEqual(a2._HeadTail);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tStreamIterator<t>
		GetEnumerator(
		) => new(aStream);
		
		public tBool
		Eq(
			tStream<t> a2,
			mStd.tFunc<t, t, tBool> aEq
		) => aStream.Match(
			[DebuggerHidden] () => a2.IsEmpty(),
			[DebuggerHidden] (aHead1, aTail1) => a2.Match(
				[DebuggerHidden] () => false,
				[DebuggerHidden] (aHead2, aTail2) => aEq(aHead1, aHead2) && Eq(aTail1, aTail2, aEq)
			)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tOut
		Match<tOut>(
			mStd.tFunc<tOut> aOnNone,
			mStd.tFunc<t, tStream<t>, tOut> aOnAny
		) => (
			aStream.Is(out var Head, out var Tail)
			? aOnAny(Head, Tail)
			: aOnNone()
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tOut
		Match<tOut>(
			mStd.tFunc<t, tStream<t>, tOut> aOnAny,
			mStd.tFunc<tOut> aOnNone
		) => aStream.Match(aOnNone, aOnAny);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Is(
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
		[return: NotNullIfNotNull(nameof(aStream))]
		public tStream<tRes>
		Map<tRes>(
			mStd.tFunc<t, tRes> aMapFunc
		) => (
			aStream.Is(out var Head, out var Tail)
			? Stream(aMapFunc(Head), [DebuggerHidden] () => Tail.Map(aMapFunc))
			: mStd.cEmpty
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tStream<tRes>
		MapWithIndex<tRes>(
			mStd.tFunc<tNat32, t, tRes> aMapFunc
		) => aStream.MapWithIndex().Map([DebuggerHidden] (a) => aMapFunc(a.Index, a.Item));
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tStream<(tNat32 Index, t Item)>
		MapWithIndex(
		) => ZipShort(Nat32StartWith(0), aStream);
		
		[Pure, DebuggerHidden]
		public tRes
		Reduce<tRes>(
			tRes aInitialAggregate,
			mStd.tFunc<tRes, t, tRes> aAggregatorFunc
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
		public tNat32
		Count(
		) => aStream.Reduce(0u, (a, _) => a + 1);
		
		[Pure, DebuggerHidden]
		public mMaybe.tMaybe<t>
		TryReduce(
			mStd.tFunc<t, t, t> aAggregatorFunc
		) => (
			aStream.Is(out var Head, out var Tail)
			? Tail.Reduce(Head, aAggregatorFunc)
			: mStd.cEmpty
		);
		
		[Pure, DebuggerHidden]
		public tStream<t>
		DontRepeat(
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
		public tStream<t>
		Sort(
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
		public t
		Join(
			mStd.tFunc<t, t, t> aAggregatorFunc,
			t aDefault
		) => (
			aStream.Is(out var Head, out var Tail)
			? Tail.Reduce(Head, aAggregatorFunc)
			: aDefault
		);
		
		[Pure, DebuggerHidden]
		public tStream<t>
		Take(
			tNat32 aCount
		) => (
			(aCount > 0 && aStream.Is(out var Head, out var Tail))
			? Stream(Head, () => Tail.Take(aCount - 1))
			: mStd.cEmpty
		);
		
		[Pure, DebuggerHidden]
		public tStream<t>
		TakeWhile(
			mStd.tFunc<t, tBool> aCond
		) => (
			aStream.Is(out var Head, out var Tail) && aCond(Head)
			? Stream(Head, () => Tail.TakeWhile(aCond))
			: mStd.cEmpty
		);
		
		[Pure, DebuggerHidden]
		public tStream<t>
		TakeUntil(
			mStd.tFunc<t, tBool> aCond
		) => (
			aStream.Is(out var Head, out var Tail) && !aCond(Head)
			? Stream(Head, () => Tail.TakeWhile(aCond))
			: mStd.cEmpty
		);
		
		[Pure, DebuggerHidden]
		public tStream<t>
		Skip(
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
		public tStream<t>
		SkipUntil(
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
		public tStream<t>
		SkipWhile(
			mStd.tFunc<t, tBool> aCond
		) => aStream.SkipUntil([DebuggerHidden] (a) => !aCond(a));
		
		[Pure, DebuggerHidden]
		public tStream<t>
		Every(
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
		public tStream<t>
		Where(
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
		public tBool
		IsEmpty(
		) => aStream._HeadTail.IsEmpty();
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public mMaybe.tMaybe<t>
		TryFirst(
		) => aStream.Is(out var Head, out _) ? Head : mStd.cEmpty;
		
		[Pure, DebuggerHidden]
		public mMaybe.tMaybe<t>
		TryLast(
		) {
			var Result = mMaybe.None<t>();
			foreach (var Item in aStream) {
				Result = Item;
			}
			return Result;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public mMaybe.tMaybe<t>
		TryGet(
			tNat32 aIndex
		) => aStream.Skip(aIndex).TryFirst();
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Any(
			mStd.tFunc<t, tBool> aPrefix
		) => aStream.Map(aPrefix).Any();
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		All(
			mStd.tFunc<t, tBool> aPrefix
		) => aStream.Map(aPrefix).All();
		
		[Pure, DebuggerHidden]
		public tStream<t>
		Reverse(
		) {
			var Result = Stream<t>([]);
			foreach (var Item in aStream) {
				Result = Stream(Item, Result);
			}
			return Result;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	All(
		this tStream<tBool> aStream
	) => !aStream.Map(__ => !__).Any();
	
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
	
	public static mStd.tFunc<tStream<t>, tStream<t>, tBool>
	Eq<t>(
		mStd.tFunc<t, t, tBool> aEq
	) => [DebuggerHidden] (a1, a2) => a1.Eq(a2, aEq);
}
