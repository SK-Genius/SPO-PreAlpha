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
	
	public static tFunc<tRes> Func<tRes>(tFunc<tRes> a) => a;
	public static tFunc<tRes, tArg> Func<tRes, tArg>(tFunc<tRes, tArg> a) => a;
	public static tFunc<tRes, tArg1, tArg2> Func<tRes, tArg1, tArg2>(tFunc<tRes, tArg1, tArg2> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3> Func<tRes, tArg1, tArg2, tArg3>(tFunc<tRes, tArg1, tArg2, tArg3> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4> Func<tRes, tArg1, tArg2, tArg3, tArg4>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4> a) => a;
	
	#endregion
	
	#region tAction
	
	public delegate void tAction();
	public delegate void tAction<in tArg>(tArg a);
	public delegate void tAction<in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate void tAction<in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate void tAction<in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	
	public static tAction<tArg> Action<tArg>(tAction<tArg> a) => a;
	public static tAction<tArg1, tArg2> Action<tArg1, tArg2>(tAction<tArg1, tArg2> a) => a;
	public static tAction<tArg1, tArg2, tArg3> Action<tArg1, tArg2, tArg3>(tAction<tArg1, tArg2, tArg3> a) => a;
	public static tAction<tArg1, tArg2, tArg3, tArg4> Action<tArg1, tArg2, tArg3, tArg4>(tAction<tArg1, tArg2, tArg3, tArg4> a) => a;
	
	#endregion
	
	public struct tVoid { 
	}
	
	public static tVoid cEmpty;
	
	#region tMaybe
	
	public struct _tFail<t> {
		internal t _Error;
	}
	
	public struct _tOK<t> {
		internal t _Value;
	}
	
	public struct tMaybe<tOK, tFail> {
		
		internal tBool _IsOK;
		internal tOK _Value;
		internal tFail _Error;
		
		override public tText ToString() => _IsOK ? _Value.ToString() : "FAIL: "+_Error;
		
		public static implicit operator tMaybe<tOK, tFail>(
			_tOK<tOK> aOK
		) => new tMaybe<tOK, tFail> {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		public static implicit operator tMaybe<tOK, tFail>(
			_tFail<tFail> aFail
		) => new tMaybe<tOK, tFail> {
			_IsOK = false,
			_Error = aFail._Error
		};
	}
	
	//================================================================================
	public static _tOK<tOK>
	OK<tOK>(
		tOK a
	//================================================================================
	) => new _tOK<tOK>{
		_Value = a
	};
	
	//================================================================================
	public static _tFail<tFail>
	Fail<tFail>(
		tFail aError
	//================================================================================
	) => new _tFail<tFail>{
		_Error = aError
	};
	
	//================================================================================
	public static _tFail<tVoid>
	Fail(
	//================================================================================
	) => new _tFail<tVoid>();
	
	//================================================================================
	public static tBool
	Match<tOK, tFail>(
		this tMaybe<tOK, tFail> a,
		out tOK aValue,
		out tFail aError
	//================================================================================
	) {
		aValue = a._Value;
		aError = a._Error;
		return a._IsOK;
	}
	
	//================================================================================
	public static tBool
	Match<tOK>(
		this tMaybe<tOK, tVoid> a,
		out tOK aValue
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
		) => (
			!_Value.IsNull() &&
			_Value.Equals(a._Value)
		);
		
		override public tBool Equals(object aObj) => aObj is tAny && Equals((tAny)aObj);
		override public tText ToString() => _Value?.ToString() ?? "-";
		public static tBool operator==(tAny a1, tAny a2) => a1.Equals(a2);
		public static tBool operator!=(tAny a1, tAny a2) => !a1.Equals(a2);
		override public int GetHashCode() => base.GetHashCode();
	}
	
	//================================================================================
	public static tAny
	Any<t>(
		t a
	//================================================================================
	) => new tAny{_Value = a};
	
	//================================================================================
	public static tBool
	Match<t>(
		this tAny a,
		out t aValue
	//================================================================================
	) {
		if (typeof(t) == typeof(tAny)) {
			Assert(false);
		}
		
		if (a._Value.IsNull() || a._Value is t) {
			aValue = (t)a._Value;
			return true;
		} else {
			aValue = default(t);
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	Match(
		this tAny a
	//================================================================================
	) => a._Value.IsNull();
	
	//================================================================================
	public static t
	To<t>(
		this tAny a
	//================================================================================
	) {
		Assert(a.Match(out t Result), $"To: {typeof(t).FullName} <- {a}");
		return Result;
	}
	
	#endregion
	
	//================================================================================
	public static tBool
	IsNull(
		this object a
	//================================================================================
	) => ReferenceEquals(a, null);
	
	#region Assert
	
	public sealed class tException<t> : System.Exception {
		public t _Value;
	}
	
	//================================================================================
	public static void
	Assert(
		tBool a
	//================================================================================
	) {
		AssertEq(a, true);
	}
	
	//================================================================================
	public static void
	Assert(
		tBool a,
		tText aMsg
	//================================================================================
	) {
		if (!a) {
			throw new System.Exception($"FAIL: {aMsg}");
		}
	}
	
	//================================================================================
	public static void
	AssertNot(
		tBool a
	//================================================================================
	) {
		AssertEq(a, !true);
	}
	
	//================================================================================
	public static void
	AssertEq<t>(
		t a1,
		t a2
	//================================================================================
	) {
		if (
			!ReferenceEquals(a1, a2) &&
			!a1.IsNull() &&
		    !a1.Equals(a2)
		) {
			throw new System.Exception($"FAIL: {a1} != {a2}");
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
			throw new System.Exception($"FAIL: {a1} == {a2}");
		}
	}
	
	//================================================================================
	public static void
	AssertNull<t>(
		t a
	//================================================================================
	) {
		if (!a.IsNull()) {
			throw new System.Exception($"FAIL: {a} != null");
		}
	}
	
	//================================================================================
	public static void
	AssertNotNull<t>(
		t a
	//================================================================================
	) {
		if (a.IsNull()) {
			throw new System.Exception("FAIL: is NULL");
		}
	}
	
	#endregion
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStd),
		mTest.Test(
			"tMaybe.ToString()",
			aStreamOut => {
				AssertEq(((tMaybe<tInt32, tText>)OK(1)).ToString(), "1");
				AssertEq(((tMaybe<tInt32, tText>)Fail("Bla")).ToString(), "FAIL: Bla");
			}
		),
		mTest.Test(
			"tMaybe.Equals()",
			aStreamOut => {
				AssertEq<tMaybe<tInt32, tText>>(OK(1), OK(1));
				AssertEq<tMaybe<tText, tText>>(OK("1"), OK("1"));
				AssertEq<tMaybe<tInt32, tText>>(Fail("Bla"), Fail("Bla"));
			}
		),
		mTest.Test(
			"tVar.ToString()",
			aStreamOut => {
				AssertEq(Any(1).ToString(), "1");
				AssertEq(Any("1").ToString(), "1");
			}
		),
		mTest.Test(
			"tVar.Equals()",
			aStreamOut => {
				AssertEq(Any(1), Any(1));
				AssertEq(Any("1"), Any("1"));
				AssertNotEq(Any(1), Any(2));
				AssertNotEq(Any(1), Any(false));
				AssertNotEq(Any("1"), Any("2"));
				AssertNotEq(Any("1"), Any(1));
			}
		),
		mTest.Test(
			"tTuple.ToString()",
			aStreamOut => {
				AssertEq((1, "2").ToString(), "(1, 2)");
				AssertEq((1, "2", ("A", "B")).ToString(), "(1, 2, (A, B))");
			}
		),
		mTest.Test(
			"tTuple.Equals()",
			aStreamOut => {
				AssertEq((1, "2"), (1, "2"));
				AssertEq((1, "2", ("A", "B")), (1, "2", ("A", "B")));
			}
		)
	);
	
	#endregion
}
