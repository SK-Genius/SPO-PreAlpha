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

public static class mStdLib {
	//================================================================================
	private static bool
	MATCH(
		this mIL_VM.tData aArg,
		out tBool a1,
		out tBool a2
	//================================================================================
	) {
		mStd.Assert(
			aArg.MATCH(
				mIL_VM.tDataType.PAIR,
				out mIL_VM.tData Arg1,
				out mIL_VM.tData Arg2_
			)
		);
		mStd.Assert(
			Arg2_.MATCH(
				mIL_VM.tDataType.PAIR,
				out mIL_VM.tData Arg2,
				out mIL_VM.tData Arg_
			)
		);
		mStd.AssertEq(Arg_._DataType, mIL_VM.tDataType.EMPTY);
		mStd.Assert(Arg1.MATCH(mIL_VM.tDataType.BOOL, out a1));
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.BOOL, out a2));
		return true;
	}
	
	//================================================================================
	private static bool
	MATCH(
		this mIL_VM.tData aArg,
		out tInt32 a1,
		out tInt32 a2
	//================================================================================
	) {
		mStd.Assert(
			aArg.MATCH(
				mIL_VM.tDataType.PAIR,
				out mIL_VM.tData Arg1,
				out mIL_VM.tData Arg2_
			)
		);
		mStd.Assert(
			Arg2_.MATCH(
				mIL_VM.tDataType.PAIR,
				out mIL_VM.tData Arg2,
				out mIL_VM.tData Arg_
			)
		);
		mStd.AssertEq(Arg_._DataType, mIL_VM.tDataType.EMPTY);
		mStd.Assert(Arg1.MATCH(mIL_VM.tDataType.INT, out a1));
		mStd.Assert(Arg2.MATCH(mIL_VM.tDataType.INT, out a2));
		return true;
	}
	
	//================================================================================
	private static mIL_VM.tData
	Not(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(mIL_VM.tDataType.BOOL, out tBool Arg_));
		return mIL_VM.BOOL(!Arg_);
	}
	
	//================================================================================
	private static mIL_VM.tData
	And(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tBool Arg1, out tBool Arg2));
		return mIL_VM.BOOL(Arg1 & Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Or(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tBool Arg1, out tBool Arg2));
		return mIL_VM.BOOL(Arg1 | Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	XOr(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tBool Arg1, out tBool Arg2));
		return mIL_VM.BOOL(Arg1 ^ Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	IfElse(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(mIL_VM.tDataType.PAIR, out mIL_VM.tData Arg1, out mIL_VM.tData Arg23_));
		mStd.Assert(Arg23_.MATCH(mIL_VM.tDataType.PAIR, out mIL_VM.tData Arg2, out mIL_VM.tData Arg3_));
		mStd.Assert(Arg3_.MATCH(mIL_VM.tDataType.PAIR, out mIL_VM.tData Arg3, out mIL_VM.tData Arg_));
		mStd.Assert(Arg1.MATCH(mIL_VM.tDataType.BOOL, out tBool Arg1_));
		mStd.AssertEq(Arg_._DataType, mIL_VM.tDataType.EMPTY);
		var Res = new mIL_VM.tData();
		mIL_VM.Run(Arg1_ ? Arg2 : Arg3, mIL_VM.EMPTY(), mIL_VM.EMPTY(), Res);
		return Res;
	}
	
	//================================================================================
	private static mIL_VM.tData
	Neg(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(mIL_VM.tDataType.INT, out tInt32 Arg_));
		return mIL_VM.INT(-Arg_);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Add(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.INT(Arg1 + Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Sub(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.INT(Arg1 - Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Mul(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.INT(Arg1 * Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Div(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.INT(Arg1 / Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Mod(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.INT(Arg1 % Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Eq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.BOOL(Arg1 == Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	NEq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.BOOL(Arg1 != Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Le(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.BOOL(Arg1 < Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	LeEq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.BOOL(Arg1 <= Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Gr(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.BOOL(Arg1 > Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	GrEq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg
	//================================================================================
	) {
		mStd.Assert(aArg.MATCH(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.BOOL(Arg1 >= Arg2);
	}
	
	public static readonly mIL_VM.tData ImportData = mIL_VM.TUPLE(
		mIL_VM.TUPLE(
			mIL_VM.EXTERN_PROC(Not, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(And, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Or, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(XOr, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(IfElse, mIL_VM.EMPTY())
		),
		mIL_VM.TUPLE(
			mIL_VM.EXTERN_PROC(Neg, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Add, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Sub, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Mul, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Div, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Mod, mIL_VM.EMPTY())
		),
		mIL_VM.TUPLE(
			mIL_VM.EXTERN_PROC(Eq, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(NEq, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Gr, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(GrEq, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(Le, mIL_VM.EMPTY()),
			mIL_VM.EXTERN_PROC(LeEq, mIL_VM.EMPTY())
		)
	);
	
	const tText cImportTuple = (
		"("+
			"(!..., ...&..., ...|..., ...^..., If...Then...Else...), "+
			"(-..., ...+..., ...-..., ...*..., .../..., ...%...), "+
			"(...==..., ...!=..., ...>..., ...>=..., ...<..., ...<=...)"+
		")"
	);
	
	#region Test
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"Fib",
			mTest.Test(
				(mStd.tAction<tText> aDebugStream) => {
					mStd.AssertEq(
						mSPO_Interpreter.Run(
							mList.List(
								$"§IMPORT {cImportTuple}",
								"",
								"§RECURSIV {",
								"	Fib... := (",
								"		a => (",
								"			.If (a .< 2) Then (",
								"				() => a",
								"			) Else (",
								"				() => ((.Fib(a .- 2)) .+ (.Fib(a .- 1)))",
								"			)",
								"		)",
								"	)",
								"}",
								"",
								"§EXPORT (.Fib 5)",
								""
							).Join((a1, a2) => a1 + "\n" + a2),
							mIL_VM.TUPLE(
								ImportData,
								mIL_VM.INT(6)
							),
							aDebugStream
						),
						mIL_VM.INT(8)
					);
					return true;
				}
			)
		)
	);
	#endregion
}