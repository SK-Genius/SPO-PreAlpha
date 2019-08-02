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

public static class
mSPO_AST_Types {
	
	#if true
	
	public static mVM_Type.tType
	AddTypesTo<tPos>(
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
			case mSPO_AST.tEmptyTypeNode<tPos> _: {
				Result = mVM_Type.Type(mVM_Type.Empty());
				break;
			}
			case mSPO_AST.tBoolTypeNode<tPos> _: {
				Result = mVM_Type.Type(mVM_Type.Bool());
				break;
			}
			case mSPO_AST.tIntTypeNode<tPos> _: {
				Result = mVM_Type.Type(mVM_Type.Int());
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				Result = mVM_Type.Type(ResolveAsType(SetType, aScope).ElseThrow(_ => ""));
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				Result = mVM_Type.Type(ResolveAsType(RecursiveType, aScope).ElseThrow(_ => ""));
				break;
			}
			case mSPO_AST.tIdentNode<tPos> Ident: {
				Result = aScope.Where(
					_ => _.Ident == Ident.Name
				).ForceFirst(
				).Type;
				break;
			}
			case mSPO_AST.tMatchNode<tPos> Match: {
				if (Match.Type is null) {
					Result = AddTypesTo(Match.Pattern, aScope, out var _);
				} else {
					Result = ResolveAsType(Match.Type, aScope).Then(
						aType => mResult.OK(AddTypesTo(Match.Pattern, aType, aScope, out var _))
					).ElseThrow(_ => _);
				}
				break;
			}
			case mSPO_AST.tTupleNode<tPos> Tuple: {
				Result = mVM_Type.Tuple(
					Tuple.Items.Map(
						_ => AddTypesTo(_, aScope)
					)
				);
				break;
			}
			case mSPO_AST.tPrefixNode<tPos> Prefix: {
				Result = mVM_Type.Prefix(
					Prefix.Prefix,
					AddTypesTo(Prefix.Element, aScope)
				);
				break;
			}
			case mSPO_AST.tRecordNode<tPos> Record: {
				Result = Record.Elements.Reduce(
					mVM_Type.Empty(),
					(aTail, aHead) => mVM_Type.Record(
						mVM_Type.Prefix(
							aHead.Key.Name,
							AddTypesTo(aHead.Value, aScope)
						),
						aTail
					)
				);
				break;
			}
			case mSPO_AST.tLambdaNode<tPos> Lambda: {
				var NewScope = aScope;
				if (Lambda.Generic != null) {
					AddTypesTo(Lambda.Generic, aScope, out NewScope);
				}
				Result = mVM_Type.Proc(
					mVM_Type.Empty(),
					AddTypesTo(Lambda.Head, NewScope, out NewScope),
					AddTypesTo(Lambda.Body, NewScope)
				);
				break;
			}
			case mSPO_AST.tMethodNode<tPos> Method: {
				Result = mVM_Type.Proc(
					AddTypesTo(Method.Obj, aScope, out var NewScope),
					AddTypesTo(Method.Arg, NewScope, out var NewScope2),
					AddTypesTo(Method.Body, NewScope2)
				);
				break;
			}
			case mSPO_AST.tBlockNode<tPos> Block: {
				var Commands = Block.Commands;
				var Types = mStream.Stream<mVM_Type.tType>();
				var BlockScope = aScope;
				Result = mVM_Type.Empty();
				while (Commands.Match(out var Command, out Commands)) {
					AddTypesTo(Command, BlockScope, out BlockScope);
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
				Result = mVM_Type.Free();
				mAssert.Assert(
					mVM_Type.Unify(
						AddTypesTo(Call.Func, aScope),
						mVM_Type.Proc(mVM_Type.Empty(), AddTypesTo(Call.Arg, aScope), Result),
						_ => { _.ToString(); }
					)
				);
				break;
			}
			case mSPO_AST.tIfMatchNode<tPos> IfMatch: {
				AddTypesTo(IfMatch.Expression, aScope);
				var Tail = IfMatch.Cases;
				var Results = mStream.Stream<mVM_Type.tType>();
				Result = default;
				while (Tail.Match(out var Head, out Tail)) {
					AddTypesTo(Head.Match, aScope, out var TempScope);
					var Temp = AddTypesTo(Head.Expression, TempScope);
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
				Result = mVM_Type.Free();
				mAssert.Assert(
					mVM_Type.Unify(
						AddTypesTo(VarToVal.Obj, aScope),
						mVM_Type.Var(Result),
						_ => { _.ToString(); }
					)
				);
				break;
			}
			case mSPO_AST.tIfNode<tPos> If: {
				var Tail = If.Cases;
				Result = mVM_Type.Free();
				while (Tail.Match(out var Head, out Tail)) {
					AddTypesTo(Head.Cond, aScope);
					mAssert.Assert(
						mVM_Type.Unify(
							Result,
							AddTypesTo(Head.Result, aScope), _ => { _.ToString(); }
						)
					);
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
	AddTypesTo<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope,
		out mStream.tStream<(tText Ident, mVM_Type.tType Type)> aNewScope
	) {
		mVM_Type.tType Result;
		switch (aMatch) {
			case mSPO_AST.tExpressionNode<tPos> Expression: {
				Result = AddTypesTo(Expression, aScope);
				aNewScope = aScope;
				break;
			}
			case mSPO_AST.tMatchFreeIdentNode<tPos> FreeIdentTuple: {
				Result = mVM_Type.Free();
				aNewScope = mStream.Stream((FreeIdentTuple.Name, Result), aScope);
				break;
			}
			case mSPO_AST.tMatchNode<tPos> Match: {
				if (Match.Type is null) {
					Result = AddTypesTo(Match.Pattern, aScope, out aNewScope);
				} else {
					(aNewScope, Result) = ResolveAsType(Match.Type, aScope).Then(
						aType => {
							var Result_ = AddTypesTo(Match.Pattern, aType, aScope, out var NewScope);
							return mResult.OK((NewScope, Result_));
						}
					).ElseThrow(_ => _);
				}
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				Result = mVM_Type.Prefix(
					MatchPrefix.Prefix,
					AddTypesTo(MatchPrefix.Match, aScope, out aNewScope)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> MatchTuple: {
				var Tail = MatchTuple.Items;
				var Types = mStream.Stream<mVM_Type.tType>();
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					Types = mStream.Stream(
						AddTypesTo(Head, aNewScope, out aNewScope),
						Types
					);
				}
				Result = mVM_Type.Tuple(Types.Reverse());
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> MatchRecord: {
				var Tail = MatchRecord.Elements;
				var Types = mStream.Stream<(tText Key, mVM_Type.tType Match)>();
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					Types = mStream.Stream(
						(
							Head.Key.Name,
							AddTypesTo(Head.Match, aNewScope, out aNewScope)
						),
						Types
					);
				}
				
				Types = Types.Reverse();
				var Record = mVM_Type.Empty();
				while (Types.Match(out var Type, out Types)) {
					Record = mVM_Type.Record(mVM_Type.Prefix(Type.Key, Type.Match), Record);
				}
				Result = Record;
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatch: {
				aNewScope = aScope;
				Result = mVM_Type.Free();
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> MatchGuard: {
				Result = AddTypesTo(MatchGuard.Match, aScope, out aNewScope);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aMatch.GetType().Name);
			}
		}
		aMatch.TypeAnnotation = Result;
		return Result;
	}
	
	public static mVM_Type.tType
	AddTypesTo<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mVM_Type.tType aType,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope,
		out mStream.tStream<(tText Ident, mVM_Type.tType Type)> aNewScope
	) {
		mVM_Type.tType Result;
		switch (aMatch) {
			case mSPO_AST.tExpressionNode<tPos> Expression: {
				Result = AddTypesTo(Expression, aScope);
				mAssert.AssertEq(Result, aType);
				aNewScope = aScope;
				break;
			}
			case mSPO_AST.tMatchNode<tPos> Match: {
				Result = AddTypesTo(Match.Pattern, aType, aScope, out aNewScope);
				if (!(Match.Type is null)) {
					mAssert.AssertEq(AddTypesTo(Match.Type, aScope), aType);
				}
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				mAssert.Assert(aType.MatchPrefix(out var Prefix, out var SubType));
				mAssert.AssertEq(Prefix, MatchPrefix.Prefix);
				AddTypesTo(MatchPrefix.Match, SubType, aScope, out aNewScope);
				Result = aType;
				break;
			}
			case mSPO_AST.tMatchFreeIdentNode<tPos> MatchFreeIdent: {
				Result = aType;
				aNewScope = mStream.Stream((MatchFreeIdent.Name, aType), aScope);
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> MatchRecord: {
				var Tail = MatchRecord.Elements;
				Result = mVM_Type.Empty();
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					Result = mVM_Type.Record(
						mVM_Type.Prefix(
							Head.Key.Name,
							AddTypesTo(Head.Match, aNewScope, out aNewScope)
						),
						Result
					);
				}
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
	AddTypesTo<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)> aScope,
		out mStream.tStream<(tText Ident, mVM_Type.tType Type)> aNewScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				AddTypesTo(
					Def.Des,
					AddTypesTo(Def.Src, aScope),
					aScope,
					out aNewScope
				);
				break;
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				mAssert.AssertEq(AddTypesTo(ReturnIf.Condition, aScope), mVM_Type.Bool());
				AddTypesTo(ReturnIf.Result, aScope);
				aNewScope = aScope;
				break;
			}
			case mSPO_AST.tMethodCallNode<tPos> MethodCall: {
				var ArgType = AddTypesTo(MethodCall.Argument, aScope);
				var MethodType = AddTypesTo(MethodCall.Method, aScope);
				mAssert.Assert(mVM_Type.Unify(ArgType, MethodType, _ => _.ToString()));
				MethodType.MatchProc(out _, out _, out var ResType);
				AddTypesTo(MethodCall.Result, ResType, aScope, out aNewScope);
				break;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				var ObjType = AddTypesTo(MethodCalls.Object, aScope);
				var Tail = MethodCalls.MethodCalls;
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					var ArgType = AddTypesTo(Head.Argument, aNewScope);
					if (Head.Method.Name == "_=...") {
						mAssert.Assert(
							mVM_Type.Unify(
								ObjType,
								ArgType,
								_ => { _.ToString(); }
							)
						);
					} else {
						mAssert.Assert(
							mVM_Type.Unify(
								AddTypesTo(Head.Method, aNewScope),
								mVM_Type.Proc(
									ObjType,
									ArgType,
									Head.Result is null ? mVM_Type.Empty() : AddTypesTo(Head.Result, aNewScope, out aNewScope)
								),
								_ => { _.ToString(); }
							)
						);
					}
				}
				break;
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				aNewScope = mStream.Stream(
					(DefVar.Ident.Name, mVM_Type.Var(AddTypesTo(DefVar.Expression, aScope))),
					aScope
				);
				AddTypesTo(
					mSPO_AST.MethodCallStatment(
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
					aNewScope = mStream.Stream((Head.Ident.Name, mVM_Type.Free()), aNewScope);
				}
				Tail = RecLambdas.List;
				while (Tail.Match(out var Head, out Tail)) {
					mAssert.Assert(
						mVM_Type.Unify(
							aNewScope.Where(_ => _.Ident == Head.Ident.Name).ForceFirst().Type,
							AddTypesTo(Head.Lambda, aNewScope),
							_ => { _.ToString(); }
						)
					);
				}
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	ResolveAsType<tPos>(
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
				Result = mVM_Type.Type(mVM_Type.Free());
				break;
			}
			case mSPO_AST.tTupleTypeNode<tPos> TupleType: {
				var Types = mStream.Stream<mVM_Type.tType>();
				var Tail = TupleType.Expressions;
				while (Tail.Match(out var Head, out Tail)) {
					if (ResolveAsType(Head, aScope).Match(out var Type, out var Error)) {
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
					!ResolveAsType(LambdaType.ArgType, aScope).Then(
						aArgType => ResolveAsType(LambdaType.ResType, aScope).Then(
							aResType => mResult.OK(mVM_Type.Proc(mVM_Type.Empty(), aArgType, aResType))
						)
					).Match(out Result, out var Error)
				) {
					return mResult.Fail(Error);
				}
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursivType: {
				var Name = RecursivType.HeadType.Name;
				var RecursivVar = mVM_Type.Free(Name);
				var TempScope = mStream.Stream((Name, RecursivVar), aScope);
				Result = mVM_Type.Recursive(RecursivVar, AddTypesTo(RecursivType.BodyType, TempScope));
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				var Tail = SetType.Expressions;
				var SubTypes = mStream.Stream<mVM_Type.tType>();
				Result = null;
				while (Tail.Match(out var Head, out Tail)) {
					var SubType = ResolveAsType(Head, aScope).ElseThrow(_ => "");
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
							_ => ResolveAsType(_, aScope).ElseThrow(__ => "")
						)
					)
				);
				break;
			}
			case mSPO_AST.tCallNode<tPos> CallNode: {
				Result = mVM_Type.Free(); // TODO: maybe delegate validation of the call to a later time point
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
