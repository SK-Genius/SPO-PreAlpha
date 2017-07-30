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

public static class mIL_VM {
	
	#region tData
	
	public enum tDataType {
		Empty,
		Bool,
		Int,
		Pair,
		Prefix,
		Proc,
		ExternProc,
		Def,
		ExternDef,
		Var
	}
	
	public sealed class tData {
		internal tDataType _DataType;
		internal mStd.tAny _Value;
		internal tBool _IsMutable;
		
		//================================================================================
		public tBool Equals(
			tData a
		//================================================================================
		) => (
			!a.IsNull() &&
			_DataType.Equals(a._DataType) &&
			_Value.Equals(a._Value)
		);
		
		override public tBool Equals(object a) => Equals(a as tData);
		override public tText ToString() => $"({_DataType} {_Value})";
	}
	
	//================================================================================
	private static tData
	Data<t>(
		tDataType aType,
		tBool aIsMutable,
		t aValue
	//================================================================================
	) => new tData{
		_DataType = aType,
		_IsMutable = aIsMutable,
		_Value = mStd.Any(aValue)
	};
	
	//================================================================================
	private static tData
	Data<t1, t2>(
		tDataType aType,
		tBool aIsMutable,
		t1 aValue1,
		t2 aValue2
	//================================================================================
	) => new tData{
		_DataType = aType,
		_IsMutable = aIsMutable,
		_Value = mStd.Any((aValue1, aValue2))
	};
	
	//================================================================================
	public static bool
	Match(
		this tData aArg,
		out tBool a1,
		out tBool a2
	//================================================================================
	) {
		mStd.Assert(
			aArg.Match(
				tDataType.Pair,
				out tData Arg1,
				out tData Arg2_
			)
		);
		mStd.Assert(
			Arg2_.Match(
				tDataType.Pair,
				out tData Arg2,
				out tData Arg_
			)
		);
		mStd.AssertEq(Arg_._DataType, tDataType.Empty);
		mStd.Assert(Arg1.Match(tDataType.Bool, out a1));
		mStd.Assert(Arg2.Match(tDataType.Bool, out a2));
		return true;
	}
	
	//================================================================================
	public static bool
	Match(
		this tData aArg,
		out tInt32 a1,
		out tInt32 a2
	//================================================================================
	) {
		mStd.Assert(
			aArg.Match(
				tDataType.Pair,
				out tData Arg1,
				out tData Arg2_
			)
		);
		mStd.Assert(
			Arg2_.Match(
				tDataType.Pair,
				out tData Arg2,
				out tData Arg_
			)
		);
		mStd.AssertEq(Arg_._DataType, tDataType.Empty);
		
		if (Arg1.Match(tDataType.Var, out tData Arg1Var)) {
			Arg1 = Arg1Var;
		}
		mStd.Assert(Arg1.Match(tDataType.Int, out a1));
		
		if (Arg2.Match(tDataType.Var, out tData Arg2Var)) {
			Arg2 = Arg2Var;
		}
		mStd.Assert(Arg2.Match(tDataType.Int, out a2));
		return true;
	}
	
	//================================================================================
	public static tData
	Empty(
	//================================================================================
	) => Data(tDataType.Empty, false, 1);
	
	//================================================================================
	public static tData
	Bool(
		tBool aValue
	//================================================================================
	) => Data(tDataType.Bool, false, aValue);
	
	//================================================================================
	public static tData
	Int(
		tInt32 aValue
	//================================================================================
	) => Data(tDataType.Int, false, aValue);
	
	//================================================================================
	public static tData
	Pair(
		tData aFirst,
		tData aSecond
	//================================================================================
	) => Data(tDataType.Pair, aFirst._IsMutable | aSecond._IsMutable, aFirst, aSecond);
	
	//================================================================================
	public static tData
	Tuple(
		params tData[] a
	//================================================================================
	) => mList.List(a).Reverse().Reduce(Empty(), (aList, aItem) => Pair(aItem, aList));
	
	//================================================================================
	public static tData
	Prefix(
		tText aPrefix,
		tData aData
	//================================================================================
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefix.GetHashCode(), aData);
	
	//================================================================================
	public static tData
	Prefix(
		tInt32 aPrefixId,
		tData aData
	//================================================================================
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefixId, aData);
	
	//================================================================================
	public static tData
	Proc(
		tProcDef aDef,
		tData aEnv
	//================================================================================
	) {
		mStd.AssertNot(aEnv._IsMutable);
		return Data(tDataType.Proc, false, aDef, aEnv); // In the end this is the place where the compiler will called !!!
	}
	
	//================================================================================
	public static tData
	ExternProc(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		tData aEnv
	//================================================================================
	) {
		mStd.AssertNot(aEnv._IsMutable);
		return Data(tDataType.ExternProc, false, aExternDef, aEnv);
	}
	
	//================================================================================
	public static tData
	Def(
		tProcDef aDef
	//================================================================================
	) => Data(tDataType.Def, false, aDef);
	
	//================================================================================
	public static tData
	Var(
		tData aValue
	//================================================================================
	) => Data(tDataType.Var, true, aValue);
	
	//================================================================================
	public static tData
	ExternDef(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	//================================================================================
	) => Data(tDataType.ExternDef, false, a);
	
	//================================================================================
	public static tBool
	Match(
		this tData aData,
		tDataType aType,
		out mStd.tAny aValue
	//================================================================================
	) {
		aValue = aData._Value;
		return aData._DataType.Equals(aType);
	}
	
	//================================================================================
	public static tBool
	Match<t>(
		this tData aData,
		tDataType aType,
		out t aValue
	//================================================================================
	) {
		aValue = default(t);
		return aData._DataType.Equals(aType) && aData._Value.Match(out aValue);
	}
	
	//================================================================================
	public static tBool
	Match<t1, t2>(
		this tData aData,
		tDataType aType,
		out t1 aValue1,
		out t2 aValue2
	//================================================================================
	) {
		aValue1 = default(t1);
		aValue2 = default(t2);
		if (
			aData._DataType.Equals(aType) &&
			aData._Value.Match(out (t1, t2) Tuple)
		) {
			(aValue1, aValue2) = Tuple;
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match<t1, t2, t3>(
		this tData aData,
		tDataType aType,
		out t1 aValue1,
		out t2 aValue2,
		out t3 aValue3
	//================================================================================
	) {
		aValue1 = default(t1);
		aValue2 = default(t2);
		aValue3 = default(t3);
		if (
			aData._DataType.Equals(aType) &&
			aData._Value.Match(out (t1, t2, t3) Tuple)
		) {
			(aValue1, aValue2, aValue3) = Tuple;
			return true;
		}
		return false;
	}
	
	#endregion
	
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
		internal mArrayList.tArrayList<tData> _Regs;
		internal tProcDef _ProcDef;
		internal tInt32 _CodePointer = 0;
		internal tData _Obj;
		internal mStd.tAction<mStd.tFunc<tText>> _TraceOut;
	}
	
	//================================================================================
	public static tCallStack NewCallStack(
		tCallStack aParent,
		tProcDef aProcDef,
		tData aEnv,
		tData aObj,
		tData aArg,
		tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		var Result = new tCallStack {
			_TraceOut = aTraceOut,
			_Parent = aParent,
			_ProcDef = aProcDef,
			_Regs = mArrayList.List(
				Empty(),
				Int(1),
				Bool(false),
				Bool(true),
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
				aCallStack._Regs.Push(Int(Arg1));
				break;
			}
			case tOpCode.And: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(BoolData1.Match(tDataType.Bool, out tBool Bool1));
				mStd.Assert(BoolData2.Match(tDataType.Bool, out tBool Bool2));
				aCallStack._Regs.Push(Bool(Bool1 & Bool2));
				break;
			}
			case tOpCode.Or: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(BoolData1.Match(tDataType.Bool, out tBool Bool1));
				mStd.Assert(BoolData2.Match(tDataType.Bool, out tBool Bool2));
				aCallStack._Regs.Push(Bool(Bool1 | Bool2));
				break;
			}
			case tOpCode.XOr: {
				var BoolData1 = aCallStack._Regs.Get(Arg1);
				var BoolData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(BoolData1.Match(tDataType.Bool, out tBool Bool1));
				mStd.Assert(BoolData2.Match(tDataType.Bool, out tBool Bool2));
				aCallStack._Regs.Push(Bool(Bool1 ^ Bool2));
				break;
			}
			case tOpCode.IntsAreEq: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.Match(tDataType.Int, out tInt32 Int1));
				mStd.Assert(IntData2.Match(tDataType.Int, out tInt32 Int2));
				aCallStack._Regs.Push(Bool(Int1 == Int2));
				break;
			}
			case tOpCode.IntsComp: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.Match(tDataType.Int, out tInt32 Int1));
				mStd.Assert(IntData2.Match(tDataType.Int, out tInt32 Int2));
				var Diff = Int1 - Int2;
				aCallStack._Regs.Push(Int(Diff < 0 ? -1 : Diff == 0 ? 0 : 1));
				break;
			}
			case tOpCode.IntsAdd: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.Match(tDataType.Int, out tInt32 Int1));
				mStd.Assert(IntData2.Match(tDataType.Int, out tInt32 Int2));
				aCallStack._Regs.Push(Int(Int1 + Int2));
				break;
			}
			case tOpCode.IntsSub: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.Match(tDataType.Int, out tInt32 Int1));
				mStd.Assert(IntData2.Match(tDataType.Int, out tInt32 Int2));
				aCallStack._Regs.Push(Int(Int1 - Int2));
				break;
			}
			case tOpCode.IntsMul: {
				var IntData1 = aCallStack._Regs.Get(Arg1);
				var IntData2 = aCallStack._Regs.Get(Arg2);
				mStd.Assert(IntData1.Match(tDataType.Int, out tInt32 Int1));
				mStd.Assert(IntData2.Match(tDataType.Int, out tInt32 Int2));
				aCallStack._Regs.Push(Int(Int1 * Int2));
				break;
			}
			case tOpCode.NewPair: {
				aCallStack._Regs.Push(
					Pair(
						aCallStack._Regs.Get(Arg1),
						aCallStack._Regs.Get(Arg2)
					)
				);
				break;
			}
			case tOpCode.First: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg1).Match(
						tDataType.Pair,
						out tData Var1,
						out tData Var2
					)
				);
				aCallStack._Regs.Push(Var1);
				break;
			}
			case tOpCode.Second: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg1).Match(
						tDataType.Pair,
						out (tData, tData) Pair
					)
				);
				var (Var1, Var2) = Pair;
				aCallStack._Regs.Push(Var2);
				break;
			}
			case tOpCode.AddPrefix: {
				aCallStack._Regs.Push(Prefix(Arg1, aCallStack._Regs.Get(Arg2)));
				break;
			}
			case tOpCode.DelPrefix: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg2).Match(
						tDataType.Prefix,
						out (tInt32, tData) PrefixData
					)
				);
				var (Prefix, Data_) = PrefixData;
				mStd.Assert(Prefix.Equals(Arg1));
				aCallStack._Regs.Push(Data_);
				break;
			}
			case tOpCode.HasPrefix: {
				mStd.Assert(
					aCallStack._Regs.Get(Arg2).Match(
						tDataType.Prefix,
						out (tInt32, tData) PrefixData
					)
				);
				var (Prefix, Data_) = PrefixData;
				aCallStack._Regs.Push(Bool(Prefix.Equals(Arg1)));
				break;
			}
			case tOpCode.Assert: {
				if (aCallStack._Regs.Get(Arg1).Match(tDataType.Bool, out tBool Bool) && Bool) {
					mStd.Assert(aCallStack._Regs.Get(Arg2).Match(tDataType.Bool, out Bool) && Bool);
				}
				break;
			}
			case tOpCode.SetObj: {
				aCallStack._Obj = aCallStack._Regs.Get(Arg1);
				break;
			}
			case tOpCode.Var: {
				aCallStack._Regs.Push(Var(aCallStack._Regs.Get(Arg1)));
				break;
			}
			case tOpCode.Call: {
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				if (Proc.Match(tDataType.ExternDef, out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> ExternDef)) {
					aCallStack._Regs.Push(ExternProc(ExternDef, Arg));
				} else if(Proc.Match(tDataType.ExternProc, out ExternDef, out tData Env)) {
					aCallStack._Regs.Push(ExternDef(Env, Empty(), Arg, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine)));
				} else if (Proc.Match(tDataType.Def, out tProcDef Def)) {
					aCallStack._Regs.Push(mIL_VM.Proc(Def, Arg));
				} else if (Proc.Match(tDataType.Proc, out tProcDef Def_, out Env)) {
					var Res = Empty();
					aCallStack._Regs.Push(Res);
					return NewCallStack(aCallStack, Def_, Env, Empty(), Arg, Res, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine()));
				} else {
					throw null;
				}
				break;
			}
			case tOpCode.Exec: {
				var Proc = aCallStack._Regs.Get(Arg1);
				var Arg  = aCallStack._Regs.Get(Arg2);
				
				if (Proc.Match(tDataType.ExternDef, out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> ExternDef)) {
					aCallStack._Regs.Push(ExternProc(ExternDef, Arg));
				} else if(Proc.Match(tDataType.ExternProc, out ExternDef, out tData Env)) {
					aCallStack._Regs.Push(ExternDef(Env, aCallStack._Obj, Arg, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine)));
				} else if (Proc.Match(tDataType.Def, out tProcDef Def)) {
					aCallStack._Regs.Push(mIL_VM.Proc(Def, Arg));
				} else if (Proc.Match(tDataType.Proc, out tProcDef Def_, out Env)) {
					var Res = Empty();
					aCallStack._Regs.Push(Res);
					return NewCallStack(aCallStack, Def_, Env, aCallStack._Obj, Arg, Res, aTraceLine => aCallStack._TraceOut(() => "	"+aTraceLine()));
				} else {
					throw null;
				}
				break;
			}
			case tOpCode.ReturnIf: {
				mStd.Assert(aCallStack._Regs.Get(Arg1).Match(tDataType.Bool, out tBool Cond));
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
				mStd.Assert(aCallStack._Regs.Get(Arg1).Match(tDataType.Bool, out tBool Cond));
				if (Cond) {
					aCallStack._Regs = mArrayList.List<tData>(
						Empty(),
						Int(1),
						Bool(false),
						Bool(true),
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
	public static tData
	GetModuleFactory(
		mList.tList<tProcDef> aDefs
	//================================================================================
	) {
		var Env = Empty();
		mStd.Assert(aDefs.Match(out var LastDef, out aDefs));
		while (aDefs.Match(out var DefTemp, out aDefs)) {
			Env = Pair(Env, Def(LastDef));
			LastDef = DefTemp;
		}
		
		return Proc(LastDef, Env);
	}
	
	//================================================================================
	public static void
	Run(
		tData aProc,
		tData aObj,
		tData aArg,
		tData aRes,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		if (aProc.Match(tDataType.Proc, out tProcDef Def, out tData Env)) {
			var CallStack = NewCallStack(null, Def, Env, aObj, aArg, aRes, aTraceOut);
			while (CallStack != null) {
				CallStack = CallStack.Step();
			}
		} else if (
			aProc.Match(
				tDataType.ExternProc,
				out mStd.tFunc<tData, tData, tData, tData> ExternDef,
				out Env
			)
		) {
			var Res = ExternDef(Env, aObj, aArg);
			aRes._DataType = Res._DataType;
			aRes._Value = Res._Value;
		}
	}
	
	#region TEST
	
	//================================================================================
	private static tData
	Add (
		tData aEnv,
		tData aObj,
		tData aArg,
		mStd.tAction<mStd.tFunc<tText>> aTraceOut
	//================================================================================
	) {
		mStd.Assert(aArg.Match(tDataType.Pair, out tData Arg1, out tData Arg2));
		mStd.Assert(Arg1.Match(tDataType.Int, out tInt32 IntArg1));
		mStd.Assert(Arg2.Match(tDataType.Int, out tInt32 IntArg2));
		return Int(IntArg1 + IntArg2);
	}
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_VM),
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
				
				var Env = ExternDef(Add);
				
				var Proc1 = new tProcDef();
				var r1 = Proc1.Pair(tProcDef.cOneReg, tProcDef.cOneReg);
				var r2 = Proc1.Call(tProcDef.cEnvReg, tProcDef.cEmptyReg);
				var r3 = Proc1.Call(r2, r1);
				Proc1.ReturnIf(tProcDef.cTrueReg, r3);
				
				var Res = Empty();
				Run(Proc(Proc1, Env), Empty(), Empty(), Res, TraceOut);
				mStd.AssertEq(Res, Int(2));
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
				
				var Env = ExternDef(Add);
				
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
				
				var Res = Empty();
				Run(Proc(Proc2, Env), Empty(), Empty(), Res, TraceOut);
				mStd.AssertEq(Res, Int(2));
			}
		)
	);
	
	#endregion
}