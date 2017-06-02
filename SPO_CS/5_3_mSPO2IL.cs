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
	// TODO: refactor
	
	public struct tModuleConstructor {
		internal mArrayList.tArrayList<mArrayList.tArrayList<mIL_AST.tCommandNode>> Defs;
	}
	
	public struct tDefConstructor {
		internal mArrayList.tArrayList<mIL_AST.tCommandNode> Commands;
		internal tInt32 LastTempReg;
		internal mArrayList.tArrayList<tText> KnownSymbols;
		internal mArrayList.tArrayList<tText> UnsolvedSymbols;
		internal tInt32 Index;
		internal tModuleConstructor ModuleConstructor;
	}
	
	//================================================================================
	public static tModuleConstructor
	NewModuleConstructor (
	//================================================================================
	) => new tModuleConstructor {
		Defs = mArrayList.List<mArrayList.tArrayList<mIL_AST.tCommandNode>>()
	};
	
	//================================================================================
	public static tDefConstructor
	NewDefConstructor (
		tModuleConstructor aModuleConstructor
	//================================================================================
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode>();
		aModuleConstructor.Defs.Push(Commands);
		return new tDefConstructor {
			Commands = Commands,
			KnownSymbols = mArrayList.List<tText>(),
			UnsolvedSymbols = mArrayList.List<tText>(),
			Index = DefIndex,
			ModuleConstructor = aModuleConstructor
		};
	}
	
	internal static tText TempReg(tInt32 a) => "t_" + a;
	internal static tText TempDef(tInt32 a) => "d_" + a;
	internal static tText Ident(tText a) => "_" + a;
	
	//================================================================================
	public static void
	MapArgs(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tMatchNode aArgs,
		tText aReg
	//================================================================================
	) {
		var Pattern = aArgs._Pattern;
		var Type = aArgs._Type;
		var ArgumentSymbols = aDefConstructor.KnownSymbols;
		
		switch (Pattern) {
			case mSPO_AST.tIdentNode IdentNode: {
				ArgumentSymbols.Push(IdentNode._Name);
				aDefConstructor.Commands.Push(mIL_AST.Alias(IdentNode._Name, aReg));
				break;
			}
			case mSPO_AST.tMatchTupleNode TupleNode: {
				var RestReg = aReg;
				var Items = TupleNode._Items;
				mStd.AssertEq(Items.Take(2).ToArrayList().Size(), 2);
				while (Items.MATCH(out var Item, out Items)) {
					aDefConstructor.LastTempReg += 1;
					var ItemReg = TempReg(aDefConstructor.LastTempReg);
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(ItemReg, RestReg));
					
					MapArgs(ref aDefConstructor, Item, ItemReg);
					
					aDefConstructor.LastTempReg += 1;
					var NewRestReg = TempReg(aDefConstructor.LastTempReg);
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(NewRestReg, RestReg));
					RestReg = NewRestReg;
				}
				break;
			}
			case mSPO_AST.tEmptyNode EmptyNode: {
				break;
			}
			case mSPO_AST.tMatchGuardNode GuardNode: {
				// TODO: ASSERT GuardNode._Guard
				MapArgs(ref aDefConstructor, GuardNode._Match, aReg);
				break;
			}
			default: {
				mStd.Assert(false);
				break;
			}
		}
	}
	
	//================================================================================
	public static mArrayList.tArrayList<mIL_AST.tCommandNode>
	UnrollList(
		tText aReg,
		mList.tList<tText> aSymbols,
		ref tInt32 aLastTempReg
	//================================================================================
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode>();
		switch (aSymbols.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				ExtractEnv.Push(mIL_AST.Alias(aSymbols._Head, aReg));
				break;
			}
			default: {
				var RestEnv = aReg;
				while (aSymbols.MATCH(out var Symbol, out aSymbols)) {
					ExtractEnv.Push(mIL_AST.GetFirst(Symbol, RestEnv));
					aLastTempReg += 1;
					var NewRestEnv = TempReg(aLastTempReg);
					ExtractEnv.Push(mIL_AST.GetSecond(NewRestEnv, RestEnv));
					RestEnv = NewRestEnv;
				}
				break;
			}
		}
		return ExtractEnv;
	}
	
	//================================================================================
	public static void
	InitMapLambda(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tLambdaNode aLambda
	//================================================================================
	) {
		var ArgumentSymbols = mList.List<tText>();
		MapArgs(ref aDefConstructor, aLambda._Head, mIL_AST.cArg);
		
		var ResultReg = MapExpresion(ref aDefConstructor, aLambda._Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(ResultReg, mIL_AST.cTrue));
		var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToLasyList(
		).Where(
			S1 => KnownSymbols.Where(S2 => S1 == S2).IsEmpty()
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	//================================================================================
	public static void
	FinishMapLambda(
		ref tDefConstructor aTempLambdaConstructor,
		mArrayList.tArrayList<tText> aUnsolveSymbols
	//================================================================================
	) {
		var Def = mArrayList.Concat(
			UnrollList(
				mIL_AST.cEnv,
				aUnsolveSymbols.ToLasyList(),
				ref aTempLambdaConstructor.LastTempReg
			),
			aTempLambdaConstructor.Commands
		);
		
		aTempLambdaConstructor.Commands = Def;
		aTempLambdaConstructor.ModuleConstructor.Defs.Set(
			aTempLambdaConstructor.Index,
			Def
		);
	}
	
	//================================================================================
	public static mStd.tTuple<tInt32, mArrayList.tArrayList<tText>>
	MapLambda(
		ref tModuleConstructor aModuleConstructor,
		mSPO_AST.tLambdaNode aLambda
	//================================================================================
	) {
		var TempLambdaDef = NewDefConstructor(aModuleConstructor);
		InitMapLambda(ref TempLambdaDef, aLambda);
		FinishMapLambda(
			ref TempLambdaDef,
			TempLambdaDef.UnsolvedSymbols
		);
		return mStd.Tuple(TempLambdaDef.Index, TempLambdaDef.UnsolvedSymbols);
	}
	
	//================================================================================
	public static tText
	InitLambda(
		ref tDefConstructor aDefConstructor,
		tText aDefName,
		mArrayList.tArrayList<tText> aEnv
	//================================================================================
	) {
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			var UnsolvedSymbols_ = aEnv.ToLasyList();
			while (UnsolvedSymbols_.MATCH(out var Symbol, out UnsolvedSymbols_)) {
				if (aDefConstructor.UnsolvedSymbols.ToLasyList().Where(S => S == Symbol).IsEmpty()) {
					aDefConstructor.UnsolvedSymbols.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0);
			} else {
				UnsolvedSymbols_ = aEnv.ToLasyList().Reverse();
				while (UnsolvedSymbols_.MATCH(out var Symbol_, out UnsolvedSymbols_)) {
					aDefConstructor.LastTempReg += 1;
					var NewArgReg = TempReg(aDefConstructor.LastTempReg);
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
		}
		
		aDefConstructor.LastTempReg += 1;
		var Lambda = TempReg(aDefConstructor.LastTempReg);
		aDefConstructor.Commands.Push(mIL_AST.Call(Lambda, aDefName, ArgReg));
		aDefConstructor.UnsolvedSymbols.Push(aDefName);
		return Lambda;
	}
	
	//================================================================================
	public static tText
	MapExpresion(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tExpressionNode aExpression
	//================================================================================
	) {
		switch (aExpression) {
			case mSPO_AST.tEmptyNode EmptyNode: {
				return mIL_AST.cEmpty;
			}
			case mSPO_AST.tFalseNode FalseNode: {
				return mIL_AST.cFalse;
			}
			case mSPO_AST.tTrueNode TrueNode: {
				return mIL_AST.cTrue;
			}
			case mSPO_AST.tNumberNode NumberNode: {
				aDefConstructor.LastTempReg += 1;
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(
						TempReg(aDefConstructor.LastTempReg), NumberNode._Value.ToString()
					)
				);
				return TempReg(aDefConstructor.LastTempReg);
			}
			case mSPO_AST.tIdentNode IdentNode: {
				if (
					aDefConstructor.UnsolvedSymbols.ToLasyList(
					).Where(
						a => a == IdentNode._Name
					).IsEmpty() &&
					aDefConstructor.Commands.ToLasyList(
					).Where(
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
								case mIL_AST.tCommandNodeType.SubPrefix: {
									return a._1 == IdentNode._Name;
								}
								case mIL_AST.tCommandNodeType.Assert:
								case mIL_AST.tCommandNodeType.Dev:
								case mIL_AST.tCommandNodeType.Module:
								case mIL_AST.tCommandNodeType.Pop:
								case mIL_AST.tCommandNodeType.Proof:
								case mIL_AST.tCommandNodeType.Push:
								case mIL_AST.tCommandNodeType.RepeatIf:
								case mIL_AST.tCommandNodeType.ReturnIf:
								default: {
									return false;
								}
							}
						}
					).IsEmpty()
				) {
					aDefConstructor.UnsolvedSymbols.Push(IdentNode._Name);
				}
				return IdentNode._Name;
			}
			case mSPO_AST.tCallNode CallNode: {
				var FuncReg = MapExpresion(ref aDefConstructor, CallNode._Func);
				var ArgReg = MapExpresion(ref aDefConstructor, CallNode._Arg);
				
				aDefConstructor.LastTempReg += 1;
				aDefConstructor.Commands.Push(
					mIL_AST.Call(TempReg(aDefConstructor.LastTempReg), FuncReg, ArgReg)
				);
				return TempReg(aDefConstructor.LastTempReg);
			}
			case mSPO_AST.tTupleNode TupleNode: {
				switch (TupleNode._Items.Take(2).ToArrayList().Size()) {
					case 0: {
						mStd.Assert(false);
						return mIL_AST.cEmpty;
					}
					case 1: {
						return MapExpresion(ref aDefConstructor, TupleNode._Items._Head);
					}
					default: {
						var List = TupleNode._Items.Reverse();
						var LastTupleReg = mIL_AST.cEmpty;
						while (List.MATCH(out var Item, out List)) {
							var ItemReg = MapExpresion(ref aDefConstructor, Item);
							aDefConstructor.LastTempReg += 1;
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
									TempReg(aDefConstructor.LastTempReg),
									ItemReg,
									LastTupleReg
								)
							);
							LastTupleReg = TempReg(aDefConstructor.LastTempReg);
						}
						return TempReg(aDefConstructor.LastTempReg);
					}
				}
			}
			case mSPO_AST.tPrefixNode PrefixNode: {
				var Reg = MapExpresion(ref aDefConstructor, PrefixNode._Element);
				aDefConstructor.LastTempReg += 1;
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(
						TempReg(aDefConstructor.LastTempReg),
						PrefixNode._Prefix,
						Reg
					)
				);
				return TempReg(aDefConstructor.LastTempReg);
			}
			case mSPO_AST.tTextNode TextNode: {
				mStd.Assert(false);
				return null;
			}
			case mSPO_AST.tLambdaNode LambdaNode: {
				MapLambda(
					ref aDefConstructor.ModuleConstructor,
					LambdaNode
				).MATCH(
					out var NewDefIndex,
					out var UnsolvedSymbols
				);
				return InitLambda(
					ref aDefConstructor,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			case mSPO_AST.tBlockNode BlockNode: {
				var CommandNodes = BlockNode._Commands;
				while (CommandNodes.MATCH(out var CommandNode, out CommandNodes)) {
					MapCommand(ref aDefConstructor, CommandNode);
				}
				// TODO: remove created symbols from unknown symbols
				return null;
			}
			case mSPO_AST.tIfNode IfNode: {
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode>();
				var Pairs = IfNode._Cases;
				while (Pairs.MATCH(out var Pair, out Pairs)) {
					Pair.MATCH(out var Test, out var Run);
					Ifs.Push(mSPO_AST.ReturnIf(Run, Test));
				}
				Ifs.Push(mSPO_AST.ReturnIf(mSPO_AST.Empty(), mSPO_AST.True())); // TODO: ASSERT FALSE
				
				aDefConstructor.LastTempReg += 1;
				var Reg = TempReg(aDefConstructor.LastTempReg);
				
				MapCommand(
					ref aDefConstructor,
					mSPO_AST.Assignment(
						mSPO_AST.Match(
							new mSPO_AST.tIdentNode{_Name = Reg},
							null
						),
						mSPO_AST.Call(
							mSPO_AST.Lambda(
								mSPO_AST.Match(
									mSPO_AST.Empty(),
									null
								),
								mSPO_AST.Block(Ifs.ToLasyList())
							),
							mSPO_AST.Empty()
						)
					)
				);
				return Reg; 
			}
			case mSPO_AST.tIfMatchNode IfMatchNode: {
				var Imput = new mSPO_AST.tIdentNode{
					_Name = MapExpresion(ref aDefConstructor, IfMatchNode._Expression)
				};
				
				var ModuleConstructor = aDefConstructor.ModuleConstructor;
				var KnownSymbols = aDefConstructor.KnownSymbols.ToLasyList();
				
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode>();
				
				var Rest = IfMatchNode._Cases;
				while (Rest.MATCH(out var Case, out Rest)) {
					Case.MATCH(out var Match, out var Run);
					var TestDef = NewDefConstructor(ModuleConstructor);
					
					MapMatchTest(ref TestDef, mIL_AST.cArg, Match);
					FinishMapLambda(ref TestDef, TestDef.UnsolvedSymbols);
					TestDef.Commands.Push(mIL_AST.ReturnIf(mIL_AST.cTrue, mIL_AST.cTrue));
					
					Ifs.Push(
						mSPO_AST.ReturnIf(
							mSPO_AST.Call(
								mSPO_AST.Lambda(Match, Run),
								Imput
							),
							mSPO_AST.Call(
								mSPO_AST.Call(
									new mSPO_AST.tIdentNode{ _Name = TempDef(TestDef.Index) },
									mSPO_AST.Tuple(
										TestDef.UnsolvedSymbols.ToLasyList(
										).Map(
											Symbol => (mSPO_AST.tExpressionNode)new mSPO_AST.tIdentNode{ _Name = Symbol }
										)
									)
								),
								Imput
							)
						)
					);
				}
				Ifs.Push(mSPO_AST.ReturnIf(mSPO_AST.Empty(), mSPO_AST.True())); // TODO: ASSERT FALSE
				
				aDefConstructor.LastTempReg += 1;
				var ResultReg = TempReg(aDefConstructor.LastTempReg);
				
				MapCommand(
					ref aDefConstructor,
					mSPO_AST.Assignment(
						mSPO_AST.Match(
							new mSPO_AST.tIdentNode{_Name = ResultReg},
							null
						),
						mSPO_AST.Call(
							mSPO_AST.Lambda(
								mSPO_AST.Match(
									mSPO_AST.Empty(),
									null
								),
								mSPO_AST.Block(Ifs.ToLasyList())
							),
							mSPO_AST.Empty()
						)
					)
				);
				return ResultReg; 
			}
			default: {
				mStd.Assert(false);
				return null;
			}
		}
	}
	
	//================================================================================
	public static tBool
	MapMatch(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tMatchNode aMatchNode,
		tText aValue
	//================================================================================
	) {
		var PatternNode = aMatchNode._Pattern;
		var TypeNode = aMatchNode._Type;
		
		switch (PatternNode) {
			case mSPO_AST.tIdentNode IdentNode: {
				aDefConstructor.Commands.Push(mIL_AST.Alias(IdentNode._Name, aValue));
				aDefConstructor.KnownSymbols.Push(IdentNode._Name);
				return true;
			}
			case mSPO_AST.tMatchPrefixNode PrefixNode: {
				aDefConstructor.LastTempReg += 1;
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						TempReg(aDefConstructor.LastTempReg),
						PrefixNode._Prefix,
						aValue
					)
				);
				return MapMatch(
					ref aDefConstructor,
					PrefixNode._Match,
					TempReg(aDefConstructor.LastTempReg)
				);
			}
			case mSPO_AST.tMatchTupleNode TupleNode: {
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
						aDefConstructor.LastTempReg += 1;
						HeadReg = TempReg(aDefConstructor.LastTempReg);
						aDefConstructor.Commands.Push(mIL_AST.GetFirst(HeadReg, OldTailReg));
						aDefConstructor.LastTempReg += 1;
						var NewTailReg = TempReg(aDefConstructor.LastTempReg);
						aDefConstructor.Commands.Push(mIL_AST.GetSecond(NewTailReg, OldTailReg));
						OldTailReg = NewTailReg;
					}
					
					MapMatch(ref aDefConstructor, Item, HeadReg);
				}
				return true;
			}
			default: {
				mStd.Assert(false);
				return true;
			}
		}
	}
	
	//================================================================================
	public static void
	MapMatchTest(
		ref tDefConstructor aDefConstructor,
		tText aInReg,
		mSPO_AST.tMatchNode aMatchNode
	//================================================================================
	) {
		var PatternNode = aMatchNode._Pattern;
		var TypeNode = aMatchNode._Type;
		
		switch (PatternNode) {
			case mSPO_AST.tIdentNode IdentNode: {
				if (
					aDefConstructor.KnownSymbols.ToLasyList(
					).Where(
						Symbol => Symbol == IdentNode._Name
					).IsEmpty()
				) {
					aDefConstructor.Commands.Push(
						mIL_AST.Alias(IdentNode._Name, aInReg)
					);
				} else {
					mStd.Assert(false);
				}
				break;
			}
			case mSPO_AST.tMatchPrefixNode PrefixNode: {
				var Prefix = PrefixNode._Prefix;
				var SubMatch = PrefixNode._Match;
				aDefConstructor.LastTempReg += 1;
				var Reg = TempReg(aDefConstructor.LastTempReg);
				aDefConstructor.LastTempReg += 1;
				var InvReg = TempReg(aDefConstructor.LastTempReg);
				aDefConstructor.LastTempReg += 1;
				var SubValue = TempReg(aDefConstructor.LastTempReg);
				aDefConstructor.Commands.Push(
					mIL_AST.HasPrefix(Reg, Prefix, aInReg)
				).Push(
					mIL_AST.XOr(InvReg, Reg, mIL_AST.cTrue)
				).Push(
					mIL_AST.ReturnIf(mIL_AST.cFalse, InvReg)
				).Push(
					mIL_AST.SubPrefix(SubValue, Prefix, aInReg)
				);
				MapMatchTest(ref aDefConstructor, SubValue, SubMatch);
				break;
			}
			case mSPO_AST.tMatchGuardNode GuardNode: {
				var SubMatch = GuardNode._Match;
				var Guard = GuardNode._Guard;
				
				MapMatchTest(ref aDefConstructor, aInReg, SubMatch);
				
				aDefConstructor.LastTempReg += 1;
				var InvReg = TempReg(aDefConstructor.LastTempReg);
				
				var TestReg = MapExpresion(ref aDefConstructor, Guard);
				aDefConstructor.Commands.Push(
					mIL_AST.XOr(InvReg, TestReg, mIL_AST.cTrue)
				).Push(
					mIL_AST.ReturnIf(mIL_AST.cFalse, InvReg)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode TupleNode: {
				var Items = TupleNode._Items;
				aDefConstructor.LastTempReg += 1;
				while (Items.MATCH(out var Item, out Items)) {
					aDefConstructor.LastTempReg += 1;
					var Reg = TempReg(aDefConstructor.LastTempReg);
					aDefConstructor.LastTempReg += 1;
					var NotReg = TempReg(aDefConstructor.LastTempReg);
					
					MapMatchTest(ref aDefConstructor, aInReg, Item);
					aDefConstructor.Commands.Push(
						mIL_AST.XOr(NotReg, Reg, mIL_AST.cTrue)
					).Push(
						mIL_AST.ReturnIf(mIL_AST.cFalse, NotReg)
					);
				}
				break;
			}
			case mSPO_AST.tNumberNode NumberNode: {
				aDefConstructor.LastTempReg += 1;
				var IntReg = TempReg(aDefConstructor.LastTempReg);
				aDefConstructor.LastTempReg += 1;
				var CondReg = TempReg(aDefConstructor.LastTempReg);
				aDefConstructor.LastTempReg += 1;
				var InvCondReg = TempReg(aDefConstructor.LastTempReg);
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(IntReg, $"{NumberNode._Value}")
				).Push(
					mIL_AST.IntsAreEq(CondReg, aInReg, IntReg)
				).Push(
					mIL_AST.XOr(InvCondReg, CondReg, mIL_AST.cTrue)
				).Push(
					mIL_AST.ReturnIf(mIL_AST.cFalse, InvCondReg)
				);
				break;
			}
			default: {
				mStd.Assert(false); // TODO: WIP
				break;
			}
		}
	}
	
	//================================================================================
	public static tBool
	MapAssignment(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tAssignmantNode aAssignment
	//================================================================================
	) {
		var ValueReg = MapExpresion(ref aDefConstructor, aAssignment._Src);
		return MapMatch(ref aDefConstructor, aAssignment._Des, ValueReg);
	}
	
	//================================================================================
	public static tBool
	MapReturnIf(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tReturnIfNode aReturn
	//================================================================================
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(
				MapExpresion(ref aDefConstructor, aReturn._Result),
				MapExpresion(ref aDefConstructor, aReturn._Condition)
			)
		);
		return true;
	}
	
	//================================================================================
	public static tBool
	MapRecursiveLambdas(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tRecLambdasNode aRecLambdas
	//================================================================================
	) {
		var NewDefIndices = mArrayList.List<tInt32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor>();
		var AllUnsolvedSymbols = mArrayList.List<tText>();
		
		var List = aRecLambdas._List;
		while (List.MATCH(out var RecLambdaItemNode, out List)) {
			var NewDefIndex = aDefConstructor.ModuleConstructor.Defs.Size();
			NewDefIndices.Push(NewDefIndex);
			AllUnsolvedSymbols.Push(TempDef(NewDefIndex));
			var TempLambdaDef = NewDefConstructor(aDefConstructor.ModuleConstructor);
			TempLambdaDefs.Push(TempLambdaDef);
			SPODefNodes.Push(RecLambdaItemNode);
		}
		
		var Max = NewDefIndices.Size();
		
		var FuncNames = aRecLambdas._List.Map(a => a._Ident._Name).ToArrayList();
		for (var I = 0; I < Max; I += 1) {
			var TempLambdaDef = TempLambdaDefs.Get(I);
			for (var J = 0; J < Max; J += 1) {
				TempLambdaDef.Commands.Push(
					mIL_AST.Call(FuncNames.Get(J), AllUnsolvedSymbols.Get(J), mIL_AST.cEnv)
				);
			}
		}
		
		for (var I = 0; I < Max; I += 1) {
			var DefIndex = NewDefIndices.Get(I);
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef= TempLambdaDefs.Get(I);
			
			InitMapLambda(ref TempLambdaDef, RecLambdaItemNode._Lambda);
			var KnownSymbols = TempLambdaDef.KnownSymbols.ToLasyList();
			var TempUnsolvedSymbols = TempLambdaDef.UnsolvedSymbols.ToLasyList(
			).Where(
				aUnsolved => (
					KnownSymbols.Where(aKnown => aKnown == aUnsolved).IsEmpty() &&
					aRecLambdas._List.Where(
						aRecLambda => aRecLambda._Ident._Name == aUnsolved
					).IsEmpty()
				)
			);
			
			while (TempUnsolvedSymbols.MATCH(out var Symbol, out TempUnsolvedSymbols)) {
				AllUnsolvedSymbols.Push(Symbol);
			}
		}
		
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			TempDefConstructor.LastTempReg += 1;
			var Reg = TempReg(TempDefConstructor.LastTempReg);
			FinishMapLambda(
				ref TempDefConstructor,
				AllUnsolvedSymbols
			);
			var DefIndex = TempDefConstructor.Index;
			
			aDefConstructor.ModuleConstructor.Defs.Set(
				DefIndex,
				aDefConstructor.ModuleConstructor.Defs.Get(DefIndex)
			);
			
			var FuncName = RecLambdaItemNode._Ident._Name;
			
			var ArgReg = mIL_AST.cEmpty;
			if (!AllUnsolvedSymbols.IsEmpty()) {
				var Iterator = AllUnsolvedSymbols.ToLasyList();
				while (Iterator.MATCH(out tText UnsolvedSymbol, out Iterator)) {
					if (
						aDefConstructor.UnsolvedSymbols.ToLasyList(
						).Where(
							a => a == UnsolvedSymbol
						).IsEmpty()
					) {
						aDefConstructor.UnsolvedSymbols.Push(UnsolvedSymbol);
					}
				}
				
				Iterator = AllUnsolvedSymbols.ToLasyList().Reverse();
				while (Iterator.MATCH(out var Symbol_, out Iterator)) {
					aDefConstructor.LastTempReg += 1;
					var NewArgReg = TempReg(aDefConstructor.LastTempReg);
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(NewArgReg, Symbol_, ArgReg));
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(mIL_AST.Call(FuncName, TempDef(DefIndex), ArgReg));
		}
		
		return true;
	}
	
	//================================================================================
	public static tBool
	MapCommand(
		ref tDefConstructor aDefConstructor,
		mSPO_AST.tCommandNode aCommand
	//================================================================================
	) {
		switch (aCommand) {
			case mSPO_AST.tAssignmantNode AssignmentNode: {
				return MapAssignment(ref aDefConstructor, AssignmentNode);
			}
			case mSPO_AST.tRecLambdasNode RecLambdasNode: {
				return MapRecursiveLambdas(ref aDefConstructor, RecLambdasNode);
			}
			case mSPO_AST.tReturnIfNode ReturnNode: {
				return MapReturnIf(ref aDefConstructor, ReturnNode);
			}
			default: {
				mStd.Assert(false);
				return false;
			}
		}
	}
	
	//================================================================================
	public static tModuleConstructor
	MapModule(
		mSPO_AST.tModuleNode aModuleNode
	//================================================================================
	) {
		var ModuleConstructor = NewModuleConstructor();
		var TempLambdaDef = NewDefConstructor(ModuleConstructor);
		InitMapLambda(
			ref TempLambdaDef,
			mSPO_AST.Lambda(
				aModuleNode._Import._Match,
				mSPO_AST.Block(
					mList.Concat(
						aModuleNode._Commands,
						mList.List<mSPO_AST.tCommandNode>(
							mSPO_AST.ReturnIf(aModuleNode._Export._Expression, mSPO_AST.True())
						)
					)
				)
			)
		);
		mStd.AssertEq(
			TempLambdaDef.UnsolvedSymbols.Size(),
			ModuleConstructor.Defs.Size() - 1
		);
		
		var DefSymbols = mArrayList.List<tText>();
		for (var I = 1; I < ModuleConstructor.Defs.Size(); I += 1) {
			DefSymbols.Push(TempDef(I));
		}
		FinishMapLambda(ref TempLambdaDef, DefSymbols);
		
		return ModuleConstructor;
	}
	
	#region TEST
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>>
	Test = mTest.Tests(
		mStd.Tuple(
			"MapExpresion",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tExpressionNode ExpressionNode;
					mStd.Assert(
						mSPO_Parser.EXPRESSION.ParseText(
							"2 .< (4 .+ 3) < 3",
							aStreamOut
						).MATCH(out ExpressionNode)
					);
					
					var Def = NewDefConstructor(NewModuleConstructor());
					mStd.AssertEq(MapExpresion(ref Def, ExpressionNode), TempReg(11));
					
					mStd.AssertEq(
						Def.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "3"),
							mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(3), "3"),
							mIL_AST.CreatePair(TempReg(4), TempReg(3), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(5), "4"),
							mIL_AST.CreatePair(TempReg(6), TempReg(5), TempReg(4)),
							mIL_AST.Call(TempReg(7), Ident("...+..."), TempReg(6)),
							mIL_AST.CreatePair(TempReg(8), TempReg(7), TempReg(2)),
							mIL_AST.CreateInt(TempReg(9), "2"),
							mIL_AST.CreatePair(TempReg(10), TempReg(9), TempReg(8)),
							mIL_AST.Call(TempReg(11), Ident("...<...<..."), TempReg(10))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapAssignment1",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tAssignmantNode AssignmentNode;
					mStd.Assert(
						mSPO_Parser.ASSIGNMENT.ParseText(
							"a := (1, 2)",
							aStreamOut
						).MATCH(out AssignmentNode)
					);
					
					var DefConstructor = NewDefConstructor(NewModuleConstructor());
					mStd.Assert(MapAssignment(ref DefConstructor, AssignmentNode));
					
					mStd.AssertEq(
						DefConstructor.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "2"),
							mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(3), "1"),
							mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
							
							mIL_AST.Alias(Ident("a"), TempReg(4))
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
						mSPO_Parser.ASSIGNMENT.ParseText(
							"(a, (b, c)) := (1, (2, 3))",
							aStreamOut
						).MATCH(out AssignmantNode)
					);
					
					var DefConstructor = NewDefConstructor(NewModuleConstructor());
					mStd.Assert(MapAssignment(ref DefConstructor, AssignmantNode));
					
					mStd.AssertEq(
						DefConstructor.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "3"),
							mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(3), "2"),
							mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
							mIL_AST.CreatePair(TempReg(5), TempReg(4), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(6), "1"),
							mIL_AST.CreatePair(TempReg(7), TempReg(6), TempReg(5)),
							
							mIL_AST.GetFirst(TempReg(8), TempReg(7)),
							mIL_AST.GetSecond(TempReg(9), TempReg(7)),
							mIL_AST.Alias(Ident("a"), TempReg(8)),
							mIL_AST.GetFirst(TempReg(10), TempReg(9)),
							mIL_AST.GetSecond(TempReg(11), TempReg(9)),
							mIL_AST.Alias(Ident("b"), TempReg(10)),
							mIL_AST.Alias(Ident("c"), TempReg(11))
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
						mSPO_Parser.ASSIGNMENT.ParseText(
							"(a, b, c) := (1, 2, 3)",
							aStreamOut
						).MATCH(out AssignmantNode)
					);
					
					var Module = NewDefConstructor(NewModuleConstructor());
					mStd.Assert(MapAssignment(ref Module, AssignmantNode));
					
					mStd.AssertEq(
						Module.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "3"),
							mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(3), "2"),
							mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
							mIL_AST.CreateInt(TempReg(5), "1"),
							mIL_AST.CreatePair(TempReg(6), TempReg(5), TempReg(4)),
							
							mIL_AST.GetFirst(TempReg(7), TempReg(6)),
							mIL_AST.GetSecond(TempReg(8), TempReg(6)),
							mIL_AST.Alias(Ident("a"), TempReg(7)),
							mIL_AST.GetFirst(TempReg(9), TempReg(8)),
							mIL_AST.GetSecond(TempReg(10), TempReg(8)),
							mIL_AST.Alias(Ident("b"), TempReg(9)),
							mIL_AST.Alias(Ident("c"), TempReg(10))
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
						mSPO_Parser.ASSIGNMENT.ParseText(
							"(a, b, (#bla (c , d))) := (1, 2, (#bla (3, 4)))",
							aStreamOut
						).MATCH(out AssignmantNode)
					);
					
					var DefConstructor = NewDefConstructor(NewModuleConstructor());
					mStd.Assert(MapAssignment(ref DefConstructor, AssignmantNode));
					
					mStd.AssertEq(
						DefConstructor.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreateInt(TempReg(1), "4"),
							mIL_AST.CreatePair(TempReg(2), TempReg(1), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(3), "3"),
							mIL_AST.CreatePair(TempReg(4), TempReg(3), TempReg(2)),
							mIL_AST.AddPrefix(TempReg(5), Ident("bla..."), TempReg(4)),
							mIL_AST.CreatePair(TempReg(6), TempReg(5), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(7), "2"),
							mIL_AST.CreatePair(TempReg(8), TempReg(7), TempReg(6)),
							mIL_AST.CreateInt(TempReg(9), "1"),
							mIL_AST.CreatePair(TempReg(10), TempReg(9), TempReg(8)),
							
							mIL_AST.GetFirst(TempReg(11), TempReg(10)),
							mIL_AST.GetSecond(TempReg(12), TempReg(10)),
							mIL_AST.Alias(Ident("a"), TempReg(11)),
							mIL_AST.GetFirst(TempReg(13), TempReg(12)),
							mIL_AST.GetSecond(TempReg(14), TempReg(12)),
							mIL_AST.Alias(Ident("b"), TempReg(13)),
							mIL_AST.SubPrefix(TempReg(15), Ident("bla..."), TempReg(14)),
							mIL_AST.GetFirst(TempReg(16), TempReg(15)),
							mIL_AST.GetSecond(TempReg(17), TempReg(15)),
							mIL_AST.Alias(Ident("c"), TempReg(16)),
							mIL_AST.Alias(Ident("d"), TempReg(17))
						)
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapLambda1",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tAssignmantNode AssignmantNode;
					mStd.Assert(
						mSPO_Parser.ASSIGNMENT.ParseText(
							"x := a => 2 .* a",
							aStreamOut
						).MATCH(out AssignmantNode)
					);
					
					var DefConstructor = NewDefConstructor(NewModuleConstructor());
					mStd.Assert(MapAssignment(ref DefConstructor, AssignmantNode));
					
					mStd.AssertEq(DefConstructor.ModuleConstructor.Defs.Size(), 2);
					mStd.AssertEq(
						DefConstructor.ModuleConstructor.Defs.Get(1),
						mArrayList.List(
							mIL_AST.Alias(Ident("...*..."), mIL_AST.cEnv),
							
							mIL_AST.Alias(Ident("a"), mIL_AST.cArg),
							
							mIL_AST.CreatePair(TempReg(1), Ident("a"), mIL_AST.cEmpty),
							mIL_AST.CreateInt(TempReg(2), "2"),
							mIL_AST.CreatePair(TempReg(3), TempReg(2), TempReg(1)),
							mIL_AST.Call(TempReg(4), Ident("...*..."), TempReg(3)),
							mIL_AST.ReturnIf(TempReg(4), mIL_AST.cTrue)
						)
					);
					
					mStd.AssertEq(DefConstructor.Index, 0);
					
					mStd.AssertEq(
						DefConstructor.Commands,
						mArrayList.List(
							mIL_AST.Call(TempReg(1), TempDef(1), Ident("...*...")),
							mIL_AST.Alias(Ident("x"), TempReg(1))
						)
					);
					
					mStd.AssertEq(
						DefConstructor.UnsolvedSymbols,
						mArrayList.List(Ident("...*..."), TempDef(1))
					);
					
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
						mSPO_Parser.ASSIGNMENT.ParseText(
							"...*...+... := (a, b, c) => (a .* b) .+ c",
							aStreamOut
						).MATCH(out AssignmantNode)
					);
					
					var DefConstructor = NewDefConstructor(NewModuleConstructor());
					mStd.Assert(MapAssignment(ref DefConstructor, AssignmantNode));
					
					mStd.AssertEq(DefConstructor.ModuleConstructor.Defs.Size(), 2);
					mStd.AssertEq(
						DefConstructor.ModuleConstructor.Defs.Get(1),
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.GetFirst(Ident("...+..."), mIL_AST.cEnv),
							mIL_AST.GetSecond(TempReg(13), mIL_AST.cEnv),
							mIL_AST.GetFirst(Ident("...*..."), TempReg(13)),
							mIL_AST.GetSecond(TempReg(14), TempReg(13)),
							
							mIL_AST.GetFirst(TempReg(1), mIL_AST.cArg),
							mIL_AST.Alias(Ident("a"), TempReg(1)),
							mIL_AST.GetSecond(TempReg(2), mIL_AST.cArg),
							mIL_AST.GetFirst(TempReg(3), TempReg(2)),
							mIL_AST.Alias(Ident("b"), TempReg(3)),
							mIL_AST.GetSecond(TempReg(4), TempReg(2)),
							mIL_AST.GetFirst(TempReg(5), TempReg(4)),
							mIL_AST.Alias(Ident("c"), TempReg(5)),
							mIL_AST.GetSecond(TempReg(6), TempReg(4)),
							
							mIL_AST.CreatePair(TempReg(7), Ident("c"), mIL_AST.cEmpty),
							mIL_AST.CreatePair(TempReg(8), Ident("b"), mIL_AST.cEmpty),
							mIL_AST.CreatePair(TempReg(9), Ident("a"), TempReg(8)),
							mIL_AST.Call(TempReg(10), Ident("...*..."), TempReg(9)),
							mIL_AST.CreatePair(TempReg(11), TempReg(10), TempReg(7)),
							mIL_AST.Call(TempReg(12), Ident("...+..."), TempReg(11)),
							mIL_AST.ReturnIf(TempReg(12), mIL_AST.cTrue)
						)
					);
					
					mStd.AssertEq(DefConstructor.Index, 0);
					mStd.AssertEq(
						DefConstructor.Commands,
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.CreatePair(TempReg(1), Ident("...*..."), mIL_AST.cEmpty),
							mIL_AST.CreatePair(TempReg(2), Ident("...+..."), TempReg(1)),
							mIL_AST.Call(TempReg(3), TempDef(1), TempReg(2)),
							mIL_AST.Alias(Ident("...*...+..."), TempReg(3))
						)
					);
					
					 mStd.AssertEq(
						DefConstructor.UnsolvedSymbols,
						mArrayList.List(Ident("...+..."), Ident("...*..."), TempDef(1))
					);
					
					return true;
				}
			)
		),
		mStd.Tuple(
			"MapNestedMatch",
			mTest.Test(
				(mStd.tAction<tText> aStreamOut) => {
					mSPO_AST.tLambdaNode LambdaNode;
					mStd.Assert(
						mSPO_Parser.LAMBDA.ParseText(
							"(a, b, (x, y, z)) => a .* z",
							aStreamOut
						).MATCH(out LambdaNode)
					);
					
					var ModuleConstructor = NewModuleConstructor();
					tInt32 DefIndex;
					var UnsolvedSymbols = mArrayList.List<tText>();
					MapLambda(
						ref ModuleConstructor,
						LambdaNode
					).MATCH(out DefIndex, out UnsolvedSymbols);
					
					mStd.AssertEq(ModuleConstructor.Defs.Size(), 1);
					mStd.AssertEq(DefIndex, 0);
					mStd.AssertEq(
						ModuleConstructor.Defs.Get(DefIndex),
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.Alias(Ident("...*..."), mIL_AST.cEnv),
							
							mIL_AST.GetFirst(TempReg(1), mIL_AST.cArg),
							mIL_AST.Alias(Ident("a"), TempReg(1)),
							mIL_AST.GetSecond(TempReg(2), mIL_AST.cArg),
							mIL_AST.GetFirst(TempReg(3), TempReg(2)),
							mIL_AST.Alias(Ident("b"), TempReg(3)),
							mIL_AST.GetSecond(TempReg(4), TempReg(2)),
							mIL_AST.GetFirst(TempReg(5), TempReg(4)),
							mIL_AST.GetFirst(TempReg(6), TempReg(5)),
							mIL_AST.Alias(Ident("x"), TempReg(6)),
							mIL_AST.GetSecond(TempReg(7), TempReg(5)),
							mIL_AST.GetFirst(TempReg(8), TempReg(7)),
							mIL_AST.Alias(Ident("y"), TempReg(8)),
							mIL_AST.GetSecond(TempReg(9), TempReg(7)),
							mIL_AST.GetFirst(TempReg(10), TempReg(9)),
							mIL_AST.Alias(Ident("z"), TempReg(10)),
							mIL_AST.GetSecond(TempReg(11), TempReg(9)),
							mIL_AST.GetSecond(TempReg(12), TempReg(4)),
							
							mIL_AST.CreatePair(TempReg(13), Ident("z"), mIL_AST.cEmpty),
							mIL_AST.CreatePair(TempReg(14), Ident("a"), TempReg(13)),
							mIL_AST.Call(TempReg(15), Ident("...*..."), TempReg(14)),
							mIL_AST.ReturnIf(TempReg(15), mIL_AST.cTrue)
						)
					);
					
					mStd.AssertEq(
						UnsolvedSymbols,
						mArrayList.List(Ident("...*..."))
					);
					
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
						mSPO_Parser.MODULE.ParseText(
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
								"x... := a => k .* a;",
								"y := .x 1",
								"",
								"§EXPORT y",
								""
							).Join((a1, a2) => a1 + "\n" + a2),
							aStreamOut
						).MATCH(out ModuleNode)
					);
					
					var ModuleConstructor = MapModule(ModuleNode);
					
					mStd.AssertEq(ModuleConstructor.Defs.Size(), 2);
					
					mStd.AssertEq(
						ModuleConstructor.Defs.Get(0),
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.Alias(TempDef(1), mIL_AST.cEnv),
							
							mIL_AST.GetFirst(TempReg(1), mIL_AST.cArg),
							mIL_AST.Alias(Ident("T"), TempReg(1)),
							mIL_AST.GetSecond(TempReg(2), mIL_AST.cArg),
							mIL_AST.GetFirst(TempReg(3), TempReg(2)),
							mIL_AST.Alias(Ident("...*..."), TempReg(3)),
							mIL_AST.GetSecond(TempReg(4), TempReg(2)),
							mIL_AST.GetFirst(TempReg(5), TempReg(4)),
							mIL_AST.Alias(Ident("k"), TempReg(5)),
							mIL_AST.GetSecond(TempReg(6), TempReg(4)),
							
							mIL_AST.CreatePair(TempReg(7), Ident("k"), mIL_AST.cEmpty),
							mIL_AST.CreatePair(TempReg(8), Ident("...*..."), TempReg(7)),
							mIL_AST.Call(TempReg(9), TempDef(1), TempReg(8)),
							mIL_AST.Alias(Ident("x..."), TempReg(9)),
							mIL_AST.CreateInt(TempReg(10), "1"),
							mIL_AST.Call(TempReg(11), Ident("x..."), TempReg(10)),
							mIL_AST.Alias(Ident("y"), TempReg(11)),
							mIL_AST.ReturnIf(Ident("y"), mIL_AST.cTrue),
							mIL_AST.ReturnIf(null, mIL_AST.cTrue) // TODO: remove this line
						)
					);
					
					mStd.AssertEq(
						ModuleConstructor.Defs.Get(1),
						mArrayList.List<mIL_AST.tCommandNode>(
							mIL_AST.GetFirst(Ident("...*..."), mIL_AST.cEnv),
							mIL_AST.GetSecond(TempReg(4), mIL_AST.cEnv),
							mIL_AST.GetFirst(Ident("k"), TempReg(4)),
							mIL_AST.GetSecond(TempReg(5), TempReg(4)),
							
							mIL_AST.Alias(Ident("a"), mIL_AST.cArg),
							
							mIL_AST.CreatePair(TempReg(1), Ident("a"), mIL_AST.cEmpty),
							mIL_AST.CreatePair(TempReg(2), Ident("k"), TempReg(1)),
							mIL_AST.Call(TempReg(3), Ident("...*..."), TempReg(2)),
							mIL_AST.ReturnIf(TempReg(3), mIL_AST.cTrue)
						)
					);
					
					return true;
				}
			)
		)
	);
	
	#endregion
}