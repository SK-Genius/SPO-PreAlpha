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

public static class mVM_Data {
	
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
		
		public readonly mArrayList.tArrayList<(tOpCode, tInt32, tInt32)>
			Commands = mArrayList.List<(tOpCode, tInt32, tInt32)>();
		
		public readonly mVM_Type.tType DefType = mVM_Type.Proc(
			mVM_Type.Empty(),
			mVM_Type.Unknown(),
			mVM_Type.Proc(
				mVM_Type.Unknown(),
				mVM_Type.Unknown(),
				mVM_Type.Unknown()
			)
		);
		
		public readonly mArrayList.tArrayList<mVM_Type.tType>
			Types = mArrayList.List<mVM_Type.tType>();
		
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
	) => aDef.Commands.Push((aCommand, aReg1, aReg2));
	
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
		Var,
		Type
	}
	
	public sealed class tData {
		public tDataType _DataType;
		public mStd.tAny _Value;
		public tBool _IsMutable;
		
		//================================================================================
		public tBool
		Equals(
			tData a
		//================================================================================
		) => (
			_DataType.Equals(a._DataType) &&
			_Value.Equals(a._Value)
		);
		
		override public tBool Equals(object a) => this.Equals((tData)a);
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
	private static tBool
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
	private static tBool
	Match<t>(
		this tData aData,
		tDataType aType,
		out t aValue
	//================================================================================
	) {
		aValue = default;
		return aData._DataType.Equals(aType) && aData._Value.Match(out aValue);
	}
	
	//================================================================================
	private static tBool
	Match<t1, t2>(
		this tData aData,
		tDataType aType,
		out t1 aValue1,
		out t2 aValue2
	//================================================================================
	) {
		aValue1 = default;
		aValue2 = default;
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
	public static tData
	Empty(
	//================================================================================
	) => Data(tDataType.Empty, false, 1);
	
	//================================================================================
	public static tBool
	MatchEmpty(
		this tData aData
	//================================================================================
	) => aData._DataType == tDataType.Empty;
	
	//================================================================================
	public static tData
	Bool(
		tBool aValue
	//================================================================================
	) => Data(tDataType.Bool, false, aValue);
	
	//================================================================================
	public static tBool
	MatchBool(
		this tData aData,
		out tBool aValue
	//================================================================================
	) => aData.Match(tDataType.Bool, out aValue);
	
	//================================================================================
	public static tData
	Int(
		tInt32 aValue
	//================================================================================
	) => Data(tDataType.Int, false, aValue);
	
	//================================================================================
	public static tBool
	MatchInt(
		this tData aData,
		out tInt32 aValue
	//================================================================================
	) => aData.Match(tDataType.Int, out aValue);
	
	//================================================================================
	public static tData
	Pair(
		tData aFirst,
		tData aSecond
	//================================================================================
	) => Data(tDataType.Pair, aFirst._IsMutable || aSecond._IsMutable, aFirst, aSecond);
	
	//================================================================================
	public static tBool
	MatchPair(
		this tData aData,
		out tData aFirst,
		out tData aSecond
	//================================================================================
	) => aData.Match(tDataType.Pair, out aFirst, out aSecond);
	
	//================================================================================
	public static tData
	Tuple(
		params tData[] a
	//================================================================================
	) => mList.List(a).Reverse().Reduce(Empty(), (aList, aItem) => Pair(aItem, aList));
	
	//================================================================================
	public static tData
	Prefix(
		tInt32 aPrefixId,
		tData aData
	//================================================================================
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefixId, aData);
	
	//================================================================================
	public static tBool
	MatchPrefix(
		this tData aData,
		out tInt32 aPrefixId,
		out tData aValue
	//================================================================================
	) => aData.Match(tDataType.Prefix, out aPrefixId, out aValue);
	
	//================================================================================
	public static tBool
	MatchPrefix(
		this tData aData,
		tInt32 aPrefixId,
		out tData aValue
	//================================================================================
	) => aData.MatchPrefix(out var PrefixId, out aValue) && PrefixId == aPrefixId;
	
	//================================================================================
	public static tData
	Prefix(
		tText aPrefix,
		tData aData
	//================================================================================
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefix.GetHashCode(), aData);
	
	//================================================================================
	public static tBool
	MatchPrefix(
		this tData aData,
		tText aPrefix,
		out tData aValue
	//================================================================================
	) => aData.MatchPrefix(aPrefix.GetHashCode(), out aValue);
	
	//================================================================================
	public static tData
	Proc(
		tProcDef aDef,
		tData aEnv
	//================================================================================
	) {
		mDebug.AssertNot(aEnv._IsMutable);
		// In the end this is the place where the compiler will called !!!
		return Data(tDataType.Proc, false, aDef, aEnv);
	}
	
	//================================================================================
	public static tBool
	MatchProc(
		this tData aData,
		out tProcDef aDef,
		out tData aEnv
	//================================================================================
	) => aData.Match(tDataType.Proc, out aDef, out aEnv);
	
	//================================================================================
	public static tData
	ExternProc(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		tData aEnv
	//================================================================================
	) {
		mDebug.AssertNot(aEnv._IsMutable);
		return Data(tDataType.ExternProc, false, aExternDef, aEnv);
	}
	
	//================================================================================
	public static tBool
	MatchExternProc(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		out tData aEnv
	//================================================================================
	) => aData.Match(tDataType.ExternProc, out aExternDef, out aEnv);
	
	//================================================================================
	public static tData
	Def(
		tProcDef aDef
	//================================================================================
	) => Data(tDataType.Def, false, aDef);
	
	//================================================================================
	public static tBool
	MatchDef(
		this tData aData,
		out tProcDef aDef
	//================================================================================
	) => aData.Match(tDataType.Def, out aDef);
	
	//================================================================================
	public static tData
	Var(
		tData aValue
	//================================================================================
	) => Data(tDataType.Var, true, aValue);
	
	//================================================================================
	public static tBool
	MatchVar(
		this tData aData,
		out tData aValue
	//================================================================================
	) => aData.Match(tDataType.Var, out aValue);
	
	//================================================================================
	public static tData
	ExternDef(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	//================================================================================
	) => Data(tDataType.ExternDef, false, a);
	
	//================================================================================
	public static tBool
	MatchExternDef(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	//================================================================================
	) => aData.Match(tDataType.ExternDef, out a);
	
	//================================================================================
	public static tData
	TypeType(
	//================================================================================
	) => Data(tDataType.Type, false, mVM_Type.Type(null));
	
	//================================================================================
	public static tData
	TypeEmpty(
	//================================================================================
	) => Data(tDataType.Type, false, mVM_Type.Empty());
	
	//================================================================================
	public static tData
	TypeBool(
	//================================================================================
	) => Data(tDataType.Type, false, mVM_Type.Bool());
	
	//================================================================================
	public static tData
	TypeInt(
	//================================================================================
	) => Data(tDataType.Type, false, mVM_Type.Int());
	
	//================================================================================
	public static tData
	TypePair(
		mVM_Type.tType aType1,
		mVM_Type.tType aType2
	//================================================================================
	) => Data(tDataType.Type, false, mVM_Type.Pair(aType1, aType2));
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM_Data),
		mTest.Test(
			"TODO: ExternDef",
			aDebugStream => {
				// TODO: Tests
			}
		)
	);
	
	#endregion
}