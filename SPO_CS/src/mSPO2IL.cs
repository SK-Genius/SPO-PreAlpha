﻿//IMPORT mSPO_AST.cs
//IMPORT mIL_AST.cs
//IMPORT mArrayList.cs
//IMPORT mPerf.cs

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

// TODO: maybe use nested functions to cleanup ???

public static class
mSPO2IL {
	
	public struct
	tModuleConstructor<tPos> {
		public mArrayList.tArrayList<mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>> Defs;
		internal mStd.tFunc<tPos, tPos, tPos> MergePos;
	}
	
	public struct
	tDefConstructor<tPos> {
		public mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>> Commands;
		public tInt32 LastTempReg;
		public mArrayList.tArrayList<tText> KnownSymbols;
		public mArrayList.tArrayList<(tText Ident, tPos Pos)> UnsolvedSymbols;
		public tInt32 Index;
		public tModuleConstructor<tPos> ModuleConstructor;
	}
	
	public static tModuleConstructor<tPos>
	NewModuleConstructor<tPos>(
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) => new tModuleConstructor<tPos> {
		Defs = mArrayList.List<mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>>(),
		MergePos = aMergePos
	};
	
	public static tDefConstructor<tPos>
	NewDefConstructor<tPos>(
		this tModuleConstructor<tPos> aModuleConstructor
	) {
		var DefIndex = aModuleConstructor.Defs.Size();
		var Commands = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		aModuleConstructor.Defs.Push(Commands);
		return new tDefConstructor<tPos> {
			Commands = Commands,
			KnownSymbols = mArrayList.List<tText>(),
			UnsolvedSymbols = mArrayList.List<(tText Ident, tPos Pos)>(),
			Index = DefIndex,
			ModuleConstructor = aModuleConstructor
		};
	}
	
	public static tText TempReg(tInt32 a) => "t_" + a;
	public static tText TempDef(tInt32 a) => "d_" + a;
	public static tText Ident(tText a) => "_" + a;
	
	public static tText
	CreateTempReg<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor
	) {
		aDefConstructor.LastTempReg += 1;
		return TempReg(aDefConstructor.LastTempReg);
	}
	
	public static mArrayList.tArrayList<mIL_AST.tCommandNode<tPos>>
	UnrollList<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aReg,
		mStream.tStream<(tText Ident, tPos Pos)> aSymbols
	) {
		var ExtractEnv = mArrayList.List<mIL_AST.tCommandNode<tPos>>();
		switch (aSymbols.Take(2).ToArrayList().Size()) {
			case 0: {
				break;
			}
			case 1: {
				mDebug.Assert(aSymbols.Match(out var Head, out var _));
				ExtractEnv.Push(mIL_AST.Alias(aPos, Head.Ident, aReg));
				break;
			}
			default: {
				var RestEnv = aReg;
				while (aSymbols.Match(out var Symbol, out aSymbols)) {
					ExtractEnv.Push(mIL_AST.GetFirst(aPos, Symbol.Ident, RestEnv));
					var NewRestEnv = aDefConstructor.CreateTempReg();
					ExtractEnv.Push(mIL_AST.GetSecond(aPos, NewRestEnv, RestEnv));
					RestEnv = NewRestEnv;
				}
				break;
			}
		}
		return ExtractEnv;
	}
	
	public static void
	InitMapLambda<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		aDefConstructor.MapMatch(aLambdaNode.Head, mIL_AST.cArg);
		var ResultReg = aDefConstructor.MapExpresion(aLambdaNode.Body);
		if (!(aLambdaNode.Body is mSPO_AST.tBlockNode<tPos>)) {
			aDefConstructor.Commands.Push(
				mIL_AST.ReturnIf(aLambdaNode.Body.Pos, ResultReg, mIL_AST.cTrue)
			);
		}
		var KnownSymbols = aDefConstructor.KnownSymbols.ToStream();
		aDefConstructor.UnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToStream(
		).Where(
			S1 => KnownSymbols.All(S2 => S1.Ident != S2)
		).ToArrayList();
	}
	
	public static void
	InitMapMethod<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		aDefConstructor.MapMatch(aMethodNode.Arg, mIL_AST.cArg);
		aDefConstructor.MapMatch(aMethodNode.Obj, mIL_AST.cObj);
		
		var ResultReg = aDefConstructor.MapExpresion(aMethodNode.Body);
		aDefConstructor.Commands.Push(mIL_AST.ReturnIf(aMethodNode.Pos, ResultReg, mIL_AST.cTrue));
		var KnownSymbols = aDefConstructor.KnownSymbols.ToStream();
		var NewUnsolvedSymbols = aDefConstructor.UnsolvedSymbols.ToStream(
		).Where(
			S1 => KnownSymbols.All(S2 => S1.Ident != S2)
		).ToArrayList();
		aDefConstructor.UnsolvedSymbols = NewUnsolvedSymbols;
	}
	
	public static void
	FinishMapProc<tPos>(
		this ref tDefConstructor<tPos> aTempDefConstructor,
		tPos aPos,
		mArrayList.tArrayList<(tText Ident, tPos Pos)> aUnsolveSymbols
	) {
		var Def = mArrayList.Concat(
			aTempDefConstructor.UnrollList(
				aPos,
				mIL_AST.cEnv,
				aUnsolveSymbols.ToStream()
			),
			aTempDefConstructor.Commands
		);
		
		aTempDefConstructor.Commands = Def;
		aTempDefConstructor.ModuleConstructor.Defs.Set(
			aTempDefConstructor.Index,
			Def
		);
	}
	
	public static (tInt32, mArrayList.tArrayList<(tText Ident, tPos Pos)>)
	MapLambda<tPos>(
		this ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tLambdaNode<tPos> aLambdaNode
	) {
		var TempLambdaDef = aModuleConstructor.NewDefConstructor();
		TempLambdaDef.InitMapLambda(aLambdaNode);
		TempLambdaDef.FinishMapProc(
			aLambdaNode.Pos,
			TempLambdaDef.UnsolvedSymbols
		);
		return (TempLambdaDef.Index, TempLambdaDef.UnsolvedSymbols);
	}
	
	public static (tInt32, mArrayList.tArrayList<(tText Ident, tPos Pos)>)
	MapMethod<tPos>(
		this ref tModuleConstructor<tPos> aModuleConstructor,
		mSPO_AST.tMethodNode<tPos> aMethodNode
	) {
		var TempMethodDef = aModuleConstructor.NewDefConstructor();
		TempMethodDef.InitMapMethod(aMethodNode);
		TempMethodDef.FinishMapProc(
			aMethodNode.Pos,
			TempMethodDef.UnsolvedSymbols
		);
		return (TempMethodDef.Index, TempMethodDef.UnsolvedSymbols);
	}
	
	public static tText
	InitProc<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tPos aPos,
		tText aDefName,
		mArrayList.tArrayList<(tText Ident, tPos Pos)> aEnv
	) {
		var ArgReg = mIL_AST.cEmpty;
		if (!aEnv.IsEmpty()) {
			var UnsolvedSymbols = aEnv.ToStream();
			while (UnsolvedSymbols.Match(out var Symbol, out UnsolvedSymbols)) {
				if (aDefConstructor.UnsolvedSymbols.ToStream().All(_ => _.Ident != Symbol.Ident)) {
					aDefConstructor.UnsolvedSymbols.Push(Symbol);
				}
			}
			
			if (aEnv.Size() == 1) {
				ArgReg = aEnv.Get(0).Ident;
			} else {
				UnsolvedSymbols = aEnv.ToStream().Reverse();
				while (UnsolvedSymbols.Match(out var Symbol_, out UnsolvedSymbols)) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.CreatePair(aPos, NewArgReg, Symbol_.Ident, ArgReg));
					ArgReg = NewArgReg;
				}
			}
		}
		
		var Proc = aDefConstructor.CreateTempReg();
		aDefConstructor.Commands.Push(mIL_AST.CallFunc(aPos, Proc, aDefName, ArgReg));
		aDefConstructor.UnsolvedSymbols.Push((aDefName, aPos));
		return Proc;
	}
	
	public static tText
	MapExpresion<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tExpressionNode<tPos> aExpressionNode
	) {
		switch (aExpressionNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cEmpty;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tFalseNode<tPos> FalseNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cFalse;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTrueNode<tPos> TrueNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cTrue;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyTypeNode<tPos> EmptyTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cEmptyType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tBoolTypeNode<tPos> BoolTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cBoolType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIntTypeNode<tPos> IntTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cIntType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTypeTypeNode<tPos> TypeTypeNode: {
			//--------------------------------------------------------------------------------
				return mIL_AST.cTypeType;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tNumberNode<tPos> NumberNode: {
			//--------------------------------------------------------------------------------
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CreateInt(
						NumberNode.Pos,
						ResultReg,
						"" + NumberNode.Value
					)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIdentNode<tPos> IdentNode: {
			//--------------------------------------------------------------------------------
				if (
					aDefConstructor.UnsolvedSymbols.ToStream().All(_ => _.Ident != IdentNode.Name) &&
					aDefConstructor.Commands.ToStream(
					).All(
						_ => !_.TryGetResultReg(out var Name) || Name != IdentNode.Name
					)
				) {
					aDefConstructor.UnsolvedSymbols.Push((IdentNode.Name, IdentNode.Pos));
				}
				return IdentNode.Name;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tCallNode<tPos> CallNode: {
			//--------------------------------------------------------------------------------
				var FuncReg = aDefConstructor.MapExpresion(CallNode.Func);
				var ArgReg = aDefConstructor.MapExpresion(CallNode.Arg);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.CallFunc(CallNode.Pos, ResultReg, FuncReg, ArgReg)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTupleNode<tPos> TupleNode: {
			//--------------------------------------------------------------------------------
				switch (TupleNode.Items.Take(2).ToArrayList().Size()) {
					case 0: {
						throw mStd.Error("impossible");
					}
					case 1: {
						mDebug.Assert(TupleNode.Items.Match(out var Head, out var _));
						return aDefConstructor.MapExpresion(Head);
					}
					default: {
						var List = TupleNode.Items.Reverse();
						var ResultReg = mIL_AST.cEmpty;
						while (List.Match(out var Item, out List)) {
							var ItemReg = aDefConstructor.MapExpresion(Item);
							var TupleReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CreatePair(
									Item.Pos,
									TupleReg,
									ItemReg,
									ResultReg
								)
							);
							ResultReg = TupleReg;
						}
						return ResultReg;
					}
				}
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPrefixNode<tPos> PrefixNode: {
			//--------------------------------------------------------------------------------
				var ExpresionReg = aDefConstructor.MapExpresion(PrefixNode.Element);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.AddPrefix(
						PrefixNode.Pos,
						ResultReg,
						PrefixNode.Prefix,
						ExpresionReg
					)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tRecordNode<tPos> RecordNode: {
			//--------------------------------------------------------------------------------
				var Elements = RecordNode.Elements;
				var ResultReg = mIL_AST.cEmpty;
				while (Elements.Match(out var Element, out Elements)) {
					var Expression = aDefConstructor.MapExpresion(Element.Value);
					var PrefixReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.AddPrefix(Element.Key.Pos, PrefixReg, Element.Key.Name, Expression));
					var NewResultReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.ExtendRec(Element.Key.Pos, NewResultReg, ResultReg, PrefixReg));
					ResultReg = NewResultReg;
				}
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTextNode<tPos> TextNode: {
			//--------------------------------------------------------------------------------
				var Pos = TextNode.Pos;
				var TextReg = mIL_AST.cEmpty;
				var Index = TextNode.Value.Length;
				while (Index --> 0) {
					var Char = TextNode.Value[Index];
					var CharOrdReg = aDefConstructor.CreateTempReg();
					var CharReg = aDefConstructor.CreateTempReg();
					var TextReg_ = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreateInt(Pos, CharOrdReg, ((int)Char).ToString()),
						mIL_AST.AddPrefix(Pos, CharReg, "Char", CharOrdReg),
						mIL_AST.CreatePair(Pos, TextReg_, CharReg, TextReg)
					);
					TextReg = TextReg_;
				}
				return TextReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tLambdaNode<tPos> LambdaNode: {
			//--------------------------------------------------------------------------------
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapLambda(
					LambdaNode
				);
				return aDefConstructor.InitProc(
					LambdaNode.Pos,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMethodNode<tPos> MethodNode: {
			//--------------------------------------------------------------------------------
				var(NewDefIndex, UnsolvedSymbols) = aDefConstructor.ModuleConstructor.MapMethod(
					MethodNode
				);
				return aDefConstructor.InitProc(
					MethodNode.Pos,
					TempDef(NewDefIndex),
					UnsolvedSymbols
				);
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tBlockNode<tPos> BlockNode: {
			//--------------------------------------------------------------------------------
				var CommandNodes = BlockNode.Commands;
				while (CommandNodes.Match(out var CommandNode, out CommandNodes)) {
					aDefConstructor.MapCommand(CommandNode);
				}
				// TODO: remove created symbols from unknown symbols
				return mIL_AST.cEmpty;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIfNode<tPos> IfNode: {
			//--------------------------------------------------------------------------------
				var Ifs = mArrayList.List<mSPO_AST.tCommandNode<tPos>>();
				var Pairs = IfNode.Cases;
				while (Pairs.Match(out var Pair, out Pairs)) {
					var (Test, Run) = Pair;
					Ifs.Push(mSPO_AST.ReturnIf(aDefConstructor.ModuleConstructor.MergePos(Test.Pos, Run.Pos), Run, Test));
				}
				Ifs.Push(
					mSPO_AST.ReturnIf(
						IfNode.Pos,
						mSPO_AST.Empty(IfNode.Pos),
						mSPO_AST.True(IfNode.Pos)
					)
				); // TODO: ASSERT FALSE
				
				var ResultReg = aDefConstructor.CreateTempReg();
				
				aDefConstructor.MapCommand(
					mSPO_AST.Def(
						IfNode.Pos,
						mSPO_AST.Match(
							IfNode.Pos,
							new mSPO_AST.tMatchFreeIdentNode<tPos>{Name = ResultReg},
							null
						),
						mSPO_AST.Call(
							IfNode.Pos,
							mSPO_AST.Lambda(
								IfNode.Pos,
								mSPO_AST.Match(
									IfNode.Pos,
									mSPO_AST.Empty(IfNode.Pos),
									null
								),
								mSPO_AST.Block(IfNode.Pos, Ifs.ToStream())
							),
							mSPO_AST.Empty(IfNode.Pos)
						)
					)
				);
				return ResultReg; 
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIfMatchNode<tPos> IfMatchNode: {
			//--------------------------------------------------------------------------------
				var Input = aDefConstructor.MapExpresion(IfMatchNode.Expression);
				
				var ModuleConstructor = aDefConstructor.ModuleConstructor;
				var Rest = IfMatchNode.Cases;
				
				var SwitchDef = ModuleConstructor.NewDefConstructor();
				
				while (Rest.Match(out var Case, out Rest)) {
					var (Match, Run) = Case;
					var CasePos = ModuleConstructor.MergePos(Match.Pos, Run.Pos);
					
					var TestDef = ModuleConstructor.NewDefConstructor();
					TestDef.MapMatchTest(mIL_AST.cArg, Match);
					TestDef.Commands.Push(mIL_AST.ReturnIf(CasePos, mIL_AST.cTrue, mIL_AST.cTrue));
					TestDef.FinishMapProc(CasePos, TestDef.UnsolvedSymbols);
					var TestProc = SwitchDef.InitProc(CasePos, TempDef(TestDef.Index), TestDef.UnsolvedSymbols);
					var TestResut = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CallFunc(CasePos, TestResut, TestProc, mIL_AST.cArg));
					
					var RunDef = ModuleConstructor.NewDefConstructor();
					RunDef.MapMatch(Match, mIL_AST.cArg);
					var Result = RunDef.MapExpresion(Run);
					RunDef.Commands.Push(mIL_AST.ReturnIf(CasePos, Result, mIL_AST.cTrue));
					RunDef.FinishMapProc(CasePos, RunDef.UnsolvedSymbols);
					var RunProc = SwitchDef.InitProc(CasePos, TempDef(RunDef.Index), RunDef.UnsolvedSymbols);
					var CallerArgPair = SwitchDef.CreateTempReg();
					SwitchDef.Commands.Push(mIL_AST.CreatePair(CasePos, CallerArgPair, RunProc, mIL_AST.cArg));
					
					SwitchDef.Commands.Push(mIL_AST.TailCallIf(CasePos, CallerArgPair, TestResut));
				}
				SwitchDef.Commands.Push(mIL_AST.ReturnIf(aExpressionNode.Pos, mIL_AST.cFalse, mIL_AST.cTrue));
				SwitchDef.FinishMapProc(aExpressionNode.Pos, SwitchDef.UnsolvedSymbols);
				var SwitchProc = aDefConstructor.InitProc(aExpressionNode.Pos, TempDef(SwitchDef.Index), SwitchDef.UnsolvedSymbols);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.CallFunc(IfMatchNode.Pos, ResultReg, SwitchProc, Input));
				
				return ResultReg; 
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tVarToValNode<tPos> VarToValNode: {
			//--------------------------------------------------------------------------------
				var ObjReg = aDefConstructor.MapExpresion(VarToValNode.Obj);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(mIL_AST.VarGet(VarToValNode.Pos, ResultReg, ObjReg));
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveTypeNode: {
			//--------------------------------------------------------------------------------
				mStd.AssertNot(aDefConstructor.UnsolvedSymbols.ToStream().Any(a => a.Ident == RecursiveTypeNode.HeadType.Name));
				mStd.AssertNot(aDefConstructor.KnownSymbols.ToStream().Any(a => a == RecursiveTypeNode.HeadType.Name));
				aDefConstructor.Commands.Push(
					mIL_AST.TypeFree(RecursiveTypeNode.HeadType.Pos, RecursiveTypeNode.HeadType.Name)
				);
				var BodyTypeReg = aDefConstructor.MapExpresion(RecursiveTypeNode.BodyType);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypeRecursive(
						RecursiveTypeNode.Pos,
						ResultReg,
						RecursiveTypeNode.HeadType.Name,
						BodyTypeReg
					)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tSetTypeNode<tPos> SetTypeNode: {
			//--------------------------------------------------------------------------------
				SetTypeNode.Expressions.Match(out var First, out var Rest);
				var ResultReg = aDefConstructor.MapExpresion(First);
				while (Rest.Match(out var Head, out Rest)) {
					var ExprReg = aDefConstructor.MapExpresion(Head);
					var SetTypeReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.TypeSet(Head.Pos, SetTypeReg, ExprReg, ResultReg));
					ResultReg = SetTypeReg;
				}
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tTupleTypeNode<tPos> TupleTypeNode: {
			//--------------------------------------------------------------------------------
				if (!TupleTypeNode.Expressions.Match(out var First, out var Rest)) {
					return mIL_AST.cEmptyType;
				} else {
					var ResultReg = aDefConstructor.MapExpresion(First);
					while (Rest.Match(out var Head, out Rest)) {
						var PairTypeReg = aDefConstructor.CreateTempReg();
						var ExprReg = aDefConstructor.MapExpresion(Head);
						aDefConstructor.Commands.Push(mIL_AST.TypePair(Head.Pos, PairTypeReg, ExprReg, ResultReg));
						ResultReg = PairTypeReg;
					}
					return ResultReg;
				}
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixTypeNode: {
			//--------------------------------------------------------------------------------
				var InnerType = aDefConstructor.MapExpresion(
					mSPO_AST.TupleType(PrefixTypeNode.Pos, PrefixTypeNode.Expressions)
				);
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.TypePrefix(PrefixTypeNode.Pos, ResultReg, PrefixTypeNode.Prefix.Name, InnerType)
				);
				return ResultReg;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPipeToRightNode<tPos> PipeToRightNode: {
			//--------------------------------------------------------------------------------
					switch (PipeToRightNode.Right) {
						case mSPO_AST.tPipeToRightNode<tPos> PipeToRightNode_: {
							return aDefConstructor.MapExpresion(
								mSPO_AST.PipeToRight(
									PipeToRightNode.Pos,
									mSPO_AST.PipeToRight(
										PipeToRightNode.Left.Pos, //TODO: mStd.Merge(PipeToRightNode.Left.Pos, PipeToRightNode_.Left.Pos),
										PipeToRightNode.Left,
										PipeToRightNode_.Left
									),
									PipeToRightNode_.Right
								)
							);
						}
						case mSPO_AST.tCallNode<tPos> CallNode: {
							mSPO_AST.tExpressionNode<tPos> Func;
							if (CallNode.Func is mSPO_AST.tIdentNode<tPos> IdentNode) {
								Func = mSPO_AST.Ident(IdentNode.Pos, "..." + IdentNode.Name.Substring(1));
							} else {
								Func = CallNode.Func;
							}
							var FuncReg = aDefConstructor.MapExpresion(Func);
							mSPO_AST.tExpressionNode<tPos> Arg;
							if (CallNode.Arg is mSPO_AST.tTupleNode<tPos> Tuple) {
								Arg = mSPO_AST.Tuple(
									Tuple.Pos,
									mStream.Stream(PipeToRightNode.Right, Tuple.Items)
								);
							} else {
								Arg = mSPO_AST.Tuple(
									CallNode.Arg.Pos,
									mStream.Stream(PipeToRightNode.Right, mStream.Stream(CallNode.Arg))
								);
							}
							var ArgReg = aDefConstructor.MapExpresion(Arg);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(CallNode.Pos, ResultReg, FuncReg, ArgReg)
							);
							return ResultReg;
						}
						default: {
							var FirstArgReg = aDefConstructor.MapExpresion(PipeToRightNode.Left);
							var FuncReg = aDefConstructor.MapExpresion(PipeToRightNode.Right);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(PipeToRightNode.Pos, ResultReg, FuncReg, FirstArgReg)
							);
							return ResultReg;
						}
					}
				}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tPipeToLeftNode<tPos> PipeToLeftNode: {
			//--------------------------------------------------------------------------------
					switch (PipeToLeftNode.Left) {
						case mSPO_AST.tCallNode<tPos> CallNode: {
							mSPO_AST.tExpressionNode<tPos> Func;
							if (CallNode.Func is mSPO_AST.tIdentNode<tPos> IdentNode) {
								Func = mSPO_AST.Ident(IdentNode.Pos, IdentNode.Name.Substring(1) + "...");
							} else {
								Func = CallNode.Func;
							}
							var FuncReg = aDefConstructor.MapExpresion(Func);
							mSPO_AST.tExpressionNode<tPos> Arg;
							if (CallNode.Arg is mSPO_AST.tTupleNode<tPos> Tuple) {
								Arg = mSPO_AST.Tuple(
									Tuple.Pos,
									mStream.Concat(Tuple.Items, mStream.Stream(PipeToLeftNode.Right))
								);
							} else {
								Arg = mSPO_AST.Tuple(
									CallNode.Arg.Pos,
									mStream.Stream(CallNode.Arg, mStream.Stream(PipeToLeftNode.Right))
								);
							}
							var ArgReg = aDefConstructor.MapExpresion(Arg);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(CallNode.Pos, ResultReg, FuncReg, ArgReg)
							);
							return ResultReg;
						}
						default: {
							var FirstArgReg = aDefConstructor.MapExpresion(PipeToLeftNode.Right);
							var FuncReg = aDefConstructor.MapExpresion(PipeToLeftNode.Left);
							var ResultReg = aDefConstructor.CreateTempReg();
							aDefConstructor.Commands.Push(
								mIL_AST.CallFunc(PipeToLeftNode.Pos, ResultReg, FuncReg, FirstArgReg)
							);
							return ResultReg;
						}
					}
				}
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mStd.Error(
					$"not implemented: case {nameof(mSPO_AST)}.{aExpressionNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapExpresion)}(...)"
				);
			}
		}
	}
	
	public static void
	MapMatch<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMatchNode<tPos> aMatchNode,
		tText aReg
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tEmptyNode<tPos> EmptyNode: {
			//--------------------------------------------------------------------------------
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchFreeIdentNode<tPos> FreeIdentNode: {
			//--------------------------------------------------------------------------------
				mDebug.AssertNotEq(FreeIdentNode.Name, "_");
				aDefConstructor.Commands.Push(mIL_AST.Alias(FreeIdentNode.Pos, FreeIdentNode.Name, aReg));
				aDefConstructor.KnownSymbols.Push(FreeIdentNode.Name);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
			//--------------------------------------------------------------------------------
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchPrefixNode<tPos> PrefixNode: {
			//--------------------------------------------------------------------------------
				var ResultReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.SubPrefix(
						PrefixNode.Pos,
						ResultReg,
						PrefixNode.Prefix,
						aReg
					)
				);
				aDefConstructor.MapMatch(
					PrefixNode.Match,
					ResultReg
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchRecordNode<tPos> RecordNode: {
			//--------------------------------------------------------------------------------
				var Elements = RecordNode.Elements;
				while (Elements.Match(out var Element, out Elements)) {
					var Pos = Element.Key.Pos;
					var TempFuncDef = aDefConstructor.ModuleConstructor.NewDefConstructor();
					var HeadTailReg = TempFuncDef.CreateTempReg();
					TempFuncDef.Commands.Push(mIL_AST.DivideRec(Pos, HeadTailReg, mIL_AST.cArg));
					var LastReg = TempFuncDef.CreateTempReg();
					TempFuncDef.Commands.Push(mIL_AST.GetSecond(Pos, LastReg, HeadTailReg));
					var HasFoundReg = TempFuncDef.CreateTempReg();
					TempFuncDef.Commands.Push(mIL_AST.HasPrefix(Pos, HasFoundReg, Element.Key.Name, LastReg));
					TempFuncDef.Commands.Push(mIL_AST.ReturnIf(Pos, LastReg, HasFoundReg));
					var RestReg = TempFuncDef.CreateTempReg();
					TempFuncDef.Commands.Push(mIL_AST.GetFirst(Pos, RestReg, HeadTailReg));
					TempFuncDef.Commands.Push(mIL_AST.RepeatIf(Pos, RestReg, mIL_AST.cTrue));

					var TempFuncReg = aDefConstructor.CreateTempReg();
					aDefConstructor.UnsolvedSymbols.Push((TempDef(TempFuncDef.Index), Element.Key.Pos));
					aDefConstructor.Commands.Push(mIL_AST.CallFunc(Pos, TempFuncReg, TempDef(TempFuncDef.Index), mIL_AST.cEmpty));
					var TempValueReg_ = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.CallFunc(Pos, TempValueReg_, TempFuncReg, aReg));
					var TempValueReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.SubPrefix(Pos, TempValueReg, Element.Key.Name, TempValueReg_));
					aDefConstructor.MapMatch(Element.Match, TempValueReg);
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchTupleNode<tPos> TupleNode: {
			//--------------------------------------------------------------------------------
				var RestReg = aReg;
				var Items = TupleNode.Items;
				mDebug.AssertEq(Items.Take(2).ToArrayList().Size(), 2);
				while (Items.Match(out var Item, out Items)) {
					var ItemReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetFirst(Item.Pos, ItemReg, RestReg));
					
					aDefConstructor.MapMatch(Item, ItemReg);
					
					var NewRestReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(mIL_AST.GetSecond(Item.Pos, NewRestReg, RestReg));
					RestReg = NewRestReg;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchGuardNode<tPos> GuardNode: {
			//--------------------------------------------------------------------------------
				// TODO: ASSERT GuardNode._Guard
				aDefConstructor.MapMatch(GuardNode.Match, aReg);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
			//--------------------------------------------------------------------------------
				if (TypeNode is null) {
					aDefConstructor.MapMatch(MatchNode, aReg);
				} else if (MatchNode.Type is null) {
					aDefConstructor.MapMatch(
						mSPO_AST.Match(MatchNode.Pos, MatchNode.Pattern, TypeNode),
						aReg
					);
				} else {
					throw mStd.Error("not implemented"); //TODO: Unify MatchNode.Type & TypeNode
				}
				break;
			}
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mStd.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatch)}(...)"
				);
			}
		}
	}
	
	public static void
	MapMatchTest<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		tText aInReg,
		mSPO_AST.tMatchNode<tPos> aMatchNode
	) {
		var PatternNode = aMatchNode.Pattern;
		var TypeNode = aMatchNode.Type;
		
		switch (PatternNode) {
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchFreeIdentNode<tPos> FreeIdentNode: {
			//--------------------------------------------------------------------------------
				mDebug.AssertNotEq(FreeIdentNode.Name, "_");
				mDebug.Assert(
					aDefConstructor.KnownSymbols.ToStream().All(_ => _ != FreeIdentNode.Name)
				);
				aDefConstructor.Commands.Push(
					mIL_AST.Alias(FreeIdentNode.Pos, FreeIdentNode.Name, aInReg)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatchNode: {
			//--------------------------------------------------------------------------------
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchPrefixNode<tPos> PrefixNode: {
			//--------------------------------------------------------------------------------
				var Prefix = PrefixNode.Prefix;
				var SubMatch = PrefixNode.Match;
				var IsPrefix = aDefConstructor.CreateTempReg();
				var IsNotPrefix = aDefConstructor.CreateTempReg();
				var Reg = aDefConstructor.CreateTempReg();
				var InvReg = aDefConstructor.CreateTempReg();
				var SubValue = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.IsPrefix(PrefixNode.Pos, IsPrefix, aInReg),
					mIL_AST.XOr(PrefixNode.Pos, IsNotPrefix, IsPrefix, mIL_AST.cTrue),
					mIL_AST.ReturnIf(PrefixNode.Pos, mIL_AST.cFalse, IsNotPrefix),
					mIL_AST.HasPrefix(PrefixNode.Pos, Reg, Prefix, aInReg),
					mIL_AST.XOr(PrefixNode.Pos, InvReg, Reg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(PrefixNode.Pos, mIL_AST.cFalse, InvReg),
					mIL_AST.SubPrefix(PrefixNode.Pos, SubValue, Prefix, aInReg)
				);
				aDefConstructor.MapMatchTest(SubValue, SubMatch);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchGuardNode<tPos> GuardNode: {
			//--------------------------------------------------------------------------------
				var SubMatch = GuardNode.Match;
				var Guard = GuardNode.Guard;
				
				aDefConstructor.MapMatchTest(aInReg, SubMatch);
				
				var InvReg = aDefConstructor.CreateTempReg();
				
				var TestReg = aDefConstructor.MapExpresion(Guard);
				aDefConstructor.Commands.Push(
					// TODO: check type
					mIL_AST.XOr(GuardNode.Pos, InvReg, TestReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(GuardNode.Pos, mIL_AST.cFalse, InvReg)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchTupleNode<tPos> TupleNode: {
			//--------------------------------------------------------------------------------
				var Items = TupleNode.Items;
				var RestReg = aInReg;
				while (Items.Match(out var Item, out Items)) {
					var IsPairReg = aDefConstructor.CreateTempReg();
					var IsNotPairReg = aDefConstructor.CreateTempReg();
					var TempReg = aDefConstructor.CreateTempReg();
					var RestReg_ = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.IsPair(TupleNode.Pos, IsPairReg, aInReg),
						mIL_AST.XOr(TupleNode.Pos, IsNotPairReg, IsPairReg, mIL_AST.cTrue),
						mIL_AST.ReturnIf(TupleNode.Pos, mIL_AST.cFalse, IsNotPairReg),
						mIL_AST.GetFirst(Item.Pos, TempReg, RestReg),
						mIL_AST.GetSecond(Item.Pos, RestReg_, RestReg)
					);
					aDefConstructor.MapMatchTest(TempReg, Item);
					RestReg = RestReg_;
				}
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tNumberNode<tPos> NumberNode: {
			//--------------------------------------------------------------------------------
				var IsIntReg = aDefConstructor.CreateTempReg();
				var IsNotIntReg = aDefConstructor.CreateTempReg();
				var IntReg = aDefConstructor.CreateTempReg();
				var CondReg = aDefConstructor.CreateTempReg();
				var InvCondReg = aDefConstructor.CreateTempReg();
				aDefConstructor.Commands.Push(
					mIL_AST.IsPair(NumberNode.Pos, IsIntReg, aInReg),
					mIL_AST.XOr(NumberNode.Pos, IsNotIntReg, IsIntReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(NumberNode.Pos, mIL_AST.cFalse, IsNotIntReg),
					mIL_AST.CreateInt(NumberNode.Pos, IntReg, $"{NumberNode.Value}"),
					mIL_AST.IntsAreEq(NumberNode.Pos, CondReg, aInReg, IntReg),
					mIL_AST.XOr(NumberNode.Pos, InvCondReg, CondReg, mIL_AST.cTrue),
					mIL_AST.ReturnIf(NumberNode.Pos, mIL_AST.cFalse, InvCondReg)
				);
				break;
			}
			//--------------------------------------------------------------------------------
			case mSPO_AST.tMatchNode<tPos> MatchNode: {
			//--------------------------------------------------------------------------------
				aDefConstructor.MapMatch(MatchNode, aInReg);
				break;
			}
			//--------------------------------------------------------------------------------
			default: {
			//--------------------------------------------------------------------------------
				throw mStd.Error(
					$"not implemented: {nameof(mSPO_AST)}.{PatternNode.GetType().Name} " +
					$"in {nameof(mSPO2IL)}.{nameof(MapMatchTest)}(...)"
				);
			}
		}
	}
	
	public static void
	MapDef<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefNode<tPos> aDefNode
	) {
		var ValueReg = aDefConstructor.MapExpresion(aDefNode.Src);
		aDefConstructor.MapMatch(aDefNode.Des, ValueReg);
	}
	
	public static void
	MapReturnIf<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tReturnIfNode<tPos> aReturnNode
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.ReturnIf(
				aReturnNode.Pos,
				aDefConstructor.MapExpresion(aReturnNode.Result),
				aDefConstructor.MapExpresion(aReturnNode.Condition)
			)
		);
	}
	
	public static void
	MapRecursiveLambdas<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tRecLambdasNode<tPos> aRecLambdasNode
	) {
		var NewDefIndices = mArrayList.List<tInt32>();
		var SPODefNodes = mArrayList.List<mSPO_AST.tRecLambdaItemNode<tPos>>();
		var TempLambdaDefs = mArrayList.List<tDefConstructor<tPos>>();
		var AllUnsolvedSymbols = mArrayList.List<(tText Ident, tPos Pos)>();
		
		var List = aRecLambdasNode.List;
		while (List.Match(out var RecLambdaItemNode, out List)) {
			var NewDefIndex = aDefConstructor.ModuleConstructor.Defs.Size();
			NewDefIndices.Push(NewDefIndex);
			AllUnsolvedSymbols.Push((TempDef(NewDefIndex), RecLambdaItemNode.Pos));
			TempLambdaDefs.Push(aDefConstructor.ModuleConstructor.NewDefConstructor());
			SPODefNodes.Push(RecLambdaItemNode);
		}
		
		var Max = NewDefIndices.Size();
		
		// create all rec. func. in each rec. func.
		var FuncNames = aRecLambdasNode.List.Map(a => a.Ident.Name).ToArrayList();
		foreach (var TempLambdaDef in TempLambdaDefs.ToArray()) {
			for (var J = 0; J < Max; J += 1) {
				var Definition = AllUnsolvedSymbols.Get(J);
				TempLambdaDef.Commands.Push(
					mIL_AST.CallFunc(Definition.Pos, FuncNames.Get(J), Definition.Ident, mIL_AST.cEnv)
				);
			}
		}
		
		// InitMapLambda(...) for all rec. func.
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempLambdaDef = TempLambdaDefs.Get(I);
			
			TempLambdaDef.InitMapLambda(RecLambdaItemNode.Lambda);
			var KnownSymbols = TempLambdaDef.KnownSymbols.ToStream();
			var TempUnsolvedSymbols = TempLambdaDef.UnsolvedSymbols.ToStream(
			).Where(
				aUnsolved => (
					KnownSymbols.All(_ => _ != aUnsolved.Ident) &&
					aRecLambdasNode.List.All(_ => _.Ident.Name != aUnsolved.Ident)
				)
			);
			
			while (TempUnsolvedSymbols.Match(out var Symbol, out TempUnsolvedSymbols)) {
				AllUnsolvedSymbols.Push(Symbol);
			}
		}
		
		// FinishMapProc(...) for all rec. func.
		for (var I = 0; I < Max; I += 1) {
			var RecLambdaItemNode = SPODefNodes.Get(I);
			var TempDefConstructor = TempLambdaDefs.Get(I);
			
			TempDefConstructor.FinishMapProc(
				RecLambdaItemNode.Pos,
				AllUnsolvedSymbols
			);
			
			var ArgReg = mIL_AST.cEmpty;
			if (!AllUnsolvedSymbols.IsEmpty()) {
				var Iterator = AllUnsolvedSymbols.ToStream();
				while (Iterator.Match(out var UnsolvedSymbol, out Iterator)) {
					if (aDefConstructor.UnsolvedSymbols.ToStream().All(_ => _.Ident != UnsolvedSymbol.Ident)) {
						aDefConstructor.UnsolvedSymbols.Push(UnsolvedSymbol);
					}
				}
				
				Iterator = AllUnsolvedSymbols.ToStream().Reverse();
				while (Iterator.Match(out var Symbol_, out Iterator)) {
					var NewArgReg = aDefConstructor.CreateTempReg();
					aDefConstructor.Commands.Push(
						mIL_AST.CreatePair(RecLambdaItemNode.Pos, NewArgReg, Symbol_.Ident, ArgReg)
					);
					ArgReg = NewArgReg;
				}
			}
			
			aDefConstructor.Commands.Push(
				mIL_AST.CallFunc(RecLambdaItemNode.Pos, RecLambdaItemNode.Ident.Name, TempDef(TempDefConstructor.Index), ArgReg)
			);
			
			aDefConstructor.KnownSymbols.Push(RecLambdaItemNode.Ident.Name);
		}
	}
	
	public static void
	MapVar<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tDefVarNode<tPos> aVarNode
	) {
		aDefConstructor.Commands.Push(
			mIL_AST.VarDef(
				aVarNode.Pos,
				aVarNode.Ident.Name,
				aDefConstructor.MapExpresion(aVarNode.Expression)
			)
		);
	}
	
	public static void
	MapMethodCalls<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tMethodCallsNode<tPos> aMethodCallsNode
	) {
		var Rest = aMethodCallsNode.MethodCalls;
		var Object = aDefConstructor.MapExpresion(aMethodCallsNode.Object);
		while (Rest.Match(out var Call, out Rest)) {
			var Arg = aDefConstructor.MapExpresion(Call.Argument);
			var MethodName = Call.Method.Name;
			if (MethodName == "_=...") {
				aDefConstructor.Commands.Push(mIL_AST.VarSet(aMethodCallsNode.Pos, Object, Arg));
				continue;
			}
			var Result = (Call.Result is null) ? mIL_AST.cEmpty : aDefConstructor.CreateTempReg();
			var MethodReg = aDefConstructor.CreateTempReg();
			aDefConstructor.Commands.Push(
				mIL_AST.CreatePair(aMethodCallsNode.Object.Pos, MethodReg, Object, MethodName),
				mIL_AST.CallProc(aMethodCallsNode.Pos, Result, MethodReg, Arg)
			);
			if (Call.Result != null) {
				aDefConstructor.MapMatch(Call.Result.Value, Result);
			}
			
			var KnownSymbols = aDefConstructor.KnownSymbols.ToStream();
			if (KnownSymbols.All(_ => _ != MethodName)) {
				aDefConstructor.UnsolvedSymbols.Push((MethodName, Call.Method.Pos));
			}
		}
	}
	
	public static void
	MapCommand<tPos>(
		this ref tDefConstructor<tPos> aDefConstructor,
		mSPO_AST.tCommandNode<tPos> aCommandNode
	) {
		switch (aCommandNode) {
			case mSPO_AST.tDefNode<tPos> DefNode: {
				aDefConstructor.MapDef(DefNode);
				break;
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdasNode: {
				aDefConstructor.MapRecursiveLambdas(RecLambdasNode);
				break;
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnNode: {
				aDefConstructor.MapReturnIf(ReturnNode);
				break;
			}
			case mSPO_AST.tDefVarNode<tPos> VarNode: {
				aDefConstructor.MapVar(VarNode);
				break;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCallsNode: {
				aDefConstructor.MapMethodCalls(MethodCallsNode);
				break;
			}
			default: {
				throw mStd.Error("impossible");
			}
		}
	}
	
	public static tModuleConstructor<tPos>
	MapModule<tPos>(
		mSPO_AST.tModuleNode<tPos> aModuleNode,
		mStd.tFunc<tPos, tPos, tPos> aMergePos
	) {
		using (mPerf.Measure()) {
			var ModuleConstructor = NewModuleConstructor(aMergePos);
			var TempLambdaDef = ModuleConstructor.NewDefConstructor();
			TempLambdaDef.InitMapLambda(
				mSPO_AST.Lambda(
					aModuleNode.Pos,
					aModuleNode.Import.Match,
					mSPO_AST.Block(
						aMergePos(
							aModuleNode.Commands.IsEmpty() ? default : aModuleNode.Commands.First().Pos,
							aModuleNode.Export.Pos
						),
						mStream.Concat(
							aModuleNode.Commands,
							mStream.Stream<mSPO_AST.tCommandNode<tPos>>(
								mSPO_AST.ReturnIf(
									aModuleNode.Export.Pos,
									aModuleNode.Export.Expression,
									mSPO_AST.True(aModuleNode.Export.Pos)
								)
							)
						)
					)
				)
			);
			if (TempLambdaDef.UnsolvedSymbols.Size() != ModuleConstructor.Defs.Size() - 1) {
				var First = TempLambdaDef.UnsolvedSymbols.ToStream().Where(_ => !_.Ident.StartsWith("d_")).First();
				throw mStd.Error($"Unknown symbol '{First.Ident}' @ {First.Pos}", First.Pos);
			}
			
			var DefSymbols = mArrayList.List<(tText Ident, tPos Pos)>();
			for (var I = 1; I < ModuleConstructor.Defs.Size(); I += 1) {
				DefSymbols.Push((TempDef(I), default));
			}
			TempLambdaDef.FinishMapProc(aModuleNode.Pos, DefSymbols);
			
			return ModuleConstructor;
		}
	}
}