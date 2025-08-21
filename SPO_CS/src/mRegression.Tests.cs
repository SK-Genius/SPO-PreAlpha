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
			"Tests"
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
				foreach (var SpoPath in System.IO.Directory.GetFiles(cTestFolder.FullName, "*.spo")) {
					var BaseName = System.IO.Path.GetFileNameWithoutExtension(SpoPath);
					var ResPath = System.IO.Path.Combine(cTestFolder.FullName, BaseName + ".result.spo");
					var IlPath = System.IO.Path.Combine(cTestFolder.FullName, BaseName + ".ilt");
					if (
						BaseName.StartsWith("_") ||
						SpoPath.EndsWith(".result.spo") ||
						!System.IO.File.Exists(ResPath) ||
						!System.IO.File.Exists(IlPath)
					) {
						continue;
					}
					
					var SpoText = mLazy.Lazy(() => System.IO.File.ReadAllText(SpoPath));
					var ResText = mLazy.Lazy(() => System.IO.File.ReadAllText(ResPath));
					var IlText = mLazy.Lazy(() => System.IO.File.ReadAllText(IlPath));
					
					var ResRes = mLazy.Lazy(
						() => {
							var Log = "";
							var WriteToLog = (mStd.tFunc<tText> aGetLine) => {
								Log +="\n" + aGetLine();
							};
							var Result = mSPO_Interpreter.Run(
								ResText.Value,
								SpoPath,
								(mVM_Data.Empty(), mVM_Type.Empty()),
								_ => WriteToLog(_)
							);
							return (Result, Log);
						}
					);
					
					Tests.Push(
						mTest.Tests(
							BaseName,
							[
								mTest.Test(
									".spo == .result.spo",
									aDebug => {
										var SpoRes = mSPO_Interpreter.Run(
											SpoText.Value,
											SpoPath,
											mStdLib.GetImportData(_ => aDebug(_())),
											_ => aDebug(_())
										);
										aDebug(ResRes.Value.Log);
										mAssert.AreEquals(SpoRes.Type.ToText(), ResRes.Value.Result.Type.ToText());
										mAssert.AreEquals(SpoRes.Data.ToText(1000), ResRes.Value.Result.Data.ToText(1000));
									},
									SpoPath,
									1
								),
								mTest.Test(
									".spo -> .ilt",
									aDebug => {
										mAssert.AreEquals(
											mSPO_Parser.Module.ParseText(
												SpoText.Value,
												SpoPath,
												_ => aDebug(_())
											).ToText(),
											IlText.Value
										);
									},
									IlPath,
									1
								),
								mTest.Test(
									".ilt == .result.spo",
									aDebug => {
										var IlModule = mIL_Parser.Module.ParseText(
											IlText.Value,
											IlPath,
											_ => aDebug(_())
										);
										var IlRes = mVM.Run(
											IlModule,
											mStdLib.GetImportData(_ => aDebug(_())),
											p => $"{p.Start.Id}({p.Start.Row}:{p.Start.Col} .. {p.End.Row}:{p.End.Col})",
											_ => aDebug(_())
										);
										aDebug(ResRes.Value.Log);
										mAssert.AreEquals(IlRes.Type.ToText(), ResRes.Value.Result.Type.ToText());
										mAssert.AreEquals(IlRes.Data.ToText(1000), ResRes.Value.Result.Data.ToText(1000));
									},
									IlPath,
									1
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