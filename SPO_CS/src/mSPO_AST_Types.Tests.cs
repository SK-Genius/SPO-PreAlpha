// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mStream
// IMPORT Common/mSpan
// IMPORT Common/mTextStream
// IMPORT Common/mParserGen
// IMPORT mVM_Type
// IMPORT mTokenizer
// IMPORT mSPO_AST
// IMPORT mSPO_AST_Types
// IMPORT mSPO_Parser

public static class
mSPO_AST_Types_Tests {
	#if true
	
	private const tInt32 cNoPos = 1;
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_AST_Types),
		[
			mTest.Test("Literals",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Int(cNoPos, 1).UpdateTypes(mStd.cEmpty),
						mVM_Type.Int()
					);
					mAssert.AreEquals(
						mSPO_AST.False(cNoPos).UpdateTypes(mStd.cEmpty),
						mVM_Type.Bool()
					);
				}
			),
			mTest.Test("Tuple",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Tuple(
							cNoPos,
							[
								mSPO_AST.Int(cNoPos, 1),
								mSPO_AST.True(cNoPos)
							]
						).UpdateTypes(mStd.cEmpty),
						mVM_Type.Tuple(
							[mVM_Type.Int(), mVM_Type.Bool()]
						)
					);
					mAssert.AreEquals(
						mSPO_AST.False(cNoPos).UpdateTypes(mStd.cEmpty),
						mVM_Type.Bool()
					);
				}
			),
			mTest.Test("Lambda",
				aDebugStream => {
					mAssert.AreEquals(
						mSPO_AST.Lambda(
							cNoPos,
							mStd.cEmpty,
							mSPO_AST.Match(
								cNoPos,
								mSPO_AST.MatchPrefix(
									cNoPos,
									mSPO_AST.Id(cNoPos, "Bla..."),
									mSPO_AST.Match(
										cNoPos,
										mSPO_AST.MatchFreeId(cNoPos, "a"),
										mSPO_AST.BoolType(cNoPos)
									)
								),
								mStd.cEmpty
							),
							mSPO_AST.Id(cNoPos, "a")
						).UpdateTypes(mStd.cEmpty),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix("_Bla...", mVM_Type.Bool()),
							mVM_Type.Bool()
						)
					);

					mAssert.AreEquals(
						mSPO_Parser.Expression.ParseText(
							"(#Bla (§DEF a € §BOOL)) => a",
							"",
							_ => { aDebugStream(_()); }
						).UpdateTypes(mStd.cEmpty),
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix("_Bla...", mVM_Type.Bool()),
							mVM_Type.Bool()
						)
					);
					
					mAssert.AreEquals(
						mSPO_AST.False(cNoPos).UpdateTypes(mStd.cEmpty),
						mVM_Type.Bool()
					);
				}
			),
			mTest.Tests("Types",
				mStream.Stream<(tText FileLine, tText Code, mVM_Type.tType Type)>(
					[
						(
							mStd.FileLine(),
							"()",
							mVM_Type.Empty()
						),
						(
							mStd.FileLine(),
							"§TRUE",
							mVM_Type.Bool()
						),
						(
							mStd.FileLine(),
							"§FALSE",
							mVM_Type.Bool()
						),
						(
							mStd.FileLine(),
							"0",
							mVM_Type.Int()
						),
						(
							mStd.FileLine(),
							"(1, §TRUE)",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Bool()])
						),
						(
							mStd.FileLine(),
							"(1, (2, 3), 4)",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]), mVM_Type.Int()])
						),
						(
							mStd.FileLine(),
							"((1, 2), 3)",
							mVM_Type.Tuple([mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]), mVM_Type.Int()])
						),
						(
							mStd.FileLine(),
							"(1, (2, 3))",
							mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()])])
						),
						(
							mStd.FileLine(),
							"#Bla 1",
							mVM_Type.Prefix("_Bla...", mVM_Type.Int())
						),
						(
							mStd.FileLine(),
							"{A: 1, B: 2, C: 3}",
							mVM_Type.Record([("_A", mVM_Type.Int()), ("_B", mVM_Type.Int()), ("_C", mVM_Type.Int())])
						),
					]
				).Map(
					a => {
						var Groups = System.Text.RegularExpressions.Regex.Match(a.FileLine, @"^(.*)\:(\d+)$").Groups;
						var FilePath = Groups[1].Value;
						var LineNr = tInt32.Parse(Groups[2].Value);
						
						return mTest.Test(a.Code,
							aDebugStream => {
								mAssert.AreEquals(
									mSPO_Parser.Expression.ParseText(
										a.Code,
										"",
										_ => { aDebugStream(_()); }
									).UpdateTypes(mStd.cEmpty),
									a.Type
								);
							},
							FilePath,
							LineNr
						);
					}
				).ToArrayList(
				).ToArray(
				)
			),
		]
	);
	
	#endif
}
