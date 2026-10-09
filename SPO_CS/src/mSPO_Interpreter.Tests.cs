#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref mVM_Data.cs
#:ref mVM_Type.cs
#:ref mSPO_Interpreter.cs

public static class
mSPO_Interpreter_Tests {
	private static tText
	RunError(
		tText aSource,
		tText aId = "diagnostic.SPO"
	) {
		var Result = mSPO_Interpreter.Run(
			aSource,
			aId,
			(mVM_Data.Empty(), mVM_Type.Empty()),
			_ => { }
		);
		mAssert.IsFalse(Result.Match(out _, out var Error), "Expected a type error.");
		return Error!;
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_Interpreter),
		[
			mTest.Test("Large record errors identify the nested value without dumping the record",
				aDebug => {
					const tInt32 cFieldCount = 48;
					var Fields = mStream.Int32StartWith(0).Take(cFieldCount);
					var Signature = Fields.Map(__ => $"Extra{__}: §INT")
					.Join((aLeft, aRight) => aLeft + ", " + aRight, "");
					var Values = Fields.Map(__ => $"\tExtra{__}: 0")
					.Join((aLeft, aRight) => aLeft + "\n" + aRight, "");
					var Source = (
						"§DEF Read... = §DEF aRecord € [< Value: [< Count: §INT >], " + Signature + " >] => 0\n" +
						"§EXPORT .Read {\n\tValue: {\n\t\tCount: §FALSE\n\t}\n" + Values + "\n}\n"
					);
					var Error = RunError(Source);
					aDebug(Error);
					mAssert.IsTrue(Error.StartsWith("diagnostic.SPO:4,10..4,15\n"), Error);
					mAssert.IsTrue(Error.Contains("Count: §FALSE"), Error);
					mAssert.IsTrue(Error.Contains("Field '_Value': Field '_Count': Expected §INT, got §FALSE."), Error);
					mAssert.IsTrue(Error.Length < 300, Error);
					mAssert.IsTrue(Error.IndexOf("Count: §FALSE") < Error.IndexOf("Expected §INT"), Error);
				}
			),
			mTest.Test("Multiple wrong fields keep the message and source span together",
				aDebug => {
					const tText cLine = "§EXPORT .Read { B: §FALSE, A: 123 }";
					var Error = RunError(
						"§DEF Read... = §DEF aRecord € [< A: §BOOL, B: §INT >] => 0\n" + cLine + "\n"
					);
					aDebug(Error);
					var Column = cLine.IndexOf("§FALSE") + 1;
					mAssert.IsTrue(Error.StartsWith($"diagnostic.SPO:2,{Column}..2,{Column + 5}\n"), Error);
					mAssert.IsTrue(Error.Contains("Field '_B': Expected §INT, got §FALSE."), Error);
				}
			),
			mTest.Test("Binding annotations point at the incompatible record field",
				aDebug => {
					var Error = RunError("§DEF Value € [< X: §INT >] = {\n\tX: §FALSE\n}\n§EXPORT Value\n");
					aDebug(Error);
					mAssert.IsTrue(Error.StartsWith("diagnostic.SPO:2,5..2,10\n"), Error);
					mAssert.IsTrue(Error.Contains("Expected §INT, got §FALSE."), Error);
				}
			),
			mTest.Test("Nested tuple arguments point at the incompatible item",
				aDebug => {
					const tText cLine = "§EXPORT .Read ((1, §FALSE), 3)";
					var Error = RunError(
						"§DEF Read... = §DEF aTuple € [[§INT, §INT], §INT] => 0\n" + cLine + "\n"
					);
					aDebug(Error);
					var Column = cLine.IndexOf("§FALSE") + 1;
					mAssert.IsTrue(Error.StartsWith($"diagnostic.SPO:2,{Column}..2,{Column + 5}\n"), Error);
					mAssert.IsTrue(Error.Contains("Expected §INT, got §FALSE."), Error);
				}
			),
			mTest.Test("Return guards point at the condition",
				aDebug => {
					const tText cLine = "\t§RETURN aValue IF 123";
					var Error = RunError(
						"§DEF Choose... = §DEF aValue € §INT => {\n" + cLine +
						"\n\t§RETURN 0\n}\n§EXPORT .Choose 1\n"
					);
					aDebug(Error);
					var Column = cLine.IndexOf("123") + 1;
					mAssert.IsTrue(Error.StartsWith($"diagnostic.SPO:2,{Column}..2,{Column + 2}\n"), Error);
					mAssert.IsTrue(Error.Contains("123\n"), Error);
					mAssert.IsTrue(Error.Contains("~~~\n"), Error);
				}
			),
			mTest.Test("Multiline errors limit source context and retain the missing field",
				aDebug => {
					var Fields = mStream.Int32StartWith(0).Take(20).Map(__ => $"\tExtra{__}: 0")
					.Join((aLeft, aRight) => aLeft + "\n" + aRight, "");
					var Error = RunError(
						"§DEF Read... = §DEF aRecord € [< Required: §INT >] => 0\n§EXPORT .Read {\n" +
						Fields + "\n}\n"
					);
					aDebug(Error);
					mAssert.IsTrue(Error.StartsWith("diagnostic.SPO:2,"), Error);
					mAssert.IsTrue(Error.Contains("Missing field '_Required'"), Error);
					mAssert.IsTrue(Error.Contains("\n  ...\n"), Error);
					mAssert.IsFalse(Error.Contains("Extra19"), Error);
					mAssert.IsTrue(Error.Split('\n').Length <= 10, Error);
				}
			),
			mTest.Test("Imported signature errors show the imported source",
				aDebug => {
					var Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid() + ".SIG");
					try {
						System.IO.File.WriteAllText(Path, "[< Value: MissingType >]");
						var SourcePath = Path.Replace('\\', '/');
						var Error = RunError(
							$"§DEF Value € [§LOAD \"{SourcePath}\"] = 1\n§EXPORT Value\n"
						);
						aDebug(Error);
						mAssert.IsTrue(Error.StartsWith(new System.Uri(Path).AbsoluteUri + ":"), Error);
						mAssert.IsTrue(Error.Contains("[< Value: MissingType >]"), Error);
						mAssert.IsTrue(Error.Contains("~"), Error);
					} finally {
						System.IO.File.Delete(Path);
					}
				}
			),
		]
	);
}