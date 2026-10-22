#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mSpan.cs
#:ref Common/mMaybe.cs
#:ref Common/mStream.cs
#:ref Common/mTreeMap.cs
#:ref Common/mAssert.cs
#:ref Common/mTest.cs
#:ref Common/mTextStream.cs
#:ref Common/mParserGen.cs
#:ref mTokenizer.cs
#:ref mIL_GenerateOpcodes.cs
#:ref mIL_AST.cs
#:ref mIL_Parser.cs
#:ref mVM_Data.cs
#:ref mVM.cs
#:ref mModule.cs
#:ref mSPO_Interpreter.cs
#:ref mSPO_Parser.cs
#:ref mSPO_AST.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mIL_GenerateOpcodes_Tests {
	private static (mStream.tStream<mVM_Data.tProcDef<tSpan>> Defs, mTreeMap.tTree<tText, tNat32> DefLookup)
	CompileModule(
		tText aSourceCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		var IL = mIL_Parser.Module.TryParseText(aSourceCode, aId, aTrace).ElseThrow();
		mAssert.IsTrue(mIL_TypeChecker.AreModuleTypesValid(IL, out var Error), () => mIL_TypeChecker.ErrorText(Error));
		return mIL_GenerateOpcodes.GenerateOpcodes(IL, (mVM_Data.Empty(), mVM_Type.Empty()));
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mIL_GenerateOpcodes),
		[
			mTest.Test(
				"Module checking is independent of imports and reusable",
				aDebug => {
					const tText cSource = """
					§TYPES
						F := [INT => INT]
						D := [EMPTY_TYPE => F]
					§DEF main € D
						§RETURN ARG IF TRUE
					
					""";
					var IL = mIL_Parser.Module.TryParseText(cSource, "", __ => {}).ElseThrow();
					var Before = IL.ToILT();
					mAssert.IsTrue(mIL_TypeChecker.AreModuleTypesValid(IL, out var Error), () => mIL_TypeChecker.ErrorText(Error));
					mAssert.AreEquals(IL.ToILT(), Before);
					mAssert.IsTrue(mIL_TypeChecker.AreImportTypesValid(IL, mVM_Type.Int(), out _));
					mAssert.IsFalse(mIL_TypeChecker.AreImportTypesValid(IL, mVM_Type.Empty(), out _));
					foreach (var Value in mStream.Stream(7, 9)) {
						var Import = (mVM_Data.Int(Value), mVM_Type.Int());
						var Compiled = mIL_GenerateOpcodes.GenerateOpcodes(IL, Import);
						var Result = mVM.RunModule(Compiled.Defs, Import.Item1, mTextParser.ToText, __ => {}).ElseThrow();
						mAssert.IsTrue(Result.Data.IsInt(out var Actual));
						mAssert.AreEquals(Actual, Value);
					}
					var Invalid = mIL_Parser.Module.TryParseText(cSource.Replace("IF TRUE", "IF ONE"), "", __ => {}).ElseThrow();
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Invalid, out _));
				}
			),
			mTest.Test(
				"Runtime caches module checks before linking and initialization",
				aDebug => {
					var Setup = mModule.ModuleSetup(mFS.File("TestFiles/ILT_Modules/_Modules/TestValue.ILT").Path, mStd.cEmpty);
					mAssert.IsTrue(Setup.Checked.IsNone());
					Setup.Check(aDebug).ElseThrow();
					var First = Setup.IL;
					Setup.Check(aDebug).ElseThrow();
					mAssert.IsTrue(Setup.Checked.IsSome(out var Status) && Status.Match(out _, out _));
					mAssert.IsTrue(mStd.RefEq(First!, Setup.IL!));
					mAssert.IsTrue(Setup.Module.IsNone());
					mAssert.IsTrue(Setup.Link(aDebug).Match(out _, out _));
					mAssert.IsTrue(Setup.Module.IsNone());
					var Invalid = mModule.ModuleSetup(mFS.File("TestFiles/ILT_Function_Errors/03_01_MutableArgumentRead.ILT").Path, mStd.cEmpty);
					mAssert.IsFalse(Invalid.Check(aDebug).Match(out _, out _));
					mAssert.IsTrue(Invalid.Checked.IsSome(out var Failure) && !Failure.Match(out _, out _));
				}
			),
			mTest.Test(
				"Validation reports invalid type operands",
				aDebug => {
					const tText cSource = """
					§TYPES
						F := [EMPTY_TYPE => EMPTY_TYPE]
						D := [EMPTY_TYPE => F]
					§DEF main € D
						T := [TRUE => INT_TYPE]
						§RETURN EMPTY IF TRUE
					
					""";
					var IL = mIL_Parser.Module.TryParseText(cSource, "", __ => {}).ElseThrow();
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(IL, out var Error));
					mAssert.AreEquals(Error.ErrorText, "expected type or type function");
				}
			),
			mTest.Test(
				"SPO to IL rejects mutable captures without linking",
				aDebug => {
					const tText cSource = """
					§IMPORT ()
					§VAR Cell := 7 .
					§EXPORT () => Cell
					""";
					var Parsed = mSPO_Parser.Module.TryParseText(cSource, "", __ => {}).ElseThrow();
					mAssert.IsFalse(Parsed.TryToILT().Match(out _, out var Error));
					mAssert.IsTrue(Error.Contains("closure environment"));
				}
			),
			mTest.Test(
				"Mutable import records are rejected after checking the module and link",
				aDebug => {
					const tText cSource = """
					§TYPES
						F := [EMPTY_TYPE => INT]
						Init := [F => INT]
						D := [EMPTY_TYPE => Init]
					§DEF main € D
						§RETURN ONE IF TRUE
					
					""";
					var Module = mIL_Parser.Module.TryParseText(cSource, "", __ => aDebug(__())).ElseThrow();
					var Import = mVM_Data.Record(
						[
							("Cell", mVM_Data.Var(mVM_Data.Int(0))),
							("Read", mVM_Data.ExternProc((aEnv, aObj, aArg, aTrace) => aEnv, mVM_Data.Int(7)))
						]
					);
					var Type = mIL_TypeChecker.ModuleSignature(Module).Then(__ => __.Import).ModifyError(mIL_TypeChecker.ErrorText).ElseThrow();
					var Result = mVM.TryRun(Module, (Import, Type), mTextParser.ToText, __ => aDebug(__()));
					mAssert.IsFalse(Result.Match(out _, out var Error));
					mAssert.AreEquals(Error, "mutable module import is not allowed");
				}
			),
			mTest.Tests(
				"Mutable values cannot enter closure environments",
				mStream.Stream(false, true).Map(
					aExtern => mTest.Test(
						aExtern ? "External closure" : "ILT closure",
						aDebug => {
							var Mutable = mVM_Data.Record([("Cell", mVM_Data.Var(mVM_Data.Int(7)))]);
							var Constant = mVM_Data.Int(7);
							mVM_Data.tData Closure;
							if (aExtern) {
								mAssert.ThrowsError(() => mVM_Data.ExternProc((aEnv, aObj, aArg, aTrace) => aEnv, Mutable));
								Closure = mVM_Data.ExternProc((aEnv, aObj, aArg, aTrace) => aEnv, Constant);
							} else {
								var Module = CompileModule(
									"""
									§TYPES
										F := [EMPTY_TYPE => INT]
										D := [INT => F]
									§DEF f € D
										§RETURN ENV IF TRUE
									
									""",
									"",
									__ => aDebug(__())
								);
								var Def = Module.Defs.TryFirst().AssertNotEmpty();
								mAssert.ThrowsError(() => mVM_Data.Proc(Def, Mutable));
								Closure = mVM_Data.Proc(Def, Constant);
							}
							mAssert.IsFalse(Closure._IsMutable);
							var Result = mVM_Data.Empty();
							mVM.Run<tSpan>(Closure, mVM_Data.Empty(), mVM_Data.Empty(), Result, mTextParser.ToText, __ => aDebug(__()));
							mAssert.IsTrue(Result.IsInt(out var Value));
							mAssert.AreEquals(Value, 7);
						}
					)
				).ToArrayList(
				).ToArray()
			),
			mTest.Test("ALL is callable and describes generic function signatures",
				aDebug => {
					const tText Source = """
					§TYPES
						t := [§FREE TYPE]
						Body := [t => t]
						Identity := [§ALL t => Body]
						IdDef := [EMPTY_TYPE => Identity]
						Func := [EMPTY_TYPE => TYPE]
						Def := [IdDef => Func]
					§DEF identity € IdDef
						§RETURN ARG IF TRUE
					§DEF main € Def
						id := .ENV EMPTY
						seven := 7
						value := .id seven
						flag := .id TRUE
						t := [§FREE Type_TYPE]
						Body := [t => t]
						T := [§ALL t => Body]
						result := .T INT_TYPE
						§RETURN result IF TRUE
					
					""";
					var Module = CompileModule(Source, "", __ => aDebug(__()));
					var IdDef = Module.Defs.TryFirst().AssertNotEmpty();
					var Def = Module.Defs.TryGet(Module.DefLookup.TryGet("main").AssertNotEmpty()).AssertNotEmpty();
					var Kind = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Type(), mVM_Type.Type());
					mAssert.IsTrue(IdDef.DefType.Refs[2].KindType().SameType(Kind));
					mAssert.IsTrue(Def.Types.Get(Def._LastReg - 1).SameType(Kind));
					mAssert.IsTrue(Def.Types.Get(Def._LastReg).IsType());
				}
			),
			mTest.Test("SIG uses type values while head registers have type TYPE",
				aDebug => {
					const tText Source = """
					§TYPES
						Func := [EMPTY_TYPE => TYPE]
						Def := [EMPTY_TYPE => Func]
					§DEF main € Def
						t := [§FREE Type_TYPE]
						contract := [§SIG_WITH t IN t]
						body := 7
						payload := INT_TYPE, body
						package := §SIG contract WITH payload
						head := §SIG_HEAD package
						§RETURN head IF TRUE
					
					""";
					var Def = CompileModule(Source, "", __ => aDebug(__())).Defs.TryFirst().AssertNotEmpty();
					mAssert.IsTrue(Def.Types.Get(mVM_Data.cIntTypeReg).IsType());
					mAssert.IsTrue(Def.Types.Get(Def._LastReg).IsType());
				}
			),
		]
	);
}