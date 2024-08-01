public static class
mSPO_AST_Types_Tests {
	#if true
	
	private const tInt32 cNoPos = 1; 
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_AST),
		mTest.Test("Literals",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.Int(cNoPos, 1), default),
					mResult.OK(mVM_Type.Int())
				);
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.False(cNoPos), default),
					mResult.OK(mVM_Type.Bool())
				);
			}
		),
		mTest.Test("Tuple",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(
						mSPO_AST.Tuple(
							cNoPos,
							mStream.Stream<mSPO_AST.tExpressionNode<tInt32>>(
								mSPO_AST.Int(cNoPos, 1),
								mSPO_AST.True(cNoPos)
							)
						),
						default
					),
					mResult.OK(
						mVM_Type.Tuple(
							mVM_Type.Int(),
							mVM_Type.Bool()
						)
					)
				);
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.False(cNoPos), default),
					mResult.OK(mVM_Type.Bool())
				);
			}
		),
		mTest.Test("Lambda",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(
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
						),
						default
					),
					mResult.OK(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix("_Bla...", mVM_Type.Bool()),
							mVM_Type.Bool()
						)
					)
				);
				
				var AST = mSPO_Parser.Expression.ParseText("(#Bla (§DEF a € §BOOL)) => a", "", _ => { aDebugStream(_()); });
				var Type = mSPO_AST_Types.UpdateExpressionTypes(AST, default);
				mAssert.AreEquals(
					Type,
					mResult.OK(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Prefix("_Bla...", mVM_Type.Bool()),
							mVM_Type.Bool()
						)
					)
				);
				
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.False(cNoPos), default),
					mResult.OK(mVM_Type.Bool())
				);
			}
		)
	);
	
	#endif
}
