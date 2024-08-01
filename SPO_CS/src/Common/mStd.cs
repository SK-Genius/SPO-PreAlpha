global using tBool = System.Boolean;

global using tNat8 = System.Byte;
global using tNat16 = System.UInt16;
global using tNat32 = System.UInt32;
global using tNat64 = System.UInt64;

global using tInt8 = System.SByte;
global using tInt16 = System.Int16;
global using tInt32 = System.Int32;
global using tInt64 = System.Int64;

global using tChar = System.Char;
global using tText = System.String;

public static class mStd {
	#region tFunc & tAction
	
	public delegate tRes tFunc<out tRes>();
	public delegate tRes tFunc<out tRes, in tArg>(tArg a);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3>(tArg1 a1, tArg2 a2, tArg3 a3);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4, in tArg5>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5);
	public delegate tRes tFunc<out tRes, in tArg1, in tArg2, in tArg3, in tArg4, in tArg5, in tArg6>(tArg1 a1, tArg2 a2, tArg3 a3, tArg4 a4, tArg5 a5, tArg6 a6);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes> Func<tRes>(tFunc<tRes> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes, tArg> Func<tRes, tArg>(tFunc<tRes, tArg> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes, tArg1, tArg2> Func<tRes, tArg1, tArg2>(tFunc<tRes, tArg1, tArg2> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes, tArg1, tArg2, tArg3> Func<tRes, tArg1, tArg2, tArg3>(tFunc<tRes, tArg1, tArg2, tArg3> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4> Func<tRes, tArg1, tArg2, tArg3, tArg4>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5> Func<tRes, tArg1, tArg2, tArg3, tArg4, tArg5>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6> Func<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6>(tFunc<tRes, tArg1, tArg2, tArg3, tArg4, tArg5, tArg6> a) => a;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static t Call<t>(tFunc<t> a) => a();
	
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
	public static tRes
	Let<tArg, tRes>(
		tArg a,
		tFunc<tRes, tArg> aFunc
	) => aFunc(a);
	
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
	_<t>(
		this t a,
		tAction<t> aDo
	) where t : class {
		aDo(a);
		return a;
	}
	
	public static readonly tEmpty cEmpty = new();
}
