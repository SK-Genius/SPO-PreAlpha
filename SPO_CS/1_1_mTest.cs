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
	
	public class tTestRun : tTest {
		public tText _Name;
		public mStd.tAction<mStd.tAction<tText>> _TestFunc;
	}
	
	public class tTests : tTest {
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
		{
			var TestRun = aTest as tTestRun;
			if (!TestRun.IsNull()) {
				aDebugStream(TestRun._Name);
				if (aFilters.IsNull() || aFilters.Map(aFilter => TestRun._Name.Contains(aFilter)).Any()) {
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
		}
		{
			var Tests = aTest as tTests;
			if (!Tests.IsNull()) {
				aDebugStream(Tests._Name);
				if (aFilters.IsNull() || aFilters.Map(aFilter => Tests._Name.Contains(aFilter)).Any()) {
					aFilters = null;
				}
				var Result = tResult.Skip;
				foreach (var Test in Tests._Tests) {
					switch (Test.Run(aText => aDebugStream(cTab + aText), aFilters)) {
						case tResult.OK: {
							Result = tResult.OK;
						} break;
						case tResult.Fail: {
							aDebugStream("-> Fail");
							aDebugStream("");
						} return tResult.Fail;
						case tResult.Skip: {
						} break;
					}
				}
				aDebugStream($"-> {Result}");
				aDebugStream("");
				return Result;
			}
		}
		mStd.Assert(false);
		return tResult.Fail;
	}
}
