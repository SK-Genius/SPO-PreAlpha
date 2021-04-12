//IMPORT mTest.cs
//IMPORT mStdLib.cs
//IMPORT mSPO_Interpreter.cs

#nullable enable

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

public static class
mStdLib_Tests {
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mStdLib),
		mTest.Test(
			"IfThenElse",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...<...: §DEF ...<... € [[§INT, §INT] => §BOOL]
									...+...: §DEF ...+... € [[§INT, §INT] => §INT]
									...-...: §DEF ...-... € [[§INT, §INT] => §INT]
								}
								§DEF n € §INT
							)
							
							§DEF If...Then...Else... = (§DEF c € §BOOL, §DEF i € [[] => §INT], §DEF e € [[] => §INT]) => {
								§RETURN (.i) IF c
								§RETURN (.e)
							}
							
							§RECURSIVE {
								§DEF Fib... = §DEF a € §INT => .If (a .< 2) Then (
									() => a
								) Else (
									() => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
								)
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(a => aDebugStream(a())),
							mVM_Data.Int(8)
						),
						a => aDebugStream(a())
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"If2",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...<...: §DEF ...<... € [[§INT, §INT] => §BOOL]
									...==...: §DEF ...==... € [[§INT, §INT] => §BOOL]
									...+...: §DEF ...+... € [[§INT, §INT] => §INT]
									...-...: §DEF ...-... € [[§INT, §INT] => §INT]
								}
								§DEF n € §INT
							)
							
							§RECURSIVE {
								§DEF Fib... = (§DEF a € §INT) => §IF {
									a .< 2 => a
									1 .== 1 => ((.Fib(a .- 2)) .+ (.Fib(a .- 1)))
								}
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(a => aDebugStream(a())),
							mVM_Data.Int(8)
						),
						a => aDebugStream(a())
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"IfMatch1",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...==...: §DEF ...==... € [[§INT, §INT] => §BOOL]
									...+...: §DEF ...+... € [[§INT, §INT] => §INT]
									...-...: §DEF ...-... € [[§INT, §INT] => §INT]
								}
								§DEF n € §INT
							)
							
							§RECURSIVE {
								§DEF Fib... = (§DEF a € §INT) => §IF a MATCH {
									(§DEF a & a .== 0) => 0
									(§DEF a & a .== 1) => 1
									§DEF a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
								}
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(a => aDebugStream(a())),
							mVM_Data.Int(8)
						),
						a => aDebugStream(a())
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"IfMatch2",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...<...: §DEF ...<... € [[§INT, §INT] => §BOOL]
									...+...: §DEF ...+... € [[§INT, §INT] => §INT]
									...-...: §DEF ...-... € [[§INT, §INT] => §INT]
								}
								§DEF n € §INT
							)
							
							§RECURSIVE {
								§DEF Fib... = (§DEF x € §INT) => §IF x MATCH {
									(§DEF a & a .< 2) => a
									§DEF a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
								}
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(a => aDebugStream(a())),
							mVM_Data.Int(8)
						),
						a => aDebugStream(a())
					),
					mVM_Data.Int(21)
				);
			}
		),
		mTest.Test(
			"VAR",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT {
								...+...: §DEF ...+... € [[§INT, §INT] => §INT]
								...*...: §DEF ...*... € [[§INT, §INT] => §INT]
							}
							
							§DEF +... = §DEF o € §INT : §DEF i € §INT {
								o := ((§TO_VAL o) .+ i) .
							}
							
							§DEF *... = §DEF o € §INT : §DEF i € §INT {
								o := ((§TO_VAL o) .* i) .
							}
							
							§VAR x := 1 .
							
							x : + 3, * 2 .
							
							x :
								+ 3
								* 2
							.
							
							§EXPORT x
						",
						"",
						mStdLib.GetImportData(a => aDebugStream(a())),
						a => aDebugStream(a())
					),
					mVM_Data.Var(mVM_Data.Int(22))
				);
			}
		#if !true // TODO: implement methode type
		),
		mTest.Test(
			"Echo",
			aDebugStream => {
				var ReadLine = mVM_Data.ExternProc(
					(aEnv, aObj, aArg, aTrace) => {
						mAssert.IsTrue(aObj._IsMutable);
						mAssert.IsTrue(aObj.MatchVar(out var Stream));
						mAssert.IsTrue(Stream._Value.Match(out System.IO.TextReader Reader));
						var Line = Reader.ReadLine();
						return mVM_Data.Prefix("Text", new mVM_Data.tData{ _Value = mAny.Any(Line) });
					},
					mVM_Data.Empty()
				);
				var WriteLine = mVM_Data.ExternProc(
					(aEnv, aObj, aArg, aTrace) => {
						mAssert.IsTrue(aObj._IsMutable);
						mAssert.IsTrue(aObj.MatchVar(out var Stream));
						mAssert.IsTrue(aArg.MatchPrefix("Text", out var Line));
						mAssert.IsTrue(Stream._Value.Match(out  System.IO.TextWriter Writer));
						mAssert.IsTrue(Line._Value.Match(out tText Text));
						Writer.WriteLine(Text);
						Writer.Flush();
						return mVM_Data.Empty();
					},
					mVM_Data.Empty()
				);
				var Main = mSPO_Interpreter.Run(
					@"
						§IMPORT (
							§DEF ReadLine € [tStreamIn : => §TEXT]
							§DEF WriteLine... € [tStreamOut : §TEXT]
						)
						
						§EXPORT (§DEF StdIn € tStreamIn, §DEF StdOut € tStreamOut) : §DEF Args € §INT {
							StdIn : ReadLine => §DEF Line .
							StdOut : WriteLine Line .
							§RETURN 0
						}
					",
					"",
					mVM_Data.Tuple(ReadLine, WriteLine),
					a => aDebugStream(a())
				);
				
				var Reference = new byte[]{ 44, 55, 66, (int)'\n' };
				var StreamIn = mVM_Data.Var(
					new mVM_Data.tData{
						_Value = mAny.Any(
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
						_Value = mAny.Any(
							new System.IO.StreamWriter(Result)
						)
					}
				);
				
				var Res = mVM_Data.Empty();
				mVM.Run<mSpan.tSpan<mTextStream.tPos>>(
					Main,
					mVM_Data.Tuple(StreamIn, StreamOut),
					mVM_Data.Empty(),
					Res,
					a => "" + a,
					a => aDebugStream(a())
				);
				
				mAssert.AreEquals(Res, mVM_Data.Int(0));
				
				var ResArray = Result.GetBuffer();
				for (var I = 0; I < Reference.Length - 1; I += 1) {
					mAssert.AreEquals(ResArray[I], Reference[I]);
				}
			}
		#endif
		)
	);
}
