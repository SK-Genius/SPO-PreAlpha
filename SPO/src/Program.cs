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

static class mProgram {
	static void Main(tText[] aArgs) {
		var ProjectFile = new System.IO.FileInfo(aArgs.Length > 0 ? aArgs[0] : "src/_.spo");
		var Folder = ProjectFile.Directory.FullName;
		var DebugOut = mStd.Action((tText a) => System.Console.Error.WriteLine(a));
		try {
			var StdLib = mStdLib.GetImportData();
			
			var Result = mSPO_Interpreter.Run(
				System.IO.File.ReadAllText(ProjectFile.FullName),
				mVM_Data.Record(
					("_StdLib", StdLib),
					(
						"_LoadModule...",
						mVM_Data.ExternDef(
							(aDef, aObj, aArg, _) => {
								var File = "";
								var RestText = aDef;
								while (RestText.MatchPair(out var Char, out RestText)) {
									mStd.Assert(Char.MatchPrefix("Char", out var Int));
									mStd.Assert(Int.MatchInt(out var Ord));
									File += (char)Ord;
								}
								return mSPO_Interpreter.Run(
									System.IO.File.ReadAllText(
										System.IO.Path.Combine(Folder, File)
									),
									aArg,
									DebugOut
								);
							}
						)
					)
				),
				DebugOut
			);
			mStd.Assert(Result.MatchBool(out var IsOK));
			mStd.Assert(IsOK);
			DebugOut("OK");
		} catch (mStd.tError<mStd.tEmpty> Error) {
			DebugOut(Error.Message);
			System.Environment.Exit(-1);
		}
	}
}
