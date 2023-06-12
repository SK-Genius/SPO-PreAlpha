﻿//IMPORT mVM_Data.cs
//IMPORT mArrayList.cs
//IMPORT mPerf.cs

#nullable enable

public static class
mVM {
	
	public sealed class
	tCallStack<tPos> {
		internal mMaybe.tMaybe<tCallStack<tPos>> _Parent;
		internal mArrayList.tArrayList<mVM_Data.tData> _Regs = default!;
		internal mVM_Data.tProcDef<tPos> _ProcDef = default!;
		internal tInt32 _CodePointer = 0;
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
		var Result = new tCallStack<tPos> {
			_TraceOut = aTraceOut,
			_Parent = aParent,
			_ProcDef = aProcDef,
			_Regs = mArrayList.List(
				mVM_Data.Empty(),
				mVM_Data.Int(1),
				mVM_Data.Bool(false),
				mVM_Data.Bool(true),
				mVM_Data.TypeEmpty(),
				mVM_Data.TypeBool(),
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
		aTraceOut(() => " 4 := EMPTY_TYPE");
		aTraceOut(() => " 5 := BOOL_TYPE");
		aTraceOut(() => " 6 := INT_TYPE");
		aTraceOut(() => " 7 := TYPE_TYPE");
		aTraceOut(() => " 8 := ENV  |  " + aEnv.ToText(20));
		aTraceOut(() => " 9 := OBJ  |  " + aObj.ToText(20));
		aTraceOut(() => "10 := ARG  |  " + aArg.ToText(20));
		aTraceOut(() => "11 := RES");
		
		return Result;
	}
	
	public static mMaybe.tMaybe<tCallStack<tPos>>
	Step<tPos>(
		this tCallStack<tPos> aCallStack,
		mStd.tFunc<tText, tPos> aPosToText
	) {
		var (OpCode, Arg1, Arg2) = aCallStack._ProcDef.Commands.Get(aCallStack._CodePointer);
		mStd.tFunc<tText> CommandLine = () => $"{aCallStack._Regs.Size():#0} := {OpCode} {Arg1} {Arg2} // {aPosToText(aCallStack._ProcDef.PosList.Get(aCallStack._CodePointer))}";
		aCallStack._TraceOut(CommandLine);
		aCallStack._CodePointer += 1;
		
		switch (OpCode) {
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.NewInt: {
			//--------------------------------------------------------------------------------
				aCallStack._Regs.Push(mVM_Data.Int(Arg1));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsBool: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Bool));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.And: {
			//--------------------------------------------------------------------------------
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.MatchBool(out var Bool1));
				mAssert.IsTrue(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 && Bool2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Or: {
			//--------------------------------------------------------------------------------
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.MatchBool(out var Bool1));
				mAssert.IsTrue(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 || Bool2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.XOr: {
			//--------------------------------------------------------------------------------
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(BoolData1.MatchBool(out var Bool1));
				mAssert.IsTrue(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 ^ Bool2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsInt: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Int));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsAreEq: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.MatchInt(out var Int1));
				mAssert.IsTrue(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Bool(Int1 == Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsComp: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.MatchInt(out var Int1));
				mAssert.IsTrue(IntData2.MatchInt(out var Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(mVM_Data.Int(Diff.Sign()));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsAdd: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.MatchInt(out var Int1));
				mAssert.IsTrue(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 + Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsSub: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.MatchInt(out var Int1));
				mAssert.IsTrue(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 - Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsMul: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.MatchInt(out var Int1));
				mAssert.IsTrue(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 * Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsDiv: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mAssert.IsTrue(IntData1.MatchInt(out var Int1));
				mAssert.IsTrue(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Pair(mVM_Data.Int(Int1 / Int2), mVM_Data.Int(Int1 % Int2)));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsPair: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Pair));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.NewPair: {
			//--------------------------------------------------------------------------------
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.First: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg1).MatchPair(
						out var Var1,
						out var Var2
					),
					$"{CommandLine()} # expect pair but is {aCallStack._Regs.Get(Arg1).ToText(20)}"
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Second: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg1).MatchPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var2);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsPrefix: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Prefix));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.AddPrefix: {
			//--------------------------------------------------------------------------------
				aCallStack._Regs.Push(mVM_Data.Prefix(Arg1, aCallStack._Regs.Get(Arg2)));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.DelPrefix: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg2).MatchPrefix(Arg1, out var Data_)
				);
				aCallStack._Regs.Push(Data_);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.HasPrefix: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(
					aCallStack._Regs.Get(Arg2).MatchPrefix(out var PrefixId, out var Data)
				);
				aCallStack._Regs.Push(mVM_Data.Bool(PrefixId.Equals(Arg1)));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsRecord: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Record));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.ExtendRec: {
			//--------------------------------------------------------------------------------
				aCallStack._Regs.Push(
					mVM_Data.Record(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.DivideRec: {
			//--------------------------------------------------------------------------------
				var Arg = aCallStack._Regs.Get(Arg1);
				mAssert.IsTrue(Arg.MatchRecord(out var Record, out var Prefix));
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						Record,
						Prefix
					)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Assert: {
			//--------------------------------------------------------------------------------
				if (aCallStack._Regs.Get(Arg1).MatchBool(out var Bool) && Bool) {
					mAssert.IsTrue(aCallStack._Regs.Get(Arg2).MatchBool(out Bool) && Bool);
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsVar: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Var));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.VarDef: {
			//--------------------------------------------------------------------------------
				aCallStack._Regs.Push(mVM_Data.Var(aCallStack._Regs.Get(Arg1)));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.VarSet: {
			//--------------------------------------------------------------------------------
				aCallStack._Regs.Get(Arg1)._Value = mAny.Any(aCallStack._Regs.Get(Arg2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.VarGet: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1)._Value.Match(out mVM_Data.tData X));
				aCallStack._Regs.Push(X);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.CallFunc: {
			//--------------------------------------------------------------------------------
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				switch (0) {
					case 0 when Proc.MatchExternDef(out var ExternDef): {
						aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
						break;
					}
					case 0 when Proc.MatchExternProc(out var ExternDef, out var Env): {
						aCallStack._Regs.Push(ExternDef(Env, mVM_Data.Empty(), Arg, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine())));
						break;
					}
					case 0 when Proc.MatchDef<tPos>(out var Def): {
						aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
						break;
					}
					case 0 when Proc.MatchProc<tPos>(out var Def_, out var Env): {
						var Res = mVM_Data.Empty();
						aCallStack._Regs.Push(Res);
						return NewCallStack(
							aCallStack,
							Def_,
							Env,
							mVM_Data.Empty(),
							Arg,
							Res,
							aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine())
						);
					}
					default: {
						throw mError.Error("impossible");
					}
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.CallProc: {
			//--------------------------------------------------------------------------------
				var Proc_ = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				mAssert.IsTrue(Proc_.MatchPair(out var Obj, out var Proc));
				
				switch (0) {
					case 0 when Proc.MatchExternDef(out var ExternDef): {
						aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
						break;
					}
					case 0 when Proc.MatchExternProc(out var ExternDef, out var Env): {
						aCallStack._Regs.Push(ExternDef(Env, Obj, Arg, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine())));
						break;
					}
					case 0 when Proc.MatchDef<tPos>(out var Def): {
						aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
						break;
					}
					case 0 when Proc.MatchProc<tPos>(out var Def, out var Env): {
						var Res = mVM_Data.Empty();
						aCallStack._Regs.Push(Res);
						return NewCallStack(aCallStack, Def, Env, Obj, Arg, Res, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine()));
					}
					default: {
						throw mError.Error("impossible");
					}
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.ReturnIf: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					var Src = aCallStack._Regs.Get(Arg2);
					var Des = aCallStack._Regs.Get(mVM_Data.cResReg);
					Des._DataType = Src._DataType;
					Des._Value = Src._Value;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.ContinueIf: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					aCallStack._Regs = mArrayList.List(
						mVM_Data.Empty(),
						mVM_Data.Int(1),
						mVM_Data.Bool(false),
						mVM_Data.Bool(true),
						mVM_Data.TypeEmpty(),
						mVM_Data.TypeBool(),
						mVM_Data.TypeInt(),
						mVM_Data.TypeType(),
						aCallStack._Regs.Get(mVM_Data.cEnvReg),
						aCallStack._Regs.Get(mVM_Data.cObjReg),
						aCallStack._Regs.Get(Arg2),
						aCallStack._Regs.Get(mVM_Data.cResReg)
					);
					aCallStack._CodePointer = 0;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.TailCallIf: {
			//--------------------------------------------------------------------------------
				mAssert.IsTrue(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					var CallerArgPair = aCallStack._Regs.Get(Arg2);
					mAssert.IsTrue(CallerArgPair.MatchPair(out var Proc, out var Arg));
					mVM_Data.tData Res;
					switch (0) {
						case 0 when Proc.MatchExternDef(out var ExternDef): {
							Res = mVM_Data.ExternProc(ExternDef, Arg);
							break;
						}
						case 0 when Proc.MatchExternProc(out var ExternDef, out var Env): {
							Res = ExternDef(Env, mVM_Data.Empty(), Arg, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine()));
							break;
						}
						case 0 when Proc.MatchDef<tPos>(out var Def): {
							Res = mVM_Data.Proc(Def, Arg);
							break;
						}
						case 0 when Proc.MatchProc<tPos>(out var Def, out var Env): {
							return NewCallStack(
								aCallStack._Parent,
								Def,
								Env,
								mVM_Data.Empty(),
								Arg,
								aCallStack._Regs.Get(mVM_Data.cResReg),
								aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine())
							);
						}
						default: {
							throw mError.Error("impossible");
						}
					}
					var Des = aCallStack._Regs.Get(mVM_Data.cResReg);
					Des._DataType = Res._DataType;
					Des._Value = Res._Value;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IsType: {
			//--------------------------------------------------------------------------------
				var Data = aCallStack._Regs.Get(Arg1);
				aCallStack._Regs.Push(mVM_Data.Bool(Data._DataType == mVM_Data.tDataType.Type));
				break;
			}
			
			// TODO: missing IL Command
			// - Create Process
			// - Send Message
			
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mError.Error("TODO");
			}
		}
		aCallStack._TraceOut(() => $@"    \ {aCallStack._Regs.Size()-1} = {aCallStack._Regs.Get(aCallStack._Regs.Size()-1).ToText(20)}");
		return aCallStack;
	}
	
	public static mVM_Data.tData
	GetModuleFactory<tPos>(
		mStream.tStream<mVM_Data.tProcDef<tPos>>? aDefs
	) {
		var Env = mVM_Data.Empty();
		mAssert.IsTrue(aDefs.Match(out var LastDef, out aDefs));
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
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	) {
		using var _ = mPerf.Measure();
		switch (0) {
			case 0 when aProc.MatchProc<tPos>(out var Def, out var Env): {
				var CallStack = NewCallStack(mStd.cEmpty, Def, Env, aObj, aArg, aRes, aTraceOut);
				while (CallStack.IsSome(out var CallStack_)) {
					CallStack = CallStack_.Step(aPosToText);
				}
				break;
			}
			case 0 when aProc.MatchExternProc(out var ExternDef, out var Env): {
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
	
	public static mVM_Data.tData
	Run<tPos>(
		mIL_AST.tModule<tPos> aModule,
		mVM_Data.tData aImport,
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aTrace
	) => Run(mIL_GenerateOpcodes.GenerateOpcodes(aModule, aTrace), aImport, aPosToText, aTrace);
	
	public static mVM_Data.tData
	Run<tPos>(
		(mStream.tStream<mVM_Data.tProcDef<tPos>>?, mTreeMap.tTree<tText, tInt32>) aModule,
		mVM_Data.tData aImport,
		mStd.tFunc<tText, tPos> aPosToText,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var (VMModule, ModuleMap) = aModule;
		var Res = mVM_Data.Empty();
		var Defs = VMModule.Skip(1).Reverse();
		
		var DefTuple = Defs.Take(2).Count() switch {
			0 => mVM_Data.Empty(),
			1 => mVM_Data.Def(Defs.TryFirst().ElseThrow()),
			_ => Defs.Reduce(
				mVM_Data.Empty(),
				(aTuple, aDef) => mVM_Data.Pair(
					aTuple,
					mVM_Data.Def(aDef)
				)
			),
		};
		var InitProc = VMModule.TryFirst().ElseThrow();
		
#if MY_TRACE
		var TraceOut = aDebugStream;
#else
		var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => { });
#endif
		
		mVM.Run(
			mVM_Data.Proc(InitProc, DefTuple),
			mVM_Data.Empty(),
			aImport,
			Res,
			aPosToText,
			TraceOut
		);
		
		return Res;
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
