//IMPORT mStd.cs
//IMPORT mError.cs

#nullable enable

using System.Diagnostics.CodeAnalysis;

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
	IsTrue(
		[DoesNotReturnIf(false)]tBool a,
		mStd.tFunc<tText>? aMsg = null
	) {
		if (!a) {
			throw mError.Error(cErrorPrefix+(aMsg?.Invoke() ?? "is not true"));
		}
	}
	
	public static void
	IsFalse(
		[DoesNotReturnIf(true)]tBool a,
		mStd.tFunc<tText>? aMsg = null
	) {
		IsTrue(!a, aMsg);
	}
	
	public static void
	AreEquals<t>(
		t a1,
		t a2,
		mStd.tFunc<tBool, t, t>? aAreEqual = null,
		mStd.tFunc<tText, t>? aToText = null
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
				Text1 = a1?.ToString() ?? "null";
				Text2 = a2?.ToString() ?? "null";
			}
#else
			Text1 = a1?.ToString() ?? "null";
			Text2 = a2?.ToString() ?? "null";
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
	AreNotEquals<t>(
		t a1,
		t a2
	) {
		if (Equals(a1, a2)) {
			throw mError.Error(cErrorPrefix + $"{a1} == {a2}");
		}
	}
	
	public static t
	IsNull<t>(
		t a,
		mStd.tFunc<tText>? aMsg = null
	) => (
		a is not null
		? throw mError.Error(cErrorPrefix + (aMsg?.Invoke() ?? $"{a} is not null"))
		: a
	);
	
	public static t
	IsNotNull<t>(
		t a,
		mStd.tFunc<tText>? aMsg = null
	) => a ?? throw mError.Error(cErrorPrefix + (aMsg?.Invoke() ?? $"{a} is null"));
	
	public static void
	IsIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (Equals(a1, Element)) {
				return;
			}
		}
		throw mError.Error(cErrorPrefix + $"{a1} in {a2}");
	}
	
	public static void
	IsNotIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (Equals(a1, Element)) {
				throw mError.Error(cErrorPrefix + $"{a1} not in {a2}");
			}
		}
	}
	
	public static void
	ThrowsError(
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
