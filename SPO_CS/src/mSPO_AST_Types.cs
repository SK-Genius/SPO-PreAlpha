//IMPORT mArrayList.cs
//IMPORT mVM_Type.cs
//IMPORT mSPO_AST.cs
//IMPORT mResult.cs

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
using System.Text.RegularExpressions;

public static class
mSPO_AST_Types {
	
	#if true
	
	public enum tTypeRelation {
		Sub,
		Equal,
		Super,
	}
	
	public static mVM_Type.tType
	UpdateTypes<tPos>(
		mSPO_AST.tExpressionNode<tPos> aNode,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope
	) {
		mVM_Type.tType Result;
		switch (aNode) {
			case mSPO_AST.tEmptyNode<tPos> _: {
				Result = mVM_Type.Empty();
				break;
			}
			case mSPO_AST.tTrueNode<tPos> _: {
				Result = mVM_Type.Bool();
				break;
			}
			case mSPO_AST.tFalseNode<tPos> _: {
				Result = mVM_Type.Bool();
				break;
			}
			case mSPO_AST.tIntNode<tPos> _: {
				Result = mVM_Type.Int();
				break;
			}
			case mSPO_AST.tIdentNode<tPos> Ident: {
				Result = aScope.Where(
					_ => _.Ident == Ident.Name
				).ForceFirst(
				).Type;
				break;
			}
			case mSPO_AST.tTypeNode<tPos> Type: {
				Result = mVM_Type.Type(ResolveTypeExpression(Type, aScope).ElseThrow(_ => ""));
				break;
			}
			case mSPO_AST.tTupleNode<tPos> Tuple: {
				Result = mVM_Type.Tuple(
					Tuple.Items.Map(
						_ => UpdateTypes(_, aScope)
					)
				);
				break;
			}
			case mSPO_AST.tPrefixNode<tPos> Prefix: {
				Result = mVM_Type.Prefix(
					Prefix.Prefix,
					UpdateTypes(Prefix.Element, aScope)
				);
				break;
			}
			case mSPO_AST.tRecordNode<tPos> Record: {
				Result = Record.Elements.Reduce(
					mVM_Type.Empty(),
					(aTail, aHead) => mVM_Type.Record(
						mVM_Type.Prefix(
							aHead.Key.Name,
							UpdateTypes(aHead.Value, aScope)
						),
						aTail
					)
				);
				break;
			}
			case mSPO_AST.tLambdaNode<tPos> Lambda: {
				var NewScope = aScope;
				if (Lambda.Generic != null) {
					UpdateTypes(Lambda.Generic, null, tTypeRelation.Sub, aScope, out NewScope);
				}
				Result = mVM_Type.Proc(
					mVM_Type.Empty(),
					UpdateTypes(Lambda.Head, null, tTypeRelation.Sub, NewScope, out NewScope),
					UpdateTypes(Lambda.Body, NewScope)
				);
				break;
			}
			case mSPO_AST.tMethodNode<tPos> Method: {
				Result = mVM_Type.Proc(
					UpdateTypes(Method.Obj, null, tTypeRelation.Equal, aScope, out var NewScope),
					UpdateTypes(Method.Arg, null, tTypeRelation.Sub, NewScope, out var NewScope2),
					UpdateTypes(Method.Body, NewScope2)
				);
				break;
			}
			case mSPO_AST.tBlockNode<tPos> Block: {
				var Commands = Block.Commands;
				var Types = mStream.Stream<mVM_Type.tType>();
				var BlockScope = aScope;
				Result = mVM_Type.Empty();
				while (Commands.Match(out var Command, out Commands)) {
					UpdateTypes(Command, BlockScope, out BlockScope);
					if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
						var Type = ReturnIf.Result.TypeAnnotation;
						if (Types.All(_ => !_.Equals(Type))) {
							Result = Types.IsEmpty() 
								? Type
								: mVM_Type.Set(Type, Result);
							Types = mStream.Stream(Type, Types);
						}
					}
				}
				break;
			}
			case mSPO_AST.tCallNode<tPos> Call: {
				var FuncType = UpdateTypes(Call.Func, aScope);
				mAssert.IsTrue(FuncType.MatchProc(out var ObjType, out var ArgType, out var ResType));
				mAssert.IsTrue(ObjType.MatchEmpty());
				//TODO: mAssert.True(ArgType.IsSubtype(Call.Arg.TypeAnnotation));
				Result = ResType;
				break;
			}
			case mSPO_AST.tIfMatchNode<tPos> IfMatch: {
				var MatchType = UpdateTypes(IfMatch.Expression, aScope);
				
				var Tail = IfMatch.Cases;
				var Results = mStream.Stream<mVM_Type.tType>();
				Result = default;
				while (Tail.Match(out var Head, out Tail)) {
					UpdateTypes(Head.Match, MatchType, tTypeRelation.Super, aScope, out var TempScope);
					var Temp = UpdateTypes(Head.Expression, TempScope);
					if (Results.IsEmpty()) {
						Result = Temp;
						Results = mStream.Stream(Temp);
					} else if (Results.All(_ => _ != Temp)) {
						Results = mStream.Stream(Temp, Results);
						Result = mVM_Type.Set(Temp, Result);
					}
				}
				break;
			}
			case mSPO_AST.tVarToValNode<tPos> VarToVal: {
				VarToVal.Obj.TypeAnnotation.MatchVar(out var ValType);
				Result = ValType;
				break;
			}
			case mSPO_AST.tIfNode<tPos> If: {
				var Tail = If.Cases;
				Result = null;
				while (Tail.Match(out var Head, out Tail)) {
					mAssert.AreEquals(UpdateTypes(Head.Cond, aScope), mVM_Type.Bool());
					var SubResult = UpdateTypes(Head.Result, aScope);
					if (Result is null) {
						Result = SubResult;
					} else {
						Result = mVM_Type.Set(Result, SubResult);
					}
				}
				break;
			}
			default: {
				throw mError.Error($"not implemented: " + aNode.GetType().Name);
			}
		}
		aNode.TypeAnnotation = Result;
		return Result;
	}
	
	public static mVM_Type.tType
	UpdateTypes<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mVM_Type.tType aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope,
		out mStream.tStream<(tText Ident, mVM_Type.tType Type)> aNewScope
	) {
		mVM_Type.tType Result;
		switch (aMatch) {
			case mSPO_AST.tMatchNode<tPos> Match: {
				if (Match.Type != null) {
					var Type = ResolveTypeExpression(Match.Type, aScope).ElseThrow(_ => _);
					Result = UpdateTypes(Match.Pattern, Type, aTypeRelation, aScope, out aNewScope);
				} else {
					Result = UpdateTypes(Match.Pattern, aType, aTypeRelation, aScope, out aNewScope);
				}
				break;
			}
			case mSPO_AST.tMatchFreeIdentNode<tPos> MatchFreeIdent: {
				mAssert.IsNotNull(aType);
				Result = aType;
				aNewScope = mStream.Stream((MatchFreeIdent.Name, aType), aScope);
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatch: {
				aNewScope = aScope;
				Result = aType;
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				var SubType = (mVM_Type.tType)null;
				if (aType != null) {
					var Prefix = (tText)null;
					while (aType.MatchSet(out var Type, out var Types)) {
						if (Type.MatchPrefix(out Prefix, out SubType) && Prefix == MatchPrefix.Prefix) {
							aType = Type;
							break;
						}
						aType = Types;
					}
					mAssert.IsTrue(aType.MatchPrefix(out Prefix, out SubType));
					mAssert.AreEquals(Prefix, MatchPrefix.Prefix);
				}
				Result = mVM_Type.Prefix(
					MatchPrefix.Prefix,
					UpdateTypes(MatchPrefix.Match, SubType, aTypeRelation, aScope, out aNewScope)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> MatchTuple: {
				var Tail = MatchTuple.Items;
				var Types = mStream.Stream<mVM_Type.tType>();
				aNewScope = aScope;
				if (aType is null) {
					while (Tail.Match(out var Head, out Tail)) {
						Types = mStream.Stream(
							UpdateTypes(Head, null, aTypeRelation, aNewScope, out aNewScope),
							Types
						);
					}
				} else {
					while (Tail.Match(out var Head, out Tail)) {
						mAssert.IsTrue(aType.MatchPair(out var Type, out aType));
						Types = mStream.Stream(
							UpdateTypes(Head, Type, aTypeRelation, aNewScope, out aNewScope),
							Types
						);
					}
				}
				Result = mVM_Type.Tuple(Types.Reverse());
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> MatchRecord: {
				var Tail = MatchRecord.Elements;
				Result = mVM_Type.Empty();
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					var Type = (mVM_Type.tType)null;
					if (aType != null) { 
						var RecordType = aType;
						while (RecordType.MatchRecord(out var Key, out Type, out RecordType)) {
							if (Key == Head.Key.Name) {
								break;
							}
						}
						mAssert.IsNotNull(Type);
					}
					
					Result = mVM_Type.Record(
						mVM_Type.Prefix(
							Head.Key.Name,
							UpdateTypes(Head.Match, Type, aTypeRelation, aNewScope, out aNewScope)
						),
						Result
					);
				}
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> MatchGuard: {
				mAssert.AreEquals(
					UpdateTypes(MatchGuard.Guard, aScope),
					mVM_Type.Bool()
				);
				aNewScope = aScope;
				Result = UpdateTypes(MatchGuard.Match, aType, tTypeRelation.Super, aScope, out aNewScope);
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
					Result = UpdateTypes(Expression, aScope);
					aNewScope = aScope;
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aMatch.GetType().Name);
			}
		}
		aMatch.TypeAnnotation = Result;
		return Result;
	}
	
	public static void
	UpdateTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope,
		out mStream.tStream<(tText Ident, mVM_Type.tType Type)> aNewScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				if (Def.Des.Type is null) {
					UpdateTypes(
						Def.Des,
						UpdateTypes(Def.Src, aScope),
						tTypeRelation.Equal,
						aScope,
						out aNewScope
					);
				} else {
					UpdateTypes(
						Def.Des,
						ResolveTypeExpression(Def.Des.Type, aScope).ElseThrow(_ => _),
						tTypeRelation.Super,
						aScope,
						out aNewScope
					);
				}
				break;
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				mAssert.AreEquals(UpdateTypes(ReturnIf.Condition, aScope), mVM_Type.Bool());
				UpdateTypes(ReturnIf.Result, aScope);
				aNewScope = aScope;
				break;
			}
			case mSPO_AST.tMethodCallNode<tPos> MethodCall: {
				var ArgType = UpdateTypes(MethodCall.Argument, aScope);
				var MethodType = UpdateTypes(MethodCall.Method, aScope);
				mAssert.IsTrue(MethodType.MatchProc(out var MethObjType, out var MethArgType, out var MethResType));
				mAssert.AreEquals(ArgType, MethArgType);
				UpdateTypes(MethodCall.Result, MethResType, tTypeRelation.Sub, aScope, out aNewScope);
				break;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				var ObjType = UpdateTypes(MethodCalls.Object, aScope);
				var Tail = MethodCalls.MethodCalls;
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					var ArgType = UpdateTypes(Head.Argument, aNewScope);
					if (Head.Method.Name == "_=...") {
						ObjType = mVM_Type.Var(ArgType); // TODO
					} else {
						var MethodType = UpdateTypes(Head.Method, aNewScope);
						mAssert.IsTrue(MethodType.MatchProc(out var ObjType_, out var ArgType_, out var ResType_));
						//mAssert.AreEquals(ObjType, ObjType_);
						mAssert.AreEquals(ArgType, ArgType_);
						if (Head.Result != null) {
							UpdateTypes(Head.Result, ResType_, tTypeRelation.Sub, aNewScope, out aNewScope);
						}
					}
				}
				break;
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				aNewScope = mStream.Stream(
					(DefVar.Ident.Name, mVM_Type.Var(UpdateTypes(DefVar.Expression, aScope))),
					aScope
				);
				UpdateTypes(
					mSPO_AST.MethodCallStatement(
						default,
						DefVar.Expression,
						DefVar.MethodCalls
					),
					aNewScope,
					out aNewScope
				);
				break;
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				aNewScope = aScope;
				var Tail = RecLambdas.List;
				while (Tail.Match(out var Head, out Tail)) {
					aNewScope = mStream.Stream(
						(
							Head.Ident.Name,
							Type: UpdateTypes(Head.Lambda, aScope)
						),
						aNewScope
					);
				}
				Tail = RecLambdas.List;
				while (Tail.Match(out var Head, out Tail)) {
					var DesType = aNewScope.Where(_ => _.Ident == Head.Ident.Name).ForceFirst().Type;
					var SrcType = UpdateTypes(Head.Lambda, aNewScope);
					DesType.Kind = SrcType.Kind;
					DesType.Id = SrcType.Id;
					DesType.Prefix = SrcType.Prefix;
					DesType.Refs = SrcType.Refs;
				}
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	ResolveTypeExpression<tPos>(
		mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope
	) {
		mVM_Type.tType Result;
		switch (aExpression) {
			case mSPO_AST.tEmptyTypeNode<tPos> _: {
				Result = mVM_Type.Empty();
				break;
			}
			case mSPO_AST.tBoolTypeNode<tPos> _: {
				Result = mVM_Type.Bool();
				break;
			}
			case mSPO_AST.tIntTypeNode<tPos> _: {
				Result = mVM_Type.Int();
				break;
			}
			case mSPO_AST.tTypeTypeNode<tPos> _: {
				Result = mVM_Type.Type(null);
				break;
			}
			case mSPO_AST.tTupleTypeNode<tPos> TupleType: {
				var Types = mStream.Stream<mVM_Type.tType>();
				var Tail = TupleType.Expressions;
				while (Tail.Match(out var Head, out Tail)) {
					if (ResolveTypeExpression(Head, aScope).Match(out var Type, out var Error)) {
						Types = mStream.Stream(Type, Types);
					} else {
						return mResult.Fail(Error);
					}
				}
				Result = mVM_Type.Tuple(Types);
				break;
			}
			case mSPO_AST.tIdentNode<tPos> Ident: {
				if (aScope.Where(_ => _.Ident == Ident.Name).First().Match(out var Value)) {
					Result = Value.Type;
				} else {
					return mResult.Fail($"{Ident.Pos}: unknown type of identifier '{Ident.Name}'");
				}
				break;
			}
			case mSPO_AST.tLambdaTypeNode<tPos> LambdaType: {
				if (
					!ResolveTypeExpression(LambdaType.ArgType, aScope).ThenTry(
						aArgType => ResolveTypeExpression(LambdaType.ResType, aScope).Then(
							aResType => mVM_Type.Proc(mVM_Type.Empty(), aArgType, aResType)
						)
					).Match(out Result, out var Error)
				) {
					return mResult.Fail(Error);
				}
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				var Name = RecursiveType.HeadType.Name;
				var RecursiveVar = mVM_Type.Free(Name);
				var TempScope = mStream.Stream((Name, RecursiveVar), aScope);
				Result = mVM_Type.Recursive(RecursiveVar, UpdateTypes(RecursiveType.BodyType, TempScope));
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				var Tail = SetType.Expressions;
				var SubTypes = mStream.Stream<mVM_Type.tType>();
				Result = null;
				while (Tail.Match(out var Head, out Tail)) {
					var SubType = ResolveTypeExpression(Head, aScope).ElseThrow(_ => "");
					if (Result is null) {
						SubTypes = mStream.Stream(SubType);
						Result = SubType;
					} else if (SubTypes.All(_ => _ != SubType)) {
						SubTypes = mStream.Stream(SubType, SubTypes);
						Result = mVM_Type.Set(SubType, Result);
					}
				}
				break;
			}
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixType: {
				Result = mVM_Type.Prefix(
					PrefixType.Prefix.Name,
					mVM_Type.Tuple(
						PrefixType.Expressions.Map(
							_ => ResolveTypeExpression(_, aScope).ElseThrow(__ => "")
						)
					)
				);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aExpression.GetType().Name);
			}
		}
		aExpression.TypeAnnotation = Result;
		return mResult.OK(Result);
	}
	
#endif
}
