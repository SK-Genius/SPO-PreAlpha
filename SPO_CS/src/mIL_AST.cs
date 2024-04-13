//IMPORT mDebug.cs
//IMPORT mMaybe.cs

#nullable enable

//#define MY_TRACE

public static class
mIL_AST {
	
	public record tDef<tPos>(
		tText Id,
		tText Type,
		mStream.tStream<tCommandNode<tPos>>? Commands
	);
	
	public static tDef<tPos>
	Def<tPos>(
		tText Id,
		tText Type,
		mStream.tStream<tCommandNode<tPos>>? Commands
	) => new(Id, Type, Commands);
	
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
		_, // unused
		_BeginExpressions_,
		Alias = _BeginExpressions_, // X := X
		Int,                        // X := §INT I
		IntsAreEq,                  // X := §INT X == X
		IntsComp,                   // X := §INT X <=> X
		IntsAdd,                    // X := §INT X + X
		IntsSub,                    // X := §INT X - X
		IntsMul,                    // X := §INT X * X
		IntsDiv,                    // X := §INT X / X
		BoolAnd,                    // X := §BOOL X & X
		BoolOr,                     // X := §BOOL X | X
		BoolXOr,                    // X := §BOOL X ^ X
		Pair,                       // X := X, X
		First,                      // X := §1ST X
		Second,                     // X := §2ND X
		ApplyPrefix,                // X := +#N X
		RemovePrefix,               // X := -#N X
		ExtendRec,                  // X := {X} +X
		DivideRec,                  // X := {X} /
		CallFunc,                   // X := .X X
		CallProc,                   // X := :X X
		VarDef,                     // X := §VAR X
		VarGet,                     // X := §VAR X ->
		_EndExpressions_,
		
		_BeginTypes_ = _EndExpressions_,
		TypePair = _BeginTypes_,  // T := [T, T]
		TypePrefix,               // T := [#N T]
		TypeRecord,               // T := [{T} +T]
		TypeFunc,                 // T := [T -> T]
		TypeMethod,               // T := [T : T]
		TypeSet,                  // T := [T | T]
		TypeCond,                 // T := [T & P]
		TypeVar,                  // T := [§VAR T]
		TypeFree,                 // T := t in T (see type definitions below)
		TypeRecursive,            // T := [§RECURSIVE t => T]
		TypeInterface,            // T := [§ANY t => T]
		TypeGeneric,              // T := [§ALL t => T]
		_EndTypes_,    
		
		_BeginCommands_ = _EndTypes_,
		VarSet = _BeginCommands_,   // §VAR X <- X
		ReturnIf,                   // §RETURN X IF X
		ReturnIfNotEmpty,           // §RETURN x IF_NOT_EMPTY
		TryAsInt,                   // X := §TRY X AS_INT
		TryAsBool,                   // X := §TRY X AS_BOOL
		TryAsPair,                  // X := §TRY X AS_PAIR
		TryAsRecord,                // X := §TRY X AS_RECORD
		TryAsVar,                   // X := §TRY X AS_VAR
		TryAsRef,                   // X := §TRY X AS_REF
		TryAsType,                  // X := §TRY X AS_TYPE
		TryRemovePrefixFrom,        // X := §TRY_REMOVE_PREFIX P FROM X

		// TODO:
		// y := §TRY x AS_FUNC_WITH_ARG_TYPE t # ???
		// y := §TRY x AS_METH_WITH_ARG_TYPE t # ???
		// or
		// y := §TRY .f x   # failed on wrong arg type or if f fails internal
		// y := §TRY :m e_x # failed on wrong arg type or if m fails internal

		Assert,                     // §ASSERT X
		Proof,                      // §ASSERT X => X
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
	public static readonly tText cSelfFunc = "SELF";
	public static readonly tText cEmptyType = "EMPTY_TYPE";
	public static readonly tText cBoolType = "BOOL_TYPE";
	public static readonly tText cIntType = "INT_TYPE";
	public static readonly tText cTypeType = "Type_TYPE";
	
	[DebuggerDisplay("{ToText(this)}")]
	public struct
	tCommandNode<tPos> {
		public tCommandNodeType NodeType;
		public tPos Pos;
		public tText _1;
		public mMaybe.tMaybe<tText> _2;
		public mMaybe.tMaybe<tText> _3;
		
		public override readonly string ToString() => ToText(this);
	}
	
	public static tText
	ToText<tPos>(
		this tCommandNode<tPos> a
	) => a.NodeType switch {
		tCommandNodeType.Alias => $"{a._1} := {a._2}",
		tCommandNodeType.Int => $"{a._1} := §INT {a._2}",
		tCommandNodeType.IntsAreEq => $"{a._1} := §INT {a._2} == {a._3}",
		tCommandNodeType.IntsComp => $"{a._1} := §INT {a._2} <=> {a._3}",
		tCommandNodeType.IntsAdd => $"{a._1} := §INT {a._2} + {a._3}",
		tCommandNodeType.IntsSub => $"{a._1} := §INT {a._2} - {a._3}",
		tCommandNodeType.IntsMul => $"{a._1} := §INT {a._2} * {a._3}",
		tCommandNodeType.IntsDiv => $"{a._1} := §INT {a._2} / {a._3}",
		tCommandNodeType.BoolAnd => $"{a._1} := §BOOL {a._2} & {a._3}",
		tCommandNodeType.BoolOr => $"{a._1} := §BOOL {a._2} | {a._3}",
		tCommandNodeType.BoolXOr => $"{a._1} := §BOOL {a._2} ^ {a._3}",
		tCommandNodeType.Pair => $"{a._1} := {a._2}, {a._3}",
		tCommandNodeType.First => $"{a._1} := §1ST {a._2}",
		tCommandNodeType.Second => $"{a._1} := §2ND {a._2}",
		tCommandNodeType.ApplyPrefix => $"{a._1} := +#{a._2} {a._3}",
		tCommandNodeType.RemovePrefix => $"{a._1} := -#{a._2} {a._3}",
		tCommandNodeType.ExtendRec => $"{a._1} := {{{a._2}}} + {a._3}",
		tCommandNodeType.DivideRec => $"{a._1} := {{{a._2}}} /",
		tCommandNodeType.CallFunc => $"{a._1} := .{a._2} {a._3}",
		tCommandNodeType.CallProc => $"{a._1} := §OBJ:{a._2} {a._3}",
		tCommandNodeType.VarDef => $"{a._1} := §VAR {a._2}",
		tCommandNodeType.VarGet => $"{a._1} := §VAR {a._2} ->",
		
		tCommandNodeType.TypePair => $"{a._1} := [{a._2}, {a._3}]",
		tCommandNodeType.TypePrefix => $"{a._1} := [#{a._2} {a._3}]",
		tCommandNodeType.TypeRecord => $"{a._1} := [{{{a._2}}} + {a._3}]",
		tCommandNodeType.TypeFunc => $"{a._1} := [{a._2} => {a._3}]",
		tCommandNodeType.TypeMethod => $"{a._1} := [{a._2} : {a._3}]",
		tCommandNodeType.TypeSet => $"{a._1} := [{a._2} | {a._3}]",
		tCommandNodeType.TypeCond => $"{a._1} := [{a._2} & {a._3}]",
		tCommandNodeType.TypeVar => $"{a._1} := [§VAR {a._2}]",
		tCommandNodeType.TypeFree => $"{a._1} := [{a._2}]",
		tCommandNodeType.TypeRecursive => $"{a._1} := [§REC {a._2} => {a._3}]",
		tCommandNodeType.TypeInterface => $"{a._1} := [§ANY {a._2} => {a._3}]",
		tCommandNodeType.TypeGeneric => $"{a._1} := [§ALL {a._2} => {a._3}]",
		
		tCommandNodeType.Assert => $"§ASSERT {a._1} => {a._2}",
		tCommandNodeType.Proof => $"§PROOF {a._2} => {a._3}",
		tCommandNodeType.VarSet => $"§VAR {a._1} <- {a._2}",
		tCommandNodeType.ReturnIf => $"§RETURN {a._2} IF {a._1}",
		tCommandNodeType.ReturnIfNotEmpty => $"§RETURN {a._2} IF_NOT_EMPTY",
		tCommandNodeType.TryAsInt => $"{a._1} := §TRY {a._2} AS_INT",
		tCommandNodeType.TryAsBool => $"{a._1} := §TRY {a._2} AS_BOOL",
		tCommandNodeType.TryAsRef => $"{a._1} := §TRY {a._2} AS_REF",
		tCommandNodeType.TryAsVar => $"{a._1} := §TRY {a._2} AS_VAR",
		tCommandNodeType.TryAsType => $"{a._1} := §TRY {a._2} AS_TYPE",
		tCommandNodeType.TryAsPair => $"{a._1} := §TRY {a._2} AS_PAIR",
		tCommandNodeType.TryAsRecord => $"{a._1} := §TRY {a._2} AS_RECORD",
		tCommandNodeType.TryRemovePrefixFrom => $"{a._1} := §TRY_REMOVE #{a._3} FROM {a._2}",
		_ => $"{a._1} := {a._2} {a._3}"
	};
	
	public static tText
	ToTextWithSpan<tPos>(
		this tCommandNode<tPos> a
	) => $"{a.Pos}: {a.ToText()}";
	
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
	Is<tPos>(
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
	Is<tPos>(
		this tCommandNode<tPos> aNode,
		tCommandNodeType aNodeType,
		out tPos aPos,
		out tText aId1,
		out tText aId2
	) {
		aPos = aNode.Pos;
		if (aNode.NodeType == aNodeType) {
			aId1 = aNode._1;
			aId2 = aNode._2.ElseThrow();
			return true;
		} else {
			aId1 = default!;
			aId2 = default!;
			return false;
		}
	}
	
	public static tBool
	Is<tPos>(
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
			aId2 = aNode._2.ElseThrow();
			aId3 = aNode._3.ElseThrow();
			return true;
		} else {
			aId1 = default!;
			aId2 = default!;
			aId3 = default!;
			return false;
		}
	}
	
	public static tCommandNode<tPos>
	Alias<tPos>(
		tPos aPos,
		tText aResReg,
		tText aBoolReg1
	) => CommandNode(tCommandNodeType.Alias, aPos, aResReg, aBoolReg1);
	
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
	AddPrefix<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	) => CommandNode(tCommandNodeType.ApplyPrefix, aPos, aResReg, aPrefix, aArgReg);
	
	public static tCommandNode<tPos>
	SubPrefix<tPos>(
		tPos aPos,
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	) => CommandNode(tCommandNodeType.RemovePrefix, aPos, aResReg, aPrefix, aArgReg);
	
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
	Assert<tPos>(
		tPos aPos,
		tText aPreCondReg,
		tText aPostCondReg
	) => CommandNode(tCommandNodeType.Assert, aPos, aPreCondReg, aPostCondReg);
	
	public static tCommandNode<tPos>
	Proof<tPos>(
		tPos aPos,
		tText aId1,
		tText aId2,
		tText aId3
	) => CommandNode(tCommandNodeType.Proof, aPos, aId1, aId2, aId3);
	
	public static tCommandNode<tPos>
	ReturnIf<tPos>(
		tPos aPos,
		tText aCondReg,
		tText aResReg
	) => CommandNode(tCommandNodeType.ReturnIf, aPos, aCondReg, aResReg);
	
	public static tCommandNode<tPos>
	ReturnIfNotEmpty<tPos>(
		tPos aPos,
		tText aArgReg
	) => CommandNode(tCommandNodeType.ReturnIfNotEmpty, aPos, cEmpty, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsBool<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsBool, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsInt<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsInt, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsType<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsType, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsPair<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsPair, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsRecord<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsRecord, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsVar<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsVar, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryAsRef<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg
	) => CommandNode(tCommandNodeType.TryAsRef, aPos, aResReg, aArgReg);
	
	public static tCommandNode<tPos>
	TryRemovePrefixFrom<tPos>(
		tPos aPos,
		tText aResReg,
		tText aArgReg,
		tText aPrefix
	) => CommandNode(tCommandNodeType.TryRemovePrefixFrom, aPos, aResReg, aArgReg, aPrefix);
	
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
	) => CommandNode(tCommandNodeType.TypeRecord, aPos, aId1, aId2, aId3);
	
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
