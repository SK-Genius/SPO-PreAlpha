public static class mStd {
	#region tFunc & tAction
	
	public delegate tRes tFunc<out tRes>();
	public delegate tRes tFunc<in tArg, out tRes>(tArg a);
	public delegate tRes tFunc<in tArg1, in tArg2, out tRes>(tArg1 a1, tArg2 a2);
	public delegate tRes tFunc<in tArg1, in tArg2, in tArg3, out tRes>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate tRes tFunc<in tArg1, in tArg2, in tArg3, in tArg4, out tRes>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	public delegate tRes tFunc<in tArg1, in tArg2, in tArg3, in tArg4, in tArg5, out tRes>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5);
	public delegate tRes tFunc<in tArg1, in tArg2, in tArg3, in tArg4, in tArg5, in tArg6, out tRes>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5, tArg6 a6);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes> Func<tRes>(tFunc<tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tArg, tRes> Func<tArg, tRes>(tFunc<tArg, tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tArg1, tArg2, tRes> Func<tArg1, tArg2, tRes>(tFunc<tArg1, tArg2, tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tArg1, tArg2, tArg3, tRes> Func<tArg1, tArg2, tArg3, tRes>(tFunc<tArg1, tArg2, tArg3, tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tArg1, tArg2, tArg3, tArg4, tRes> Func<tArg1, tArg2, tArg3, tArg4, tRes>(tFunc<tArg1, tArg2, tArg3, tArg4, tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tArg1, tArg2, tArg3, tArg4, tArg5, tRes> Func<tArg1, tArg2, tArg3, tArg4, tArg5, tRes>(tFunc<tArg1, tArg2, tArg3, tArg4, tArg5, tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tArg1, tArg2, tArg3, tArg4, tArg5, tArg6, tRes> Func<tArg1, tArg2, tArg3, tArg4, tArg5, tArg6, tRes>(tFunc<tArg1, tArg2, tArg3, tArg4, tArg5, tArg6, tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tRes Call<tRes>(tFunc<tRes> aFunc) => aFunc();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tRes With<tRes, tArg>(tArg a, tFunc<tArg, tRes> aFunc)
	=> aFunc(a);
	
	public delegate void tAction();
	public delegate void tAction<in tArg>(tArg a);
	public delegate void tAction<in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate void tAction<in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate void tAction<in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAction Action(tAction a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAction<tArg> Action<tArg>(tAction<tArg> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAction<tArg1, tArg2> Action<tArg1, tArg2>(tAction<tArg1, tArg2> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAction<tArg1, tArg2, tArg3> Action<tArg1, tArg2, tArg3>(tAction<tArg1, tArg2, tArg3> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tAction<tArg1, tArg2, tArg3, tArg4> Action<tArg1, tArg2, tArg3, tArg4>(tAction<tArg1, tArg2, tArg3, tArg4> a) => a;
	
	#endregion
	
	public readonly struct
	tEmpty {
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	Line(
		[CallerLineNumber] tInt32 aLine = 0
	) => aLine;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	File(
		[CallerFilePath] tText aFile = ""
	) => aFile;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	FileLine(
		[CallerFilePath] tText aFile = "",
		[CallerLineNumber] tInt32 aLine = 0
	) => aFile + ":" + aLine;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	Name(
		[CallerMemberName] tText aName = ""
	) => aName;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t
	Do<t>(
		this t a,
		tAction<t> aDo
	) where t : class {
		aDo(a);
		return a;
	}
	
	public static readonly tEmpty cEmpty = new();
	
	public static tNat64 gDebugId = 0;
	public static tNat64 gNextDebugId = 1;
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tNat64
	NewDebugId(
	) {
		var DebugId = gNextDebugId;
		if (DebugId == gDebugId) {
			System.Diagnostics.Debugger.Launch();
			System.Diagnostics.Debugger.Break();
		}
		gNextDebugId += 1;
		return DebugId;
	}
}
