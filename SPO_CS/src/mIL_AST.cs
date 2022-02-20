//IMPORT mDebug.cs
//IMPORT mMaybe.cs

#nullable enable

//#define MY_TRACE

public static class
mIL_AST {
	
	public record tDef<tPos>(
		tText Ident,
		tText Type,
		mStream.tStream<tCommandNode<tPos>>? Commands
	);
	
	public static tDef<tPos>
	Def<tPos>(
		tText Ident,
		tText Type,
		mStream.tStream<tCommandNode<tPos>>? Commands
	) => new(Ident, Type, Commands);
	
	public record tModule<tPos>(
		mStream.tStream<tCommandNode<tPos>>? TypeDef,
		mStream.tStream<tDef<tPos>>? Defs
	);
	
	public static tModule<tPos>
	Module<tPos>(
		mStream.tStream<tCommandNode<tPos>>? aTypeDef,
		mStream.tStream<tDef<tPos>>? aDefs
	) => new(aTypeDef, aDefs);
	
	public enum
	tCommandNodeType {
		_BeginExpressions_,
		IsInt  = _BeginExpressions_,     // X := §IS_INT X
		Int,       // X := §INT I
		IntsAreEq, // X := §INT X == X
		IntsComp,  // X := §INT X <=> X
		IntsAdd,   // X := §INT X + X
		IntsSub,   // X := §INT X - X
		IntsMul,   // X := §INT X * X
		IntsDiv,   // X := §INT X / X
		IsBool,    // X := §IS_BOOL X
		BoolNot,   // X := §BOOL ! X
		BoolAnd,   // X := §BOOL X & X
		BoolOr,    // X := §BOOL X | X
		BoolXOr,   // X := §BOOL X ^ X
		Alias,     // X := X
		IsPair,    // X := §IS_PAIR
		Pair,      // X := X, X
		First,     // X := §FIRST X
		Second,    // X := §SECOND X
		IsPrefix,  // X := §IS_PREFIX X
		AddPrefix, // X := +N X
		SubPrefix, // X := -N X
		HasPrefix, // X := ?N X
		IsRecord,  // X := §IS_RECORD X
		ExtendRec, // X := {X} +X
		DivideRec, // X := {X} /
		CallFunc,  // X := .X X
		CallProc,  // X := :X X
		IsVar,     // X := §IS_VAR X
		VarDef,    // X := §VAR X
		VarGet,    // X := §VAR X ->
		_EndExpressions_,
		
		_BeginTypes_ = _EndExpressions_,
		IsType = _BeginTypes_,        // X := §IS_TYPE X
		TypePair,      // T := [T, T]
		TypePrefix,    // T := [+N T]
		TypeRec,       // T := [{T} +T]
		TypeFunc,      // T := [T -> T]
		TypeMethod,    // T := [T : T]
		TypeSet,       // T := [T | T]
		TypeCond,      // T := [T & P]
		TypeVar,       // T := [§VAR T]
		TypeFree,      // T := t in T (see type definitions below)
		TypeRecursive, // T := [§RECURSIVE t T]
		TypeInterface, // T := [§INTERFACE t T]
		TypeGeneric,   // T := [§GENERIC t T]
		_EndTypes_,
		
		_BeginCommands_ = _EndTypes_,
		VarSet = _BeginCommands_, // §VAR X <- X
		ReturnIf,            // §RETURN X IF X
		RepeatIf,            // §REPEAT X IF X
		TailCallIf,          // §TAIL_CALL X IF X
		Assert,              // §ASSERT X
		Proof,               // §ASSERT X => X
		_EndCommands_,
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
		
	[System.Diagnostics.DebuggerDisplay("{Pos}: {_1} = {NodeType} {_2.Else(\"-\")} {_3.Else(\"-\")}")]
	public struct
	tCommandNode<tPos> {
		public tCommandNodeType NodeType;
		public tPos Pos;
		public tText _1;
		public mMaybe.tMaybe<tText> _2;
		public mMaybe.tMaybe<tText> _3;
	}
	
	public static tText
	ToText<tPos>(
		this tCommandNode<tPos> a
	) => $"{a.Pos}: {a._1} = {a.NodeType} {a._2.Else("-")} {a._3.Else("-")}";
		
	public static mMaybe.tMaybe<tText>
	GetResultReg<tPos>(
		this tCommandNode<tPos> aNode
	) => (aNode.NodeType < tCommandNodeType._BeginCommands_)
		? mMaybe.Some(aNode._1)
		: mStd.cEmpty;
	
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		tPos aPos,
		tText a1
	) => _CommandNode(
		aNodeType,
		aPos,
		mAssert.IsNotNull(a1),
		mStd.cEmpty,
		mStd.cEmpty
	);
	
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		tPos aPos,
		tText a1,
		tText a2
	) => _CommandNode(
		aNodeType,
		aPos,
		mAssert.IsNotNull(a1),
		mAssert.IsNotNull(mMaybe.Some(a2)),
		mStd.cEmpty
	);
	
	public static tCommandNode<tPos>
	CommandNode<tPos>(
		tCommandNodeType aNodeType,
		tPos aPos,
		tText a1,
		tText a2,
		tText a3
	) => _CommandNode(
		aNodeType,
		aPos,
		mAssert.IsNotNull(a1),
		mAssert.IsNotNull(mMaybe.Some(a2)),
		mAssert.IsNotNull(mMaybe.Some(a3))
	);
	
	private static tCommandNode<tPos>
	_CommandNode<tPos>(
		tCommandNodeType aNodeType,
		tPos aPos,
		tText a1,
		mMaybe.tMaybe<tText> a2,
		mMaybe.tMaybe<tText> a3
	) => new() {
		NodeType = aNodeType,
		Pos = aPos,
		_1 = a1,
		_2 = a2,
		_3 = a3
	};
	
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out tPos aPos,
		out tText aId
	) {
		aPos = aNode.Pos;
		if (aNode.NodeType == aNodeType) {
			aId = aNode._1;
			return true;
		} else {
			aId = default!;
			return false;
		}
	}
	
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out tPos aPos,
		out tText aId1,
		out tText aId2
	) {
		aPos = aNode.Pos;
		if (aNode.NodeType == aNodeType) {
			aId1 = aNode._1;
			aId2 = aNode._2.ElseThrow("");
			return true;
		} else {
			aId1 = default!;
			aId2 = default!;
			return false;
		}
	}
	
	public static tBool
	Match<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out tPos aPos,
		out tText aId1,
		out tText aId2,
		out tText aId3
	) {
		aPos = aNode.Pos;
		if (aNode.NodeType == aNodeType) {
			aId1 = aNode._1;
			aId2 = aNode._2.ElseThrow("");
			aId3 = aNode._3.ElseThrow("");
			return true;
		} else {
			aId1 = default!;
			aId2 = default!;
			aId3 = default!;
			return false;
		}
	}
	
	public static tCommandNode<tPos>
	IsBool<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsBool, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	And<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	) => CommandNode(tCommandNodeType.BoolAnd, aPos, aResReg, aBoolReg1, aBoolReg2);
	
	public static tCommandNode<tPos>
	Or<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	) => CommandNode(tCommandNodeType.BoolOr, aPos, aResReg, aBoolReg1, aBoolReg2);
	
	public static tCommandNode<tPos>
	XOr<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	) => CommandNode(tCommandNodeType.BoolXOr, aPos, aResReg, aBoolReg1, aBoolReg2);
	
	public static tCommandNode<tPos>
	IsInt<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsInt, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	IntsAreEq<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	) => CommandNode(tCommandNodeType.IntsAreEq, aPos, aResReg, aIntReg1, aIntReg2);
	
	public static tCommandNode<tPos>
	IntsComp<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	) => CommandNode(tCommandNodeType.IntsComp, aPos, aResReg, aIntReg1, aIntReg2);
	
	public static tCommandNode<tPos>
	CreateInt<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg
	) => CommandNode(tCommandNodeType.Int, aPos, aResReg, aIntReg);
	
	public static tCommandNode<tPos>
	IntsAdd<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	) => CommandNode(tCommandNodeType.IntsAdd, aPos, aResReg, aIntReg1, aIntReg2);
	
	public static tCommandNode<tPos>
	IntsSub<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	) => CommandNode(tCommandNodeType.IntsSub, aPos, aResReg, aIntReg1, aIntReg2);
	
	public static tCommandNode<tPos>
	IntsMul<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	) => CommandNode(tCommandNodeType.IntsMul, aPos, aResReg, aIntReg1, aIntReg2);
	
	public static tCommandNode<tPos>
	IntsDiv<tPos>(
		tPos aPos,
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	) => CommandNode(tCommandNodeType.IntsDiv, aPos, aResReg, aIntReg1, aIntReg2);
	
	public static tCommandNode<tPos>
	IsPair<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsPair, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	CreatePair<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg1,
		tText aArgReg2
	) => CommandNode(tCommandNodeType.Pair, aPos, aResReg, aArgReg1, aArgReg2);
	
	public static tCommandNode<tPos>
	GetFirst<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPairReg
	) => CommandNode(tCommandNodeType.First, aPos, aResReg, aPairReg);
	
	public static tCommandNode<tPos>
	GetSecond<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPairReg
	) => CommandNode(tCommandNodeType.Second, aPos, aResReg, aPairReg);
	
	public static tCommandNode<tPos>
	IsPrefix<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsPrefix, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	AddPrefix<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	) => CommandNode(tCommandNodeType.AddPrefix, aPos, aResReg, aPrefix, aArgReg);
	
	public static tCommandNode<tPos>
	SubPrefix<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	) => CommandNode(tCommandNodeType.SubPrefix, aPos, aResReg, aPrefix, aArgReg);
	
	public static tCommandNode<tPos>
	HasPrefix<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	) => CommandNode(tCommandNodeType.HasPrefix, aPos, aResReg, aPrefix, aArgReg);
	
	public static tCommandNode<tPos>
	IsRecord<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsRecord, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	ExtendRec<tPos>(
		tPos aPos,
		tText aResReg,
		tText aRecord,
		tText aPrefix
	) => CommandNode(tCommandNodeType.ExtendRec, aPos, aResReg, aRecord, aPrefix);
	
	public static tCommandNode<tPos>
	DivideRec<tPos>(
		tPos aPos,
		tText aResReg,
		tText aRecord
	) => CommandNode(tCommandNodeType.DivideRec, aPos, aResReg, aRecord);
	
	public static tCommandNode<tPos>
	CallFunc<tPos>(
		tPos aPos,
		tText aResReg,
		tText aFuncReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.CallFunc, aPos, aResReg, aFuncReg, aArgReg);
	
	public static tCommandNode<tPos>
	CallProc<tPos>(
		tPos aPos,
		tText aResReg,
		tText aProcReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.CallProc, aPos, aResReg, aProcReg, aArgReg);
	
	public static tCommandNode<tPos>
	IsVar<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsVar, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	VarDef<tPos>(
		tPos aPos,
		tText aResReg,
		tText aValueReg
	) => CommandNode(tCommandNodeType.VarDef, aPos, aResReg, aValueReg);
	
	public static tCommandNode<tPos>
	VarSet<tPos>(
		tPos aPos,
		tText aVarReg,
		tText aValueReg
	) => CommandNode(tCommandNodeType.VarSet, aPos, aVarReg, aValueReg);
	
	public static tCommandNode<tPos>
	VarGet<tPos>(
		tPos aPos,
		tText aValueReg,
		tText aVarReg
	) => CommandNode(tCommandNodeType.VarGet, aPos, aValueReg, aVarReg);
	
	public static tCommandNode<tPos>
	ReturnIf<tPos>(
		tPos aPos,
		tText aResReg,
		tText aCondReg
	) => CommandNode(tCommandNodeType.ReturnIf, aPos, aResReg, aCondReg);
	
	public static tCommandNode<tPos>
	RepeatIf<tPos>(
		tPos aPos,
		tText aArgReg,
		tText aCondReg
	) => CommandNode(tCommandNodeType.RepeatIf, aPos, aArgReg, aCondReg);
	
	public static tCommandNode<tPos>
	TailCallIf<tPos>(
		tPos aPos,
		tText aCallerArgReg,
		tText aCondReg
	) => CommandNode(tCommandNodeType.TailCallIf, aPos, aCallerArgReg, aCondReg);
	
	public static tCommandNode<tPos>
	Assert<tPos>(
		tPos aPos,
		tText aPreCondReg,
		tText aPostCondReg
	) => CommandNode(tCommandNodeType.Assert, aPos, aPreCondReg, aPostCondReg);
	
	public static tCommandNode<tPos>
	Alias<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.Alias, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	Proof<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.Proof, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	IsType<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.IsType, aPos, aResReg, aBoolReg1);
	
	public static tCommandNode<tPos>
	TypeCond<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeCond, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeFunc<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeFunc, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeMethod<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeMethod, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypePair<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypePair, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypePrefix<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypePrefix, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeRecord<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeRec, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeSet<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeSet, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeVar<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2
	) => CommandNode(tCommandNodeType.TypeVar, aPos, aId1, aId2);
	
	public static tCommandNode<tPos>
	TypeFree<tPos>(
		tPos aPos,
		tText aId
	) => CommandNode(tCommandNodeType.TypeFree, aPos, aId);
	
	public static tCommandNode<tPos>
	TypeRecursive<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeRecursive, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeInterface<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeInterface, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	TypeGeneric<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.TypeGeneric, aPos, aId1, aId2, aId3);
}
