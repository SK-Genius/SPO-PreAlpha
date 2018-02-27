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
				if (aFilters is null || aFilters.Map(TestRun._Name.Contains).Any()) {
					try {
						TestRun._TestFunc(aText => aDebugStream("| " + aText));
						aDebugStream("-> OK");
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
				if (aFilters is null || aFilters.Map(Tests._Name.Contains).Any()) {
					aFilters = null;
				}
				var Result = tResult.Skip;
				foreach (var Test in Tests._Tests) {
					switch (Test.Run(aText => aDebugStream(cTab + aText), aFilters)) {
						case tResult.OK: {
							Result = tResult.OK;
							break;
						}
						case tResult.Fail: {
							aDebugStream("-> Fail");
							aDebugStream("");
							return tResult.Fail;
						}
						case tResult.Skip: {
							break;
						}
					}
				}
				aDebugStream($"-> {Result}");
				aDebugStream("");
				return Result;
			}
			default: {
				throw mStd.Error("impossible");
			}
		}
	}
}
