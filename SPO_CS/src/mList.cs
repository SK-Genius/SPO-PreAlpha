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

public static class mList {
	
	public sealed class tList<t> {
		internal t _Head;
		internal mStd.tLazy<tList<t>> _Tail;
		
		//================================================================================
		public tBool
		Equals(
			tList<t> a
		//================================================================================
		) => (
			this.Match(out var Head1, out var Tail1) &&
			a.Match(out var Head2, out var Tail2) &&
			Head1.Equals(Head2) &&
			(Tail1 is null ? Tail2 is null : Tail1.Equals(Tail2))
		);
		
		override public tBool Equals(object a) => Equals((tList<t>)a);
		override public tText ToString() => this.Map(a => a.ToString()).Join((a1, a2) => $"{a1} \n{a2}");
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		t aHead,
		mStd.tFunc<tList<t>> aTailFunc
	//================================================================================
	) {
		var Result = new tList<t> {
			_Head = aHead
		};
		Result._Tail.Func = aTailFunc;
		return Result;
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		t aHead,
		tList<t> aTail
	//================================================================================
	) {
		var Result = new tList<t> {
			_Head = aHead
		};
		Result._Tail.Value = aTail;
		return Result;
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		params t[] aList
	//================================================================================
	) {
		var Result = (tList<t>)null;
		for (var I = aList.Length; I --> 0;) {
			Result = List(aList[I], Result);
		}
		return Result;
	}
	
	//================================================================================
	public static tList<tInt32>
	Nat(
		int aStart
	//================================================================================
	) => List(aStart, () => Nat(aStart+1));
	
	//================================================================================
	public static tBool
	Match<t>(
		this tList<t> aList,
		out t aHead,
		out tList<t> aTail
	//================================================================================
	) {
		if (aList.IsEmpty()) {
			aHead = default;
			aTail = default;
			return false;
		}
		aHead = aList._Head;
		aTail = aList._Tail.Value;
		return true;
	}
	
	//================================================================================
	public static tList<t>
	Concat<t>(
		tList<t> a1,
		tList<t> a2
	//================================================================================
	) => (
		a1.Match(out var Head, out  var Tail)
		? List(Head, () => Concat(Tail, a2))
		: a2
	);
	
	//================================================================================
	public static tList<tRes>
	Map<tRes, tElem>(
		this tList<tElem> aList,
		mStd.tFunc<tRes, tElem> aMapFunc
	//================================================================================
	) => (
		aList.Match(out var Head, out var Tail)
		? List(aMapFunc(Head), () => Tail.Map(aMapFunc))
		: List<tRes>()
	);
	
	//================================================================================
	public static tList<tRes>
	MapWithIndex<tRes, tElem>(
		this tList<tElem> aList,
		mStd.tFunc<tRes, tInt32, tElem> aMapFunc
	//================================================================================
	) => Zip(Nat(0), aList).Map(_ => aMapFunc(_.Item1, _.Item2));
	
	//================================================================================
	public static tRes
	Reduce<tRes, tElem>(
		this tList<tElem> aList,
		tRes aInitialAgregate,
		mStd.tFunc<tRes, tRes, tElem> aAgregatorFunc
	//================================================================================
	) => (
		aList.Match(out var Head, out var Tail)
		? Tail.Reduce(aAgregatorFunc(aInitialAgregate, Head), aAgregatorFunc)
		: aInitialAgregate
	);
	
	//================================================================================
	public static tList<t>
	Flat<t>(
		this tList<tList<t>> aListList
	//================================================================================
	) => aListList.Reduce(List<t>(), Concat);
	
	//================================================================================
	public static t
	Join<t>(
		this tList<t> aList,
		mStd.tFunc<t, t, t> aAgregatorFunc
	//================================================================================
	) => (
		aList.Match(out var Head, out var Tail)
		? Tail.Reduce(Head, aAgregatorFunc)
		: default
	);
	
	//================================================================================
	public static tList<t>
	Take<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) => (
		(aCount > 0 && aList.Match(out var Head, out var Tail))
		? List(Head, () => Tail.Take(aCount-1))
		: List<t>()
	);
	
	//================================================================================
	public static tList<t>
	Skip<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) {
		#if DEBUG
			mStd.Assert(aCount >= 0);
		#endif
		return (
			(aCount > 0 && aList.Match(out var Head, out var Tail))
			? Tail.Skip(aCount - 1)
			: aList
		);
	}
	
	//================================================================================
	public static tList<t>
	Every<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) => (
		aList.Match(out var Head, out var Tail)
		? List(Head, () => Tail.Skip(aCount - 1).Every(aCount))
		: List<t>()
	);
	
	//================================================================================
	public static tList<t>
	Where<t>(
		this tList<t> aList,
		mStd.tFunc<tBool, t> aPredicate
	//================================================================================
	) => (
		!aList.Match(out var Head, out var Tail) ? List<t>() :
		aPredicate(Head) ? List(Head, () => Tail.Where(aPredicate)) :
		Tail.Where(aPredicate)
	);
	
	//================================================================================
	public static tBool
	IsEmpty<t>(
		this tList<t> aList
	//================================================================================
	) => aList == List<t>();
	
	//================================================================================
	public static t
	First<t>(
		this tList<t> aList
	//================================================================================
	) => aList._Head;
	
	//================================================================================
	public static tBool
	Any(
		this tList<tBool> aList
	//================================================================================
	) => aList.Match(out var Head, out var Tail) && (Head || Tail.Any());
	
	//================================================================================
	public static tBool
	Any<t>(
		this tList<t> aList,
		mStd.tFunc<tBool, t> aPrefix
	//================================================================================
	) => aList.Map(aPrefix).Any();
	
	//================================================================================
	public static tBool
	All(
		this tList<tBool> aList
	//================================================================================
	) => !aList.Map(_ => !_).Any();
	
	//================================================================================
	public static tBool
	All<t>(
		this tList<t> aList,
		mStd.tFunc<tBool, t> aPrefix
	//================================================================================
	) => aList.Map(aPrefix).All();
	
	//================================================================================
	public static tList<t>
	Reverse<t>(
		this tList<t> aList
	//================================================================================
	) {
		var Result = List<t>();
		var Tail = aList;
		while (Tail.Match(out var Head, out Tail)) {
			Result = List(Head, Result);
		}
		return Result;
	}
	
	//================================================================================
	public static tList<(t1, t2)>
	Zip<t1, t2>(
		tList<t1> a1,
		tList<t2> a2
	//================================================================================
	) => (
		(
			a1.Match(out var Head1, out var Tail1) &&
			a2.Match(out var Head2, out var Tail2)
		)
		? List((Head1, Head2), () => Zip(Tail1, Tail2))
		: List<(t1, t2)>()
	);
}
