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
	public struct tModuleConstructor {
		internal mArrayList.tArrayList<mIL_AST.tCommandNode> Commands;
		internal tInt32 LastTempReg;
		internal mArrayList.tArrayList<mStd.tTuple<tText, mArrayList.tArrayList<mIL_AST.tCommandNode>>> Defs;
		internal tInt32 LastTempDef;
		internal mArrayList.tArrayList<tText> UnsolvedSymbols;
	}
	
	//================================================================================
	public static tModuleConstructor
	NewModuleConstructor (
	//================================================================================
	) {
		return new tModuleConstructor {
			Commands = mArrayList.List<mIL_AST.tCommandNode>(),
			Defs = mArrayList.List<mStd.tTuple<tText, mArrayList.tArrayList<mIL_AST.tCommandNode>>>(),
			UnsolvedSymbols = mArrayList.List<tText>()
		};
	}
	
	
	private static tText TempReg(tInt32 a) { return "t_" + a; }
	private static tText TempDef(tInt32 a) { return "d_" + a; }
	private static tText Ident(tText a) { return "_" + a; }
	
	//================================================================================
	public static void
	MapArgs(
		ref tModuleConstructor aModule,
		ref mList.tList<tText> aArgumentSymbols,
		mSPO_AST.tMatchItemNode aArgs,
		tText aReg
	//================================================================================
	) {
		{
			var IdentNode = aArgs as mSPO_AST.tIdentNode;
			if (!IdentNode.IsNull()) {
				aArgumentSymbols = mList.Concat(aArgumentSymbols, mList.List(IdentNode._Name));
				aModule.Commands.Push(mIL_AST.Alias(IdentNode._Name, aReg));
				return;
			}
		}
		{
			var MatchNode = aArgs as mSPO_AST.tMatchNode;
			if (!MatchNode.IsNull()) {
				var RestReg = aReg;
				var Items = MatchNode._Items;
				if (!Items.IsEmpty()) {
					mSPO_AST.tMatchItemNode Item;
					while (Items.MATCH(out Item, out Items)) {
						if (Items.IsEmpty()) {
							MapArgs(ref aModule, ref aArgumentSymbols, Item, RestReg);
							break;
						}
						
						aModule.LastTempReg += 1;
						var ItemReg = TempReg(aModule.LastTempReg);
						aModule.Commands.Push(mIL_AST.GetFirst(ItemReg, aReg));
						
						MapArgs(ref aModule, ref aArgumentSymbols, Item, ItemReg);
						
						aModule.LastTempReg += 1;
						RestReg = TempReg(aModule.LastTempReg);
						aModule.Commands.Push(mIL_AST.GetSecond(RestReg, aReg));
					}
				}
				return;
			}
		}
		
		mStd.Assert(false);
	}
	
	//================================================================================
	public static mStd.tTuple<tText, mArrayList.tArrayList<tText>>
	MapLambda(
		ref tModuleConstructor aModule,
		mSPO_AST.tLambdaNode aLambda
	//================================================================================
	) {
		var LambdaModule = NewModuleConstructor();
		
		var ArgumentSymbols = mList.List<tText>();
		MapArgs(ref LambdaModule, ref ArgumentSymbols, aLambda._Head, "ARG");
		
		var ResultReg = MapExpresion(ref LambdaModule, aLambda._Body);
		var NewUnsolvedSymbols = mArrayList.List<tText>();
		LambdaModule.UnsolvedSymbols
			.ToLasyList()
			.Where(S1 => !ArgumentSymbols.Map(S2 => S1 == S2).Any())
			.Map(S => NewUnsolvedSymbols.Push(S));
		LambdaModule.UnsolvedSymbols = NewUnsolvedSymbols;
		var Def = mArrayList.Concat(LambdaModule.Commands, mArrayList.List(mIL_AST.ReturnIf(ResultReg, "TRUE")));
		
		if (!LambdaModule.UnsolvedSymbols.IsEmpty()) {
			var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode>();
			tText Symbol;
			var Symbols = LambdaModule.UnsolvedSymbols.ToLasyList();
			var RestEnv = "ENV";
			while (Symbols.MATCH(out Symbol, out Symbols)) {
				if (Symbols.IsEmpty()) {
					ExtractEnv.Push(mIL_AST.Alias(Symbol, RestEnv));
					break;
				}
				ExtractEnv.Push(mIL_AST.GetFirst(Symbol, RestEnv));
				aModule.LastTempReg += 1;
				var NewRestEnv = TempReg(aModule.LastTempReg);
				ExtractEnv.Push(mIL_AST.GetSecond(NewRestEnv, RestEnv));
				RestEnv = NewRestEnv;
			}
			
			Def = mArrayList.Concat(ExtractEnv, Def);
		}
		
		aModule.LastTempDef += 1;
		var NewDef = TempDef(aModule.LastTempDef);
		aModule.Defs.Push(mStd.Tuple(NewDef, Def));
		return mStd.Tuple(NewDef, LambdaModule.UnsolvedSymbols);
	}
	
	//================================================================================
	public static tText
	MapExpresion(
		ref tModuleConstructor aModule,
		mSPO_AST.tExpressionNode aExpression
	//================================================================================
	) {
		{
			var NumberNode = aExpression as mSPO_AST.tNumberNode;
			if (!NumberNode.IsNull()) {
				aModule.LastTempReg += 1;
				aModule.Commands.Push(mIL_AST.CreateInt(TempReg(aModule.LastTempReg), NumberNode._Value.ToString()));
				return TempReg(aModule.LastTempReg);
			}
		}
		{
			var IdentNode = aExpression as mSPO_AST.tIdentNode;
			if (!IdentNode.IsNull()) {
				aModule.UnsolvedSymbols.Push(IdentNode._Name);
				return IdentNode._Name;
			}
		}
		{
			var CallNode = aExpression as mSPO_AST.tCallNode;
			if (!CallNode.IsNull()) {
				tText FuncReg = MapExpresion(ref aModule, CallNode._Func);
				tText ArgReg = MapExpresion(ref aModule, CallNode._Arg);
				
				aModule.LastTempReg += 1;
				aModule.Commands.Push(mIL_AST.Call(TempReg(aModule.LastTempReg), FuncReg, ArgReg));
				return TempReg(aModule.LastTempReg);
			}
		}
		{
			var TupleNode = aExpression as mSPO_AST.tTupleNode;
			if (!TupleNode.IsNull()) {
				var List = TupleNode._Items.Reverse();
				var LastTupleReg = MapExpresion(ref aModule, List._Head);
				List = List.Skip(1);
				mSPO_AST.tExpressionNode Item;
				while (List.MATCH(out Item, out List)) {
					var ItemReg = MapExpresion(ref aModule, Item);
					aModule.LastTempReg += 1;
					aModule.Commands.Push(mIL_AST.CreatePair(TempReg(aModule.LastTempReg), ItemReg, LastTupleReg));
					LastTupleReg = TempReg(aModule.LastTempReg);
				}
				return TempReg(aModule.LastTempReg);
			}
		}
		{
			var PrefixNode = aExpression as mSPO_AST.tPrefixNode;
			if (!PrefixNode.IsNull()) {
				var Reg = MapExpresion(ref aModule, PrefixNode._Element);
				aModule.LastTempReg += 1;
				aModule.Commands.Push(
					mIL_AST.AddPrefix(
						TempReg(aModule.LastTempReg),
						PrefixNode._Prefix,
						Reg
					)
				);
				return TempReg(aModule.LastTempReg);
			}
		}
		{
			var TextNode = aExpression as mSPO_AST.tTextNode;
			if (!TextNode.IsNull()) {
				mStd.Assert(false);
				return null;
			}
		}
		{
			var LambdaNode = aExpression as mSPO_AST.tLambdaNode;
			if (!LambdaNode.IsNull()) {
				tText NewDef;
				mArrayList.tArrayList<tText> UnsolvedSymbols;
				mStd.Assert(MapLambda(ref aModule, LambdaNode).MATCH(out NewDef, out UnsolvedSymbols));
				tText Symbol;
				var ArgReg = "_";
				if (!UnsolvedSymbols.IsEmpty()) {
					var UnsolvedSymbols_ = UnsolvedSymbols.ToLasyList();
					while (UnsolvedSymbols_.MATCH(out Symbol, out UnsolvedSymbols_)) {
						if (aModule.UnsolvedSymbols.ToLasyList().Where(S => S == Symbol).IsEmpty()) {
							aModule.UnsolvedSymbols.Push(Symbol);
						}
					}
					
					UnsolvedSymbols_ = UnsolvedSymbols.ToLasyList().Reverse();
					mStd.Assert(UnsolvedSymbols_.MATCH(out Symbol, out UnsolvedSymbols_));
					ArgReg = Symbol;
					while (UnsolvedSymbols_.MATCH(out Symbol, out UnsolvedSymbols_)) {
						aModule.LastTempReg += 1;
						var NewArgReg = TempReg(aModule.LastTempReg);
						aModule.Commands.Push(mIL_AST.CreatePair(NewArgReg, Symbol, ArgReg));
						ArgReg = NewArgReg;
					}
				}
				
				aModule.LastTempReg += 1;
				var Lambda = TempReg(aModule.LastTempReg);
				aModule.Commands.Push(mIL_AST.Call(Lambda, NewDef, ArgReg));
				
				return Lambda;
			}
		}
		
		mStd.Assert(false);
		return null;
	}
	
	//================================================================================
	public static tBool
	MapMatch(
		ref tModuleConstructor aModule,
		mSPO_AST.tMatchNode aPattern,
		tText aValue
	//================================================================================
	) {
		{
			var PrefixNode = aPattern as mSPO_AST.tMatchPrefixNode;
			if (!PrefixNode.IsNull()) {
				aModule.LastTempReg += 1;
				aModule.Commands.Push(mIL_AST.SubPrefix(TempReg(aModule.LastTempReg), PrefixNode._Prefix, aValue));
				return MapMatch(ref aModule, PrefixNode._Match, TempReg(aModule.LastTempReg));
			}
		}
		
		mSPO_AST.tMatchItemNode Item;
		
		{
			mList.tList<mSPO_AST.tMatchItemNode> Rest;
			mStd.Assert(aPattern._Items.MATCH(out Item, out Rest));
			if (Rest.IsNull()) {
				aModule.Commands.Push(mIL_AST.Alias((Item as mSPO_AST.tIdentNode)._Name, aValue));
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
				aModule.LastTempReg += 1;
				HeadReg = TempReg(aModule.LastTempReg);
				aModule.Commands.Push(mIL_AST.GetFirst(HeadReg, OldTailReg));
				aModule.LastTempReg += 1;
				var NewTailReg = TempReg(aModule.LastTempReg);
				aModule.Commands.Push(mIL_AST.GetSecond(NewTailReg, OldTailReg));
				OldTailReg = NewTailReg;
			}
			
			{
				var IdentNode = Item as mSPO_AST.tIdentNode;
				if (!IdentNode.IsNull()) {
					aModule.Commands.Push(mIL_AST.Alias(IdentNode._Name, HeadReg));
					continue;
				}
			}
			{
				var MatchNode = Item as mSPO_AST.tMatchNode;
				if (!MatchNode.IsNull()) {
					MapMatch(ref aModule, MatchNode, HeadReg);
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
		ref tModuleConstructor aModule,
		mSPO_AST.tAssignmantNode aAssignment
	//================================================================================
	) {
		var ValueReg = MapExpresion(ref aModule, aAssignment._Src);
		return MapMatch(ref aModule, aAssignment._Des, ValueReg);

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
					mSPO_AST.tExpressionNode ExpressionNode;
					mStd.Assert(mSPO_Parser.ELEMENT.Parse("(2 .< (4 .+ 3) < 3)").MATCH(out ExpressionNode));
					
					var Module = NewModuleConstructor();
					mStd.AssertEq(MapExpresion(ref Module, ExpressionNode), TempReg(9));
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
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
					mSPO_AST.tAssignmantNode AssignmentNode;
					mStd.Assert(mSPO_Parser.ASSIGNMENT.Parse("(a) := (1, 2)").MATCH(out AssignmentNode));
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmentNode));
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
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
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(
						mSPO_Parser.ASSIGNMENT.Parse(
							"(a, (b, c)) := (1, (2, 3))"
						).MATCH(out AssignmantNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
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
					var Defs = mList.List<mStd.tTuple<tText, mList.tList<mIL_AST.tCommandNode>>>();
					
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(
						mSPO_Parser.ASSIGNMENT.Parse(
							"(a, b, c) := (1, 2, 3)"
						).MATCH(out AssignmantNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
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
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(
						mSPO_Parser.ASSIGNMENT
						.Parse("(a, b, (#bla (c , d))) := (1, 2, (#bla (3, 4)))")
						.MATCH(out AssignmantNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "4"),
							mIL_AST.CreateInt(TempReg(2), "3"),
							mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
							mIL_AST.AddPrefix(TempReg(4), Ident("bla"), TempReg(3)),
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
							mIL_AST.SubPrefix(TempReg(13), Ident("bla"), TempReg(12)),
							mIL_AST.GetFirst(TempReg(14), TempReg(13)),
							mIL_AST.GetSecond(TempReg(15), TempReg(13)),
							mIL_AST.Alias(Ident("c"), TempReg(14)),
							mIL_AST.Alias(Ident("d"), TempReg(15))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapLambda",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(
						mSPO_Parser.ASSIGNMENT.Parse(
							"(x) := ((a) => (2 .* a))"
						).MATCH(out AssignmantNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(Module.Defs.Size(), 1);
					mStd.AssertEq(
						Module.Defs.Get(0),
						mStd.Tuple(
							TempDef(1),
							mArrayList.List<mIL_AST.tCommandNode>(
								mIL_AST.Alias(Ident("...*..."), "ENV"),
								
								mIL_AST.Alias(Ident("a"), "ARG"),
								
								mIL_AST.CreateInt(TempReg(1), "2"),
								mIL_AST.CreatePair(TempReg(2), TempReg(1), Ident("a")),
								mIL_AST.Call(TempReg(3), Ident("...*..."), TempReg(2)),
								mIL_AST.ReturnIf(TempReg(3), "TRUE")
							)
						)
					);
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.Call(TempReg(1), TempDef(1), Ident("...*...")),
							mIL_AST.Alias(Ident("x"), TempReg(1))
						)
					);
					
					mStd.AssertEq(Module.UnsolvedSymbols, mArrayList.List(Ident("...*...")));
					
					return true;
				}
			)
		)
	);
	
	#endregion
}