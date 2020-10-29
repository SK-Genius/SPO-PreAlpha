//IMPORT mVM_Type.cs
//IMPORT mAny.cs

#nullable enable

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

public static class
mVM_Data {
	
	public enum
	tOpCode {
		// BOOL
		IsBool,
		And,
		Or,
		XOr,
		
		// INT
		IsInt,
		NewInt,
		IntsAreEq,
		IntsComp,
		IntsAdd,
		IntsSub,
		IntsMul,
		
		// PAIR
		IsPair,
		NewPair,
		First,
		Second,
		
		// PREFIX
		IsPrefix,
		AddPrefix,
		DelPrefix,
		HasPrefix,
		
		// RECORD
		IsRecord,
		ExtendRec,
		DivideRec,
		
		// VAR
		IsVar,
		VarDef,
		VarSet,
		VarGet,
		
		// JUMP
		CallFunc,
		CallProc,
		ReturnIf,
		ContinueIf,
		TailCallIf,
		
		// ASSERT
		Assert,
		
		// TYPE
		IsType,
		TypeEmpty,
		TypeAny,
		TypeInt,
		TypeFree,
		TypePair,
		TypePrefix,
		TypeRecord,
		TypeVar,
		TypeSet,
		TypeCond,
		TypeFunc,
		TypeMeth,
		TypeRecursive,
		TypeInterface,
		TypeGeneric,
	}
	
	public interface
	tProcDef {
		tText FirstPosText { get; }
	}
	
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
	
	[System.Diagnostics.DebuggerDisplay("{this.DefType.ToText(10)}")]
	public sealed class
	tProcDef<tPos> : tProcDef {
		public readonly mArrayList.tArrayList<(tOpCode, tInt32, tInt32)>
			Commands = mArrayList.List<(tOpCode, tInt32, tInt32)>();
		
		public readonly mArrayList.tArrayList<tPos>
			PosList = mArrayList.List<tPos>();
		
		public readonly mVM_Type.tType DefType = mVM_Type.Proc(
			mVM_Type.Empty(),
			mVM_Type.Free("§ENV"),
			mVM_Type.Proc(
				mVM_Type.Free("§OBJ"),
				mVM_Type.Free("§ARG"),
				mVM_Type.Free("§RES")
			)
		);
		
		public readonly mArrayList.tArrayList<mVM_Type.tType>
		Types = mArrayList.List<mVM_Type.tType>();
		
		public tInt32 _LastReg = cResReg;
		
		public string FirstPosText => "" + this.PosList.ToStream().First()._Value;
	}
	
	internal static void
	_AddCommand<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tInt32 aReg1
	) => aDef._AddCommand(aPos, aCommand, aReg1, -1);
	
	internal static void
	_AddCommand<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tInt32 aReg1,
		tInt32 aReg2
	) {
		aDef.PosList.Push(aPos);
		aDef.Commands.Push((aCommand, aReg1, aReg2));
	}
	
	internal static tInt32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand
	) => aDef._AddReg(aPos, aCommand, -1, -1);
	
	internal static tInt32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tInt32 aReg1
	) => aDef._AddReg(aPos, aCommand, aReg1, -1);
	
	internal static tInt32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tInt32 aReg1,
		tInt32 aReg2
	) {
		aDef._AddCommand(aPos, aCommand, aReg1, aReg2);
		aDef._LastReg += 1;
		return aDef._LastReg;
	}
	
	public static tInt32
	IsBool<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsBool, aValue);
	
	public static tInt32
	And<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aBoolReg1,
		tInt32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.And, aBoolReg1, aBoolReg2);
	
	public static tInt32
	Or<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aBoolReg1,
		tInt32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.Or, aBoolReg1, aBoolReg2);
	
	public static tInt32
	XOr<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aBoolReg1,
		tInt32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.XOr, aBoolReg1, aBoolReg2);
	
	public static tInt32
	IsInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsInt, aValue);
	
	public static tInt32
	Int<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntValue
	) => aDef._AddReg(aPos, tOpCode.NewInt, aIntValue);
	
	public static  tInt32
	IntsAreEq<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsAreEq, aIntReg1, aIntReg2);
	
	public static tInt32
	IntsComp<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsComp, aIntReg1, aIntReg2);
	
	public static tInt32
	IntsAdd<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsAdd, aIntReg1, aIntReg2);
	
	public static tInt32
	IntsSub<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsSub, aIntReg1, aIntReg2);
	
	public static tInt32
	IntsMul<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntReg1,
		tInt32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsMul, aIntReg1, aIntReg2);
	
	public static tInt32
	IsPair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsPair, aValue);
	
	public static tInt32
	Pair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aDataReg1,
		tInt32 aDataReg2
	) => aDef._AddReg(aPos, tOpCode.NewPair, aDataReg1, aDataReg2);
	
	public static tInt32
	First<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPairReg
	) => aDef._AddReg(aPos, tOpCode.First, aPairReg);
	
	public static tInt32
	Second<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPairReg
	) => aDef._AddReg(aPos, tOpCode.Second, aPairReg);
	
	public static tInt32
	IsPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsPrefix, aValue);
	
	public static tInt32
	AddPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPrefixId,
		tInt32 aDataReg
	) => aDef._AddReg(aPos, tOpCode.AddPrefix, aPrefixId, aDataReg);
	
	public static tInt32
	DelPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPrefixId,
		tInt32 aReg
	) => aDef._AddReg(aPos, tOpCode.DelPrefix, aPrefixId, aReg);
	
	public static tInt32
	HasPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPrefixId,
		tInt32 aDataReg
	) => aDef._AddReg(aPos, tOpCode.HasPrefix, aPrefixId, aDataReg);
	
	public static tInt32
	IsRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsRecord, aValue);
	
	public static tInt32
	ExtendRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aRecReg,
		tInt32 aPrefixReg
	) => aDef._AddReg(aPos, tOpCode.ExtendRec, aRecReg, aPrefixReg);
	
	public static tInt32
	DivideRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aRecReg
	) => aDef._AddReg(aPos, tOpCode.DivideRec, aRecReg);
	
	public static tInt32
	ExtendRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aRecReg
	) => aDef._AddReg(aPos, tOpCode.DivideRec, aRecReg);
	
	public static tInt32
	IsVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsVar, aValue);
	
	public static tInt32
	VarDef<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValueReg
	) => aDef._AddReg(aPos, tOpCode.VarDef, aValueReg);
	
	public static void
	VarSet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aVarReg,
		tInt32 aValueReg
	) {
		aDef._AddCommand(aPos, tOpCode.VarSet, aVarReg, aValueReg);
	}
	
	public static tInt32
	VarGet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aVarReg
	) => aDef._AddReg(aPos, tOpCode.VarGet, aVarReg);
	
	public static tInt32
	Call<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aProcReg,
		tInt32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.CallFunc, aProcReg, aArgReg);
	
	public static tInt32
	Exec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aProcReg,
		tInt32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.CallProc, aProcReg, aArgReg);
	
	public static void
	ReturnIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aCondReg,
		tInt32 aResReg
	) {
		aDef._AddCommand(aPos, tOpCode.ReturnIf, aCondReg, aResReg);
	}
	
	public static void
	ContinueIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aCondReg,
		tInt32 aArgReg
	) {
		aDef._AddCommand(aPos, tOpCode.ContinueIf, aCondReg, aArgReg);
	}
	
	public static void
	TailCallIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aCondReg,
		tInt32 aCallerArgReg
	) {
		aDef._AddCommand(aPos, tOpCode.TailCallIf, aCondReg, aCallerArgReg);
	}
	
	public static void
	Assert<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPreCondReg,
		tInt32 aPostCondReg
	) {
		aDef._AddCommand(aPos, tOpCode.Assert, aPreCondReg, aPostCondReg);
	}
	
	public static tInt32
	IsType<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsType, aValue);
	
	public static tInt32
	TypeEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeEmpty);
	
	public static tInt32
	TypeAny<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeAny);
	
	public static tInt32
	TypeInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeInt);
	
	public static tInt32
	TypePair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aTypeReg1,
		tInt32 aTypeReg2
	) => aDef._AddReg(aPos, tOpCode.TypePair, aTypeReg1, aTypeReg2);
	
	public static tInt32
	TypePrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aPrefix,
		tInt32 aTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypePrefix, aPrefix, aTypeReg);
	
	public static tInt32
	TypeRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aRecordTypeReg,
		tInt32 aPrefixTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeRecord, aRecordTypeReg, aPrefixTypeReg);
	
	public static tInt32
	TypeVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeVar, aTypeReg);
	
	public static tInt32
	TypeSet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aTypeReg1,
		tInt32 aTypeReg2
	) => aDef._AddReg(aPos, tOpCode.TypeSet, aTypeReg1, aTypeReg2);
	
	public static tInt32
	TypeFunc<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aArgTypeReg,
		tInt32 aResTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeFunc, aArgTypeReg, aResTypeReg);
	
	public static tInt32
	TypeMeth<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aObjTypeReg,
		tInt32 aFuncTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeFunc, aObjTypeReg, aFuncTypeReg);
	
	public static tInt32
	TypeFree<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeRecursive);
	
	public static tInt32
	TypeRecursive<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aHeadTypeReg,
		tInt32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeRecursive, aHeadTypeReg, aBodyTypeReg);
	
	public static tInt32
	TypeInterface<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aHeadTypeReg,
		tInt32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeInterface, aHeadTypeReg, aBodyTypeReg);
	
	public static tInt32
	TypeGeneric<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aHeadTypeReg,
		tInt32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeGeneric, aHeadTypeReg, aBodyTypeReg);
	
	// TODO: Match Types
	
	public enum
	tDataType {
		Empty,
		Bool,
		Int,
		Pair,
		Prefix,
		Record,
		Proc,
		ExternProc,
		Def,
		ExternDef,
		Var,
		Type
	}
	
	[System.Diagnostics.DebuggerDisplay("{mVM_Data.ToText(this, 3)}")]
	public sealed class
	tData {
		public tDataType _DataType;
		public mAny.tAny _Value;
		public tBool _IsMutable;
		
		public tBool
		Equals(
			tData a
		) => (
			a is not null
			&& this._DataType.Equals(a._DataType)
			&& this._Value.Equals(a._Value)
		);
		
		public override tBool
		Equals(
			object? a
		) => this.Equals((tData)a!);
	}
	
	private static tData
	Data<t>(
		tDataType aType,
		tBool aIsMutable,
		t aValue
	) => new tData{
		_DataType = aType,
		_IsMutable = aIsMutable,
		_Value = mAny.Any(aValue)
	};
	
	private static tData
	Data<t1, t2>(
		tDataType aType,
		tBool aIsMutable,
		t1 aValue1,
		t2 aValue2
	) => new tData{
		_DataType = aType,
		_IsMutable = aIsMutable,
		_Value = mAny.Any((aValue1, aValue2))
	};
	
	private static tBool
	Match<t>(
		this tData aData,
		tDataType aType,
		out t aValue
	) {
		aValue = default!;
		return (
			aData._DataType.Equals(aType) &&
			aData._Value.Match(out aValue)
		);
	}
	
	private static tBool
	Match<t1, t2>(
		this tData aData,
		tDataType aType,
		out t1 aValue1,
		out t2 aValue2
	) {
		if (
			aData._DataType.Equals(aType) &&
			aData._Value.Match(out (t1, t2) Tuple)
		) {
			(aValue1, aValue2) = Tuple;
			return true;
		} else {
			aValue1 = default!;
			aValue2 = default!;
			return false;
		}
	}
	
	public static tData
	Empty(
	) => Data(tDataType.Empty, false, 1);
	
	public static tBool
	MatchEmpty(
		this tData aData
	) => aData._DataType == tDataType.Empty;
	
	public static tData
	Bool(
		tBool aValue
	) => Data(tDataType.Bool, false, aValue);
	
	public static tBool
	MatchBool(
		this tData aData,
		out tBool aValue
	) => aData.Match(tDataType.Bool, out aValue);
	
	public static tData
	Int(
		tInt32 aValue
	) => Data(tDataType.Int, false, aValue);
	
	public static tBool
	MatchInt(
		this tData aData,
		out tInt32 aValue
	) => aData.Match(tDataType.Int, out aValue);
	
	public static tData
	Pair(
		tData aFirst,
		tData aSecond
	) => Data(tDataType.Pair, aFirst._IsMutable || aSecond._IsMutable, aFirst, aSecond);
	
	public static tBool
	MatchPair(
		this tData aData,
		out tData aFirst,
		out tData aSecond
	) => aData.Match(tDataType.Pair, out aFirst, out aSecond);
	
	public static tData
	Tuple(
		params tData[] a
	) => mStream.Stream(a).Reverse().Reduce(Empty(), (aList, aItem) => Pair(aItem, aList));
	
	public static tBool
	MatchTuple(
		this tData aData,
		out tData a1,
		out tData a2
	) {
		a2 = default!;
		return (
			aData.MatchPair(out a1, out var Rest2) &&
			Rest2.MatchPair(out a2, out var Rest_) &&
			Rest_.MatchEmpty()
		);
	}
	
	public static tBool
	MatchTuple(
		this tData aData,
		out tData a1,
		out tData a2,
		out tData a3
	) {
		a2 = default!;
		a3 = default!;
		return (
			aData.MatchPair(out a1, out var Rest23) &&
			Rest23.MatchPair(out a2, out var Rest3) &&
			Rest3.MatchPair(out a3, out var Rest_) &&
			Rest_.MatchEmpty()
		);
	}
	
	public static tBool
	MatchTuple(
		this tData aData,
		out tData a1,
		out tData a2,
		out tData a3,
		out tData a4
	) {
		a2 = default!;
		a3 = default!;
		a4 = default!;
		return (
			aData.MatchPair(out a1, out var Rest234) &&
			Rest234.MatchPair(out a2, out var Rest34) &&
			Rest34.MatchPair(out a3, out var Rest4) &&
			Rest4.MatchPair(out a4, out var Rest_) &&
			Rest_.MatchEmpty()
		);
	}
	
	public static tData
	Prefix(
		tInt32 aPrefixId,
		tData aData
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefixId, aData);
	
	public static tBool
	MatchPrefix(
		this tData aData,
		out tInt32 aPrefixId,
		out tData aValue
	) => aData.Match(tDataType.Prefix, out aPrefixId, out aValue);
	
	public static tBool
	MatchPrefix(
		this tData aData,
		tInt32 aPrefixId,
		out tData aValue
	) => aData.MatchPrefix(out var PrefixId, out aValue) && PrefixId == aPrefixId;
	
	public static tData
	Prefix(
		tText aPrefix,
		tData aData
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefix.GetHashCode(), aData);
	
	public static tBool
	MatchPrefix(
		this tData aData,
		tText aPrefix,
		out tData aValue
	) => aData.MatchPrefix(aPrefix.GetHashCode(), out aValue);
	
	public static tData
	Record(
		tData aRecord,
		tData aPrefix
	) {
		mAssert.IsTrue(aPrefix.MatchPrefix(out var PrefixHash, out _));
		var Record = aRecord;
		while (!Record.MatchEmpty()) {
			mAssert.IsTrue(Record.MatchRecord(out Record, out var Prefix));
			mAssert.IsTrue(Prefix.MatchPrefix(out var PrefixHash_, out _));
			mAssert.AreNotEquals(PrefixHash, PrefixHash_);
		}
		return Data(tDataType.Record, aRecord._IsMutable || aPrefix._IsMutable, aRecord, aPrefix);
	}
	
	public static tData
	Record(
		params (tText, tData)[] aFields
	) {
		var Result = Empty();
		foreach (var (Key, Value) in aFields) {
			Result = Record(Result, Prefix(Key, Value));
		}
		return Result;
	}
	
	public static tBool
	MatchRecord(
		this tData aData,
		out tData aRecord,
		out tData aPrefix
	) => aData.Match(tDataType.Record, out aRecord, out aPrefix);
	
	public static tData
	Proc<tPos>(
		tProcDef<tPos> aDef,
		tData aEnv
	) {
		mAssert.IsFalse(aEnv._IsMutable);
		// In the end this is the place where the compiler will called !!!
		return Data(tDataType.Proc, false, aDef, aEnv);
	}
	
	public static tBool
	MatchProc(
		this tData aData,
		out tProcDef aDef,
		out tData aEnv
	) => aData.Match(tDataType.Proc, out aDef, out aEnv);
	
	public static tBool
	MatchProc<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef,
		out tData aEnv
	) => aData.Match(tDataType.Proc, out aDef, out aEnv);
	
	public static tData
	ExternProc(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		tData aEnv
	) {
		mAssert.IsFalse(aEnv._IsMutable);
		return Data(tDataType.ExternProc, false, aExternDef, aEnv);
	}
	
	public static tBool
	MatchExternProc(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		out tData aEnv
	) => aData.Match(tDataType.ExternProc, out aExternDef, out aEnv);
	
	public static tData
	Def<tPos>(
		tProcDef<tPos> aDef
	) => Data(tDataType.Def, false, aDef);
	
	public static tBool
	MatchDef(
		this tData aData,
		out tProcDef aDef
	) => aData.Match(tDataType.Def, out aDef);
	
	public static tBool
	MatchDef<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef
	) => aData.Match(tDataType.Def, out aDef);
	
	public static tData
	Var(
		tData aValue
	) => Data(tDataType.Var, true, aValue);
	
	public static tBool
	MatchVar(
		this tData aData,
		out tData aValue
	) => aData.Match(tDataType.Var, out aValue);
	
	public static tData
	ExternDef(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	) => Data(tDataType.ExternDef, false, a);
	
	public static tBool
	MatchExternDef(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	) => aData.Match(tDataType.ExternDef, out a);
	
	public static tData
	TypeType(
	) => Data(tDataType.Type, false, mVM_Type.Type(null));
	
	// TODO: Matches for Types ???
	
	public static tData
	TypeEmpty(
	) => Data(tDataType.Type, false, mVM_Type.Empty());
	
	public static tData
	TypeBool(
	) => Data(tDataType.Type, false, mVM_Type.Bool());
	
	public static tData
	TypeInt(
	) => Data(tDataType.Type, false, mVM_Type.Int());
	
	public static tData
	TypePair(
		mVM_Type.tType aType1,
		mVM_Type.tType aType2
	) => Data(tDataType.Type, false, mVM_Type.Pair(aType1, aType2));
	
	public static tText
	ToText(
		this tData a,
		tInt32 aLimit
	) {
		if (aLimit == 0) {
			return "...";
		}
		var NextLimit = aLimit - 1;
		
		if (a.MatchEmpty()) {
			return "§EMPTY";
			
		} else if (a.MatchBool(out var Bool)) {
			return Bool ? "§TRUE" : "§FALSE";
			
		} else if (a.MatchInt(out var Int)) {
			return $"{Int}";
			
		} else if (a.MatchPrefix(out var Prefix, out var Value)) {
			return $"(#{Prefix} {Value.ToText(NextLimit)})";
			
		} else if (a.MatchRecord(out var SubRecord, out var KeyValue)) {
			KeyValue.MatchPrefix(out var Key, out Value);
			var Result = $"{{ {Key}: {Value}";
			while (SubRecord.MatchRecord(out KeyValue, out var Temp)) {
				Result += $", {Key}: {Value.ToText(NextLimit)}";
				SubRecord = Temp;
			}
			return Result + "}";
			
		} else if (a.MatchVar(out Value)) {
			return $"(§VAR {Value.ToText(NextLimit)})";
			
		} else if (a.MatchPair(out var Rest1, out var Rest2)) {
			var Result = "(" + Rest1.ToText(NextLimit);
			while (Rest2.MatchPair(out Rest1, out var Temp)) {
				Result += ", " + Rest1.ToText(NextLimit);
				Rest2 = Temp;
			}
			if (!Rest2.MatchEmpty()) {
				Result += "; " + Rest2.ToText(NextLimit);
			}
			return Result + ")";
			
		} else if (a.MatchProc(out var Def, out var Env)) {
			return $"(Proc @ {Def.FirstPosText})";
			
		} else if (a.MatchDef(out var Def_)) {
			return $"(Def @ {Def_.FirstPosText})";
			
		} else {
			return $"(?{a._DataType}?)";
		}
	}
}
