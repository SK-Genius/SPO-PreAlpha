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
		FAIL,
		SKIP
	}
	
	//================================================================================
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>>
	Tests(
		params mStd.tTuple<tText, mStd.tFunc<tResult, mStd.tAction<tText>, mList.tList<tText>>>[] aTests
	//================================================================================
	) => mStd.Func(
		(mStd.tAction<tText> aWriter, mList.tList<tText> aFilters) => {
			tBool HasAnyResult = false;
			foreach (var SubTest in aTests) {
				SubTest.MATCH(out var Name, out var Test_);
				var Filters = Name.Contains(((aFilters.IsNull()) ? "" : aFilters._Head) ?? "") ? aFilters.Skip(1) : aFilters;
				aWriter(Name);
				switch (Test_(aText => aWriter($"|\t{aText}"), Filters)) {
					case tResult.OK: {
						HasAnyResult = true;
						aWriter("-> OK");
						aWriter("");
					} break;
							
					case tResult.FAIL: {
						HasAnyResult = true;
						aWriter("-> FAIL");
						aWriter("");
					} return tResult.FAIL;
							
					case tResult.SKIP: {
						aWriter("-> SKIP");
						aWriter("");
					} break;
							
					default: {
						mStd.Assert(false);
					} break;
				}
			}
			return HasAnyResult ? tResult.OK : tResult.SKIP;
		}
	);
	
	//================================================================================
	public static
	mStd.tFunc<tResult, mStd.tAction<tText>, mList.tList<tText>>
	Test(
		mStd.tFunc<tBool, mStd.tAction<tText>> aTest
	//================================================================================
	) => mStd.Func(
		(
			mStd.tAction<tText> aStreamOut,
			mList.tList<tText> aFilters
		) => {
			if (aFilters.IsNull()) {
#				if DEBUG
					return aTest(aStreamOut) ? tResult.OK : tResult.FAIL;
#				else
					try {
						return aTest(aStreamOut) ? tResult.OK : tResult.FAIL;
					} catch {
						return tResult.FAIL;
					}
#				endif
			} else {
				return tResult.SKIP;
			}
		}
	);
}
