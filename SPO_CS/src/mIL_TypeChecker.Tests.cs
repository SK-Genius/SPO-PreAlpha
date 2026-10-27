#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mTest.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref Common/mArrayList.cs
#:ref Common/mSpan.cs
#:ref Common/mTextStream.cs
#:ref mIL_AST.cs
#:ref mIL_Parser.cs
#:ref mIL_TypeChecker.cs

public static class
mIL_TypeChecker_Tests {
	public static readonly mTest.tTest
	Tests = mStd.Call(
		() => {
			const tText cValid = """
			§TYPES
				t := [§FREE TYPE]
				tF := [EMPTY_TYPE => INT]
				tMethod := [t : tF]
				tField := [#Read tMethod]
				tBody := [{EMPTY_TYPE} + tField]
				tInterface := [§ANY t => tBody]
				tImplementation := [INT : tF]
				tMain := [tImplementation => tF]
			§DEF Main € tMain
				Field := +#Read ENV
				Methods := {EMPTY} + Field
				Payload := ONE, Methods
				Object := §INTERFACE tInterface WITH Payload
				§RETURN ONE IF TRUE
			""";
			
			var Call = cValid.Replace(
				"§RETURN ONE IF TRUE",
				"""
				Endpoint := +#Read EMPTY
				Call := Object, Endpoint
				Value := §OBJ:Call EMPTY
				§RETURN Value IF TRUE
				"""
			);
			
			var Subset = cValid.Replace(
				"§RETURN ONE IF TRUE",
				"""
				Subset := §SUBINTERFACE tInterface WITH Object
				§RETURN ONE IF TRUE
				"""
			);
			
			return mTest.Tests(
				"IL interface contracts",
				mStream.Stream<(tText Name, tText Source, tBool Valid, tText Error)>(
					(
						"Compatible hidden receiver",
						cValid,
						true,
						""
					),
					(
						"Incompatible hidden receiver",
						cValid.Replace(
							"tImplementation := [INT : tF]",
							"tImplementation := [TRUE : tF]"
						),
						false,
						""
					),
					(
						"Contract must use its hidden type",
						cValid.Replace(
							"[t : tF]",
							"[INT : tF]"
						),
						false,
						"hidden type"
					),
					(
						"Bound method dispatch",
						Call,
						true,
						""
					),
					(
						"Missing method",
						Call.Replace(
							"Endpoint := +#Read",
							"Endpoint := +#Missing"
						),
						false,
						"Unknown interface method"
					),
					(
						"Payload cannot be projected as a record",
						cValid.Replace(
							"§RETURN ONE IF TRUE",
							"""
							Method := {Object} #Read
							§RETURN ONE IF TRUE
							"""
						),
						false,
						"expected record"
					),
					(
						"Subinterface preserves allowed methods",
						Subset,
						true,
						""
					),
					(
						"Subinterface cannot add unavailable methods",
						Subset.Replace(
							"tImplementation :=",
							"""
							tAdminField := [#Admin tMethod]
							tAdminBody := [{tBody} + tAdminField]
							tAdmin := [§ANY t => tAdminBody]
							tImplementation :=
							"""
						).Replace(
							"§RETURN ONE IF TRUE",
							"""
							Restored := §SUBINTERFACE tAdmin WITH Subset
							§RETURN ONE IF TRUE
							"""
						),
						false,
						"missing field 'Admin'"
					)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						__ => {
							var Module = mIL_Parser.Module.TryParseText(
								aCase.Source + "\n",
								"",
								_ => {}
							).ElseThrow();
							
							var Valid = mIL_TypeChecker.AreModuleTypesValid(Module, out var Error);
							
							mAssert.IsTrue(
								Valid == aCase.Valid,
								() => (
									Valid
									? "unexpected interface acceptance"
									: mIL_TypeChecker.ErrorText(Error)
								)
							);
							
							if (!Valid && aCase.Error.Length > 0) {
								mAssert.IsTrue(Error.ErrorText.Contains(aCase.Error), Error.ErrorText);
							}
						}
					)
				).ToArrayList(
				).ToArray()
			);
		}
	);
}