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
		Int,
		Alias,
		Pair,
		First,
		Second,
		AddPrefix,
		SubPrefix,
		HasPrefix,
		Call,
		Push,
		Pop,
		ReturnIf,
		RepeatIf,
		Assert,
		Proof,
		Dev,
		Module
	}
	
	public class tCommandNode {
		internal tCommandNodeType _NodeType;
		internal tText _1;
		internal tText _2;
		internal tText _3;
		
		//================================================================================
		public tBool Equals(
			tCommandNode a
		//================================================================================
		) {
			return (
				!a.IsNull() &&
				_NodeType == a._NodeType &&
				_1 == a._1 &&
				_2 == a._2 &&
				_3 == a._3
			);
		}
		
		public override tBool Equals(object a) { return Equals(a as tCommandNode); }
		public override tText ToString() { return "{"+_NodeType+" "+_1+" "+_2+" "+_3+"}"; }
	}
	
	//================================================================================
	public static tCommandNode
	CommandNode(
		tCommandNodeType aNodeType,
		tText a1 = null,
		tText a2 = null,
		tText a3 = null
	//================================================================================
	) {
		return new tCommandNode{
			_NodeType = aNodeType,
			_1 = a1,
			_2 = a2,
			_3 = a3
		};
	}
	
	//================================================================================
	public static tBool
	MATCH(
		this tCommandNode aNode,
		tCommandNodeType aNodeType,
		out tText aId
	//================================================================================
	) {
		if (
			aNode._NodeType != aNodeType ||
			aNode._1.IsNull() ||
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
	MATCH(
		this tCommandNode aNode,
		tCommandNodeType aNodeType,
		out tText aId1,
		out tText aId2
	//================================================================================
	) {
		if (
			aNode._NodeType != aNodeType ||
			aNode._1.IsNull() ||
			aNode._2.IsNull() ||
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
	MATCH(
		this tCommandNode aNode,
		tCommandNodeType aNodeType,
		out tText aId1,
		out tText aId2,
		out tText aId3
	//================================================================================
	) {
		if (
			aNode._NodeType != aNodeType ||
			aNode._1.IsNull() ||
			aNode._2.IsNull() ||
			aNode._3.IsNull()
		) {
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
	public static mStd.tFunc<tCommandNode, tText, tText>
	CreateInt = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.Int, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText, tText>
	CreatePair = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => CommandNode(tCommandNodeType.Pair, aId1, aId2, aId3);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText>
	GetFirst = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.First, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText>
	GetSecond = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.Second, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText, tText>
	AddPrefix = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => CommandNode(tCommandNodeType.AddPrefix, aId1, aId2, aId3);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText, tText>
	SubPrefix = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => CommandNode(tCommandNodeType.SubPrefix, aId1, aId2, aId3);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText, tText>
	HasPrefix = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => CommandNode(tCommandNodeType.HasPrefix, aId1, aId2, aId3);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText, tText>
	Call = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => CommandNode(tCommandNodeType.Call, aId1, aId2, aId3);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText>
	Push = (
		tText aId
	//================================================================================
	) => CommandNode(tCommandNodeType.Push, aId);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode>
	Pop = (
	//================================================================================
	) => CommandNode(tCommandNodeType.Pop);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText>
	ReturnIf = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.ReturnIf, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText>
	RepeatIf = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.RepeatIf, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText>
	Assert = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.Assert, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText>
	Alias = (
		tText aId1,
		tText aId2
	//================================================================================
	) => CommandNode(tCommandNodeType.Alias, aId1, aId2);
	
	//================================================================================
	public static mStd.tFunc<tCommandNode, tText, tText, tText>
	Proof = (
		tText aId1,
		tText aId2,
		tText aId3
	//================================================================================
	) => CommandNode(tCommandNodeType.Proof, aId1, aId2, aId3);
	
	#region Test
	
	// TODO: add tests
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"???",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					return true;
				}
			)
		)
	);
	
	#endregion
	
}