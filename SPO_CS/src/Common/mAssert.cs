//IMPORT mStd.cs
//IMPORT mError.cs
//IMPORT mMaybe.cs

#nullable enable

public static class
mAssert {
	
	private static readonly tText cErrorPrefix = "FAIL: ";
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	[DoesNotReturn]
	public static void
	Fail(
		tText? aMsg = null
	) => throw mError.Error(cErrorPrefix + (aMsg ?? $"Fail"));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static void
	IsTrue(
		[DoesNotReturnIf(false)] tBool a,
		[CallerArgumentExpression("a")] tText aMsg = ""
	) {
		if (!a) {
			Fail(aMsg);
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static void
	IsTrue(
		[DoesNotReturnIf(false)] tBool a,
		mStd.tFunc<tText>? aMsg
	) {
		if (!a) {
			Fail(aMsg?.Invoke() ?? "is not true");
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static void
	IsFalse(
		[DoesNotReturnIf(true)]tBool a,
		mStd.tFunc<tText>? aMsg
	) {
		IsTrue(!a, () => aMsg?.Invoke() ?? "is not false");
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static void
	IsFalse(
		[DoesNotReturnIf(true)] tBool a,
		[CallerArgumentExpression("a")] tText aMsg = ""
	) {
		if (a) {
			Fail(aMsg);
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
		Fail(
			mStream.ZipExtend(
				Text1.Split('\n').AsStream(),
				Text2.Split('\n').AsStream()
			).MapWithIndex(
				(aIndex, Line) => {
					var Line1 = Line._1.IsSome(out var Temp1) ? Temp1 : null;
					var Line2 = Line._2.IsSome(out var Temp2) ? Temp2 : null;
					return (
						Line1 is null ? $"""
							
							{Gray($">{aIndex + 1}:")} {Green(Line2)}
							""" :
						
						Line2 is null ? $"""
							
							{Gray($"<{aIndex + 1}:")} {Red(Line1)}
							""" :
						
						Line1 == Line2 ? $"""
							
							{Gray($"={aIndex + 1}:{Line2}")}
							""" :
						
						$"""
							
							{Gray($"<{aIndex + 1}:")} {Red(Line1)}
							{Gray($">{aIndex + 1}:")} {Green(Line2)}
							"""
					);
				}
			).Join(
				(a1, a2) => a1 + '\n' + a2,
				""
			).Replace("\r", "") ?? ""
		);
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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	AreNotEquals<t>(
		t a1,
		t a2
	) {
		IsFalse(Equals(a1, a2), () => $"{a1} == {a2}");
		return a1;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	IsNull<t>(
		t a,
		mStd.tFunc<tText>? aMsg = null
	) {
		IsTrue(a is null, () => aMsg?.Invoke() ?? $"{a} is not null");
		return a;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	IsNotNull<t>(
		t a,
		mStd.tFunc<tText>? aMsg = null
	) {
		IsFalse(a is null, () => aMsg?.Invoke() ?? $"{a} is null");
		return a;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	[DoesNotReturn]
	public static void
	Impossible(
	) {
		Fail("Impossible");
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tText
	Green(
		tText a
	) => mConsole.Color(mConsole.tColorCode.Green, a);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tText
	Red(
		tText a
	) => mConsole.Color(mConsole.tColorCode.Red, a);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tText
	Gray(
		tText a
	) => mConsole.Color(mConsole.tColorCode.Gray, a);
}
