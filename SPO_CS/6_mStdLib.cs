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
	private static mVM_Data.tData
	ObjAssign(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aObj._IsMutable);
		if (aArg.MatchVar(out var Arg)) {
			aObj._Value = Arg._Value;
		} else {
			aObj._Value = mVM_Data.Var(aArg)._Value;
		}
		return mVM_Data.Empty();
	}
	
	//================================================================================
	private static mVM_Data.tData
	Not(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.MatchBool(out var Arg_));
		return mVM_Data.Bool(!Arg_);
	}
	
	//================================================================================
	private static mVM_Data.tData
	And(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tBool Arg1, out tBool Arg2));
		return mVM_Data.Bool(Arg1 & Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Or(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tBool Arg1, out tBool Arg2));
		return mVM_Data.Bool(Arg1 | Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	XOr(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tBool Arg1, out tBool Arg2));
		return mVM_Data.Bool(Arg1 ^ Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Neg(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.MatchInt(out var Arg_));
		return mVM_Data.Int(-Arg_);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Add(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Int(Arg1 + Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Sub(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Int(Arg1 - Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Mul(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Int(Arg1 * Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Div(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Int(Arg1 / Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Mod(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Int(Arg1 % Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Eq(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Bool(Arg1 == Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	NEq(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Bool(Arg1 != Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Le(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Bool(Arg1 < Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	LeEq(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Bool(Arg1 <= Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	Gr(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Bool(Arg1 > Arg2);
	}
	
	//================================================================================
	private static mVM_Data.tData
	GrEq(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	//================================================================================
	) {
		mStd.Assert(aArg.Match(out tInt32 Arg1, out tInt32 Arg2));
		return mVM_Data.Bool(Arg1 >= Arg2);
	}
	
	public static readonly mVM_Data.tData ImportData = mVM_Data.Tuple(
		mVM_Data.ExternProc(ObjAssign, mVM_Data.Empty()),
		mVM_Data.Tuple(
			mVM_Data.ExternProc(Not, mVM_Data.Empty()),
			mVM_Data.ExternProc(And, mVM_Data.Empty()),
			mVM_Data.ExternProc(Or, mVM_Data.Empty()),
			mVM_Data.ExternProc(XOr, mVM_Data.Empty())
		),
		mVM_Data.Tuple(
			mVM_Data.ExternProc(Neg, mVM_Data.Empty()),
			mVM_Data.ExternProc(Add, mVM_Data.Empty()),
			mVM_Data.ExternProc(Sub, mVM_Data.Empty()),
			mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
			mVM_Data.ExternProc(Div, mVM_Data.Empty()),
			mVM_Data.ExternProc(Mod, mVM_Data.Empty())
		),
		mVM_Data.Tuple(
			mVM_Data.ExternProc(Eq, mVM_Data.Empty()),
			mVM_Data.ExternProc(NEq, mVM_Data.Empty()),
			mVM_Data.ExternProc(Gr, mVM_Data.Empty()),
			mVM_Data.ExternProc(GrEq, mVM_Data.Empty()),
			mVM_Data.ExternProc(Le, mVM_Data.Empty()),
			mVM_Data.ExternProc(LeEq, mVM_Data.Empty())
		)
	);
	
	public const tText cImportTuple = (
		@"("+
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
						$@"
							§IMPORT ({cImportTuple}, n)
							
							§DEF If...Then...Else... = (c, i, e) => {{
								§RETURN (.i) IF c
								§RETURN (.e)
							}}
							
							§RECURSIV Fib... = a => .If (a .< 2) Then (
								() => a
							) Else (
								() => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
							)
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							ImportData,
							mVM_Data.Int(8)
						),
						aDebugStream
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"If2",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						$@"
							§IMPORT ({cImportTuple}, n)
							
							§RECURSIV Fib... = a => §IF {{
								(a .< 2) => a
								(1 .== 1) => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
							}}
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							ImportData,
							mVM_Data.Int(8)
						),
						aDebugStream
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"IfMatch1",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						$@"
							§IMPORT ({cImportTuple}, n)
							
							§RECURSIV Fib... = a => §IF a MATCH {{
								(a | a .== 0) => 0
								(a | a .== 1) => 1
								a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
							}}
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							ImportData,
							mVM_Data.Int(8)
						),
						aDebugStream
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"IfMatch2",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						$@"
							§IMPORT ({cImportTuple}, n)
							
							§RECURSIV Fib... = x => §IF x MATCH {{
								(a | a .< 2) => a
								a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
							}}
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							ImportData,
							mVM_Data.Int(8)
						),
						aDebugStream
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"VAR",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						$@"
							§IMPORT {cImportTuple}
							
							§DEF +... = o : i {{
								o : = (o .+ i) .
							}}
							
							§DEF *... = o : i {{
								o : = (o .* i) .
							}}
							
							§VAR x := 1 .
							
							x : + 3, * 2 .
							
							x :
							  + 3
							  * 2
							.
							
							§EXPORT x
						",
						ImportData,
						aDebugStream
					),
					mVM_Data.Var(mVM_Data.Int(22))
				);
			}
		),
		mTest.Test(
			"Echo",
			aDebugStream => {
#if !true
				var ReadLine = mVM_Data.ExternProc(
					(aEnv, aObj, aArg, aTrace) => {
						mStd.Assert(aObj._IsMutable);
						mStd.Assert(aObj.MatchVar(out var Stream));
						var Line = ((System.IO.TextReader)Stream._Value._Value).ReadLine();
						return mVM_Data.Prefix("Text", new mVM_Data.tData{ _Value = mStd.Any(Line) });
					},
					mVM_Data.Empty()
				);
				var WriteLine = mVM_Data.ExternProc(
					(aEnv, aObj, aArg, aTrace) => {
						mStd.Assert(aObj._IsMutable);
						mStd.Assert(aObj.MatchVar(out var Stream));
						mStd.Assert(aArg.MatchPrefix("Text", out var Line));
						(Stream._Value._Value as System.IO.TextWriter).WriteLine((tText)Line._Value._Value);
						return mVM_Data.Empty();
					},
					mVM_Data.Empty()
				);
				var Main = mSPO_Interpreter.Run(
					$@"
						§IMPORT (
							ReadLine...
							WriteLine...
						)
							
						§EXPORT (StdIn, StdOut) : Args {{
							StdIn : ReadLine => §DEF Line .
							StdOut : WriteLine Line .
						}}
					",
					mVM_Data.Tuple(ReadLine, WriteLine),
					aDebugStream
				);
				
				var Reference = new byte[]{ 44, 55, 66 };
				var StreamIn = mVM_Data.Var(
					new mVM_Data.tData{
						_Value = mStd.Any(
							new System.IO.StreamReader(
								new System.IO.MemoryStream(
									Reference
								)
							)
						)
					}
				);
				var Result = new System.IO.MemoryStream();
				var StreamOut = mVM_Data.Var(
					new mVM_Data.tData{
						_Value = mStd.Any(
							new System.IO.StreamWriter(Result)
						)
					}
				);
				
				var Res = mVM_Data.Empty();
				mVM.Run(
					Main,
					mVM_Data.Tuple(StreamIn, StreamOut),
					mVM_Data.Empty(),
					Res,
					a => aDebugStream(a())
				);
				
				mStd.AssertEq(Result.GetBuffer(), Reference);
#endif
			}
		)
	);
	
	#endregion
}