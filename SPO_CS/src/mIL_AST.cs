//#define TRACE

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

public static class mIL_AST {
	
	public enum tCommandNodeType {
		Int,       // X := §INT I
		IntsAreEq, // X := §INT X == X
		IntsComp,  // X := §INT X <=> X
		IntsAdd,   // X := §INT X + X
		IntsSub,   // X := §INT X - X
		IntsMul,   // X := §INT X * X
		BoolNot,       // X := §BOOL ! X
		BoolAnd,       // X := §BOOL X & X
		BoolOr,        // X := §BOOL X | X
		BoolXOr,       // X := §BOOL X ^ X
		Alias,     // X := X
		Pair,      // X := X, X
		First,     // X := §FIRST X
		Second,    // X := §SECOND X
		AddPrefix, // X := +N X
		SubPrefix, // X := -N X
		HasPrefix, // X := ?N X
		Call,      // X := .X X
		Exec,      // X := :X X
		Push,      // §PUSH X
		Pop,       // §POP
		VarDef,    // X := §VAR X
		VarSet,    // §VAR X <- X
		VarGet,    // X := §VAR X ->
		ReturnIf,  // §RETURN X IF X
		RepeatIf,  // §REPEAT X IF X
		Assert,    // §ASSERT X
		Proof,     // §ASSERT X => X
		
		TypePair, // T := [T, T]
		TypePrefix, // T := [+N T]
		TypeFunc, // T := [T -> T]
		TypeMethod, // T := [T : T]
		TypeSet, // T := [T | T]
		TypeCond, // T := [T & P]
		TypeVar, // T := [§VAR T]
		TypeRecursive, // T := [§RECURSIVE t T]
		TypeInterface, // T := [§INTERFACE t T]
		TypeGeneric, // T := [§GENERIC t T]
		
		TypeIs, // §TYPE_OF X IS T
	}
	
	public static readonly tText cEmpty = "EMPTY";
	public static readonly tText cOne = "ONE";
	public static readonly tText cTrue = "TRUE";
	public static readonly tText cFalse = "FALSE";
	public static readonly tText cEnv = "ENV";
	public static readonly tText cObj = "OBJ";
	public static readonly tText cArg = "ARG";
	public static readonly tText cRes = "RES";
	public static readonly tText cEmptyType = "EMPTY_TYPE";
	public static readonly tText cBoolType = "BOOL_TYPE";
	public static readonly tText cIntType = "INT_TYPE";
	public static readonly tText cTypeType = "Type_TYPE";
	
	public struct tCommandNode<tPos> {
		public tCommandNodeType NodeType;
		public mStd.tSpan<tPos> Span;
		public tText _1;
		public tText _2;
		public tText _3;
	}
	
	//================================================================================
	public static tBool
	TryGetResultReg<tPos>(
		this tCommandNode<tPos> aNode,
		out tText aResultReg
	//================================================================================
	) {
		switch (aNode.NodeType) {
			case tCommandNodeType.AddPrefix:
			case tCommandNodeType.Alias:
			case tCommandNodeType.BoolAnd:
			case tCommandNodeType.Call:
			case tCommandNodeType.Exec:
			case tCommandNodeType.First:
			case tCommandNodeType.HasPrefix:
			case tCommandNodeType.Int:
			case tCommandNodeType.IntsAdd:
			case tCommandNodeType.IntsAreEq:
			case tCommandNodeType.IntsComp:
			case tCommandNodeType.IntsMul:
			case tCommandNodeType.IntsSub:
			case tCommandNodeType.BoolNot:
			case tCommandNodeType.BoolOr:
			case tCommandNodeType.Pair:
			case tCommandNodeType.Second:
			case tCommandNodeType.SubPrefix:
			case tCommandNodeType.VarDef:
			case tCommandNodeType.BoolXOr:
			case tCommandNodeType.TypePrefix:
			case tCommandNodeType.TypeCond:
			case tCommandNodeType.TypeFunc:
			case tCommandNodeType.TypeMethod:
			case tCommandNodeType.TypePair:
			case tCommandNodeType.TypeSet:
			case tCommandNodeType.TypeVar:
			case tCommandNodeType.TypeRecursive:
			case tCommandNodeType.TypeInterface:
			case tCommandNodeType.TypeGeneric: {
				aResultReg = aNode._1;
				return true;
			}
			case tCommandNodeType.Assert:
			case tCommandNodeType.Pop:
			case tCommandNodeType.Proof:
			case tCommandNodeType.Push:
			case tCommandNodeType.RepeatIf:
			case tCommandNodeType.ReturnIf:
			case tCommandNodeType.TypeIs: {
				aResultReg = null;
				return false;
			}
			default: {
				throw mStd.Error($"impossible (Missing: {aNode.NodeType})");
			}
		}
	}
	
	//================================================================================
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => _CommandNode(aNodeType, aSpan, null, null, null);
	
	//================================================================================
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		mStd.tSpan<tPos> aSpan,
		tText a1
	//================================================================================
	) => _CommandNode(
		aNodeType,
		aSpan,
		a1.AssertNotNull(),
		null,
		null
	);
	
	//================================================================================
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		mStd.tSpan<tPos> aSpan,
		tText a1,
		tText a2
	//================================================================================
	) => _CommandNode(
		aNodeType,
		aSpan,
		a1.AssertNotNull(),
		a2.AssertNotNull(),
		null
	);
	
	//================================================================================
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		mStd.tSpan<tPos> aSpan,
		tText a1,
		tText a2,
		tText a3
	//================================================================================
	) => _CommandNode(
		aNodeType,
		aSpan,
		a1.AssertNotNull(),
		a2.AssertNotNull(),
		a3.AssertNotNull()
	);
	
	//================================================================================
	private static tCommandNode<tPos>
	_CommandNode<tPos>(
		tCommandNodeType aNodeType,
		mStd.tSpan<tPos> aSpan,
		tText a1,
		tText a2,
		tText a3
	//================================================================================
	) => new tCommandNode<tPos>{
		NodeType = aNodeType,
		Span = aSpan,
		_1 = a1,
		_2 = a2,
		_3 = a3
	};
	
	//================================================================================
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out mStd.tSpan<tPos> aSpan
	//================================================================================
	) {
		if (aNode.Match(aNodeType, out aSpan, out var Id1, out var Id2, out var Id3)) {
			mDebug.AssertNull(Id1);
			mDebug.AssertNull(Id2);
			mDebug.AssertNull(Id3);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out mStd.tSpan<tPos> aSpan,
		out tText aId
	//================================================================================
	) {
		if (aNode.Match(aNodeType, out aSpan, out aId, out var Id2, out var Id3)) {
			mDebug.AssertNull(Id2);
			mDebug.AssertNull(Id3);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out mStd.tSpan<tPos> aSpan,
		out tText aId1,
		out tText aId2
	//================================================================================
	) {
		if (aNode.Match(aNodeType, out aSpan, out aId1, out aId2, out var Id3)) {
			mDebug.AssertNull(Id3);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out mStd.tSpan<tPos> aSpan,
		out tText aId1,
		out tText aId2,
		out tText aId3
	//================================================================================
	) {
		aSpan = aNode.Span;
		if (aNode.NodeType == aNodeType) {
			aId1 = aNode._1;
			aId2 = aNode._2;
			aId3 = aNode._3;
			return true;
		}
		aId1 = null;
		aId2 = null;
		aId3 = null;
		return false;
	}
	
	//================================================================================
	public static tCommandNode<tPos>
	And<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.BoolAnd, aSpan, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	Or<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.BoolOr, aSpan, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	XOr<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.BoolXOr, aSpan, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	IntsAreEq<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsAreEq, aSpan, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	IntsComp<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsComp, aSpan, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	CreateInt<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aIntReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Int, aSpan, aResReg, aIntReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	IntsAdd<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsAdd, aSpan, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	IntsSub<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsSub, aSpan, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	IntsMul<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsMul, aSpan, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	CreatePair<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aArgReg1,
		tText aArgReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.Pair, aSpan, aResReg, aArgReg1, aArgReg2);
	
	//================================================================================
	public static tCommandNode<tPos>
	GetFirst<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aPairReg
	//================================================================================
	) => CommandNode(tCommandNodeType.First, aSpan, aResReg, aPairReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	GetSecond<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aPairReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Second, aSpan, aResReg, aPairReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	AddPrefix<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.AddPrefix, aSpan, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	SubPrefix<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.SubPrefix, aSpan, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	HasPrefix<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.HasPrefix, aSpan, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Call<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aFuncReg,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.Call, aSpan, aResReg, aFuncReg, aArgReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Exec<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aProcReg,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.Exec, aSpan, aResReg, aProcReg, aArgReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Push<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aObjReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Push, aSpan, aObjReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Pop<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => CommandNode(tCommandNodeType.Pop, aSpan);
	
	//================================================================================
	public static tCommandNode<tPos>
	VarDef<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aValueReg
	//================================================================================
	) => CommandNode(tCommandNodeType.VarDef, aSpan, aResReg, aValueReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	VarSet<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aVarReg,
		tText aValueReg
	//================================================================================
	) => CommandNode(tCommandNodeType.VarSet, aSpan, aVarReg, aValueReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	VarGet<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aValueReg,
		tText aVarReg
	//================================================================================
	) => CommandNode(tCommandNodeType.VarGet, aSpan, aValueReg, aVarReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	ReturnIf<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aCondReg
	//================================================================================
	) => CommandNode(tCommandNodeType.ReturnIf, aSpan, aResReg, aCondReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	RepeatIf<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aArgReg,
		tText aCondReg
	//================================================================================
	) => CommandNode(tCommandNodeType.RepeatIf, aSpan, aArgReg, aCondReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Assert<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aPreCondReg,
		tText aPostCondReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Assert, aSpan, aPreCondReg, aPostCondReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Alias<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aResReg,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Alias, aSpan, aResReg, aArgReg);
	
	//================================================================================
	public static tCommandNode<tPos>
	Proof<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.Proof, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeCond<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeCond, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeFunc<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeFunc, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeMethod<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeMethod, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypePair<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypePair, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypePrefix<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypePrefix, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeSet<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeSet, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeVar<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeVar, aSpan, aId1, aId2, null);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeRecursive<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeRecursive, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeInterface<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeInterface, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeGeneric<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeGeneric, aSpan, aId1, aId2, aId3);
	
	//================================================================================
	public static tCommandNode<tPos>
	TypeIs<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aId1,
		tText aId2
	//================================================================================
	) => _CommandNode(tCommandNodeType.TypeIs, aSpan, aId1, aId2, null);
	
	#region Test
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mIL_AST),
		mTest.Test(
			"TODO",
			aStreamOut => {
			}
		)
	);
	
	#endregion
}