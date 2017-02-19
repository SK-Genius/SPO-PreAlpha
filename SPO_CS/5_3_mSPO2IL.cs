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
		tInt32 aLastTempDef = 0
	//================================================================================
	) {
		return new tModuleConstructor {
			Commands = mArrayList.List<mIL_AST.tCommandNode>(),
			Defs = mArrayList.List<mStd.tTuple<tText, mArrayList.tArrayList<mIL_AST.tCommandNode>>>(),
			UnsolvedSymbols = mArrayList.List<tText>(),
			LastTempDef = aLastTempDef
		};
	}
	
	private static tText TempReg(tInt32 a) => "t_" + a;
	private static tText TempDef(tInt32 a) => "d_" + a;
	private static tText Ident(tText a) => "_" + a;
	
	//================================================================================
	public static void
	MapArgs(
		ref tModuleConstructor aModule,
		ref mList.tList<tText> aArgumentSymbols,
		mSPO_AST.tMatchNode aArgs,
		tText aReg
	//================================================================================
	) {
		var Pattern = aArgs._Pattern;
		var Type = aArgs._Type;
		
		{
			var IdentNode = Pattern as mSPO_AST.tIdentNode;
			if (!IdentNode.IsNull()) {
				aArgumentSymbols = mList.Concat(aArgumentSymbols, mList.List(IdentNode._Name));
				aModule.Commands.Push(mIL_AST.Alias(IdentNode._Name, aReg));
				return;
			}
		}
		{
			var TupleNode = Pattern as mSPO_AST.tMatchTupleNode;
			if (!TupleNode.IsNull()) {
				var RestReg = aReg;
				var Items = TupleNode._Items;
				if (!Items.IsEmpty()) {
					while (Items.MATCH(out var Item, out Items)) {
						if (Items.IsEmpty()) {
							MapArgs(ref aModule, ref aArgumentSymbols, Item, RestReg);
							break;
						}
						
						aModule.LastTempReg += 1;
						var ItemReg = TempReg(aModule.LastTempReg);
						aModule.Commands.Push(mIL_AST.GetFirst(ItemReg, RestReg));
						
						MapArgs(ref aModule, ref aArgumentSymbols, Item, ItemReg);
						
						aModule.LastTempReg += 1;
						var NewRestReg = TempReg(aModule.LastTempReg);
						aModule.Commands.Push(mIL_AST.GetSecond(NewRestReg, RestReg));
						RestReg = NewRestReg;
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
		var LambdaModule = NewModuleConstructor(aModule.LastTempDef);
		
		var ArgumentSymbols = mList.List<tText>();
		MapArgs(ref LambdaModule, ref ArgumentSymbols, aLambda._Head, "ARG");
		
		var ResultReg = MapExpresion(ref LambdaModule, aLambda._Body);
		var NewUnsolvedSymbols = LambdaModule.UnsolvedSymbols
			.ToLasyList()
			.Where(S1 => !ArgumentSymbols.Map(S2 => S1 == S2).Any())
			.ToArrayList();
		LambdaModule.UnsolvedSymbols = NewUnsolvedSymbols;
		var Def = mArrayList.Concat(LambdaModule.Commands, mArrayList.List(mIL_AST.ReturnIf(ResultReg, "TRUE")));
		
		if (!LambdaModule.UnsolvedSymbols.IsEmpty()) {
			var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode>();
			var Symbols = LambdaModule.UnsolvedSymbols.ToLasyList();
			var RestEnv = "ENV";
			while (Symbols.MATCH(out var Symbol, out Symbols)) {
				if (Symbols.IsEmpty()) {
					ExtractEnv.Push(mIL_AST.Alias(Symbol, RestEnv));
					break;
				}
				ExtractEnv.Push(mIL_AST.GetFirst(Symbol, RestEnv));
				LambdaModule.LastTempReg += 1;
				var NewRestEnv = TempReg(LambdaModule.LastTempReg);
				ExtractEnv.Push(mIL_AST.GetSecond(NewRestEnv, RestEnv));
				RestEnv = NewRestEnv;
			}
			
			Def = mArrayList.Concat(ExtractEnv, Def);
		}
		
		var DefsToCopy = LambdaModule.Defs.ToLasyList(); 
		while (DefsToCopy.MATCH(out var DefToCopy, out DefsToCopy)) {
			aModule.Defs.Push(DefToCopy);
		}
		
		aModule.LastTempDef = LambdaModule.LastTempDef;
		var NewDef = TempDef(aModule.LastTempDef);
		aModule.Defs.Push(mStd.Tuple(NewDef, Def));
		aModule.LastTempDef += 1;
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
				if (aModule.Defs.ToLasyList().Where(a => a._1 == IdentNode._Name).IsEmpty() &&
					aModule.Commands.ToLasyList()
						.Where(
							a => {
								switch (a._NodeType) {
									case mIL_AST.tCommandNodeType.AddPrefix:
									case mIL_AST.tCommandNodeType.Alias:
									case mIL_AST.tCommandNodeType.Call:
									case mIL_AST.tCommandNodeType.First:
									case mIL_AST.tCommandNodeType.HasPrefix:
									case mIL_AST.tCommandNodeType.Int:
									case mIL_AST.tCommandNodeType.Pair:
									case mIL_AST.tCommandNodeType.Second:
									case mIL_AST.tCommandNodeType.SubPrefix:
										return a._1 == IdentNode._Name;
									
									case mIL_AST.tCommandNodeType.Assert:
									case mIL_AST.tCommandNodeType.Dev:
									case mIL_AST.tCommandNodeType.Module:
									case mIL_AST.tCommandNodeType.Pop:
									case mIL_AST.tCommandNodeType.Proof:
									case mIL_AST.tCommandNodeType.Push:
									case mIL_AST.tCommandNodeType.RepeatIf:
									case mIL_AST.tCommandNodeType.ReturnIf:
									default:
										return false;
								}
							}
						)
						.IsEmpty()
				) {
					aModule.UnsolvedSymbols.Push(IdentNode._Name);
				}
				return IdentNode._Name;
			}
		}
		{
			var CallNode = aExpression as mSPO_AST.tCallNode;
			if (!CallNode.IsNull()) {
				var FuncReg = MapExpresion(ref aModule, CallNode._Func);
				var ArgReg = MapExpresion(ref aModule, CallNode._Arg);
				
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
				MapLambda(ref aModule, LambdaNode).MATCH(out var NewDef, out var UnsolvedSymbols);
				var ArgReg = "_";
				if (!UnsolvedSymbols.IsEmpty()) {
					var UnsolvedSymbols_ = UnsolvedSymbols.ToLasyList();
					while (UnsolvedSymbols_.MATCH(out var Symbol, out UnsolvedSymbols_)) {
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
		{
			var BlockNode = aExpression as mSPO_AST.tBlockNode;
			if (!BlockNode.IsNull()) {
				var CommandNodes = BlockNode._Commands;
				while (CommandNodes.MATCH(out var CommandNode, out CommandNodes)) {
					MapCommand(ref aModule, CommandNode);
				}
				return null;
			}
		}
		
		mStd.Assert(false);
		return null;
	}
	
	//================================================================================
	public static tBool
	MapMatch(
		ref tModuleConstructor aModule,
		mSPO_AST.tMatchNode aMatchNode,
		tText aValue
	//================================================================================
	) {
		var PatternNode = aMatchNode._Pattern;
		var TypeNode = aMatchNode._Type;
		
		{
			var IdentNode = PatternNode as mSPO_AST.tIdentNode;
			if (!IdentNode.IsNull()) {
				aModule.Commands.Push(mIL_AST.Alias(IdentNode._Name, aValue));
				return true;
			}
		}
		
		{
			var PrefixNode = PatternNode as mSPO_AST.tMatchPrefixNode;
			if (!PrefixNode.IsNull()) {
				aModule.LastTempReg += 1;
				aModule.Commands.Push(mIL_AST.SubPrefix(TempReg(aModule.LastTempReg), PrefixNode._Prefix, aValue));
				return MapMatch(ref aModule, PrefixNode._Match, TempReg(aModule.LastTempReg));
			}
		}
		
		{
			var TupleNode = PatternNode as mSPO_AST.tMatchTupleNode;
			if (!TupleNode.IsNull()) {
				mStd.Assert(TupleNode._Items.MATCH(out var Item, out var Rest));
				if (Rest.IsNull()) {
					mStd.Assert(false);
					return false;
				}
				
				var OldTailReg = aValue;
				var List = TupleNode._Items;
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
					
					MapMatch(ref aModule, Item, HeadReg);
				}
			}
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
	
	//================================================================================
	public static tBool
	MapReturn(
		ref tModuleConstructor aModule,
		mSPO_AST.tReturnNode aReturn
	//================================================================================
	) {
		aModule.Commands.Push(mIL_AST.ReturnIf(MapExpresion(ref aModule, aReturn._Result), "TRUE"));
		return true;
	}
	
	//================================================================================
	public static tBool
	MapCommand(
		ref tModuleConstructor aModule,
		mSPO_AST.tCommandNode aCommand
	//================================================================================
	) {
		var AssignmentNode = aCommand as mSPO_AST.tAssignmantNode;
		if (!AssignmentNode.IsNull()) {
			return MapAssignment(ref aModule, AssignmentNode);
		}
		
		var ReturnNode = aCommand as mSPO_AST.tReturnNode;
		if (!ReturnNode.IsNull()) {
			return MapReturn(ref aModule, ReturnNode);
		}
		
mStd.Assert(false);
		return false;
	}
	
	//================================================================================
	public static tBool
	MapModule(
		ref tModuleConstructor aModule,
		mSPO_AST.tModuleNode aModuleNode
	//================================================================================
	) {
		mStd.AssertEq(
			MapLambda(
				ref aModule,
				mSPO_AST.Lambda(
					aModuleNode._Import._Match,
					mSPO_AST.Block(
						mList.Concat(
							aModuleNode._Commands,
							mList.List<mSPO_AST.tCommandNode>(mSPO_AST.Return(aModuleNode._Export._Expression))
						)
					)
				)
			)._2,
			mArrayList.List<tText>()
		);
		
		return true;
	}
	
	#region TEST
	
	private static mParserGen.tResultList Parse(
		this tText_Parser aParser,
		tText aText
	) {
		var Text1 = mTextParser.TextStream(mTextParser.TextToStream(aText));
		Text1.MATCH(out var List, out var Info);
		var MaybeResult1 = aParser.Parse(List);
		mStd.Assert(MaybeResult1.MATCH(out var Result), $"({Info._Line}, {Info._Coll}): {Info._ErrorMessage}");
		Result.MATCH(out var ResultList, out var Rest);
		return ResultList;
	}
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"MapExpresion",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tExpressionNode ExpressionNode;
					mStd.Assert(mSPO_Parser.EXPRESSION.Parse("(2 .< (4 .+ 3) < 3)").MATCH(out ExpressionNode));
					
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
					mStd.Assert(mSPO_Parser.ASSIGNMENT.Parse("a := (1, 2)").MATCH(out AssignmentNode));
					
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
							"x := (a => (2 .* a))"
						).MATCH(out AssignmantNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(Module.Defs.Size(), 1);
					mStd.AssertEq(
						Module.Defs.Get(0),
						mStd.Tuple(
							TempDef(0),
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
							mIL_AST.Call(TempReg(1), TempDef(0), Ident("...*...")),
							mIL_AST.Alias(Ident("x"), TempReg(1))
						)
					);
					
					mStd.AssertEq(Module.UnsolvedSymbols, mArrayList.List(Ident("...*...")));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapLambda2",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tAssignmantNode AssignmantNode;
						mStd.Assert(
						mSPO_Parser.ASSIGNMENT.Parse(
							"...*...+... := ((a, b, c) => ((a .* b) .+ c))"
						).MATCH(out AssignmantNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(Module.Defs.Size(), 1);
					mStd.AssertEq(
						Module.Defs.Get(0),
						mStd.Tuple(
							TempDef(0),
							mArrayList.List<mIL_AST.tCommandNode>(
								mIL_AST.GetFirst(Ident("...+..."), "ENV"),
								mIL_AST.GetSecond(TempReg(9), "ENV"),
								mIL_AST.Alias(Ident("...*..."), TempReg(9)),
								
								mIL_AST.GetFirst(TempReg(1), "ARG"),
								mIL_AST.Alias(Ident("a"), TempReg(1)),
								mIL_AST.GetSecond(TempReg(2), "ARG"),
								mIL_AST.GetFirst(TempReg(3), TempReg(2)),
								mIL_AST.Alias(Ident("b"), TempReg(3)),
								mIL_AST.GetSecond(TempReg(4), TempReg(2)),
								mIL_AST.Alias(Ident("c"), TempReg(4)),
								
								mIL_AST.CreatePair(TempReg(5), Ident("a"), Ident("b")),
								mIL_AST.Call(TempReg(6), Ident("...*..."), TempReg(5)),
								mIL_AST.CreatePair(TempReg(7), TempReg(6), Ident("c")),
								mIL_AST.Call(TempReg(8), Ident("...+..."), TempReg(7)),
								mIL_AST.ReturnIf(TempReg(8), "TRUE")
							)
						)
					);
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreatePair(TempReg(1), Ident("...+..."), Ident("...*...")),
							mIL_AST.Call(TempReg(2), TempDef(0), TempReg(1)),
							mIL_AST.Alias(Ident("...*...+..."), TempReg(2))
						)
					);
					
					 mStd.AssertEq(Module.UnsolvedSymbols, mArrayList.List(Ident("...+..."), Ident("...*...")));
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapModule",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tModuleNode ModuleNode;
					mStd.Assert(
						mSPO_Parser.MODULE.Parse(
							mList.List(
								"§IMPORT (",
#if !true
								"	T € [[]]",
								"	...*... € [[T, T] => T],",
								"	k € T",
#else
								"	T,",
								"	...*...,",
								"	k",
#endif
								")",
								"",
								"x... := (a => (k .* a));",
								"y := (.x 1)",
								"",
								"§EXPORT y",
								""
							).Join((a1, a2) => a1 + "\n" + a2)
						).MATCH(out ModuleNode)
					);
					
					var Module = NewModuleConstructor();
					mStd.Assert(MapModule(ref Module, ModuleNode));
					
					mStd.AssertEq(Module.Defs.Size(), 2);
					
					mStd.AssertEq(
						Module.Defs.Get(0),
						mStd.Tuple(
							TempDef(0),
							mArrayList.List<mIL_AST.tCommandNode>(
								mIL_AST.GetFirst(Ident("...*..."), "ENV"),
								mIL_AST.GetSecond(TempReg(3), "ENV"),
								mIL_AST.Alias(Ident("k"), TempReg(3)),
								
								mIL_AST.Alias(Ident("a"), "ARG"),
								
								mIL_AST.CreatePair(TempReg(1), Ident("k"), Ident("a")),
								mIL_AST.Call(TempReg(2), Ident("...*..."), TempReg(1)),
								mIL_AST.ReturnIf(TempReg(2), "TRUE")
							)
						)
					);
					
					mStd.AssertEq(
						Module.Defs.Get(1),
						mStd.Tuple(
							TempDef(1),
							mArrayList.List<mIL_AST.tCommandNode>(
								mIL_AST.GetFirst(TempReg(1), "ARG"),
								mIL_AST.Alias(Ident("T"), TempReg(1)),
								mIL_AST.GetSecond(TempReg(2), "ARG"),
								mIL_AST.GetFirst(TempReg(3), TempReg(2)),
								mIL_AST.Alias(Ident("...*..."), TempReg(3)),
								mIL_AST.GetSecond(TempReg(4), TempReg(2)),
								mIL_AST.Alias(Ident("k"), TempReg(4)),
								
								mIL_AST.CreatePair(TempReg(5), Ident("...*..."), Ident("k")),
								mIL_AST.Call(TempReg(6), TempDef(0), TempReg(5)),
								mIL_AST.Alias(Ident("x..."), TempReg(6)),
								mIL_AST.CreateInt(TempReg(7), "1"),
								mIL_AST.Call(TempReg(8), Ident("x..."), TempReg(7)),
								mIL_AST.Alias(Ident("y"), TempReg(8)),
								mIL_AST.ReturnIf(Ident("y"), "TRUE"),
								mIL_AST.ReturnIf(null, "TRUE") // TODO: remove this line
							)
						)
					);
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>()
					);
					
					mStd.AssertEq(Module.UnsolvedSymbols, mArrayList.List<tText>());
					
					return true;
				}
			)
		)
	);
	
	#endregion
}