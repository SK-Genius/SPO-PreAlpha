#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../SPO_CS/src/Common/mAssert.cs
#:ref ../../SPO_CS/src/Common/mResult.cs
#:ref ../../SPO_CS/src/Common/mStd.cs
#:ref ../../SPO_CS/src/Common/mSpan.cs
#:ref ../../SPO_CS/src/Common/mTextStream.cs
#:ref ../../SPO_CS/src/mModule.cs
#:ref ../../SPO_CS/src/mVM.cs
#:ref ../../SPO_CS/src/mVM_Data.cs
#:ref ../../SPO_CS/src/mVM_Type.cs
#:property OutputType = Exe

public static class
mProgram {
	public static void
	Main(
		tText[] aArgs
	) {
		var ProjectFile = new System.IO.FileInfo(aArgs.Length > 0 ? aArgs[0] : "src/_.spo");
		var Folder = ProjectFile.Directory.FullName;
		var DebugOut = mStd.Action(
			(mStd.tFunc<tText> a) => {
				System.Console.Error.WriteLine(a());
				System.Console.Error.Flush();
			}
		);
		try {
			var StdLib = mModule.Module_Std.Init(__ => DebugOut(() => __)).ElseThrow();
			
			var Method = mSPO_Interpreter.Run(
				System.IO.File.ReadAllText(ProjectFile.FullName),
				ProjectFile.FullName,
				(
					mVM_Data.Record(new (tText, mVM_Data.tData)[] {
						("_StdLib", StdLib.Data),
						(
							"_LoadModule...",
							mVM_Data.ExternProc(
								(aDef, aObj, aArg, _) => {
									mAssert.IsTrue(aObj.IsPrefix("IO", out var X));
									mAssert.IsTrue(X.IsEmpty());
									
									var File = "";
									var RestText = aArg;
									while (RestText.IsPair(out var Char, out RestText)) {
										mAssert.IsTrue(Char.IsPrefix("Char", out var Int));
										mAssert.IsTrue(Int.IsInt(out var Ord));
										File += (char)Ord;
									}
									
									var Path = System.IO.Path.Combine(Folder, File);
									return mVM_Data.ExternProc(
										(aDef2, aObj2, aArg2, aDebugOut) => mSPO_Interpreter.Run(
											System.IO.File.ReadAllText(Path),
											Path,
											(aArg2, mVM_Type.Empty()),
											aDebugOut
										).ElseThrow().Data,
										mVM_Data.Empty()
									);
								},
								mVM_Data.Empty()
							)
						)
					}),
					mVM_Type.Record(new (tText, mVM_Type.tType)[] {
						("_StdLib", StdLib.Type),
						("_LoadModule...", mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty()))
					})
				),
				DebugOut
			).ElseThrow();
			
			var Result = mVM_Data.Empty();
			mVM.Run<mSpan.tSpan<mTextStream.tPos>>(
				Method.Data,
				mVM_Data.Prefix("IO", mVM_Data.Empty()),
				mVM_Data.Empty(),
				Result,
				a => $"{a.Start.Id}({a.Start.Row}:{a.Start.Col} .. {a.End.Row}:{a.End.Col})",
				a => {} //DebugOut
			);
			
			mAssert.IsTrue(Result.IsBool(out var IsOK));
			mAssert.IsTrue(IsOK);
			DebugOut(() => "OK");
		} catch (mError.tError Error) {
			DebugOut(() => Error.Message);
			System.Environment.Exit(-1);
		}
	}
}