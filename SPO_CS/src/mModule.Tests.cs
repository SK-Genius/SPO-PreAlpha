#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref mVM_Data.cs
#:ref mSPO_Interpreter.cs
#:ref mModule.cs

public static class
mModule_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mModule_Tests),
		[
			mTest.Test("Std",
				aDebugStream => {
					var Module_Std = mModule.Module_Std.Init(
						aDebugStream
					).ElseThrow(
					);
					
					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							"""
							§IMPORT {
								...+...: §DEF ...+... € [[§INT, §INT] => §INT]
								...-...: §DEF ...-... € [[§INT, §INT] => §INT]
								...*...: §DEF ...*... € [[§INT, §INT] => §INT]
								.../...: §DEF .../... € [[§INT, §INT] => §INT]
							}
							
							§EXPORT (
								2 .+ 5
								7 .- 3
								3 .* 4
								8 ./ 3
							)
							""",
							"",
							(Module_Std.Data, Module_Std.Type),
							__ => aDebugStream(__())
						).Then(
							__ => __.Data
						).ElseThrow(
						),
						mVM_Data.Tuple(
							[
								mVM_Data.Int(7),
								mVM_Data.Int(4),
								mVM_Data.Int(12),
								mVM_Data.Int(2),
							]
						)
					);
				}
			),
			mTest.Test("Char",
				aDebugStream => {
					var Module_Char = mModule.Module_Char.Init(
						aDebugStream
					).ElseThrow(
					);
					
					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							"""
							§IMPORT {
								...==...: §DEF ...==... € [[§CHAR, §CHAR] => §BOOL]
								...ToUpper: §DEF ...ToUpper € [§CHAR => §CHAR]
								...ToLower: §DEF ...ToLower € [§CHAR => §CHAR]
							}
							
							§EXPORT (
								(§a .ToUpper) .== §A
								(§A .ToLower) .== §a
							)
							""",
							"",
							(Module_Char.Data, Module_Char.Type),
							__ => aDebugStream(__())
						).Then(
							__ => __.Data
						).ElseThrow(
						),
						mVM_Data.Tuple(
							[
								mVM_Data.Bool(true),
								mVM_Data.Bool(true),
							]
						),
						null,
						__ => mVM_Data.ToText(__, 4)
					);
				}
			),
			mTest.Test("Text",
				aDebugStream => {
					var Module_Text = mModule.Module_Text.Init(
						aDebugStream
					).ElseThrow(
					);
					
					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							"""
							§IMPORT {
								...==...: §DEF ...==... € [[§TEXT, §TEXT] => §BOOL]
								...ToUpper: §DEF ...ToUpper € [§TEXT => §TEXT]
								...ToLower: §DEF ...ToLower € [§TEXT => §TEXT]
								...Trim: §DEF ...Trim € [§TEXT => §TEXT]
								...TrimStart: §DEF ...TrimStart € [§TEXT => §TEXT]
								...TrimEnd: §DEF ...TrimEnd € [§TEXT => §TEXT]
							}
							
							§EXPORT (
								("abc" .ToUpper) .== "ABC"
								("ABC" .ToLower) .== "abc"
								("
								|  hello  
								" .Trim) .== "hello"
								("  hello  " .TrimStart) .== "hello  "
								("  hello  " .TrimEnd) .== "  hello"
							)
							""",
							"",
							(Module_Text.Data, Module_Text.Type),
							__ => aDebugStream(__())
						).Then(
							__ => __.Data
						).ElseThrow(
						),
						mVM_Data.Tuple(
							[
								mVM_Data.Bool(true),
								mVM_Data.Bool(true),
								mVM_Data.Bool(true),
								mVM_Data.Bool(true),
								mVM_Data.Bool(true),
							]
						),
						default,
						__ => __.ToText(20)
					);
				}
			),
			mTest.Test("Maybe",
				aDebugStream => {
					var TestFolder = mFS.CWD() / "TestFiles" / "Modules";
					var ConsumerFile = TestFolder.GetFile("MaybeConsumer.SPO");
					var ExpectedFile = TestFolder.GetFile("MaybeConsumer.result.SPO");
					var Modules = mModule.Modules.Init(
						aDebugStream
					).ElseThrow(
					);
					
					var Expected = mSPO_Interpreter.Run(
						ExpectedFile.TryReadText().ElseThrow(),
						(TestFolder._Path / ExpectedFile.Name).ToText(),
						(mVM_Data.Empty(), mVM_Type.Empty()),
						__ => aDebugStream(__())
					).ElseThrow();

					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							ConsumerFile.TryReadText().ElseThrow(),
							(TestFolder._Path / ConsumerFile.Name).ToText(),
							(Modules.Data, Modules.Type),
							__ => aDebugStream(__())
						).Then(
							__ => __.Data
						).ElseThrow(
						),
						Expected.Data,
						default,
						__ => __.ToText(20)
					);
				}
			),
			mTest.Test("Result",
				aDebugStream => {
					var TestFolder = mFS.CWD() / "TestFiles" / "Modules";
					var ConsumerFile = TestFolder.GetFile("ResultConsumer.SPO");
					var ExpectedFile = TestFolder.GetFile("ResultConsumer.result.SPO");
					var Modules = mModule.Modules.Init(
						aDebugStream
					).ElseThrow(
					);

					var Expected = mSPO_Interpreter.Run(
						ExpectedFile.TryReadText().ElseThrow(),
						(TestFolder._Path / ExpectedFile.Name).ToText(),
						(mVM_Data.Empty(), mVM_Type.Empty()),
						__ => aDebugStream(__())
					).ElseThrow();

					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							ConsumerFile.TryReadText().ElseThrow(),
							(TestFolder._Path / ConsumerFile.Name).ToText(),
							(Modules.Data, Modules.Type),
							__ => aDebugStream(__())
						).Then(
							__ => __.Data
						).ElseThrow(
						),
						Expected.Data,
						default,
						__ => __.ToText(20)
					);
				}
			),
		]
	);
}
