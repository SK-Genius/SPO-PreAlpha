// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT mVM_Data
// IMPORT mStdLib
// IMPORT mSPO_Interpreter

public static class
mStdLib_Tests {
	private static tText StdLibImportLog = "";
	
	private static readonly mLazy.tLazy<(mVM_Data.tData Data, mVM_Type.tType Type)>
	StdLibImport = mLazy.Lazy(
		() => mStdLib.GetImportData(_ => { StdLibImportLog += _(); })
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mStdLib),
		[
			mTest.Test("VAR",
				aDebugStream => {
					var Import = StdLibImport.Value;
					if (StdLibImportLog is not "") {
						aDebugStream(StdLibImportLog);
					}
					
					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							"""
							§IMPORT {
								...+...: §DEF ...+... € [[§INT, §INT] => §INT]
								...*...: §DEF ...*... € [[§INT, §INT] => §INT]
							}
							
							§DEF +... = §DEF o € [§VAR §INT] : §DEF i € §INT {
								o := ((§TO_VAL o) .+ i) .
							}
							
							§DEF *... = §DEF o € [§VAR §INT] : §DEF i € §INT {
								o := ((§TO_VAL o) .* i) .
							}
							
							§VAR x := 1 .
							
							x : + 3, * 2 .
							
							x :
								+ 3
								* 2
							.
							
							§EXPORT x
							""",
							"",
							Import,
							_ => aDebugStream(_())
						).Then(
							_ => _.Data
						).ElseThrow(
						),
						mVM_Data.Var(mVM_Data.Int(22))
					);
				}
			#if !true // TODO: implement method type
			),
			mTest.Test("Echo",
				aDebugStream => {
					var ReadLine = mVM_Data.ExternProc(
						(aEnv, aObj, aArg, aTrace) => {
							mAssert.IsTrue(aObj._IsMutable);
							mAssert.IsTrue(aObj.MatchVar(out var Stream));
							mAssert.IsTrue(Stream._Value.Match(out System.IO.TextReader Reader));
							var Line = Reader.ReadLine();
							return mVM_Data.Prefix("Text", new mVM_Data.tData { _Value = mAny.Any(Line) });
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
						"""
						§IMPORT (
							§DEF ReadLine € [tStreamIn : => §TEXT]
							§DEF WriteLine... € [tStreamOut : §TEXT]
						)
						
						§EXPORT (§DEF StdIn € tStreamIn, §DEF StdOut € tStreamOut) : §DEF Args € §INT {
							StdIn : ReadLine => §DEF Line .
							StdOut : WriteLine Line .
							§RETURN 0
						}
						""",
						"",
						mVM_Data.Tuple(ReadLine, WriteLine),
						_ => aDebugStream(_())
					);
					
					var Reference = new byte[] { 44, 55, 66, (int)'\n' };
					var StreamIn = mVM_Data.Var(
						new mVM_Data.tData {
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
						new mVM_Data.tData {
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
						_ => "" + _,
						_ => aDebugStream(_())
					);
					
					mAssert.AreEquals(Res, mVM_Data.Int(0));
					
					var ResArray = Result.GetBuffer();
					for (var I = 0; I < Reference.Length - 1; I += 1) {
						mAssert.AreEquals(ResArray[I], Reference[I]);
					}
				}
			#endif
			)
		]
	);
}
