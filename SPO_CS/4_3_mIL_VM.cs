using System.Reflection;
using System.Text.RegularExpressions;
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
		) => (
			!a.IsNull() &&
			_DataType.Equals(a._DataType) &&
			_Value.Equals(a._Value)
		);
		
		public override tBool Equals(object a) => Equals(a as tData);
		public override tText ToString() => $"({_DataType} {_Value})";
	}
	
	//================================================================================
	public static tData
	Data<t>(
		tDataType aType,
		t aValue
	//================================================================================
	) => new tData{_DataType = aType, _Value = mStd.Any(aValue)};
	
	//================================================================================
	public static tData
	Data<t1, t2>(
		tDataType aType,
		t1 aValue1,
		t2 aValue2
	//================================================================================
	) => new tData{
		_DataType = aType,
		_Value = mStd.Any(mStd.Tuple(aValue1, aValue2))
	};
	
	//================================================================================
	public static tData
	Data<t1, t2, t3>(
		tDataType aType,
		t1 aValue1,
		t2 aValue2,
		t3 aValue3
	//================================================================================
	) => new tData{
		_DataType = aType,
		_Value = mStd.Any(mStd.Tuple(aValue1, aValue2, aValue3))
	};
	
	//================================================================================
	public static tData
	EMPTY(
	//================================================================================
	) => Data(tDataType.EMPTY, 1);
	
	//================================================================================
	public static tData
	BOOL(
		tBool a
	//================================================================================
	) => Data(tDataType.BOOL, a);
	
	//================================================================================
	public static tData
	INT(
		tInt32 a
	//================================================================================
	) => Data(tDataType.INT, a);
	
	//================================================================================
	public static tData
	PAIR(
		tData a1,
		tData a2
	//================================================================================
	) => Data(tDataType.PAIR, a1, a2);
	
	//================================================================================
	public static tData
	TUPLE(
		params tData[] a
	//================================================================================
	) => mList.List(a).Reverse().Reduce(EMPTY(), (aList, aItem) => PAIR(aItem, aList));
	
	//================================================================================
	public static tData
	PREFIX(
		tText a1,
		tData a2
	//================================================================================
	) => Data(tDataType.PREFIX, a1.GetHashCode(), a2);
	
	//================================================================================
	public static tData
	PROC(
		tProcDef a1,
		tData a2
	//================================================================================
	) => Data(tDataType.PROC, a1, a2); // In the end this is the place where the compiler will called !!!
	
	//================================================================================
	public static tData
	EXTERN_PROC(
		mStd.tFunc<tData, tData, tData, tData> a1,
		tData a2
	//================================================================================
	) => Data(tDataType.EXTERN_PROC, a1, a2);
	
	//================================================================================
	public static tData
	DEF(
		tProcDef a
	//================================================================================
	) => Data(tDataType.DEF, a);
	
	//================================================================================
	public static tData
	EXTERN_DEF(
		mStd.tFunc<tData, tData, tData, tData> a
	//================================================================================
	) => Data(tDataType.EXTERN_DEF, a);
	
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
		aValue1 = default(t1);
		aValue2 = default(t2);
		if (
			aData._DataType.Equals(aType) &&
			aData._Value.MATCH(out mStd.tTuple<t1, t2> Tuple)
		) {
			Tuple.MATCH(out aValue1, out aValue2);
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
			aData._Value.MATCH(out mStd.tTuple<t1, t2, t3> Tuple)
		) {
			Tuple.MATCH(out aValue1, out aValue2, out aValue3);
			return true;
		}
		return false;
	}
	
	#endregion
	
	public enum tOpCode {
		NEW_INT,
		NEW_PAIR,
		FIRST,
		SECOND,
		ADD_PREFIX,
		DEL_PREFIX,
		HAS_PREFIX,
		SET_OBJ,
		ASSERT,
		CALL,
		RETURN_IF,
		CONTUNUE_IF
	}
	
	public class tProcDef {
		// standard stack indexes
		public const tInt32 EMPTY_Reg = 0;
		public const tInt32 ONE_Reg   = 1;
		public const tInt32 FALSE_Reg = 2;
		public const tInt32 TRUE_Reg  = 3;
		public const tInt32 ENV_Reg   = 4;
		public const tInt32 OBJ_Reg   = 5;
		public const tInt32 ARG_Reg   = 6;
		public const tInt32 RES_Reg   = 7;
		
		internal mArrayList.tArrayList<mStd.tTuple<tOpCode, tInt32, tInt32>>
			_Commands = mArrayList.List<mStd.tTuple<tOpCode, tInt32, tInt32>>();
		
		internal tInt32 _LastReg = 7;
		
		//================================================================================
		internal void
		_AddCommand(
			tOpCode Command,
			tInt32 a1,
			tInt32 a2 = -1
		//================================================================================
		) {
			_Commands.Push(mStd.Tuple(Command, a1, a2));
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
		Int(
			tInt32 a1
		//================================================================================
		) => _AddReg(tOpCode.NEW_INT, a1);
		
		//================================================================================
		public tInt32
		Pair(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) => _AddReg(tOpCode.NEW_PAIR, a1, a2);
		
		//================================================================================
		public tInt32
		First(
			tInt32 a
		//================================================================================
		) => _AddReg(tOpCode.FIRST, a);
		
		//================================================================================
		public tInt32
		Second(
			tInt32 a
		//================================================================================
		) => _AddReg(tOpCode.SECOND, a);
		
		//================================================================================
		public tInt32
		AddPrefix(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) => _AddReg(tOpCode.ADD_PREFIX, a1, a2);
		
		//================================================================================
		public tInt32
		DelPrefix(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) => _AddReg(tOpCode.DEL_PREFIX, a1, a2);
		
		//================================================================================
		public tInt32
		HasPrefix(
			tInt32 a1,
			tInt32 a2
		//================================================================================
		) => _AddReg(tOpCode.HAS_PREFIX, a1, a2);
		
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
		) => _AddReg(tOpCode.CALL, Proc, Arg);
		
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
		internal mArrayList.tArrayList<tData> _Reg = mArrayList.List<tData>();
		internal tProcDef _ProcDef;
		internal tInt32 _CodePointer = 0;
		internal tData _Obj;
		
		//================================================================================
		public tCallStack(
			tCallStack aParent,
			tProcDef aProcDef,
			tData aEnv,
			tData aObj,
			tData aArg,
			tData aRes
		//================================================================================
		) {
			_Parent = aParent;
			_ProcDef = aProcDef;
			
			_Reg = mArrayList.Concat(
				_Reg,
				mArrayList.List(
					EMPTY(),
					INT(1),
					BOOL(false),
					BOOL(true),
					aEnv,
					aObj,
					aArg,
					aRes
				)
			);
			System.Diagnostics.Debug.WriteLine("    ______________________________________");
			System.Diagnostics.Debug.WriteLine("     0 := EMPTY");
			System.Diagnostics.Debug.WriteLine("     1 := 1");
			System.Diagnostics.Debug.WriteLine("     2 := FALSE");
			System.Diagnostics.Debug.WriteLine("     3 := TRUE");
			System.Diagnostics.Debug.WriteLine("     4 := ENV  |  "+aEnv);
			System.Diagnostics.Debug.WriteLine("     5 := OBJ  |  "+aObj);
			System.Diagnostics.Debug.WriteLine("     6 := ARG  |  "+aArg);
			System.Diagnostics.Debug.WriteLine("     7 := RES");
		}
		
		//================================================================================
		public tCallStack
		Step(
		//================================================================================
		) {
			var Command = _ProcDef._Commands.Get(_CodePointer);
			_CodePointer += 1;
			_Obj = EMPTY();
			
			Command.MATCH(out var OpCode, out var Arg1, out var Arg2);
			System.Diagnostics.Debug.Write($"    {_Reg.Size():#0} := {OpCode} {Arg1} {Arg2}");
			
			switch (OpCode) {
				case tOpCode.NEW_INT: {
					_Reg.Push(INT(Arg1));
				} break;
				
				case tOpCode.NEW_PAIR: {
					_Reg.Push(PAIR(_Reg.Get(Arg1), _Reg.Get(Arg2)));
				} break;
				
				case tOpCode.FIRST: {
					mStd.Assert(_Reg.Get(Arg1).MATCH(tDataType.PAIR, out tData Var1, out tData Var2));
					_Reg.Push(Var1);
				} break;
				
				case tOpCode.SECOND: {
					mStd.Assert(_Reg.Get(Arg1).MATCH(tDataType.PAIR, out mStd.tTuple<tData, tData> Pair));
					Pair.MATCH(out var Var1, out var Var2);
					_Reg.Push(Var2);
				} break;
				
				case tOpCode.ADD_PREFIX: {
					_Reg.Push(Data(tDataType.PREFIX, mStd.Tuple(Arg1, _Reg.Get(Arg2))));
				} break;
				
				case tOpCode.DEL_PREFIX: {
					mStd.Assert(_Reg.Get(Arg2).MATCH(tDataType.PREFIX, out mStd.tTuple<tInt32, tData> PrefixData));
					PrefixData.MATCH(out var Prefix, out var Data_);
					mStd.Assert(Prefix.Equals(Arg1));
					_Reg.Push(Data_);
				} break;
				
				case tOpCode.HAS_PREFIX: {
					mStd.Assert(_Reg.Get(Arg2).MATCH(tDataType.PREFIX, out mStd.tTuple<tInt32, tData> PrefixData));
					PrefixData.MATCH(out var Prefix, out var Data_);
					_Reg.Push(mIL_VM.BOOL(Prefix.Equals(Arg1)));
				} break;
				
				case tOpCode.ASSERT: {
					if (_Reg.Get(Arg1).MATCH(tDataType.BOOL, out tBool Bool) && Bool) {
						mStd.Assert(_Reg.Get(Arg2).MATCH(tDataType.BOOL, out Bool) && Bool);
					}
				} break;
				
				case tOpCode.SET_OBJ: {
					_Obj = _Reg.Get(Arg1);
				} break;
				
				case tOpCode.CALL: {
					var Proc = _Reg.Get(Arg1);
					var Arg  = _Reg.Get(Arg2);
					
					if (Proc.MATCH(tDataType.EXTERN_DEF, out mStd.tFunc<tData, tData, tData, tData> ExternDef)) {
						_Reg.Push(EXTERN_PROC(ExternDef, Arg));
					} else if(Proc.MATCH(tDataType.EXTERN_PROC, out ExternDef, out tData Env)) {
						_Reg.Push(ExternDef(Env, _Obj, Arg));
					} else if (Proc.MATCH(tDataType.DEF, out tProcDef Def)) {
						_Reg.Push(PROC(Def, Arg));
					} else if (Proc.MATCH(tDataType.PROC, out tProcDef Def_, out Env)) {
						var Res = EMPTY();
						_Reg.Push(Res);
						System.Diagnostics.Debug.WriteLine("");
						return new tCallStack(this, Def_, Env, _Obj, Arg, Res);
					} else {
						mStd.Assert(false);
					}
				} break;
				
				case tOpCode.RETURN_IF: {
					mStd.Assert(_Reg.Get(Arg1).MATCH(tDataType.BOOL, out tBool Cond));
					if (Cond) {
						var Src = _Reg.Get(Arg2);
						var Des = _Reg.Get(tProcDef.RES_Reg);
						Des._DataType = Src._DataType;
						Des._Value = Src._Value;
						System.Diagnostics.Debug.WriteLine("");
						return _Parent;
					}
				} break;
				
				case tOpCode.CONTUNUE_IF: {
					mStd.Assert(_Reg.Get(Arg1).MATCH(tDataType.BOOL, out tBool Cond));
					if (Cond) {
						_Reg = mArrayList.List<tData>(
							EMPTY(),
							INT(1),
							BOOL(false),
							BOOL(true),
							_Reg.Get(tProcDef.ENV_Reg),
							_Reg.Get(tProcDef.OBJ_Reg),
							_Reg.Get(Arg2),
							_Reg.Get(tProcDef.RES_Reg)
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
			System.Diagnostics.Debug.WriteLine($"  |  {_Reg.Size()-1} = {_Reg.Get(_Reg.Size()-1)}");
			return this;
		}
	}
	
	//================================================================================
	public static tData
	GetModuleFactory(
		mList.tList<tProcDef> aDefs
	//================================================================================
	) {
		var Env = EMPTY();
		mStd.Assert(aDefs.MATCH(out var LastDef, out aDefs));
		while (aDefs.MATCH(out var DefTemp, out aDefs)) {
			Env = PAIR(Env, DEF(LastDef));
			LastDef = DefTemp;
		}
		
		return PROC(LastDef, Env);
	}
	
	//================================================================================
	public static void
	Run(
		tData aProc,
		tData aObj,
		tData aArg,
		tData aRes
	//================================================================================
	) {
		if (aProc.MATCH(tDataType.PROC, out tProcDef Def, out tData Env)) {
			var CallStack = new tCallStack(null, Def, Env, aObj, aArg, aRes);
			while (CallStack != null) {
				CallStack = CallStack.Step();
			}
		} else if (aProc.MATCH(tDataType.EXTERN_PROC, out mStd.tFunc<tData, tData, tData, tData> ExternDef, out Env)) {
			var Res = ExternDef(Env, aObj, aArg);
			aRes._DataType = Res._DataType;
			aRes._Value = Res._Value;
		}
	}
	
	#region TEST
	//TODO: add tests (First, Second, AddPrefix, DelPrefix, Assert, ...)
	
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
		tInt32 Arg1_;
		tInt32 Arg2_;
		mStd.Assert(Arg.MATCH(tDataType.PAIR, out Arg1, out Arg2));
		mStd.Assert(Arg1.MATCH(tDataType.INT, out Arg1_));
		mStd.Assert(Arg2.MATCH(tDataType.INT, out Arg2_));
		return INT(Arg1_ + Arg2_);
	};
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"ExternDef",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var Env = EXTERN_DEF(Add);
					
					var Proc1 = new tProcDef();
					var r1 = Proc1.Pair(tProcDef.ONE_Reg, tProcDef.ONE_Reg);
					var r2 = Proc1.Call(tProcDef.ENV_Reg, tProcDef.EMPTY_Reg);
					var r3 = Proc1.Call(r2, r1);
					Proc1.ReturnIf(tProcDef.TRUE_Reg, r3);
					
					var Res = EMPTY();
					Run(PROC(Proc1, Env), EMPTY(), EMPTY(), Res);
					mStd.AssertEq(Res, INT(2));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"InternDef",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var Env = EXTERN_DEF(Add);
					
					var Proc2 = new tProcDef();
					Proc2.ReturnIf(
						tProcDef.TRUE_Reg,
						Proc2.Call(
							Proc2.Call(
								tProcDef.ENV_Reg,
								tProcDef.EMPTY_Reg
							),
							Proc2.Pair(
								tProcDef.ONE_Reg,
								tProcDef.ONE_Reg
							)
						)
					);
					
					var Res = EMPTY();
					Run(PROC(Proc2, Env), EMPTY(), EMPTY(), Res);
					mStd.AssertEq(Res, INT(2));
					
					return true;
				}
			)
		)
		// TODO: add tests
	);
	
	#endregion
	
}