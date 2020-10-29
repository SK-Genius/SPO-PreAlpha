//IMPORT mStream.cs

#nullable enable

using System.Runtime.CompilerServices;

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

public static class
mTest {
	
	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern bool
	QueryThreadCycleTime(
		System.IntPtr aThreadHandle,
		out tNat64 aCycles
	);
	
	private static readonly System.IntPtr PseudoHandle = (System.IntPtr)(-2);
	
	public enum
	tResult {
		OK,
		Fail,
		Skip
	}
	
	public interface
	tTest {
	}
	
	public struct
	tTestRun : tTest {
		internal tText _Name { get; init; }
		internal mStd.tAction<mStd.tAction<tText>> _TestFunc { get; init; }
		internal tText _File { get; init; }
		internal tInt32 _Line { get; init; }
	}
	
	public struct
	tTests : tTest {
		internal tText _Name { get; init; }
		internal tTest[] _Tests { get; init; }
	}
	
	public static tTest
	Tests(
		tText aName,
		params tTest[] aTests
	) => new tTests { _Name = aName, _Tests = aTests };
	
	public static tTest
	Test(
		tText aName,
		mStd.tAction<mStd.tAction<tText>> aTestFunc,
    [CallerFilePath] tText aFile = null!,
		[CallerLineNumber] tInt32 aLine = 0
	) => new tTestRun {
		_Name = aName,
		_TestFunc = aTestFunc,
		_File = aFile,
		_Line = aLine,
	};
	
	public static (tResult Result, tInt32 FailCount, tInt32 SkipCount, tInt32 OK_Count)
	Run(
		this tTest aTest,
		mStd.tAction<tText> aDebugStream,
		mStream.tStream<tText>? aFilters
	) {
		System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
		
		const tText cTab = "|  ";
		switch (aTest) {
			case tTestRun TestRun: {
				aDebugStream(TestRun._Name);
				aDebugStream($"[{TestRun._File}:{TestRun._Line}]");
				if (aFilters.IsEmpty() || aFilters.Any(TestRun._Name.Contains)) {
					try {
						QueryThreadCycleTime(PseudoHandle, out var ClocksStart);
						TestRun._TestFunc(LineByLine(aDebugStream.AddPrefix(cTab)));
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
						aDebugStream($"> OK ({Value}{SubValue} {E}Clocks)");
						aDebugStream("");
						return (tResult.OK, 0, 0, 1);
					} catch (System.Exception Exception) {
						LineByLine(aDebugStream.AddPrefix(cTab))(Exception.Message);
						LineByLine(aDebugStream.AddPrefix(cTab + cTab))(Exception.StackTrace!);
						aDebugStream("> Fail");
						aDebugStream("");
						return (tResult.Fail, 1, 0, 0);
					}
				} else {
					aDebugStream("> Skip");
					aDebugStream("");
					return (tResult.Skip, 0, 1, 0);
				}
			}
			case tTests Tests: {
				aDebugStream(Tests._Name);
				if (aFilters.IsEmpty() || aFilters.Any(Tests._Name.Contains)) {
					aFilters = null;
				}
				var Result = tResult.Skip;
				var OK_Count = 0;
				var SkipCount = 0;
				var FailCount = 0;
				
				var OK_CountSum = 0;
				var SkipCountSum = 0;
				var FailCountSum = 0;
				
				var StopWatch = new System.Diagnostics.Stopwatch();
				StopWatch.Start();
				foreach (var Test in Tests._Tests) {
					var SubResult = Test.Run(LineByLine(aDebugStream.AddPrefix(cTab)), aFilters);
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
				var E = "mSec";
				var Value_00 = MSec * 100;
					switch (MSec) {
						case >= 60 * 60 * 1000: {
							E = "Hour";
							Value_00 /= 60 * 60 * 1000;
							break;
						}
						case >= 60 * 1000: {
							E = "Min";
							Value_00 /= 60 * 1000;
							break;
						}
						case >= 1000: {
							E = "Sec";
							Value_00 /= 1000;
							break;
						}
					}
					
					aDebugStream(
					tText.Concat(
						(
							"> "
						), (
							FailCountSum == 0 ? "" :
							FailCount == FailCountSum ? $"Fail:{FailCount} " :
							$"Fail:{FailCount}|{FailCountSum} "
						), (
							OK_CountSum == 0 ? "" :
							OK_Count == OK_CountSum ? $"OK:{OK_Count} " :
							$"OK:{OK_Count}|{OK_CountSum} "
						), (
							SkipCountSum == 0 ? "" :
							SkipCount == SkipCountSum ? $"Skip:{SkipCount} " :
							$"Skip:{SkipCount}|{SkipCountSum} "
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
	
	private static mStd.tAction<tText>
	LineByLine(
		mStd.tAction<tText> aWritLine
	) {
		return (tText aLines) => {
			foreach (var Line in aLines.Split('\n')) {
				aWritLine(Line.TrimEnd('\r'));
			}
		};
	}
	
	private static mStd.tAction<tText>
	AddPrefix(
		this mStd.tAction<tText> aWritLine,
		tText aPrefix
	) => (tText aLine) => {
		aWritLine(aPrefix + aLine);
	};
}
