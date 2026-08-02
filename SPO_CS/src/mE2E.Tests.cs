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
							var Log = new System.Text.StringBuilder();
							
							var Result = mSPO_Interpreter.Run(
								SPO_ResText.Value,
								(cTestFilesFolder._Path / SPO_File.Name).ToText(),
								(mVM_Data.Empty(), mVM_Type.Empty()),
								__ => Log.Append('\n').Append(__())
							).ElseThrow();
							
							return (Result, Log: Log.ToString());
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
											ResRes.Value.Result.Type.IsSubType(SPO_Res.Type).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(
											SPO_Res.Data.ToText(1000),
											ResRes.Value.Result.Data.ToText(1000)
										);
									},
									SPO_File.Name + ", " + mStd.File()
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
											ILT_File.TryCreate(IL_TextNew);
										}
									},
									SPO_File.Name + ", " + ILT_File + ", " + mStd.File()
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
											ResRes.Value.Result.Type.IsSubType(
												IL_Res.Type
											).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(
											IL_Res.Data.ToText(1000),
											ResRes.Value.Result.Data.ToText(1000)
										);
									},
									ILT_File.Name + ", " + mStd.File()
								),
							]
						)
					);
				}
				
				var ModuleTestFolder = mFS.CWD() / "TestFiles" / "Modules";
				
				foreach (
					var ConsumerSPO in ModuleTestFolder.GetFiles().Where(
						__ => (
							__.Name.EndsWith(".SPO") &&
							!__.Name.EndsWith(".result.SPO") &&
							!__.Name.StartsWith("_")
						)
					)
				) {
					var Name = ConsumerSPO.Name.Replace(".SPO", "");
					var ConsumerILT = ModuleTestFolder.GetFile(Name + ".ILT");
					var ExpectedSPO = ModuleTestFolder.GetFile(Name + ".result.SPO");
					
					var ConsumerSource = mLazy.Lazy(() => ConsumerSPO.TryReadText().ElseThrow());
					var ConsumerIL = ConsumerILT.Exists() ? mLazy.Lazy(() => ConsumerILT.TryReadText().ElseThrow()) : "";
					var ExpectedSource = mLazy.Lazy(() => ExpectedSPO.TryReadText().ElseThrow());
					
					(mVM_Data.tData Data, mVM_Type.tType Type)
					Expected(
						mStd.tAction<mStd.tFunc<tText>> aDebug
					) => mSPO_Interpreter.Run(
						ExpectedSource.Value,
						(ModuleTestFolder._Path / ExpectedSPO.Name).ToText(),
						(mVM_Data.Empty(), mVM_Type.Empty()),
						aDebug
					).ElseThrow();
					
					Tests.Push(
						mTest.Tests(
							"Modules/" + Name + ".SPO",
							[
								mTest.Test(
									".SPO == .result.SPO",
									aDebug => {
										var Modules = mModule.Modules.Init(aDebug).ElseThrow();
										
										var Actual = mSPO_Interpreter.Run(
											ConsumerSource.Value,
											(ModuleTestFolder._Path / ConsumerSPO.Name).ToText(),
											(Modules.Data, Modules.Type),
											__ => aDebug(__())
										).ElseThrow();
										
										var Expected_ = Expected(__ => aDebug(__()));
										
										mAssert.IsTrue(
											Expected_.Type.IsSubType(Actual.Type).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(
											Actual.Data.ToText(1000),
											Expected_.Data.ToText(1000)
										);
									},
									ConsumerSPO.Name + ", " + mStd.File()
								),
								mTest.Test(
									".SPO -> .ILT",
									aDebug => {
										var NewILT = mSPO_Parser.Module.ParseText(
											ConsumerSource.Value,
											(ModuleTestFolder._Path / ConsumerSPO.Name).ToText(),
											__ => aDebug(__())
										).ToILT();
										
										if (NewILT.Replace("\r", "") != ConsumerIL.Value.Replace("\r", "")) {
											ConsumerILT.TryCreate(NewILT);
											mAssert.Fail(mAssert.DiffText(NewILT, ConsumerIL.Value));
										}
									},
									ConsumerSPO.Name + ", " + mStd.File()
								),
								mTest.Test(
									".ILT == .result.SPO",
									aDebug => {
										var Modules = mModule.Modules.Init(aDebug).ElseThrow();
										
										var Actual = mVM.Run(
											mIL_Parser.Module.ParseText(
												ConsumerIL.Value,
												(ModuleTestFolder._Path / ConsumerILT.Name).ToText(),
												__ => aDebug(__())
											),
											(Modules.Data, Modules.Type),
											mTextParser.ToText,
											__ => aDebug(__())
										);
										
										var Expected_ = Expected(__ => aDebug(__()));
										
										mAssert.IsTrue(
											Expected_.Type.IsSubType(Actual.Type).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(
											Actual.Data.ToText(1000),
											Expected_.Data.ToText(1000)
										);
									},
									ConsumerILT.Name + ", " + mStd.File()
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
