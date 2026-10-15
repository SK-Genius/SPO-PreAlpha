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

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mIL_GenerateOpcodes_Tests {
	private static (mStream.tStream<mVM_Data.tProcDef<tSpan>> Defs, mTreeMap.tTree<tText, tNat32> DefLookup)
	CompileModule(
		tText aSourceCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => mIL_GenerateOpcodes.GenerateOpcodes(
		mIL_Parser.Module.TryParseText(aSourceCode, aId, aTrace).ElseThrow(),
		aTrace
	).ElseThrow();
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(nameof(mIL_GenerateOpcodes),
		[
			mTest.Tests("Mutable access: mutable closure environments can execute",
				mStream.Stream(false, true).Map(
					aExtern => mTest.Test(
						aExtern ? "External closure" : "ILT closure",
						aDebug => {
							var Cell = mVM_Data.Var(mVM_Data.Int(7));
							
							mVM_Data.tData Closure;
							if (aExtern) {
								Closure = mVM_Data.ExternProc((aEnv, aObj, aArg, aTrace) => aEnv, Cell);
							} else {
								var Module = CompileModule(
									"""
									§TYPES
										V := [§VAR INT]
										F := [EMPTY_TYPE => V]
										D := [V => F]
									§DEF f € D
										§RETURN ENV IF TRUE
									
									""",
									"",
									__ => aDebug(__())
								);
								
								Closure = mVM_Data.Proc(
									Module.Defs.TryFirst().AssertNotEmpty(),
									Cell
								);
							}
							
							mAssert.IsTrue(Closure._IsMutable);
							var Result = mVM_Data.Empty();
							mVM.Run<tSpan>(
								Closure,
								mVM_Data.Empty(),
								mVM_Data.Empty(),
								Result,
								mTextParser.ToText,
								__ => aDebug(__())
							);
							
							mAssert.IsTrue(Result.IsVar(out var Content));
							mAssert.IsTrue(Content.IsInt(out var Value));
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
