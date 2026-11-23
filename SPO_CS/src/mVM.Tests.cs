#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
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
				mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Any(), mVM_Type.True())
			)
		);
		
		Definition.Constants = [mVM_Data.TypeExpression(Contract)];
		Definition.TryAsSig(mStd.cEmpty, mVM_Data.cArgReg, 0);
		Definition.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, mVM_Data.cTrueReg);
		
		var Result = mVM.Call<mStd.tEmpty>(
			mVM_Data.Proc(Definition, mVM_Data.Empty()),
			mVM_Data.Empty(),
			mVM_Data.Sig(Contract, mVM_Data.TypeExpression(mVM_Type.Int()), aValue),
			_ => "",
			_ => {}
		);
		
		return Result.IsBool(out var Accepted) && Accepted;
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mVM),
		[
			mTest.Test(
				"Public calls check arguments and receivers before entering the VM",
				aDebug => {
					var Definition = new mVM_Data.tProcDef<mStd.tEmpty>(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Proc(mVM_Type.Int(), mVM_Type.Int(), mVM_Type.Int())
						)
					);
					
					Definition.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, mVM_Data.cOneReg);
					var Function = mVM_Data.Proc(Definition, mVM_Data.Empty());
					var Traces = 0;
					var Trace = mStd.Action<mStd.tFunc<tText>>(_ => Traces += 1);
					
					foreach (
						var (Object, Argument, Role) in mStream.Stream(
							(mVM_Data.Int(1), mVM_Data.Bool(true), "Argument"),
							(mVM_Data.Bool(false), mVM_Data.Int(1), "Empfänger")
						)
					) {
						try {
							mVM.Call<mStd.tEmpty>(Function, Object, Argument, _ => "", Trace);
							throw new System.InvalidOperationException("Invalid call was accepted.");
						} catch (mError.tError Error) {
							mAssert.IsTrue(Error.Message.Contains(Role));
							mAssert.IsTrue(Error.Message.Contains("Erwarteter Typ:\n  §INT"));
							mAssert.IsTrue(Error.Message.Contains("Aktueller Typ:\n  §"));
						}
					}
					
					var Receiver = mVM_Type.Free(mVM_Type.Type());
					
					var WrongReceiver = mVM_Data.Interface(
						mVM_Type.Interface(
							Receiver,
							mVM_Type.Record([("Read", mVM_Type.Proc(Receiver, mVM_Type.Int(), mVM_Type.Int()))])
						),
						mVM_Data.Bool(false),
						mVM_Data.Record([("Read", Function)])
					);
					
					mAssert.ThrowsError(
						() => mVM.CallMethod<mStd.tEmpty>(WrongReceiver, "Read", mVM_Data.Int(1), _ => "", Trace)
					);
					mAssert.AreEquals(0, Traces);
					var Result = mVM.Call<mStd.tEmpty>(
						Function,
						mVM_Data.Int(1),
						mVM_Data.Int(2),
						_ => "",
						Trace
					);
					
					mAssert.AreEquals(mVM_Data.Int(1), Result);
					mAssert.IsTrue(Traces > 0);
				}
			),
			mTest.Test(
				"Public compiled interface calls preserve the original argument",
				aDebug => {
					var Implementation = new mVM_Data.tProcDef<mStd.tEmpty>(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Proc(mVM_Type.Int(), mVM_Type.Any(), mVM_Type.Any())
						)
					);
					
					Implementation.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, mVM_Data.cArgReg);
					
					var Receiver = mVM_Type.Free(mVM_Type.Type());
					var ArgumentType = mVM_Type.Record([("Value", mVM_Type.Int())]);
					
					// A static record view does not change the value passed through the implementation.
					var Contract = mVM_Type.Interface(
						Receiver,
						mVM_Type.Record([("Echo", mVM_Type.Proc(Receiver, ArgumentType, mVM_Type.Any()))])
					);
					
					var Capability = mVM_Data.Interface(
						Contract,
						mVM_Data.Int(1),
						mVM_Data.Record([("Echo", mVM_Data.Proc(Implementation, mVM_Data.Empty()))])
					);
					
					var Argument = mVM_Data.Record([("Value", mVM_Data.Int(7)), ("Extra", mVM_Data.Int(9))]);
					
					var Result = mVM.CallMethod<mStd.tEmpty>(
						Capability,
						"Echo",
						Argument,
						_ => "",
						__ => aDebug(__())
					);
					
					mAssert.IsTrue(Result.IsRecord(out var Fields));
					mAssert.IsTrue(Fields.TryGet("Value".PrefixHash()).IsSome(out var Value));
					mAssert.AreEquals(mVM_Data.Int(7), Value);
					mAssert.IsTrue(Fields.TryGet("Extra".PrefixHash()).IsSome(out _));
					mAssert.AreEquals(Argument, Result);
					mAssert.IsTrue(Argument.IsRecord(out var OriginalFields));
					mAssert.AreEquals(2u, OriginalFields.ToStream().Count());
				}
			),
			mTest.Test(
				"Public interface calls pass values to the bound native implementation",
				aDebug => {
					var Calls = 0;
					
					var Native = mVM_Data.ExternProc(
						(aEnvironment, aObject, aArgument, aTrace) => {
							Calls += 1;
							mAssert.AreEquals(mVM_Data.Int(7), aObject);
							return aArgument;
						},
						mVM_Data.Empty()
					);
					
					var Receiver = mVM_Type.Free(mVM_Type.Type());
					
					var Contract = mVM_Type.Interface(
						Receiver,
						mVM_Type.Record([("Read", mVM_Type.Proc(Receiver, mVM_Type.Int(), mVM_Type.Int()))])
					);
					
					var Capability = mVM_Data.Interface(
						Contract,
						mVM_Data.Int(7),
						mVM_Data.Record([("Read", Native)])
					);
					
					mAssert.ThrowsError(
						() => mVM.CallMethod<mStd.tEmpty>(
							Capability,
							"Read",
							mVM_Data.Bool(true),
							_ => "",
							_ => {}
						)
					);
					
					mAssert.AreEquals(0, Calls);
					
					// An untyped native procedure must not bypass its interface contract.
					mAssert.ThrowsError(
						() => mVM.Call<mStd.tEmpty>(
							Native,
							mVM_Data.Int(7),
							mVM_Data.Bool(true),
							_ => "",
							_ => {}
						)
					);
					
					mAssert.AreEquals(0, Calls);
					
					var Result = mVM.CallMethod<mStd.tEmpty>(
						Capability,
						"Read",
						mVM_Data.Int(42),
						_ => "",
						_ => {}
					);
					
					mAssert.AreEquals(mVM_Data.Int(42), Result);
					mAssert.AreEquals(1, Calls);
				}
			),
			mTest.Test(
				"Runtime patterns accept matching values and reject mismatches",
				aDebug => {
					foreach (
						var Case in mStream.Stream<
							(
								tText Name,
								mVM_Data.tData Value,
								mVM_Type.tType Type,
								tBool Accepted
							)
						>(
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
						)
					) {
						mAssert.IsTrue(Accepts(Case.Value, Case.Type) == Case.Accepted, Case.Name);
					}
				}
			),
			mTest.Test(
				"Recursive snoc list patterns match long lists and reject a wrong tail",
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
			mTest.Test(
				"Recursive pattern cycle detection still checks other pair fields",
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
			mTest.Test(
				"ExternDef",
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
								mVM_Type.Empty(),
								mVM_Type.Int()
							)
						)
					);
					
					var Reg1 = Proc1.Pair(mStd.cEmpty, mVM_Data.cOneReg, mVM_Data.cOneReg);
					var Reg2 = Proc1.Call(mStd.cEmpty, mVM_Data.cEnvReg, mVM_Data.cEmptyReg);
					var Reg3 = Proc1.Call(mStd.cEmpty, Reg2, Reg1);
					Proc1.ReturnIf(mStd.cEmpty, mVM_Data.cTrueReg, Reg3);
					
					var Res = mVM.Call<mStd.tEmpty>(
						mVM_Data.Proc(Proc1, Env),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
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
					
					var Native = mVM_Data.ExternProc(
						(aEnv, aObject, aArgument, aTrace) => {
							mAssert.IsTrue(ReferenceEquals(Trace, aTrace));
							aTrace(() => "native trace");
							return aArgument;
						},
						mVM_Data.Empty()
					);
					
					static mVM_Data.tProcDef<mStd.tEmpty> Definition() => new(
						mVM_Type.Proc(
							mVM_Type.Empty(),
							mVM_Type.Any(),
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
					
					var Result = mVM.Call<mStd.tEmpty>(
						mVM_Data.Proc(
							Root,
							mVM_Data.Proc(
								Child,
								mVM_Data.Pair(
									Native,
									mVM_Data.Proc(Method, mVM_Data.Empty())
								)
							)
						),
						mVM_Data.Empty(),
						mVM_Data.Empty(),
						_ => "",
						Trace
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
							mVM_Type.Empty(),
							mVM_Type.Empty(),
							mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Empty())
						)
					);
					
					Definition.TryAsPair(mStd.cEmpty, mVM_Data.cArgReg);
					
					var Result = mVM.Call<mStd.tEmpty>(
						mVM_Data.Proc(Definition, mVM_Data.Empty()),
						mVM_Data.Empty(),
						mVM_Data.Int(1),
						_ => "",
						aLine => Lines.Add(aLine())
					);
					
					mAssert.IsTrue(Result.IsEmpty());
					mAssert.AreEquals(new tText('>', 40), Lines[0]);
					mAssert.AreEquals(new tText('<', 40), Lines[^1]);
				}
			)
		]
	);
}