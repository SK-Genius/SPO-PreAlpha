//IMPORT mStd.cs
//IMPORT mError.cs

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

public static class
mAssert {
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
			throw mError.Error($"FAIL: {aMsg}");
		}
	}

	public static void
	Assert(
		tBool a,
		mStd.tFunc<tText> aMsg
	) {
		if (!a) {
			throw mError.Error($"FAIL: {aMsg()}");
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
		mStd.tFunc<tBool, t, t> aAreEqual = null,
		mStd.tFunc<tText, t> aToText = null
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
		throw mError.Error($"FAIL:\n{Text1}\n!=\n{Text2}");

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
			throw mError.Error($"FAIL: {a1} == {a2}");
		}
	}
	
	public static void
	AssertIsNull<t>(
		t a
	) {
		if (ReferenceEquals(a, null)) {
			throw mError.Error($"FAIL: {a} is null");
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
		throw mError.Error($"FAIL: {a1} in {a2}");
	}

	public static void
	AssertIsNotIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (a1.Equals(Element)) {
				throw mError.Error($"FAIL: {a1} not in {a2}");
			}
		}
	}

	public static void
	AssertError(
		mStd.tAction a
	) {
		try {
			a();
		} catch {
			return;
		}
		throw mError.Error("FAIL: Error expected");
	}
}
