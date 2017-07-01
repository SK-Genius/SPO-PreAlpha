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
	
	public class tData {
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
		_Value = mStd.Any(mStd.Tuple(aValue1, aValue2))
	};
	
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
	ExternDef(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	//================================================================================
	) => Data(tDataType.ExternDef, false, a);
	
	//================================================================================
	public static tBool
	MATCH(
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
	MATCH<t>(
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
	MATCH<t1, t2>(
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
			aData._Value.Match(out mStd.tTuple<t1, t2> Tuple)
		) {
			Tuple.Match(out aValue1, out aValue2);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	MATCH<t1, t2, t3>(
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
			aData._Value.Match(out mStd.tTuple<t1, t2, t3> Tuple)
		) {
			Tuple.Match(out aValue1, out aValue2, out aValue3);
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
		Assert,
		Call,
		Exec,
		ReturnIf,
		ContinueIf
	}
	
	public class tProcDef {
		// standard stack indexes
		public const tInt32 cEmptyReg = 0;
		public const tInt32 cOneReg   = 1;
		public const tInt32 cFalseReg = 2;
		public const tInt32 cTrueReg  = 3;
		public const tInt32 cEnvReg   = 4;
		public const tInt32 cObjReg   = 5;
		public const tInt32 cArgReg   = 6;
		public const tInt32 cResReg   = 7;
		
		internal readonly mArrayList.tArrayList<mStd.tTuple<tOpCode, tInt32, tInt32>>
			_Commands = mArrayList.List<mStd.tTuple<tOpCode, tInt32, tInt32>>();
		
		private tInt32 _LastReg = 7;
		
		//================================================================================
		internal void
		_AddCommand(
			tOpCode aCommand,
			tInt32 aReg1,
			tInt32 aReg2 = -1
		//================================================================================
		) {
			_Commands.Push(mStd.Tuple(aCommand, aReg1, aReg2));
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
	
	public class tCallStack {
		internal readonly tCallStack _Parent;
		internal mArrayList.tArrayList<tData> _Regs = mArrayList.List<tData>();
		internal readonly tProcDef _ProcDef;
		internal tInt32 _CodePointer = 0;
		internal tData _Obj;
		internal readonly mStd.tAction<mStd.tFunc<tText>> _TraceOut;
		
		//================================================================================
		public tCallStack(
			tCallStack aParent,
			tProcDef aProcDef,
			tData aEnv,
			tData aObj,
			tData aArg,
			tData aRes,
			mStd.tAction<mStd.tFunc<tText>> aTraceOut
		//================================================================================
		) {
			_TraceOut = aTraceOut;
			
			_Parent = aParent;
			_ProcDef = aProcDef;
			
			_Regs = mArrayList.Concat(
				_Regs,
				mArrayList.List(
					Empty(),
					Int(1),
					Bool(false),
					Bool(true),
					aEnv,
					aObj,
					aArg,
					aRes
				)
			);
			aTraceOut(() => "______________________________________");
			aTraceOut(() => " 0 := EMPTY");
			aTraceOut(() => " 1 := 1");
			aTraceOut(() => " 2 := FALSE");
			aTraceOut(() => " 3 := TRUE");
			aTraceOut(() => " 4 := ENV  |  "+aEnv);
			aTraceOut(() => " 5 := OBJ  |  "+aObj);
			aTraceOut(() => " 6 := ARG  |  "+aArg);
			aTraceOut(() => " 7 := RES");
		}
		
		//================================================================================
		public tCallStack
		Step(
		//================================================================================
		) {
			var Command = _ProcDef._Commands.Get(_CodePointer);
			_CodePointer += 1;
			_Obj = Empty();
			
			Command.Match(out var OpCode, out var Arg1, out var Arg2);
			_TraceOut(() => $"{_Regs.Size():#0} := {OpCode} {Arg1} {Arg2}");
			
			switch (OpCode) {
				case tOpCode.NewInt: {
					_Regs.Push(Int(Arg1));
				} break;
				
				case tOpCode.And: {
					var BoolData1 = _Regs.Get(Arg1);
					var BoolData2 = _Regs.Get(Arg2);
					mStd.Assert(BoolData1.MATCH(tDataType.Bool, out tBool Bool1));
					mStd.Assert(BoolData2.MATCH(tDataType.Bool, out tBool Bool2));
					_Regs.Push(Bool(Bool1 & Bool2));
				} break;
				
				case tOpCode.Or: {
					var BoolData1 = _Regs.Get(Arg1);
					var BoolData2 = _Regs.Get(Arg2);
					mStd.Assert(BoolData1.MATCH(tDataType.Bool, out tBool Bool1));
					mStd.Assert(BoolData2.MATCH(tDataType.Bool, out tBool Bool2));
					_Regs.Push(Bool(Bool1 | Bool2));
				} break;
				
				case tOpCode.XOr: {
					var BoolData1 = _Regs.Get(Arg1);
					var BoolData2 = _Regs.Get(Arg2);
					mStd.Assert(BoolData1.MATCH(tDataType.Bool, out tBool Bool1));
					mStd.Assert(BoolData2.MATCH(tDataType.Bool, out tBool Bool2));
					_Regs.Push(Bool(Bool1 ^ Bool2));
				} break;
				
				case tOpCode.IntsAreEq: {
					var IntData1 = _Regs.Get(Arg1);
					var IntData2 = _Regs.Get(Arg2);
					mStd.Assert(IntData1.MATCH(tDataType.Int, out tInt32 Int1));
					mStd.Assert(IntData2.MATCH(tDataType.Int, out tInt32 Int2));
					_Regs.Push(Bool(Int1 == Int2));
				} break;
				
				case tOpCode.IntsComp: {
					var IntData1 = _Regs.Get(Arg1);
					var IntData2 = _Regs.Get(Arg2);
					mStd.Assert(IntData1.MATCH(tDataType.Int, out tInt32 Int1));
					mStd.Assert(IntData2.MATCH(tDataType.Int, out tInt32 Int2));
					var Diff = Int1 - Int2;
					_Regs.Push(Int(Diff < 0 ? -1 : Diff == 0 ? 0 : 1));
				} break;
				
				case tOpCode.IntsAdd: {
					var IntData1 = _Regs.Get(Arg1);
					var IntData2 = _Regs.Get(Arg2);
					mStd.Assert(IntData1.MATCH(tDataType.Int, out tInt32 Int1));
					mStd.Assert(IntData2.MATCH(tDataType.Int, out tInt32 Int2));
					_Regs.Push(Int(Int1 + Int2));
				} break;
				
				case tOpCode.IntsSub: {
					var IntData1 = _Regs.Get(Arg1);
					var IntData2 = _Regs.Get(Arg2);
					mStd.Assert(IntData1.MATCH(tDataType.Int, out tInt32 Int1));
					mStd.Assert(IntData2.MATCH(tDataType.Int, out tInt32 Int2));
					_Regs.Push(Int(Int1 - Int2));
				} break;
				
				case tOpCode.IntsMul: {
					var IntData1 = _Regs.Get(Arg1);
					var IntData2 = _Regs.Get(Arg2);
					mStd.Assert(IntData1.MATCH(tDataType.Int, out tInt32 Int1));
					mStd.Assert(IntData2.MATCH(tDataType.Int, out tInt32 Int2));
					_Regs.Push(Int(Int1 * Int2));
				} break;
				
				case tOpCode.NewPair: {
					_Regs.Push(Pair(_Regs.Get(Arg1), _Regs.Get(Arg2)));
				} break;
				
				case tOpCode.First: {
					mStd.Assert(_Regs.Get(Arg1).MATCH(tDataType.Pair, out tData Var1, out tData Var2));
					_Regs.Push(Var1);
				} break;
				
				case tOpCode.Second: {
					mStd.Assert(_Regs.Get(Arg1).MATCH(tDataType.Pair, out mStd.tTuple<tData, tData> Pair));
					Pair.Match(out var Var1, out var Var2);
					_Regs.Push(Var2);
				} break;
				
				case tOpCode.AddPrefix: {
					_Regs.Push(Prefix(Arg1, _Regs.Get(Arg2)));
				} break;
				
				case tOpCode.DelPrefix: {
					mStd.Assert(_Regs.Get(Arg2).MATCH(tDataType.Prefix, out mStd.tTuple<tInt32, tData> PrefixData));
					PrefixData.Match(out var Prefix, out var Data_);
					mStd.Assert(Prefix.Equals(Arg1));
					_Regs.Push(Data_);
				} break;
				
				case tOpCode.HasPrefix: {
					mStd.Assert(_Regs.Get(Arg2).MATCH(tDataType.Prefix, out mStd.tTuple<tInt32, tData> PrefixData));
					PrefixData.Match(out var Prefix, out var Data_);
					_Regs.Push(Bool(Prefix.Equals(Arg1)));
				} break;
				
				case tOpCode.Assert: {
					if (_Regs.Get(Arg1).MATCH(tDataType.Bool, out tBool Bool) && Bool) {
						mStd.Assert(_Regs.Get(Arg2).MATCH(tDataType.Bool, out Bool) && Bool);
					}
				} break;
				
				case tOpCode.SetObj: {
					_Obj = _Regs.Get(Arg1);
				} break;
				
				case tOpCode.Call: {
					var Proc = _Regs.Get(Arg1);
					var Arg  = _Regs.Get(Arg2);
					
					if (Proc.MATCH(tDataType.ExternDef, out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> ExternDef)) {
						_Regs.Push(ExternProc(ExternDef, Arg));
					} else if(Proc.MATCH(tDataType.ExternProc, out ExternDef, out tData Env)) {
						_Regs.Push(ExternDef(Env, Empty(), Arg, aTraceLine => _TraceOut(() => "	"+aTraceLine)));
					} else if (Proc.MATCH(tDataType.Def, out tProcDef Def)) {
						this._Regs.Push(mIL_VM.Proc(Def, Arg));
					} else if (Proc.MATCH(tDataType.Proc, out tProcDef Def_, out Env)) {
						var Res = Empty();
						_Regs.Push(Res);
						return new tCallStack(this, Def_, Env, Empty(), Arg, Res, aTraceLine => _TraceOut(() => "	"+aTraceLine));
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.Exec: {
					var Proc = _Regs.Get(Arg1);
					var Arg  = _Regs.Get(Arg2);
					
					if (Proc.MATCH(tDataType.ExternDef, out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> ExternDef)) {
						_Regs.Push(ExternProc(ExternDef, Arg));
					} else if(Proc.MATCH(tDataType.ExternProc, out ExternDef, out tData Env)) {
						_Regs.Push(ExternDef(Env, _Obj, Arg, aTraceLine => _TraceOut(() => "	"+aTraceLine)));
					} else if (Proc.MATCH(tDataType.Def, out tProcDef Def)) {
						this._Regs.Push(mIL_VM.Proc(Def, Arg));
					} else if (Proc.MATCH(tDataType.Proc, out tProcDef Def_, out Env)) {
						var Res = Empty();
						_Regs.Push(Res);
						return new tCallStack(this, Def_, Env, _Obj, Arg, Res, aTraceLine => _TraceOut(() => "	"+aTraceLine));
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.ReturnIf: {
					mStd.Assert(_Regs.Get(Arg1).MATCH(tDataType.Bool, out tBool Cond));
					if (Cond) {
						var Src = _Regs.Get(Arg2);
						var Des = _Regs.Get(tProcDef.cResReg);
						Des._DataType = Src._DataType;
						Des._Value = Src._Value;
						_TraceOut(() => "====================================");
						return _Parent;
					}
				} break;
				
				case tOpCode.ContinueIf: {
					mStd.Assert(_Regs.Get(Arg1).MATCH(tDataType.Bool, out tBool Cond));
					if (Cond) {
						_Regs = mArrayList.List<tData>(
							Empty(),
							Int(1),
							Bool(false),
							Bool(true),
							_Regs.Get(tProcDef.cEnvReg),
							_Regs.Get(tProcDef.cObjReg),
							_Regs.Get(Arg2),
							_Regs.Get(tProcDef.cResReg)
						);
						_CodePointer = 0;
					}
				} break;
				
				// TODO: missing IL Command
				// - Create Process
				// - Send Message
				// - Create Var
				// - Pattern Matching (Element e is from Type t?)
				
				default: {
					mStd.Assert(false);
				} break;
			}
			_TraceOut(() => $@"    \ {_Regs.Size()-1} = {_Regs.Get(_Regs.Size()-1)}");
			return this;
		}
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
		if (aProc.MATCH(tDataType.Proc, out tProcDef Def, out tData Env)) {
			var CallStack = new tCallStack(null, Def, Env, aObj, aArg, aRes, aTraceOut);
			while (CallStack != null) {
				CallStack = CallStack.Step();
			}
		} else if (
			aProc.MATCH(
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
	private static readonly mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>>
	Add = (
		aEnv,
		aObj,
		aArg,
		aTraceOut
	//================================================================================
	) => {
		tData Arg1;
		tData Arg2;
		tInt32 Arg1_;
		tInt32 Arg2_;
		mStd.Assert(aArg.MATCH(tDataType.Pair, out Arg1, out Arg2));
		mStd.Assert(Arg1.MATCH(tDataType.Int, out Arg1_));
		mStd.Assert(Arg2.MATCH(tDataType.Int, out Arg2_));
		return Int(Arg1_ + Arg2_);
	};
	
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