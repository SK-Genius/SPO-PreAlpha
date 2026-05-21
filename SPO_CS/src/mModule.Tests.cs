#:include Common/mStd.cs
#:include Common/mTest.cs
#:include Common/mAssert.cs
#:include mVM_Data.cs
#:include mSPO_Interpreter.cs
#:include mModule.cs

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
					var Module_Maybe = mModule.Module_Maybe.Init(
						aDebugStream
					).ElseThrow(
					);
					
					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							"""
							§IMPORT {
								tMaybe...: §DEF tMaybe... € [§TYPE => §TYPE]
								...>>...: §DEF ...>>... € [
									§GENERIC tIn [
										§GENERIC tOut [
											[
												.tMaybe tIn
												tIn => tOut
											] => [.tMaybe tOut]
										]
									]
								]
								...Or...: §DEF ...Or... € [
									§GENERIC t [
										[
											t | []
											t
										] => t
									]
								]
							}
							
							§DEF X € [§INT | []] = 1
							§DEF Y € [§INT | []] = ()
							
							§EXPORT (
								X §>.>> (§DEF a € [§INT | []] => §TRUE) §>.Or §FALSE
								Y §>.>> (§DEF a € [§INT | []] => §TRUE) §>.Or §FALSE
								X .Or 0
								Y .Or 0
							)
							""",
							"",
							(Module_Maybe.Data, Module_Maybe.Type),
							__ => aDebugStream(__())
						).Then(
							__ => __.Data
						).ElseThrow(
						),
						mVM_Data.Tuple(
							[
								mVM_Data.Bool(true),
								mVM_Data.Bool(false),
								mVM_Data.Int(1),
								mVM_Data.Int(0),
							]
						),
						default,
						__ => __.ToText(20)
					);
				}
			),
		]
	);
}