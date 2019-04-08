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
mProgram {
	public static void
	Main(
		tText[] aArgs
	) {
		var ProjectFile = new System.IO.FileInfo(aArgs.Length > 0 ? aArgs[0] : "src/_.spo");
		var Folder = ProjectFile.Directory.FullName;
		var DebugOut = mStd.Action((mStd.tFunc<tText> a) => System.Console.Error.WriteLine(a()));
		try {
			var StdLib = mStdLib.GetImportData();
			
			var Method = mSPO_Interpreter.Run(
				System.IO.File.ReadAllText(ProjectFile.FullName),
				ProjectFile.FullName,
				mVM_Data.Record(
					("_StdLib", StdLib),
					(
						"_LoadModule...",
						mVM_Data.ExternProc(
							(aDef, aObj, aArg, _) => {
								mAssert.Assert(aObj.MatchPrefix("IO", out var X));
								mAssert.Assert(X.MatchEmpty());
								
								var File = "";
								var RestText = aArg;
								while (RestText.MatchPair(out var Char, out RestText)) {
									mAssert.Assert(Char.MatchPrefix("Char", out var Int));
									mAssert.Assert(Int.MatchInt(out var Ord));
									File += (char)Ord;
								}
								
								var Path = System.IO.Path.Combine(Folder, File);
								return mVM_Data.ExternProc(
									(aDef2, aObj2, aArg2, aDebugOut) => mSPO_Interpreter.Run(
										System.IO.File.ReadAllText(Path),
										Path,
										aArg2,
										aDebugOut
									),
									mVM_Data.Empty()
								);
							},
							mVM_Data.Empty()
						)
					)
				),
				DebugOut
			);
			
			var Result = mVM_Data.Empty();
			mVM.Run<mSpan.tSpan<mTextStream.tPos>>(
				Method,
				mVM_Data.Prefix("IO", mVM_Data.Empty()),
				mVM_Data.Empty(),
				Result,
				a => $"{a.Start.Ident}({a.Start.Row}:{a.Start.Col} .. {a.End.Row}:{a.End.Col})",
				a => {} //DebugOut
			);
			
			mAssert.Assert(Result.MatchBool(out var IsOK));
			mAssert.Assert(IsOK);
			DebugOut(() => "OK");
		} catch (mError.tError<mStd.tEmpty> Error) {
			DebugOut(() => Error.Message);
			System.Environment.Exit(-1);
		}
	}
}
