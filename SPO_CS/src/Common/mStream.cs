//IMPORT mStd.cs
//IMPORT mLazy.cs
//IMPORT mMaybe.cs

#nullable enable

//#define TAIL_RECURSIVE

using System.Diagnostics.CodeAnalysis;

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

[System.Diagnostics.DebuggerStepThrough]
public static class
mStream {
	
	[System.Diagnostics.DebuggerTypeProxy(typeof(mStream.tStream<>.tDebuggerProxy))]
	public sealed class
	tStream<t> {
		internal readonly t _Head = default!;
		internal readonly mLazy.tLazy<tStream<t>?> _Tail;
		
		internal
		tStream(
			t aHead,
			mLazy.tLazy<tStream<t>?> aTail
		) {
			this._Head = aHead;
			this._Tail = aTail;
		}
		
		public tBool
		Equals(
			tStream<t>? a
		) => (
			this.Match(out var Head1, out var Tail1) &&
			a.Match(out var Head2, out var Tail2) &&
			Head1.Equals(Head2) &&
			(Tail1 is null ? Tail2 is null : Tail1.Equals(Tail2))
		);
		
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
			[System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
			public t[] Text => this._Stream.Take(100).ToArrayList().ToArray();
		}
	}
	
	public struct
	tStreamIterator<t> {
		private t _Head;
		private tStream<t>? _Tail;
		
		public
		tStreamIterator(
			tStream<t> aStream
		) {
			this._Head = default!;
			this._Tail = aStream;
		}
		
		public t Current => this._Head;
		
		public bool
		MoveNext(
		) => this._Tail.Match(out this._Head, out this._Tail);
	}
	
	public static tStreamIterator<t>
	GetEnumerator<t>(
		this tStream<t> aStream
	) => new tStreamIterator<t>(aStream);
	
	public static tStream<t>?
	Stream<t>(
		t aHead,
		mStd.tFunc<tStream<t>?> aTailFunc
	) => new tStream<t>(
		aHead,
		mLazy.Lazy(aTailFunc)
	);
	
	public static tStream<t>?
	Stream<t>(
		t aHead,
		tStream<t>? aTail
	) => new tStream<t>(
		aHead,
		mLazy.NonLazy(aTail)
	);
	
	public static tStream<t>?
	Stream<t>(
	) => mStd.cEmpty;
	
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
	
	public static tStream<t>?
	AsStream<t>(
		this t[] a
	) => Stream(a);
	
	public static tStream<tInt32>?
	Nat(
		int aStart
	) => Stream(aStart, () => Nat(aStart+1));
	
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
	
	public static tStream<t>?
	Concat<t>(
		tStream<t>? a1,
		tStream<t>? a2
	) => (
		a1.Match(out var Head, out var Tail)
		? Stream(Head, () => Concat(Tail, a2))
		: a2
	);
	
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
	
	public static tStream<tRes>?
	MapWithIndex<tRes, tElem>(
		this tStream<tElem>? aStream,
		mStd.tFunc<tRes, tInt32, tElem> aMapFunc
	) => aStream.MapWithIndex().Map(a => aMapFunc(a.Index, a.Item));
	
	public static tStream<(tInt32 Index, t Item)>?
	MapWithIndex<t>(
		this tStream<t>? aStream
	) => Zip(Nat(0), aStream);
	
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
	
	public static mMaybe.tMaybe<t>
	TryReduce<t>(
		this tStream<t>? aStream,
		mStd.tFunc<t, t, t> aAggregatorFunc
	) => (
		aStream.Match(out var Head, out var Tail)
		? mMaybe.Some(Tail.Reduce(Head, aAggregatorFunc))
		: mMaybe.None<t>()
	);
	
	public static tStream<t>?
	Flat<t>(
		this tStream<tStream<t>?>? aStreamStream
	) => aStreamStream.Reduce(Stream<t>(), Concat);
	
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
	
	public static tStream<t>?
	Take<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) => (
		(aCount > 0 && aStream.Match(out var Head, out var Tail))
		? Stream(Head, () => Tail.Take(aCount - 1))
		: mStd.cEmpty
	);
	
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
	
	public static tStream<t>?
	SkipWhile<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aCond
	) => aStream.SkipUntil(_ => !aCond(_));
	
	public static tStream<t>?
	Every<t>(
		this tStream<t>? aStream,
		tInt32 aCount
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(Head, () => Tail.Skip(aCount - 1).Every(aCount))
		: Stream<t>()
	);
	
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
	
	public static tBool
	IsEmpty<t>(
		[NotNullWhen(false)]this tStream<t>? aStream
	) => aStream is null;
	
	public static mMaybe.tMaybe<t>
	TryFirst<t>(
		this tStream<t>? aStream
	) => (
		aStream.IsEmpty()
		? mStd.cEmpty
		: mMaybe.Some(aStream._Head)
	);
	
	public static mMaybe.tMaybe<t>
	TryLast<t>(
		this tStream<t>? aStream
	) {
		var Result = mMaybe.None<t>();
		foreach (var Item in aStream) {
			Result = mMaybe.Some(Item);
		}
		return Result;
	}
	
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
	
	public static mMaybe.tMaybe<t>
	TryGet<t>(
		this tStream<t>? aStream,
		tInt32 aIndex
	) => aStream.Skip(aIndex).TryFirst();
	
	public static tBool
	Any<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPrefix
	) => aStream.Map(aPrefix).Any();
	
	public static tBool
	All(
		this tStream<tBool>? aStream
	) => !aStream.Map(_ => !_).Any();
	
	public static tBool
	All<t>(
		this tStream<t>? aStream,
		mStd.tFunc<tBool, t> aPrefix
	) => aStream.Map(aPrefix).All();
	
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
