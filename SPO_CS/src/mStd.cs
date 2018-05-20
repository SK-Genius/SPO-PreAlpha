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
	
	public static tAction Action(tAction a) => a;
	public static tAction<tArg> Action<tArg>(tAction<tArg> a) => a;
	public static tAction<tArg1, tArg2> Action<tArg1, tArg2>(tAction<tArg1, tArg2> a) => a;
	public static tAction<tArg1, tArg2, tArg3> Action<tArg1, tArg2, tArg3>(tAction<tArg1, tArg2, tArg3> a) => a;
	public static tAction<tArg1, tArg2, tArg3, tArg4> Action<tArg1, tArg2, tArg3, tArg4>(tAction<tArg1, tArg2, tArg3, tArg4> a) => a;
	
	#endregion
	
	#region tSpan
	
	public struct tSpan<tPos> {
		public tPos Start;
		public tPos End;
		
		override public tText ToString() => $"{Start}..{End}";
	}
	
	public static tSpan<tPos>
	Merge<tPos>(
		tSpan<tPos> a1,
		tSpan<tPos> a2
	) {
		if (a1.Equals(default(tSpan<tPos>))) {
			return a2;
		}
		if (a2.Equals(default(tSpan<tPos>))) {
			return a1;
		}
		return new tSpan<tPos> {
			Start = a1.Start,
			End = a2.End
		};
	}
	
	#endregion
	
	public struct tEmpty {
	}
	
	public static readonly tEmpty cEmpty = new tEmpty();
	
	//================================================================================
	public static tRes
	Switch<tArg, tRes>(
		this tArg a,
		params (tArg, tFunc<tRes, tArg>)[] aCases
	//================================================================================
	) => a.Switch(
		_ => {
			throw Error($@"unknown case {_}");
		},
		aCases
	);
	
	//================================================================================
	public static tRes
	Switch<tArg, tRes>(
		this tArg a,
		tFunc<tRes, tArg> aDefaultFunc,
		params (tArg, tFunc<tRes, tArg>)[] aCases
	//================================================================================
	) {
		foreach (var Case in aCases) {
			var (Match, Func) = Case;
			if (Match.Equals(a)) {
				return Func(a);
			}
		}
		return aDefaultFunc(a);
	}
	
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
	public static _tFail<tEmpty>
	Fail(
	//================================================================================
	) => new _tFail<tEmpty>();
	
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
		this tMaybe<tOK, tEmpty> a,
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
			!(_Value is null) &&
			_Value.Equals(a._Value)
		);
		
		override public tBool Equals(object a) => a is tAny && Equals((tAny)a);
		override public tText ToString() => _Value?.ToString() ?? "-";
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
		#if DEBUG
			if (typeof(t) == typeof(tAny)) {
				throw Error("");
			}
		#endif
		
		if (a._Value is null || a._Value is t) {
			aValue = (t)a._Value;
			return true;
		} else {
			aValue = default;
			return false;
		}
	}
	
	//================================================================================
	public static tBool
	Match(
		this tAny a
	//================================================================================
	) => a._Value is null;
	
	//================================================================================
	public static t
	To<t>(
		this tAny a
	//================================================================================
	) {
		if (!a.Match(out t Result)) {
			throw Error($"To: {typeof(t).FullName} <- {a}");
		}
		return Result;
	}
	
	#endregion
	
	//================================================================================
	public static tBool
	IsNull(
		this object a
	//================================================================================
	) => a is null;
	
	public sealed class tError<t> : System.Exception {
		public tError(tText a) : base(a) {}
		public t Value;
	}
	
	//================================================================================
	public static tError<t>
	Error<t>(
		t a
	//================================================================================
	) => new tError<t> (a.ToString()) { Value = a };
	
	public struct tLazy<t> {
		private t _Value;
		public tFunc<t> Func { private get; set; }
		public t Value {
			get {
				if (this.Func is null) {
					return _Value;
				}
				this.Value = this.Func();
				return this.Value;
			}
			set {
				_Value = value;
				this.Func = null;
			}
		}
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
			throw mStd.Error($"FAIL: {aMsg}");
		}
	}
	
	//================================================================================
	public static void
	Assert(
		tBool a,
		mStd.tFunc<tText> aMsg
	//================================================================================
	) {
		if (!a) {
			throw mStd.Error($"FAIL: {aMsg()}");
		}
	}
	
	//================================================================================
	public static void
	AssertNot(
		tBool a
	//================================================================================
	) {
		AssertEq(a, false);
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
			throw mStd.Error($"FAIL: {a1} != {a2}");
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
			throw mStd.Error($"FAIL: {a1} == {a2}");
		}
	}
	
	//================================================================================
	public static void
	AssertError(
		mStd.tAction a
	//================================================================================
	) {
		try {
			a();
			throw mStd.Error("FAIL: Error expected");
		} catch {
			return;
		}
	}
}
