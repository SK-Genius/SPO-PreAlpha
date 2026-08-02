#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mError.cs
#:ref Common/mPerf.cs
#:ref Common/mMaybe.cs
#:ref Common/mMath.cs
#:ref Common/mStream.cs
#:ref Common/mAny.cs
#:ref Common/mArrayList.cs
#:ref Common/mTreeMap.cs
#:ref mVM_Data.cs
#:ref mIL_AST.cs
#:ref mIL_GenerateOpcodes.cs

public static class
mVM {
	public sealed class
	tCallStack<tPos> {
		internal mMaybe.tMaybe<tCallStack<tPos>> _Parent;
		internal mArrayList.tArrayList<mVM_Data.tData> _Regs = default!;
		internal mVM_Data.tProcDef<tPos> _ProcDef = default!;
		internal tNat32 _CodePointer = 0;
		internal mStd.tAction<mStd.tFunc<tText>> _TraceOut = default!;
	}
	
	public static mMaybe.tMaybe<tCallStack<tPos>>
	NewCallStack<tPos>(
		mMaybe.tMaybe<tCallStack<tPos>> aParent,
		mVM_Data.tProcDef<tPos> aProcDef,
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		var FreeType = aProcDef.TypeFree(default);
		
		var Result = new tCallStack<tPos> {
			_TraceOut = aTraceOut,
			_Parent = aParent,
			_ProcDef = aProcDef,
			_Regs = mArrayList.List(
				mVM_Data.Empty(),
				mVM_Data.Int(1),
				mVM_Data.Bool(false),
				mVM_Data.Bool(true),
				//mVM_Data.Proc(aProcDef, aEnv),
				mVM_Data.TypeEmpty(),
				mVM_Data.TypeInt(),
				mVM_Data.TypeType(),
				aEnv,
				aObj,
				aArg,
				aRes
			),
		};
		aTraceOut(() => "______________________________________");
		aTraceOut(() => " 0 := EMPTY");
		aTraceOut(() => " 1 := 1");
		aTraceOut(() => " 2 := FALSE");
		aTraceOut(() => " 3 := TRUE");
		//aTraceOut(() => " 4 := SELF");
		aTraceOut(() => " 4 := EMPTY_TYPE");
		aTraceOut(() => " 5 := INT_TYPE");
		aTraceOut(() => " 6 := TYPE_TYPE");
		aTraceOut(() => " 7 := ENV  |  " + aEnv.ToText(20));
		aTraceOut(() => " 8 := OBJ  |  " + aObj.ToText(20));
		aTraceOut(() => " 9 := ARG  |  " + aArg.ToText(20));
		aTraceOut(() => "10 := RES");
		
		return Result;
	}
	
	private static mMaybe.tMaybe<mVM_Type.tType>
	TryGetFuncArgType(
		mVM_Data.tData aProc
	) {
		if (
			!aProc.IsProc(out var Def, out _) ||
			!Def.DefType.IsProc(out _, out _, out var FuncType)
		) {
			return mStd.cEmpty;
		}
		
		while (FuncType.IsGeneric(out _, out var BodyType)) {
			FuncType = BodyType;
		}
		
		return (
			FuncType.IsProc(out _, out var ArgType, out _)
			? ArgType
			: mStd.cEmpty
		);
	}
	
	private static tBool
	Matches(
		this mVM_Data.tData aData,
		mVM_Type.tType aType,
		System.Collections.Generic.HashSet<(tNat64 Data, tNat64 Type)>? aVisited = null
	) {
		if (aType.IsTypeVariable(out _, out _)) {
			return true;
		} else if (aType.IsAny()) {
			return true;
		}
		
		aVisited ??= [];
		if (!aVisited.Add((aData._DebugId, aType.DebugId))) {
			return true;
		} else if (aType.IsSet(out var Type1, out var Type2)) {
			return aData.Matches(Type1, aVisited) || aData.Matches(Type2, aVisited);
		} else if (aType.IsRecursive(out var HeadType, out var BodyType)) {
			return aData.Matches(BodyType.Substitute(HeadType, aType), aVisited);
		} else if (aType.IsInterface(out _, out BodyType) || aType.IsGeneric(out _, out BodyType)) {
			return aData.Matches(BodyType, aVisited);
		} else if (aType.IsSig(out _, out _, out _)) {
			return aData.IsSig(out _, out _);
		} else {
			return aType.Kind switch {
				mVM_Type.tKind.Empty => aData.IsEmpty(),
				mVM_Type.tKind.True => aData.IsBool(out var Bool) && Bool,
				mVM_Type.tKind.False => aData.IsBool(out var Bool) && !Bool,
				mVM_Type.tKind.Int => aData.IsInt(out _),
				mVM_Type.tKind.Type => aData._DataType is mVM_Data.tDataType.Type,
				mVM_Type.tKind.Pair => (
					aData.IsPair(out var First, out var Second) &&
					aType.IsPair(out var FirstType, out var SecondType) &&
					First.Matches(FirstType, aVisited) &&
					Second.Matches(SecondType, aVisited)
				),
				mVM_Type.tKind.Prefix => (
					aType.IsPrefix(out var Prefix, out var InnerType) &&
					aData.IsPrefix(Prefix, out var Inner) &&
					Inner.Matches(InnerType, aVisited)
				),
				mVM_Type.tKind.Record => (
					aType.IsRecord(out var FieldTypes) &&
					aData.IsRecord(out var Fields) &&
					FieldTypes.ToStream().All(
						__ => (
							Fields.TryGet(__.Key.PrefixHash()).IsSome(out var Field) &&
							Field.Matches(__.Value, aVisited)
						)
					)
				),
				mVM_Type.tKind.Proc => (
					aData.IsProc(out var Def, out _) &&
					Def.DefType.IsProc(out _, out _, out var FuncType) &&
					FuncType.IsSubType(aType).Match(out _, out _)
				),
				mVM_Type.tKind.Var => (
					aType.IsVar(out var ValueType) &&
					aData.IsVar(out var Value) &&
					Value.Matches(ValueType, aVisited)
				),
				_ => false,
			};
		}
	}
	
	private static mVM_Data.tData
	RunFunc<tPos>(
		mVM_Data.tData aProc,
		mVM_Data.tData aArg,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		if (aProc.IsProc<tPos>(out var Def, out var Env)) {
			var Res = mVM_Data.Empty();
			Run(
				mVM_Data.Proc(Def, Env),
				mVM_Data.Empty(),
				aArg,
				Res,
				aPosToText,
				aTraceOut
			);
			return Res;
		} else if (aProc.IsExternProc(out var ExternDef, out var ExternEnv)) {
			return ExternDef(ExternEnv, mVM_Data.Empty(), aArg, aTraceOut);
		} else {
			throw mError.Error("expected proc but is: " + aProc._DataType);
		}
	}
	
	public static mMaybe.tMaybe<tCallStack<tPos>>
	Step<tPos>(
		this tCallStack<tPos> aCallStack,
		mStd.tFunc<tPos, tText> aPosToText
	) {
		var (OpCode, Arg1, Arg2, DebugId) = aCallStack._ProcDef.Commands.Get(aCallStack._CodePointer);
		tText CommandLine() => $">>>   {aCallStack._Regs.Size:#0} := {OpCode} {Arg1} {Arg2} // CommandDebugId:{DebugId} // {aCallStack._ProcDef.PosList.Get(aCallStack._CodePointer)}";
		aCallStack._TraceOut(CommandLine);
		aCallStack._TraceOut(() => $"{mStd.NewDebugId()}");
		aCallStack._CodePointer += 1;
		
		switch (OpCode) {
			case mVM_Data.tOpCode.NewInt: {
				aCallStack._Regs.Push(mVM_Data.Int((tInt32)Arg1));
				break;
			}
			case mVM_Data.tOpCode.And: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.IsBool(out var Bool1));
				mAssert.IsTrue(BoolData2.IsBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 && Bool2));
				break;
			}
			case mVM_Data.tOpCode.Or: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.IsBool(out var Bool1));
				mAssert.IsTrue(BoolData2.IsBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 || Bool2));
				break;
			}
			case mVM_Data.tOpCode.XOr: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.IsBool(out var Bool1));
				mAssert.IsTrue(BoolData2.IsBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 ^ Bool2));
				break;
			}
			case mVM_Data.tOpCode.IntsAreEq: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1), () => $"{IntData1.ToText(100)}//DebugId:{IntData1._DebugId}");
				mAssert.IsTrue(IntData2.IsInt(out var Int2), () => $"{IntData2.ToText(100)}//DebugId:{IntData2._DebugId}");
				aCallStack._Regs.Push(mVM_Data.Bool(Int1 == Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsComp: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(mVM_Data.Int(Diff.Sign()));
				break;
			}
			case mVM_Data.tOpCode.IntsAdd: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 + Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsSub: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 - Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsMul: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 * Int2));
				break;
			}
			case mVM_Data.tOpCode.IntsDiv: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.IsInt(out var Int1));
				mAssert.IsTrue(IntData2.IsInt(out var Int2));
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						mVM_Data.Int(Int1 / Int2),
						mVM_Data.Int(Int1 % Int2)
					)
				);
				break;
			}
			case mVM_Data.tOpCode.NewPair: {
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			case mVM_Data.tOpCode.First: {
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg1).IsPair(
						out var Var1,
						out var Var2
					),
					$"{CommandLine()} # expect pair but is {aCallStack._Regs.Get(Arg1).ToText(20)}"
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			case mVM_Data.tOpCode.Second: {
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg1).IsPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var2);
				break;
			}
			case mVM_Data.tOpCode.NewSig: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsPair(out var Head, out var Body));
				aCallStack._Regs.Push(mVM_Data.Sig(Head, Body));
				break;
			}
			case mVM_Data.tOpCode.SigHead: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsSig(out var Head, out _));
				aCallStack._Regs.Push(Head);
				break;
			}
			case mVM_Data.tOpCode.SigBody: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsSig(out _, out var Body));
				aCallStack._Regs.Push(Body);
				break;
			}
			case mVM_Data.tOpCode.AddPrefix: {
				aCallStack._Regs.Push(mVM_Data.Prefix(Arg1, aCallStack._Regs.Get(Arg2)));
				break;
			}
			case mVM_Data.tOpCode.DelPrefix: {
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg2).IsPrefix(out var Prefix, out var Data_)
				);
				mAssert.AreEquals(Prefix, Arg1, (a1, a2) => a1 == a2);
				aCallStack._Regs.Push(Data_);
				break;
			}
			case mVM_Data.tOpCode.TryRemovePrefixFrom: {
				var Data = aCallStack._Regs.Get(Arg2);
				if (Data.IsPrefix(out var PrefixId, out var Inner) && PrefixId == Arg1) {
					aCallStack._Regs.Push(Inner);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.AddField: {
				aCallStack._Regs.Push(
					mVM_Data.Record(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			case mVM_Data.tOpCode.GetField: {
				var Arg = aCallStack._Regs.Get(Arg1);
				mAssert.IsTrue(Arg.IsRecord(out var Fields));
				aCallStack._Regs.Push(
					Fields.TryGet(Arg2).ElseFail(() => $"{aCallStack._CodePointer}: {OpCode} {mVM_Data.gHashToPrefix.TryGet(Arg2).ElseUse("" + Arg2)} {Arg.ToText(3)}").ElseThrow()
				);
				break;
			}
			case mVM_Data.tOpCode.Assert: {
				if (aCallStack._Regs.Get(Arg1).IsBool(out var Bool) && Bool) {
					mAssert.IsTrue(aCallStack._Regs.Get(Arg2).IsBool(out Bool) && Bool);
				}
				break;
			}
			case mVM_Data.tOpCode.VarDef: {
				aCallStack._Regs.Push(mVM_Data.Var(aCallStack._Regs.Get(Arg1)));
				break;
			}
			case mVM_Data.tOpCode.VarSet: {
				aCallStack._Regs.Get(Arg1)._Value = mAny.Any(aCallStack._Regs.Get(Arg2));
				break;
			}
			case mVM_Data.tOpCode.VarGet: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1)._Value.Is(out mVM_Data.tData X));
				aCallStack._Regs.Push(X);
				break;
			}
			case mVM_Data.tOpCode.DefRecProcs: {
				var Func = aCallStack._Regs.Get(Arg1);
				var Env = aCallStack._Regs.Get(Arg2);
				
				//mAssert.IsTrue(
				//	Func._DataType is mVM_Data.tDataType.Proc,
				//	() => $"{mVM_Data.tDataType.Proc} != {Func._DataType}"
				//);
				
				var RecProcList = mStream.Stream<mVM_Data.tData>();
				
				//mAssert.Fail();
				var Count = 1; // TODO: count rec procs
				
				var RecProcs = mVM_Data.Empty(); // first place holder
				if (Count is 1) {
					RecProcList = mStream.Stream(RecProcs, RecProcList);
				} else {
					for (var I = 0; I < Count; I += 1) {
						var Temp = mVM_Data.Empty(); // next placeholder
						RecProcs = mVM_Data.Pair(
							RecProcs,
							Temp
						);
						RecProcList = mStream.Stream(Temp, RecProcList);
					}
				}
				aCallStack._Regs.Push(RecProcs);
				
				
				mVM_Data.tData Res;
				switch (0) {
					case 0 when Func.IsExternDef(out var ExternDef): {
						Res = mVM_Data.Empty();
						Run(
							mVM_Data.ExternProc(ExternDef, Env),
							mVM_Data.Empty(),
							RecProcs,
							Res,
							aPosToText,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							}
						);
						break;
					}
					case 0 when Func.IsExternProc(out var ExternDef, out var Env_): {
						Res = mVM_Data.Empty();
						Run(
							ExternDef(
								Env_,
								mVM_Data.Empty(),
								Env,
								aTraceLine => {
									aCallStack._TraceOut(() => "\t" + aTraceLine());
								}
							),
							mVM_Data.Empty(),
							RecProcs,
							Res,
							aPosToText,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							}
						);
						break;
					}
					case 0 when Func.IsDef<tPos>(out var Def): {
						Res = mVM_Data.Empty();
						Run(
							mVM_Data.Proc(Def, Env),
							mVM_Data.Empty(),
							RecProcs,
							Res,
							aPosToText,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							}
						);
						break;
					}
					case 0 when Func.IsProc<tPos>(out var Def_, out var Env_): {
						throw mError.Error("need Env as Argument");
						// TODO:
						//Res = mVM_Data.Empty();
						//Run(
						//	mVM_Data.Proc(Def_, Env_),
						//	mVM_Data.Empty(),
						//	RecProcs,
						//	Res,
						//	aPosToText,
						//	aTraceLine => {
						//		aCallStack._TraceOut(() => "\t" + aTraceLine());
						//	}
						//);
						//break;
						
						//Res = mVM_Data.Empty();
						//aCallStack._Regs.Push(Res);
						//return NewCallStack(
						//	aCallStack,
						//	Def_,
						//	Env,
						//	mVM_Data.Empty(),
						//	Arg,
						//	Res,
						//	aTraceLine => aCallStack._TraceOut(() => "\t" + aTraceLine())
						//);
					}
					default: {
						throw mError.Error("impossible: " + Func._DataType);
					}
				}
				
				if (Count is 1) {
					RecProcs._DataType = Res._DataType;
					RecProcs._Value = Res._Value;
					RecProcs._Fields = Res._Fields;
					RecProcs._IsMutable = Res._IsMutable;
				} else {
					var Pair = Res;
					for (var I = 0; I < Count; I += 1) {
						mAssert.IsTrue(Res.IsPair(out var RecProc, out Pair));
						mAssert.IsTrue(RecProcList.Is(out var RecProc_, out RecProcList));
						RecProc_._Value = RecProc._Value;
					}
					mAssert.IsTrue(Pair.IsEmpty());
				}
				break;
			}
			case mVM_Data.tOpCode.CallFunc: {
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg = aCallStack._Regs.Get(Arg2);
				
				switch (0) {
					case 0 when Proc.IsExternDef(out var ExternDef): {
						aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
						break;
					}
					case 0 when Proc.IsExternProc(out var ExternDef, out var Env): {
						aCallStack._Regs.Push(
							ExternDef(
								Env,
								mVM_Data.Empty(),
								Arg,
								aTraceLine => {
									aCallStack._TraceOut(() => "\t" + aTraceLine());
								}
							)
						);
						break;
					}
					case 0 when Proc.IsDef<tPos>(out var Def): {
						aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
						break;
					}
					case 0 when Proc.IsProc<tPos>(out var Def_, out var Env): {
						var Res = mVM_Data.Empty();
						aCallStack._Regs.Push(Res);
						return NewCallStack(
							aCallStack,
							Def_,
							Env,
							mVM_Data.Empty(),
							Arg,
							Res,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							}
						);
					}
					default: {
						throw mError.Error("expected proc or def but is: " + Proc._DataType);
					}
				}
				break;
			}
			case mVM_Data.tOpCode.CallProc: {
				var Proc_ = aCallStack._Regs.Get(Arg1);
				var Arg = aCallStack._Regs.Get(Arg2);
				
				mAssert.IsTrue(Proc_.IsPair(out var Obj, out var Proc));
				
				switch (0) {
					case 0 when Proc.IsExternDef(out var ExternDef): {
						aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
						break;
					}
					case 0 when Proc.IsExternProc(out var ExternDef, out var Env): {
						aCallStack._Regs.Push(
							ExternDef(
								Env,
								Obj,
								Arg,
								aTraceLine => {
									aCallStack._TraceOut(() => "\t" + aTraceLine());
								}
							)
						);
						break;
					}
					case 0 when Proc.IsDef<tPos>(out var Def): {
						aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
						break;
					}
					case 0 when Proc.IsProc<tPos>(out var Def, out var Env): {
						var Res = mVM_Data.Empty();
						aCallStack._Regs.Push(Res);
						return NewCallStack(
							aCallStack,
							Def,
							Env,
							Obj,
							Arg,
							Res,
							aTraceLine => {
								aCallStack._TraceOut(() => "\t" + aTraceLine());
							}
						);
					}
					default: {
						throw mError.Error("impossible: " + Proc._DataType);
					}
				}
				break;
			}
			case mVM_Data.tOpCode.ReturnIf: {
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).IsBool(out var Cond), CommandLine());
				if (Cond) {
					var Res = aCallStack._Regs.Get(Arg2);
					var Des = aCallStack._Regs.Get(mVM_Data.cResReg);
					Des._DataType = Res._DataType;
					Des._Value = Res._Value;
					Des._Fields = Res._Fields;
					Des._IsMutable = Res._IsMutable;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.ReturnIfNotEmpty: {
				var Res = aCallStack._Regs.Get(Arg1);
				if (Res._DataType is not mVM_Data.tDataType.Empty) {
					var Des = aCallStack._Regs.Get(mVM_Data.cResReg);
					Des._DataType = Res._DataType;
					Des._Value = Res._Value;
					Des._Fields = Res._Fields;
					Des._IsMutable = Res._IsMutable;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryReturn: {
				var ProcOrPair = aCallStack._Regs.Get(Arg1);
				
				var (Proc, Guard) = (
					ProcOrPair.IsPair(out var Proc_, out var Guard_)
					? (Proc_, Guard_)
					: (ProcOrPair, mMaybe.None<mVM_Data.tData>())
				);
				
				var Arg = aCallStack._Regs.Get(Arg2);
				if (
					TryGetFuncArgType(Proc).IsSome(out var ArgType) &&
					Arg.Matches(ArgType) &&
					(
						!Guard.IsSome(out var GuardProc) ||
						(
							RunFunc(GuardProc, Arg, aPosToText, aCallStack._TraceOut).IsBool(out var GuardResult) &&
							GuardResult
						)
					)
				) {
					aCallStack._Regs.Push(RunFunc(Proc, Arg, aPosToText, aCallStack._TraceOut));
				} else {
					aCallStack._Regs.Push(mVM_Data.Empty());
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsPair: {
				var Arg = aCallStack._Regs.Get(Arg1);
				if (Arg.IsPair(out _, out _)) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsSig: {
				var Arg = aCallStack._Regs.Get(Arg1);
				var MatchesHead = Arg2 == tNat32.MaxValue;
				if (
					!MatchesHead &&
					Arg.IsSig(out var Head, out _) &&
					Head._DataType is mVM_Data.tDataType.Type &&
					Head._Value.Is(out mVM_Type.tType HeadType) &&
					aCallStack._ProcDef.TypeConstants.Get(Arg2) is var ExpectedHead
				) {
					MatchesHead = HeadType.IsSubType(ExpectedHead).Match(out _, out _) &&
						ExpectedHead.IsSubType(HeadType).Match(out _, out _);
				}
				if (Arg.IsSig(out _, out _) && MatchesHead) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsRecord: {
				var Arg = aCallStack._Regs.Get(Arg1);
				if (Arg.IsRecord(out var _)) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsVar: {
				var Arg = aCallStack._Regs.Get(Arg1);
				if (Arg.IsVar(out var _)) {
					aCallStack._Regs.Push(Arg);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TypeFree: {
				// create a fresh free type variable
				var Kind = mVM_Type.Type();
				if (Arg1 != tNat32.MaxValue) {
					var KindData = aCallStack._Regs.Get(Arg1);
					mAssert.AreEquals(KindData._DataType, mVM_Data.tDataType.Type);
					mAssert.IsTrue(KindData._Value.Is(out Kind));
				}
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.TypeVariable("", Kind))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypeSig: {
				var BinderData = aCallStack._Regs.Get(Arg1);
				var BodyData = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(BinderData._DataType, mVM_Data.tDataType.Type);
				mAssert.AreEquals(BodyData._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(BinderData._Value.Is(out mVM_Type.tType Binder));
				mAssert.IsTrue(BodyData._Value.Is(out mVM_Type.tType BodyType));
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.Sig(Binder, Binder.KindOf(), BodyType))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypePair: {
				var Type1 = aCallStack._Regs.Get(Arg1);
				var Type2 = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(Type1._DataType, mVM_Data.tDataType.Type);
				mAssert.AreEquals(Type2._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(Type1._Value.Is(out mVM_Type.tType T1));
				mAssert.IsTrue(Type2._Value.Is(out mVM_Type.tType T2));
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.Pair(T1, T2))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypePrefix: {
				var InnerData = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(InnerData._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(InnerData._Value.Is(out mVM_Type.tType Inner));
				aCallStack._Regs.Push(new mVM_Data.tData {
					_DataType = mVM_Data.tDataType.Type,
					_IsMutable = false,
					_Value = mAny.Any(mVM_Type.Prefix(Arg1.ToString(), Inner))
				});
				break;
			}
			case mVM_Data.tOpCode.TypeRecord: {
				var RecordData = aCallStack._Regs.Get(Arg1);
				var FieldData = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(RecordData._DataType, mVM_Data.tDataType.Type);
				mAssert.AreEquals(FieldData._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(RecordData._Value.Is(out mVM_Type.tType Record));
				mAssert.IsTrue(FieldData._Value.Is(out mVM_Type.tType Field));
				aCallStack._Regs.Push(new mVM_Data.tData {
					_DataType = mVM_Data.tDataType.Type,
					_IsMutable = false,
					_Value = mAny.Any(mVM_Type.Record(Record, Field))
				});
				break;
			}
			case mVM_Data.tOpCode.TypeVar: {
				var InnerData = aCallStack._Regs.Get(Arg1);
				mAssert.AreEquals(InnerData._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(InnerData._Value.Is(out mVM_Type.tType Inner));
				aCallStack._Regs.Push(new mVM_Data.tData {
					_DataType = mVM_Data.tDataType.Type,
					_IsMutable = false,
					_Value = mAny.Any(mVM_Type.Var(Inner))
				});
				break;
			}
			case mVM_Data.tOpCode.TypeSet: {
				var Type1 = aCallStack._Regs.Get(Arg1);
				var Type2 = aCallStack._Regs.Get(Arg2);
				mVM_Type.tType T1;
				if (Type1.IsBool(out var Bool1)) {
					T1 = Bool1 ? mVM_Type.True() : mVM_Type.False();
				} else {
					mAssert.IsTrue(Type1._Value.Is(out T1));
				}
				mVM_Type.tType T2;
				if (Type2.IsBool(out var Bool2)) {
					T2 = Bool2 ? mVM_Type.True() : mVM_Type.False();
				} else {
					mAssert.IsTrue(Type2._Value.Is(out T2));
				}
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.Set(T1, T2))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypeRecursive: {
				var HeadType = aCallStack._Regs.Get(Arg1);
				var BodyType = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(HeadType._DataType, mVM_Data.tDataType.Type);
				mAssert.AreEquals(BodyType._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(HeadType._Value.Is(out mVM_Type.tType Head));
				mAssert.IsTrue(BodyType._Value.Is(out mVM_Type.tType Body));
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.Recursive(Head, Body))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypeInterface: {
				var HeadData = aCallStack._Regs.Get(Arg1);
				var BodyData = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(HeadData._Value.Is(out mVM_Type.tType Head));
				mAssert.IsTrue(BodyData._Value.Is(out mVM_Type.tType Body));
				aCallStack._Regs.Push(new mVM_Data.tData {
					_DataType = mVM_Data.tDataType.Type,
					_IsMutable = false,
					_Value = mAny.Any(mVM_Type.Interface(Head, Body))
				});
				break;
			}
			case mVM_Data.tOpCode.TypeFunc: {
				var ArgData = aCallStack._Regs.Get(Arg1);
				var ResultData = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(ArgData._Value.Is(out mVM_Type.tType ArgType));
				mAssert.IsTrue(ResultData._Value.Is(out mVM_Type.tType ResultType));
				aCallStack._Regs.Push(new mVM_Data.tData {
					_DataType = mVM_Data.tDataType.Type,
					_IsMutable = false,
					_Value = mAny.Any(mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResultType))
				});
				break;
			}
			case mVM_Data.tOpCode.TypeMeth: {
				var ObjData = aCallStack._Regs.Get(Arg1);
				var FuncData = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(ObjData._Value.Is(out mVM_Type.tType ObjType));
				mAssert.IsTrue(FuncData._Value.Is(out mVM_Type.tType FuncType));
				mAssert.IsTrue(FuncType.IsProc(out _, out var ArgType, out var ResultType));
				aCallStack._Regs.Push(new mVM_Data.tData {
					_DataType = mVM_Data.tDataType.Type,
					_IsMutable = false,
					_Value = mAny.Any(mVM_Type.Proc(ObjType, ArgType, ResultType))
				});
				break;
			}
			case mVM_Data.tOpCode.TryAsEmpty: {
				var Data = aCallStack._Regs.Get(Arg1);
				if (Data.IsEmpty()) {
					aCallStack._Regs.Push(Data);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsBool: {
				var Data = aCallStack._Regs.Get(Arg1);
				if (Data.IsBool(out _)) {
					aCallStack._Regs.Push(Data);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TryAsInt: {
				var Data = aCallStack._Regs.Get(Arg1);
				if (Data.IsInt(out _)) {
					aCallStack._Regs.Push(Data);
				} else {
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case mVM_Data.tOpCode.TypeGeneric: {
				var HeadType = aCallStack._Regs.Get(Arg1);
				var BodyType = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(HeadType._DataType, mVM_Data.tDataType.Type);
				mAssert.AreEquals(BodyType._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(HeadType._Value.Is(out mVM_Type.tType Head));
				mAssert.IsTrue(BodyType._Value.Is(out mVM_Type.tType Body));
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.Generic(Head, Body))
					}
				);
				break;
			}
			case mVM_Data.tOpCode.TypeGenericApply: {
				var ConstructorData = aCallStack._Regs.Get(Arg1);
				var ArgumentData = aCallStack._Regs.Get(Arg2);
				mAssert.AreEquals(ConstructorData._DataType, mVM_Data.tDataType.Type);
				mAssert.AreEquals(ArgumentData._DataType, mVM_Data.tDataType.Type);
				mAssert.IsTrue(ConstructorData._Value.Is(out mVM_Type.tType Constructor));
				mAssert.IsTrue(ArgumentData._Value.Is(out mVM_Type.tType Argument));
				aCallStack._Regs.Push(
					new mVM_Data.tData {
						_DataType = mVM_Data.tDataType.Type,
						_IsMutable = false,
						_Value = mAny.Any(mVM_Type.TypeApply(Constructor, Argument))
					}
				);
				break;
			}
			// TODO: missing IL Command
			// - Create Process
			// - Send Message
			
			default: {
				throw mError.Error("TODO " + OpCode);
			}
		}
		aCallStack._TraceOut(
			() => $@"    \ {aCallStack._Regs.Size - 1} = {aCallStack._Regs.Get(aCallStack._Regs.Size - 1).ToText(20)}"
		);
		return aCallStack;
	}
	
	public static mVM_Data.tData
	GetModuleFactory<tPos>(
		mStream.tStream<mVM_Data.tProcDef<tPos>> aDefs
	) {
		var Env = mVM_Data.Empty();
		mAssert.IsTrue(aDefs.Is(out var LastDef, out aDefs));
		foreach (var Def in aDefs) {
			Env = mVM_Data.Pair(Env, mVM_Data.Def(LastDef));
			LastDef = Def;
		}
		
		return mVM_Data.Proc(LastDef, Env);
	}
	
	public static void
	Run<tPos>(
		mVM_Data.tData aProc,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		using var _ = mPerf.Measure();
		switch (0) {
			case 0 when aProc.IsProc<tPos>(out var Def, out var Env): {
				var CallStack = NewCallStack(mStd.cEmpty, Def, Env, aObj, aArg, aRes, aTraceOut);
				while (CallStack.IsSome(out var CallStack_)) {
					CallStack = CallStack_.Step(aPosToText);
				}
				break;
			}
			case 0 when aProc.IsExternProc(out var ExternDef, out var Env): {
				var Res = ExternDef(Env, aObj, aArg, aTraceOut);
				aRes._DataType = Res._DataType;
				aRes._Value = Res._Value;
				break;
			}
			default: {
				throw mError.Error($"tPos ({typeof(tPos)}) does not match with the tPos of aProc");
			}
		}
	}
	
	public static (mVM_Data.tData Data, mVM_Type.tType Type)
	Run<tPos>(
		mIL_AST.tModule<tPos> aModule,
		(mVM_Data.tData Data, mVM_Type.tType Type) aImport,
		mStd.tFunc<tPos, tText> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) {
		var (VMModule, ModuleMap) = mIL_GenerateOpcodes.GenerateOpcodes(aModule, aTrace);
		var Res = mVM_Data.Empty();
		var Defs = VMModule.Reverse().Skip(1).Reverse();
		
		var DefTuple = Defs.Take(2).Count() switch {
			0 => mVM_Data.Empty(),
			1 => mVM_Data.Def(Defs.TryFirst().AssertNotEmpty()),
			_ => Defs.Reduce(
				mVM_Data.Empty(),
				(aTuple, aDef) => mVM_Data.Pair(
					aTuple,
					mVM_Data.Def(aDef)
				)
			),
		};
		var InitProc = VMModule.TryLast().AssertNotEmpty();
		
		#if MY_TRACE_VM
			var TraceOut = aTrace;
		#else
			var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => { });
		#endif
		
		Run(
			mVM_Data.Proc(InitProc, DefTuple),
			mVM_Data.Empty(),
			aImport.Data,
			Res,
			aPosToText,
			TraceOut
		);
		
		mAssert.IsTrue(
			VMModule.TryLast(
			).AssertNotEmpty(
			).DefType.IsProc(
				out _,
				out _,
				out var FuncType
			)
		);
		mAssert.IsTrue(
			FuncType.IsProc(out _, out _, out var ResType),
			$"{mStd.FileLine()}: {FuncType.ToText()}"
		);
		return (Res, ResType);
	}
	
//	public static mVM_Data.tData
//	Run(
//		tText aSourceCode,
//		tText aId,
//		mVM_Data.tData aImport,
//		mStd.tAction<mStd.tFunc<tText>> aTrace
//	) => Run(
//		mIL_Parser.Module.ParseText(aSourceCode, aId, aTrace),
//		aImport,
//		SpanToText,
//		aTrace
//	);
}
