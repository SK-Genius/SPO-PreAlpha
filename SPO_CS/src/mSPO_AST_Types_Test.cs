//IMPORT mTest.cs
//IMPORT mSPO_AST_Types.cs

using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

#if NUNIT
using xTestClass = NUnit.Framework.TestFixtureAttribute;
using xTestCase = NUnit.Framework.TestCaseAttribute;

[xTestClass]
#endif
public static class
mSPO_AST_Types_Test {
	
	#if true
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_AST),
		mTest.Test(
			"Literals",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_AST_Types.AddTypesTo(mSPO_AST.Int(1, 1), default),
					mVM_Type.Int()
				);
				mStd.AssertEq(
					mSPO_AST_Types.AddTypesTo(mSPO_AST.False(1), default),
					mVM_Type.Bool()
				);
			}
		),
		mTest.Test(
			"Tuple",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_AST_Types.AddTypesTo(
						mSPO_AST.Tuple(
							1,
							mStream.Stream<mSPO_AST.tExpressionNode<int>>(
								mSPO_AST.Int(1, 1),
								mSPO_AST.True(1)
							)
						),
						default
					),
					mVM_Type.Tuple(
						mVM_Type.Int(),
						mVM_Type.Bool()
					)
				);
				mStd.AssertEq(
					mSPO_AST_Types.AddTypesTo(mSPO_AST.False(1), default),
					mVM_Type.Bool()
				);
			}
		),
		mTest.Test(
			"Lambda",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_AST_Types.AddTypesTo(
						mSPO_AST.Lambda(
							1,
							mSPO_AST.Match(
								1,
								mSPO_AST.MatchPrefix(
									1,
									mSPO_AST.Ident(1, "BLA"),
									mSPO_AST.Match(
										1,
										mSPO_AST.MatchFreeIdent(1, "a"),
										mSPO_AST.BoolType(1)
									)
								),
								null
							),
							mSPO_AST.Ident(1, "a")
						),
						default
					),
					mVM_Type.Proc(
						mVM_Type.Empty(),
						mVM_Type.Prefix("_BLA", mVM_Type.Bool()),
						mVM_Type.Bool()
					)
				);
				mStd.AssertEq(
					mSPO_AST_Types.AddTypesTo(mSPO_AST.False(1), default),
					mVM_Type.Bool()
				);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("Literals")]
	[xTestCase("Tuple")]
	[xTestCase("Lambda")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
	
	#endif
	
}
