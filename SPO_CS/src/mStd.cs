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

//TODO: split in multible mudules
public static class mStd {
	
	#region tFunc & tAction
	
	public delegate tRes tFunc<out tRes>();
	public delegate tRes tFunc<out tRes, in tArg>(tArg a);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4, in tArg5>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4, in tArg5, in tArg6>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5, tArg6 a6);
	
	public static tFunc<tRes> Func<tRes>(tFunc<tRes> a) => a;
	public static tFunc<tRes, tArg> Func<tRes, tArg>(tFunc<tRes, tArg> a) => a;
	public static tFunc<tRes, tArg1, tArg2> Func<tRes, tArg1, tArg2>(tFunc<tRes, tArg1, tArg2> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3> Func<tRes, tArg1, tArg2, tArg3>(tFunc<tRes, tArg1, tArg2, tArg3> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4> Func<tRes, tArg1, tArg2, tArg3, tArg4>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5> Func<tRes, tArg1, tArg2, tArg3, tArg4, tArg5>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5> a) => a;
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6> Func<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6> a) => a;
	
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
	
	public struct
	tSpan<tPos> {
		public tPos Start;
		public tPos End;
		
		public override tText
		ToString(
		) => $"{this.Start}..{this.End}";
	}
	
	public static tSpan<tPos>
	Span<tPos>(
		tPos aStart,
		tPos aEnd
	) => new tSpan<tPos> {
		Start = aStart,
		End = aEnd
	};
	
	public static tSpan<tPos>
	Span<tPos>(
		tPos aPos
	) => Span(aPos, aPos);
	
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
	
	public struct
	tEmpty {
	}
	
	public static readonly tEmpty cEmpty = new tEmpty();
	
	public static tRes
	Switch<tArg, tRes>(
		this tArg a,
		params (tArg, tFunc<tRes, tArg>)[] aCases
	) => a.Switch(
		_ => {
			throw Error("unknown case "+_);
		},
		aCases
	);
	
	public static tRes
	Switch<tArg, tRes>(
		this tArg a,
		tFunc<tRes, tArg> aDefaultFunc,
		params (tArg, tFunc<tRes, tArg>)[] aCases
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
	
	public struct
	tMaybe<t> {
		internal tBool _HasValue;
		internal t _Value;
		
		public static
		implicit operator tMaybe<t>(
			tEmpty _
		) {
			return new tMaybe<t> {
				_HasValue = false,
				_Value = default,
			};
		}
		
		public static
		implicit operator tMaybe<t>(
			t aValue
		) {
			return new tMaybe<t> {
				_HasValue = true,
				_Value = aValue,
			};
		}
	}
	
	public static tMaybe<t>
	Some<t>(
		t a
	) => a;
	
	public static tBool
	Match<t>(
		this tMaybe<t> a,
		out t aValue
	) {
		if (a._HasValue) {
			aValue = a._Value;
			return true;
		} else {
			aValue = default;
			return false;
		}
	}
	
	public static tMaybe<tOut>
	Then<tIn, tOut>(
		this tMaybe<tIn> a,
		tFunc<tMaybe<tOut>, tIn> aMap
	) {
		if (a.Match(out var Value)) {
			return aMap(Value);
		} else {
			return cEmpty;
		}
	}
	
	public static t
	Else<t>(
		this tMaybe<t> a,
		t aDefault
	) {
		if (a.Match(out var Value)) {
			return Value;
		} else {
			return aDefault;
		}
	}
	
	public static t
	ElseThrow<t>(
		this tMaybe<t> a,
		tText aError
	) {
		if (a.Match(out var Value)) {
			return Value;
		} else {
			throw mStd.Error(aError);
		}
	}
	
	public static t
	Else<t>(
		this tMaybe<t> a,
		tLazy<t> aDefault
	) {
		if (a.Match(out var Value)) {
			return Value;
		} else {
			return aDefault.Value;
		}
	}
	
	public static tMaybe<t>
	Assert<t>(
		this tMaybe<t> a,
		tFunc<tBool, t> aCond
	) {
		return a.Then<t, t>(
			_ => {
				if (aCond(_)) {
					return _;
				} else {
					return cEmpty;
				}
			}
		);
	}
	
	#endregion

	#region tResult

	public struct
	tResultFail<t> {
		internal t _Error;
	}
	
	public struct
	tResultOK<t> {
		internal t _Value;
	}
	
	public struct
	tResult<tOK, tFail> {
		
		internal tBool _IsOK;
		internal tOK _Value;
		internal tFail _Error;
		
		public static
		implicit operator tResult<tOK, tFail>(
			tResultOK<tOK> aOK
		) => new tResult<tOK, tFail> {
			_IsOK = true,
			_Value = aOK._Value
		};
		
		public static
		implicit operator tResult<tOK, tFail>(
			tResultFail<tFail> aFail
		) => new tResult<tOK, tFail> {
			_IsOK = false,
			_Error = aFail._Error
		};
	}
	
	public static tResultOK<tOK>
	OK<tOK>(
		tOK a
	) => new tResultOK<tOK>{
		_Value = a
	};
	
	public static tResultFail<tFail>
	Fail<tFail>(
		tFail aError
	) => new tResultFail<tFail>{
		_Error = aError
	};
	
	public static tResultFail<tEmpty>
	Fail(
	) => new tResultFail<tEmpty>();
	
	public static tBool
	Match<tOK, tFail>(
		this tResult<tOK, tFail> a,
		out tOK aValue,
		out tFail aError
	) {
		aValue = a._Value;
		aError = a._Error;
		return a._IsOK;
	}
	
	public static tBool
	Match<tOK>(
		this tResult<tOK, tEmpty> a,
		out tOK aValue
	) {
		aValue = a._Value;
		return a._IsOK;
	}
	
	public static tResult<tOK_Out, tError>
	Then<tOK_In, tOK_Out, tError>(
		this tResult<tOK_In, tError> a,
		mStd.tFunc<tResult<tOK_Out, tError>, tOK_In> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tResult<tOK_Out, tError>
	Then<tOK_In, tOK_Out, tError>(
		this tResult<tOK_In, tError> a,
		mStd.tFunc<tResultOK<tOK_Out>, tOK_In> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tResult<tOK_Out, tError>
	Then<tOK_In, tOK_Out, tError>(
		this tResult<tOK_In, tError> a,
		mStd.tFunc<tResultFail<tError>, tOK_In> aMod
	) {
		if (a.Match(out var Value, out var Error)) {
			return aMod(Value);
		} else {
			return Fail(Error);
		}
	}
	
	public static tOK
	Else<tOK, tError>(
		this tResult<tOK, tError> a,
		mStd.tFunc<tOK, tError> aOnError
	) {
		return a.Match(out var Value, out var Error)
			? Value
			: aOnError(Error);
	}
	
	public static tOK
	ElseThrow<tOK, tError>(
		this tResult<tOK, tError> a,
		mStd.tFunc<tText, tError> aModifyError
	) {
		if (a.Match(out var Value, out var Error)) {
			return Value;
		} else {
			throw mStd.Error(aModifyError(Error));
		}
	}
	
	public static tResult<tOK, tError>
	Assert<tOK, tError>(
		this tResult<tOK, tError> a,
		tFunc<tBool, tOK> aCond,
		tError aError
	) {
		return a.Then<tOK, tOK, tError>(
			_ => {
				if (aCond(_)) {
					return OK(_);
				} else {
					return Fail(aError);
				}
			}
		);
	}
	
	#endregion
	
	#region tVar
	
	public struct
	tAny {
		internal object _Value;
		
		public tBool
		Equals(
			tAny a
		) => (
			!(this._Value is null) &&
			this._Value.Equals(a._Value)
		);
		
		override public tBool
		Equals(
			object a
		) => a is tAny && this.Equals((tAny)a);
	}
	
	public static tAny
	Any<t>(
		t a
	) => new tAny{_Value = a};
	
	public static tBool
	Match<t>(
		this tAny a,
		out t aValue
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
	
	public static tBool
	Match(
		this tAny a
	) => a._Value is null;
	
	public static t
	To<t>(
		this tAny a
	) {
		if (!a.Match(out t Result)) {
			throw Error($"To: {typeof(t).FullName} <- {a}");
		}
		return Result;
	}
	
	#endregion
	
	public static tBool
	IsNull(
		this object a
	) => a is null;
	
	#region tError
	
	public sealed class tError<t> : System.Exception {
		public tError(tText a) : base(a) {}
		public t Value;
	}
	
	public static tError<t>
	Error<t>(
		tText aMsg,
		t aData
	) => new tError<t> (aMsg) { Value = aData };
	
	public static tError<tEmpty>
	Error(
		tText aMsg
	) => Error(aMsg, cEmpty);
	
	public struct tLazy<t> {
		private t _Value;
		public tFunc<t> Func { private get; set; }
		public t Value {
			get {
				if (this.Func is null) {
					return this._Value;
				}
				this.Value = this.Func();
				return this.Value;
			}
			set {
				this._Value = value;
				this.Func = null;
			}
		}
	}
	
	#endregion
	
	#region Asserts
	
	public static void
	Assert(
		tBool a
	) {
		AssertEq(a, true);
	}
	
	public static void
	Assert(
		tBool a,
		tText aMsg
	) {
		if (!a) {
			throw mStd.Error($"FAIL: {aMsg}");
		}
	}
	
	public static void
	Assert(
		tBool a,
		mStd.tFunc<tText> aMsg
	) {
		if (!a) {
			throw mStd.Error($"FAIL: {aMsg()}");
		}
	}
	
	public static void
	AssertNot(
		tBool a
	) {
		AssertEq(a, false);
	}
	
	public static void
	AssertEq<t>(
		t a1,
		t a2,
		tFunc<tBool, t, t> aAreEqual = null,
		tFunc<tText, t> aToText = null
	) {
		if (
			ReferenceEquals(a1, a2) ||
			(aAreEqual?.Invoke(a1, a2) ?? a1?.Equals(a2) ?? false)
		) {
			return;
		}

		tText Text1;
		tText Text2;
		if (aToText is null) {
			#if JSON
			try {
				Text1 = AsJSON(a1);
				Text2 = AsJSON(a2);
			} catch {
				Text1 = a1.ToString();
				Text2 = a2.ToString();
			}
			#else
			Text1 = a1.ToString();
			Text2 = a2.ToString();
			#endif
		} else {
			Text1 = aToText(a1);
			Text2 = aToText(a2);
		}
		throw Error($"FAIL:\n{Text1}\n!=\n{Text2}");
		
		#if JSON
		string
		AsJSON(
			object o
		) => Newtonsoft.Json.JsonConvert.SerializeObject(
			o,
			Newtonsoft.Json.Formatting.Indented
		);
		#endif
	}
	
	public static void
	AssertNotEq<t>(
		t a1,
		t a2
	) {
		if (a1.Equals(a2)) {
			throw Error($"FAIL: {a1} == {a2}");
		}
	}
	
	public static void
	AssertIsIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (a1.Equals(Element)) {
				return;
			}
		}
		throw Error($"FAIL: {a1} in {a2}");
	}
	
	public static void
	AssertIsNotIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (a1.Equals(Element)) {
				throw Error($"FAIL: {a1} not in {a2}");
			}
		}
	}
	
	public static void
	AssertError(
		tAction a
	) {
		try {
			a();
		} catch {
			return;
		}
		throw Error("FAIL: Error expected");
	}
	
	#endregion
}
