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
	// TODO: use build in functionality (see mIL_VM)
	
	//================================================================================
	private static mIL_VM.tData
	ObjAssign(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aObj._IsMutable);
		if (aArg.Match(mIL_VM.tDataType.Var, out mIL_VM.tData Arg)) {
			aObj._Value = Arg._Value;
		} else {
			aObj._Value = mIL_VM.Var(aArg)._Value;
		}
		return mIL_VM.Empty();
	}
	
	//================================================================================
	private static mIL_VM.tData
	Not(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(mIL_VM.tDataType.Bool, out tBool Arg_));
		return mIL_VM.Bool(!Arg_);
	}
	
	//================================================================================
	private static mIL_VM.tData
	And(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tBool Arg1, out tBool Arg2));
		return mIL_VM.Bool(Arg1 & Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Or(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tBool Arg1, out tBool Arg2));
		return mIL_VM.Bool(Arg1 | Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	XOr(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tBool Arg1, out tBool Arg2));
		return mIL_VM.Bool(Arg1 ^ Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Neg(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(mIL_VM.tDataType.Int, out tInt32 Arg_));
		return mIL_VM.Int(-Arg_);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Add(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Int(Arg1 + Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Sub(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Int(Arg1 - Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Mul(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Int(Arg1 * Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Div(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Int(Arg1 / Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Mod(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Int(Arg1 % Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Eq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Bool(Arg1 == Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	NEq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Bool(Arg1 != Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Le(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Bool(Arg1 < Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	LeEq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Bool(Arg1 <= Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	Gr(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Bool(Arg1 > Arg2);
	}
	
	//================================================================================
	private static mIL_VM.tData
	GrEq(
		mIL_VM.tData aEnv,
		mIL_VM.tData aObj,
		mIL_VM.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mIL_VM.Bool(Arg1 >= Arg2);
	}
	
	public static readonly mIL_VM.tData ImportData = mIL_VM.Tuple(
		mIL_VM.ExternProc(ObjAssign, mIL_VM.Empty()),
		mIL_VM.Tuple(
			mIL_VM.ExternProc(Not, mIL_VM.Empty()),
			mIL_VM.ExternProc(And, mIL_VM.Empty()),
			mIL_VM.ExternProc(Or, mIL_VM.Empty()),
			mIL_VM.ExternProc(XOr, mIL_VM.Empty())
		),
		mIL_VM.Tuple(
			mIL_VM.ExternProc(Neg, mIL_VM.Empty()),
			mIL_VM.ExternProc(Add, mIL_VM.Empty()),
			mIL_VM.ExternProc(Sub, mIL_VM.Empty()),
			mIL_VM.ExternProc(Mul, mIL_VM.Empty()),
			mIL_VM.ExternProc(Div, mIL_VM.Empty()),
			mIL_VM.ExternProc(Mod, mIL_VM.Empty())
		),
		mIL_VM.Tuple(
			mIL_VM.ExternProc(Eq, mIL_VM.Empty()),
			mIL_VM.ExternProc(NEq, mIL_VM.Empty()),
			mIL_VM.ExternProc(Gr, mIL_VM.Empty()),
			mIL_VM.ExternProc(GrEq, mIL_VM.Empty()),
			mIL_VM.ExternProc(Le, mIL_VM.Empty()),
			mIL_VM.ExternProc(LeEq, mIL_VM.Empty())
		)
	);
	
	public const tText cImportTuple = (
		"("+
			"=..., "+
			"(!..., ...&..., ...|..., ...^...), "+
			"(-..., ...+..., ...-..., ...*..., .../..., ...%...), "+
			"(...==..., ...!=..., ...>..., ...>=..., ...<..., ...<=...)"+
		")"
	);
	
	#region Test
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStdLib),
		mTest.Test(
			"IfThenElse",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						mList.List(
							$"§IMPORT ({cImportTuple}, n)",
							"",
							"§DEF If...Then...Else... = (c, i, e) => {",
							"	§RETURN (.i) IF c",
							"	§RETURN (.e)",
							"}",
							"",
							"§RECURSIV Fib... = a => .If (a .< 2) Then (",
							"	() => a",
							") Else (",
							"	() => (.Fib(a .- 2)) .+ (.Fib(a .- 1))",
							")",
							"",
							"§EXPORT .Fib n"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							ImportData,
							mIL_VM.Int(8)
						),
						aDebugStream
					),
					mIL_VM.Int(21)
				);
			}
		),
		mTest.Test(
			"If2",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						mList.List(
							$"§IMPORT ({cImportTuple}, n)",
							"",
							"§RECURSIV Fib... = a => §IF {",
							"	(a .< 2) => a",
							"	(1 .== 1) => (.Fib(a .- 2)) .+ (.Fib(a .- 1))",
							"}",
							"",
							"§EXPORT .Fib n"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							ImportData,
							mIL_VM.Int(8)
						),
						aDebugStream
					),
					mIL_VM.Int(21)
				);
			}
		),
		mTest.Test(
			"IfMatch1",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						mList.List(
							$"§IMPORT ({cImportTuple}, n)",
							"",
							"§RECURSIV Fib... = a => §IF a MATCH {",
							"	(a | a .== 0) => 0",
							"	(a | a .== 1) => 1",
							"	a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))",
							"}",
							"",
							"§EXPORT .Fib n"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							ImportData,
							mIL_VM.Int(8)
						),
						aDebugStream
					),
					mIL_VM.Int(21)
				);
			}
		),
		mTest.Test(
			"IfMatch2",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						mList.List(
							$"§IMPORT ({cImportTuple}, n)",
							"",
							"§RECURSIV Fib... = x => §IF x MATCH {",
							"	(a | a .< 2) => a",
							"	a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))",
							"}",
							"",
							"§EXPORT .Fib n"
						).Join((a1, a2) => a1 + "\n" + a2),
						mIL_VM.Tuple(
							ImportData,
							mIL_VM.Int(8)
						),
						aDebugStream
					),
					mIL_VM.Int(21)
				);
			}
		),
		mTest.Test(
			"VAR",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						mList.List(
							$"§IMPORT {cImportTuple}",
							"",
							"§DEF +... = o : i {",
							"	o :, = (o .+ i) .",
							"}",
							"",
							"§DEF *... = o : i {",
							"	o :, = (o .* i) .",
							"}",
							"",
							"§VAR x := 1 .",
							"",
							"x :, + 3, * 2 .",
							"",
							"x :",
							"  + 3",
							"  * 2",
							".",
							"",
							"§EXPORT x"
						).Join((a1, a2) => a1 + "\n" + a2),
						ImportData,
						aDebugStream
					),
					mIL_VM.Var(mIL_VM.Int(22))
				);
			}
		)
	);
	
	#endregion
}