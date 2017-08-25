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
	
	public enum tOpCode {
		NewInt,
		And,
		Or,
		XOr,
		IntsAreEq,
		IntsComp,
		IntsAdd,
		IntsSub,
		IntsMul,
		NewPair,
		First,
		Second,
		AddPrefix,
		DelPrefix,
		HasPrefix,
		SetObj,
		Var,
		Assert,
		Call,
		Exec,
		ReturnIf,
		ContinueIf
	}
	
	public sealed class tProcDef {
		// standard stack indexes
		public const tInt32 cEmptyReg = 0;
		public const tInt32 cOneReg   = 1;
		public const tInt32 cFalseReg = 2;
		public const tInt32 cTrueReg  = 3;
		public const tInt32 cEnvReg   = 4;
		public const tInt32 cObjReg   = 5;
		public const tInt32 cArgReg   = 6;
		public const tInt32 cResReg   = 7;
		
		internal readonly mArrayList.tArrayList<(tOpCode, tInt32, tInt32)>
			_Commands = mArrayList.List<(tOpCode, tInt32, tInt32)>();
		
		private tInt32 _LastReg = 7;
		
		//================================================================================
		internal void
		_AddCommand(
			tOpCode aCommand,
			tInt32 aReg1,
			tInt32 aReg2 = -1
		//================================================================================
		) {
			_Commands.Push((aCommand, aReg1, aReg2));
		}
		
		//================================================================================
		internal tInt32
		_AddReg(
			tOpCode aCommand,
			tInt32 aReg1,
			tInt32 aReg2 = -1
		//================================================================================
		) {
			_AddCommand(aCommand, aReg1, aReg2);
			_LastReg += 1;
			return _LastReg;
		}
		
		//================================================================================
		public tInt32
		And(
			tInt32 aBoolReg1,
			tInt32 aBoolReg2
		//================================================================================
		) => _AddReg(tOpCode.And, aBoolReg1, aBoolReg2);
		
		//================================================================================
		public tInt32
		Or(
			tInt32 aBoolReg1,
			tInt32 aBoolReg2
		//================================================================================
		) => _AddReg(tOpCode.Or, aBoolReg1, aBoolReg2);
		
		//================================================================================
		public tInt32
		XOr(
			tInt32 aBoolReg1,
			tInt32 aBoolReg2
		//================================================================================
		) => _AddReg(tOpCode.XOr, aBoolReg1, aBoolReg2);
		
		//================================================================================
		public tInt32
		Int(
			tInt32 aIntValue
		//================================================================================
		) => _AddReg(tOpCode.NewInt, aIntValue);
		
		//================================================================================
		public tInt32
		IntsAreEq(
			tInt32 aIntReg1,
			tInt32 aIntReg2
		//================================================================================
		) => _AddReg(tOpCode.IntsAreEq, aIntReg1, aIntReg2);
		
		//================================================================================
		public tInt32
		IntsComp(
			tInt32 aIntReg1,
			tInt32 aIntReg2
		//================================================================================
		) => _AddReg(tOpCode.IntsComp, aIntReg1, aIntReg2);
		
		//================================================================================
		public tInt32
		IntsAdd(
			tInt32 aIntReg1,
			tInt32 aIntReg2
		//================================================================================
		) => _AddReg(tOpCode.IntsAdd, aIntReg1, aIntReg2);
		
		//================================================================================
		public tInt32
		IntsSub(
			tInt32 aIntReg1,
			tInt32 aIntReg2
		//================================================================================
		) => _AddReg(tOpCode.IntsSub, aIntReg1, aIntReg2);
		
		//================================================================================
		public tInt32
		IntsMul(
			tInt32 aIntReg1,
			tInt32 aIntReg2
		//================================================================================
		) => _AddReg(tOpCode.IntsMul, aIntReg1, aIntReg2);
		
		//================================================================================
		public tInt32
		Pair(
			tInt32 aDataReg1,
			tInt32 aDataReg2
		//================================================================================
		) => _AddReg(tOpCode.NewPair, aDataReg1, aDataReg2);
		
		//================================================================================
		public tInt32
		First(
			tInt32 aPairReg
		//================================================================================
		) => _AddReg(tOpCode.First, aPairReg);
		
		//================================================================================
		public tInt32
		Second(
			tInt32 aPairReg
		//================================================================================
		) => _AddReg(tOpCode.Second, aPairReg);
		
		//================================================================================
		public tInt32
		AddPrefix(
			tInt32 aPrefixId,
			tInt32 aDataReg
		//================================================================================
		) => _AddReg(tOpCode.AddPrefix, aPrefixId, aDataReg);
		
		//================================================================================
		public tInt32
		DelPrefix(
			tInt32 aPrefixId,
			tInt32 aReg
		//================================================================================
		) => _AddReg(tOpCode.DelPrefix, aPrefixId, aReg);
		
		//================================================================================
		public tInt32
		HasPrefix(
			tInt32 aPrefixId,
			tInt32 aDataReg
		//================================================================================
		) => _AddReg(tOpCode.HasPrefix, aPrefixId, aDataReg);
		
		//================================================================================
		public void
		SetObj(
			tInt32 aObjReg
		//================================================================================
		) {
			_AddCommand(tOpCode.SetObj, aObjReg);
		}
		
		//================================================================================
		public tInt32
		Var(
			tInt32 aValueReg
		//================================================================================
		) => _AddReg(tOpCode.Var, aValueReg);
		
		//================================================================================
		public tInt32
		Call(
			tInt32 aProcReg,
			tInt32 aArgReg
		//================================================================================
		) => _AddReg(tOpCode.Call, aProcReg, aArgReg);
		
		//================================================================================
		public tInt32
		Exec(
			tInt32 aProcReg,
			tInt32 aArgReg
		//================================================================================
		) => _AddReg(tOpCode.Exec, aProcReg, aArgReg);
		
		//================================================================================
		public void
		ReturnIf(
			tInt32 aCondReg,
			tInt32 aResReg
		//================================================================================
		) {
			_AddCommand(tOpCode.ReturnIf, aCondReg, aResReg);
		}
		
		//================================================================================
		public void
		ContinueIf(
			tInt32 aCondReg,
			tInt32 aArgReg
		//================================================================================
		) {
			_AddCommand(tOpCode.ContinueIf, aCondReg, aArgReg);
		}
		
		//================================================================================
		public void
		Assert(
			tInt32 aPreCondReg,
			tInt32 aPostCondReg
		//================================================================================
		) {
			_AddCommand(tOpCode.Assert, aPreCondReg, aPostCondReg);
		}
	}
	
	public sealed class tCallStack {
		internal tCallStack _Parent;
		internal mArrayList.tArrayList<mVM_Data.tData> _Regs;
		internal tProcDef _ProcDef;
		internal tInt32 _CodePointer = 0;
		internal mVM_Data.tData _Obj;
		internal mStd.tAction<mStd.tFunc<tText>> _TraceOut;
	}
	
	//================================================================================
	public static tCallStack NewCallStack(
		tCallStack aParent,
		tProcDef aProcDef,
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		var Result = new tCallStack {
			_TraceOut = aTraceOut,
			_Parent = aParent,
			_ProcDef = aProcDef,
			_Regs = mArrayList.List(
				mVM_Data.Empty(),
				mVM_Data.Int(1),
				mVM_Data.Bool(false),
				mVM_Data.Bool(true),
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
		aTraceOut(() => " 4 := ENV  |  "+aEnv);
		aTraceOut(() => " 5 := OBJ  |  "+aObj);
		aTraceOut(() => " 6 := ARG  |  "+aArg);
		aTraceOut(() => " 7 := RES");
		
		return Result;
	}
	
	//================================================================================
	public static tCallStack
	Step(
		 this tCallStack aCallStack
	//================================================================================
	) {
		var (OpCode, Arg1, Arg2) = aCallStack._ProcDef._Commands.Get(aCallStack._CodePointer);
		aCallStack._CodePointer += 1;
		
		aCallStack._TraceOut(() => $"{aCallStack._Regs.Size():#0} := {OpCode} {Arg1} {Arg2}");
		
		switch (OpCode) {
			case tOpCode.NewInt: {
				aCallStack._Regs.Push(mVM_Data.Int(Arg1));
				break;
			}
			case tOpCode.And: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(BoolData1.MatchBool(out var Bool1));
				mStd.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 & Bool2));
				break;
			}
			case tOpCode.Or: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(BoolData1.MatchBool(out var Bool1));
				mStd.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 | Bool2));
				break;
			}
			case tOpCode.XOr: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(BoolData1.MatchBool(out var Bool1));
				mStd.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 ^ Bool2));
				break;
			}
			case tOpCode.IntsAreEq: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.MatchInt(out var Int1));
				mStd.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Bool(Int1 == Int2));
				break;
			}
			case tOpCode.IntsComp: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.MatchInt(out var Int1));
				mStd.Assert(IntData2.MatchInt(out var Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(mVM_Data.Int(Diff < 0 ? -1 : Diff == 0 ? 0 : 1));
				break;
			}
			case tOpCode.IntsAdd: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.MatchInt(out var Int1));
				mStd.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 + Int2));
				break;
			}
			case tOpCode.IntsSub: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.MatchInt(out var Int1));
				mStd.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 - Int2));
				break;
			}
			case tOpCode.IntsMul: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.MatchInt(out var Int1));
				mStd.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 * Int2));
				break;
			}
			case tOpCode.NewPair: {
				aCallStack._Regs.Push(
					mVM_Data.Pair(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			case tOpCode.First: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg1).MatchPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			case tOpCode.Second: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg1).MatchPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var2);
				break;
			}
			case tOpCode.AddPrefix: {
				aCallStack._Regs.Push(mVM_Data.Prefix(Arg1, aCallStack._Regs.Get(Arg2)));
				break;
			}
			case tOpCode.DelPrefix: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg2).MatchPrefix(Arg1, out var Data_)
				);
				aCallStack._Regs.Push(Data_);
				break;
			}
			case tOpCode.HasPrefix: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg2).MatchPrefix(out var PrefixId, out var Data)
				);
				aCallStack._Regs.Push(mVM_Data.Bool(PrefixId.Equals(Arg1)));
				break;
			}
			case tOpCode.Assert: {
				if (aCallStack._Regs.Get(Arg1).MatchBool(out var Bool) && Bool) {
					mStd.Assert(aCallStack._Regs.Get(Arg2).MatchBool(out Bool) && Bool);
				}
				break;
			}
			case tOpCode.SetObj: {
				aCallStack._Obj = aCallStack._Regs.Get(Arg1);
				break;
			}
			case tOpCode.Var: {
				aCallStack._Regs.Push(mVM_Data.Var(aCallStack._Regs.Get(Arg1)));
				break;
			}
			case tOpCode.Call: {
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				if (Proc.MatchExternDef(out var ExternDef)) {
					aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
				} else if(Proc.MatchExternProc(out ExternDef, out var Env)) {
					aCallStack._Regs.Push(ExternDef(Env, mVM_Data.Empty(), Arg, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine)));
				} else if (Proc.MatchDef(out var Def)) {
					aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
				} else if (Proc.MatchProc(out var Def_, out Env)) {
					var Res = mVM_Data.Empty();
					aCallStack._Regs.Push(Res);
					return NewCallStack(aCallStack, Def_, Env, mVM_Data.Empty(), Arg, Res, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine()));
				} else {
					throw null;
				}
				break;
			}
			case tOpCode.Exec: {
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				if (Proc.MatchExternDef(out var ExternDef)) {
					aCallStack._Regs.Push(mVM_Data.ExternProc(ExternDef, Arg));
				} else if(Proc.MatchExternProc(out ExternDef, out var Env)) {
					aCallStack._Regs.Push(ExternDef(Env, aCallStack._Obj, Arg, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine)));
				} else if (Proc.MatchDef(out var Def)) {
					aCallStack._Regs.Push(mVM_Data.Proc(Def, Arg));
				} else if (Proc.MatchProc(out var Def_, out Env)) {
					var Res = mVM_Data.Empty();
					aCallStack._Regs.Push(Res);
					return NewCallStack(aCallStack, Def_, Env, aCallStack._Obj, Arg, Res, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine()));
				} else {
					throw null;
				}
				break;
			}
			case tOpCode.ReturnIf: {
				mStd.Assert(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					var Src = aCallStack._Regs.Get(Arg2);
					var Des = aCallStack._Regs.Get(tProcDef.cResReg);
					Des._DataType = Src._DataType;
					Des._Value = Src._Value;
					aCallStack._TraceOut(() => "====================================");
					return aCallStack._Parent;
				}
				break;
			}
			case tOpCode.ContinueIf: {
				mStd.Assert(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
				if (Cond) {
					aCallStack._Regs = mArrayList.List(
						mVM_Data.Empty(),
						mVM_Data.Int(1),
						mVM_Data.Bool(false),
						mVM_Data.Bool(true),
						aCallStack._Regs.Get(tProcDef.cEnvReg),
						aCallStack._Regs.Get(tProcDef.cObjReg),
						aCallStack._Regs.Get(Arg2),
						aCallStack._Regs.Get(tProcDef.cResReg)
					);
					aCallStack._CodePointer = 0;
				}
				break;
			}
			
			// TODO: missing IL Command
			// - Create Process
			// - Send Message
			
			default: {
				throw null;
			};
		}
		aCallStack._TraceOut(() => $@"    \ {aCallStack._Regs.Size()-1} = {aCallStack._Regs.Get(aCallStack._Regs.Size()-1)}");
		return aCallStack;
	}
	
	//================================================================================
	public static mVM_Data.tData
	GetModuleFactory(
		mList.tList<tProcDef> aDefs
	//================================================================================
	) {
		var Env = mVM_Data.Empty();
		mStd.Assert(aDefs.Match(out var LastDef, out aDefs));
		while (aDefs.Match(out var DefTemp, out aDefs)) {
			Env = mVM_Data.Pair(Env, mVM_Data.Def(LastDef));
			LastDef = DefTemp;
		}
		
		return mVM_Data.Proc(LastDef, Env);
	}
	
	//================================================================================
	public static void
	Run(
		mVM_Data.tData aProc,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mVM_Data.tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		if (aProc.MatchProc(out var Def, out var Env)) {
			var CallStack = NewCallStack(null, Def, Env, aObj, aArg, aRes, aTraceOut);
			while (CallStack != null) {
				CallStack = CallStack.Step();
			}
		} else if (aProc.MatchExternProc(out var ExternDef, out Env)) {
			var Res = ExternDef(Env, aObj, aArg, aTraceOut);
			aRes._DataType = Res._DataType;
			aRes._Value = Res._Value;
		}
	}
	
	#region TEST
	
	//================================================================================
	private static mVM_Data.tData
	Add (
		mVM_Data.tData aEnv,
		mVM_Data.tData aObj,
		mVM_Data.tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mStd.Assert(Arg1.MatchInt(out var IntArg1));
		mStd.Assert(Arg2.MatchInt(out var IntArg2));
		return mVM_Data.Int(IntArg1 + IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM),
		mTest.Test(
			"ExternDef",
			aDebugStream => {
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc1 = new tProcDef();
				var r1 = Proc1.Pair(tProcDef.cOneReg, tProcDef.cOneReg);
				var r2 = Proc1.Call(tProcDef.cEnvReg, tProcDef.cEmptyReg);
				var r3 = Proc1.Call(r2, r1);
				Proc1.ReturnIf(tProcDef.cTrueReg, r3);
				
				var Res = mVM_Data.Empty();
				Run(mVM_Data.Proc(Proc1, Env), mVM_Data.Empty(), mVM_Data.Empty(), Res, TraceOut);
				mStd.AssertEq(Res, mVM_Data.Int(2));
			}
		),
		mTest.Test(
			"InternDef",
			aDebugStream => {
				#if TRACE
					var TraceOut = mStd.Action(
						(mStd.tFunc<tText> aLasyText) => aDebugStream(aLasyText())
					);
				#else
					var TraceOut = mStd.Action<mStd.tFunc<tText>>(_ => {});
				#endif
				
				var Env = mVM_Data.ExternDef(Add);
				
				var Proc2 = new tProcDef();
				Proc2.ReturnIf(
					tProcDef.cTrueReg,
					Proc2.Call(
						Proc2.Call(
							tProcDef.cEnvReg,
							tProcDef.cEmptyReg
						),
						Proc2.Pair(
							tProcDef.cOneReg,
							tProcDef.cOneReg
						)
					)
				);
				
				var Res = mVM_Data.Empty();
				Run(mVM_Data.Proc(Proc2, Env), mVM_Data.Empty(), mVM_Data.Empty(), Res, TraceOut);
				mStd.AssertEq(Res, mVM_Data.Int(2));
			}
		)
	);
	
	#endregion
}