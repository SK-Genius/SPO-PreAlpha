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
		EMPTY,
		BOOL,
		INT,
		PAIR,
		PREFIX,
		PROC,
		EXTERN_PROC,
		DEF,
		EXTERN_DEF
	}
	
	public class tData {
		internal tDataType _DataType;
		internal mStd.tAny _Value;
		
		//================================================================================
		public tBool Equals(
			tData a
		//================================================================================
		) {
			return (
				!a.IsNull() &&
				_DataType.Equals(a._DataType) &&
				_Value.Equals(a._Value)
			);
		}
		
		public override tBool Equals(object a) { return Equals(a as tData); }
		public override tText ToString() { return "("+_DataType+" "+_Value; }
	}
	
	//================================================================================
	public static tData
	Data<t>(
		tDataType aType,
		t aValue
	//================================================================================
	) {
		return new tData{_DataType = aType, _Value = mStd.Any(aValue)};
	}
	
	//================================================================================
	public static tData
	Data<t1, t2>(
		tDataType aType,
		t1 aValue1,
		t2 aValue2
	//================================================================================
	) {
		return new tData{
			_DataType = aType,
			_Value = mStd.Any(mStd.Tuple(aValue1, aValue2))
		};
	}
	
	//================================================================================
	public static tData
	Data<t1, t2, t3>(
		tDataType aType,
		t1 aValue1,
		t2 aValue2,
		t3 aValue3
	//================================================================================
	) {
		return new tData{
			_DataType = aType,
			_Value = mStd.Any(mStd.Tuple(aValue1, aValue2, aValue3))
		};
	}
	
	//================================================================================
	public static tData
	EMPTY(
	//================================================================================
	) {
		return Data(tDataType.EMPTY, 1);
	}
	
	//================================================================================
	public static tData
	BOOL(
		tBool a
	//================================================================================
	) {
		return Data(tDataType.BOOL, a);
	}
	
	//================================================================================
	public static tData
	INT(
		tInt32 a
	//================================================================================
	) {
		return Data(tDataType.INT, a);
	}
	
	//================================================================================
	public static tData
	PAIR(
		tData a1,
		tData a2
	//================================================================================
	) {
		return Data(tDataType.PAIR, a1, a2);
	}
	
	//================================================================================
	public static tData
	PREFIX(
		tText a1,
		tData a2
	//================================================================================
	) {
		return Data(tDataType.PREFIX, a1.GetHashCode(), a2);
	}
	
	//================================================================================
	public static tData
	PROC(
		tProcDef a1,
		tData a2
	//================================================================================
	) {
		return Data(tDataType.PROC, a1, a2);
	}
	
	//================================================================================
	public static tData
	EXTERN_PROC(
		mStd.tFunc<tData, tData, tData, tData> a1,
		tData a2
	//================================================================================
	) {
		return Data(tDataType.EXTERN_PROC, a1, a2);
	}
	
	//================================================================================
	public static tData
	DEF(
		tProcDef a
	//================================================================================
	) {
		return Data(tDataType.DEF, a);
	}
	
	//================================================================================
	public static tData
	EXTERN_DEF(
		mStd.tFunc<tData, tData, tData, tData> a
	//================================================================================
	) {
		return Data(tDataType.EXTERN_DEF, a);
	}
	
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
		return aData._DataType.Equals(aType) && aData._Value.MATCH(out aValue);
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
		mStd.tTuple<t1, t2> Tuple;
		aValue1 = default(t1);
		aValue2 = default(t2);
		return (
			aData._DataType.Equals(aType) &&
			aData._Value.MATCH(out Tuple) &&
			Tuple.MATCH(out aValue1, out aValue2)
		);
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
		mStd.tTuple<t1, t2, t3> Tuple;
		aValue1 = default(t1);
		aValue2 = default(t2);
		aValue3 = default(t3);
		return (
			aData._DataType.Equals(aType) &&
			aData._Value.MATCH(out Tuple) &&
			Tuple.MATCH(out aValue1, out aValue2, out aValue3)
		);
	}
	
	#endregion
	
	public enum tOpCode {
		NEW_PAIR,
		FIRST,
		SECOND,
		ADD_PREFIX,
		DEL_PREFIX,
		SET_OBJ,
		ASSERT,
		CALL,
		RETURN_IF,
		CONTUNUE_IF
	}
	
	public class tProcDef {
		// standard stack indexes
		public const tInt32 EMPTY = 0;
		public const tInt32 ONE   = 1;
		public const tInt32 FALSE = 2;
		public const tInt32 TRUE  = 3;
		public const tInt32 ENV   = 4;
		public const tInt32 OBJ   = 5;
		public const tInt32 ARG   = 6;
		public const tInt32 RES   = 7;
		
		internal System.Collections.Generic.List<mStd.tTuple<tOpCode, tInt32, tInt32>>
			_Commands = new System.Collections.Generic.List<mStd.tTuple<tOpCode, tInt32, tInt32>>();
		
		internal tInt32 _LastReg = 7;
		
		//================================================================================
		internal void
		_AddCommand(
			tOpCode Command,
			tInt32 a1,
			tInt32 a2 = -1
		//================================================================================
		) {
			_Commands.Add(mStd.Tuple(Command, a1, a2));
		}
		
		//================================================================================
		internal tInt32
		_AddReg(
			tOpCode Command,
			tInt32 a1,
			tInt32 a2 = -1
		//================================================================================
		) {
			_AddCommand(Command, a1, a2);
			_LastReg += 1;
			return _LastReg;
		}
		
		//================================================================================
		public tInt32
		Pair(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) {
			return _AddReg(tOpCode.NEW_PAIR, a1, a2);
		}
		
		//================================================================================
		public tInt32
		First(
			tInt32 a
		//================================================================================
		) {
			return _AddReg(tOpCode.FIRST, a);
		}
		
		//================================================================================
		public tInt32
		Second(
			tInt32 a
		//================================================================================
		) {
			return _AddReg(tOpCode.SECOND, a);
		}
		
		//================================================================================
		public tInt32
		AddPrefix(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) {
			return _AddReg(tOpCode.ADD_PREFIX, a1, a2);
		}
		
		//================================================================================
		public tInt32
		DelPrefix(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) {
			return _AddReg(tOpCode.DEL_PREFIX, a1, a2);
		}
		
		//================================================================================
		public void
		SetObj(
			tInt32 Obj
		//================================================================================
		) {
			_AddCommand(tOpCode.SET_OBJ, Obj);
		}
		
		//================================================================================
		public tInt32
		Call(
			tInt32 Proc,
			tInt32 Arg
		//================================================================================
		) {
			return _AddReg(tOpCode.CALL, Proc, Arg);
		}
		
		//================================================================================
		public void
		ReturnIf(
			tInt32 Cond,
			tInt32 Res
		//================================================================================
		) {
			_AddCommand(tOpCode.RETURN_IF, Cond, Res);
		}
		
		//================================================================================
		public void
		ContinueIf(
			tInt32 Cond,
			tInt32 Arg
		//================================================================================
		) {
			_AddCommand(tOpCode.CONTUNUE_IF, Cond, Arg);
		}
		
		//================================================================================
		public void
		Assert(
			tInt32 PreCond,
			tInt32 PostCond
		//================================================================================
		) {
			_AddCommand(tOpCode.ASSERT, PreCond, PostCond);
		}
	}
	
	public class tCallStack {
		internal tCallStack _Parent;
		internal mList.tList<tData> _Reg = mList.List<tData>();
		internal tProcDef _ProcDef;
		internal tInt32 _CodePointer = 0;
		internal tData _Obj;
		
		//================================================================================
		public tCallStack(
			tCallStack Parent,
			tProcDef ProcDef,
			tData Env,
			tData Obj,
			tData Arg,
			tData Res
		//================================================================================
		) {
			_Parent = Parent;
			_ProcDef = ProcDef;
			
			_Reg = mList.Concat(
				_Reg,
				mList.List(
					EMPTY(),
					INT(1),
					BOOL(false),
					BOOL(true),
					Env,
					Obj,
					Arg,
					Res
				)
			);
		}
		
		//================================================================================
		public tCallStack
		Step(
		//================================================================================
		) {
			var Command = _ProcDef._Commands[_CodePointer];
			_CodePointer += 1;
			_Obj = EMPTY();
			
			tOpCode OpCode;
			tInt32 Arg1;
			tInt32 Arg2;
			Command.MATCH(out OpCode, out Arg1, out Arg2);
			
			switch (OpCode) {
				case tOpCode.NEW_PAIR: {
						_Reg = mList.Concat(_Reg, mList.List(PAIR(_Reg.Skip(Arg1)._Head, _Reg.Skip(Arg2)._Head)));
				} break;
				
				case tOpCode.FIRST: {
					tData Var1;
					tData Var2;
					if (_Reg.Skip(Arg1)._Head.MATCH(tDataType.PAIR, out Var1, out Var2)
					) {
						_Reg = mList.Concat(_Reg, mList.List(Var1));
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.SECOND: {
					mStd.tTuple<tData, tData> Pair;
					tData Var1;
					tData Var2;
					if (
						_Reg.Skip(Arg1)._Head.MATCH(tDataType.PAIR, out Pair) &&
						Pair.MATCH(out Var1, out Var2)
					) {
						_Reg = mList.Concat(_Reg, mList.List(Var2));
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.ADD_PREFIX: {
					_Reg = mList.Concat(_Reg, mList.List((Data(tDataType.PREFIX, mStd.Tuple(Arg1, _Reg.Skip(Arg2)._Head)))));
				} break;
				
				case tOpCode.DEL_PREFIX: {
					mStd.tTuple<tInt32, tData> PrefixData;
					tData Data;
					tInt32 Prefix;
					if (
						_Reg.Skip(Arg2)._Head.MATCH(tDataType.PREFIX, out PrefixData) &&
						PrefixData.MATCH(out Prefix, out Data) &&
						Prefix.Equals(Arg1)
					) {
						_Reg = mList.Concat(_Reg, mList.List(Data));
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.ASSERT: {
					tBool Bool;
					if (_Reg.Skip(Arg1)._Head.MATCH(tDataType.BOOL, out Bool) && Bool) {
						mStd.Assert(_Reg.Skip(Arg2)._Head.MATCH(tDataType.BOOL, out Bool) && Bool);
					}
				} break;
				
				case tOpCode.SET_OBJ: {
					_Obj = _Reg.Skip(Arg1)._Head;
				} break;
				
				case tOpCode.CALL: {
					var Proc = _Reg.Skip(Arg1)._Head;
					var Arg  = _Reg.Skip(Arg2)._Head;
					
					mStd.tFunc<tData, tData, tData, tData> ExternDef;
					tProcDef Def;
					tData Env;
					
					if (Proc.MATCH(tDataType.EXTERN_DEF, out ExternDef)) {
						_Reg = mList.Concat(_Reg, mList.List(EXTERN_PROC(ExternDef, Arg)));
					} else if(Proc.MATCH(tDataType.EXTERN_PROC, out ExternDef, out Env)) {
						_Reg = mList.Concat(_Reg, mList.List(ExternDef(Env, _Obj, Arg)));
					} else if (Proc.MATCH(tDataType.DEF, out Def)) {
						_Reg = mList.Concat(_Reg, mList.List(PROC(Def, Arg)));
					} else if (Proc.MATCH(tDataType.PROC, out Def, out Env)) {
						var Res = EMPTY();
						_Reg = mList.Concat(_Reg, mList.List(Res));
						return new tCallStack(this, Def, Env, _Obj, Arg, Res);
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.RETURN_IF: {
					tBool Cond;
					mStd.Assert(_Reg.Skip(Arg1)._Head.MATCH(tDataType.BOOL, out Cond));
					if (Cond) {
						var Src = _Reg.Skip(Arg2)._Head;
						var Des = _Reg.Skip(tProcDef.RES)._Head;
						Des._DataType = Src._DataType;
						Des._Value = Src._Value;
						return _Parent;
					}
				} break;
				
				case tOpCode.CONTUNUE_IF: {
					tBool Cond;
					mStd.Assert(_Reg.Skip(Arg1)._Head.MATCH(tDataType.BOOL, out Cond));
					if (Cond) {
						_Reg = mList.List<tData>(
							EMPTY(),
							INT(1),
							BOOL(false),
							BOOL(true),
							_Reg.Skip(tProcDef.ENV)._Head,
							_Reg.Skip(tProcDef.OBJ)._Head,
							_Reg.Skip(Arg2)._Head,
							_Reg.Skip(tProcDef.RES)._Head
						);
						_CodePointer = 0;
					}
				} break;
				
				default: {
					mStd.Assert(false);
				} break;
			}
			return this;
		}
	}
	
	#region TEST
	//TODO: test First, Second, AddPrefix, DelPrefix, Assert ...
	
	//================================================================================
	private static readonly mStd.tFunc<tData, tData, tData, tData>
	Add = (
		tData Env,
		tData Obj,
		tData Arg
	//================================================================================
	) => {
		tData Arg1;
		tData Arg2;
		mStd.Assert(Arg.MATCH(tDataType.PAIR, out Arg1, out Arg2));
		tInt32 Arg1_;
		mStd.Assert(Arg1.MATCH(tDataType.INT, out Arg1_));
		tInt32 Arg2_;
		mStd.Assert(Arg2.MATCH(tDataType.INT, out Arg2_));
		return INT(Arg1_ + Arg2_);
	};
	
	public static mStd.tFunc<tBool, mStd.tAction<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"ExternDef",
			mStd.Func(
				(mStd.tAction<tText> aStreamOut) => {
					var Env = EXTERN_DEF(Add);
					
					var Proc1 = new tProcDef();
					var r1 = Proc1.Pair(tProcDef.ONE, tProcDef.ONE);
					var r2 = Proc1.Call(tProcDef.ENV, tProcDef.EMPTY);
					var r3 = Proc1.Call(r2, r1);
					Proc1.ReturnIf(tProcDef.TRUE, r3);
					
					var Res = EMPTY();
					var CallStack = new tCallStack(null, Proc1, Env, EMPTY(), EMPTY(), Res);
					while (CallStack.Step() != null) { }
					mStd.AssertEq(CallStack._Reg.Skip(tProcDef.RES)._Head, INT(2));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"InternDef",
			mStd.Func(
				(mStd.tAction<tText> aStreamOut) => {
					var Env = EXTERN_DEF(Add);
					
					var Proc2 = new tProcDef();
					Proc2.ReturnIf(
						tProcDef.TRUE,
						Proc2.Call(
							Proc2.Call(
								tProcDef.ENV,
								tProcDef.EMPTY
							),
							Proc2.Pair(
								tProcDef.ONE,
								tProcDef.ONE
							)
						)
					);
					
					var Res = EMPTY();
					var CallStack = new tCallStack(null, Proc2, Env, EMPTY(), EMPTY(), Res);
					while (CallStack != null) {
						CallStack = CallStack.Step();
					}
					mStd.AssertEq(Res, INT(2));
					
					return true;
				}
			)
		)
		// TODO: viele Tests
	);
		
	#endregion
	
}