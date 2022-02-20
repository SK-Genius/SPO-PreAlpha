﻿//#define MY_TRACE

#nullable enable

using xCallerName = System.Runtime.CompilerServices.CallerMemberNameAttribute;
using xCallerFile = System.Runtime.CompilerServices.CallerFilePathAttribute;

[DebuggerStepThrough]
public static class
mPerf {
	private const tInt32 cMaxLogCount = 1<<16;
	private static tInt32 gStackIndex = 0;
	
	private static tInt32 gNextLogIndex = 0;
	private static readonly (tNat64 Time, tText? File, tText? Name)[] gLog = new (tNat64, tText?, tText?)[cMaxLogCount];
	
	public sealed class
	tDisposer : System.IDisposable {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void
		Dispose(
		) {
			ExitScope();
		}
	}
	
	private static readonly tDisposer gDisposer = new();
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tDisposer
	Measure(
		[xCallerName] tText aCallerName = "",
		[xCallerFile] tText aCallerFile = ""
	) {
		EnterScope(aCallerName, aCallerFile);
		return gDisposer;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void
	EnterScope(
		tText aCallerName,
		tText aCallerFile
	) {
		gLog[gNextLogIndex] = (ThreadCycles(), aCallerFile, aCallerName);
		gNextLogIndex += 1;
		gStackIndex += 1;
	}
	
	#if MY_TRACE
	private static readonly tText[] cUnits = {"", "K", "M", "G", "T", "P"};
	#endif
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void
	ExitScope(
	) {
		gStackIndex -= 1;
		gLog[gNextLogIndex] = (ThreadCycles(), null, null);
		gNextLogIndex += 1;
		if (gStackIndex == 0) {
			#if MY_TRACE
				var Stack = new (tNat64 Time, tText File, tText Name)[1<<16];
				var StackIndex = 0;
				for (var I = 0; I < gNextLogIndex; I += 1) {
					var LogLine = gLog[I];
					if (LogLine.Name is null) {
						StackIndex -= 1;
						var StackLine = Stack[StackIndex];
						var Duration00 = (LogLine.Time - StackLine.Time) * 100;
						var Unit = 0;
						while (Duration00 > 1000_00) {
							Duration00 /= 1000;
							Unit += 1;
						}
						System.Console.WriteLine($"{LogLine.Time}: {new tText(' ', StackIndex)} END {System.IO.Path.GetFileNameWithoutExtension(StackLine.File)}::{StackLine.Name} ({Duration00/100}.{Duration00%100} {cUnits[Unit]}Cycle)");
					} else {
						System.Console.WriteLine($"{LogLine.Time}: {new tText(' ', StackIndex)} BEGIN {System.IO.Path.GetFileNameWithoutExtension(LogLine.File)}::{LogLine.Name}");
						Stack[StackIndex] = LogLine;
						StackIndex += 1;
					}
				}
			#endif
			gNextLogIndex = 0;
		}
	}
	
	[DllImport("kernel32.dll")]
	private static extern bool
	QueryThreadCycleTime(
		System.IntPtr aThreadHandle,
		out tNat64 aCycles
	);
	
	private static readonly System.IntPtr PseudoHandle = (System.IntPtr)(-2);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static tNat64
	ThreadCycles(
	) {
		QueryThreadCycleTime(PseudoHandle, out var Cycles);
		return Cycles;
	}
}
