using System;

public static class
mTest {
	
	[DllImport("kernel32.dll")]
	private static extern bool
	QueryThreadCycleTime(
		nint aThreadHandle,
		out tNat64 aCycles
	);
	
	private static readonly nint PseudoHandle = -2;
	
	private const tText cTab = "|  ";
	
	public enum
	tResult {
		OK,
		Fail,
		Skip
	}
	
	public interface
	tTest {
	}
	
	public readonly struct
	tTestRun : tTest {
		public tText Name { get; init; }
		public mStd.tAction<mStd.tAction<tText>> TestFunc { get; init; }
		public tText File { get; init; }
		public tInt32 Line { get; init; }
	}
	
	public readonly struct
	tTestCollection : tTest {
		public tText Name { get; init; }
		public tTest[] Tests { get; init; }
	}
	
	[DebuggerHidden]
	public static tTest
	Tests(
		tText aName,
		params tTest[] aTests
	) => new tTestCollection { Name = aName, Tests = aTests };
	
	public static tTest
	Test(
		tText aName,
		mStd.tAction<mStd.tAction<tText>> aTestFunc,
		[CallerFilePath] tText aFile = null!,
		[CallerLineNumber] tInt32 aLine = 0
	) => new tTestRun {
		Name = aName,
		TestFunc = aTestFunc,
		File = aFile,
		Line = aLine,
	};
	
	[DebuggerHidden]
	public static tText
	Name(
		this tTest aTest
	) => aTest switch {
		tTestRun Run => Run.Name,
		tTestCollection Collection => Collection.Name,
		_ => throw new NotImplementedException(aTest.GetType().FullName)
	};
	
	[DebuggerHidden]
	public static tBool
	HasAnyMatch(
		this tTest aTest,
		mStream.tStream<tText>? aFilters
	) => aFilters.IsEmpty() || aTest switch {
		tTestRun Run => aFilters.Any(Run.Name.Contains),
		tTestCollection Collection => (
			aFilters.Any(Collection.Name.Contains) ||
			Collection.Tests.AsStream().Any(_ => _.HasAnyMatch(aFilters))
		),
		_ => throw new NotImplementedException(aTest.GetType().FullName),
	};
	
	[DebuggerHidden]
	public static void
	List(
		this tTest aTest,
		mStd.tAction<tText> aDebugStream,
		mStream.tStream<tText>? aFilters
	) {
		if (!aTest.HasAnyMatch(aFilters)) {
			return;
		}
		
		var TestName = aTest.Name();
		aDebugStream(TestName);
		if (aFilters.Any(TestName.Contains)) {
			aFilters = default;
		}
		switch (aTest) {
			case tTestCollection Collection: {
				var PrintLn = mStd.Action((tText aLine) => aDebugStream(cTab + aLine));
				foreach (var Test in Collection.Tests) {
					Test.List(PrintLn, aFilters);
				}
				break;
			}
			case tTestRun Run: {
				aDebugStream($"[{Run.File}:{Run.Line}]");
				aDebugStream("");
				break;
			}
		}
	}
	
	[DebuggerHidden]
	public static (tResult Result, tInt32 FailCount, tInt32 SkipCount, tInt32 OK_Count)
	Run(
		this tTest aTest,
		mStd.tAction<tText> aDebugStream,
		mStream.tStream<tText>? aFilters,
		tBool aHideSkippedTests,
		tBool aIsLogEnable
	) {
		System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		
		if (aHideSkippedTests && !aTest.HasAnyMatch(aFilters)) {
			aDebugStream = _ => {};
		}
		
		aDebugStream(aTest.Name());
		switch (aTest) {
			case tTestRun Run: {
				aDebugStream($"[{Run.File}:{Run.Line}]");
				
				if (!aFilters.IsEmpty() && !aFilters.Any(Run.Name.Contains)) {
					aDebugStream(mConsole.Color(mConsole.tColorCode.Yellow, "> Skipped"));
					aDebugStream("");
					return (tResult.Skip, 0, 1, 0);
				}
				
				try {
					QueryThreadCycleTime(PseudoHandle, out var ClocksStart);
					Run.TestFunc(
						aIsLogEnable
						? LineByLine([DebuggerHidden] (_) => aDebugStream(cTab + mConsole.Color(mConsole.tColorCode.Gray, _)))
						: _ => { }
					);
					QueryThreadCycleTime(PseudoHandle, out var ClocksEnd);
					
					var Value_00 = (ClocksEnd - ClocksStart) * 100;
					var E = "";
					switch (Value_00) {
						case >= 1_000_000_000_00: {
							E = "G";
							Value_00 /= 1_000_000_000;
							break;
						}
						case >= 1_000_000_00: {
							E = "M";
							Value_00 /= 1_000_000;
							break;
						}
						case >= 1_000_00: {
							E = "k";
							Value_00 /= 1_000;
							break;
						}
					}
					var Value = Value_00 / 100;
					var SubValue = "";
					if (Value < 100) {
						SubValue += "." + ((Value_00 / 10) % 10);
					}
					if (Value < 10) {
						SubValue += Value_00 % 10;
					}
					aDebugStream($"> {mConsole.Color(mConsole.tColorCode.Green, $"OK")} ({Value}{SubValue} {E}Clocks)");
					aDebugStream("");
					return (tResult.OK, 0, 0, 1);
				} catch (Exception Exception) {
					if (aIsLogEnable) {
						LineByLine([DebuggerHidden] (_) => aDebugStream(cTab + mConsole.Color(mConsole.tColorCode.Red, _)))(Exception.GetType().Name + ":  " + Exception.Message);
						LineByLine([DebuggerHidden] (_) => aDebugStream(cTab + cTab + mConsole.Color(mConsole.tColorCode.Yellow, _)))(Exception.StackTrace!);
					}
					aDebugStream(mConsole.Color(mConsole.tColorCode.Red, "> Fail"));
					aDebugStream("");
					return (tResult.Fail, 1, 0, 0);
				}
			}
			case tTestCollection Collection: {
				if (aFilters.IsEmpty() || aFilters.Any(Collection.Name.Contains)) {
					aFilters = null;
				}
				var Result = tResult.Skip;
				var OK_Count = 0;
				var SkipCount = 0;
				var FailCount = 0;
				
				var OK_CountSum = 0;
				var SkipCountSum = 0;
				var FailCountSum = 0;
				
				var StopWatch = new Stopwatch();
				StopWatch.Start();
				foreach (var Test in Collection.Tests) {
					var SubResult = Test.Run(
						LineByLine([DebuggerHidden](_) => aDebugStream(cTab + _)),
						aFilters,
						aHideSkippedTests,
						aIsLogEnable
					);
					OK_CountSum += SubResult.OK_Count;
					SkipCountSum += SubResult.SkipCount;
					FailCountSum += SubResult.FailCount;
					
					switch (SubResult.Result) {
						case tResult.OK: {
							OK_Count += 1;
							if (Result != tResult.Fail) {
								Result = tResult.OK;
							}
							break;
						}
						case tResult.Fail: {
							FailCount += 1;
							Result = tResult.Fail;
							break;
						}
						case tResult.Skip: {
							SkipCount += 1;
							break;
						}
					}
				}
				StopWatch.Stop();
				var MSec = StopWatch.ElapsedMilliseconds;
				var (Value_00, E) = MSec switch {
					>= 60 * 60 * 1000 => (MSec / 60 * 60 * 1000, "Hour"),
					>= 60 * 1000 => (MSec / 60 * 1000, "Min"),
					>= 1000 => (MSec / 1000, "Sec"),
					_ => (MSec, "mSec")
				};
				
				aDebugStream(
					tText.Concat(
						(
							"> "
						), (
							FailCountSum == 0 ? "" :
							FailCount == FailCountSum ? mConsole.Color(mConsole.tColorCode.Red, $"Fail:{FailCount} ") :
							mConsole.Color(mConsole.tColorCode.Red, $"Fail:{FailCount}|{FailCountSum} ")
						), (
							OK_CountSum == 0 ? "" :
							OK_Count == OK_CountSum ? mConsole.Color(mConsole.tColorCode.Green, $"OK:{OK_Count} ") :
							mConsole.Color(mConsole.tColorCode.Green, $"OK:{OK_Count}|{OK_CountSum} ")
						), (
							SkipCountSum == 0 ? "" :
							SkipCount == SkipCountSum ? mConsole.Color(mConsole.tColorCode.Yellow, $"Skip:{SkipCount} ") :
							mConsole.Color(mConsole.tColorCode.Yellow, $"Skip:{SkipCount}|{SkipCountSum} ")
						), (
							$"({Value_00/100}.{(Value_00/10)%10}{Value_00%10} {E})"
						)
					)
				);
				aDebugStream("");
				return (Result, FailCountSum, SkipCountSum, OK_CountSum);
			}
			default: {
				throw mError.Error("impossible");
			}
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static mStd.tAction<tText>
	LineByLine(
		mStd.tAction<tText> aWritLine
	) {
		return [DebuggerHidden](tText aLines) => {
			foreach (var Line in aLines.Split('\n')) {
				aWritLine(Line.TrimEnd('\r'));
			}
		};
	}
}
