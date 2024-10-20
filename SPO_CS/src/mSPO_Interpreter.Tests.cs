﻿public static class
mSPO_Interpreter_Tests {
	private static mVM_Data.tData
	Mul(
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aArg.IsPair(out var Arg1_, out var Arg2));
		mAssert.IsTrue(Arg1_.IsPair(out var Empty, out var Arg1));
		mAssert.IsTrue(Empty.IsEmpty());
		mAssert.IsTrue(Arg1.IsInt(out var IntArg1));
		mAssert.IsTrue(Arg2.IsInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 * IntArg2);
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_Interpreter),
		mTest.Test("Run1",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT (
							§DEF ...*... € [[§INT, §INT] => §INT]
							§DEF k € §INT
						)
						
						§DEF x... = §DEF a € §INT => (k .* a)
						§DEF y = (.x 5)
						
						§EXPORT y
						""",
						"",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						_ => aDebugStream(_())
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test("Run2",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT (
							§DEF ...*... € [[§INT, §INT] => §INT]
							§DEF k € §INT
						)
						
						§DEF y = (.(§DEF a € §INT => (k .* a)) 5)
						
						§EXPORT y
						""",
						"",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						_ => aDebugStream(_())
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test("Run3",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT (
							§DEF ...*... € [[§INT, §INT] => §INT]
							§DEF k € §INT
						)
						
						§EXPORT .(§DEF a € §INT => (k .* a)) 5
						""",
						"",
						mVM_Data.Tuple(
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							mVM_Data.Int(2)
						),
						_ => aDebugStream(_())
					),
					mVM_Data.Int(10)
				);
			}
		),
		mTest.Test("Run4",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT (
							§DEF ...*... € [[§INT, §INT] => §INT]
						)
						
						§EXPORT .((§DEF a € §INT, _ € §INT, _ € §INT) => (2 .* a)) (3, 5, 7)
						""",
						"",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						_ => aDebugStream(_())
					),
					mVM_Data.Int(6)
				);
			}
		),
		mTest.Test("Run5",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT (
							§DEF ...*... € [[§INT, §INT] => §INT]
						)
						
						§EXPORT .(
							§DEF a € [§INT, §INT] => §IF a MATCH {
								(§DEF b, _) => (b .* b)
								_ => 0
							}
						) (2, 3)
						""",
						"",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						_ => aDebugStream(_())
					),
					mVM_Data.Int(4),
					null,
					(a) => mVM_Data.ToText(a, 20)
				);
			}
		),
		#if !true // TODO: implement generic operator (for example: <=>)
		mTest.Test("Run6",
			aDebugStream => {
				mAssert.AssertEq(
					mSPO_Interpreter.Run(
						"""
						§IMPORT (§DEF ...*... € [[§INT, §INT] => §INT])
						
						§DEF EmptyStack = #Empty ()
						
						§DEF tStack... = [[t] =>> [§RECURSIVE tStack_ = [[#Empty []] | [#Stack [t, tStack_]]]]]
						
						§RECURSIVE §TYPE tStack... = t => [#Empty | #Stack[t, tStack[t]]]	
						
						§DEF Push...To... = [
							t
						] <=> (
							§DEF Head € t
							§DEF Tail € [.tStack t]
						) => #Stack (Head, Tail)
						
						§RECURSIVE {
							§DEF Map...With... = (§DEF Stack, §DEF Func...) => §IF Stack MATCH {
								(#Stack (§DEF Head, §DEF Tail)) => .Push (.Func Head) To (.Map Tail With Func...)
								(#Empty ()) => EmptyStack
							}
						}
						
						§EXPORT .Map (.Push 3 To (.Push 2 To (.Push 1 To EmptyStack))) With (§DEF x => x .* x)
						""",
						"",
						mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
						_ => aDebugStream(_())
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
		#endif
		mTest.Test("Run7",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT ()
						
						§DEF X € [[#Bla []] | [#Blub []]] = #Bla ()
						
						§EXPORT §IF X MATCH {
							(#Blub ()) => 1
							(#Bla ()) => 2
							(_) => 3
						}
						""",
						"",
						mVM_Data.Empty(),
						_ => aDebugStream(_())
					),
					mVM_Data.Int(2)
				);
			}
		),
		mTest.Test("Run8",
			aDebugStream => {
				mAssert.AreEquals(
					mSPO_Interpreter.Run(
						"""
						§IMPORT ()
						
						{C: §DEF X, A: §DEF Y} = {A: 1, B: 2, C: 3}
						
						§EXPORT (X, Y)
						""",
						"",
						mVM_Data.Empty(),
						_ => aDebugStream(_())
					),
					mVM_Data.Tuple(mVM_Data.Int(3), mVM_Data.Int(1))
				);
			}
		)
	);
}
