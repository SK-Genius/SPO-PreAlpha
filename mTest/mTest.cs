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

public static class mTest {
	
	[System.Runtime.InteropServices.DllImport("kernel32.dll")]
	private static extern bool QueryThreadCycleTime(System.IntPtr aThreadHandle, out tNat64 aCycles);
	private static readonly System.IntPtr PseudoHandle = (System.IntPtr)(-2);
	
	public enum tResult {
		OK,
		Fail,
		Skip
	}
	
	public interface tTest {
	}
	
	public struct tTestRun : tTest {
		public tText _Name;
		public mStd.tAction<mStd.tAction<tText>> _TestFunc;
	}
	
	public struct tTests : tTest {
		public tText _Name;
		public tTest[] _Tests;
	}
	
	//================================================================================
	public static tTest
	Tests(
		tText aName,
		params tTest[] aTests
	//================================================================================
	) => new tTests { _Name = aName, _Tests = aTests };
	
	//================================================================================
	public static tTest
	Test(
		tText aName,
		mStd.tAction<mStd.tAction<tText>> aTestFunc
	//================================================================================
	) => new tTestRun {
		_Name = aName,
		_TestFunc = aTestFunc
	};
	
	//================================================================================
	public static tResult
	Run(
		this tTest aTest,
		mStd.tAction<tText> aDebugStream,
		mList.tList<tText> aFilters
	//================================================================================
	) {
		const tText cTab = "|  ";
		switch (aTest) {
			case tTestRun TestRun: {
				aDebugStream(TestRun._Name);
				if (aFilters is null || aFilters.Any(TestRun._Name.Contains)) {
					try {
						QueryThreadCycleTime(PseudoHandle, out var ClocksStart);
						TestRun._TestFunc(aText => aDebugStream("| " + aText));
						QueryThreadCycleTime(PseudoHandle, out var ClocksEnd);
						
						var Value_00 = (ClocksEnd - ClocksStart) * 100;
						var E = "";
						if (Value_00 >= 1_000_000_000_00) {
							E = "G";
							Value_00 /= 1_000_000_000;
						} else if (Value_00 >= 1_000_000_00) {
							E = "M";
							Value_00 /= 1_000_000;
						} else if (Value_00 >= 1_000_00) {
							E = "k";
							Value_00 /= 1_000;
						}
						var Value = Value_00 / 100;
						var SubValue = "";
						if (Value < 100) {
							SubValue += "." + ((Value_00 / 10) % 10);
						}
						if (Value < 10) {
							SubValue += Value_00 % 10;
						}
						aDebugStream($"-> OK ({Value}{SubValue}{E} Clocks)");
						aDebugStream("");
						return tResult.OK;
					} catch (System.Exception e) {
						aDebugStream(cTab + e.Message);
						aDebugStream("-> Fail");
						aDebugStream("");
						return tResult.Fail;
					}
				} else {
					aDebugStream("-> Skip");
					aDebugStream("");
					return tResult.Skip;
				}
			}
			case tTests Tests: {
				aDebugStream(Tests._Name);
				if (aFilters is null || aFilters.Any(Tests._Name.Contains)) {
					aFilters = null;
				}
				var Result = tResult.Skip;
				var StopWatch = new System.Diagnostics.Stopwatch();
				StopWatch.Start();
				foreach (var Test in Tests._Tests) {
					switch (Test.Run(aText => aDebugStream(cTab + aText), aFilters)) {
						case tResult.OK: {
							Result = tResult.OK;
							break;
						}
						case tResult.Fail: {
							Result = tResult.Fail;
							break;
						}
						case tResult.Skip: {
							break;
						}
					}
				}
				StopWatch.Stop();
				var MSec = StopWatch.ElapsedMilliseconds;
				var E = "mSec";
				var Value_00 = MSec * 100;
				if (MSec >= 60 * 60 * 1000) {
					E = "Hour";
					Value_00 /= 60 * 60 * 1000;
				} else if (MSec >= 60 * 1000) {
					E = "Min";
					Value_00 /= 60 * 1000;
				} else if (MSec >= 1000) {
					E = "Sec";
					Value_00 /= 1000;
				}
				
				aDebugStream($"-> {Result} ({Value_00/100}.{(Value_00/10)%10}{Value_00%10} {E})");
				aDebugStream("");
				return Result;
			}
			default: {
				throw mStd.Error("impossible");
			}
		}
	}
}
