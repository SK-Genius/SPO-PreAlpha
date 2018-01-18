﻿using tBool = System.Boolean;

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
			(Tail1.IsNull() ? Tail2.IsNull() : Tail1.Equals(Tail2))
		);
		
		override public tBool Equals(object a) => Equals((tList<t>)a);
		override public tText ToString() => $"[{this.Map(a => a.ToString()).Join((a1, a2) => $"{a1}, {a2}")}]";
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
		int aStart = 0
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
	) => Zip(Nat(), aList).Map(_ => aMapFunc(_.Item1, _.Item2));
	
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
		mStd.Assert(aCount >= 0);
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
	) => (
		aList.Match(out var Head, out var Tail)
		? (Head || Tail.Any())
		: false
	);
	
	//================================================================================
	public static tBool
	All(
		this tList<tBool> aList
	//================================================================================
	) => (
		aList.Match(out var Head, out var Tail)
		? (Head && Tail.Any())
		: true
	);
	
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
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mList),
		mTest.Test(
			"tList.ToString()",
			aStreamOut => {
				// TODO: mStd.AssertEq(List<tInt32>().ToString(), "[]"); ??? Maybe support static extension ToText() ?
				mStd.AssertEq(List(1).ToString(), "[1]");
				mStd.AssertEq(List(1, 2).ToString(), "[1, 2]");
				mStd.AssertEq(List(1, 2, 3).ToString(), "[1, 2, 3]");
				mStd.AssertEq(
					Nat(1).Take(4).ToString(),
					"[1, 2, 3, 4]"
				);
			}
		),
		mTest.Test(
			"tList.Equals()",
			aStreamOut => {
				mStd.AssertEq(List<tInt32>(), List<tInt32>());
				mStd.AssertEq(List(1), List(1));
				mStd.AssertNotEq(List(1), List<tInt32>());
				mStd.AssertNotEq(List(1), List(2));
				mStd.AssertNotEq(List(1), List(1, 2));
				mStd.AssertEq(
					Nat(1).Take(4),
					List(1, 2, 3, 4)
				);
			}
		),
		mTest.Test(
			"Concat()",
			aStreamOut => {
				mStd.AssertEq(Concat(List(1, 2), List(3, 4)), List(1, 2, 3, 4));
				mStd.AssertEq(Concat(List(1, 2), List<tInt32>()), List(1, 2));
				mStd.AssertEq(Concat(List<tInt32>(), List(3, 4)), List(3, 4));
				mStd.AssertEq(Concat(List<tInt32>(), List<tInt32>()), List<tInt32>());
			}
		),
		mTest.Test(
			"Map1()",
			aStreamOut => {
				mStd.AssertEq(List(1, 2, 3, 4).Map(a => a*a), List(1, 4, 9, 16));
				mStd.AssertEq(List<tInt32>().Map(a => a*a), List<tInt32>());
			}
		),
		mTest.Test(
			"MapWithIndex()",
			aStreamOut => {
				mStd.AssertEq(
					List(1, 2, 3, 4).MapWithIndex((i, a) => (i, a*a)),
					List((0, 1), (1, 4), (2, 9), (3, 16))
				);
			}
		),
		mTest.Test(
			"Reduce()",
			aStreamOut => {
				mStd.AssertEq(List(1, 2, 3, 4).Reduce(0, (a1, a2) => a1+a2), 10);
				mStd.AssertEq(List(1).Reduce(0, (a1, a2) => a1+a2), 1);
				mStd.AssertEq(List<tInt32>().Reduce(0, (a1, a2) => a1+a2), 0);
			}
		),
		mTest.Test(
			"Join()",
			aStreamOut => {
				mStd.AssertEq(List("a", "b", "c", "d").Join((a1, a2) => $"{a1},{a2}"), "a,b,c,d");
				mStd.AssertEq(List("a").Join((a1, a2) => $"{a1},{a2}"), "a");
				mStd.AssertEq(List<tText>().Join((a1, a2) => $"{a1},{a2}"), "");
			}
		),
		mTest.Test(
			"Take()",
			aStreamOut => {
				mStd.AssertEq(List(1, 2, 3, 4).Take(3), List(1, 2, 3));
				mStd.AssertEq(List(1, 2, 3).Take(4), List(1, 2, 3));
				mStd.AssertEq(List<tInt32>().Take(4), List<tInt32>());
				mStd.AssertEq(List(1, 2, 3).Take(0), List<tInt32>());
				mStd.AssertEq(List(1, 2, 3).Take(-1), List<tInt32>());
			}
		),
		mTest.Test(
			"Skip()",
			aStreamOut => {
				mStd.AssertEq(List(1, 2, 3, 4).Skip(3), List(4));
				mStd.AssertEq(List(1, 2, 3).Skip(4), List<tInt32>());
				mStd.AssertEq(List<tInt32>().Skip(4), List<tInt32>());
				mStd.AssertEq(List(1, 2, 3).Skip(0), List(1, 2, 3));
			}
		),
		mTest.Test(
			"IsEmpty()",
			aStreamOut => {
				mStd.Assert(List<tInt32>().IsEmpty());
				mStd.AssertNot(List(1).IsEmpty());
				mStd.AssertNot(List(1, 2).IsEmpty());
				
				mStd.AssertNot(List<tInt32>() == new tList<int>());
			}
		),
		mTest.Test(
			"Any()",
			aStreamOut => {
				mStd.AssertNot(List<tBool>().Any());
				mStd.AssertNot(List(false).Any());
				mStd.Assert(List(true).Any());
				mStd.AssertNot(List(false, false, false).Any());
				mStd.Assert(List(true, true, true).Any());
				mStd.Assert(List(false, false, true, false).Any());
				mStd.Assert(List(1, 2, 3, 4).Map(a => a == 2).Any());
				mStd.AssertNot(List(1, 3, 4).Map(a => a == 2).Any());
			}
		),
		mTest.Test(
			"Every()",
			aStreamOut => {
				mStd.AssertEq(List(1, 2, 3, 4, 5).Every(2), List(1, 3, 5));
				mStd.AssertEq(List(1, 2).Every(2), List(1));
				mStd.AssertEq(List<tInt32>().Every(2), List<tInt32>());
				mStd.AssertEq(List(1, 2, 3).Every(1), List(1, 2, 3));
			}
		)
	);
	
	#endregion
}