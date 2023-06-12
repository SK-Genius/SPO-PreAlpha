//IMPORT mStd.cs
//IMPORT mLazy.cs
//IMPORT mMaybe.cs

#nullable enable

//#define TAIL_RECURSIVE

public static class
mStream {
	
	[DebuggerTypeProxy(typeof(tStream<>.tDebuggerProxy))]
	public sealed class
	tStream<t> {
		internal readonly t _Head = default!;
		internal readonly mLazy.tLazy<tStream<t>?> _Tail;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tStream(
			t aHead,
			mLazy.tLazy<tStream<t>?> aTail
		) {
			this._Head = aHead;
			this._Tail = aTail;
		}
		
		[Pure, DebuggerHidden]
		public tBool
		Equals(
			tStream<t>? a
		) => this.Match(
			[DebuggerHidden]() => a.IsEmpty(),
			[DebuggerHidden](aHead1, aTail1) => a.Match(
				[DebuggerHidden]() => false,
				[DebuggerHidden](aHead2, aTail2) => Equals(aHead1, aHead2) && Equals(aTail1, aTail2)
			)
		);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override tBool
		Equals(
			object? a
		) => this.Equals((tStream<t>?)a);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static
		implicit operator tStream<t>?(
			mStd.tEmpty _
		) => null;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public override string
		ToString(
		) => this.Reduce(
			new System.Text.StringBuilder().AppendLine("("),
			[DebuggerHidden](aSB, a) => aSB.Append("  ").AppendLine(a.ToString())
		).AppendLine(
			")"
		).ToString();
		
		private readonly struct tDebuggerProxy {
			private readonly tStream<t> _Stream;
			
			public tDebuggerProxy(tStream<t> a) { this._Stream = a; }
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden), DebuggerHidden]
			public t[] Text {
				get {
					var Count = this._Stream.Take(100).Count();
					return this._Stream.Take(100).MapWithIndex(
						[DebuggerHidden](aIndex, aItem) => (Index: aIndex, Value: aItem)
					).Reduce(
						new t[Count],
						[DebuggerHidden](aArray, a) => {
							aArray[a.Index] = a.Value;
							return aArray;
						}
					);
				}
			}
		}
	}
	
	public struct
	tStreamIterator<t> {
		private t _Head;
		private tStream<t>? _Tail;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public
		tStreamIterator(
			tStream<t> aStream
		) {
			this._Head = default!;
			this._Tail = aStream;
		}
		
		public t Current => this._Head;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public bool
		MoveNext(
		) => this._Tail.Match(out this._Head, out this._Tail);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStreamIterator<t>
	GetEnumerator<t>(
		this tStream<t>? aStream
	) => new(aStream);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	Stream<t>(
		t aHead,
		mStd.tFunc<tStream<t>?> aTailFunc
	) => new(
		aHead,
		mLazy.Lazy(aTailFunc)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	Stream<t>(
		t aHead,
		tStream<t>? aTail
	) => new(
		aHead,
		aTail
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	Stream<t>(
	) => mStd.cEmpty;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	Stream<t>(
		params t[] aStream
	) {
		var Result = (tStream<t>?)null;
		for (var I = aStream.Length; I --> 0;) {
			Result = Stream(aStream[I], Result);
		}
		return Result;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	AsStream<t>(
		this t[] a
	) => Stream(a);
	
	[Pure, DebuggerHidden]
	public static tStream<tInt32>?
	Nat(
		int aStart
	) => Stream(aStart, () => Nat(aStart+1));
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>(
		this tStream<tIn> aStream,
		mStd.tFunc<tOut> aOnNone,
		mStd.tFunc<tOut, tIn, tStream<tIn>> aOnAny
	) => (
		aStream is null
		? aOnNone()
		: aOnAny(aStream._Head, aStream._Tail.Value)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>(
		this tStream<tIn> aStream,
		mStd.tFunc<tOut> aOnNone,
		mStd.tFunc<tOut, tStream<tIn>> aOnAny
	) => (
		aStream is null
		? aOnNone()
		: aOnAny(aStream)
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tOut
	Match<tIn, tOut>(
		this tStream<tIn> aStream,
		mStd.tFunc<tOut, tIn, tStream<tIn>> aOnAny,
		mStd.tFunc<tOut> aOnNone
	) => aStream.Match(aOnNone, aOnAny);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Match<t>(
		[NotNullWhen(true)] this tStream<t>? aStream,
		out t aHead,
		out tStream<t>? aTail
	) {
		if (aStream.IsEmpty()) {
			aHead = default!;
			aTail = default;
			return false;
		} else {
			aHead = aStream._Head;
			aTail = aStream._Tail.Value;
			return true;
		}
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	Concat<t>(
		tStream<t>? a1,
		tStream<t>? a2
	) => a1.Match(
		[DebuggerHidden]() => a2,
		[DebuggerHidden](aHead, aTail) => Stream(aHead, () => Concat(aTail, a2))
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	Flatt<t>(
		params tStream<t>?[] a
	) => mStream.Stream(a).Flatt();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	Flatt<t>(
		this tStream<tStream<t>?>? a
	) => a.Reduce(
		default(tStream<t>?),
		(aRes, a) => Concat(aRes, a)
	);
	
	[Pure, DebuggerHidden]
	[return: NotNullIfNotNull("aStream")]
	public static tStream<tRes>?
	Map<tRes, tElem>(
		this tStream<tElem>? aStream,
		mStd.tFunc<tRes, tElem> aMapFunc
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(aMapFunc(Head), [DebuggerHidden]() => Tail.Map(aMapFunc))
		: Stream<tRes>()
	);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<tRes>?
	MapWithIndex<tRes, tElem>(
		this tStream<tElem>? aStream,
		mStd.tFunc<tRes, tInt32, tElem> aMapFunc
	) => aStream.MapWithIndex().Map([DebuggerHidden](a) => aMapFunc(a.Index, a.Item));
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<(tInt32 Index, t Item)>?
	MapWithIndex<t>(
		this tStream<t>? aStream
	) => ZipShort(Nat(0), aStream);
	
	[Pure, DebuggerHidden]
	public static tRes
	Reduce<tRes, tElem>(
		this tStream<tElem>? aStream,
		tRes aInitialAggregate,
		mStd.tFunc<tRes, tRes, tElem> aAggregatorFunc
	#if TAIL_RECURSIVE
	) => (
		aStream.Match(out var Head, out var Tail)
		? Tail.Reduce(aAggregatorFunc(aInitialAggregate, Head), aAggregatorFunc)
		: aInitialAggregate
	);
	#else
	){
		var Result = aInitialAggregate;
		foreach (var Item in aStream) {
			Result = aAggregatorFunc(Result, Item);
		}
		return Result;
	}
	#endif
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Count<t>(
		this tStream<t>? aStream
	) => aStream.Reduce(0, (a, _) => a + 1);
	
	[Pure, DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryReduce<t>(
		this tStream<t>? aStream,
		mStd.tFunc<t, t, t> aAggregatorFunc
	) => (
		aStream.Match(out var Head, out var Tail)
		? mMaybe.Some(Tail.Reduce(Head, aAggregatorFunc))
		: mStd.cEmpty
	);
	
	[Pure, DebuggerHidden]
	public static t
	Join<t>(
		this tStream<t>? aStream,
		mStd.tFunc<t, t, t> aAggregatorFunc,
		t aDefault
	) => (
		aStream.Match(out var Head, out var Tail)
		? Tail.Reduce(Head, aAggregatorFunc)
		: aDefault
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	Take<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) => (
		(aCount > 0 && aStream.Match(out var Head, out var Tail))
		? Stream(Head, () => Tail.Take(aCount - 1))
		: mStd.cEmpty
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	Skip<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) {
		#if TAIL_RECURSIVE
		mAssert.(aCount >= 0);
		return (
			(aCount > 0 && aStream.Match(out var Head, out var Tail))
			? Tail.Skip(aCount - 1)
			: aStream
		);
		#else
		while (aCount --> 0 && aStream.Match(out var _, out aStream)) { }
		return aStream;
		#endif
	}
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	SkipUntil<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aCond
	) {
		while (aStream.Match(out var Head, out aStream)) {
			if (aCond(Head)) {
				return Stream(Head, aStream);
			}
		}
		return default;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tStream<t>?
	SkipWhile<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aCond
	) => aStream.SkipUntil([DebuggerHidden](a) => !aCond(a));
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	Every<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(Head, () => Tail.Skip(aCount - 1).Every(aCount))
		: Stream<t>()
	);
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	Where<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPredicate
	) {
		#if TAIL_RECURSIVE
		return (
			!aStream.Match(out var Head, out var Tail) ? Stream<t>() :
			aPredicate(Head) ? Stream(Head, [DebuggerHidden]() => Tail.Where(aPredicate)) :
			Tail.Where(aPredicate)
		);
		#else
		while (aStream.Match(out var Head, out aStream)) {
			if (aPredicate(Head)) {
				return Stream(Head, [DebuggerHidden]() => aStream.Where(aPredicate));
			}
		}
		return Stream<t>();
		#endif
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	IsEmpty<t>(
		[NotNullWhen(false)]this tStream<t>? aStream
	) => aStream is null;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryFirst<t>(
		this tStream<t>? aStream
	) => (
		aStream.IsEmpty()
		? mStd.cEmpty
		: mMaybe.Some(aStream._Head)
	);
	
	[Pure, DebuggerHidden]
	public static mMaybe.tMaybe<t>
	TryLast<t>(
		this tStream<t>? aStream
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
		this tStream<tBool>? aStream
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
		this tStream<t>? aStream,
		tInt32 aIndex
	) => aStream.Skip(aIndex).TryFirst();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	Any<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPrefix
	) => aStream.Map(aPrefix).Any();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	All(
		this tStream<tBool>? aStream
	) => !aStream.Map(a => !a).Any();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tBool
	All<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPrefix
	) => aStream.Map(aPrefix).All();
	
	[Pure, DebuggerHidden]
	public static tStream<t>?
	Reverse<t>(
		this tStream<t>? aStream
	) {
		var Result = Stream<t>();
		foreach (var Item in aStream) {
			Result = Stream(Item, Result);
		}
		return Result;
	}
	
	[Pure, DebuggerHidden]
	public static tStream<(t1 _1, t2 _2)>?
	ZipShort<t1, t2>(
		tStream<t1>? a1,
		tStream<t2>? a2
	) => (
		a1.Match(out var Head1, out var Tail1) &&
		a2.Match(out var Head2, out var Tail2)
	)
	? Stream((Head1, Head2), () => ZipShort(Tail1, Tail2))
	: Stream<(t1, t2)>();
	
	[Pure, DebuggerHidden]
	public static tStream<(mMaybe.tMaybe<t1> _1, mMaybe.tMaybe<t2> _2)>?
	ZipExtend<t1, t2>(
		tStream<t1>? a1,
		tStream<t2>? a2
	) {
		if (a1.IsEmpty() && a2.IsEmpty()) {
			return mStd.cEmpty;
		}
		
		var MaybeHead1 = a1.Match(out var Head1, out var Tail1) ? mMaybe.Some(Head1) : mStd.cEmpty;
		var MaybeHead2 = a2.Match(out var Head2, out var Tail2) ? mMaybe.Some(Head2) : mStd.cEmpty;
		return Stream((MaybeHead1, MaybeHead2), () => ZipExtend(Tail1, Tail2));
	}
}
