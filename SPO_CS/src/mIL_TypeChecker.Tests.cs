#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mTest.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
#:ref Common/mStream.cs
#:ref mIL_Parser.cs
#:ref mIL_TypeChecker.cs
#:ref mIL_GenerateOpcodes.cs
#:ref mSPO_Interpreter.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mIL_TypeChecker_Tests {
	private static mIL_AST.tModule<tSpan>
	Parse(
		tText aSource
	) => mIL_Parser.Module.TryParseText(
		aSource + "\n",
		"",
		__ => {}
	).ElseThrow();
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mIL_TypeChecker),
		[
			mTest.Test("IL interface construction checks the hidden receiver against its methods",
				aDebug => {
					const tText cSource = """
					§TYPES
						t := [§FREE TYPE]
						tF := [EMPTY_TYPE => INT]
						tMethod := [t : tF]
						tField := [#Read tMethod]
						tBody := [{EMPTY_TYPE} + tField]
						tInterface := [§ANY t => tBody]
						tWrongMethod := [TRUE : tF]
						tMain := [tWrongMethod => tF]
					§DEF Main € tMain
						Field := +#Read ENV
						Methods := {EMPTY} + Field
						Payload := ONE, Methods
						Object := §INTERFACE tInterface WITH Payload
						§RETURN ONE IF TRUE
					""";
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(cSource), out var Error));
					var Valid = cSource.Replace("tWrongMethod := [TRUE : tF]", "tWrongMethod := [INT : tF]");
					mAssert.IsTrue(mIL_TypeChecker.AreModuleTypesValid(Parse(Valid), out Error), () => mIL_TypeChecker.ErrorText(Error));
					var InvalidHead = Valid.Replace("[t : tF]", "[INT : tF]");
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(InvalidHead), out Error));
					mAssert.IsTrue(Error.ErrorText.Contains("hidden type"), Error.ErrorText);
					var Call = Valid.Replace("§RETURN ONE IF TRUE", "Endpoint := +#Read EMPTY\nCall := Object, Endpoint\nValue := §OBJ:Call EMPTY\n§RETURN Value IF TRUE");
					mAssert.IsTrue(mIL_TypeChecker.AreModuleTypesValid(Parse(Call), out Error), () => mIL_TypeChecker.ErrorText(Error));
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(Call.Replace("Endpoint := +#Read", "Endpoint := +#Missing")), out Error));
					mAssert.IsTrue(Error.ErrorText.Contains("Unknown interface method"), Error.ErrorText);
					var Extract = Valid.Replace("§RETURN ONE IF TRUE", "Method := {Object} #Read\n§RETURN ONE IF TRUE");
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(Extract), out Error));
					mAssert.IsTrue(Error.ErrorText.Contains("expected record"), Error.ErrorText);
					var Subset = Valid.Replace("§RETURN ONE IF TRUE", "Subset := §SUBINTERFACE tInterface WITH Object\n§RETURN ONE IF TRUE");
					mAssert.IsTrue(mIL_TypeChecker.AreModuleTypesValid(Parse(Subset), out Error), () => mIL_TypeChecker.ErrorText(Error));
					var Widen = Subset.Replace(
						"tWrongMethod :=", "tAdminField := [#Admin tMethod]\ntAdminBody := [{tBody} + tAdminField]\ntAdmin := [§ANY t => tAdminBody]\ntWrongMethod :="
					).Replace("§RETURN ONE IF TRUE", "Restored := §SUBINTERFACE tAdmin WITH Subset\n§RETURN ONE IF TRUE");
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(Widen), out Error));
					mAssert.IsTrue(Error.ErrorText.Contains("missing field 'Admin'"), Error.ErrorText);
				}
			),
			mTest.Test(
				"Record cell writes require matching field types",
				aDebug => {
					const tText cSource = """
					§TYPES
						tF := [EMPTY_TYPE => INT]
						tD := [EMPTY_TYPE => tF]
					§DEF Main € tD
						GoodField := +#Number ONE
						Good := {EMPTY} + GoodField
						Box := §VAR Good
						BadField := +#Number TRUE
						Bad := {EMPTY} + BadField
						§VAR Box <- Bad
						§RETURN ONE IF TRUE
					""";
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(cSource), out var Error));
					mAssert.AreEquals(Error.Pos.Start.Row, 10u);
					mAssert.IsTrue(Error.ErrorText.StartsWith("found:"));
					var Valid = cSource.Replace("BadField := +#Number TRUE", "BadField := +#Number ONE");
					mAssert.IsTrue(
						mIL_TypeChecker.AreModuleTypesValid(Parse(Valid), out Error),
						() => mIL_TypeChecker.ErrorText(Error)
					);
				}
			),
			mTest.Test(
				"Immutable use-site types discard earlier mutable access facts",
				aDebug => {
					const tText cSource = """
					§TYPES
						tV := [§VAR INT]
						tA := [tV | INT]
						tR := [#Result INT]
						tCase := [tV => tR]
						tEnv := [INT, INT]
						tInner := [EMPTY_TYPE => INT]
						tHelper := [tEnv => tInner]
						tConstructors := [tCase, tHelper]
						tF := [tA => ANY]
						tD := [tConstructors => tF]
					§DEF Main € tD
						Case := §1ST ENV
						Helper := §2ND ENV
						§TRY_RETURN .Case ARG
						Capture := ARG, ONE
						Closure := §CLOSURE Helper Capture
						Value := .Closure EMPTY
						§RETURN Value IF TRUE
					""";
					var Direct = cSource.Replace("tHelper := [tEnv => tInner]", "tHelper := [INT => tInner]").Replace(
						"Capture := ARG, ONE",
						"Capture := ARG"
					);
					foreach (var Code in mStream.Stream(cSource, Direct)) {
						mAssert.IsTrue(
							mIL_TypeChecker.AreModuleTypesValid(Parse(Code), out var Error),
							() => mIL_TypeChecker.ErrorText(Error)
						);
					}
				}
			),
			mTest.Test(
				"Free result types use inferred bindings before the conservative fallback",
				__ => {
					const tText cSource = """
					§TYPES
						t := [§FREE TYPE]
						tV := [§VAR t]
						tF := [ARG_TYPE => RESULT_TYPE]
						tG := [§ALL t => tF]
						tMain := [EMPTY_TYPE => ANY]
						tD := [tG => tMain]
					§DEF Main € tD
						Value := .ENV ONE
						§RETURN Value IF TRUE
					""";
					foreach (
						var (ArgumentType, ResultType, Valid) in mStream.Stream(
							("t", "t", true),
							("t", "tV", false),
							("INT", "t", false)
						)
					) {
						var Module = Parse(cSource.Replace("ARG_TYPE", ArgumentType).Replace("RESULT_TYPE", ResultType));
						mAssert.AreEquals(mIL_TypeChecker.AreModuleTypesValid(Module, out var Error), Valid);
						if (!Valid) {
							mAssert.AreEquals(Error.ErrorText, "mutable module export is not allowed");
						}
					}
				}
			),
			mTest.Test(
				"Module exports require immutable values even with union or ANY contracts",
				aDebug => {
					const tText cSource = """
					§TYPES
						tV := [§VAR INT]
						tUnion := [INT | tV]
						tResult := [#Result EXPORT_TYPE]
						tFactory := [EMPTY_TYPE => tResult]
						tD := [tFactory => tFactory]
					§DEF Main € tD
						Value := .ENV EMPTY
						RETURN_COMMAND
					""";
					foreach (var Type in mStream.Stream("tUnion", "ANY")) {
						var TypedSource = cSource.Replace("EXPORT_TYPE", Type);
						foreach (
							var Return in mStream.Stream(
								"§RETURN Value IF TRUE",
								"§RETURN Value IF_NOT_EMPTY",
								"§TRY_RETURN .ENV EMPTY"
							)
						) {
							var Module = Parse(TypedSource.Replace("RETURN_COMMAND", Return));
							mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Module, out var Error));
							mAssert.AreEquals(Error.ErrorText, "mutable module export is not allowed");
							mAssert.AreEquals(Error.Pos.Start.Row, 9u);
						}
						var KnownImmutable = Parse(
							TypedSource.Replace("Value := .ENV EMPTY", "Value := +#Result ONE").Replace(
								"RETURN_COMMAND",
								"§RETURN Value IF TRUE"
							)
						);
						mAssert.IsTrue(
							mIL_TypeChecker.AreModuleTypesValid(KnownImmutable, out var KnownError),
							() => mIL_TypeChecker.ErrorText(KnownError)
						);
					}
				}
			),
			mTest.Test(
				"Composites share access rights while individual values retain theirs",
				aDebug => {
					const tText cHeader = """
					§TYPES
						tV := [§VAR INT]
						tF := [tV => INT]
						tM := [tV : tF]
						tD := [EMPTY_TYPE => tM]
					§DEF Main € tD
					""";
					const tText cBody = """
						Mixed := OBJ, ARG
						Recovered := §1ST Mixed
						Value := §VAR Recovered ->
						§RETURN Value IF TRUE
					""";
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(cHeader + "\n" + cBody), out var Error));
					mAssert.AreEquals(Error.ErrorText, "opaque mutable access is not allowed");
					var Individual = cBody.Replace("Recovered := §1ST Mixed", "Recovered := OBJ");
					mAssert.IsTrue(
						mIL_TypeChecker.AreModuleTypesValid(Parse(cHeader + "\n" + Individual), out Error),
						() => mIL_TypeChecker.ErrorText(Error)
					);
					const tText cImmutableProjection = """
						Mixed := ONE, ARG
						Value := §1ST Mixed
						§RETURN Value IF TRUE
					""";
					mAssert.IsTrue(
						mIL_TypeChecker.AreModuleTypesValid(Parse(cHeader + "\n" + cImmutableProjection), out Error),
						() => mIL_TypeChecker.ErrorText(Error)
					);
				}
			),
			mTest.Test(
				"Opaque writes invalidate previously built composites without tracking their parts",
				__ => {
					var Source = """
					§TYPES
						tV := [§VAR INT]
						tF := [tV => INT]
						tM := [tV : tF]
						tD := [EMPTY_TYPE => tM]
					§DEF Main € tD
						Box := §VAR OBJ
						Mixed := Box, ONE
						§VAR Box <- ARG
						Recovered := §1ST Mixed
						Handle := §VAR Recovered ->
						Value := §VAR Handle ->
						§RETURN Value IF TRUE
					""";
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(Source), out var Error));
					mAssert.AreEquals(Error.ErrorText, "opaque mutable access is not allowed");
					mAssert.AreEquals(Error.Pos.Start.Row, 11u);
					var Direct = Parse(Source.Replace("Recovered := §1ST Mixed", "Recovered := Box"));
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Direct, out Error));
					mAssert.AreEquals(Error.ErrorText, "opaque mutable access is not allowed");
					mAssert.AreEquals(Error.Pos.Start.Row, 12u);
				}
			),
			mTest.Test(
				"Body type errors precede access errors",
				__ => {
					var Source = """
					§TYPES
						tV := [§VAR INT]
						tF := [tV => INT]
						tD := [EMPTY_TYPE => tF]
					§DEF Main € tD
						Read := §VAR ARG ->
						Bad := §INT TRUE + ONE
						§RETURN ONE IF TRUE
					""";
					
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(Source), out var Error));
					mAssert.IsTrue(Error.ErrorText.StartsWith("expected §INT"));
					mAssert.AreEquals(Error.Pos.Start.Row, 7u);
					mAssert.IsFalse(
						mIL_TypeChecker.AreModuleTypesValid(
							Parse(
								Source.Replace(
									"Bad := §INT TRUE + ONE",
									"Bad := ONE"
								)
							),
							out Error
						)
					);
					mAssert.AreEquals(Error.ErrorText, "opaque mutable access is not allowed");
					mAssert.AreEquals(Error.Pos.Start.Row, 6u);
				}
			),
			mTest.Test(
				"TRY_AS_VAR retains the cell identity shared by source aliases",
				__ => {
					var Module = Parse(
						"""
						§TYPES
							tV := [§VAR INT]
							tBox := [§VAR tV]
							tObject := [tBox | EMPTY_TYPE]
							tF := [tV => INT]
							tM := [tObject : tF]
							tD := [EMPTY_TYPE => tM]
						§DEF Main € tD
							Alias := OBJ
							First := §TRY OBJ AS_VAR
							Second := §TRY Alias AS_VAR
							§VAR First <- ARG
							Handle := §VAR Second ->
							Value := §VAR Handle ->
							§RETURN Value IF TRUE
						"""
					);
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Module, out var Error));
					mAssert.AreEquals(Error.ErrorText, "opaque mutable access is not allowed");
					mAssert.AreEquals(Error.Pos.Start.Row, 14u);
				}
			),
			mTest.Test(
				"SIG keeps payload types from before a later return refinement",
				aDebug => {
					var Module = Parse(
						"""
						§TYPES
							t := [§FREE TYPE]
							tS := [§SIG_WITH t IN t]
							tF := [EMPTY_TYPE => ANY]
							tD := [EMPTY_TYPE => tF]
						§DEF main € tD
							Payload := INT_TYPE, ONE
							Package := §SIG tS WITH Payload
							Alias := Package
							Head := §SIG_HEAD Package
							§RETURN Payload IF_NOT_EMPTY
							Known := §TRY Alias HAS_HEAD_TYPE INT_TYPE
							Body := §SIG_BODY Known
							§RETURN Body IF TRUE
						"""
					);
						
					mAssert.IsTrue(
						mIL_TypeChecker.AreModuleTypesValid(Module, out var Error),
						() => mIL_TypeChecker.ErrorText(Error)
					);
				}
			),
			mTest.Test(
				"TRY_RETURN narrows all argument aliases before subsequent calls",
				aDebug => {
					var Source = """
					§TYPES
						tA := [INT | EMPTY_TYPE]
						tR := [#Result INT]
						tCase := [INT => tR]
						tEmpty := [EMPTY_TYPE => EMPTY_TYPE]
						tEnv := [tCase, tEmpty]
						tF := [tA => tR]
						tD := [tEnv => tF]
					§DEF main € tD
						Callback := §1ST ENV
						Empty := §2ND ENV
						Alias := ARG
						§TRY_RETURN .Callback ARG
						Remaining := .Empty Alias
						§RETURN Remaining IF TRUE
					""";
					
					mAssert.IsTrue(
						mIL_TypeChecker.AreModuleTypesValid(Parse(Source), out var Error),
						() => mIL_TypeChecker.ErrorText(Error)
					);
					mAssert.IsFalse(
						mIL_TypeChecker.AreModuleTypesValid(
							Parse(Source.Replace("§TRY_RETURN .Callback ARG", "")),
							out _
						)
					);
				}
			),
			mTest.Test(
				"TRY_RETURN carries method guard effects into cell access",
				aDebug => {
					var Source = """
					§TYPES
						tV := [§VAR INT]
						tBox := [§VAR tV]
						tB := [TRUE | FALSE]
						tR := [#Result INT]
						tCase := [tV => tR]
						tPredicate := [tV => tB]
						tGuard := [tBox : tPredicate]
						tEnv := [tCase, tGuard]
						tM := [tBox : tCase]
						tD := [tEnv => tM]
					§DEF Main € tD
						Callback := §1ST ENV
						Guard := §2ND ENV
						Local := §VAR ONE
						Box := §VAR Local
						§TRY_RETURN .Callback ARG IF Guard
						Handle := §VAR Box ->
						Value := §VAR Handle ->
						Result := +#Result Value
						§RETURN Result IF TRUE
					""";
					
					mAssert.IsFalse(mIL_TypeChecker.AreModuleTypesValid(Parse(Source), out var Error));
					mAssert.AreEquals(Error.ErrorText, "opaque mutable access is not allowed");
					mAssert.AreEquals(Error.Pos.Start.Row, 19u);
					var FunctionGuard = Parse(Source.Replace("tGuard := [tBox : tPredicate]", "tGuard := [tV => tB]"));
					mAssert.IsTrue(
						mIL_TypeChecker.AreModuleTypesValid(FunctionGuard, out Error),
						() => mIL_TypeChecker.ErrorText(Error)
					);
				}
			),
			mTest.Test(
				"Recursive contracts are independent of helper definition order",
				aDebug => {
					const tText cTypes = """
					§TYPES
						tSelf := [§FREE TYPE]
						tF := [INT => INT]
						tEnv := [tSelf, INT]
						tConstructor := [tEnv => tF]
						tD := [§REC tSelf => tConstructor]
						tMain := [EMPTY_TYPE => tF]
					""";
					
					const tText cFirst = """
					§DEF First € tD
						Self := §1ST ENV
						Function := .Self ENV
						Value := .Function ARG
						§RETURN Value IF TRUE
					""";
					
					const tText cSecond = """
					§DEF Second € tD
						Value := §2ND ENV
						§RETURN Value IF TRUE
					""";
					
					const tText cMain = """
					§DEF Main € tMain
						§RETURN ARG IF TRUE
					""";
					
					foreach (var Helpers in mStream.Stream(cFirst + "\n" + cSecond, cSecond + "\n" + cFirst)) {
						var Module = Parse(cTypes + "\n" + Helpers + "\n" + cMain);
						var Before = Module.ToILT();
						for (var Repeat = 0; Repeat < 2; Repeat++) {
							mAssert.IsTrue(
								mIL_TypeChecker.AreModuleTypesValid(Module, out var Error),
								() => mIL_TypeChecker.ErrorText(Error)
							);
							mIL_GenerateOpcodes.GenerateOpcodes(Module, (mVM_Data.Int(1), mVM_Type.Int()));
						}
						mAssert.AreEquals(Module.ToILT(), Before);
					}
				}
			),
		]
	);
}
