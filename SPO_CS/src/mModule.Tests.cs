// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT mVM_Data
// IMPORT mStdLib
// IMPORT mSPO_Interpreter

public static class
mModule_Tests {
	
	private static tText ModuleFolder = System.IO.Path.Combine(
		System.IO.Directory.GetParent(
			System.IO.Path.GetDirectoryName(
				mStd.File()
			)!
		).FullName,
		"Modules"
	);
	
	public static mLazy.tLazy<
		mResult.tResult<
			(mVM_Data.tData Data, mVM_Type.tType Type, tText Log),
			(tText Error, tText Log)
		>
	>
	Module_Std = mLazy.Lazy(
		() => {
			var Log = "";
			var WriteToLog = (mStd.tFunc<tText> aGetLine) => {
				Log +="\n" + aGetLine();
			};
			
			var Std_ILT_Path = System.IO.Path.Combine(ModuleFolder, "Std.ILT");
			
			try {
				var Res = mVM.Run(
					mIL_Parser.Module.ParseText(
						System.IO.File.ReadAllText(Std_ILT_Path),
						Std_ILT_Path,
						_ => WriteToLog(_)
					),
					(mVM_Data.Empty(), mVM_Type.Empty()),
					mTextParser.ToText,
					_ => WriteToLog(_)
				);
				
				return mResult.OK(
					(
						Res.Data,
						Res.Type,
						Log
					)
				).WithErrorType<(tText Error, tText Log)>();
			} catch	(System.Exception e) {
				return mResult.Fail(
					(
						e.ToString(),
						Log
					)
				);
			}
		}
	);
	
	public static mLazy.tLazy<
		mResult.tResult<
			(mVM_Data.tData Data, mVM_Type.tType Type, tText Log),
			(tText Error, tText Log)
		>
	>
	Module_Char = mLazy.Lazy(
		() => Module_Std.Value.ThenTry(
			aModule_Std => {
				var Log = "";
				var WriteToLog = (mStd.tFunc<tText> aGetLine) => {
					Log +="\n" + aGetLine();
				};
				
				var ModulePath_Char = System.IO.Path.Combine(ModuleFolder, "Char.SPO");
				
				return mSPO_Interpreter.Run(
					System.IO.File.ReadAllText(ModulePath_Char),
					ModulePath_Char,
					(aModule_Std.Data, aModule_Std.Type),
					_ => WriteToLog(_)
				).Then(
					_ => (_.Data, _.Type, Log)
				).ModifyError(
					_ => (""+_, Log)
				);
			}
		)
	);
	
	public static  mLazy.tLazy<
		mResult.tResult<
			(mVM_Data.tData Data, mVM_Type.tType Type, tText Log),
			(tText Error, tText Log)
		>
	>
	Module_Text = mLazy.Lazy(
		() => Module_Std.Value.ThenTry(
			aModule_Std => Module_Char.Value.ThenTry(
				aModule_Char => {
					var Log = "";
					var WriteToLog = (mStd.tFunc<tText> aGetLine) => {
						Log +="\n" + aGetLine();
					};
					
					var ModulePath_Text = System.IO.Path.Combine(ModuleFolder, "Text.SPO");
					
					return mSPO_Interpreter.Run(
						System.IO.File.ReadAllText(ModulePath_Text),
						ModulePath_Text,
						(
							mVM_Data.Record(
								[
									("Std", aModule_Std.Data),
									("Char", aModule_Char.Data),
								]
							),
							mVM_Type.Record(
								[
									("Std", aModule_Std.Type),
									("Char", aModule_Char.Type),
								]
							)
						),
						_ => WriteToLog(_)
					).Then(
						_ => (_.Data, _.Type, Log)
					).ModifyError(
						_ => (""+_, Log)
					);
				}
			)
		)
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mModule_Tests),
		[
			mTest.Test("Std",
				aDebugStream => {
					var Module_Std_ = Module_Std.Value.ElseThrow(
						_ => {
							aDebugStream(_.Log);
							return _.Error;
						}
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
							(Module_Std_.Data, Module_Std_.Type),
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
					var Module_Char_ = Module_Char.Value.ElseThrow(
						_ => {
							aDebugStream(_.Log);
							return _.Error;
						}
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
							(Module_Char_.Data, Module_Char_.Type),
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
					var Module_Text_ = Module_Text.Value.ElseThrow(
						_ => {
							aDebugStream(_.Log);
							return _.Error;
						}
					);
					
//								...==...: §DEF ...==... € [[§TEXT, §TEXT] => §BOOL]
//								...ToUpper: §DEF ...ToUpper € [§TEXT => §TEXT]
//								...ToLower: §DEF ...ToLower € [§TEXT => §TEXT]
//								...Trim: §DEF ...Trim € [§TEXT => §TEXT]
//								...TrimStart: §DEF ...TrimStart € [§TEXT => §TEXT]
//								...TrimEnd: §DEF ...TrimEnd € [§TEXT => §TEXT]
					mAssert.AreEquals(
						mSPO_Interpreter.Run(
							"""
							§IMPORT {
								...==...: §DEF ...==... € [[§TEXT, §TEXT] => §BOOL]
							}
							
							§EXPORT ()
							""",
//								("abc" .ToUpper) .== "ABC"
//								("ABC" .ToLower) .== "abc"
//								("  hello  " .Trim) .== "hello"
//								("  hello  " .TrimStart) .== "hello  "
//								("  hello  " .TrimEnd) .== "  hello"
//							)
//							""",
							"",
							(Module_Text_.Data, Module_Text_.Type),
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
						)
					);
				}
			),
		]
	);
}

