//IMPORT mVM_Data.cs
//IMPORT mArrayList.cs
//IMPORT mPerf.cs

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

public static class mVM {
	
	public sealed class tCallStack<tPos> {
		internal tCallStack<tPos> _Parent;
		internal mArrayList.tArrayList<mVM_Data.tData> _Regs;
		internal mVM_Data.tProcDef<tPos> _ProcDef;
		internal tInt32 _CodePointer = 0;
		internal mVM_Data.tData _Obj;
		internal mStd.tAction<mStd.tFunc<tText>> _TraceOut;
	}
	
	//================================================================================
	public static tCallStack<tPos> NewCallStack<tPos>(
		tCallStack<tPos> aParent,
		mVM_Data.tProcDef<tPos> aProcDef,
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
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
		aTraceOut(() => " 8 := ENV  |  "+aEnv.ToText(20));
		aTraceOut(() => " 9 := OBJ  |  "+aObj.ToText(20));
		aTraceOut(() => "10 := ARG  |  "+aArg.ToText(20));
		aTraceOut(() => "11 := RES");
		
		return Result;
	}
	
	//================================================================================
	public static tCallStack<tPos>
	Step<tPos>(
		this tCallStack<tPos> aCallStack
	//================================================================================
	) {
		var (OpCode, Arg1, Arg2) = aCallStack._ProcDef.Commands.Get(aCallStack._CodePointer);
		aCallStack._TraceOut(() => $"{aCallStack._Regs.Size():#0} := {OpCode} {Arg1} {Arg2} // {aCallStack._ProcDef.PosList.Get(aCallStack._CodePointer)}");
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
				mDebug.Assert(BoolData1.MatchBool(out var Bool1));
				mDebug.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 && Bool2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Or: {
			//--------------------------------------------------------------------------------
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(BoolData1.MatchBool(out var Bool1));
				mDebug.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 || Bool2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.XOr: {
			//--------------------------------------------------------------------------------
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(BoolData1.MatchBool(out var Bool1));
				mDebug.Assert(BoolData2.MatchBool(out var Bool2));
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
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Bool(Int1 == Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsComp: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(mVM_Data.Int(Diff.Sign()));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsAdd: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 + Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsSub: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 - Int2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.IntsMul: {
			//--------------------------------------------------------------------------------
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 * Int2));
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
				mDebug.Assert(
					aCallStack._Regs.Get(Arg1).MatchPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Second: {
			//--------------------------------------------------------------------------------
				mDebug.Assert(
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
				mDebug.Assert(
					aCallStack._Regs.Get(Arg2).MatchPrefix(Arg1, out var Data_)
				);
				aCallStack._Regs.Push(Data_);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.HasPrefix: {
			//--------------------------------------------------------------------------------
				mDebug.Assert(
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
				mStd.Assert(Arg.MatchRecord(out var Record, out var Prefix));
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
					mStd.Assert(aCallStack._Regs.Get(Arg2).MatchBool(out Bool) && Bool);
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.SetObj: {
			//--------------------------------------------------------------------------------
				aCallStack._Obj = aCallStack._Regs.Get(Arg1);
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
				aCallStack._Regs.Get(Arg1)._Value = mStd.Any(aCallStack._Regs.Get(Arg2));
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.VarGet: {
			//--------------------------------------------------------------------------------
				mDebug.Assert(aCallStack._Regs.Get(Arg1)._Value.Match(out mVM_Data.tData x));
				aCallStack._Regs.Push(x);
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Call: {
			//--------------------------------------------------------------------------------
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				if (Proc.MatchExternDef(out var ExternDef)) {
					aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
				} else if(Proc.MatchExternProc(out ExternDef, out var Env)) {
					aCallStack._Regs.Push(ExternDef(Env, mVM_Data.Empty(), Arg, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine)));
				} else if (Proc.MatchDef<tPos>(out var Def)) {
					aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
				} else if (Proc.MatchProc<tPos>(out var Def_, out Env)) {
					var Res = mVM_Data.Empty();
					aCallStack._Regs.Push(Res);
					return NewCallStack(aCallStack, Def_, Env, mVM_Data.Empty(), Arg, Res, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine()));
				} else {
					throw mStd.Error("impossible");
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.Exec: {
			//--------------------------------------------------------------------------------
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				if (Proc.MatchExternDef(out var ExternDef)) {
					aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
				} else if(Proc.MatchExternProc(out ExternDef, out var Env)) {
					aCallStack._Regs.Push(ExternDef(Env, aCallStack._Obj, Arg, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine)));
				} else if (Proc.MatchDef<tPos>(out var Def)) {
					aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
				} else if (Proc.MatchProc<tPos>(out var Def_, out Env)) {
					var Res = mVM_Data.Empty();
					aCallStack._Regs.Push(Res);
					return NewCallStack(aCallStack, Def_, Env, aCallStack._Obj, Arg, Res, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine()));
				} else {
					throw mStd.Error("impossible");
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.ReturnIf: {
			//--------------------------------------------------------------------------------
				mDebug.Assert(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					var Src = aCallStack._Regs.Get(Arg2);
					var Des = aCallStack._Regs.Get(mVM_Data.tProcDef<tPos>.cResReg);
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
				mDebug.Assert(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
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
						aCallStack._Regs.Get(mVM_Data.tProcDef<tPos>.cEnvReg),
						aCallStack._Regs.Get(mVM_Data.tProcDef<tPos>.cObjReg),
						aCallStack._Regs.Get(Arg2),
						aCallStack._Regs.Get(mVM_Data.tProcDef<tPos>.cResReg)
					);
					aCallStack._CodePointer = 0;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mVM_Data.tOpCode.TailCallIf: {
			//--------------------------------------------------------------------------------
				mDebug.Assert(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					var CallerArgPair = aCallStack._Regs.Get(Arg2);
					mStd.Assert(CallerArgPair.MatchPair(out var Proc, out var Arg));
					mVM_Data.tData Res;
					if (Proc.MatchExternDef(out var ExternDef)) {
						Res = mVM_Data.ExternProc(ExternDef, Arg);
					} else if(Proc.MatchExternProc(out ExternDef, out var Env)) {
						Res = ExternDef(Env, mVM_Data.Empty(), Arg, aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine));
					} else if (Proc.MatchDef<tPos>(out var Def)) {
						Res = mVM_Data.Proc(Def, Arg);
					} else if (Proc.MatchProc<tPos>(out var Def_, out Env)) {
						return NewCallStack(
							aCallStack._Parent,
							Def_,
							Env,
							mVM_Data.Empty(),
							Arg,
							aCallStack._Regs.Get(mVM_Data.tProcDef<tPos>.cResReg),
							aTraceLine => aCallStack._TraceOut(() => "\t"+aTraceLine())
						);
					} else {
						throw mStd.Error("impossible");
					}
					var Des = aCallStack._Regs.Get(mVM_Data.tProcDef<tPos>.cResReg);
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
				throw mStd.Error("TODO");
			}
		}
		aCallStack._TraceOut(() => $@"    \ {aCallStack._Regs.Size()-1} = {aCallStack._Regs.Get(aCallStack._Regs.Size()-1).ToText(20)}");
		return aCallStack;
	}
	
	//================================================================================
	public static mVM_Data.tData
	GetModuleFactory<tPos>(
		mStream.tStream<mVM_Data.tProcDef<tPos>> aDefs
	//================================================================================
	) {
		var Env = mVM_Data.Empty();
		mDebug.Assert(aDefs.Match(out var LastDef, out aDefs));
		while (aDefs.Match(out var DefTemp, out aDefs)) {
			Env = mVM_Data.Pair(Env, mVM_Data.Def(LastDef));
			LastDef = DefTemp;
		}
		
		return mVM_Data.Proc(LastDef, Env);
	}
	
	//================================================================================
	public static void
	Run<tPos>(
		mVM_Data.tData aProc,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		using (mPerf.Measure()) {
			if (aProc.MatchProc<tPos>(out var Def, out var Env)) {
				var CallStack = NewCallStack(null, Def, Env, aObj, aArg, aRes, aTraceOut);
				while (CallStack != null) {
					CallStack = CallStack.Step();
				}
			} else if (aProc.MatchExternProc(out var ExternDef, out Env)) {
				var Res = ExternDef(Env, aObj, aArg, aTraceOut);
				aRes._DataType = Res._DataType;
				aRes._Value = Res._Value;
			} else {
				throw mStd.Error($"tPos ({typeof(tPos)}) does not match with the tPos of aProc");
			}
		}
	}
}
