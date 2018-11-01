//IMPORT mStd.cs
//#define TAIL_RECURSIVE

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

public static class mStream {
	
	public sealed class tStream<t> {
		internal t _Head;
		internal mStd.tLazy<tStream<t>> _Tail;
		
		public t Head => this.First();
		public tStream<t> Tail => this.Skip(1);
		
		//================================================================================
		public tBool
		Equals(
			tStream<t> a
		//================================================================================
		) => (
			this.Match(out var Head1, out var Tail1) &&
			a.Match(out var Head2, out var Tail2) &&
			Head1.Equals(Head2) &&
			(Tail1 is null ? Tail2 is null : Tail1.Equals(Tail2))
		);
		
		override public tBool Equals(object a) => this.Equals(a as tStream<t>);
	}
	
	//================================================================================
	public static tStream<t>
	Stream<t>(
		t aHead,
		mStd.tFunc<tStream<t>> aTailFunc
	//================================================================================
	) {
		var Result = new tStream<t> {
			_Head = aHead
		};
		Result._Tail.Func = aTailFunc;
		return Result;
	}
	
	//================================================================================
	public static tStream<t>
	Stream<t>(
		t aHead,
		tStream<t> aTail
	//================================================================================
	) {
		var Result = new tStream<t> {
			_Head = aHead
		};
		Result._Tail.Value = aTail;
		return Result;
	}
	
	//================================================================================
	public static tStream<t>
	Stream<t>(
		params t[] aStream
	//================================================================================
	) {
		var Result = (tStream<t>)null;
		for (var I = aStream.Length; I --> 0;) {
			Result = Stream(aStream[I], Result);
		}
		return Result;
	}
	
	//================================================================================
	public static tStream<tInt32>
	Nat(
		int aStart
	//================================================================================
	) => Stream(aStart, () => Nat(aStart+1));
	
	//================================================================================
	public static tBool
	Match<t>(
		this tStream<t> aStream,
		out t aHead,
		out tStream<t> aTail
	//================================================================================
	) {
		if (aStream.IsEmpty()) {
			aHead = default;
			aTail = default;
			return false;
		}
		aHead = aStream._Head;
		aTail = aStream._Tail.Value;
		return true;
	}
	
	//================================================================================
	public static tStream<t>
	Concat<t>(
		tStream<t> a1,
		tStream<t> a2
	//================================================================================
	) => (
		a1.Match(out var Head, out var Tail)
		? Stream(Head, () => Concat(Tail, a2))
		: a2
	);
	
	//================================================================================
	public static tStream<tRes>
	Map<tRes, tElem>(
		this tStream<tElem> aStream,
		mStd.tFunc<tRes, tElem> aMapFunc
	//================================================================================
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(aMapFunc(Head), () => Tail.Map(aMapFunc))
		: Stream<tRes>()
	);
	
	//================================================================================
	public static tStream<tRes>
	MapWithIndex<tRes, tElem>(
		this tStream<tElem> aStream,
		mStd.tFunc<tRes, tInt32, tElem> aMapFunc
	//================================================================================
	) => Zip(Nat(0), aStream).Map(a => aMapFunc(a._1, a._2));
	
	//================================================================================
	public static tRes
	Reduce<tRes, tElem>(
		this tStream<tElem> aStream,
		tRes aInitialAgregate,
		mStd.tFunc<tRes, tRes, tElem> aAgregatorFunc
	//================================================================================
	#if TAIL_RECURSIVE
	) => (
		aStream.Match(out var Head, out var Tail)
		? Tail.Reduce(aAgregatorFunc(aInitialAgregate, Head), aAgregatorFunc)
		: aInitialAgregate
	);
	#else
	){
		var Result = aInitialAgregate;
		while (aStream.Match(out var Head, out aStream)) {
			Result = aAgregatorFunc(Result, Head);
		}
		return Result;
	}
	#endif
	
	//================================================================================
	public static tStream<t>
	Flat<t>(
		this tStream<tStream<t>> aStreamStream
	//================================================================================
	) => aStreamStream.Reduce(Stream<t>(), Concat);
	
	//================================================================================
	public static t
	Join<t>(
		this tStream<t> aStream,
		mStd.tFunc<t, t, t> aAgregatorFunc
	//================================================================================
	) => (
		aStream.Match(out var Head, out var Tail)
		? Tail.Reduce(Head, aAgregatorFunc)
		: default
	);
	
	//================================================================================
	public static tStream<t>
	Take<t>(
		this tStream<t> aStream,
		tInt32 aCount
	//================================================================================
	) => (
		(aCount > 0 && aStream.Match(out var Head, out var Tail))
		? Stream(Head, () => Tail.Take(aCount-1))
		: Stream<t>()
	);
	
	//================================================================================
	public static tStream<t>
	Skip<t>(
		this tStream<t> aStream,
		tInt32 aCount
	//================================================================================
	) {
		#if TAIL_RECURSIVE
		mDebug.Assert(aCount >= 0);
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
	
	//================================================================================
	public static tStream<t>
	Every<t>(
		this tStream<t> aStream,
		tInt32 aCount
	//================================================================================
	) => (
		aStream.Match(out var Head, out var Tail)
		? Stream(Head, () => Tail.Skip(aCount - 1).Every(aCount))
		: Stream<t>()
	);
	
	//================================================================================
	public static tStream<t>
	Where<t>(
		this tStream<t> aStream,
		mStd.tFunc<tBool, t> aPredicate
	//================================================================================
	) => (
		!aStream.Match(out var Head, out var Tail) ? Stream<t>() :
		aPredicate(Head) ? Stream(Head, () => Tail.Where(aPredicate)) :
		Tail.Where(aPredicate)
	);
	
	//================================================================================
	public static tBool
	IsEmpty<t>(
		this tStream<t> aStream
	//================================================================================
	) => aStream is null;
	
	//================================================================================
	public static t
	First<t>(
		this tStream<t> aStream
	//================================================================================
	) => aStream._Head;
	
	//================================================================================
	public static t
	Last<t>(
		this tStream<t> aStream
	//================================================================================
	) {
		var Result = aStream.First();
		while (aStream.Match(out var Head, out aStream)) {
			Result = Head;
		}
		return Result;
	}
	
	//================================================================================
	public static tBool
	Any(
		this tStream<tBool> aStream
	//================================================================================
	) => aStream.Match(out var Head, out var Tail) && (Head || Tail.Any());
	
	//================================================================================
	public static tBool
	Any<t>(
		this tStream<t> aStream,
		mStd.tFunc<tBool, t> aPrefix
	//================================================================================
	) => aStream.Map(aPrefix).Any();
	
	//================================================================================
	public static tBool
	All(
		this tStream<tBool> aStream
	//================================================================================
	) => !aStream.Map(_ => !_).Any();
	
	//================================================================================
	public static tBool
	All<t>(
		this tStream<t> aStream,
		mStd.tFunc<tBool, t> aPrefix
	//================================================================================
	) => aStream.Map(aPrefix).All();
	
	//================================================================================
	public static tStream<t>
	Reverse<t>(
		this tStream<t> aStream
	//================================================================================
	) {
		var Result = Stream<t>();
		var Tail = aStream;
		while (Tail.Match(out var Head, out Tail)) {
			Result = Stream(Head, Result);
		}
		return Result;
	}
	
	//================================================================================
	public static tStream<(t1 _1, t2 _2)>
	Zip<t1, t2>(
		tStream<t1> a1,
		tStream<t2> a2
	//================================================================================
	) => (
		(
			a1.Match(out var Head1, out var Tail1) &&
			a2.Match(out var Head2, out var Tail2)
		)
		? Stream((Head1, Head2), () => Zip(Tail1, Tail2))
		: Stream<(t1, t2)>()
	);
}
