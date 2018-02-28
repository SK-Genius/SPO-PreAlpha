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
	}
	
	public static readonly tText cEmpty = "EMPTY";
	public static readonly tText cOne = "ONE";
	public static readonly tText cTrue = "TRUE";
	public static readonly tText cFalse = "FALSE";
	public static readonly tText cEnv = "ENV";
	public static readonly tText cObj = "OBJ";
	public static readonly tText cArg = "ARG";
	public static readonly tText cRes = "RES";
	
	public struct tCommandNode {
		internal tCommandNodeType _NodeType;
		internal tText _1;
		internal tText _2;
		internal tText _3;
		
		override public tText ToString() => $"{_NodeType} {_1} {_2} {_3}";
	}
	
	//================================================================================
	public static tBool
	TryGetResultReg(
		this tCommandNode aNode,
		out tText aResultReg
	//================================================================================
	) {
		switch (aNode._NodeType) {
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
			case tCommandNodeType.BoolXOr: {
				aResultReg = aNode._1;
				return true;
			}
			case tCommandNodeType.Assert:
			case tCommandNodeType.Pop:
			case tCommandNodeType.Proof:
			case tCommandNodeType.Push:
			case tCommandNodeType.RepeatIf:
			case tCommandNodeType.ReturnIf: {
				aResultReg = null;
				return false;
			}
			default: {
				throw mStd.Error("impossible");
			}
		}
	}
	
	//================================================================================
	public static tCommandNode
	CommandNode(
		tCommandNodeType aNodeType
	//================================================================================
	) => _CommandNode(aNodeType, null, null, null);
	
	//================================================================================
	public static tCommandNode
	CommandNode(
		tCommandNodeType aNodeType,
		tText a1
	//================================================================================
	) => _CommandNode(
		aNodeType,
		a1.AssertNotNull(),
		null,
		null
	);
	
	//================================================================================
	public static tCommandNode
	CommandNode(
		tCommandNodeType aNodeType,
		tText a1,
		tText a2
	//================================================================================
	) => _CommandNode(
		aNodeType,
		a1.AssertNotNull(),
		a2.AssertNotNull(),
		null
	);
	
	//================================================================================
	public static tCommandNode
	CommandNode(
		tCommandNodeType aNodeType,
		tText a1,
		tText a2,
		tText a3
	//================================================================================
	) => _CommandNode(
		aNodeType,
		a1.AssertNotNull(),
		a2.AssertNotNull(),
		a3.AssertNotNull()
	);
	
	//================================================================================
	private static tCommandNode
	_CommandNode(
		tCommandNodeType aNodeType,
		tText a1,
		tText a2,
		tText a3
	//================================================================================
	) => new tCommandNode{
		_NodeType = aNodeType,
		_1 = a1,
		_2 = a2,
		_3 = a3
	};
	
	//================================================================================
	public static tBool
	Match(
		this tCommandNode aNode,
		tCommandNodeType aNodeType
	//================================================================================
	) {
		if (aNode.Match(aNodeType, out var Id1, out var Id2, out var Id3)) {
			mDebug.AssertNull(Id1);
			mDebug.AssertNull(Id2);
			mDebug.AssertNull(Id3);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match(
		this tCommandNode aNode,
		tCommandNodeType aNodeType,
		out tText aId
	//================================================================================
	) {
		if (aNode.Match(aNodeType, out aId, out var Id2, out var Id3)) {
			mDebug.AssertNull(Id2);
			mDebug.AssertNull(Id3);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match(
		this tCommandNode aNode,
		tCommandNodeType aNodeType,
		out tText aId1,
		out tText aId2
	//================================================================================
	) {
		if (aNode.Match(aNodeType, out aId1, out aId2, out var Id3)) {
			mDebug.AssertNull(Id3);
			return true;
		}
		return false;
	}
	
	//================================================================================
	public static tBool
	Match(
		this tCommandNode aNode,
		tCommandNodeType aNodeType,
		out tText aId1,
		out tText aId2,
		out tText aId3
	//================================================================================
	) {
		if (aNode._NodeType == aNodeType) {
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
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	And = (
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.BoolAnd, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Or = (
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.BoolOr, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	XOr = (
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.BoolXOr, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsAreEq = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsAreEq, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsComp = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsComp, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	CreateInt = (
		tText aResReg,
		tText aIntReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Int, aResReg, aIntReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsAdd = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsAdd, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsSub = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsSub, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsMul = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.IntsMul, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	CreatePair = (
		tText aResReg,
		tText aArgReg1,
		tText aArgReg2
	//================================================================================
	) => _CommandNode(tCommandNodeType.Pair, aResReg, aArgReg1, aArgReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	GetFirst = (
		tText aResReg,
		tText aPairReg
	//================================================================================
	) => CommandNode(tCommandNodeType.First, aResReg, aPairReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	GetSecond = (
		tText aResReg,
		tText aPairReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Second, aResReg, aPairReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	AddPrefix = (
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.AddPrefix, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	SubPrefix = (
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.SubPrefix, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	HasPrefix = (
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.HasPrefix, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Call = (
		tText aResReg,
		tText aFuncReg,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.Call, aResReg, aFuncReg, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Exec = (
		tText aResReg,
		tText aProcReg,
		tText aArgReg
	//================================================================================
	) => _CommandNode(tCommandNodeType.Exec, aResReg, aProcReg, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText>
	Push = (
		tText aObjReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Push, aObjReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode>
	Pop = (
	//================================================================================
	) => CommandNode(tCommandNodeType.Pop);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	VarDef = (
		tText aResReg,
		tText aValueReg
	//================================================================================
	) => CommandNode(tCommandNodeType.VarDef, aResReg, aValueReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	VarSet = (
		tText aVarReg,
		tText aValueReg
	//================================================================================
	) => CommandNode(tCommandNodeType.VarSet, aVarReg, aValueReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	VarGet = (
		tText aValueReg,
		tText aVarReg
	//================================================================================
	) => CommandNode(tCommandNodeType.VarGet, aValueReg, aVarReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	ReturnIf = (
		tText aResReg,
		tText aCondReg
	//================================================================================
	) => CommandNode(tCommandNodeType.ReturnIf, aResReg, aCondReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	RepeatIf = (
		tText aArgReg,
		tText aCondReg
	//================================================================================
	) => CommandNode(tCommandNodeType.RepeatIf, aArgReg, aCondReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	Assert = (
		tText aPreCondReg,
		tText aPostCondReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Assert, aPreCondReg, aPostCondReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	Alias = (
		tText aResReg,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Alias, aResReg, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Proof = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => _CommandNode(tCommandNodeType.Proof, aId1, aId2, aId3);
	
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