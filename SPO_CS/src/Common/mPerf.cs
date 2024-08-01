//#define MY_TRACE

using xCallerName = System.Runtime.CompilerServices.CallerMemberNameAttribute;
using xCallerFile = System.Runtime.CompilerServices.CallerFilePathAttribute;

public static class
mPerf {
	private const tInt32 cMaxLogCount = 1<<16;
	private static tInt32 gStackIndex = 0;
	
	private static tInt32 gNextLogIndex = 0;
	private static readonly (tNat64 Time, tText? File, tText? Name)[] gLog = new (tNat64, tText?, tText?)[cMaxLogCount];
	
	public sealed class
	tDisposer : System.IDisposable {
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		Dispose(
		) {
			ExitScope();
		}
	}
	
	private static readonly tDisposer gDisposer = new();
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tDisposer
	Measure(
		[xCallerName] tText aCallerName = "",
		[xCallerFile] tText aCallerFile = ""
	) {
		EnterScope(aCallerName, aCallerFile);
		return gDisposer;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
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
	
	//private static readonly nint PseudoHandle = -2;
	//
	//[DllImport("kernel32.dll")]
	//private static extern bool
	//QueryThreadCycleTime(
	//	nint aThreadHandle,
	//	out tNat64 aCycles
	//);
	//
	//private struct tTimeQueryResult {
	//	public tNat64 Sec;
	//	public tNat64 NanoSec;
	//}
	//
	//private enum tClockId {
	//	CLOCK_REALTIME = 1,
	//	CLOCK_MONOTONIC,
	//	CLOCK_PROCESS_CPUTIME_ID,
	//	CLOCK_THREAD_CPUTIME_ID,
	//}
	//
	//[DllImport("libc.so")]
	//private static extern void
	//clock_gettime(
	//	tClockId clk_id,
	//	out tTimeQueryResult tp
	//);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tNat64
	ThreadCycles(
	) {
		// TODO: time spans for linux tests
		//if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		//{
		//	clock_gettime(tClockId.CLOCK_THREAD_CPUTIME_ID, out var tp);
		//	return tp.NanoSec;
		//	return 1;
		//} else {
		//	QueryThreadCycleTime(PseudoHandle, out var cycles);
		//	return cycles;
		//}
		return 1;
	}
}
