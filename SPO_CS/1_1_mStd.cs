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

public static class mStd {
	
	#region tFunc
	
	public delegate tRes tFunc<out tRes>();
	public delegate tRes tFunc<out tRes, in tArg>(tArg a);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	
	public static tFunc<tRes> Func<tRes>(tFunc<tRes> a) { return a; }
	public static tFunc<tRes, tArg> Func<tRes, tArg>(tFunc<tRes, tArg> a) { return a; }
	public static tFunc<tRes, tArg1, tArg2> Func<tRes, tArg1, tArg2>(tFunc<tRes, tArg1, tArg2> a) { return a; }
	public static tFunc<tRes, tArg1, tArg2, tArg3> Func<tRes, tArg1, tArg2, tArg3>(tFunc<tRes, tArg1, tArg2, tArg3> a) { return a; }
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4> Func<tRes, tArg1, tArg2, tArg3, tArg4>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4> a) { return a; }
	
	#endregion
	
	#region tAction
	
	public delegate void tAction();
	public delegate void tAction<tArg>(tArg a);
	public delegate void tAction<tArg1, tArg2>(tArg1 a1, tArg2 a2);
	public delegate void tAction<tArg1, tArg2, tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate void tAction<tArg1, tArg2, tArg3, tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	
	public static tAction<tArg> Action<tArg>(tAction<tArg> a) { return a; }
	public static tAction<tArg1, tArg2> Action<tArg1, tArg2>(tAction<tArg1, tArg2> a) { return a; }
	public static tAction<tArg1, tArg2, tArg3> Action<tArg1, tArg2, tArg3>(tAction<tArg1, tArg2, tArg3> a) { return a; }
	public static tAction<tArg1, tArg2, tArg3, tArg4> Action<tArg1, tArg2, tArg3, tArg4>(tAction<tArg1, tArg2, tArg3, tArg4> a) { return a; }
	
	#endregion
	
	#region tTuple2
	
	public struct tTuple<t1, t2> {
		internal t1 _1;
		internal t2 _2;
		
		public override tText ToString() { return "("+_1+", "+_2+")"; }
	}
	
	//================================================================================
	public static tTuple<t1, t2>
	Tuple<t1, t2>(
		t1 a1,
		t2 a2
	//================================================================================
	) {
		return new tTuple<t1, t2>{
			_1 = a1,
			_2 = a2
		};
	}
	
	//================================================================================
	public static tBool
	MATCH<t1, t2>(
		this tTuple<t1, t2> a,
		out t1 a1,
		out t2 a2
	//================================================================================
	) {
		a1 = a._1;
		a2 = a._2;
		return true;
	}
	
	#endregion
	
	#region tTuple3
	
	public struct tTuple<t1, t2, t3> {
		internal t1 _1;
		internal t2 _2;
		internal t3 _3;
		
		public override tText ToString() { return "("+_1+", "+_2+", "+_3+")"; }
    }
	
	//================================================================================
	public static tTuple<t1, t2, t3>
	Tuple<t1, t2, t3>(
		t1 a1,
		t2 a2,
		t3 a3
	//================================================================================
	) {
		return new tTuple<t1, t2, t3>{
			_1 = a1,
			_2 = a2,
			_3 = a3
		};
	}
	
	//================================================================================
	public static tBool
	MATCH<t1, t2, t3>(
		this tTuple<t1, t2, t3> a,
		out t1 a1,
		out t2 a2,
		out t3 a3
	//================================================================================
	) {
		a1 = a._1;
		a2 = a._2;
		a3 = a._3;
		return true;
	}
	
	#endregion
	
	#region tMaybe
	
	public struct tMaybe<t> {
		internal tBool _IsOK;
		internal t _Value;
		
		public override tText ToString() { return _IsOK ? _Value.ToString() : "FAIL"; }
	}
	
	//================================================================================
	public static tMaybe<t>
	OK<t>(
		t a
	//================================================================================
	) {
		return new tMaybe<t>{
			_Value = a,
			_IsOK = true
		};
	}
	
	//================================================================================
	public static tMaybe<t>
	Fail<t>(
	//================================================================================
	) {
		return new tMaybe<t>{
			_Value = default(t),
			_IsOK = false
		};
	}
	
	//================================================================================
	public static tBool
	MATCH<t>(
		this tMaybe<t> a,
		out t aValue
	//================================================================================
	) {
		aValue = a._Value;
		return a._IsOK;
	}
	
	#endregion
	
	#region tVar
	
	public struct tAny {
		internal object _Value;
		
		//================================================================================
		public tBool
		Equals(
			tAny a
		//================================================================================
		) {
			return (
				!_Value.IsNull() &&
				_Value.Equals(a._Value)
			);
		}
		
		public override tBool Equals(object obj) { return obj is tAny && this.Equals((tAny)obj); }
		public override tText ToString() { return _Value != null ? _Value.ToString() : "-"; }
		public static tBool operator==(tAny a1, tAny a2) { return a1.Equals(a2); }
		public static tBool operator!=(tAny a1, tAny a2) { return !a1.Equals(a2); }
	}
	
	//================================================================================
	public static tAny
	Any<t>(
		t a
	//================================================================================
	) {
		return new tAny{_Value = a};
	}
	
	//================================================================================
	public static tBool
	MATCH<t>(
		this tAny a,
		out t aValue
	//================================================================================
	) {
		if (typeof(t) == typeof(tAny)) {
			mStd.Assert(false);
		}
		
		if (a._Value is t) {
			aValue = (t)a._Value;
			return true;
		} else {
			aValue = default(t);
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	MATCH(
		this tAny a
	//================================================================================
	) {
		return a._Value.IsNull();
	}
	
	//================================================================================
	public static t
	To<t>(
		this tAny a
	//================================================================================
	) {
		t Result;
		Assert(a.MATCH(out Result));
		return Result;
	}
	
	#endregion
	
	//================================================================================
	public static tBool
	IsNull(
		this object a
	//================================================================================
	) {
		return object.ReferenceEquals(a, null);
	}
	
	#region Assert
	
	//================================================================================
	public static void
	Assert(
		tBool a
	//================================================================================
	) {
		if (!a) {
			System.Console.WriteLine("FAIL");
			throw null;
		}
	}
	
	//================================================================================
	public static void
	Assert(
		tBool a,
		tText aMsg
	//================================================================================
	) {
		if (!a) {
			System.Console.WriteLine(aMsg);
			throw null;
		}
	}
	
	//================================================================================
	public static void
	AssertNot(
		tBool a
	//================================================================================
	) {
		if (a) {
			System.Console.WriteLine("FAIL");
			throw null;
		}
	}
	
	//================================================================================
	public static void
	AssertEq<t>(
		t a1,
		t a2
	//================================================================================
	) {
		if (
			!object.ReferenceEquals(a1, a2) &&
			!a1.IsNull() &&
		    !a1.Equals(a2)
		) {
			System.Console.WriteLine("FAIL: "+a1+" != "+a2);
			throw null;
		}
	}
	
	//================================================================================
	public static void
	AssertNotEq<t>(
		t a1,
		t a2
	//================================================================================
	) {
		if (a1.Equals(a2)) {
			System.Console.WriteLine("FAIL: "+a1+" == "+a2);
			throw null;
		}
	}
	
	//================================================================================
	public static void
	AssertNull<t>(
		t a
	//================================================================================
	) {
		if (!a.IsNull()) {
			System.Console.WriteLine("FAIL: "+a+" != null");
			throw null;
		}
	}
	
	//================================================================================
	public static void
	AssertNotNull<t>(
		t a
	//================================================================================
	) {
		if (a.IsNull()) {
			System.Console.WriteLine("FAIL");
			throw null;
		}
	}
	
	#endregion
	
	#region TEST
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"tMaybe.ToString()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					AssertEq(OK(1).ToString(), "1");
					AssertEq(Fail<tText>().ToString(), "FAIL");
					return true;
				}
			)
		),
		mStd.Tuple(
			"tMaybe.Equals()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					AssertEq(OK(1), OK(1));
					AssertEq(OK("1"), OK("1"));
					AssertEq(Fail<tText>(), Fail<tText>());
					return true;
				}
			)
		),
		mStd.Tuple(
			"tVar.ToString()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					AssertEq(Any(1).ToString(), "1");
					AssertEq(Any("1").ToString(), "1");
					return true;
				}
			)
		),
		mStd.Tuple(
			"tVar.Equals()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					AssertEq(Any(1), Any(1));
					AssertEq(Any("1"), Any("1"));
					AssertNotEq(Any(1), Any(2));
					AssertNotEq(Any(1), Any(false));
					AssertNotEq(Any("1"), Any("2"));
					AssertNotEq(Any("1"), Any(1));
					return true;
				}
			)
		),
		mStd.Tuple(
			"tTuple.ToString()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					AssertEq(Tuple(1, "2").ToString(), "(1, 2)");
					AssertEq(Tuple(1, "2", Tuple("A", "B")).ToString(), "(1, 2, (A, B))");
					return true;
				}
			)
		),
		mStd.Tuple(
			"tTuple.Equals()",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					AssertEq(Tuple(1, "2"), Tuple(1, "2"));
					AssertEq(Tuple(1, "2", Tuple("A", "B")), Tuple(1, "2", Tuple("A", "B")));
					return true;
				}
			)
		)
	);
	
	#endregion
}
