// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT mVM_Data
// IMPORT mSPO_Interpreter

public static class
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
	
	private static readonly mVM_Type.tType
	MulType = mVM_Type.Proc(
		mVM_Type.Empty(),
		mVM_Type.Tuple(
			[
				mVM_Type.Int(),
				mVM_Type.Int()
			]
		),
		mVM_Type.Int()
	);
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mSPO_Interpreter),
		[
			mTest.Tests("PatternMatching",
				mStream.Stream(
					[
						(
							mStd.FileLine(),
							"(§DEF A, §DEF B, _) = (1, 2, 3)",
							"(A, B)",
							mVM_Data.Tuple([mVM_Data.Int(1), mVM_Data.Int(2)])
						),
						(
							mStd.FileLine(),
							"(§DEF A, §DEF B, _) = (1, 2, (3, 4))",
							"(A, B)",
							mVM_Data.Tuple([mVM_Data.Int(1), mVM_Data.Int(2)])
						),
						(
							mStd.FileLine(),
							"((§DEF A, §DEF B), §DEF C) = ((1, 2), 3)",
							"(A, B, C)",
							mVM_Data.Tuple([mVM_Data.Int(1), mVM_Data.Int(2), mVM_Data.Int(3)])
						),
						(
							mStd.FileLine(),
							"(§DEF A, §DEF B, §DEF C) = (1, 2, 3)",
							"(A, B, C)",
							mVM_Data.Tuple([mVM_Data.Int(1), mVM_Data.Int(2), mVM_Data.Int(3)])
						),
						(
							mStd.FileLine(),
							"{C: §DEF X, A: §DEF Y} = {A: 1, B: 2, C: 3}",
							"(X, Y)",
							mVM_Data.Tuple([mVM_Data.Int(3), mVM_Data.Int(1)])
						),
						(
							mStd.FileLine(),
							"(#Bla §DEF A) = #Bla 1",
							"(A)",
							mVM_Data.Int(1)
						),
					]
				).Map(
					a => {
						var Groups = System.Text.RegularExpressions.Regex.Match(a.Item1, @"^(.*)\:(\d+)$").Groups;
						var FilePath = Groups[1].Value;
						var LineNr = tInt32.Parse(Groups[2].Value);
						
						return mTest.Test(a.Item2,
							aDebugStream => {
								mAssert.AreEquals(
									mSPO_Interpreter.Run(
										$"""
										§IMPORT ()
										
										{a.Item2}
										
										§EXPORT {a.Item3}
										""",
										"",
										(mVM_Data.Empty(), mVM_Type.Empty()),
										_ => aDebugStream(_())
									).Data,
									a.Item4
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
							(
								mVM_Data.Tuple(
									[
										mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
										mVM_Data.Int(2)
									]
								),
								mVM_Type.Tuple(
									[
										MulType,
										mVM_Type.Int()
									]
								)
							),
							_ => aDebugStream(_())
						).Data,
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
							(
								mVM_Data.Tuple(
									[
										mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
										mVM_Data.Int(2)
									]
								),
								mVM_Type.Tuple(
									[
										MulType,
										mVM_Type.Int()
									]
								)
							),
							_ => aDebugStream(_())
						).Data,
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
							(
								mVM_Data.Tuple(
									[
										mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
										mVM_Data.Int(2)
									]
								),
								mVM_Type.Tuple(
									[
										MulType,
										mVM_Type.Int()
									]
								)
							),
							_ => aDebugStream(_())
						).Data,
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
							(
								mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
								MulType
							),
							_ => aDebugStream(_())
						).Data,
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
							(
								mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
								MulType
							),
							_ => aDebugStream(_())
						).Data,
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
							
							§RECURSIVE {
								§DEF tStack... = [[t] => [[] | [t, §REF [.tStack t]]]]
							}
							
							§DEF Push...To... = [
								§DEF t
							] <=> (
								§DEF aHead € t
								§DEF aTail € [§REF [.tStack t]]
							) => (aHead, aTail)
							
							§RECURSIVE {
								§DEF Map...With... = [
									§DEF tIn,
									§DEF tOut
								] <=> (
									§DEF aStack € [§REF tStack[tIn]],
									§DEF aFunc... € [tIn => tOut]
								) => §IF aStack MATCH {
									(§DEF Head, §DEF Tail) => .Push (.aFunc Head) To (.Map Tail With aFunc...)
									() => ()
								}
							}
							
							§EXPORT .Map (.Push 3 To §< Push 2 To §< Push 1 To ()) With (§DEF x => x .* x)
							""",
							"",
							mVM_Data.ExternProc(Mul, mVM_Data.Empty()),
							_ => aDebugStream(_())
						),
						mVM_Data.Tuple(
							mVM_Data.Int(9),
							mVM_Data.Ref(
								mVM_Data.Tuple(
									mVM_Data.Int(4),
									mVM_Data.Ref(
										mVM_Data.Tuple(
											mVM_Data.Int(1),
											mVM_Data.Empty()
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
							(mVM_Data.Empty(), mVM_Type.Empty()),
							_ => aDebugStream(_())
						).Data,
						mVM_Data.Int(2)
					);
				}
			),
		]
	);
}
