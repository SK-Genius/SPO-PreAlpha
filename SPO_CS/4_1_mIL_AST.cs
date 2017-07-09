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
		Int,                        // X := INT I
		IntsAreEq,                  // X := INT X == X
		IntsComp,                   // X := INT X <=> X
		IntsAdd,                    // X := INT X + X
		IntsSub,                    // X := INT X - X
		IntsMul,                    // X := INT X * X
		Not,                        // X := BOOL ! X
		And,                        // X := BOOL X & X
		Or,                         // X := BOOL X | X
		XOr,                        // X := BOOL X ^ X
		Alias,                      // X := X
		Pair,                       // X := (X, X)
		First,                      // X := FIRST X
		Second,                     // X := SECOND X
		AddPrefix,                  // X := +N X
		SubPrefix,                  // X := -N X
		HasPrefix,                  // X := ?N X
		Call,                       // X := .X X
		Exec,                       // X := :X X
		Push,                       // PUSH X
		Pop,                        // POP
		Var,                        // X := VAR X
		ReturnIf,                   // RETURN X IF X
		RepeatIf,                   // REPEAT X IF X
		Assert,                     // ASSERT X
		Proof,                      // ASSERT X => X
	}
	
	public const tText cEmpty = "EMPTY";
	public const tText cOne = "ONE";
	public const tText cTrue = "TRUE";
	public const tText cFalse = "FALSE";
	public const tText cEnv = "ENV";
	public const tText cObj = "OBJ";
	public const tText cArg = "ARG";
	public const tText cRes = "RES";
	
	public class tCommandNode {
		internal tCommandNodeType _NodeType;
		internal tText _1;
		internal tText _2;
		internal tText _3;
		
		//================================================================================
		public tBool Equals(
			tCommandNode a
		//================================================================================
		) => (
			!a.IsNull() &&
			_NodeType == a._NodeType &&
			_1 == a._1 &&
			_2 == a._2 &&
			_3 == a._3
		);
		
		override public tBool Equals(object a) => Equals(a as tCommandNode);
		override public tText ToString() => $"{{{_NodeType} {_1} {_2} {_3}}}";
	}
	
	//================================================================================
	public static tCommandNode
	CommandNode(
		tCommandNodeType aNodeType,
		tText a1 = null,
		tText a2 = null,
		tText a3 = null
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
		tCommandNodeType aNodeType,
		out tText aId
	//================================================================================
	) {
		if (
			aNode._NodeType != aNodeType ||
			!aNode._2.IsNull() ||
			!aNode._3.IsNull()
		) {
			aId = null;
			return false;
		}
		aId = aNode._1;
		return true;
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
		if (
			aNode._NodeType != aNodeType ||
			!aNode._3.IsNull()
		) {
			aId1 = null;
			aId2 = null;
			return false;
		}
		aId1 = aNode._1;
		aId2 = aNode._2;
		return true;
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
		if (aNode._NodeType != aNodeType) {
			aId1 = null;
			aId2 = null;
			aId3 = null;
			return false;
		}
		aId1 = aNode._1;
		aId2 = aNode._2;
		aId3 = aNode._3;
		return true;
	}
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	And = (
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.And, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Or = (
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.Or, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	XOr = (
		tText aResReg,
		tText aBoolReg1,
		tText aBoolReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.XOr, aResReg, aBoolReg1, aBoolReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsAreEq = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.IntsAreEq, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsComp = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.IntsComp, aResReg, aIntReg1, aIntReg2);
	
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
	) => CommandNode(tCommandNodeType.IntsAdd, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsSub = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.IntsSub, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	IntsMul = (
		tText aResReg,
		tText aIntReg1,
		tText aIntReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.IntsMul, aResReg, aIntReg1, aIntReg2);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	CreatePair = (
		tText aResReg,
		tText aArgReg1,
		tText aArgReg2
	//================================================================================
	) => CommandNode(tCommandNodeType.Pair, aResReg, aArgReg1, aArgReg2);
	
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
	) => CommandNode(tCommandNodeType.AddPrefix, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	SubPrefix = (
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.SubPrefix, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	HasPrefix = (
		tText aResReg,
		tText aPrefix,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.HasPrefix, aResReg, aPrefix, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Call = (
		tText aResReg,
		tText aFuncReg,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Call, aResReg, aFuncReg, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText, tText>
	Exec = (
		tText aResReg,
		tText aProcReg,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Exec, aResReg, aProcReg, aArgReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	Var = (
		tText aResReg,
		tText aValueReg
	//================================================================================
	) => CommandNode(tCommandNodeType.Var, aResReg, aValueReg);
	
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
	ReturnIf = (
		tText aCondReg,
		tText aResReg
	//================================================================================
	) => CommandNode(tCommandNodeType.ReturnIf, aCondReg, aResReg);
	
	//================================================================================
	public static readonly mStd.tFunc<tCommandNode, tText, tText>
	RepeatIf = (
		tText aCondReg,
		tText aArgReg
	//================================================================================
	) => CommandNode(tCommandNodeType.RepeatIf, aCondReg, aArgReg);
	
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
	) => CommandNode(tCommandNodeType.Proof, aId1, aId2, aId3);
	
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