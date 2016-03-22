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
	
	//================================================================================
	public static mStd.tFunc<tBool, mStd.tAction<tText>>
	Tests(
		params mStd.tTuple<tText, mStd.tFunc<tBool, mStd.tAction<tText>>>[] aTests
	//================================================================================
	) {
		return mStd.Func(
			(mStd.tAction<tText> aWriter) => {
				foreach (var SubTest in aTests) {
					tText Name;
					mStd.tFunc<tBool, mStd.tAction<tText>> Test;
					SubTest.MATCH(out Name, out Test);
					if (aWriter != null) {
						aWriter(Name);
						if (Test(aText => aWriter("|\t"+aText))) {
							aWriter("-> OK");
							aWriter("");
						} else {
							aWriter("-> FAIL");
							aWriter("");
							return false;
						}
					} else {
						if (!Test((tText _) => {})) {
							return false;
						}
					}
				}
				return true;
			}
		);
	}
}
