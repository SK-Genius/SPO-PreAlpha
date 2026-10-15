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
#:ref Common/mTreeMap.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
#:ref Common/mFS.cs
#:ref mVM_Data.cs
#:ref mVM.cs
#:ref mIL_AST.cs
#:ref mIL_Parser.cs
#:ref mIL_GenerateOpcodes.cs
#:ref mSPO2IL.cs
#:ref mSPO_AST.cs
#:ref mSPO_Parser.cs
#:ref mSPO_Interpreter.cs
#:ref mModule.cs

#:ref Common/mLazy.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mE2E_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mE2E_Tests),
		mStd.Call(
			() => {
				var Tests = mArrayList.List<mTest.tTest>();
				var TestRoot = mFS.CWD() / "TestFiles";
				const tText cScopedModulePattern = @"^\d{2}_\d{2}_";
				
				tText RelativeDiagnostic(tText aError) {
					var Prefix = mFS.CWD()._Path.ToText().TrimEnd('/', '\\') + "/";
					return aError.Replace(
						Prefix,
						""
					).Replace(
						Prefix.Replace('/', '\\'),
						""
					).Replace(
						new System.Uri(Prefix).AbsoluteUri,
						""
					);
				}
				
				mStream.tStream<mFS.tFile> ModuleFiles(mFS.tFolder aFolder) => aFolder.Exists()
					? aFolder.GetFiles(
					).Where(
						a => a.Path.Parent.Deref.AssertNotEmpty() == aFolder._Path && (
							a.Extension is "SPO" ||
							(
								a.Extension is "ILT" &&
								!a.Folder.GetFile(a.NameWithoutExtension + ".SPO").Exists()
							)
						)
					).Sort(
						(a, b) => tText.CompareOrdinal(a.Name, b.Name)
					)
					: mStd.cEmpty;
				
				mStream.tStream<tText> ImportNames(mFS.tFile aFile, mStd.tAction<tText> aDebug) {
					var Source = aFile.TryReadText().ElseThrow();
					if (aFile.Extension is "SPO") {
						var Pattern = mSPO_Parser.Module.ParseText(
							Source, aFile.Path.ToText(), a => aDebug(a())
						).Import.Pattern;
						
						while (Pattern is mSPO_AST.tTypedPatternNode<tSpan> Typed) {
							Pattern = Typed.Pattern;
						}
						
						return Pattern is mSPO_AST.tRecordPatternNode<tSpan> Record
						? Record.Elements.Map(a => a.Id.Id[1..])
						: mStd.cEmpty;
					}
					
					var Module = mIL_GenerateOpcodes.GenerateOpcodes(
						mIL_Parser.Module.ParseText(Source, aFile.Path.ToText(), a => aDebug(a())),
						a => aDebug(a())
					).Module.TryLast(
					).AssertNotEmpty();
					
					mAssert.IsTrue(Module.DefType.IsProc(out _, out _, out var Function));
					mAssert.IsTrue(Function.IsProc(out _, out var Argument, out _));
					
					return Argument.IsRecord(out var Fields)
					? Fields.ToStream().Map(a => a.Key[1..])
					: mStd.cEmpty;
				}
				
				var Cache = mTreeMap.Tree<tText, mModule.tModuleSetup>(
					(a, b) => tText.CompareOrdinal(a, b).Sign(),
					[]
				);
				
				mFS.tFile ModuleFile(
					tText aName,
					mTreeMap.tTree<tText, mFS.tFile> aModules,
					mFS.tFolder aModuleFolder
				) {
					if (aModules.TryGet(aName).IsSome(out var File)) {
						return File;
					}
					throw new System.IO.FileNotFoundException(
						"Module not available: " + aName, aModuleFolder.GetFile(aName + ".SPO").Path.ToText()
					);
				}
				
				mModule.tModuleSetup Setup(
					mFS.tFile aFile,
					mTreeMap.tTree<tText, mFS.tFile> aModules,
					mFS.tFolder aModuleFolder,
					mStream.tStream<tText> aAncestors,
					mStd.tAction<tText> aDebug
				) {
					if (!System.Text.RegularExpressions.Regex.IsMatch(aFile.NameWithoutExtension, cScopedModulePattern)) {
						aModules = mTreeMap.Tree<tText, mFS.tFile>(
							(a, b) => tText.CompareOrdinal(a, b).Sign(),
							aModules.ToStream().Where(a => !System.Text.RegularExpressions.Regex.IsMatch(a.Key, cScopedModulePattern))
							.Map(a => (a.Key, a.Value)).ToArrayList().ToArray()
						);
					}
					
					var Path = System.IO.Path.GetFullPath(aFile.Path.ToText());
					
					if (System.OperatingSystem.IsWindows()) {
						Path = Path.ToUpperInvariant();
					}
					
					mAssert.IsFalse(aAncestors.Any(a => a == Path), "Module dependency cycle: " + aFile.Path.ToText());
					
					if (Cache.TryGet(Path).IsSome(out var Cached)) {
						return Cached;
					}
					
					var Dependencies = ImportNames(aFile, aDebug).Map(
						aName => (
							aName,
							Setup(
								ModuleFile(aName, aModules, aModuleFolder),
								aModules,
								aModuleFolder,
								mStream.Stream(Path, aAncestors),
								aDebug
							)
						)
					).ToArrayList(
					).ToStream();
					
					var Module = mModule.ModuleSetup(aFile.Path, Dependencies);
					
					Cache = Cache.Set(Path, Module);
					
					return Module;
				}
				
				(mVM_Data.tData Data, mVM_Type.tType Type) Import(
					mFS.tFile aFile,
					mTreeMap.tTree<tText, mFS.tFile> aModules,
					mFS.tFolder aModuleFolder,
					mStd.tAction<tText> aDebug
				) {
					var Data = mVM_Data.Empty();
					var Type = mVM_Type.Empty();
					
					foreach (var Name in ImportNames(aFile, aDebug)) {
						(mVM_Data.tData Data, mVM_Type.tType Type) Value;
						if (aModules.TryGet(Name).IsSome(out var File)) {
							Value = Setup(File, aModules, aModuleFolder, mStd.cEmpty, aDebug).Init(aDebug).ElseThrow();
						} else {
							var Std = aModules.TryGet("Std").IsSome(out var StdFile)
								? Setup(StdFile, aModules, aModuleFolder, mStd.cEmpty, aDebug).Init(aDebug).ElseThrow()
								: (Data: mVM_Data.Empty(), Type: mVM_Type.Empty());
							
							if (Std.Type.IsRecord(out var Fields) && Fields.TryGet("_" + Name).IsSome(out var FieldType)) {
								mAssert.IsTrue(Std.Data.IsRecord(out var Values));
								Value = (Values.TryGet(mVM_Data.PrefixHash("_" + Name)).AssertNotEmpty(), FieldType);
							} else {
								Value = Setup(
									ModuleFile(Name, aModules, aModuleFolder),
									aModules,
									aModuleFolder,
									mStd.cEmpty,
									aDebug
								).Init(
									aDebug
								).ElseThrow();
							}
						}
						Data = mVM_Data.Record(Data, mVM_Data.Prefix("_" + Name, Value.Data));
						Type = mVM_Type.Record(Type, mVM_Type.Prefix("_" + Name, Value.Type));
					}
					return (Data, Type);
				}
				
				foreach (
					var Folder in TestRoot.GetFolders().Where(
						a => (
							a._Path.Parent.Deref.AssertNotEmpty() == TestRoot._Path &&
							!System.IO.Path.GetFileName(a._Path.ToText()).StartsWith("_")
						)
					).Sort(
						(a, b) => tText.CompareOrdinal(a._Path.ToText(), b._Path.ToText())
					)
				) {
					var FolderName = System.IO.Path.GetFileName(Folder._Path.ToText());
					var ModuleFolder = Folder / "_Modules";
					var GroupModules = ModuleFiles(ModuleFolder);
					
					foreach (
						var SourceFile in Folder.GetFiles(
						).Where(
							a => (
								a.Extension is "SPO" &&
								!a.Name.StartsWith("_") &&
								!a.Name.EndsWith(".result.SPO")
							) || (
								a.Extension is "ILT" &&
								!a.Folder.GetFile(a.NameWithoutExtension + ".SPO").Exists()
							)
						).Sort(
							(a, b) => tText.CompareOrdinal(a.Name, b.Name)
						)
					) {
						var Extension = SourceFile.Extension;
						var Case = FolderName + "/" + SourceFile.Name.Replace('\\', '/');
						var SourceId = "TestFiles/" + Case;
						var BaseName = SourceFile.NameWithoutExtension;
						var Modules = mTreeMap.Tree<tText, mFS.tFile>(
							(a, b) => tText.CompareOrdinal(a, b).Sign(),
							GroupModules.Where(
								a => (
									!System.Text.RegularExpressions.Regex.IsMatch(
										a.NameWithoutExtension,
										cScopedModulePattern
									) ||
									BaseName.Length >= 6 &&
									a.NameWithoutExtension.StartsWith(
										BaseName[..6], System.StringComparison.Ordinal
									)
								)
							).Map(
								a => (a.NameWithoutExtension, a)
							).ToArrayList(
							).ToArray()
						);
						
						if (Folder.GetFile(BaseName + ".error.txt").Exists()) {
							Tests.Push(
								mTest.Test(
									SourceId + " == .error.txt",
									aDebug => {
										var ExpectedFile = Folder.GetFile(BaseName + ".error.txt");
										tText? Actual = null;
										
										try {
											var Source = SourceFile.TryReadText().ElseThrow();
											var Imports = Import(SourceFile, Modules, ModuleFolder, aDebug);
											if (Extension is "SPO") {
												if (
													!mSPO_Interpreter.Run(
														Source,
														SourceId,
														Imports,
														a => aDebug(a())
													).Match(
														out _,
														out var Error
													)
												) {
													Actual = Error;
												}
											} else {
												var Module = mIL_Parser.Module.ParseText(Source, SourceId, a => aDebug(a()));
												mIL_GenerateOpcodes.GenerateOpcodes(Module, a => aDebug(a()));
											}
										} catch (System.Exception Error) {
											Folder.GetFile(BaseName + ".error.new.txt").TryCreate(Error.ToString());
											throw;
										}
										
										Actual = Actual is null ? null : RelativeDiagnostic(Actual);
										
										var Expected = ExpectedFile.Exists()
										? RelativeDiagnostic(ExpectedFile.TryReadText().ElseThrow())
										: null;
										
										if (Actual != Expected || Actual is null) {
											Folder.GetFile(
												BaseName + ".error.new.txt"
											).TryCreate(
												Actual ?? "Compilation succeeded; expected an error."
											);
											
											mAssert.IsTrue(
												Actual is not null,
												"Expected a compiler diagnostic, compilation succeeded"
											);
											mAssert.IsTrue(
												Expected is not null,
												"Missing expected diagnostic: " + ExpectedFile.Path.ToText()
											);
											mAssert.AreEquals(Actual, Expected);
										}
									},
									SourceFile.Path.ToText(),
									1
								)
							);
							
							continue;
						}
						
						var ResultFile = Folder.GetFile(BaseName + ".result.SPO");
						var ILT = Extension is "ILT" ? SourceFile : Folder.GetFile(BaseName + ".ILT");
						var Expected = mLazy.Lazy(
							() => mSPO_Interpreter.Run(
								ResultFile.TryReadText().ElseThrow(),
								ResultFile.Path.ToText(),
								(mVM_Data.Empty(), mVM_Type.Empty()),
								_ => {}
							).ElseThrow()
						);
						var Cases = mArrayList.List<mTest.tTest>();
						
						void
						GenerateILT(
							mStd.tAction<tText> aDebug
						) {
							var NewILT = mSPO_Parser.Module.ParseText(
								SourceFile.TryReadText().ElseThrow(),
								SourceFile.Path.ToText(),
								a => aDebug(a())
							).ToILT();
							
							if (
								!ILT.Exists() ||
								NewILT.Replace("\r", "") != ILT.TryReadText().ElseThrow().Replace("\r", "")
							) {
								ILT.TryCreate(NewILT);
							}
						}
						
						void
						AddResultTest(
							tText aExtension,
							mFS.tFile aFile
						) {
							Cases.Push(
								mTest.Test(
									"." + aExtension + " == .result.SPO",
									aDebug => {
										var Imports = Import(SourceFile, Modules, ModuleFolder, aDebug);
										
										if (aExtension is "ILT" && Extension is "SPO") {
											GenerateILT(aDebug);
										}
										
										var Source = aFile.TryReadText().ElseThrow();
										
										var Actual = aExtension is "SPO"
										? mSPO_Interpreter.Run(
											Source,
											aFile.Path.ToText(),
											Imports,
											a => aDebug(a())
										).ElseThrow()
										: mVM.Run(
											mIL_Parser.Module.ParseText(
												Source,
												aFile.Path.ToText(),
												a => aDebug(a())
											),
											Imports,
											mTextParser.ToText,
											a => aDebug(a())
										);
										
										mAssert.IsTrue(
											Expected.Value.Type.IsSubType(Actual.Type).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(
											Actual.Data.ToText(1000),
											Expected.Value.Data.ToText(1000)
										);
									},
									aFile.Path.ToText(),
									1
								)
							);
						}
						
						if (Extension is "SPO") {
							AddResultTest("SPO", SourceFile);
							Cases.Push(mTest.Test(".SPO -> .ILT", GenerateILT, SourceFile.Path.ToText(), 1));
						}
						
						AddResultTest("ILT", ILT);
						Tests.Push(mTest.Tests(FolderName + "/" + BaseName, Cases.ToArray()));
					}
				}
				return Tests.ToArray();
			}
		)
	);
}
