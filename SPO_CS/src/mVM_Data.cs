// IMPORT Common/mStd
// IMPORT Common/mAny
// IMPORT Common/mArrayList
// IMPORT Common/mAssert
// IMPORT Common/mMaybe
// IMPORT Common/mStream
// IMPORT mVM_Type

public static class
mVM_Data {
	
	public enum
	tOpCode {
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
		IntsDiv,
		
		// PAIR
		NewPair,
		First,
		Second,
		
		// PREFIX
		AddPrefix,
		DelPrefix,
		
		// RECORD
		AddField,
		GetField,
		
		// VAR
		VarDef,
		VarSet,
		VarGet,
		
		// JUMP
		CallFunc,
		CallProc,
		DefRecProcs,
		ReturnIf,
		ReturnIfNotEmpty,
		TryAsNotEmpty,
		TryAsBool,
		TryAsInt,
		TryAsType,
		TryAsPair,
		TryAsVar,
		TryAsRef,
		TryAsRecord,
		TryRemovePrefixFrom,
		
		// ASSERT
		Assert,
		
		// TYPE
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
	//public static readonly tNat32 cSelfFuncReg = 4;
	public static readonly tNat32 cEmptyTypeReg = 4;
	public static readonly tNat32 cBoolTypeReg = 5;
	public static readonly tNat32 cIntTypeReg = 6;
	public static readonly tNat32 cTypeTypeReg = 7;
	public static readonly tNat32 cEnvReg = 8;
	public static readonly tNat32 cObjReg = 9;
	public static readonly tNat32 cArgReg = 10;
	public static readonly tNat32 cResReg = 11;
	
	[DebuggerDisplay("{this.DefType.ToText()}")]
	public sealed class
	tProcDef<tPos> : tProcDef {
		public readonly mArrayList.tArrayList<(tOpCode, tNat32, tNat32, tNat64)>
			Commands = mArrayList.List<(tOpCode, tNat32, tNat32, tNat64)>();
		
		public readonly mArrayList.tArrayList<tPos>
			PosList = mArrayList.List<tPos>();
		
		public readonly mVM_Type.tType DefType;
		
		public readonly mArrayList.tArrayList<mVM_Type.tType>
		Types = mArrayList.List<mVM_Type.tType>();
		
		public tNat32 _LastReg = cResReg;
		
		public tText FirstPosText => "" + this.PosList.ToStream().TryFirst().AssertNotEmpty();
		
		public tProcDef(
			mVM_Type.tType aDefType
		) {
			mAssert.IsTrue(aDefType.IsProc(out var Empty, out var Env, out var Proc));
			mAssert.AreEquals(Empty.Kind, mVM_Type.tKind.Empty);
			
			while (Proc.IsGeneric(out _, out var Body)) {
				Proc = Body;
			}
			
			if (Proc.Kind is not mVM_Type.tKind.Proc) {
				mAssert.Fail($"expected proc type but is:\n{Proc.ToText()}");
			}
			
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
		aDef.Commands.Push((aCommand, aReg1, aReg2, mStd.NewDebugId()));
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
	ExtendRec<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg,
		tNat32 aPrefixReg
	) => aDef._AddReg(aPos, tOpCode.AddField, aRecReg, aPrefixReg);
	
	public static tNat32
	GetField<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg,
		tNat32 aFieldId
	) => aDef._AddReg(aPos, tOpCode.GetField, aRecReg, aFieldId);
	
	public static tNat32
	AddField<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aRecReg
	) => aDef._AddReg(aPos, tOpCode.AddField, aRecReg);
	
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
	DefRecProcs<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aFuncReg,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.DefRecProcs, aFuncReg, aArgReg);
	
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
	ReturnIfNotEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aResReg
	) {
		aDef._AddCommand(aPos, tOpCode.ReturnIfNotEmpty, aResReg);
	}
	
	public static tNat32
	TryAsNotEmpty<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsNotEmpty, aArgReg);
	
	public static tNat32
	TryAsBool<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsBool, aArgReg);
	
	public static tNat32
	TryAsInt<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsInt, aArgReg);
	
	public static tNat32
	TryAsType<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsType, aArgReg);
	
	public static tNat32
	TryRemovePrefixFrom<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aPrefixId,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryRemovePrefixFrom, aPrefixId, aArgReg);
	
	public static tNat32
	TryAsRecord<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsRecord, aArgReg);

	public static tNat32
	TryAsPair<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsPair, aArgReg);

	public static tNat32
	TryAsVar<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsVar, aArgReg);

	public static tNat32
	TryAsRef<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos,
		tNat32 aArgReg
	) => aDef._AddReg(aPos, tOpCode.TryAsRef, aArgReg);
	
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
	) => aDef._AddReg(aPos, tOpCode.TypeMeth, aObjTypeReg, aFuncTypeReg);
	
	public static tNat32
	TypeFree<tPos>(
		this tProcDef<tPos> aDef,
		tPos aPos
	) => aDef._AddReg(aPos, tOpCode.TypeFree);
	
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
	
	[DebuggerDisplay("{mVM_Data.ToText(this, 10)}")]
	public sealed class
	tData {
		public tDataType _DataType;
		public mAny.tAny _Value;
		public mTreeMap.tTree<tNat32, tData> _Fields = mTreeMap.Tree<tNat32, tData>((a1, a2) => a1.CompareTo(a2), []);
		public tBool _IsMutable;
		
		public tNat64 _DebugId = mStd.NewDebugId();
		
		public tBool
		Equals(
			tData a
		) => (
			a is not null
			&& this._DataType.Equals(a._DataType)
			&& this._Value.Equals(a._Value)
			&& this._Fields.Equals(a._Fields)
		);
		
		public override tBool
		Equals(
			tUnknown? a
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
	Data(
		tDataType aType,
		tBool aIsMutable,
		mTreeMap.tTree<tNat32, tData> aFields
	) => new() {
		_DataType = aType,
		_IsMutable = aIsMutable,
		_Fields = aFields,
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
		System.Span<tData> a
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
	) => Data(tDataType.Prefix, aData._IsMutable, aPrefix.PrefixHash(), aData);
	
	public static tBool
	IsPrefix(
		this tData aData,
		tText aPrefix,
		out tData aValue
	) => aData.IsPrefix(aPrefix.PrefixHash(), out aValue);
	
	public static tData
	Record(
		tData aRecord,
		tData aPrefix
	) {
		if (aRecord.IsEmpty()) {
			aRecord = Data(
				tDataType.Record,
				aPrefix._IsMutable,
				mTreeMap.Tree<tNat32, tData>((a1, a2) => a1.CompareTo(a2), [])
			);
		}
		
		mAssert.IsTrue(aPrefix.IsPrefix(out var PrefixHash, out var Data_));
		mAssert.IsTrue(aRecord.IsRecord(out var Fields));
		mAssert.IsTrue(Fields.TryGet(PrefixHash).IsNone());
		
		return Data(
			tDataType.Record,
			aRecord._IsMutable || aPrefix._IsMutable,
			Fields.Set(PrefixHash, Data_)
		);
	}
	
	public static tData
	Record(
		(tText Key, tData Value)[] aFields
	) => aFields.AsStream(
	).Map(
		_ => Prefix(_.Key, _.Value)
	).Reduce(
		Empty(),
		Record
	);
	
	public static tBool
	IsRecord(
		this tData aData,
		[NotNullWhen(true)]out mTreeMap.tTree<tNat32, tData> aFields
	) {
		if (aData._DataType is tDataType.Record) {
			aFields =  aData._Fields;
			return true;
		} else {
			aFields = default!;
			return false;
		}
	}
	
	public static tData
	GetField(
		tData aRecord,
		tText aFieldName
	) {
		mAssert.Fail(); // TODO
		return default;
	}
	
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
	) {
		if (aData._DataType != tDataType.Proc) {
			aDef = default!;
			aEnv = default!;
			return false;
		}
		
		var Data = (dynamic)aData._Value._Value;
		aDef = Data.Item1 as tProcDef;
		aEnv = Data.Item2;
		return true;
	}
	
	public static tBool
	IsProc<tPos>(
		this tData aData,
		out tProcDef<tPos> aDef,
		out tData aEnv
	) => aData.Is(tDataType.Proc, out aDef, out aEnv);
	
	public static tData
	ExternProc(
		mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> aExternDef,
		tData aEnv
	) {
		mAssert.IsFalse(aEnv._IsMutable);
		return Data(tDataType.ExternProc, false, aExternDef, aEnv);
	}
	
	public static tBool
	IsExternProc(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> aExternDef,
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
	) {
		if (aData._DataType != tDataType.Def) {
			aDef = default!;
			return false;
		}
		
		var Data = (dynamic)aData._Value._Value;
		aDef = Data as tProcDef;
		return true;
	}
	
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
		mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> a
	) => Data(tDataType.ExternDef, false, a);
	
	public static tBool
	IsExternDef(
		this tData aData,
		out mStd.tFunc<tData, tData, tData, mStd.tAction<mStd.tFunc<tText>>, tData> a
	) => aData.Is(tDataType.ExternDef, out a);
	
	public static tData
	TypeType(
	) => Data(tDataType.Type, false, mVM_Type.Type());
	
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
			
			_ when a.IsRecord(out var Fields)
			=> mStd.Call(() => {
				var Result = $"{{ ";
				foreach (var (Key, Value) in Fields.ToStream()) {
					Result += $", {Key}: {Value.ToText(NextLimit)}";
				}
				return Result + " }";
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
	
	public static tNat32
	PrefixHash(
		this tText a
	) {
		if (a is null) {
			return 0;
		}
		
		unchecked {
			var Hash = (tNat32)a.Length ^ 0xDEADBEEF;
			foreach (var ch in a) {
				Hash ^= ch;
				var Shift = Hash & 31;
				var Rot = (Hash << (tInt32)Shift) | (Hash << ((tInt32)Shift - 32));
				Hash ^= Rot;
			}
			Hash ^= Hash >> 16;
			return Hash;
		}
	}
}
