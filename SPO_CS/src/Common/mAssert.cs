//IMPORT mStd.cs
//IMPORT mError.cs
//IMPORT mMaybe.cs

#nullable enable

[DebuggerStepThrough]
public static class
mAssert {
	
	private static readonly tText cErrorPrefix = "FAIL: ";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[DoesNotReturn]
	public static void
	Fail(
		tText? aMsg = null
	) {
		throw mError.Error(cErrorPrefix + (aMsg ?? $"Fail"));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void
	IsTrue(
		[DoesNotReturnIf(false)]tBool a,
		mStd.tFunc<tText>? aMsg = null
	) {
		if (!a) {
			Fail(aMsg?.Invoke() ?? "is not true");
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void
	IsFalse(
		[DoesNotReturnIf(true)]tBool a,
		mStd.tFunc<tText>? aMsg = null
	) {
		IsTrue(!a, () => aMsg?.Invoke() ?? "is not false");
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
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
			return a1;
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
		Fail($"\n{Text1}\n!=\n{Text2}");
		return a1;
		
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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	AreNotEquals<t>(
		t a1,
		t a2
	) {
		IsFalse(Equals(a1, a2), () => $"{a1} == {a2}");
		return a1;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	IsNull<t>(
		t a,
		mStd.tFunc<tText>? aMsg = null
	) {
		IsTrue(a is null, () => aMsg?.Invoke() ?? $"{a} is not null");
		return a;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	IsNotNull<t>(
		t a,
		mStd.tFunc<tText>? aMsg = null
	) {
		IsFalse(a is null, () => aMsg?.Invoke() ?? $"{a} is null");
		return a;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	IsIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			if (Equals(a1, Element)) {
				return a1;
			}
		}
		Fail($"{a1} in {a2.AsStream().Map(a => "" + a).TryReduce((a1, a2) => a1 + ", " + a2).Else("")}");
		return a1;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static t
	IsNotIn<t>(
		t a1,
		params t[] a2
	) {
		foreach (var Element in a2) {
			IsFalse(Equals(a1, Element), () => $"{a1} not in {a2}");
		}
		return a1;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[DoesNotReturn]
	public static void
	Impossible(
	) {
		Fail("Impossible");
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void
	ThrowsError(
		mStd.tAction a
	) {
		try {
			a();
		} catch {
			return;
		}
		Fail("Error expected");
	}
}
