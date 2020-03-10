//IMPORT mArrayList.cs
//IMPORT mVM_Type.cs
//IMPORT mSPO_AST.cs
//IMPORT mResult.cs

#nullable enable

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
	public enum tTypeRelation {
		Sub,
		Equal,
		Super,
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	UpdateExpressionTypes<tPos>(
		mSPO_AST.tExpressionNode<tPos> aNode,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope
	) {
		mResult.tResult<mVM_Type.tType, tText> Result;
		switch (aNode) {
			case mSPO_AST.tEmptyNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Empty());
				break;
			}
			case mSPO_AST.tTrueNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Bool());
				break;
			}
			case mSPO_AST.tFalseNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Bool());
				break;
			}
			case mSPO_AST.tIntNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Int());
				break;
			}
			case mSPO_AST.tIdentNode<tPos> Ident: {
				Result = aScope.Where(
					_ => _.Ident == Ident.Name
				).First(
				).ElseFail(
					() => ""
				).Then(
					_ => _.Type
				);
				break;
			}
			case mSPO_AST.tTypeNode<tPos> Type: {
				Result = ResolveTypeExpression(Type, aScope).Then(mVM_Type.Type);
				break;
			}
			case mSPO_AST.tTupleNode<tPos> Tuple: {
				Result = Tuple.Items.Map(
					_ => UpdateExpressionTypes(_, aScope)
				).WhenAllThen(
					mVM_Type.Tuple
				);
				break;
			}
			case mSPO_AST.tPrefixNode<tPos> Prefix: {
				Result = UpdateExpressionTypes(Prefix.Element, aScope).Then(
					_ => mVM_Type.Prefix(Prefix.Prefix, _)
				);
				break;
			}
			case mSPO_AST.tRecordNode<tPos> Record: {
				Result = Record.Elements.Map(
					_ => UpdateExpressionTypes(_.Value, aScope).Then(__ => mVM_Type.Prefix(_.Key.Name, __))
				).WhenAllThen(
					_ => _.Reduce(
						mVM_Type.Empty(),
						(aTail, aHead) => mVM_Type.Record(aHead, aTail)
					)
				);
				break;
			}
			case mSPO_AST.tLambdaNode<tPos> Lambda: {
				var NewScope = aScope;
				if (Lambda.Generic.Match(out var Match)) {
					if (!UpdateMatchTypes(Match, mStd.cEmpty, tTypeRelation.Sub, aScope, out NewScope).Match(out _, out var Error)) {
						return mResult.Fail(Error);
					}
				}
				Result = UpdateMatchTypes(Lambda.Head, mStd.cEmpty, tTypeRelation.Sub, NewScope, out NewScope).ThenTry(
					aArgType => UpdateExpressionTypes(Lambda.Body, NewScope).Then(
						aResType => mVM_Type.Proc(
							mVM_Type.Empty(),
							aArgType,
							aResType
						)
					)
				);
				break;
			}
			case mSPO_AST.tMethodNode<tPos> Method: {
				Result = UpdateMatchTypes(Method.Obj, mStd.cEmpty, tTypeRelation.Equal, aScope, out var NewScope).ThenTry(
					aObjType => UpdateMatchTypes(Method.Arg, mStd.cEmpty, tTypeRelation.Sub, NewScope, out var NewScope2).ThenTry(
						aArgType => UpdateExpressionTypes(Method.Body, NewScope2).Then(
							aResType => mVM_Type.Proc(aObjType, aArgType, aResType)
						)
					)
				);
				break;
			}
			case mSPO_AST.tBlockNode<tPos> Block: {
				var Commands = Block.Commands;
				var Types = mStream.Stream<mVM_Type.tType>();
				var BlockScope = aScope;
				while (Commands.Match(out var Command, out Commands)) {
					if (UpdateCommandTypes(Command, BlockScope).Match(out BlockScope, out var Error)) {
						if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
							var Type = ReturnIf.Result.TypeAnnotation;
							mAssert.IsTrue(Type.Match(out var Type_));
							if (Types.All(_ => !Equals(_, Type_))) {
								Types = mStream.Stream(Type_, Types);
							}
						}
					} else {
						return mResult.Fail(Error);
					}
				}
				Result = mResult.OK(Types.Join((a1, a2) => mVM_Type.Set(a2, a1), mVM_Type.Empty()));
				break;
			}
			case mSPO_AST.tCallNode<tPos> Call: {
				Result = UpdateExpressionTypes(Call.Func, aScope).ThenTry(
					FuncType => (
						FuncType.MatchProc(out var ObjType, out var ArgType, out var ResType)
						? (mResult.tResult<(mVM_Type.tType ObjType, mVM_Type.tType ArgType, mVM_Type.tType ResType), tText>)mResult.OK((ObjType, ArgType, ResType))
						: mResult.Fail("")
					).ThenAssert(
						_ => _.ObjType.MatchEmpty(),
						_ => ""
					).Then(
						_ => _.ResType
					)
				);
				//TODO: mAssert.True(ArgType.IsSubtype(Call.Arg.TypeAnnotation));
				break;
			}
			case mSPO_AST.tIfMatchNode<tPos> IfMatch: {
				Result = UpdateExpressionTypes(IfMatch.Expression, aScope).ThenTry(
					MatchType => IfMatch.Cases.Map(
						aCase => UpdateMatchTypes(aCase.Match, mMaybe.Some(MatchType), tTypeRelation.Super, aScope, out var TempScope).ThenTry(
							_ => UpdateExpressionTypes(aCase.Expression, TempScope)
						)
					).WhenAllThen(
						aCaseTypes => aCaseTypes.Reduce(
							mStream.Stream<mVM_Type.tType>(),
							(aList, aItem) => aList.All(a => a != aItem) ? mStream.Stream(aItem, aList) : aList
						).Reduce(
							(mVM_Type.tType)null!,
							(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
						)
					)
				);
				break;
			}
			case mSPO_AST.tVarToValNode<tPos> VarToVal: {
				mAssert.IsTrue(VarToVal.Obj.TypeAnnotation.Match(out var Type));
				mAssert.IsTrue(Type.MatchVar(out var ValType));
				Result = mResult.OK(ValType);
				break;
			}
			case mSPO_AST.tIfNode<tPos> If: {
				Result = If.Cases.Map(
					aCase => UpdateExpressionTypes(aCase.Cond, aScope).ThenAssert(
						a => a == mVM_Type.Bool(),
						_ => ""
					).ThenTry(
						_ => UpdateExpressionTypes(aCase.Result, aScope)
					)
				).WhenAllThen(
					a => a.Reduce(
						mStream.Stream<mVM_Type.tType>(),
						(aList, aItem) => aList.All(a => a != aItem) ? mStream.Stream(aItem, aList) : aList
					).Reduce(
						(mVM_Type.tType)null!,
						(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
					)
				);
				break;
			}
			default: {
				throw mError.Error($"not implemented: " + aNode.GetType().Name);
			}
		}
		return Result.ThenDo(_ => { aNode.TypeAnnotation = mMaybe.Some(_); });
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	UpdateMatchTypes<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope,
		out mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aNewScope
	) {
		mResult.tResult<mVM_Type.tType, tText> Result;
		switch (aMatch) {
			case mSPO_AST.tMatchNode<tPos> Match: {
				if (Match.Type.Match(out var Type_)) {
					var Type = ResolveTypeExpression(Type_, aScope).ElseThrow(_ => _);
					Result = UpdateMatchTypes(Match.Pattern, mMaybe.Some(Type), aTypeRelation, aScope, out aNewScope);
				} else {
					Result = UpdateMatchTypes(Match.Pattern, aType, aTypeRelation, aScope, out aNewScope);
				}
				break;
			}
			case mSPO_AST.tMatchFreeIdentNode<tPos> MatchFreeIdent: {
				if (aType.Match(out var Type)) {
					Result = mResult.OK(Type);
					aNewScope = mStream.Stream((MatchFreeIdent.Name, Type), aScope);
				} else {
					aNewScope = default;
					return mResult.Fail($"{MatchFreeIdent.Pos} : ERROR missing type for {MatchFreeIdent.Name}");
				}
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatch: {
				aNewScope = aScope;
				mAssert.IsTrue(aType.Match(out var Type_));
				Result = mResult.OK(Type_);
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				var SubType = mMaybe.None<mVM_Type.tType>();
				if (aType.Match(out var Type_)) {
					
					while (Type_.MatchSet(out var Type, out var Types)) {
						if (Type.MatchPrefix(out var Prefix, out var SubType_) && Prefix == MatchPrefix.Prefix) {
							SubType = mMaybe.Some(SubType_);
							Type_ = Type;
							break;
						}
						Type_ = Types;
					}
					{
						mAssert.IsTrue(Type_.MatchPrefix(out var Prefix, out var SubType__));
						SubType = mMaybe.Some(SubType__);
						mAssert.AreEquals(Prefix, MatchPrefix.Prefix);
					}
				}
				Result = UpdateMatchTypes(MatchPrefix.Match, SubType, aTypeRelation, aScope, out aNewScope).Then(
					_ => mVM_Type.Prefix(MatchPrefix.Prefix, _)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> MatchTuple: {
				var Tail = MatchTuple.Items;
				var Types = mStream.Stream<mVM_Type.tType>();
				aNewScope = aScope;
				if (aType.Match(out var Type__)) {
					while (Tail.Match(out var Head, out Tail)) {
						if (!Type__.MatchPair(out var Type, out Type__!)) {
							return mResult.Fail($"{Head.Pos}: ERROR expected pair");
						}
						
						if (UpdateMatchTypes(Head, mMaybe.Some(Type), aTypeRelation, aNewScope, out aNewScope).Match(out var Type_, out var Error)) {
							Types = mStream.Stream(Type_, Types);
						} else {
							return mResult.Fail(Error);
						}
					}
				} else {
					while (Tail.Match(out var Head, out Tail)) {
						if (UpdateMatchTypes(Head, mStd.cEmpty, aTypeRelation, aNewScope, out aNewScope).Match(out var Type, out var Error)) {
							Types = mStream.Stream(Type, Types);
						} else {
							return mResult.Fail(Error);
						}
					}
				}
				Result = mResult.OK(mVM_Type.Tuple(Types.Reverse()));
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> MatchRecord: {
				var Tail = MatchRecord.Elements;
				Result = mResult.OK(mVM_Type.Empty());
				aNewScope = aScope;
				while (Tail.Match(out var Head, out Tail)) {
					var Type = mMaybe.None<mVM_Type.tType>();
					if (aType.Match(out var RecordType)) { 
						while (RecordType.MatchRecord(out var Key, out var Type_, out RecordType!)) {
							Type = mMaybe.Some(Type_);
							if (Key == Head.Key.Name) {
								break;
							}
						}
						mAssert.IsTrue(Type.Match(out _));
					}
					
					Result = UpdateMatchTypes(
						Head.Match,
						Type,
						aTypeRelation,
						aNewScope,
						out aNewScope
					).Then(
						a => mVM_Type.Record(mVM_Type.Prefix(Head.Key.Name, a), Result._Value)
					);
				}
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> MatchGuard: {
				mAssert.AreEquals(
					UpdateExpressionTypes(MatchGuard.Guard, aScope),
					mResult.OK(mVM_Type.Bool())
				);
				aNewScope = aScope;
				Result = UpdateMatchTypes(MatchGuard.Match, aType, tTypeRelation.Super, aScope, out aNewScope);
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
					Result = UpdateExpressionTypes(Expression, aScope);
					aNewScope = aScope;
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aMatch.GetType().Name);
			}
		}
		return Result.ThenDo(_ => { aMatch.TypeAnnotation = mMaybe.Some(_); });
	}
	
	public static mResult.tResult<mStream.tStream<(tText Ident, mVM_Type.tType Type)>?, tText>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope
	) {
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? NewScope;
		
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				if (Def.Des.Type.Match(out var Type)) {
					return ResolveTypeExpression(Type, aScope).ThenTry(
						aType => UpdateMatchTypes(
							Def.Des,
							mMaybe.Some(aType),
							tTypeRelation.Super,
							aScope,
							out var Scope_
						).Then(
							_ => Scope_
						)
					);
				} else {
					return UpdateExpressionTypes(Def.Src, aScope).ThenTry(
						aType => UpdateMatchTypes(
							Def.Des,
							mMaybe.Some(aType),
							tTypeRelation.Equal,
							aScope,
							out var Scope_
						).Then(
							_ => Scope_
						)
					);
				}
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return UpdateExpressionTypes(ReturnIf.Condition, aScope)
				.ThenAssert(_ => _ == mVM_Type.Bool(), _ => "")
				.ThenTry(_ => UpdateExpressionTypes(ReturnIf.Result, aScope))
				.Then(_ => aScope);
			}
			case mSPO_AST.tMethodCallNode<tPos> MethodCall: {
				return UpdateExpressionTypes(MethodCall.Argument, aScope).ThenTry(
					aArgType => UpdateExpressionTypes(MethodCall.Method, aScope).ThenTry(
						aMethodType => (
							aMethodType.MatchProc(out var MethObjType, out var MethArgType, out var MethResType)
							? (mResult.tResult<(mVM_Type.tType MethObjType, mVM_Type.tType MethArgType, mVM_Type.tType MethResType), tText>)mResult.OK((MethObjType, MethArgType, MethResType))
							: mResult.Fail("")
						)
					).ThenAssert(
						_ => aArgType == _.MethArgType,
						_ => ""
					).ThenTry(
						_ => (
							!MethodCall.Result.Match(out var T)
							? mResult.OK(aScope)
							: UpdateMatchTypes<tPos>(
								T,
								mMaybe.Some(_.MethResType),
								tTypeRelation.Sub,
								aScope,
								out var Scope_
							).Then(
								_ => Scope_
							)
						)
					)
				);
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return UpdateExpressionTypes(MethodCalls.Object, aScope).ThenTry(
					ObjType => {
						var Tail = MethodCalls.MethodCalls;
						NewScope = aScope;
						while (Tail.Match(out var Head, out Tail)) {
							if (
								!UpdateExpressionTypes(Head.Argument, NewScope).ThenTry(
									ArgType => {
										if (Head.Method.Name == "_=...") {
											ObjType = mVM_Type.Var(ArgType); // TODO
											return mResult.OK(NewScope);
										} else {
											return UpdateExpressionTypes(Head.Method, NewScope).ThenTry(
												MethodType => {
													if (MethodType.MatchProc(out var ObjType_, out var ArgType_, out var ResType_)) {
														return (mResult.tResult<(mVM_Type.tType Obj, mVM_Type.tType Arg, mVM_Type.tType Res), tText>)mResult.OK((Obj: ObjType_, Arg: ArgType_, Res: ResType_));
													} else {
														return mResult.Fail("");
													}
												}
											).ThenAssert(
												Types => Types.Obj.Equals(ObjType),
												_ => ""
											).ThenTry(
												Types => {
													if (Head.Result.Match(out var Result)) {
														return UpdateMatchTypes(Result, mMaybe.Some(Types.Res), tTypeRelation.Sub, NewScope, out NewScope).Then(
															_ => NewScope
														);
													}
													return mResult.OK(NewScope);
												}
											);
										}
									}
								).Match(out _, out var Error)
							) {
								return (mResult.tResult<mStream.tStream<(tText Ident, mVM_Type.tType Type)>?, tText>)mResult.Fail(Error);
							}
						}
						return mResult.OK(NewScope);
					}
				);
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				return UpdateExpressionTypes(DefVar.Expression, aScope).ThenTry(
					aValueType => UpdateCommandTypes(
						mSPO_AST.MethodCallStatement(
							DefVar.Pos,
							DefVar.Expression,
							DefVar.MethodCalls
						),
						mStream.Stream(
							(DefVar.Ident.Name, mVM_Type.Var(aValueType)),
							aScope
						)
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				NewScope = aScope;
				var Tail = RecLambdas.List;
				while (Tail.Match(out var Head, out Tail)) {
					if (
						!UpdateExpressionTypes(Head.Lambda, aScope).Then(
							Type => mStream.Stream(
								(
									Head.Ident.Name,
									Type: Type
								),
								NewScope
							)
						).Match(out NewScope, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				Tail = RecLambdas.List;
				while (Tail.Match(out var Head, out Tail)) {
					if (!NewScope.Where(
						_ => _.Ident == Head.Ident.Name
					).First(
					).ElseFail(
						() => ""
					).Then(
						_ => _.Type
					).ThenTry(
						DesType => UpdateExpressionTypes(Head.Lambda, NewScope).ThenDo(
							SrcType => {
								DesType.Kind = SrcType.Kind;
								DesType.Id = SrcType.Id;
								DesType.Prefix = SrcType.Prefix;
								DesType.Refs = SrcType.Refs;
							}
						)
					).Match(out _, out var Error)) {
						return mResult.Fail(Error);
					}
				}
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
		return mResult.OK(NewScope);
	}
	
	public static mResult.tResult<mVM_Type.tType, tText>
	ResolveTypeExpression<tPos>(
		mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope
	) {
		mResult.tResult<mVM_Type.tType, tText> Result;
		switch (aExpression) {
			case mSPO_AST.tEmptyTypeNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Empty());
				break;
			}
			case mSPO_AST.tBoolTypeNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Bool());
				break;
			}
			case mSPO_AST.tIntTypeNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Int());
				break;
			}
			case mSPO_AST.tTypeTypeNode<tPos> _: {
				Result = mResult.OK(mVM_Type.Free("***"));
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
				Result = mResult.OK(mVM_Type.Tuple(Types));
				break;
			}
			case mSPO_AST.tIdentNode<tPos> Ident: {
				Result = aScope.Where(_ => _.Ident == Ident.Name).First(
				).ElseFail(
					() => $"{Ident.Pos}: unknown type of identifier '{Ident.Name}'"
				).Then(
					_ => _.Type
				);
				break;
			}
			case mSPO_AST.tLambdaTypeNode<tPos> LambdaType: {
				Result = ResolveTypeExpression(LambdaType.ArgType, aScope).ThenTry(
					aArgType => ResolveTypeExpression(LambdaType.ResType, aScope).Then(
						aResType => mVM_Type.Proc(mVM_Type.Empty(), aArgType, aResType)
					)
				);
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				var Name = RecursiveType.HeadType.Name;
				var RecursiveVar = mVM_Type.Free(Name);
				var TempScope = mStream.Stream((Name, RecursiveVar), aScope);
				
				Result = UpdateExpressionTypes(RecursiveType.BodyType, TempScope).Then(
					_ => mVM_Type.Recursive(RecursiveVar, _)
				);
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				Result = SetType.Expressions.Map(
					a => ResolveTypeExpression(a, aScope)
				).WhenAllThen(
					a => a.Reduce(
						mStream.Stream<mVM_Type.tType>(),
						(aList, aItem) => aList.All(a => a != aItem) ? mStream.Stream(aItem, aList) : aList
					).Reduce(
						mVM_Type.Empty(),
						(aSet, aItem) => mVM_Type.Set(aItem, aSet)
					)
				);
				break;
			}
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixType: {
				Result = PrefixType.Expressions.Map(
					_ => ResolveTypeExpression(_, aScope)
				).WhenAllThen(
					_ => mVM_Type.Prefix(PrefixType.Prefix.Name, mVM_Type.Tuple(_))
				);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aExpression.GetType().Name);
			}
		}
		return Result.ThenDo(_ => { aExpression.TypeAnnotation = mMaybe.Some(_); });
	}
}
