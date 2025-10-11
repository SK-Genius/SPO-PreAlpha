// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT mVM_Data
// IMPORT mStdLib
// IMPORT mSPO_Interpreter

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
							_ => aDebugStream(_())
						).Then(
							_ => _.Data
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
							_ => aDebugStream(_())
						).Then(
							_ => _.Data
						).ElseThrow(
						),
						mVM_Data.Tuple(
							[
								mVM_Data.Bool(true),
								mVM_Data.Bool(true),
							]
						),
						null,
						_ => mVM_Data.ToText(_, 4)
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
							_ => aDebugStream(_())
						).Then(
							_ => _.Data
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
						_ => _.ToText(20)
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
								...>>...: §DEF ...>>... € [
									§GENERIC tIn [
										§GENERIC tOut [
											[
												[tIn | []]
												[tIn => tOut]
											] => [tOut | []]
										]
									]
								]
								...|...: §DEF ...|... € [
									§GENERIC t [
										[
											[t | []]
											t
										] => t
									]
								]
							}
							
							§DEF X € [§INT | []] = 1
							§DEF Y € [§INT | []] = ()
							
							§EXPORT (
								X §>.>> (§DEF a € [§INT | []] => §TRUE) §>.| §FALSE
								Y §>.>> (§DEF a € [§INT | []] => §TRUE) §>.| §FALSE
								X .| 0
								Y .| 0
							)
							""",
							"",
							(Module_Maybe.Data, Module_Maybe.Type),
							_ => aDebugStream(_())
						).Then(
							_ => _.Data
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
						_ => _.ToText(20)
					);
				}
			),
		]
	);
}