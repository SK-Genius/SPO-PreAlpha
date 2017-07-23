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
		internal tList<t> _Tail;
		internal mStd.tFunc<mStd.tMaybe<t, mStd.tVoid>> _TryNextFunc;
		
		//================================================================================
		public tBool Equals(
			tList<t> a
		//================================================================================
		) => (
			!a.IsNull() &&
			this.Match(out var Head1, out var Tail1) &&
			a.Match(out var Head2, out var Tail2) &&
			Head1.Equals(Head2) &&
			(Tail1.IsNull() ? Tail2.IsNull() : Tail1.Equals(Tail2))
		);
		
		override public tBool Equals(object a) => Equals(a as tList<t>);
		override public tText ToString() => $"[{this.Map(a => a.ToString()).Join((a1, a2) => $"{a1}, {a2}")}]";
		public static tBool operator==(tList<t> a1, tList<t> a2) => a1.IsNull() ? a2.IsNull() : a1.Equals(a2);
		public static tBool operator!=(tList<t> a1, tList<t> a2) => !(a1 == a2);
	}
	
	//================================================================================
	public static tList<t>
	LasyList<t>(
		mStd.tFunc<mStd.tMaybe<t, mStd.tVoid>> aTryNextFunc = null
	//================================================================================
	) {
		if (
			!aTryNextFunc.IsNull() &&
			aTryNextFunc().Match(out t Head)
		) {
			return new tList<t>{
				_Head = Head,
				_TryNextFunc = aTryNextFunc
			};
		}
		
		return null;
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		t aHead,
		tList<t> aTail
	//================================================================================
	) => new tList<t>{_Head = aHead, _Tail = aTail};
	
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
	public static tList<t>
	Concat<t>(
		tList<t> a1,
		tList<t> a2
	//================================================================================
	) => LasyList<t>(
		() => {
			if (a1.Match(out var Head, out a1)) {
				return mStd.OK(Head);
			} else if (a2.Match(out Head, out a2)) {
				return mStd.OK(Head);
			} else {
				return mStd.Fail();
			}
		}
	);
	
	//================================================================================
	public static tBool
	Match<t>(
		this tList<t> aList,
		out t aHead,
		out tList<t> aTail
	//================================================================================
	) {
		if (aList.IsNull()) {
			aHead = default(t);
			aTail = null;
			return false;
		}
		
		if (!aList._TryNextFunc.IsNull()) {
			aList._Tail = LasyList(aList._TryNextFunc);
			aList._TryNextFunc = null;
		}
		
		aHead = aList._Head;
		aTail = aList._Tail;
		return true;
	}
	
	//================================================================================
	public static tList<tRes>
	Map<tRes, tElem>(
		this tList<tElem> aList,
		mStd.tFunc<tRes, tElem> aMapFunc
	//================================================================================
	) => aList.MapWithIndex((aIndex, aElem) => aMapFunc(aElem));
	
	//================================================================================
	public static tList<tRes>
	MapWithIndex<tRes, tElem>(
		this tList<tElem> aList,
		mStd.tFunc<tRes, tInt32, tElem> aMapFunc
	//================================================================================
	) {
		var RestList = aList;
		var Index = (tInt32?)-1;
		return LasyList<tRes>(
			() => {
				if (RestList.Match(out var Head, out RestList)) {
					Index += 1;
					return mStd.OK(aMapFunc(Index.Value, Head));
				} else {
					return mStd.Fail();
				}
			}
		);
	}
	
	//================================================================================
	public static tList<tRes>
	Map<tRes, tElem1, tElem2>(
		this tList<(tElem1, tElem2)> aList,
		mStd.tFunc<tRes, tElem1, tElem2> aMapFunc
	//================================================================================
	) => aList.MapWithIndex((aIndex, aElem1, aElem2) => aMapFunc(aElem1, aElem2));
	
	//================================================================================
	public static tList<tRes>
	MapWithIndex<tRes, tElem1, tElem2>(
		this tList<(tElem1, tElem2)> aList,
		mStd.tFunc<tRes, tInt32, tElem1, tElem2> aMapFunc
	//================================================================================
	) {
		var RestList = aList;
		var Index = (tInt32?)0;
		return LasyList<tRes>(
			() => {
				if (RestList.Match(out var Head, out RestList)) {
					Index += 1;
					return mStd.OK(aMapFunc(Index.Value - 1, Head.Item1, Head.Item2));
				} else {
					return mStd.Fail();
				}
			}
		);
	}
	
	//================================================================================
	public static tRes
	Reduce<tRes, tElem>(
		this tList<tElem> aList,
		tRes aInitialAgregate,
		mStd.tFunc<tRes, tRes, tElem> aAgregatorFunc
	//================================================================================
	) {
		var Agregate = aInitialAgregate;
		var RestList = aList;
		while (RestList.Match(out var Head, out RestList)) {
			Agregate = aAgregatorFunc(Agregate, Head);
		}
		return Agregate;
	}
	
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
	) => aList.Match(out var Head, out var Tail) ? Tail.Reduce(Head, aAgregatorFunc) : default(t);
	
	//================================================================================
	public static tList<t>
	Take<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) {
		var Result = List<t>();
		var RestList = aList;
		while (aCount > 0 && RestList.Match(out var Head, out RestList)) {
			Result = Concat(Result, List(Head));
			aCount -= 1;
		}
		return Result;
	}
	
	//================================================================================
	public static tList<t>
	Skip<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) {
		var RestList = aList;
		while (aCount > 0 && RestList.Match(out var _, out RestList)) {
			aCount -= 1;
		}
		return RestList;
	}
	
	//================================================================================
	public static tList<t>
	Every<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) {
		var Result = List<t>();
		var RestList = aList;
		while (RestList.Match(out var Head, out RestList)) {
			Result = Concat(Result, List(Head));
			RestList = RestList.Skip(aCount - 1);
		}
		return Result;
	}
	
	//================================================================================
	public static tList<t>
	Where<t>(
		this tList<t> aList,
		mStd.tFunc<tBool, t> aPredicate
	//================================================================================
	) {
		var Result = List<t>();
		var RestList = aList;
		while (RestList.Match(out var Head, out RestList)) {
			if (aPredicate(Head)) {
				Result = Concat(Result, List(Head));
			}
		}
		return Result;
	}
	
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
	) {
		var RestList = aList;
		while (RestList.Match(out var Head, out RestList)) {
			if (Head) {
				return true;
			}
		}
		return false;
	}
	
	//================================================================================
	public static tList<t>
	Reverse<t>(
		this tList<t> aList
	//================================================================================
	) {
		var Result = List<t>();
		var RestList = aList;
		while (RestList.Match(out var Head, out RestList)) {
			Result = List(Head, Result);
		}
		return Result;
	}
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mList),
		mTest.Test(
			"tList.ToString()",
			aStreamOut => {
				// TODO: AssertEq(List<tInt32>().ToString(), "[]"); ???
				mStd.AssertEq(List(1).ToString(), "[1]");
				mStd.AssertEq(List(1, 2).ToString(), "[1, 2]");
				mStd.AssertEq(List(1, 2, 3).ToString(), "[1, 2, 3]");
				var LasyListCounter = (tInt32?)0;
				mStd.AssertEq(
					LasyList<tInt32>(
						() => {
							LasyListCounter += 1;
							if (LasyListCounter.Value < 5) {
								return mStd.OK(LasyListCounter.Value);
							} else {
								return mStd.Fail();
							}
						}
					).ToString(),
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
				var LasyListCounter = (tInt32?)0;
				mStd.AssertEq(
					LasyList<tInt32>(
						() => {
							LasyListCounter += 1;
							if (LasyListCounter.Value < 5) {
								return mStd.OK(LasyListCounter.Value);
							} else {
								return mStd.Fail();
							}
						}
					),
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
				mStd.AssertEq(List(1, 2, 3).Skip(-1), List(1, 2, 3));
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
				mStd.AssertEq(List(1, 2, 3).Every(0), List(1, 2, 3));
				mStd.AssertEq(List(1, 2, 3).Every(-1), List(1, 2, 3));
			}
		)
	);
	
	#endregion
}
