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

using xTest = Xunit.TheoryAttribute;
using xArg = Xunit.InlineDataAttribute;
using xTrait = Xunit.TraitAttribute;

public static class mStdLib_Test {
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mStdLib),
		mTest.Test(
			"IfThenElse",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						$@"
							§IMPORT ({mStdLib.cImportTuple}, n)
							
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
							mStdLib.GetImportData(),
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
							§IMPORT ({mStdLib.cImportTuple}, n)
							
							§RECURSIV Fib... = a => §IF {{
								(a .< 2) => a
								(1 .== 1) => ((.Fib(a .- 2)) .+ (.Fib(a .- 1)))
							}}
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							mStdLib.GetImportData(),
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
							§IMPORT ({mStdLib.cImportTuple}, n)
							
							§RECURSIV Fib... = a => §IF a MATCH {{
								(a | a .== 0) => 0
								(a | a .== 1) => 1
								a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
							}}
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							mStdLib.GetImportData(),
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
							§IMPORT ({mStdLib.cImportTuple}, n)
							
							§RECURSIV Fib... = x => §IF x MATCH {{
								(a | a .< 2) => a
								a => (.Fib(a .- 2)) .+ (.Fib(a .- 1))
							}}
							
							§EXPORT .Fib n
						",
						mVM_Data.Tuple(
							mStdLib.GetImportData(),
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
							§IMPORT {mStdLib.cImportTuple}
							
							§DEF +... = o : i {{
								o := ((§TO_VAL o) .+ i) .
							}}
							
							§DEF *... = o : i {{
								o := ((§TO_VAL o) .* i) .
							}}
							
							§VAR x := 1 .
							
							x : + 3, * 2 .
							
							x :
								+ 3
								* 2
							.
							
							§EXPORT x
						",
						mStdLib.GetImportData(),
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
						mStd.Assert(Stream._Value.Match(out System.IO.TextReader Reader));
						var Line = Reader.ReadLine();
						return mVM_Data.Prefix("Text", new mVM_Data.tData{ _Value = mStd.Any(Line) });
					},
					mVM_Data.Empty()
				);
				var WriteLine = mVM_Data.ExternProc(
					(aEnv, aObj, aArg, aTrace) => {
						mStd.Assert(aObj._IsMutable);
						mStd.Assert(aObj.MatchVar(out var Stream));
						mStd.Assert(aArg.MatchPrefix("Text", out var Line));
						mStd.Assert(Stream._Value.Match(out  System.IO.TextWriter Writer));
						mStd.Assert(Line._Value.Match(out tText Text));
						Writer.WriteLine(Text);
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
	
	[xArg("IfThenElse")]
	[xArg("If2")]
	[xArg("IfMatch1")]
	[xArg("IfMatch2")]
	[xArg("VAR")]
	[xArg("Echo")]
	[xTest] public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mList.List(a)),
			mTest.tResult.OK
		);
	}
}
