﻿//IMPORT mTest.cs
//IMPORT mStdLib.cs
//IMPORT mSPO_Interpreter.cs

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

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class
mStdLib_Test {
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStdLib),
		mTest.Test(
			"IfThenElse",
			aDebugStream => {
				mAssert.Equals(
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
							
							§RECURSIV {
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
							mStdLib.GetImportData(),
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
				mAssert.Equals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...<...: §DEF ...<...
									...==...: §DEF ...==...
									...+...: §DEF ...+...
									...-...: §DEF ...-...
								}
								§DEF n
							)
							
							§RECURSIV {
								§DEF Fib... = §DEF a => §IF {
									a .< 2 => a
									1 .== 1 => ((.Fib(a .- 2)) .+ (.Fib(a .- 1)))
								}
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(),
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
				mAssert.Equals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...==...: §DEF ...==...
									...+...: §DEF ...+...
									...-...: §DEF ...-...
								}
								§DEF n
							)
							
							§RECURSIV {
								§DEF Fib... = §DEF a => §IF a MATCH {
									(§DEF a & a .== 0) => 0
									(§DEF a & a .== 1) => 1
									§DEF a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
								}
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(),
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
				mAssert.Equals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								{
									...<...: §DEF ...<...
									...+...: §DEF ...+...
									...-...: §DEF ...-...
								}
								§DEF n
							)
							
							§RECURSIV {
								§DEF Fib... = §DEF x => §IF x MATCH {
									(§DEF a & a .< 2) => a
									§DEF a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
								}
							}
							
							§EXPORT .Fib n
						",
						"",
						mVM_Data.Tuple(
							mStdLib.GetImportData(),
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
				mAssert.Equals(
					mSPO_Interpreter.Run(
						@"
							§IMPORT {
								...+...: §DEF ...+...
								...*...: §DEF ...*...
							}
							
							§DEF +... = §DEF o : §DEF i {
								o := ((§TO_VAL o) .+ i) .
							}
							
							§DEF *... = §DEF o : §DEF i {
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
						mStdLib.GetImportData(),
						a => aDebugStream(a())
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
						mAssert.True(aObj._IsMutable);
						mAssert.True(aObj.MatchVar(out var Stream));
						mAssert.True(Stream._Value.Match(out System.IO.TextReader Reader));
						var Line = Reader.ReadLine();
						return mVM_Data.Prefix("Text", new mVM_Data.tData{ _Value = mAny.Any(Line) });
					},
					mVM_Data.Empty()
				);
				var WriteLine = mVM_Data.ExternProc(
					(aEnv, aObj, aArg, aTrace) => {
						mAssert.True(aObj._IsMutable);
						mAssert.True(aObj.MatchVar(out var Stream));
						mAssert.True(aArg.MatchPrefix("Text", out var Line));
						mAssert.True(Stream._Value.Match(out  System.IO.TextWriter Writer));
						mAssert.True(Line._Value.Match(out tText Text));
						Writer.WriteLine(Text);
						Writer.Flush();
						return mVM_Data.Empty();
					},
					mVM_Data.Empty()
				);
				var Main = mSPO_Interpreter.Run(
					@"
						§IMPORT (
							§DEF ReadLine
							§DEF WriteLine...
						)
						
						§EXPORT (§DEF StdIn, §DEF StdOut) : §DEF Args {
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
				
				mAssert.Equals(Res, mVM_Data.Int(0));
				
				var ResArray = Result.GetBuffer();
				for (var I = 0; I < Reference.Length - 1; I += 1) {
					mAssert.Equals(ResArray[I], Reference[I]);
				}
			}
		)
	);
	
	#if NUNIT
	[xTestCase("IfThenElse")]
	[xTestCase("If2")]
	[xTestCase("IfMatch1")]
	[xTestCase("IfMatch2")]
	[xTestCase("VAR")]
	[xTestCase("Echo")]
	public static void _(tText a) {
		mAssert.Equals(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}