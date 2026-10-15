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
				
				static tText
				RelativeDiagnostic(
					tText aError
				) {
					var Prefix = mFS.CWD()._Path.ToText().TrimEnd('/', '\\') + "/";
					
					return aError.Replace(
						new System.Uri(Prefix).AbsoluteUri,
						""
					).Replace(
						Prefix,
						""
					).Replace(
						Prefix.Replace('/', '\\'),
						""
					).TrimEnd(
						'\r',
						'\n'
					);
				}
				
				mStream.tStream<mFS.tFile> ModuleFiles(mFS.tFolder aFolder) => aFolder.Exists()
					? aFolder.GetFiles(
					).Where(
						__ => __.Path.Parent.Deref.AssertNotEmpty() == aFolder._Path && (
							__.Extension is "SPO" ||
							(
								__.Extension is "ILT" &&
								!__.Folder.GetFile(__.NameWithoutExtension + ".SPO").Exists()
							)
						)
					).Sort(
						(a1, a2) => tText.CompareOrdinal(a1.Name, a2.Name)
					)
					: mStd.cEmpty;
				
				mResult.tResult<mStream.tStream<tText>, tText> ImportNames(
					mFS.tFile aFile,
					mStd.tAction<tText> aDebug
				) {
					if (aFile.TryReadText().IsFail(out var ReadError, out var Source)) {
						return ReadError;
					}
					
					if (aFile.Extension is "SPO") {
						return mSPO_Parser.Module.TryParseText(
							Source,
							aFile.Path.ToText(),
							__ => aDebug(__())
						).Then(
							aModule => {
								var Pattern = aModule.Import.Pattern;
								
								while (Pattern is mSPO_AST.tTypedPatternNode<mSpan.tSpan<mTextStream.tPos>> Typed) {
									Pattern = Typed.Pattern;
								}
								
								return Pattern is mSPO_AST.tRecordPatternNode<mSpan.tSpan<mTextStream.tPos>> Record
								? Record.Elements.Map(__ => __.Id.Id[1..])
								: mStd.cEmpty;
							}
						);
					}
					
					return mIL_Parser.Module.TryParseText(
						Source,
						aFile.Path.ToText(),
						__ => aDebug(__())
					).ThenTry(
						aModule => mIL_GenerateOpcodes.GenerateOpcodes(aModule, __ => aDebug(__()))
					).Then(
						aModule => {
							mAssert.IsTrue(
								aModule.Module.TryLast(
								).AssertNotEmpty(
								).DefType.IsProc(
									out _,
									out _,
									out var Function
								)
							);
							
							mAssert.IsTrue(
								Function.IsProc(
									out _,
									out var Argument,
									out _
								)
							);
							
							return (
								Argument.IsRecord(
									out var Fields
								)
								? Fields.ToStream(
								).Where(
									__ => __.Key.StartsWith("_")
								).Map(
									__ => __.Key[1..]
								)
								: mStd.cEmpty
							);
						}
					);
				}
				
				var Cache = mTreeMap.Tree<tText, mModule.tModuleSetup>(
					(a1, a2) => tText.CompareOrdinal(a1, a2).Sign(),
					[]
				);
				
				mResult.tResult<mModule.tModuleSetup, tText>
				Setup(
					mFS.tFile aFile,
					mTreeMap.tTree<tText, mFS.tFile> aModules,
					mStream.tStream<tText> aAncestors,
					mStd.tAction<tText> aDebug
				) {
					if (!System.Text.RegularExpressions.Regex.IsMatch(aFile.NameWithoutExtension, cScopedModulePattern)) {
						aModules = mTreeMap.Tree<tText, mFS.tFile>(
							(a1, a2) => tText.CompareOrdinal(a1, a2).Sign(),
							aModules.ToStream(
							).Where(
								__ => !System.Text.RegularExpressions.Regex.IsMatch(__.Key, cScopedModulePattern)
							).Map(
								__ => (__.Key, __.Value)
							).ToArrayList(
							).ToArray()
						);
					}
					
					var Path = System.IO.Path.GetFullPath(aFile.Path.ToText());
					
					if (System.OperatingSystem.IsWindows()) {
						Path = Path.ToUpperInvariant();
					}
					
					if (aAncestors.Any(__ => __ == Path)) {
						return mResult.Fail("Module dependency cycle: " + aFile.Path.ToText());
					}
					
					if (Cache.TryGet(Path).IsSome(out var Cached)) {
						return Cached;
					}
					
					if (ImportNames(aFile, aDebug).IsFail(out var ImportError, out var Names)) {
						return ImportError;
					}
					
					var Dependencies = mArrayList.List<(tText Key, mModule.tModuleSetup Module)>();
					foreach (var Name in Names) {
						if (!aModules.TryGet(Name).IsSome(out var File)) {
							return mResult.Fail("Module not available: " + Name);
						}
						if (Setup(File, aModules, mStream.Stream(Path, aAncestors), aDebug).IsFail(out var Error, out var Module)) {
							return Error;
						}
						Dependencies.Push((Name, Module));
					}
					
					var Result = mModule.ModuleSetup(aFile.Path, Dependencies.ToStream());
					Cache = Cache.Set(Path, Result);
					return Result;
				}
				
				mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
				Import(
					mFS.tFile aFile,
					mTreeMap.tTree<tText, mFS.tFile> aModules,
					mStd.tAction<tText> aDebug
				) {
					if (ImportNames(aFile, aDebug).IsFail(out var ImportError, out var Names)) {
						return ImportError;
					}
					
					var Data = mVM_Data.Empty();
					var Type = mVM_Type.Empty();
					
					foreach (var Name in Names) {
						(mVM_Data.tData Data, mVM_Type.tType Type) Value;
						if (aModules.TryGet(Name).IsSome(out var File)) {
							if (
								Setup(
									File,
									aModules,
									mStd.cEmpty,
									aDebug
								).ThenTry(
									__ => __.Init(aDebug)
								).IsFail(
									out var Error,
									out Value
								)
							) {
								return Error;
							}
						} else {
							var Std = (Data: mVM_Data.Empty(), Type: mVM_Type.Empty());
							
							if (aModules.TryGet("Std").IsSome(out var StdFile)) {
								if (
									Setup(
										StdFile,
										aModules,
										mStd.cEmpty,
										aDebug
									).ThenTry(
										__ => __.Init(aDebug)
									).IsFail(
										out var Error,
										out Std
									)
								) {
									return Error;
								}
							}
							
							if (
								!Std.Type.IsRecord(out var Fields) ||
								!Fields.TryGet("_" + Name).IsSome(out var FieldType)
							) {
								return mResult.Fail("Module not available: " + Name);
							}
							
							mAssert.IsTrue(Std.Data.IsRecord(out var Values));
							Value = (
								Values.TryGet(mVM_Data.PrefixHash("_" + Name)).AssertNotEmpty(),
								FieldType
							);
						}
						
						Data = mVM_Data.Record(Data, mVM_Data.Prefix("_" + Name, Value.Data));
						Type = mVM_Type.Record(Type, mVM_Type.Prefix("_" + Name, Value.Type));
					}
					
					return (Data, Type);
				}
				
				foreach (
					var Folder in TestRoot.GetFolders(
					).Where(
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
							a => a.Path.Parent.Deref.AssertNotEmpty() == Folder._Path && (
								(
									a.Extension is "SPO" &&
									!a.Name.StartsWith("_") &&
									!a.Name.EndsWith(".result.SPO")
								) || (
									a.Extension is "ILT" &&
									!a.Folder.GetFile(a.NameWithoutExtension + ".SPO").Exists()
								)
							)
						).Sort(
							(a1, a2) => tText.CompareOrdinal(a1.Name, a2.Name)
						)
					) {
						var Extension = SourceFile.Extension;
						var Case = FolderName + "/" + SourceFile.Name.Replace('\\', '/');
						var SourceId = "TestFiles/" + Case;
						var BaseName = SourceFile.NameWithoutExtension;
						var Modules = mTreeMap.Tree<tText, mFS.tFile>(
							(a1, a2) => tText.CompareOrdinal(a1, a2).Sign(),
							GroupModules.Where(
								a => (
									!System.Text.RegularExpressions.Regex.IsMatch(
										a.NameWithoutExtension,
										cScopedModulePattern
									) ||
									BaseName.Length >= 6 &&
									a.NameWithoutExtension.StartsWith(
										BaseName[..6],
										System.StringComparison.Ordinal
									)
								)
							).Map(
								__ => (__.NameWithoutExtension, __)
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
											var Result = Import(
												SourceFile,
												Modules,
												aDebug
											).ThenTry(
												aImports => Extension is "SPO"
												? mSPO_Interpreter.Run(
													Source,
													SourceId,
													aImports,
													__ => aDebug(__())
												).Then(
													_ => mStd.cEmpty
												)
												: mIL_Parser.Module.TryParseText(
													Source,
													SourceId,
													__ => aDebug(__())
												).ThenTry(
													aModule => mIL_GenerateOpcodes.GenerateOpcodes(
														aModule,
														__ => aDebug(__())
													)
												).Then(
													_ => mStd.cEmpty
												)
											);
											
											if (!Result.Match(out _, out var Error)) {
												Actual = Error;
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
											mAssert.AreEquals(
												Actual,
												Expected
											);
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
							var NewILT = mSPO_Parser.Module.TryParseText(
								SourceFile.TryReadText().ElseThrow(),
								SourceFile.Path.ToText(),
								a => aDebug(a())
							).ThenTry(
								a => a.TryToILT()
							).ElseThrow();
							
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
										var Imports = Import(SourceFile, Modules, aDebug).ElseThrow();
										
										if (aExtension is "ILT" && Extension is "SPO") {
											GenerateILT(aDebug);
										}
										
										var Source = aFile.TryReadText().ElseThrow();
										
										var Actual = aExtension is "SPO"
										? mSPO_Interpreter.Run(
											Source,
											aFile.Path.ToText(),
											Imports,
											__ => aDebug(__())
										).ElseThrow()
										: mVM.TryRun(
											mIL_Parser.Module.TryParseText(
												Source,
												aFile.Path.ToText(),
												__ => aDebug(__())
											).ElseThrow(),
											Imports,
											mTextParser.ToText,
											__ => aDebug(__())
										).ElseThrow();
										
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
