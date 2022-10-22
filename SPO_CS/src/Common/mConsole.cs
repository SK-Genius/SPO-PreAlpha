public static class
mConsole {
	
	private enum
	tFn {
		Reset = 0,
		Bright = 1,
		Dim = 2,
		Underscore = 4,
		Blink = 5,
		Reverse = 7,
		Hidden = 8,
	}
	
	public enum
	tColorCode {
		Black = 30,
		Red = 31,
		Green = 32,
		Yellow = 33,
		Blue = 34,
		Magenta = 35,
		Cyan = 36,
		White = 37,
		Gray = 90,
		BrightRed = 91,
		BrightGreen = 92,
		BrightYellow = 93,
		BrightBlue = 94,
		BrightMagenta = 95,
		BrightCyan = 96,
		BrightWhite = 97,
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	Color(
		tColorCode aColor,
		tText aText
	) => Esc((tInt32)aColor) + aText + Esc((tInt32)tFn.Reset);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tText
	SwitchColor(
		tText aText
	) => Esc((tInt32)tFn.Reverse) + aText + Esc((tInt32)tFn.Reverse);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tText
	Esc(
		tInt32 a
	) => $"\x1b[{a}m";
}