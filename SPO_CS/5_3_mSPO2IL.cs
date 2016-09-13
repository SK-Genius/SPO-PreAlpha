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

using tText_Parser = mParserGen.tParser<mStd.tTuple<char, mStd.tAction<string>>>;

public static class mSPO2IL {
	
	//================================================================================
	public static tText
	MapExpresion(
		mSPO_AST.tExpressionNode aExpression,
		ref mList.tList<mIL_AST.tCommandNode> aCommands,
		ref tInt32 aLastReg
	//================================================================================
	) {
		{
			var NumberNode = aExpression as mSPO_AST.tNumberNode;
			if (!NumberNode.IsNull()) {
				aLastReg += 1;
				aCommands = mList.Concat(
					aCommands,
					mList.List(
						mIL_AST.CreateInt("temp"+aLastReg, NumberNode._Value.ToString())
					)
				);
				return "temp"+aLastReg;
			}
		}
		{
			var IdentNode = aExpression as mSPO_AST.tIdentNode;
			if (!IdentNode.IsNull()) {
				return IdentNode._Name;
			}
		}
		{
			var CallNode = aExpression as mSPO_AST.tCallNode;
			if (!CallNode.IsNull()) {
				var FuncReg = MapExpresion(CallNode._Func, ref aCommands, ref aLastReg);
				var ArgReg = MapExpresion(CallNode._Arg, ref aCommands, ref aLastReg);
				aLastReg += 1;
				aCommands = mList.Concat(aCommands, mList.List(mIL_AST.Call("temp"+aLastReg, FuncReg, ArgReg)));
				return "temp"+aLastReg;
			}
		}
		{
			var TupleNode = aExpression as mSPO_AST.tTupleNode;
			if (!TupleNode.IsNull()) {
				var List = TupleNode._Items.Reverse();
				var LastTupleReg = MapExpresion(List._Head, ref aCommands, ref aLastReg);;
				List = List.Skip(1);
				mSPO_AST.tExpressionNode Item;
				while (List.MATCH(out Item, out List)) {
					var ItemReg = MapExpresion(Item, ref aCommands, ref aLastReg);
					aLastReg += 1;
					aCommands = mList.Concat(aCommands, mList.List(mIL_AST.CreatePair("temp"+aLastReg, ItemReg, LastTupleReg)));
					LastTupleReg = "temp"+aLastReg;
				}
				return "temp"+aLastReg;
			}
		}
		{
			var TextNode = aExpression is mSPO_AST.tTextNode;
			if (!TextNode.IsNull()) {
				// TODO
			}
		}
		{
			var LambdaNode = aExpression as mSPO_AST.tLambdaNode;
			if (!LambdaNode.IsNull()) {
				// TODO
			}
		}
		
		mStd.Assert(false);
		return null;
	}
	
	//================================================================================
	public static tText
	MapMatch(
		mSPO_AST.tMatchNode aPattern,
		tText aValue,
		ref mList.tList<mIL_AST.tCommandNode> aCommands,
		ref tInt32 aLastReg
	//================================================================================
	) {
		var Result = mList.List<mIL_AST.tCommandNode>();
		
		// TODO
		
		return null;
	}
	
	//================================================================================
	public static void
	MapAssignment(
		mSPO_AST.tAssignmantNode aAssignment,
		ref mList.tList<mIL_AST.tCommandNode> aCommands,
		ref tInt32 aLastReg
	//================================================================================
	) {
		var x = MapExpresion(aAssignment._Src, ref aCommands, ref aLastReg);
		MapMatch(aAssignment._Des, x, ref aCommands, ref aLastReg);
	}
	
	#region TEST
	
	private static mParserGen.tResultList Parse(
		this tText_Parser aParser,
		tText aText
	) {
		mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> List;
		mTextParser.tFailInfo Info;
		mStd.tTuple<mParserGen.tResultList, mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>>> Result;
		mParserGen.tResultList ResultList;
		mList.tList<mStd.tTuple<tChar, mStd.tAction<tText>>> Rest;
		
		var Text1 = mTextParser.TextStream(mTextParser.TextToStream(aText));
		mStd.Assert(Text1.MATCH(out List, out Info));
		var MaybeResult1 = aParser.Parse(List);
		mStd.Assert(MaybeResult1.MATCH(out Result), "("+Info._Line+", "+Info._Coll+"): "+Info._ErrorMessage);
		mStd.Assert(Result.MATCH(out ResultList, out Rest));
		return ResultList;
	}

	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"MapExpresion",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var LastReg = 100;
					
					mSPO_AST.tExpressionNode z;
					mStd.Assert(mSPO_Parser.ELEMENT.Parse("(2 .< (4 .+ 3) < 3)").MATCH(out z));
					
					var x = mList.List<mIL_AST.tCommandNode>();
					mStd.AssertEq(MapExpresion(z, ref x, ref LastReg), "temp109");
					
					mStd.AssertEq(
						x,
						mList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt("temp101", "3"),
							mIL_AST.CreateInt("temp102", "3"),
							mIL_AST.CreateInt("temp103", "4"),
							mIL_AST.CreatePair("temp104", "temp103", "temp102"),
							mIL_AST.Call("temp105", "...+...", "temp104"),
							mIL_AST.CreatePair("temp106", "temp105", "temp101"),
							mIL_AST.CreateInt("temp107", "2"),
							mIL_AST.CreatePair("temp108", "temp107", "temp106"),
							mIL_AST.Call("temp109", "...<...<...", "temp108")
						)
					);
					
					return true;
				}
			)
		)
	);
	
	#endregion
}