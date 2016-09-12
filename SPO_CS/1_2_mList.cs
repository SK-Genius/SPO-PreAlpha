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

	public class tList<t> {
		internal t _Head;
		internal tList<t> _Tail;
		internal mStd.tFunc<mStd.tMaybe<t>> _TryNextFunc;
		
		//================================================================================
		public tBool Equals(
			tList<t> a
		//================================================================================
		) {
			t Head1;
			t Head2;
			tList<t> Tail1;
			tList<t> Tail2;
			return (
				!a.IsNull() &&
				this.MATCH(out Head1, out Tail1) &&
				a.MATCH(out Head2, out Tail2) &&
				Head1.Equals(Head2) &&
				(Tail1.IsNull() ? Tail2.IsNull() : Tail1.Equals(Tail2))
			);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tList<t>); }
		public override tText ToString() { return "["+this.Map(a => a.ToString()).Join((a1, a2) => a1+", "+a2)+"]"; }
		public static tBool operator==(tList<t> a1, tList<t> a2) { return a1.Equals(a2); }
		public static tBool operator!=(tList<t> a1, tList<t> a2) { return !a1.Equals(a2); }
	}
	
	//================================================================================
	public static tList<t>
	LasyList<t>(
		mStd.tFunc<mStd.tMaybe<t>> aTryNextFunc = null
	//================================================================================
	) {
		t Head;
		if (!aTryNextFunc.IsNull() &&
			aTryNextFunc().MATCH(out Head) == true
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
	//================================================================================
	) {
		return null;
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		t a
	//================================================================================
	) {
		return List(a, List<t>());
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		t aHead,
		tList<t> aTail
	//================================================================================
	) {
		return new tList<t>{_Head = aHead, _Tail = aTail};
	}
	
	//================================================================================
	public static tList<t>
	List<t>(
		t a,
		params t[] aList
	//================================================================================
	) {
		var Result = List<t>();
		for (var I = aList.Length; I --> 0;) {
			Result = List(aList[I], Result);
		}
		return List(a, Result);
	}
	
	//================================================================================
	public static tList<t>
	Concat<t>(
		tList<t> a1,
		tList<t> a2
	//================================================================================
	) {
		t Head;
		tList<t> Tail;
		return a1.MATCH(out Head, out Tail) ? List(Head, Concat(Tail, a2)) : a2;
	}
	
	//================================================================================
	public static tBool
	MATCH<t>(
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
	) {
		// TODO: make it lasy (use LasyList)
		var ResultList = List<tRes>();
		tElem Head;
		var RestList = aList;
		while (RestList.MATCH(out Head, out RestList)) {
			ResultList = Concat(ResultList, List(aMapFunc(Head)));
		}
		return ResultList;
	}

	//================================================================================
	public static tRes
	Reduce<tRes, tElem>(
		this tList<tElem> aList,
		tRes aInitialAgregate,
		mStd.tFunc<tRes, tRes, tElem> aAgregatorFunc
	//================================================================================
	) {
		tElem Head;
		var Agregate = aInitialAgregate;
		var RestList = aList;
		while (RestList.MATCH(out Head, out RestList)) {
			Agregate = aAgregatorFunc(Agregate, Head);
		}
		return Agregate;
	}
	
	//================================================================================
	public static tList<t>
	Flat<t>(
		this tList<tList<t>> aListList
	//================================================================================
	) {
		// TODO: make it lasy
		return aListList.Reduce(List<t>(), (aSum, a) => Concat(aSum, a));
	}
	
	//================================================================================
	public static t
	Join<t>(
		this tList<t> aList,
		mStd.tFunc<t, t, t> aAgregatorFunc
	//================================================================================
	) {
		t Head;
		tList<t> Tail;
		aList.MATCH(out Head, out Tail);
		return Tail.Reduce(Head, aAgregatorFunc);
	}
	
	//================================================================================
	public static tList<t>
	Take<t>(
		this tList<t> aList,
		tInt32 aCount
	//================================================================================
	) {
		var Result = List<t>();
		var RestList = aList;
		t Head;
		while (aCount > 0 && RestList.MATCH(out Head, out RestList)) {
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
		t Head;
		while (aCount > 0 && RestList.MATCH(out Head, out RestList)) {
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
		t Head;
		while (RestList.MATCH(out Head, out RestList)) {
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
		t Head;
		while (RestList.MATCH(out Head, out RestList)) {
			if (aPredicate(Head)) {
				Result = Concat(Result, List(Head));
			}
		}
		return Result;
	}
	
	#region TEST
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"tList.ToString()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					// TODO: AssertEq(List<tInt32>().ToString(), "[]"); ???
					mStd.AssertEq(List(1).ToString(), "[1]");
					mStd.AssertEq(List(1, 2).ToString(), "[1, 2]");
					mStd.AssertEq(List(1, 2, 3).ToString(), "[1, 2, 3]");
					var LasyListCounter = (tInt32?)0;
					mStd.AssertEq(
						LasyList(
							() => {
								LasyListCounter += 1;
								return (LasyListCounter.Value < 5) ? mStd.OK(LasyListCounter.Value) : mStd.Fail<tInt32>();
							}
						).ToString(),
						"[1, 2, 3, 4]"
					);
					return true;
				}
			)
		),
		mStd.Tuple(
			"tList.Equals()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List<tInt32>(), List<tInt32>());
					mStd.AssertEq(List(1), List(1));
					mStd.AssertNotEq(List(1), List<tInt32>());
					mStd.AssertNotEq(List(1), List(2));
					mStd.AssertNotEq(List(1), List(1, 2));
					var LasyListCounter = (tInt32?)0;
					mStd.AssertEq(
						LasyList(
							() => {
								LasyListCounter += 1;
								return (LasyListCounter.Value < 5) ? mStd.OK(LasyListCounter.Value) : mStd.Fail<tInt32>();
							}
						),
						List(1, 2, 3, 4)
					);
					return true;
				}
			)
		),
		mStd.Tuple(
			"Concat()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(Concat(List(1, 2), List(3, 4)), List(1, 2, 3, 4));
					mStd.AssertEq(Concat(List(1, 2), List<tInt32>()), List(1, 2));
					mStd.AssertEq(Concat(List<tInt32>(), List(3, 4)), List(3, 4));
					mStd.AssertEq(Concat(List<tInt32>(), List<tInt32>()), List<tInt32>());
					return true;
				}
			)
		),
		mStd.Tuple(
			"Map()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List(1, 2, 3, 4).Map(a => a*a), List(1, 4, 9, 16));
					mStd.AssertEq(List<tInt32>().Map(a => a*a), List<tInt32>());
					return true;
				}
			)
		),
		mStd.Tuple(
			"Reduce()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List(1, 2, 3, 4).Reduce(0, (a1, a2) => a1+a2), 10);
					mStd.AssertEq(List(1).Reduce(0, (a1, a2) => a1+a2), 1);
					mStd.AssertEq(List<tInt32>().Reduce(0, (a1, a2) => a1+a2), 0);
					return true;
				}
			)
		),
		mStd.Tuple(
			"Join()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List("a", "b", "c", "d").Join((a1, a2) => a1+","+a2), "a,b,c,d");
					mStd.AssertEq(List("a").Join((a1, a2) => a1+","+a2), "a");
					mStd.AssertEq(List<tText>().Join((a1, a2) => a1+","+a2), "");
					return true;
				}
			)
		),
		mStd.Tuple(
			"Take()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List(1, 2, 3, 4).Take(3), List(1, 2, 3));
					mStd.AssertEq(List(1, 2, 3).Take(4), List(1, 2, 3));
					mStd.AssertEq(List<tInt32>().Take(4), List<tInt32>());
					mStd.AssertEq(List(1, 2, 3).Take(0), List<tInt32>());
					mStd.AssertEq(List(1, 2, 3).Take(-1), List<tInt32>());
					return true;
				}
			)
		),
		mStd.Tuple(
			"Skip()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List(1, 2, 3, 4).Skip(3), List(4));
					mStd.AssertEq(List(1, 2, 3).Skip(4), List<tInt32>());
					mStd.AssertEq(List<tInt32>().Skip(4), List<tInt32>());
					mStd.AssertEq(List(1, 2, 3).Skip(0), List(1, 2, 3));
					mStd.AssertEq(List(1, 2, 3).Skip(-1), List(1, 2, 3));
					return true;
				}
			)
		),
		mStd.Tuple(
			"Every()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mStd.AssertEq(List(1, 2, 3, 4, 5).Every(2), List(1, 3, 5));
					mStd.AssertEq(List(1, 2).Every(2), List(1));
					mStd.AssertEq(List<tInt32>().Every(2), List<tInt32>());
					mStd.AssertEq(List(1, 2, 3).Every(0), List(1, 2, 3));
					mStd.AssertEq(List(1, 2, 3).Every(-1), List(1, 2, 3));
					return true;
				}
			)
		)
	);
	
	#endregion
}
