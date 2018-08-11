//#define TRACE

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

using xCallerName = System.Runtime.CompilerServices.CallerMemberNameAttribute;
using xCallerFile = System.Runtime.CompilerServices.CallerFilePathAttribute;

public static class mPerf {
	private const tInt32 cMaxLogCount = 1<<16;
	private static tInt32 gStackIndex = 0;
	
	private static tInt32 gNextLogIndex = 0;
	private static (tNat64 Time, tText File, tText Name)[] gLog = new (tNat64, tText, tText)[cMaxLogCount];
	
	public sealed class tDisposer : System.IDisposable {
		public void Dispose() { ExitScope(); }
	}
	
	private static readonly tDisposer gDisposer = new tDisposer();
	
	//================================================================================
	public static tDisposer
	Measure(
		[xCallerName] tText aCallerName = "",
		[xCallerFile] tText aCallerFile = ""
	//================================================================================
	) {
		EnterScope(aCallerName, aCallerFile);
		return gDisposer;
	}
	
	//================================================================================
	public static void
	EnterScope(
		tText aCallerName,
		tText aCallerFile
	//================================================================================
	) {
		gLog[gNextLogIndex] = (ThreadCycles(), aCallerFile, aCallerName);
		gNextLogIndex += 1;
		gStackIndex += 1;
	}
	
	private static tText[] cUnits = {"", "K", "M", "G", "T", "P"};
	
	//================================================================================
	public static void
	ExitScope(
	//================================================================================
	) {
		gStackIndex -= 1;
		gLog[gNextLogIndex] = (ThreadCycles(), null, null);
		gNextLogIndex += 1;
		if (gStackIndex == 0) {
			#if TRACE
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
						System.Console.WriteLine($"{LogLine.Time}: {new string(' ', StackIndex)} END {System.IO.Path.GetFileNameWithoutExtension(StackLine.File)}::{StackLine.Name} ({Duration00/100}.{Duration00%100} {cUnits[Unit]}Cycle)");
					} else {
						System.Console.WriteLine($"{LogLine.Time}: {new string(' ', StackIndex)} BEGIN {System.IO.Path.GetFileNameWithoutExtension(LogLine.File)}::{LogLine.Name}");
						Stack[StackIndex] = LogLine;
						StackIndex += 1;
					}
				}
			#endif
			gNextLogIndex = 0;
		}
	}
	
	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern bool QueryThreadCycleTime(System.IntPtr aThreadHandle, out tNat64 aCycles);
	private static readonly System.IntPtr PseudoHandle = (System.IntPtr)(-2);
	
	//================================================================================
	private static tNat64
	ThreadCycles(
	) {
	//================================================================================
		QueryThreadCycleTime(PseudoHandle, out var Cycles);
		return Cycles;
	}
}
