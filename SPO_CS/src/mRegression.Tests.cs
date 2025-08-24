// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT Common/mStream
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mArrayList
// IMPORT Common/mSpan
// IMPORT Common/mTextStream
// IMPORT mVM_Data
// IMPORT mVM
// IMPORT mIL_AST
// IMPORT mIL_Parser
// IMPORT mSPO2IL
// IMPORT mSPO_AST
// IMPORT mSPO_AST_Types
// IMPORT mSPO_Parser
// IMPORT mSPO_Interpreter

public static class
mRegression_Tests {
	private static readonly System.IO.DirectoryInfo
	cTestFolder = new System.IO.DirectoryInfo(
		System.IO.Path.Combine(
			System.IO.Directory.GetParent(mStd.File()).FullName,
			"..",
			"Regression.Tests"
		)
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mRegression_Tests),
		mStd.Call(
			() => {
				if (!cTestFolder.Exists) {
					System.Console.WriteLine("Folder not found: " + cTestFolder);
					return [];
				}
				
				var Tests = mArrayList.List<mTest.tTest>();
				foreach (var SPO_Path in System.IO.Directory.GetFiles(cTestFolder.FullName, "*.SPO")) {
					var BaseName = System.IO.Path.GetFileNameWithoutExtension(SPO_Path);
					var ResPath = System.IO.Path.Combine(cTestFolder.FullName, BaseName + ".result.SPO");
					var IL_Path = System.IO.Path.Combine(cTestFolder.FullName, BaseName + ".ILT");
					if (
						BaseName.StartsWith("_") ||
						SPO_Path.EndsWith(".result.SPO") ||
						!System.IO.File.Exists(ResPath)
					) {
						continue;
					}
					
					var SPO_Text = mLazy.Lazy(() => System.IO.File.ReadAllText(SPO_Path));
					var SPO_ResText = mLazy.Lazy(() => System.IO.File.ReadAllText(ResPath));
					var IL_Text = System.IO.File.Exists(IL_Path)
						? mLazy.Lazy(() => System.IO.File.ReadAllText(IL_Path))
						: "";
					
					var ResRes = mLazy.Lazy(
						() => {
							var Log = "";
							var WriteToLog = (mStd.tFunc<tText> aGetLine) => {
								Log +="\n" + aGetLine();
							};
							var Result = mSPO_Interpreter.Run(
								SPO_ResText.Value,
								SPO_Path,
								(mVM_Data.Empty(), mVM_Type.Empty()),
								_ => WriteToLog(_)
							).ElseThrow();
							return (Result, Log);
						}
					);
					
					Tests.Push(
						mTest.Tests(
							BaseName,
							[
								mTest.Test(
									".SPO == .result.SPO",
									aDebug => {
										var SPO_Res = mSPO_Interpreter.Run(
											SPO_Text.Value,
											SPO_Path,
											mStdLib.GetImportData(_ => aDebug(_())),
											_ => aDebug(_())
										).ElseThrow();
										aDebug(ResRes.Value.Log);
										mAssert.IsTrue(
											ResRes.Value.Result.Type.IsSubType(SPO_Res.Type, mStd.cEmpty).Match(out _, out var Error),
											Error
										);
										mAssert.AreEquals(SPO_Res.Data.ToText(1000), ResRes.Value.Result.Data.ToText(1000));
									},
									SPO_Path + ", " + mStd.File()
								),
								mTest.Test(
									".SPO -> .ILT",
									aDebug => {
										var IL_TextNew = mSPO_Parser.Module.ParseText(
												SPO_Text.Value,
												SPO_Path,
												_ => aDebug(_())
											).ToText();
										
										if (IL_TextNew != IL_Text.Value) {
											System.IO.File.WriteAllText(
												IL_Path + ".new",
												IL_TextNew
											);
										}
										mAssert.AreEquals(
											IL_TextNew,
											IL_Text.Value
										);
									},
									SPO_Path + ", " + IL_Path + ", " + mStd.File()
								),
								mTest.Test(
									".ILT == .result.SPO",
									aDebug => {
										var IlModule = mIL_Parser.Module.ParseText(
											IL_Text.Value,
											IL_Path,
											_ => aDebug(_())
										);
										var IL_Res = mVM.Run(
											IlModule,
											mStdLib.GetImportData(_ => aDebug(_())),
											_ => $"{_.Start.Id}:{_.Start.Row}|{_.Start.Col}..{_.End.Row}|{_.End.Col}",
											_ => aDebug(_())
										);
										aDebug(ResRes.Value.Log);
										
										mAssert.IsTrue(
											ResRes.Value.Result.Type.IsSubType(IL_Res.Type, mStd.cEmpty).Match(out _, out var Error),
											Error
										);
										
										mAssert.AreEquals(IL_Res.Data.ToText(1000), ResRes.Value.Result.Data.ToText(1000));
									},
									IL_Path + ", " + mStd.File()
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