#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mStream.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mArrayList.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
#:ref Common/mFS.cs
#:ref mVM_Data.cs
#:ref mVM.cs
#:ref mIL_AST.cs
#:ref mIL_Parser.cs
#:ref mSPO2IL.cs
#:ref mSPO_AST.cs
#:ref mSPO_AST_Types.cs
#:ref mSPO_Parser.cs
#:ref mSPO_Interpreter.cs
#:ref mModule.cs

public static class
mE2E_Tests {
	private static readonly mFS.tFolder
	cTestFilesFolder = mFS.CWD() / "TestFiles" / "Functions";
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mE2E_Tests),
		mStd.Call(
			() => {
				if (!cTestFilesFolder.Exists()) {
					System.Console.WriteLine("Folder not found: " + cTestFilesFolder);
					return [];
				}
				
				var Tests = mArrayList.List<mTest.tTest>();
				foreach (var SPO_File in cTestFilesFolder.GetFiles().Where(__ => __.Name.EndsWith(".SPO"))) {
					var ResFile = cTestFilesFolder.GetFile(SPO_File.Name.Replace(".SPO", ".result.SPO"));
					var ILT_File = cTestFilesFolder.GetFile(SPO_File.Name.Replace(".SPO", ".ILT"));
					if (
						SPO_File.Name.StartsWith("_") ||
						SPO_File.Name.EndsWith(".result.SPO") ||
						!SPO_File.Exists()
					) {
						continue;
					}
					
					var SPO_Text = mLazy.Lazy(() => SPO_File.TryReadText().ElseThrow());
					var SPO_ResText = mLazy.Lazy(() => ResFile.TryReadText().ElseThrow());
					var IL_Text = ILT_File.Exists()
						? mLazy.Lazy(() => ILT_File.TryReadText().ElseThrow())
						: "";
					
					var ResRes = mLazy.Lazy(
						() => {
							var Log = "";
							var WriteToLog = (mStd.tFunc<tText> aGetLine) => {
								Log +="\n" + aGetLine();
							};
							var Result = mSPO_Interpreter.Run(
								SPO_ResText.Value,
								(cTestFilesFolder._Path / SPO_File.Name).ToText(),
								(mVM_Data.Empty(), mVM_Type.Empty()),
								__ => WriteToLog(__)
							).ElseThrow();
							return (Result, Log);
						}
					);
					
					Tests.Push(
						mTest.Tests(
							SPO_File.Name.Replace(".SPO", ""),
							[
								mTest.Test(
									".SPO == .result.SPO",
									aDebug => {
										var Module_Std = mModule.Module_Std.Init(aDebug).ElseThrow();
										
										var SPO_Res = mSPO_Interpreter.Run(
											SPO_Text.Value,
											(cTestFilesFolder._Path / SPO_File.Name).ToText(),
											(Module_Std.Data, Module_Std.Type),
											__ => aDebug(__())
										).ElseThrow();
										aDebug(ResRes.Value.Log);
										mAssert.IsTrue(
											ResRes.Value.Result.Type.IsSubType(SPO_Res.Type, mStd.cEmpty).Match(out _, out var Error),
											Error
										);
										mAssert.AreEquals(
											SPO_Res.Data.ToText(1000),
											ResRes.Value.Result.Data.ToText(1000)
										);
									},
									SPO_File + ", " + mStd.File()
								),
								mTest.Test(
									".SPO -> .ILT",
									aDebug => {
										var IL_TextNew = mSPO_Parser.Module.ParseText(
												SPO_Text.Value,
												(cTestFilesFolder._Path / SPO_File.Name).ToText(),
												__ => aDebug(__())
											).ToILT();
										
										if (IL_TextNew.Replace("\r", "") != IL_Text.Value.Replace("\r", "")) {
											cTestFilesFolder.GetFile(
												ILT_File.Name + ".new"
											).TryCreate(
												IL_TextNew
											);
											mAssert.Fail(
												mAssert.DiffText(
													IL_TextNew,
													IL_Text.Value
												)
											);
										}
									},
									SPO_File + ", " + ILT_File + ", " + mStd.File()
								),
								mTest.Test(
									".ILT == .result.SPO",
									aDebug => {
										var Module_Std = mModule.Module_Std.Init(aDebug).ElseThrow();
										
										var IlModule = mIL_Parser.Module.ParseText(
											IL_Text.Value,
											(cTestFilesFolder._Path / ILT_File.Name).ToText(),
											__ => aDebug(__())
										);
										var IL_Res = mVM.Run(
											IlModule,
											(Module_Std.Data, Module_Std.Type),
											mTextParser.ToText,
											__ => aDebug(__())
										);
										aDebug(ResRes.Value.Log);
										
										mAssert.IsTrue(
											ResRes.Value.Result.Type.IsSubType(IL_Res.Type, mStd.cEmpty).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(IL_Res.Data.ToText(1000), ResRes.Value.Result.Data.ToText(1000));
									},
									ILT_File + ", " + mStd.File()
								),
							]
						)
					);
				}
				
				return Tests.ToArray();
			}
		)
	);
}