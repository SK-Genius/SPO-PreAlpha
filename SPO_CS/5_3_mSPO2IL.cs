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

using tResults = mList.tList<mStd.tAny>;

using tIL_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

public static class mSPO2IL {
#if false
	public static mList.tList<mStd.tTuple<tText, mIL_AST.tCommandNode>>
	Create_IL_Module(
		mList.tList<mSPO_AST.tAssignmantNode> aSPO_Module
	) {
		var ResultDefs = mList.List<mList.tList<mStd.tTuple<tText, mIL_AST.tCommandNode>>>();
		
		var RestList = aSPO_Module;
		mSPO_AST.tAssignmantNode SPO_Assignment;
		while (RestList.MATCH(out SPO_Assignment, out RestList)) {
			if (SPO_Assignment._Src is mSPO_AST.tLambdaNode) {
				
			}
		}
		return null;
	}
#endif
}
