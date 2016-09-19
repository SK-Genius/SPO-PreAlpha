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
	private static tText TempReg(tInt32 a) { return "t_" + a; }
	private static tText Ident(tText a) { return "_" + a; }
	
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
					mList.List(mIL_AST.CreateInt(TempReg(aLastReg), NumberNode._Value.ToString()))
				);
				return TempReg(aLastReg);
			}
		}
		{
			var IdentNode = aExpression as mSPO_AST.tIdentNode;
			if (!IdentNode.IsNull()) {
				return Ident(IdentNode._Name);
			}
		}
		{
			var CallNode = aExpression as mSPO_AST.tCallNode;
			if (!CallNode.IsNull()) {
				var FuncReg = MapExpresion(CallNode._Func, ref aCommands, ref aLastReg);
				var ArgReg = MapExpresion(CallNode._Arg, ref aCommands, ref aLastReg);
				aLastReg += 1;
				aCommands = mList.Concat(
					aCommands,
					mList.List(mIL_AST.Call(TempReg(aLastReg), FuncReg, ArgReg))
				);
				return TempReg(aLastReg);
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
					aCommands = mList.Concat(
						aCommands,
						mList.List(mIL_AST.CreatePair(TempReg(aLastReg), ItemReg, LastTupleReg))
					);
					LastTupleReg = TempReg(aLastReg);
				}
				return TempReg(aLastReg);
			}
		}
		{
			var PrefixNode = aExpression as mSPO_AST.tPrefixNode;
			if (!PrefixNode.IsNull()) {
				var Reg = MapExpresion(PrefixNode._Element, ref aCommands, ref aLastReg);
				aLastReg += 1;
				aCommands = mList.Concat(
					aCommands,
					mList.List(mIL_AST.AddPrefix(TempReg(aLastReg), PrefixNode._Prefix, Reg))
				);
				return TempReg(aLastReg);
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
	public static tBool
	MapMatch(
		mSPO_AST.tMatchNode aPattern,
		tText aValue,
		ref mList.tList<mIL_AST.tCommandNode> aCommands,
		ref tInt32 aLastReg
	//================================================================================
	) {
		{
			var PrefixNode = aPattern as mSPO_AST.tMatchPrefixNode;
			if (!PrefixNode.IsNull()) {
				aLastReg += 1;
				aCommands = mList.Concat(aCommands, mList.List(mIL_AST.SubPrefix(TempReg(aLastReg), PrefixNode._Prefix, aValue)));
				mStd.Assert(MapMatch(PrefixNode._Match, TempReg(aLastReg), ref aCommands, ref aLastReg));
				return true;
			}
		}
		
		mSPO_AST.tMatchItemNode Item;
		
		{
			mList.tList<mSPO_AST.tMatchItemNode> Rest;
			mStd.Assert(aPattern._Items.MATCH(out Item, out Rest));
			if (Rest.IsNull()) {
				aCommands = mList.Concat(
					aCommands,
					mList.List(mIL_AST.Alias(Ident((Item as mSPO_AST.tIdentNode)._Name), aValue))
				);
				return true;
			}
		}
		
		var OldTailReg = aValue;
		var List = aPattern._Items;
		while (List.MATCH(out Item, out List)) {
			tText HeadReg;
			if (List.IsNull()) {
				HeadReg = OldTailReg;
			} else {
				aLastReg += 1;
				HeadReg = TempReg(aLastReg);
				aCommands = mList.Concat(aCommands, mList.List(mIL_AST.GetFirst(HeadReg, OldTailReg)));
				aLastReg += 1;
				var NewTailReg = TempReg(aLastReg);
				aCommands = mList.Concat(aCommands, mList.List(mIL_AST.GetSecond(NewTailReg, OldTailReg)));
				OldTailReg = NewTailReg;
			}
			
			{
				var IdentNode = Item as mSPO_AST.tIdentNode;
				if (!IdentNode.IsNull()) {
					aCommands = mList.Concat(aCommands, mList.List(mIL_AST.Alias(Ident(IdentNode._Name), HeadReg)));
					continue;
				}
			}
			{
				var MatchNode = Item as mSPO_AST.tMatchNode;
				if (!MatchNode.IsNull()) {
					MapMatch(MatchNode, HeadReg, ref aCommands, ref aLastReg);
					continue;
				}
			}
			
			mStd.Assert(false);
			return false;
		}
		return true;
	}
	
	//================================================================================
	public static tBool
	MapAssignment(
		mSPO_AST.tAssignmantNode aAssignment,
		ref mList.tList<mIL_AST.tCommandNode> aCommands,
		ref tInt32 aLastReg
	//================================================================================
	) {
		var x = MapExpresion(aAssignment._Src, ref aCommands, ref aLastReg);
		return MapMatch(aAssignment._Des, x, ref aCommands, ref aLastReg);
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
					var LastReg = 0;
					
					mSPO_AST.tExpressionNode ExpressionNode;
					mStd.Assert(mSPO_Parser.ELEMENT.Parse("(2 .< (4 .+ 3) < 3)").MATCH(out ExpressionNode));
					
					var CommandNodes = mList.List<mIL_AST.tCommandNode>();
					mStd.AssertEq(MapExpresion(ExpressionNode, ref CommandNodes, ref LastReg), TempReg(9));
					
					mStd.AssertEq(
						CommandNodes,
						mList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "3"),
							mIL_AST.CreateInt(TempReg(2), "3"),
							mIL_AST.CreateInt(TempReg(3), "4"),
							mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
							mIL_AST.Call(TempReg(5), Ident("...+..."), TempReg(4)),
							mIL_AST.CreatePair(TempReg(6), TempReg(5), TempReg(1)),
							mIL_AST.CreateInt(TempReg(7), "2"),
							mIL_AST.CreatePair(TempReg(8), TempReg(7), TempReg(6)),
							mIL_AST.Call(TempReg(9), Ident("...<...<..."), TempReg(8))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapAssignment",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var LastReg = 0;
					
					mSPO_AST.tAssignmantNode AssignmentNode;
					mStd.Assert(mSPO_Parser.ASSIGNMENT.Parse("(a) := (1, 2)").MATCH(out AssignmentNode));
					
					var CommandNodes = mList.List<mIL_AST.tCommandNode>();
					mStd.Assert(MapAssignment(AssignmentNode, ref CommandNodes, ref LastReg));
					
					mStd.AssertEq(
						CommandNodes,
						mList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "2"),
							mIL_AST.CreateInt(TempReg(2), "1"),
							mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
							
							mIL_AST.Alias(Ident("a"), TempReg(3))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapAssignmentMatch",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var LastReg = 0;
					
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(mSPO_Parser.ASSIGNMENT.Parse("(a, (b, c)) := (1, (2, 3))").MATCH(out AssignmantNode));
					
					var CommandNodes = mList.List<mIL_AST.tCommandNode>();
					mStd.Assert(MapAssignment(AssignmantNode, ref CommandNodes, ref LastReg));
					
					mStd.AssertEq(
						CommandNodes,
						mList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "3"),
							mIL_AST.CreateInt(TempReg(2), "2"),
							mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
							mIL_AST.CreateInt(TempReg(4), "1"),
							mIL_AST.CreatePair(TempReg(5), TempReg(4), TempReg(3)),
							
							mIL_AST.GetFirst(TempReg(6), TempReg(5)),
							mIL_AST.GetSecond(TempReg(7), TempReg(5)),
							mIL_AST.Alias(Ident("a"), TempReg(6)),
							mIL_AST.GetFirst(TempReg(8), TempReg(7)),
							mIL_AST.GetSecond(TempReg(9), TempReg(7)),
							mIL_AST.Alias(Ident("b"), TempReg(8)),
							mIL_AST.Alias(Ident("c"), TempReg(9))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MatchTuple",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var LastReg = 0;
					
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(mSPO_Parser.ASSIGNMENT.Parse("(a, b, c) := (1, 2, 3)").MATCH(out AssignmantNode));
					
					var CommandNodes = mList.List<mIL_AST.tCommandNode>();
					mStd.Assert(MapAssignment(AssignmantNode, ref CommandNodes, ref LastReg));
					
					mStd.AssertEq(
						CommandNodes,
						mList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "3"),
							mIL_AST.CreateInt(TempReg(2), "2"),
							mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
							mIL_AST.CreateInt(TempReg(4), "1"),
							mIL_AST.CreatePair(TempReg(5), TempReg(4), TempReg(3)),
							
							mIL_AST.GetFirst(TempReg(6), TempReg(5)),
							mIL_AST.GetSecond(TempReg(7), TempReg(5)),
							mIL_AST.Alias(Ident("a"), TempReg(6)),
							mIL_AST.GetFirst(TempReg(8), TempReg(7)),
							mIL_AST.GetSecond(TempReg(9), TempReg(7)),
							mIL_AST.Alias(Ident("b"), TempReg(8)),
							mIL_AST.Alias(Ident("c"), TempReg(9))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapMatchPrefix",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					var LastReg = 0;
					
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(mSPO_Parser.ASSIGNMENT.Parse("(a, b, (#bla (c , d))) := (1, 2, (#bla (3, 4)))").MATCH(out AssignmantNode));
					
					var CommandNodes = mList.List<mIL_AST.tCommandNode>();
					mStd.Assert(MapAssignment(AssignmantNode, ref CommandNodes, ref LastReg));
					
					mStd.AssertEq(
						CommandNodes,
						mList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "4"),
							mIL_AST.CreateInt(TempReg(2), "3"),
							mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
							mIL_AST.AddPrefix(TempReg(4), "bla", TempReg(3)),
							mIL_AST.CreateInt(TempReg(5), "2"),
							mIL_AST.CreatePair(TempReg(6), TempReg(5), TempReg(4)),
							mIL_AST.CreateInt(TempReg(7), "1"),
							mIL_AST.CreatePair(TempReg(8), TempReg(7), TempReg(6)),
							
							mIL_AST.GetFirst(TempReg(9), TempReg(8)),
							mIL_AST.GetSecond(TempReg(10), TempReg(8)),
							mIL_AST.Alias(Ident("a"), TempReg(9)),
							mIL_AST.GetFirst(TempReg(11), TempReg(10)),
							mIL_AST.GetSecond(TempReg(12), TempReg(10)),
							mIL_AST.Alias(Ident("b"), TempReg(11)),
							mIL_AST.SubPrefix(TempReg(13), "bla", TempReg(12)),
							mIL_AST.GetFirst(TempReg(14), TempReg(13)),
							mIL_AST.GetSecond(TempReg(15), TempReg(13)),
							mIL_AST.Alias(Ident("c"), TempReg(14)),
							mIL_AST.Alias(Ident("d"), TempReg(15))
						)
					);
					
					return true;
				}
			)
		)
		// TODO NEXT BIG THING: "x := (a => (2 .* a))"
	);
	
	#endregion
}