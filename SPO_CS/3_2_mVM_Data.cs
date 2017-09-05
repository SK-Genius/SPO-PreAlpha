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
	
	#region tData
	
	internal enum tDataType {
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
	public static bool
	Match(
		this tData aArg,
		out tBool a1,
		out tBool a2
	//================================================================================
	) {
		mStd.Assert(
			aArg.MatchPair(
				out var Arg1,
				out var Arg2_
			)
		);
		mStd.Assert(
			Arg2_.MatchPair(
				out var Arg2,
				out var Arg_
			)
		);
		mStd.Assert(Arg_.MatchEmpty());
		mStd.Assert(Arg1.MatchBool(out a1));
		mStd.Assert(Arg2.MatchBool(out a2));
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
			aArg.MatchPair(
				out var Arg1,
				out var Arg2_
			)
		);
		mStd.Assert(
			Arg2_.MatchPair(
				out var Arg2,
				out var Arg_
			)
		);
		mStd.Assert(Arg_.MatchEmpty());
		mStd.Assert(Arg1.MatchInt(out a1));
		mStd.Assert(Arg2.MatchInt(out a2));
		return true;
	}
	
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
		aValue = default(t);
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
	) => Data(tDataType.Pair, aFirst._IsMutable | aSecond._IsMutable, aFirst, aSecond);
	
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
		mVM.tProcDef aDef,
		tData aEnv
	//================================================================================
	) {
		mStd.AssertNot(aEnv._IsMutable);
		// In the end this is the place where the compiler will called !!!
		return Data(tDataType.Proc, false, aDef, aEnv);
	}
	
	//================================================================================
	public static tBool
	MatchProc(
		this tData aData,
		out mVM.tProcDef aDef,
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
		mStd.AssertNot(aEnv._IsMutable);
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
		mVM.tProcDef aDef
	//================================================================================
	) => Data(tDataType.Def, false, aDef);
	
	//================================================================================
	public static tBool
	MatchDef(
		this tData aData,
		out mVM.tProcDef aDef
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
	
	#endregion
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mVM_Data),
		mTest.Test(
			"ExternDef",
			aDebugStream => {
				// TODO
			}
		)
	);
	
	#endregion
}