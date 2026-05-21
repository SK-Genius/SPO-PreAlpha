#:include Common/mStd.cs
#:include Common/mTest.cs
#:include Common/mAssert.cs
#:include Common/mStream.cs
#:include Common/mMaybe.cs
#:include Common/mResult.cs
#:include Common/mArrayList.cs
#:include Common/mSpan.cs
#:include Common/mTextStream.cs
#:include Common/mFS.cs
#:include mVM_Data.cs
#:include mVM.cs
#:include mIL_AST.cs
#:include mIL_Parser.cs
#:include mSPO2IL.cs
#:include mSPO_AST.cs
#:include mSPO_AST_Types.cs
#:include mSPO_Parser.cs
#:include mSPO_Interpreter.cs
#:include mModule.cs

public static class
mRegression_Tests {
	private static readonly mFS.tFolder
	cTestFolder = mFS.CWD() / "Regression.Tests";
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mRegression_Tests),
		mStd.Call(
			() => {
				if (!cTestFolder.Exists()) {
					System.Console.WriteLine("Folder not found: " + cTestFolder);
					return [];
				}
				
				var Tests = mArrayList.List<mTest.tTest>();
				foreach (var SPO_File in cTestFolder.GetFiles().Where(__ => __.Name.EndsWith(".SPO"))) {
					var ResFile = cTestFolder.GetFile(SPO_File.Name.Replace(".SPO", ".result.SPO"));
					var ILT_File = cTestFolder.GetFile(SPO_File.Name.Replace(".SPO", ".ILT"));
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
								(cTestFolder._Path / SPO_File.Name).ToText(),
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
											(cTestFolder._Path / SPO_File.Name).ToText(),
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
												(cTestFolder._Path / SPO_File.Name).ToText(),
												__ => aDebug(__())
											).ToILT();
										
										if (IL_TextNew.Replace("\r", "") != IL_Text.Value.Replace("\r", "")) {
											cTestFolder.GetFile(
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
											(cTestFolder._Path / ILT_File.Name).ToText(),
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