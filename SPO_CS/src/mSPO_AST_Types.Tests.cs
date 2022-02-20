//IMPORT mTest.cs
//IMPORT mSPO_AST_Types.cs

#nullable enable

public static class
mSPO_AST_Types_Tests {
	
	#if true
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_AST),
		mTest.Test(
			"Literals",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.Int(1, 1), default),
					mResult.OK(mVM_Type.Int())
				);
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.False(1), default),
					mResult.OK(mVM_Type.Bool())
				);
			}
		),
		mTest.Test(
			"Tuple",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(
						mSPO_AST.Tuple(
							1,
							mStream.Stream<mSPO_AST.tExpressionNode<int>>(
								mSPO_AST.Int(1, 1),
								mSPO_AST.True(1)
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
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.False(1), default),
					mResult.OK(mVM_Type.Bool())
				);
			}
		),
		mTest.Test(
			"Lambda",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_AST_Types.UpdateExpressionTypes(
						mSPO_AST.Lambda(
							1,
							mStd.cEmpty,
							mSPO_AST.Match(
								1,
								mSPO_AST.MatchPrefix(
									1,
									mSPO_AST.Ident(1, "Bla..."),
									mSPO_AST.Match(
										1,
										mSPO_AST.MatchFreeIdent(1, "a"),
										mSPO_AST.BoolType(1)
									)
								),
								mStd.cEmpty
							),
							mSPO_AST.Ident(1, "a")
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
				
				var AST = mSPO_Parser.Expression.ParseText("(#Bla (§DEF a € §BOOL)) => a", "", a => { aDebugStream(a()); });
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
					mSPO_AST_Types.UpdateExpressionTypes(mSPO_AST.False(1), default),
					mResult.OK(mVM_Type.Bool())
				);
			}
		)
	);
	
	#endif
}
