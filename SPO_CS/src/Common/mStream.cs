//IMPORT mStd.cs
//IMPORT mLazy.cs
//IMPORT mMaybe.cs

#nullable enable

//#define TAIL_RECURSIVE

using System.Diagnostics.CodeAnalysis;

[DebuggerStepThrough]
public static class
mStream {
	
	[DebuggerStepThrough]
	[DebuggerTypeProxy(typeof(tStream<>.tDebuggerProxy))]
	public sealed class
	tStream<t> {
		internal readonly t _Head = default!;
		internal readonly mLazy.tLazy<tStream<t>?> _Tail;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal
		tStream(
			t aHead,
			mLazy.tLazy<tStream<t>?> aTail
		) {
			this._Head = aHead;
			this._Tail = aTail;
		}
		
		[Pure]
		public tBool
		Equals(
			tStream<t>? a
		) => (
			this.Match(out var Head1, out var Tail1) &&
			a.Match(out var Head2, out var Tail2) &&
			Head1.Equals(Head2) &&
			(Tail1 is null ? Tail2 is null : Tail1.Equals(Tail2))
		);
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override tBool
		Equals(
			object? a
		) => this.Equals((tStream<t>?)a);
		
		public static
		implicit operator tStream<t>?(
			mStd.tEmpty _
		) => null;
		
		private struct tDebuggerProxy {
			private readonly tStream<t> _Stream;
			
			public tDebuggerProxy(tStream<t> a) { this._Stream = a; }
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public t[] Text {
				get {
					var Count = this._Stream.Take(100).Reduce(0, (a, _) => a + 1);
					return this._Stream.Take(100).MapWithIndex(
						(aIndex, aItem) => (Index: aIndex, Value: aItem)
					).Reduce(
						new t[Count],
						(aArray, a) => {
							aArray[a.Index] = a.Value;
							return aArray;
						}
					);
				}
			}
		}
	}
	
	[DebuggerStepThrough]
	public struct
	tStreamIterator<t> {
		private t _Head;
		private tStream<t>? _Tail;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public
		tStreamIterator(
			tStream<t> aStream
		) {
			this._Head = default!;
			this._Tail = aStream;
		}
		
		public t Current => this._Head;
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool
		MoveNext(
		) => this._Tail.Match(out this._Head, out this._Tail);
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStreamIterator<t>
	GetEnumerator<t>(
		this tStream<t> aStream
	) => new(aStream);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<t>?
	Stream<t>(
		t aHead,
		mStd.tFunc<tStream<t>?> aTailFunc
	) => new(
		aHead,
		mLazy.Lazy(aTailFunc)
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<t>?
	Stream<t>(
		t aHead,
		tStream<t>? aTail
	) => new(
		aHead,
		aTail
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<t>?
	Stream<t>(
	) => mStd.cEmpty;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<t>?
	AsStream<t>(
		this t[] a
	) => Stream(a);
	
	[Pure]
	public static tStream<tInt32>?
	Nat(
		int aStart
	) => Stream(aStart, () => Nat(aStart+1));
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	
	[Pure]
	public static tStream<t>?
	Concat<t>(
		tStream<t>? a1,
		tStream<t>? a2
	) => (
		a1.Match(out var Head, out var Tail)
		? Stream(Head, () => Concat(Tail, a2))
		: a2
	);
	
	[Pure]
	[return: NotNullIfNotNull("aStream")]
	public static tStream<tRes>?
	Map<tRes, tElem>(
		this tStream<tElem>? aStream,
		mStd.tFunc<tRes, tElem> aMapFunc
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(aMapFunc(Head), () => Tail.Map(aMapFunc))
		: Stream<tRes>()
	);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<tRes>?
	MapWithIndex<tRes, tElem>(
		this tStream<tElem>? aStream,
		mStd.tFunc<tRes, tInt32, tElem> aMapFunc
	) => aStream.MapWithIndex().Map(a => aMapFunc(a.Index, a.Item));
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<(tInt32 Index, t Item)>?
	MapWithIndex<t>(
		this tStream<t>? aStream
	) => Zip(Nat(0), aStream);
	
	[Pure]
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
	
	[Pure]
	public static mMaybe.tMaybe<t>
	TryReduce<t>(
		this tStream<t>? aStream,
		mStd.tFunc<t, t, t> aAggregatorFunc
	) => (
		aStream.Match(out var Head, out var Tail)
		? mMaybe.Some(Tail.Reduce(Head, aAggregatorFunc))
		: mStd.cEmpty
	);
	
	[Pure]
	public static tStream<t>?
	Flat<t>(
		this tStream<tStream<t>?>? aStreamStream
	) => aStreamStream.Reduce(Stream<t>(), Concat);
	
	[Pure]
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
	
	[Pure]
	public static tStream<t>?
	Take<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) => (
		(aCount > 0 && aStream.Match(out var Head, out var Tail))
		? Stream(Head, () => Tail.Take(aCount - 1))
		: mStd.cEmpty
	);
	
	[Pure]
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
	
	[Pure]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tStream<t>?
	SkipWhile<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aCond
	) => aStream.SkipUntil(_ => !aCond(_));
	
	[Pure]
	public static tStream<t>?
	Every<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(Head, () => Tail.Skip(aCount - 1).Every(aCount))
		: Stream<t>()
	);
	
	[Pure]
	public static tStream<t>?
	Where<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPredicate
	) {
		#if TAIL_RECURSIVE
		return (
			!aStream.Match(out var Head, out var Tail) ? Stream<t>() :
			aPredicate(Head) ? Stream(Head, () => Tail.Where(aPredicate)) :
			Tail.Where(aPredicate)
		);
		#else
		while (aStream.Match(out var Head, out aStream)) {
			if (aPredicate(Head)) {
				return Stream(Head, () => aStream.Where(aPredicate));
			}
		}
		return Stream<t>();
		#endif
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tBool
	IsEmpty<t>(
		[NotNullWhen(false)]this tStream<t>? aStream
	) => aStream is null;
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static mMaybe.tMaybe<t>
	TryFirst<t>(
		this tStream<t>? aStream
	) => (
		aStream.IsEmpty()
		? mStd.cEmpty
		: mMaybe.Some(aStream._Head)
	);
	
	[Pure]
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
	
	[Pure]
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
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static mMaybe.tMaybe<t>
	TryGet<t>(
		this tStream<t>? aStream,
		tInt32 aIndex
	) => aStream.Skip(aIndex).TryFirst();
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tBool
	Any<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPrefix
	) => aStream.Map(aPrefix).Any();
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tBool
	All(
		this tStream<tBool>? aStream
	) => !aStream.Map(_ => !_).Any();
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tBool
	All<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPrefix
	) => aStream.Map(aPrefix).All();
	
	[Pure]
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
	
	[Pure]
	public static tStream<(t1 _1, t2 _2)>?
	Zip<t1, t2>(
		tStream<t1>? a1,
		tStream<t2>? a2
	) => (
		(
			a1.Match(out var Head1, out var Tail1) &&
			a2.Match(out var Head2, out var Tail2)
		)
		? Stream((Head1, Head2), () => Zip(Tail1, Tail2))
		: Stream<(t1, t2)>()
	);
}
