// IMPORT mStd
// IMPORT mStream
// IMPORT mConsole
// IMPORT mPerf
// IMPORT mError

public static class
mTest {
	private const tText cTab = "|  ";
	
	public enum
	tResult {
		OK,
		Fail,
		Skip
	}
	
	public struct
	tTestSettings {   
		public mStream.tStream<tText> Filters;
		public tBool HasToMatchAll;
		public tBool HideSkippedTests;
		public tBool HidePassedGroups;
		public tBool HidePassedTests;
		public tInt32 OutputLevel;
		public tInt32 TreeLevel;
		public tBool DebuggerBreak;
		public tBool StopOnFirstFail;
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
		tTest[] aTests
	) => new tTestCollection { Name = aName, Tests = aTests };
	
	public static tTest
	Test(
		tText aName,
		mStd.tAction<mStd.tAction<tText>> aTestFunc,
		[CallerFilePath] tText aFile = null!,
		[CallerLineNumber] System.Int32 aLine = 0
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
		_ => throw new System.NotImplementedException(aTest.GetType().FullName)
	};
	
	[DebuggerHidden]
	public static tBool
	IsMatchingAny(
		this tTest aTest,
		mStream.tStream<tText> aFilters
	) => aFilters.IsEmpty() || aTest switch {
		tTestRun Run => aFilters.Any(Run.Name.Contains),
		tTestCollection Collection => (
			aFilters.Any(Collection.Name.Contains) ||
			System.MemoryExtensions.AsSpan(
				Collection.Tests
			).AsStream(
			).Any(
				_ => _.IsMatchingAny(aFilters)
			)
		),
		_ => throw new System.NotImplementedException(aTest.GetType().FullName),
	};
	
	[DebuggerHidden]
	public static tBool
	IsMatchingAll(
		this tTest aTest,
		mStream.tStream<tText> aFilters
	) => aFilters.IsEmpty() || aTest switch {
		tTestRun Run => aFilters.All(Run.Name.Contains),
		tTestCollection Collection => (
			aFilters.All(Collection.Name.Contains) ||
			System.MemoryExtensions.AsSpan(
				Collection.Tests
			).AsStream(
			).Any(
				_ => _.IsMatchingAll(
					aFilters.Where(
						_ => !Collection.Name.Contains(_)
					)
				)
			)
		),
		_ => throw new System.NotImplementedException(aTest.GetType().FullName),
	};
	
	[DebuggerHidden]
	public static void
	List(
		this tTest aTest,
		mStd.tAction<tText> aDebugStream,
		mStream.tStream<tText> aFilters,
		tBool aHasToMatchAll
	) {
		if (aHasToMatchAll) {
			if (!aTest.IsMatchingAll(aFilters)) {
				return;
			}
		} else {
			if (!aTest.IsMatchingAny(aFilters)) {
				return;
			}
		}
		
		var TestName = aTest.Name();
		aDebugStream(TestName);
		if (aHasToMatchAll) {
			aFilters = aFilters.Where(_ => !TestName.Contains(_));
		} else {
			if (aFilters.Any(TestName.Contains)) {
				aFilters = mStd.cEmpty;
			}
		}
		switch (aTest) {
			case tTestCollection Collection: {
				var PrintLn = mStd.Action((tText aLine) => aDebugStream(cTab + aLine));
				foreach (var Test in Collection.Tests) {
					Test.List(PrintLn, aFilters, aHasToMatchAll);
				}
				break;
			}
			case tTestRun Run: {
				aDebugStream($"[{Run.File}:{Run.Line}]");
				aDebugStream("");
				break;
			}
			default: {
				throw mError.Error("impossible");
			}
		}
	}
	
	[DebuggerHidden]
	public static (tResult Result, tInt32 FailCount, tInt32 SkipCount, tInt32 OK_Count)
	Run(
		this tTest aTest,
		mStd.tAction<tText> aDebugStream,
		tTestSettings aSettings
	) {
		System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		
		if (
			aSettings.TreeLevel <= 0 ||
			(aSettings.HideSkippedTests && !(aSettings.HasToMatchAll ? aTest.IsMatchingAll(aSettings.Filters) : aTest.IsMatchingAny(aSettings.Filters)))
		) {
			aDebugStream = _ => {};
		}
		
		var BufferedLines = mStream.Stream<tText>();
		var DebugStream = LineByLine(
			[DebuggerHidden] (_) => {
				BufferedLines = mStream.Stream(_, BufferedLines);
			}
		);
		DebugStream(aTest.Name());
		switch (aTest) {
			case tTestRun Run: {
				if (!aSettings.HidePassedTests) {
					DebugStream = aDebugStream;
					foreach (var Line in BufferedLines) {
						aDebugStream(Line);
					}
					BufferedLines = mStd.cEmpty;
				}
				
				if (aSettings.OutputLevel >= 1) {
					DebugStream($"[ {Run.File}:{Run.Line} ]");
				}
				
				if (!aSettings.Filters.IsEmpty() && !aSettings.Filters.Any(Run.Name.Contains)) {
					DebugStream(mConsole.Color(mConsole.tColorCode.Yellow, "> Skipped"));
					DebugStream("");
					return (tResult.Skip, 0, 1, 0);
				}
				
				try {
					
					var ClocksStart = mPerf.ThreadCycles();
					if (aSettings.DebuggerBreak) {
						Debugger.Launch();
					}
					Run.TestFunc(
						aSettings.OutputLevel >= 4
							? LineByLine([DebuggerHidden] (_) => DebugStream(cTab + mConsole.Color(mConsole.tColorCode.Gray, _)))
							: _ => { }
					);
					var ClocksEnd = mPerf.ThreadCycles();
					
					var Clocks = (ClocksEnd - ClocksStart);
					
					var (_100xValue, Unit) = Clocks switch {
						>= 1_000_000_000 => (100 * Clocks / 1_000_000_000, "G"),
						>= 1_000_000 => (100 * Clocks / 1_000_000, "M"),
						>= 1_000 => (100 * Clocks / 1_000, "k"),
						_ => (100 * Clocks, "")
					};
					
					DebugStream(
						tText.Concat(
							$"> {mConsole.Color(mConsole.tColorCode.Green, $"OK")}",
							$" ({_100xValue / 100}.{(_100xValue / 10) % 10}{_100xValue % 10} {Unit}Clocks)"
						)
					);
					
					DebugStream("");
					return (tResult.OK, 0, 0, 1);
				} catch (System.Exception Exception) {
					foreach (var Line in BufferedLines) {
						aDebugStream(Line);
					}
					
					if (aSettings.OutputLevel >= 2) {
						LineByLine([DebuggerHidden] (_) => aDebugStream(cTab + mConsole.Color(mConsole.tColorCode.Red, _)))(Exception.GetType().Name + ": " + Exception.Message);
					}
					
					if (aSettings.OutputLevel >= 3) {
						LineByLine([DebuggerHidden] (_) => aDebugStream(cTab + cTab + mConsole.Color(mConsole.tColorCode.Yellow, _)))(Exception.StackTrace!.Replace(":line ", ":"));
					}
					aDebugStream(mConsole.Color(mConsole.tColorCode.Red, "> Fail"));
					aDebugStream("");
					return (tResult.Fail, 1, 0, 0);
				}
			}
			case tTestCollection Collection: {
				if (aSettings.HasToMatchAll) {
					aSettings.Filters = aSettings.Filters.Where(_ => !Collection.Name.Contains(_));
				} else {
					if (aSettings.Filters.Any(Collection.Name.Contains)) {
						aSettings.Filters = mStd.cEmpty;
					}
				}
				
				var Result = tResult.Skip;
				var OK_Count = 0;
				var SkipCount = 0;
				var FailCount = 0;
				
				var OK_CountSum = 0;
				var SkipCountSum = 0;
				var FailCountSum = 0;
				
				if (!aSettings.HidePassedGroups && !aSettings.HidePassedTests) {
					DebugStream = DebugStream;
				}
				
				var StopWatch = new Stopwatch();
				StopWatch.Start();
				foreach (var Test in Collection.Tests) {
					var SubResult = Test.Run(
						LineByLine(_ => { DebugStream(cTab + _); }),
						mStd.With(
							aSettings,
							static _ => {
								_.TreeLevel -= 1;
								return _;
							}
						)
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
							foreach (var Line in BufferedLines.Reverse()) {
								aDebugStream(Line);
							}
							BufferedLines = mStd.cEmpty;
							if (!aSettings.HidePassedTests) {
								DebugStream = aDebugStream;
							}
							break;
						}
						case tResult.Skip: {
							SkipCount += 1;
							break;
						}
						default: {
							throw mError.Error("impossible");
						}
					}
					
					if (aSettings.StopOnFirstFail && FailCount > 0) {
						break;
					}
				}
				StopWatch.Stop();
				var MSec = StopWatch.ElapsedMilliseconds;
				var (_100xValue, Unit) = MSec switch {
					>= 60 * 60_000 => (100 * MSec / (60 * 60_000), "Hour"),
					>= 60_000 => (100 * MSec / 60_000, "Min"),
					>= 1000 => (100 * MSec / 1000, "Sec"),
					_ => (100 * MSec, "mSec")
				};
				
				if (FailCount > 0) {
					DebugStream = aDebugStream;
				}
				
				DebugStream(
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
							$"({_100xValue / 100}.{(_100xValue / 10) % 10}{_100xValue % 10} {Unit})"
						)
					)
				);
				DebugStream("");
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
	) => [DebuggerHidden] (aLines) => {
		foreach (var Line in aLines.Split('\n')) {
			aWritLine(Line.TrimEnd('\r'));
		}
	};
}
