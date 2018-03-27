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
		// BOOL
		And,
		Or,
		XOr,
		
		// INT
		NewInt,
		IntsAreEq,
		IntsComp,
		IntsAdd,
		IntsSub,
		IntsMul,
		
		// PAIR
		NewPair,
		First,
		Second,
		
		// PREFIX
		AddPrefix,
		DelPrefix,
		HasPrefix,
		
		// VAR
		VarDef,
		VarSet,
		VarGet,
		
		// JUMP
		SetObj,
		Call,
		Exec,
		ReturnIf,
		ContinueIf,
		
		// ASSERT
		Assert,
		
		// TYPE
		TypePair,
		TypePrefix,
		TypeVar,
		TypeSet,
		TypeCond,
		TypeFunc,
		TypeMeth,
	}
	
	public sealed class tProcDef {
		// standard stack indexes
		public static readonly tInt32 cEmptyReg = 0;
		public static readonly tInt32 cOneReg = 1;
		public static readonly tInt32 cFalseReg = 2;
		public static readonly tInt32 cTrueReg = 3;
		public static readonly tInt32 cEmptyTypeReg = 4;
		public static readonly tInt32 cBoolTypeReg = 5;
		public static readonly tInt32 cIntTypeReg = 6;
		public static readonly tInt32 cTypeTypeReg = 7;
		public static readonly tInt32 cEnvReg = 8;
		public static readonly tInt32 cObjReg = 9;
		public static readonly tInt32 cArgReg = 10;
		public static readonly tInt32 cResReg = 11;
		
		internal readonly mArrayList.tArrayList<(tOpCode, tInt32, tInt32)>
			_Commands = mArrayList.List<(tOpCode, tInt32, tInt32)>();
		
		internal readonly mVM_Type.tType _DefType = mVM_Type.Proc(
			mVM_Type.Empty(),
			mVM_Type.Unknown(),
			mVM_Type.Proc(
				mVM_Type.Unknown(),
				mVM_Type.Unknown(),
				mVM_Type.Unknown()
			)
		);
		
		internal readonly mArrayList.tArrayList<mVM_Type.tType>
			_Types = mArrayList.List<mVM_Type.tType>();
		
		internal tInt32 _LastReg = cResReg;
	}
	
	//================================================================================
	internal static void
	_AddCommand(
		this tProcDef aDef,
		tOpCode aCommand,
		tInt32 aReg1
	//================================================================================
	) => aDef._AddCommand(aCommand, aReg1, -1);
	
	//================================================================================
	internal static void
	_AddCommand(
		this tProcDef aDef,
		tOpCode aCommand,
		tInt32 aReg1,
		tInt32 aReg2
	//================================================================================
	) => aDef._Commands.Push((aCommand, aReg1, aReg2));
	
	//================================================================================
	internal static tInt32
	_AddReg(
		this tProcDef aDef,
		tOpCode aCommand,
		tInt32 aReg1
	//================================================================================
	) => aDef._AddReg(aCommand, aReg1, -1);
	
	//================================================================================
	internal static tInt32
	_AddReg(
		this tProcDef aDef,
		tOpCode aCommand,
		tInt32 aReg1,
		tInt32 aReg2
	//================================================================================
	) {
		aDef._AddCommand(aCommand, aReg1, aReg2);
		aDef._LastReg += 1;
		return aDef._LastReg;
	}
	
	//================================================================================
	public static tInt32
	And(
		this tProcDef aDef,
		tInt32 aBoolReg1,
		tInt32 aBoolReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.And, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static tInt32
	Or(
		this tProcDef aDef,
		tInt32 aBoolReg1,
		tInt32 aBoolReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.Or, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static tInt32
	XOr(
		this tProcDef aDef,
		tInt32 aBoolReg1,
		tInt32 aBoolReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.XOr, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static tInt32
	Int(
		this tProcDef aDef,
		tInt32 aIntValue
	//================================================================================
	) => aDef._AddReg(tOpCode.NewInt, aIntValue);
	
	//================================================================================
	public static  tInt32
	IntsAreEq(
		this tProcDef aDef,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.IntsAreEq, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tInt32
	IntsComp(
		this tProcDef aDef,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.IntsComp, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tInt32
	IntsAdd(
		this tProcDef aDef,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.IntsAdd, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tInt32
	IntsSub(
		this tProcDef aDef,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.IntsSub, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tInt32
	IntsMul(
		this tProcDef aDef,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.IntsMul, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tInt32
	Pair(
		this tProcDef aDef,
		tInt32 aDataReg1,
		tInt32 aDataReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.NewPair, aDataReg1, aDataReg2);
	
	//================================================================================
	public static tInt32
	First(
		this tProcDef aDef,
		tInt32 aPairReg
	//================================================================================
	) => aDef._AddReg(tOpCode.First, aPairReg);
	
	//================================================================================
	public static tInt32
	Second(
		this tProcDef aDef,
		tInt32 aPairReg
	//================================================================================
	) => aDef._AddReg(tOpCode.Second, aPairReg);
	
	//================================================================================
	public static tInt32
	AddPrefix(
		this tProcDef aDef,
		tInt32 aPrefixId,
		tInt32 aDataReg
	//================================================================================
	) => aDef._AddReg(tOpCode.AddPrefix, aPrefixId, aDataReg);
	
	//================================================================================
	public static tInt32
	DelPrefix(
		this tProcDef aDef,
		tInt32 aPrefixId,
		tInt32 aReg
	//================================================================================
	) => aDef._AddReg(tOpCode.DelPrefix, aPrefixId, aReg);
	
	//================================================================================
	public static tInt32
	HasPrefix(
		this tProcDef aDef,
		tInt32 aPrefixId,
		tInt32 aDataReg
	//================================================================================
	) => aDef._AddReg(tOpCode.HasPrefix, aPrefixId, aDataReg);
	
	//================================================================================
	public static void
	SetObj(
		this tProcDef aDef,
		tInt32 aObjReg
	//================================================================================
	) {
		aDef._AddCommand(tOpCode.SetObj, aObjReg);
	}
	
	//================================================================================
	public static tInt32
	VarDef(
		this tProcDef aDef,
		tInt32 aValueReg
	//================================================================================
	) => aDef._AddReg(tOpCode.VarDef, aValueReg);
	
	//================================================================================
	public static void
	VarSet(
		this tProcDef aDef,
		tInt32 aVarReg,
		tInt32 aValueReg
	//================================================================================
	) {
		aDef._AddCommand(tOpCode.VarSet, aVarReg, aValueReg);
	}
	
	//================================================================================
	public static tInt32
	VarGet(
		this tProcDef aDef,
		tInt32 aVarReg
	//================================================================================
	) => aDef._AddReg(tOpCode.VarGet, aVarReg);
	
	//================================================================================
	public static tInt32
	Call(
		this tProcDef aDef,
		tInt32 aProcReg,
		tInt32 aArgReg
	//================================================================================
	) => aDef._AddReg(tOpCode.Call, aProcReg, aArgReg);
	
	//================================================================================
	public static tInt32
	Exec(
		this tProcDef aDef,
		tInt32 aProcReg,
		tInt32 aArgReg
	//================================================================================
	) => aDef._AddReg(tOpCode.Exec, aProcReg, aArgReg);
	
	//================================================================================
	public static void
	ReturnIf(
		this tProcDef aDef,
		tInt32 aCondReg,
		tInt32 aResReg
	//================================================================================
	) {
		aDef._AddCommand(tOpCode.ReturnIf, aCondReg, aResReg);
	}
	
	//================================================================================
	public static void
	ContinueIf(
		this tProcDef aDef,
		tInt32 aCondReg,
		tInt32 aArgReg
	//================================================================================
	) {
		aDef._AddCommand(tOpCode.ContinueIf, aCondReg, aArgReg);
	}
	
	//================================================================================
	public static void
	Assert(
		this tProcDef aDef,
		tInt32 aPreCondReg,
		tInt32 aPostCondReg
	//================================================================================
	) {
		aDef._AddCommand(tOpCode.Assert, aPreCondReg, aPostCondReg);
	}
	
	//================================================================================
	public static tInt32
	TypePair(
		this tProcDef aDef,
		tInt32 aTypeReg1,
		tInt32 aTypeReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.TypePair, aTypeReg1, aTypeReg2);
	
	//================================================================================
	public static tInt32
	TypePrefix(
		this tProcDef aDef,
		tInt32 aPrefix,
		tInt32 aTypeReg
	//================================================================================
	) => aDef._AddReg(tOpCode.TypePrefix, aPrefix, aTypeReg);
	
	//================================================================================
	public static tInt32
	TypeVar(
		this tProcDef aDef,
		tInt32 aTypeReg
	//================================================================================
	) => aDef._AddReg(tOpCode.TypeVar, aTypeReg);
	
	//================================================================================
	public static tInt32
	TypeSet(
		this tProcDef aDef,
		tInt32 aTypeReg1,
		tInt32 aTypeReg2
	//================================================================================
	) => aDef._AddReg(tOpCode.TypeSet, aTypeReg1, aTypeReg2);
	
	//================================================================================
	public static tInt32
	TypeFunc(
		this tProcDef aDef,
		tInt32 aArgTypeReg,
		tInt32 aResTypeReg
	//================================================================================
	) => aDef._AddReg(tOpCode.TypeFunc, aArgTypeReg, aResTypeReg);
	
	//================================================================================
	public static tInt32
	TypeMeth(
		this tProcDef aDef,
		tInt32 aObjTypeReg,
		tInt32 aFuncTypeReg
	//================================================================================
	) => aDef._AddReg(tOpCode.TypeFunc, aObjTypeReg, aFuncTypeReg);
	
	// TODO: Match Types
	
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
		aTraceOut(() => " 8 := ENV  |  "+aEnv);
		aTraceOut(() => " 9 := OBJ  |  "+aObj);
		aTraceOut(() => " 10 := ARG  |  "+aArg);
		aTraceOut(() => " 11 := RES");
		
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
				mDebug.Assert(BoolData1.MatchBool(out var Bool1));
				mDebug.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 && Bool2));
				break;
			}
			case tOpCode.Or: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(BoolData1.MatchBool(out var Bool1));
				mDebug.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 || Bool2));
				break;
			}
			case tOpCode.XOr: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(BoolData1.MatchBool(out var Bool1));
				mDebug.Assert(BoolData2.MatchBool(out var Bool2));
				aCallStack._Regs.Push(mVM_Data.Bool(Bool1 ^ Bool2));
				break;
			}
			case tOpCode.IntsAreEq: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Bool(Int1 == Int2));
				break;
			}
			case tOpCode.IntsComp: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(mVM_Data.Int(Diff.Sign()));
				break;
			}
			case tOpCode.IntsAdd: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 + Int2));
				break;
			}
			case tOpCode.IntsSub: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
				aCallStack._Regs.Push(mVM_Data.Int(Int1 - Int2));
				break;
			}
			case tOpCode.IntsMul: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mDebug.Assert(IntData1.MatchInt(out var Int1));
				mDebug.Assert(IntData2.MatchInt(out var Int2));
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
				mDebug.Assert(
					aCallStack._Regs.Get(Arg1).MatchPair(
						out var Var1,
						out var Var2
					)
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			case tOpCode.Second: {
				mDebug.Assert(
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
				mDebug.Assert(
					aCallStack._Regs.Get(Arg2).MatchPrefix(Arg1, out var Data_)
				);
				aCallStack._Regs.Push(Data_);
				break;
			}
			case tOpCode.HasPrefix: {
				mDebug.Assert(
					aCallStack._Regs.Get(Arg2).MatchPrefix(out var PrefixId, out var Data)
				);
				aCallStack._Regs.Push(mVM_Data.Bool(PrefixId.Equals(Arg1)));
				break;
			}
			case tOpCode.Assert: {
				if (aCallStack._Regs.Get(Arg1).MatchBool(out var Bool) && Bool) {
					mDebug.Assert(aCallStack._Regs.Get(Arg2).MatchBool(out Bool) && Bool);
				}
				break;
			}
			case tOpCode.SetObj: {
				aCallStack._Obj = aCallStack._Regs.Get(Arg1);
				break;
			}
			case tOpCode.VarDef: {
				aCallStack._Regs.Push(mVM_Data.Var(aCallStack._Regs.Get(Arg1)));
				break;
			}
			case tOpCode.VarSet: {
				aCallStack._Regs.Get(Arg1)._Value._Value = aCallStack._Regs.Get(Arg2);
				break;
			}
			case tOpCode.VarGet: {
				aCallStack._Regs.Push((mVM_Data.tData)aCallStack._Regs.Get(Arg1)._Value._Value);
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
					throw mStd.Error("impossible");
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
					throw mStd.Error("impossible");
				}
				break;
			}
			case tOpCode.ReturnIf: {
				mDebug.Assert(aCallStack._Regs.Get(Arg1).MatchBool(out var Cond));
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
				throw mStd.Error("TODO");
			}
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
		mDebug.Assert(aDefs.Match(out var LastDef, out aDefs));
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
		} else {
			throw mStd.Error("impossible");
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
		mTest.Assert(aArg.MatchPair(out var Arg1, out var Arg2));
		mTest.Assert(Arg1.MatchInt(out var IntArg1));
		mTest.Assert(Arg2.MatchInt(out var IntArg2));
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
				mTest.AssertEq(Res, mVM_Data.Int(2));
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
				mTest.AssertEq(Res, mVM_Data.Int(2));
			}
		)
	);
	
	#endregion
}