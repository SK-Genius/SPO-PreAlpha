#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mStream.cs
#:ref mVM_Type.cs
#:ref mVM_Data.cs
#:ref mVM.cs

public static class
mVM_Tests {
	private static
	mVM_Data.tData
	Add (
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		mAssert.IsTrue(aArg.IsPair(out var Arg1, out var Arg2));
		mAssert.IsTrue(Arg1.IsInt(out var IntArg1));
		mAssert.IsTrue(Arg2.IsInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 + IntArg2);
	}
	
	private static tBool
	Accepts(
		mVM_Data.tData aValue,
		mVM_Type.tType aType
	) {
		var Contract = mVM_Type.Sig(mVM_Type.Free(mVM_Type.Type()), aType);
		var Definition = new mVM_Data.tProcDef<mStd.tEmpty>(
			mVM_Type.Proc(
				mVM_Type.Empty(),
				mVM_Type.Empty(),
				mVM_Type.Proc(mVM_Type.Empty(), Contract, mVM_Type.True())
			)
		);
		Definition.Constants = [mVM_Data.TypeExpression(Contract)];
		Definition.TryAsSig(mStd.cEmpty, mVM_Data.cArgReg, 0);
		Definition.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, mVM_Data.cTrueReg);
		var Result = mVM_Data.Empty();
		mVM.Run<mStd.tEmpty>(
			mVM_Data.Proc(Definition, mVM_Data.Empty()),
			mVM_Data.Empty(),
			mVM_Data.Sig(Contract, mVM_Data.TypeExpression(mVM_Type.Int()), aValue),
			Result,
			_ => "",
			_ => {}
		);
		return Result.IsBool(out var Accepted) && Accepted;
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM),
		[
			mTest.Test("Runtime patterns accept matching values and reject mismatches",
				aDebug => {
					foreach (var Case in mStream.Stream<(
						tText Name, mVM_Data.tData Value, mVM_Type.tType Type, tBool Accepted
					)>(
						("Empty", mVM_Data.Empty(), mVM_Type.Empty(), true),
						("Nonempty", mVM_Data.Int(1), mVM_Type.Empty(), false),
						("Integer", mVM_Data.Int(1), mVM_Type.Int(), true),
						("Wrong integer", mVM_Data.Bool(true), mVM_Type.Int(), false),
						("True", mVM_Data.Bool(true), mVM_Type.True(), true),
						("False", mVM_Data.Bool(false), mVM_Type.False(), true),
						("Wrong boolean", mVM_Data.Bool(false), mVM_Type.True(), false),
						("Any", mVM_Data.Var(mVM_Data.Int(1)), mVM_Type.Any(), true),
						("Free", mVM_Data.Int(1), mVM_Type.Free(mVM_Type.Type()), true),
						("Type", mVM_Data.TypeExpression(mVM_Type.Int()), mVM_Type.Type(), true),
						("Wrong type", mVM_Data.Int(1), mVM_Type.Type(), false),
						("Union empty first", mVM_Data.Int(1), mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Int()), true),
						("Union empty last", mVM_Data.Empty(), mVM_Type.Set(mVM_Type.Int(), mVM_Type.Empty()), true),
						("Wrong union", mVM_Data.Bool(true), mVM_Type.Set(mVM_Type.Int(), mVM_Type.Empty()), false),
						("Prefix", mVM_Data.Prefix("Value", mVM_Data.Int(1)), mVM_Type.Prefix("Value", mVM_Type.Int()), true),
						("Wrong prefix", mVM_Data.Prefix("Other", mVM_Data.Int(1)), mVM_Type.Prefix("Value", mVM_Type.Int()), false),
						("Record", mVM_Data.Record([("Value", mVM_Data.Int(1))]), mVM_Type.Record([("Value", mVM_Type.Int())]), true),
						("Wrong record", mVM_Data.Record([("Value", mVM_Data.Bool(true))]), mVM_Type.Record([("Value", mVM_Type.Int())]), false),
						("Variable", mVM_Data.Var(mVM_Data.Int(1)), mVM_Type.Var(mVM_Type.Int()), true),
						("Wrong variable", mVM_Data.Var(mVM_Data.Bool(true)), mVM_Type.Var(mVM_Type.Int()), false),
						("Generic", mVM_Data.Int(1), mVM_Type.Generic(mVM_Type.Free(mVM_Type.Type()), mVM_Type.Int()), true)
					)) {
						mAssert.IsTrue(Accepts(Case.Value, Case.Type) == Case.Accepted, Case.Name);
					}
				}
			),
			mTest.Test("Recursive snoc list patterns match long lists and reject a wrong tail",
				aDebug => {
					var Head = mVM_Type.Free(mVM_Type.Type());
					var ListType = mVM_Type.Recursive(
						Head,
						mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Head, mVM_Type.Int()))
					);
					var Valid = mVM_Data.Empty();
					var Invalid = mVM_Data.Bool(false);
					for (var Index = 0; Index < 8192; Index += 1) {
						var Item = mVM_Data.Int(Index);
						Valid = mVM_Data.Pair(Valid, Item);
						Invalid = mVM_Data.Pair(Invalid, Item);
					}
					mAssert.IsTrue(Accepts(Valid, ListType));
					mAssert.IsFalse(Accepts(Invalid, ListType));
				}
			),
			mTest.Test("Runtime pattern cycle detection still checks other pair fields",
				aDebug => {
					var Head = mVM_Type.Free(mVM_Type.Type());
					var ListType = mVM_Type.Recursive(
						Head,
						mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Head, mVM_Type.Int()))
					);
					var Cycle = mVM_Data.Pair(mVM_Data.Empty(), mVM_Data.Int(7));
					Cycle._Value = mVM_Data.Pair(Cycle, mVM_Data.Int(7))._Value;
					mAssert.IsTrue(Accepts(Cycle, ListType));
					var WrongCycle = mVM_Data.Pair(mVM_Data.Empty(), mVM_Data.Bool(true));
					WrongCycle._Value = mVM_Data.Pair(WrongCycle, mVM_Data.Bool(true))._Value;
					mAssert.IsFalse(Accepts(WrongCycle, ListType));
					var CellHead = mVM_Type.Free(mVM_Type.Type());
					var CellType = mVM_Type.Recursive(CellHead, mVM_Type.Var(CellHead));
					var Cell = mVM_Data.Var(mVM_Data.Empty());
					Cell._Value = mVM_Data.Var(Cell)._Value;
					mAssert.IsTrue(Accepts(Cell, CellType));
					mAssert.IsFalse(Accepts(mVM_Data.Var(mVM_Data.Int(7)), CellType));
				}
			),
			mTest.Test("ExternDef",
				aDebugStream => {
					#if MY_TRACE_VM
						var TraceOut = mStd.Action(
							(mStd.tFunc<tText> aLazyText) => aDebugStream(aLazyText())
						);
					#else
						var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
					#endif
					
					var Env = mVM_Data.ExternDef(Add);
					
					var Proc1 = new mVM_Data.tProcDef<mStd.tEmpty>(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Tuple([mVM_Type.Int(), mVM_Type.Int()]),
								mVM_Type.Int()
							),
							mVM_Type.Proc(
								mVM_Type.Empty(),
								mVM_Type.Int(),
								mVM_Type.Int()
							)
						)
					);
					var Reg1 = Proc1.Pair(mStd.cEmpty, mVM_Data.cOneReg, mVM_Data.cOneReg);
					var Reg2 = Proc1.Call(mStd.cEmpty, mVM_Data.cEnvReg, mVM_Data.cEmptyReg);
					var Reg3 = Proc1.Call(mStd.cEmpty, Reg2, Reg1);
					Proc1.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, Reg3);
					
					var Res = mVM_Data.Empty();
					mVM.Run<mStd.tEmpty>(
						mVM_Data.Proc(Proc1, Env),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						Res,
						__ => "" + __,
						TraceOut
					);
					mAssert.AreEquals(Res, mVM_Data.Int(2));
				}
			),
			mTest.Test(
				"Nested calls share the trace callback and emit balanced frame markers",
				aDebug => {
					var Lines = new System.Collections.Generic.List<tText>();
					var Trace = mStd.Action<mStd.tFunc<tText>>(aLine => Lines.Add(aLine()));
					var Native = mVM_Data.ExternProc((aEnv, aObject, aArgument, aTrace) => {
						mAssert.IsTrue(ReferenceEquals(Trace, aTrace));
						aTrace(() => "native trace");
						return aArgument;
					}, mVM_Data.Empty());
					
					static mVM_Data.tProcDef<mStd.tEmpty> Definition() => new(
						mVM_Type.Proc(
							mVM_Type.Empty(), mVM_Type.Any(),
							mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Any(), mVM_Type.Int())
						)
					);
					
					var Method = Definition();
					Method.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, mVM_Data.cArgReg);
					var Child = Definition();
					var NativeReg = Child.First(mStd.cEmpty, mVM_Data.cEnvReg);
					Child.Call(mStd.cEmpty, NativeReg, mVM_Data.cOneReg);
					var NativeMethod = Child.Pair(mStd.cEmpty, mVM_Data.cEmptyReg, NativeReg);
					Child.Exec(mStd.cEmpty, NativeMethod, mVM_Data.cOneReg);
					var MethodReg = Child.Second(mStd.cEmpty, mVM_Data.cEnvReg);
					var BoundMethod = Child.Pair(mStd.cEmpty, mVM_Data.cEmptyReg, MethodReg);
					var Returned = Child.Exec(mStd.cEmpty, BoundMethod, mVM_Data.cOneReg);
					Child.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, Returned);
					var Root = Definition();
					var Called = Root.Call(mStd.cEmpty, mVM_Data.cEnvReg, mVM_Data.cEmptyReg);
					Root.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, Called);
					var Result = mVM_Data.Empty();
					mVM.Run<mStd.tEmpty>(
						mVM_Data.Proc(Root, mVM_Data.Proc(Child, mVM_Data.Pair(
							Native, mVM_Data.Proc(Method, mVM_Data.Empty())
						))),
						mVM_Data.Empty(), mVM_Data.Empty(), Result, _ => "", Trace
					);
					mAssert.AreEquals(mVM_Data.Int(1), Result);
					
					var Depth = 0;
					var Entries = 0;
					var MaxDepth = 0;
					var NativeLines = 0;
					foreach (var Line in Lines) {
						mAssert.IsFalse(Line.StartsWith('\t'));
						if (Line == new tText('>', 40)) {
							Depth += 1;
							Entries += 1;
							MaxDepth = System.Math.Max(MaxDepth, Depth);
						} else if (Line == new tText('<', 40)) {
							Depth -= 1;
							mAssert.IsTrue(Depth >= 0);
						} else if (Line == "native trace") {
							mAssert.AreEquals(2, Depth);
							NativeLines += 1;
						}
					}
					mAssert.AreEquals(0, Depth);
					mAssert.AreEquals(3, Entries);
					mAssert.AreEquals(3, MaxDepth);
					mAssert.AreEquals(2, NativeLines);
				}
			),
			mTest.Test(
				"A failed pattern closes its frame in the trace",
				aDebug => {
					var Lines = new System.Collections.Generic.List<tText>();
					var Definition = new mVM_Data.tProcDef<mStd.tEmpty>(
						mVM_Type.Proc(
							mVM_Type.Empty(), mVM_Type.Empty(),
							mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Empty())
						)
					);
					Definition.TryAsPair(mStd.cEmpty, mVM_Data.cArgReg);
					var Result = mVM_Data.Empty();
					mVM.Run<mStd.tEmpty>(
						mVM_Data.Proc(Definition, mVM_Data.Empty()),
						mVM_Data.Empty(), mVM_Data.Int(1), Result, _ => "", aLine => Lines.Add(aLine())
					);
					mAssert.IsTrue(Result.IsEmpty());
					mAssert.AreEquals(new tText('>', 40), Lines[0]);
					mAssert.AreEquals(new tText('<', 40), Lines[^1]);
				}
			)
		]
	);
}