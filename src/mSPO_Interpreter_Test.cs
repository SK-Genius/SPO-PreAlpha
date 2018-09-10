//IMPORT mTest.cs
//IMPORT mSPO_Interpreter.cs

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
public static class mSPO_Interpreter_Test {
	
	//================================================================================
	private static mVM_Data.tData
	Mul(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchPair(out var Arg2_, out var _));
		mStd.Assert(Arg2_.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 * IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_Interpreter),
		mTest.Test(
			"Run1",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§DEF x... = (a => (k .* a))
							§DEF y = (.x 5)
							
							§EXPORT y
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run2",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§DEF y = (.(a => (k .* a)) 5)
							
							§EXPORT y
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run3",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
								k
							)
							
							§EXPORT .(a => (k .* a)) 5
						",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						aDebugStream
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test(
			"Run4",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
							)
							
							§EXPORT .((a, _, _) => (2 .* a)) (3, 5, 7)
						",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						aDebugStream
					),
					mVM_Data.Int(6)
				);
			}
		),
		mTest.Test(
			"Run5",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (
								...*...
							)
							
							§EXPORT .(
								a => §IF a MATCH {
									(_, a, _) => (a .* a)
									_ => 0
								}
							) (1, 2, 3)
						",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						aDebugStream
					),
					mVM_Data.Int(4)
				);
			}
		),
		mTest.Test(
			"Run6",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT (...*... € [[§INT, §INT] => §INT])
							
							§DEF EmptyStack = #Empty ()
							
							§DEF tStack... = t => [§RECURSIV tStack_ [[#Empty []] | [#Stack [t, tStack_]]]]
							
							§DEF Push...To... = (
								Head € t
								Tail € (.tStack t)
							) => #Stack (Head, Tail)
							
							§RECURSIV Map...With... = (Stack, Func...) => §IF Stack MATCH {
								(#Stack (Head, Tail)) => .Push (.Func Head) To (.Map Tail With Func...)
								(#Empty ()) => EmptyStack
							}
							
							§EXPORT .Map (.Push 3 To (.Push 2 To (.Push 1 To EmptyStack))) With (x => x .* x)
						",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						aDebugStream
					),
					mVM_Data.Prefix(
						"_Stack...",
						mVM_Data.Tuple(
							mVM_Data.Int(9),
							mVM_Data.Prefix(
								"_Stack...",
								mVM_Data.Tuple(
									mVM_Data.Int(4),
									mVM_Data.Prefix(
										"_Stack...",
										mVM_Data.Tuple(
											mVM_Data.Int(1),
											mVM_Data.Prefix("_Empty...", mVM_Data.Empty())
										)
									)
								)
							)
						)
					)
				);
			}
		),
		mTest.Test(
			"Run7",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT ()
							
							§EXPORT §IF (#Bla ()) MATCH {
								(#Blub ()) => 1
								(#Bla ()) => 2
								(_) => 3
							}
						",
						mVM_Data.Empty(),
						aDebugStream
					),
					mVM_Data.Int(2)
				);
			}
		),
		mTest.Test(
			"Run8",
			aDebugStream => {
				mStd.AssertEq(
					mSPO_Interpreter.Run(
						@"
							§IMPORT ()
							
							§DEF {C: X, A: Y} = {A: 1, B: 2, C: 3}
							
							§EXPORT (X, Y)
						",
						mVM_Data.Empty(),
						aDebugStream
					),
					mVM_Data.Tuple(mVM_Data.Int(3), mVM_Data.Int(1))
				);
			}
		)
	);
	
	#if NUNIT
	[xTestCase("Run1")]
	[xTestCase("Run2")]
	[xTestCase("Run3")]
	[xTestCase("Run4")]
	[xTestCase("Run5")]
	[xTestCase("Run6")]
	[xTestCase("Run7")]
	[xTestCase("Run8")]
	public static void _(tText a) {
		mStd.AssertEq(
			Test.Run(System.Console.WriteLine, mStream.Stream(a)),
			mTest.tResult.OK
		);
	}
	#endif
}
