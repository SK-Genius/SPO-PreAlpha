//IMPORT mVM_Type.cs
//IMPORT mAny.cs

#nullable enable

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
		IntsDiv,
		
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
	public static readonly tNat32 cEmptyReg = 0;
	public static readonly tNat32 cOneReg = 1;
	public static readonly tNat32 cFalseReg = 2;
	public static readonly tNat32 cTrueReg = 3;
	public static readonly tNat32 cEmptyTypeReg = 4;
	public static readonly tNat32 cBoolTypeReg = 5;
	public static readonly tNat32 cIntTypeReg = 6;
	public static readonly tNat32 cTypeTypeReg = 7;
	public static readonly tNat32 cEnvReg = 8;
	public static readonly tNat32 cObjReg = 9;
	public static readonly tNat32 cArgReg = 10;
	public static readonly tNat32 cResReg = 11;
	
	[DebuggerDisplay("{this.DefType.ToText(10)}")]
	public sealed class
	tProcDef<tPos> : tProcDef {
		public readonly mArrayList.tArrayList<(tOpCode, tNat32, tNat32)>
			Commands = mArrayList.List<(tOpCode, tNat32, tNat32)>();
		
		public readonly mArrayList.tArrayList<tPos>
			PosList = mArrayList.List<tPos>();
		
		public readonly mVM_Type.tType DefType;
		
		public readonly mArrayList.tArrayList<mVM_Type.tType>
		Types = mArrayList.List<mVM_Type.tType>();
		
		public tNat32 _LastReg = cResReg;
		
		public string FirstPosText => "" + this.PosList.ToStream().TryFirst()._Value;
		
		public tProcDef(
			mVM_Type.tType aDefType
		) {
			mAssert.IsTrue(aDefType.IsProc(out var Empty, out var Env, out var Proc));
			mAssert.AreEquals(Empty.Kind, mVM_Type.tKind.Empty);
			mAssert.AreEquals(Proc.Kind, mVM_Type.tKind.Proc);
			
			this.DefType = aDefType;
		}
	}
	
	internal static void
	_AddCommand<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1
	) => aDef._AddCommand(aPos, aCommand, aReg1, 0);
	
	internal static void
	_AddCommand<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1,
		tNat32 aReg2
	) { 
		aDef.PosList.Push(aPos);
		aDef.Commands.Push((aCommand, aReg1, aReg2));
	}
	
	internal static tNat32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand
	) => aDef._AddReg(aPos, aCommand, 0, 0);
	
	internal static tNat32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1
	) => aDef._AddReg(aPos, aCommand, aReg1, 0);
	
	internal static tNat32
	_AddReg<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tOpCode aCommand,
		tNat32 aReg1,
		tNat32 aReg2
	) {
		aDef._AddCommand(aPos, aCommand, aReg1, aReg2);
		aDef._LastReg += 1;
		return aDef._LastReg;
	}
	
	public static tNat32
	IsBool<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsBool, aValue);
	
	public static tNat32
	And<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBoolReg1,
		tNat32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.And, aBoolReg1, aBoolReg2);
	
	public static tNat32
	Or<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBoolReg1,
		tNat32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.Or, aBoolReg1, aBoolReg2);
	
	public static tNat32
	XOr<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aBoolReg1,
		tNat32 aBoolReg2
	) => aDef._AddReg(aPos, tOpCode.XOr, aBoolReg1, aBoolReg2);
	
	public static tNat32
	IsInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsInt, (tNat32)aValue);
	
	public static tNat32
	Int<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tInt32 aIntValue
	) => aDef._AddReg(aPos, tOpCode.NewInt, (tNat32)aIntValue);
	
	public static tNat32
	IntsAreEq<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsAreEq, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsComp<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsComp, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsAdd<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsAdd, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsSub<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsSub, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsMul<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsMul, aIntReg1, aIntReg2);
	
	public static tNat32
	IntsDiv<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aIntReg1,
		tNat32 aIntReg2
	) => aDef._AddReg(aPos, tOpCode.IntsDiv, aIntReg1, aIntReg2);
	
	public static tNat32
	IsPair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsPair, aValue);
	
	public static tNat32
	Pair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aDataReg1,
		tNat32 aDataReg2
	) => aDef._AddReg(aPos, tOpCode.NewPair, aDataReg1, aDataReg2);
	
	public static tNat32
	First<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPairReg
	) => aDef._AddReg(aPos, tOpCode.First, aPairReg);
	
	public static tNat32
	Second<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPairReg
	) => aDef._AddReg(aPos, tOpCode.Second, aPairReg);
	
	public static tNat32
	IsPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsPrefix, aValue);
	
	public static tNat32
	AddPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aDataReg
	) => aDef._AddReg(aPos, tOpCode.AddPrefix, aPrefixId, aDataReg);
	
	public static tNat32
	DelPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aReg
	) => aDef._AddReg(aPos, tOpCode.DelPrefix, aPrefixId, aReg);
	
	public static tNat32
	HasPrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aDataReg
	) => aDef._AddReg(aPos, tOpCode.HasPrefix, aPrefixId, aDataReg);
	
	public static tNat32
	IsRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsRecord, aValue);
	
	public static tNat32
	ExtendRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg,
		tNat32 aPrefixReg
	) => aDef._AddReg(aPos, tOpCode.ExtendRec, aRecReg, aPrefixReg);
	
	public static tNat32
	DivideRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg
	) => aDef._AddReg(aPos, tOpCode.DivideRec, aRecReg);
	
	public static tNat32
	ExtendRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg
	) => aDef._AddReg(aPos, tOpCode.DivideRec, aRecReg);
	
	public static tNat32
	IsVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsVar, aValue);
	
	public static tNat32
	VarDef<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValueReg
	) => aDef._AddReg(aPos, tOpCode.VarDef, aValueReg);
	
	public static void
	VarSet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aVarReg,
		tNat32 aValueReg
	) {
		aDef._AddCommand(aPos, tOpCode.VarSet, aVarReg, aValueReg);
	}
	
	public static tNat32
	VarGet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aVarReg
	) => aDef._AddReg(aPos, tOpCode.VarGet, aVarReg);
	
	public static tNat32
	Call<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aProcReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.CallFunc, aProcReg, aArgReg);
	
	public static tNat32
	Exec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aProcReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.CallProc, aProcReg, aArgReg);
	
	public static void
	ReturnIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aCondReg,
		tNat32 aResReg
	) {
		aDef._AddCommand(aPos, tOpCode.ReturnIf, aCondReg, aResReg);
	}
	
	public static void
	ContinueIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aCondReg,
		tNat32 aArgReg
	) {
		aDef._AddCommand(aPos, tOpCode.ContinueIf, aCondReg, aArgReg);
	}
	
	public static void
	TailCallIf<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aCondReg,
		tNat32 aCallerArgReg
	) {
		aDef._AddCommand(aPos, tOpCode.TailCallIf, aCondReg, aCallerArgReg);
	}
	
	public static void
	Assert<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPreCondReg,
		tNat32 aPostCondReg
	) {
		aDef._AddCommand(aPos, tOpCode.Assert, aPreCondReg, aPostCondReg);
	}
	
	public static tNat32
	IsType<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aValue
	) => aDef._AddReg(aPos, tOpCode.IsType, aValue);
	
	public static tNat32
	TypeEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeEmpty);
	
	public static tNat32
	TypeAny<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeAny);
	
	public static tNat32
	TypeInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeInt);
	
	public static tNat32
	TypePair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aTypeReg1,
		tNat32 aTypeReg2
	) => aDef._AddReg(aPos, tOpCode.TypePair, aTypeReg1, aTypeReg2);
	
	public static tNat32
	TypePrefix<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefix,
		tNat32 aTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypePrefix, aPrefix, aTypeReg);
	
	public static tNat32
	TypeRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecordTypeReg,
		tNat32 aPrefixTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeRecord, aRecordTypeReg, aPrefixTypeReg);
	
	public static tNat32
	TypeVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeVar, aTypeReg);
	
	public static tNat32
	TypeSet<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aTypeReg1,
		tNat32 aTypeReg2
	) => aDef._AddReg(aPos, tOpCode.TypeSet, aTypeReg1, aTypeReg2);
	
	public static tNat32
	TypeFunc<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgTypeReg,
		tNat32 aResTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeFunc, aArgTypeReg, aResTypeReg);
	
	public static tNat32
	TypeMeth<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aObjTypeReg,
		tNat32 aFuncTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeFunc, aObjTypeReg, aFuncTypeReg);
	
	public static tNat32
	TypeFree<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeRecursive);
	
	public static tNat32
	TypeRecursive<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aHeadTypeReg,
		tNat32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeRecursive, aHeadTypeReg, aBodyTypeReg);
	
	public static tNat32
	TypeInterface<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aHeadTypeReg,
		tNat32 aBodyTypeReg
	) => aDef._AddReg(aPos, tOpCode.TypeInterface, aHeadTypeReg, aBodyTypeReg);
	
	public static tNat32
	TypeGeneric<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aHeadTypeReg,
		tNat32 aBodyTypeReg
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
	
	[DebuggerDisplay("{mVM_Data.ToText(this, 3)}")]
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
	) => new() {
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
	) => new() {
		_DataType = aType,
		_IsMutable = aIsMutable,
		_Value = mAny.Any((aValue1, aValue2))
	};
	
	private static tBool
	Is<t>(
		this tData aData,
		tDataType aType,
		out t aValue
	) {
		aValue = default!;
		return (
			aData._DataType.Equals(aType) &&
			aData._Value.Is(out aValue)
		);
	}
	
	private static tBool
	Is<t1, t2>(
		this tData aData,
		tDataType aType,
		out t1 aValue1,
		out t2 aValue2
	) {
		if (
			aData._DataType.Equals(aType) &&
			aData._Value.Is(out (t1, t2) Tuple)
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
	IsEmpty(
		this tData aData
	) => aData._DataType == tDataType.Empty;
	
	public static tData
	Bool(
		tBool aValue
	) => Data(tDataType.Bool, false, aValue);
	
	public static tBool
	IsBool(
		this tData aData,
		out tBool aValue
	) => aData.Is(tDataType.Bool, out aValue);
	
	public static tData
	Int(
		tInt32 aValue
	) => Data(tDataType.Int, false, aValue);
	
	public static tBool
	IsInt(
		this tData aData,
		out tInt32 aValue
	) => aData.Is(tDataType.Int, out aValue);
	
	public static tData
	Pair(
		tData aFirst,
		tData aSecond
	) => Data(tDataType.Pair, aFirst._IsMutable || aSecond._IsMutable, aFirst, aSecond);
	
	public static tBool
	IsPair(
		this tData aData,
		out tData aFirst,
		out tData aSecond
	) {
		if (!aData.Is(tDataType.Pair, out aFirst, out aSecond)) {
			aFirst = aData;
			aSecond = Empty();
			return false;
		}
		return true;
	}
	
	public static tData
	Tuple(
		params tData[] a
	) => mStream.Stream(a).Reduce(Empty(), Pair);
	
	public static tBool
	IsTuple(
		this tData aData,
		out tData a1,
		out tData a2
	) {
		a2 = default!;
		return (
			aData.IsPair(out a1, out var Rest2) &&
			Rest2.IsPair(out a2, out var Rest_) &&
			Rest_.IsEmpty()
		);
	}
	
	public static tBool
	IsTuple(
		this tData aData,
		out tData a1,
		out tData a2,
		out tData a3
	) {
		a2 = default!;
		a3 = default!;
		return (
			aData.IsPair(out a1, out var Rest23) &&
			Rest23.IsPair(out a2, out var Rest3) &&
			Rest3.IsPair(out a3, out var Rest_) &&
			Rest_.IsEmpty()
		);
	}
	
	public static tBool
	IsTuple(
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
			aData.IsPair(out a1, out var Rest234) &&
			Rest234.IsPair(out a2, out var Rest34) &&
			Rest34.IsPair(out a3, out var Rest4) &&
			Rest4.IsPair(out a4, out var Rest_) &&
			Rest_.IsEmpty()
		);
	}
	
	public static tData
	Prefix(
		tNat32 aPrefixId,
		tData aData
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefixId, aData);
	
	public static tBool
	IsPrefix(
		this tData aData,
		out tNat32 aPrefixId,
		out tData aValue
	) => aData.Is(tDataType.Prefix, out aPrefixId, out aValue);
	
	public static tBool
	IsPrefix(
		this tData aData,
		tNat32 aPrefixId,
		out tData aValue
	) => aData.IsPrefix(out var PrefixId, out aValue) && PrefixId == aPrefixId;
	
	public static tData
	Prefix(
		tText aPrefix,
		tData aData
	) => Data(tDataType.Prefix, aData._IsMutable, (tNat32)aPrefix.GetHashCode(), aData);
	
	public static tBool
	IsPrefix(
		this tData aData,
		tText aPrefix,
		out tData aValue
	) => aData.IsPrefix((tNat32)aPrefix.GetHashCode(), out aValue);
	
	public static tData
	Record(
		tData aRecord,
		tData aPrefix
	) {
		mAssert.IsTrue(aPrefix.IsPrefix(out var PrefixHash, out _));
		var Record = aRecord;
		while (!Record.IsEmpty()) {
			mAssert.IsTrue(Record.IsRecord(out Record, out var Prefix));
			mAssert.IsTrue(Prefix.IsPrefix(out var PrefixHash_, out _));
			mAssert.AreNotEquals(PrefixHash, PrefixHash_);
		}
		return Data(tDataType.Record, aRecord._IsMutable || aPrefix._IsMutable, aRecord, aPrefix);
	}
	
	public static tData
	Record(
		params (tText Key, tData Value)[] aFields
	) => aFields.AsStream(
	).Map(
		a => Prefix(a.Key, a.Value)
	).Reduce(
		Empty(),
		(aTail, aHead) => Record(aTail, aHead)
	);
	
	public static tBool
	IsRecord(
		this tData aData,
		out tData aRecord,
		out tData aPrefix
	) => aData.Is(tDataType.Record, out aRecord, out aPrefix);
	
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
	IsProc(
		this tData aData,
		out tProcDef aDef,
		out tData aEnv
	) => aData.Is(tDataType.Proc, out aDef, out aEnv);
	
	public static tBool
	IsProc<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef,
		out tData aEnv
	) => aData.Is(tDataType.Proc, out aDef, out aEnv);
	
	public static tData
	ExternProc(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		tData aEnv
	) {
		mAssert.IsFalse(aEnv._IsMutable);
		return Data(tDataType.ExternProc, false, aExternDef, aEnv);
	}
	
	public static tBool
	IsExternProc(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> aExternDef,
		out tData aEnv
	) => aData.Is(tDataType.ExternProc, out aExternDef, out aEnv);
	
	public static tData
	Def<tPos>(
		tProcDef<tPos> aDef
	) => Data(tDataType.Def, false, aDef);
	
	public static tBool
	IsDef(
		this tData aData,
		out tProcDef aDef
	) => aData.Is(tDataType.Def, out aDef);
	
	public static tBool
	IsDef<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef
	) => aData.Is(tDataType.Def, out aDef);
	
	public static tData
	Var(
		tData aValue
	) => Data(tDataType.Var, true, aValue);
	
	public static tBool
	IsVar(
		this tData aData,
		out tData aValue
	) => aData.Is(tDataType.Var, out aValue);
	
	public static tData
	ExternDef(
		mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	) => Data(tDataType.ExternDef, false, a);
	
	public static tBool
	IsExternDef(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>> a
	) => aData.Is(tDataType.ExternDef, out a);
	
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
		
		return 0 switch {
			_ when a.IsEmpty()
			=> "()",
			
			_ when a.IsBool(out var Bool)
			=> Bool ? "§TRUE" : "§FALSE",
			
			_ when a.IsInt(out var Int)
			=> $"{Int}",
			
			_ when a.IsPrefix(out var Prefix, out var Value)
			=> $"(#{Prefix} {Value.ToText(NextLimit)})",
			
			_ when a.IsRecord(out var SubRecord, out var KeyValue)
			=> mStd.Call(() => {
				KeyValue.IsPrefix(out var Key, out var Value);
				var Result = $"{{ {Key}: {Value}";
				while (SubRecord.IsRecord(out KeyValue, out var Temp)) {
					Result += $", {Key}: {Value.ToText(NextLimit)}";
					SubRecord = Temp;
				}
				return Result + "}";
			}),
			
			_ when a.IsVar(out var Value)
			=> $"(§VAR {Value.ToText(NextLimit)})",
			
			_ when a.IsPair(out var Left, out var Right)
			=> mStd.Call(() => {
				var Result = Right.ToText(NextLimit) + ")";
				while (Left.IsPair(out Left, out Right)) {
					Result = Right.ToText(NextLimit) + ", " + Result;
				}
				if (!Left.IsEmpty()) {
					Result = Left.ToText(NextLimit) + "; " + Result;
				}
				return "(" + Result;
			}),
			
			_ when a.IsProc(out var Def, out var Env)
			=> $"(Proc @ {Def.FirstPosText})",
			
			_ when a.IsDef(out var Def_)
			=> $"(Def @ {Def_.FirstPosText})",
			
			_
			=> $"(?{a._DataType}?)",
		};
	}
}
