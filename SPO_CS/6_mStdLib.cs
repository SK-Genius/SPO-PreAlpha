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
	private static readonly mVM_Data.tData
	ImportData = mIL_Interpreter.Run(
@"DEF Init
	_01 := ENV
	!... := §1ST _01
	_02 := §2ND _01
	...&... := §1ST _02
	_03 := §2ND _02
	...|... := §1ST _03
	_04 := §2ND _03
	...^... := §1ST _04
	_05 := §2ND _04
	-... := §1ST _05
	_06 := §2ND _05
	...+... := §1ST _06
	_07 := §2ND _06
	...-... := §1ST _07
	_08 := §2ND _07
	...*... := §1ST _08
	_09 := §2ND _08
	.../... := §1ST _09
	_10 := §2ND _09
	...%... := §1ST _10
	_11 := §2ND _10
	...==... := §1ST _11
	_12 := §2ND _11
	...!=... := §1ST _12
	_13 := §2ND _12
	...>... := §1ST _13
	_14 := §2ND _13
	...>=... := §1ST _14
	_15 := §2ND _14
	...<... := §1ST _15
	_16 := §2ND _15
	...<=... := §1ST _16
	_50 := . ...<=... EMPTY
	_51 := _50, EMPTY
	_52 := . ...<... EMPTY
	_53 := _52, _51
	_54 := . ...>=... EMPTY
	_55 := _54, _53
	_56 := . ...>... EMPTY
	_57 := _56, _55
	_58 := . ...!=... EMPTY
	_59 := _58, _57
	_60 := . ...==... EMPTY
	_61 := _60, _59
	_62 := . ...%... EMPTY
	_63 := _62, _61
	_64 := . .../... EMPTY
	_65 := _64, _63
	_66 := . ...*... EMPTY
	_67 := _66, _65
	_68 := . ...-... EMPTY
	_69 := _68, _67
	_70 := . ...+... EMPTY
	_71 := _70, _69
	_72 := . -... EMPTY
	_73 := _72, _71
	_74 := . ...^... EMPTY
	_75 := _74, _73
	_76 := . ...|... EMPTY
	_77 := _76, _75
	_78 := . ...&... EMPTY
	_79 := _78, _77
	_80 := . !... EMPTY
	_81 := _80, _79
	§RETURN _81 IF TRUE
DEF !...
	Res := §BOOL ARG ^ TRUE
	§RETURN Res IF TRUE
DEF ...&...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §BOOL A & B
	§RETURN Res IF TRUE
DEF ...|...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §BOOL A | B
	§RETURN Res IF TRUE
DEF ...^...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §BOOL A ^ B
	§RETURN Res IF TRUE
DEF -...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	1 := ONE
	0 := §INT 1 - 1
	Res := §INT 0 - B
	§RETURN Res IF TRUE
DEF ...+...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A + B
	§RETURN Res IF TRUE
DEF ...-...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A - B
	§RETURN Res IF TRUE
DEF ...*...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A * B
	§RETURN Res IF TRUE
DEF .../...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A * B
	§RETURN Res IF TRUE
DEF ...%...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A * B
	§RETURN Res IF TRUE
DEF ...==...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Res := §INT A == B
	§RETURN Res IF TRUE
DEF ...!=...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	Comp := §INT A == B
	Res := §BOOL Comp ^ TRUE
	§RETURN Res IF TRUE
DEF ...>...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	1 := ONE
	Comp := §INT A <=> B
	Res := §INT Comp == 1
	§RETURN Res IF TRUE
DEF ...>=...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	1 := ONE
	0 := §INT 1 - 1
	Comp := §INT A <=> B
	>? := §INT Comp == 1
	=? := §INT Comp == 0
	Res := §BOOL >? | =?
	§RETURN Res IF TRUE
DEF ...<...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	1 := ONE
	0 := §INT 1 - 1
	-1 := §INT 0 - 1
	Comp := §INT A <=> B
	Res := §INT Comp == -1
	§RETURN Res IF TRUE
DEF ...<=...
	A := §1ST ARG
	_ := §2ND ARG
	B := §1ST _
	1 := ONE
	0 := §INT 1 - 1
	-1 := §INT 0 - 1
	Comp := §INT A <=> B
	<? := §INT Comp == -1
	=? := §INT Comp == 0
	Res := §BOOL <? | =?
	§RETURN Res IF TRUE
",
		mVM_Data.Empty(),
		aLine => { }
	);
	
	public const tText cImportTuple = (
		@"("+
			"!..., ...&..., ...|..., ...^..., "+
			"-..., ...+..., ...-..., ...*..., .../..., ...%..., "+
			"...==..., ...!=..., ...>..., ...>=..., ...<..., ...<=..."+
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
								o := ((o :=>) .+ i) .
							}}
							
							§DEF *... = o : i {{
								o := ((o :=>) .* i) .
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
						var Writer = (Stream._Value._Value as System.IO.TextWriter);
						Writer.WriteLine((tText)Line._Value._Value);
						Writer.Flush();
						return mVM_Data.Empty();
					},
					mVM_Data.Empty()
				);
				var Main = mSPO_Interpreter.Run(
					$@"
						§IMPORT (
							ReadLine
							WriteLine...
						)
						
						§EXPORT (StdIn, StdOut) : Args {{
							StdIn : ReadLine => §DEF Line .
							StdOut : WriteLine Line .
							§RETURN 0
						}}
					",
					mVM_Data.Tuple(ReadLine, WriteLine),
					aDebugStream
				);
				
				var Reference = new byte[]{ 44, 55, 66, (int)'\n' };
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
				
				mStd.AssertEq(Res, mVM_Data.Int(0));
				
				var ResArray = Result.GetBuffer();
				for (var I = 0; I < Reference.Length - 1; I += 1) {
					mStd.AssertEq(ResArray[I], Reference[I]);
				}
			}
		)
	);
	
	#endregion
}