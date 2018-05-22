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
				if (aFilters is null || aFilters.Any(Tests._Name.Contains)) {
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
	
	#region TEST
	
	public static readonly mTest.tTest
	Test_ = mTest.Tests(
		nameof(mTest),
		mTest.Tests(
			nameof(mStd),
			mTest.Test(
				"tMaybe.ToString()",
				aStreamOut => {
					mStd.AssertEq(((mStd.tMaybe<tInt32, tText>)mStd.OK(1)).ToString(), "1");
					mStd.AssertEq(((mStd.tMaybe<tInt32, tText>)mStd.Fail("Bla")).ToString(), "FAIL: Bla");
				}
			),
			mTest.Test(
				"tMaybe.Equals()",
				aStreamOut => {
					mStd.AssertEq<mStd.tMaybe<tInt32, tText>>(mStd.OK(1), mStd.OK(1));
					mStd.AssertEq<mStd.tMaybe<tText, tText>>(mStd.OK("1"), mStd.OK("1"));
					mStd.AssertEq<mStd.tMaybe<tInt32, tText>>(mStd.Fail("Bla"), mStd.Fail("Bla"));
				}
			),
			mTest.Test(
				"tVar.ToString()",
				aStreamOut => {
					mStd.AssertEq(mStd.Any(1).ToString(), "1");
					mStd.AssertEq(mStd.Any("1").ToString(), "1");
				}
			),
			mTest.Test(
				"tVar.Equals()",
				aStreamOut => {
					mStd.AssertEq(mStd.Any(1), mStd.Any(1));
					mStd.AssertEq(mStd.Any("1"), mStd.Any("1"));
					mStd.AssertNotEq(mStd.Any(1), mStd.Any(2));
					mStd.AssertNotEq(mStd.Any(1), mStd.Any(false));
					mStd.AssertNotEq(mStd.Any("1"), mStd.Any("2"));
					mStd.AssertNotEq(mStd.Any("1"), mStd.Any(1));
				}
			)
		),
		mTest.Tests(
			nameof(mList),
			mTest.Test(
				"tList.ToString()",
				aStreamOut => {
					// TODO: mStd.AssertEq(List<tInt32>().ToString(), "[]"); ??? Maybe support static extension ToText() ?
					mStd.AssertEq(mList.List(1).ToString(), "1");
					mStd.AssertEq(mList.List(1, 2).ToString(), "1 \n2");
					mStd.AssertEq(mList.List(1, 2, 3).ToString(), "1 \n2 \n3");
					mStd.AssertEq(
						mList.Nat(1).Take(4).ToString(),
						"1 \n2 \n3 \n4"
					);
				}
			),
			mTest.Test(
				"tList.Equals()",
				aStreamOut => {
					mStd.AssertEq(mList.List<tInt32>(), mList.List<tInt32>());
					mStd.AssertEq(mList.List(1), mList.List(1));
					mStd.AssertNotEq(mList.List(1), mList.List<tInt32>());
					mStd.AssertNotEq(mList.List(1), mList.List(2));
					mStd.AssertNotEq(mList.List(1), mList.List(1, 2));
					mStd.AssertEq(
						mList.Nat(1).Take(4),
						mList.List(1, 2, 3, 4)
					);
				}
			),
			mTest.Test(
				"Concat()",
				aStreamOut => {
					mStd.AssertEq(mList.Concat(mList.List(1, 2), mList.List(3, 4)), mList.List(1, 2, 3, 4));
					mStd.AssertEq(mList.Concat(mList.List(1, 2), mList.List<tInt32>()), mList.List(1, 2));
					mStd.AssertEq(mList.Concat(mList.List<tInt32>(), mList.List(3, 4)), mList.List(3, 4));
					mStd.AssertEq(mList.Concat(mList.List<tInt32>(), mList.List<tInt32>()), mList.List<tInt32>());
				}
			),
			mTest.Test(
				"Map()",
				aStreamOut => {
					mStd.AssertEq(mList.List(1, 2, 3, 4).Map(a => a*a), mList.List(1, 4, 9, 16));
					mStd.AssertEq(mList.List<tInt32>().Map(a => a*a), mList.List<tInt32>());
				}
			),
			mTest.Test(
				"MapWithIndex()",
				aStreamOut => {
					mStd.AssertEq(
						mList.List(1, 2, 3, 4).MapWithIndex((i, a) => (i, a*a)),
						mList.List((0, 1), (1, 4), (2, 9), (3, 16))
					);
				}
			),
			mTest.Test(
				"Reduce()",
				aStreamOut => {
					mStd.AssertEq(mList.List(1, 2, 3, 4).Reduce(0, (a1, a2) => a1+a2), 10);
					mStd.AssertEq(mList.List(1).Reduce(0, (a1, a2) => a1+a2), 1);
					mStd.AssertEq(mList.List<tInt32>().Reduce(0, (a1, a2) => a1+a2), 0);
				}
			),
			mTest.Test(
				"Join()",
				aStreamOut => {
					mStd.AssertEq(mList.List("a", "b", "c", "d").Join((a1, a2) => $"{a1},{a2}"), "a,b,c,d");
					mStd.AssertEq(mList.List("a").Join((a1, a2) => $"{a1},{a2}"), "a");
					mStd.AssertEq(mList.List<tText>().Join((a1, a2) => $"{a1},{a2}"), "");
				}
			),
			mTest.Test(
				"Take()",
				aStreamOut => {
					mStd.AssertEq(mList.List(1, 2, 3, 4).Take(3), mList.List(1, 2, 3));
					mStd.AssertEq(mList.List(1, 2, 3).Take(4), mList.List(1, 2, 3));
					mStd.AssertEq(mList.List<tInt32>().Take(4), mList.List<tInt32>());
					mStd.AssertEq(mList.List(1, 2, 3).Take(0), mList.List<tInt32>());
					mStd.AssertEq(mList.List(1, 2, 3).Take(-1), mList.List<tInt32>());
				}
			),
			mTest.Test(
				"Skip()",
				aStreamOut => {
					mStd.AssertEq(mList.List(1, 2, 3, 4).Skip(3), mList.List(4));
					mStd.AssertEq(mList.List(1, 2, 3).Skip(4), mList.List<tInt32>());
					mStd.AssertEq(mList.List<tInt32>().Skip(4), mList.List<tInt32>());
					mStd.AssertEq(mList.List(1, 2, 3).Skip(0), mList.List(1, 2, 3));
				}
			),
			mTest.Test(
				"IsEmpty()",
				aStreamOut => {
					mStd.Assert(mList.List<tInt32>().IsEmpty());
					mStd.AssertNot(mList.List(1).IsEmpty());
					mStd.AssertNot(mList.List(1, 2).IsEmpty());
				
					mStd.AssertNot(mList.List<tInt32>() == new mList.tList<int>());
				}
			),
			mTest.Test(
				"Any()",
				aStreamOut => {
					mStd.AssertNot(mList.List<tBool>().Any());
					mStd.AssertNot(mList.List(false).Any());
					mStd.Assert(mList.List(true).Any());
					mStd.AssertNot(mList.List(false, false, false).Any());
					mStd.Assert(mList.List(true, true, true).Any());
					mStd.Assert(mList.List(false, false, true, false).Any());
					mStd.Assert(mList.List(1, 2, 3, 4).Map(a => a == 2).Any());
					mStd.AssertNot(mList.List(1, 3, 4).Map(a => a == 2).Any());
				}
			),
			mTest.Test(
				"Every()",
				aStreamOut => {
					mStd.AssertEq(mList.List(1, 2, 3, 4, 5).Every(2), mList.List(1, 3, 5));
					mStd.AssertEq(mList.List(1, 2).Every(2), mList.List(1));
					mStd.AssertEq(mList.List<tInt32>().Every(2), mList.List<tInt32>());
					mStd.AssertEq(mList.List(1, 2, 3).Every(1), mList.List(1, 2, 3));
				}
			)
		)
	);
	
	#endregion
}