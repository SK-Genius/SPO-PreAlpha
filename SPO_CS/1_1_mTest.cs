﻿using tBool = System.Boolean;

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
	) {
		return mStd.Func(
			(mStd.tAction<tText> aWriter, mList.tList<tText> aFilters) => {
				tBool HasAnyResult = false;
				foreach (var SubTest in aTests) {
					var Buffer = mList.List<tText>();
					var Writer = mStd.Action((tText aText) => { Buffer = mList.Concat(Buffer, mList.List(aText)); });
					tText Name;
					mStd.tFunc<tResult, mStd.tAction<tText>, mList.tList<tText>> Test_;
					SubTest.MATCH(out Name, out Test_);
					var Filters = Name.Contains(((aFilters.IsNull()) ? "" : aFilters._Head) ?? "") ? aFilters.Skip(1) : aFilters;
					Writer(Name);
					switch (Test_(aText => Writer("|\t"+aText), Filters)) {
						case tResult.OK: {
							HasAnyResult = true;
							tText Head;
							while (Buffer.MATCH(out Head, out Buffer)) {
								aWriter(Head);
							}
							aWriter("-> OK");
							aWriter("");
						} break;
							
						case tResult.FAIL: {
							HasAnyResult = true;
							tText Head;
							while (Buffer.MATCH(out Head, out Buffer)) {
								aWriter(Head);
							}
							aWriter("-> FAIL");
							aWriter("");
						} return tResult.FAIL;
							
						case tResult.SKIP: {
						} break;
							
						default: {
							mStd.Assert(false);
						} break;
					}
				}
				return HasAnyResult ? tResult.OK : tResult.SKIP;
			}
		);
	}
	
	//================================================================================
	public static
	mStd.tFunc<tResult, mStd.tAction<tText>, mList.tList<tText>>
	Test(
		mStd.tFunc<tBool, mStd.tAction<tText>> aTest
	//================================================================================
	) {
		return mStd.Func(
			(
				mStd.tAction<tText> aStreamOut,
				mList.tList<tText> aFilters
			) => {
				if (aFilters.IsNull()) {
					return aTest(aStreamOut) ? tResult.OK : tResult.FAIL;
				} else {
					return tResult.SKIP;
				}
			}
		);
	}
}