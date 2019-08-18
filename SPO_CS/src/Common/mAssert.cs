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
	
	private static readonly tText cErrorPrefix = "FAIL: ";
	
	public static void
	True(
		tBool a,
		mStd.tFunc<tText> aMsg = null
	) {
		if (!a) {
			throw mError.Error(cErrorPrefix+(aMsg?.Invoke() ?? "is not true"));
		}
	}
	
	public static void
	False(
		tBool a
	) {
		Equals(a, false);
	}
	
	public static void
	Equals<t>(
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
		throw mError.Error(cErrorPrefix + $"\n{Text1}\n!=\n{Text2}");
		
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
	NotEquals<t>(
		t a1,
		t a2
	) {
		if (a1.Equals(a2)) {
			throw mError.Error(cErrorPrefix + $"{a1} == {a2}");
		}
	}
	
	public static t
	Null<t>(
		t a,
		mStd.tFunc<tText> aMsg = null
	) {
		if (!ReferenceEquals(a, null)) {
			throw mError.Error(cErrorPrefix + (aMsg?.Invoke() ?? $"{a} is not null"));
		}
		return a;
	}
	
	public static t
	NotNull<t>(
		t a,
		mStd.tFunc<tText> aMsg = null
	) {
		if (ReferenceEquals(a, null)) {
			throw mError.Error(cErrorPrefix + (aMsg?.Invoke() ?? $"{a} is null"));
		}
		return a;
	}
	
	public static void
	In<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (a1.Equals(Element)) {
				return;
			}
		}
		throw mError.Error(cErrorPrefix + $"{a1} in {a2}");
	}
	
	public static void
	NotIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (a1.Equals(Element)) {
				throw mError.Error(cErrorPrefix + $"{a1} not in {a2}");
			}
		}
	}
	
	public static void
	Error(
		mStd.tAction a
	) {
		try {
			a();
		} catch {
			return;
		}
		throw mError.Error(cErrorPrefix + "Error expected");
	}
}
