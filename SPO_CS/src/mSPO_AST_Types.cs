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
	) => (
		aNode switch {
			mSPO_AST.tEmptyNode<tPos> _ => mResult.OK(mVM_Type.Empty()),
			mSPO_AST.tTrueNode<tPos> _ => mResult.OK(mVM_Type.Bool()),
			mSPO_AST.tFalseNode<tPos> _ => mResult.OK(mVM_Type.Bool()),
			mSPO_AST.tIntNode<tPos> _ => mResult.OK(mVM_Type.Int()),
			mSPO_AST.tIdentNode<tPos> Ident => (
				Ident.Name == "_=..."
			) ? (
				mStd.Let(mVM_Type.Free(), FreeType => mResult.OK(mVM_Type.Proc(FreeType, FreeType, mVM_Type.Empty())))
			) : (
				aScope.Where(
					_ => _.Ident == Ident.Name
				).TryFirst(
				).ElseFail(
					() => $"No identifier '{Ident.Name}' in scope."
				).Then(
					_ => _.Type
				)
			),
			mSPO_AST.tTypeNode<tPos> Type => ResolveTypeExpression(Type, aScope).Then(mVM_Type.Type),
			mSPO_AST.tTupleNode<tPos> Tuple => (
				Tuple.Items.Map(
					_ => UpdateExpressionTypes(_, aScope)
				).WhenAllThen(
					mVM_Type.Tuple
				)
			),
			mSPO_AST.tPrefixNode<tPos> Prefix => (
				UpdateExpressionTypes(
					Prefix.Element,
					aScope
				).Then(
					_ => mVM_Type.Prefix(Prefix.Prefix, _)
				)
			),
			mSPO_AST.tRecordNode<tPos> Record => (
				Record.Elements.Map(
					_ => UpdateExpressionTypes(_.Value, aScope).Then(_Type => mVM_Type.Prefix(_.Key.Name, _Type))
				).WhenAllThen(
					_ => _.Reduce(
						mVM_Type.Empty(),
						(aTail, aHead) => mVM_Type.Record(aHead, aTail)
					)
				)
			),
			mSPO_AST.tLambdaNode<tPos> Lambda => (
				Lambda.Generic.ElseFail(
					() => ""
				).ThenTry(
					aMatch => UpdateMatchTypes(
						aMatch,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aScope
					)
				).ElseTry<(mVM_Type.tType Type, mStream.tStream<(tText Ident, mVM_Type.tType Type)>? Scope), tText>(
					_ => UpdateMatchTypes(
						Lambda.Head,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aScope
					).ThenTry(
						aArg => UpdateExpressionTypes(
							Lambda.Body,
							aArg.Scope
						).Then(
							aRes => (
								mVM_Type.Proc(
									mVM_Type.Empty(),
									aArg.Type,
									aRes
								),
								aArg.Scope
							)
						)
					)
				).Then(
					_ => _.Type
				)
			),
			mSPO_AST.tMethodNode<tPos> Method => (
				UpdateMatchTypes(Method.Obj, mStd.cEmpty, tTypeRelation.Equal, aScope).ThenTry(
					aObj => UpdateMatchTypes(Method.Arg, mStd.cEmpty, tTypeRelation.Sub, aObj.Scope).ThenTry(
						aArg => UpdateExpressionTypes(Method.Body, aArg.Scope).Then(
							aResType => mVM_Type.Proc(aObj.Type, aArg.Type, aResType)
						)
					)
				)
			),
			mSPO_AST.tBlockNode<tPos> Block => (
				mStd.Call(
					() => {
						var Types = mStream.Stream<mVM_Type.tType>();
						var BlockScope = aScope;
						foreach (var Command in Block.Commands) {
							if (UpdateCommandTypes(Command, BlockScope).Match(out BlockScope, out var Error)) {
								if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
									var Type = ReturnIf.Result.TypeAnnotation.ElseThrow("");
									if (Types.All(_ => !Equals(_, Type))) {
										Types = mStream.Stream(Type, Types);
									}
								}
							} else {
								return (mResult.tResult<mVM_Type.tType, tText>)mResult.Fail(Error);
							}
						}
						return mResult.OK(Types.Join((a1, a2) => mVM_Type.Set(a2, a1), mVM_Type.Empty()));
					}
				)
			),
			mSPO_AST.tCallNode<tPos> Call => (
				UpdateExpressionTypes(Call.Func, aScope).ThenTry(
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
				)
			//TODO: mAssert.True(ArgType.IsSubtype(Call.Arg.TypeAnnotation));
			),
			mSPO_AST.tIfMatchNode<tPos> IfMatch => (
				UpdateExpressionTypes(IfMatch.Expression, aScope).ThenTry(
					MatchType => IfMatch.Cases.Map(
						aCase => UpdateMatchTypes(aCase.Match, mMaybe.Some(MatchType), tTypeRelation.Super, aScope).ThenTry(
							_ => UpdateExpressionTypes(aCase.Expression, _.Scope)
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
				)
			),
			mSPO_AST.tVarToValNode<tPos> VarToVal => (
				mStd.Call(
					() => {
						var Type = VarToVal.Obj.TypeAnnotation.ElseThrow("");
						mAssert.IsTrue(Type.MatchVar(out var ValType));
						return mResult.OK(ValType);
					}
				)
			),
			mSPO_AST.tIfNode<tPos> If => (
				If.Cases.Map(
					aCase => UpdateExpressionTypes(
						aCase.Cond,
						aScope
					).ThenAssert(
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
				)
			),
			_ => throw mError.Error("not implemented: " + aNode.GetType().Name),
		}
	).ThenDo(
		a_ => { aNode.TypeAnnotation = mMaybe.Some(a_); }
	);
	
	public static mResult.tResult<(mVM_Type.tType Type, mStream.tStream<(tText Ident, mVM_Type.tType Type)>? Scope), tText>
	UpdateMatchTypes<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope
	) {
		mResult.tResult<(mVM_Type.tType Type, mStream.tStream<(tText Ident, mVM_Type.tType Type)>? Scope), tText> Result;
		switch (aMatch) {
			case mSPO_AST.tMatchNode<tPos> Match: {
					Result = Match.Type.Match(
						Some: Type_ => UpdateMatchTypes(
							Match.Pattern,
							mMaybe.Some(ResolveTypeExpression(Type_, aScope).ElseThrow(_ => _)),
							aTypeRelation,
							aScope
						),
						None: () => UpdateMatchTypes(Match.Pattern, aType, aTypeRelation, aScope)
					);
				break;
			}
			case mSPO_AST.tMatchFreeIdentNode<tPos> MatchFreeIdent: {
				if (aType.IsSome(out var Type)) {
					Result = mResult.OK((Type,  mStream.Stream((MatchFreeIdent.Name, Type), aScope)));
				} else {
					return mResult.Fail($"{MatchFreeIdent.Pos} : ERROR missing type for {MatchFreeIdent.Name}");
				}
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatch: {
				Result = aType.Then(_ => (_, aScope)).ElseFail(() => "");
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				var SubType = mMaybe.None<mVM_Type.tType>();
				if (aType.IsSome(out var Type_)) {
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
				Result = UpdateMatchTypes(MatchPrefix.Match, SubType, aTypeRelation, aScope).Then(
					a => (mVM_Type.Prefix(MatchPrefix.Prefix, a.Type), a.Scope)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> MatchTuple: {
				var Types = mStream.Stream<mVM_Type.tType>();
				var NewScope = aScope;
				if (aType.IsSome(out var Type__)) {
					foreach (var Item in MatchTuple.Items) {
						if (!Type__.MatchPair(out var Type, out Type__!)) {
							return mResult.Fail($"{Item.Pos}: ERROR expected pair");
						}
						
						if (UpdateMatchTypes(Item, mMaybe.Some(Type), aTypeRelation, NewScope).Match(out var Type_, out var Error)) {
							Types = mStream.Stream(Type_.Type, Types);
						} else {
							return mResult.Fail(Error);
						}
					}
				} else {
					foreach (var Item in MatchTuple.Items) {
						if (UpdateMatchTypes(Item, mStd.cEmpty, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
							NewScope = TS.Scope;
							Types = mStream.Stream(TS.Type, Types);
						} else {
							return mResult.Fail(Error);
						}
					}
				}
				Result = mResult.OK((mVM_Type.Tuple(Types.Reverse()), NewScope));
				break;
			}
			case mSPO_AST.tMatchRecordNode<tPos> MatchRecord: {
				Result = mResult.OK((mVM_Type.Empty(), aScope));
				foreach (var Item in MatchRecord.Elements) {
					var Type = mMaybe.None<mVM_Type.tType>();
					if (aType.IsSome(out var RecordType)) { 
						while (RecordType.MatchRecord(out var Key, out var Type_, out RecordType!)) {
							Type = mMaybe.Some(Type_);
							if (Key == Item.Key.Name) {
								break;
							}
						}
						Type.ElseThrow("");
					}
					
					Result = Result.ThenTry(
						a1 => UpdateMatchTypes(
							Item.Match,
							Type,
							aTypeRelation,
							a1.Scope
						).Then(
							a2 => (mVM_Type.Record(mVM_Type.Prefix(Item.Key.Name, a2.Type), a1.Type), a2.Scope)
						)
					);
				}
				break;
			}
			case mSPO_AST.tMatchGuardNode<tPos> MatchGuard: {
				mAssert.AreEquals(
					UpdateExpressionTypes(MatchGuard.Guard, aScope),
					mResult.OK(mVM_Type.Bool())
				);
				Result = UpdateMatchTypes(MatchGuard.Match, aType, tTypeRelation.Super, aScope);
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
					Result = UpdateExpressionTypes(Expression, aScope).Then(a => (a, aScope));
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aMatch.GetType().Name);
			}
		}
		return Result.ThenDo(_ => { aMatch.TypeAnnotation = mMaybe.Some(_.Type); });
	}
	
	public static mResult.tResult<mStream.tStream<(tText Ident, mVM_Type.tType Type)>?, tText>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope
	) => UpdateExpressionTypes(aMethodCall.Argument, aScope).ThenTry(
		aArgType => UpdateExpressionTypes(aMethodCall.Method, aScope).ThenTry(
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
				!aMethodCall.Result.IsSome(out var T)
				? mResult.OK(aScope)
				: UpdateMatchTypes<tPos>(
					T,
					mMaybe.Some(_.MethResType),
					tTypeRelation.Sub,
					aScope
				).Then(_ => _.Scope)
			)
		)
	);
	
	public static mResult.tResult<mStream.tStream<(tText Ident, mVM_Type.tType Type)>?, tText>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<(tText Ident, mVM_Type.tType Type)>? aScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return (
					Def.Des.Type.IsSome(out var DesType)
					? ResolveTypeExpression(DesType, aScope).ThenTry(
						aDesType => UpdateMatchTypes(
							Def.Des,
							mMaybe.Some(aDesType),
							tTypeRelation.Super,
							aScope
						)
					)
					: UpdateExpressionTypes(Def.Src, aScope).ThenTry(
						aSrcType => UpdateMatchTypes(
							Def.Des,
							mMaybe.Some(aSrcType),
							tTypeRelation.Equal,
							aScope
						)
					)
				).Then(_ => _.Scope);
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return UpdateExpressionTypes(
					ReturnIf.Condition,
					aScope
				).ThenAssert(
					aConditionType => aConditionType == mVM_Type.Bool(),
					_ => ""
				).ThenTry(
					_ => UpdateExpressionTypes(ReturnIf.Result, aScope)
				).Then(
					_ => aScope
				);
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				return UpdateExpressionTypes(DefVar.Expression, aScope).ThenTry(
					aValueType => DefVar.MethodCalls.Reduce(
						mResult.OK(aScope).AsResult<tText>(),
						(Scope, MethodCall) => Scope.ThenTry(_ => UpdateMethodCallTypes<tPos>(MethodCall, _))
					).Then(
						aScope => mStream.Stream(
							(DefVar.Ident.Name, mVM_Type.Var(aValueType)),
							aScope
						)
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				var NewScope = aScope;
				foreach (var Item in RecLambdas.List) {
					if (
						!UpdateExpressionTypes(Item.Lambda, aScope).Then(
							Type => mStream.Stream(
								(
									Item.Ident.Name,
									Type
								),
								NewScope
							)
						).Match(out NewScope, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				foreach (var Item in RecLambdas.List) {
					if (!NewScope.Where(
						_ => _.Ident == Item.Ident.Name
					).TryFirst(
					).ElseFail(
						() => ""
					).Then(
						_ => _.Type
					).ThenTry(
						DesType => UpdateExpressionTypes(Item.Lambda, NewScope).ThenDo(
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
				return mResult.OK(NewScope);
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return MethodCalls.MethodCalls.Reduce(
					mResult.OK(aScope).AsResult<tText>(),
					(Scope, MethodCall) => Scope.ThenTry(_ => UpdateMethodCallTypes<tPos>(MethodCall, _))
				);
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
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
				foreach (var Expression in TupleType.Expressions) {
					if (ResolveTypeExpression(Expression, aScope).Match(out var Type, out var Error)) {
						Types = mStream.Stream(Type, Types);
					} else {
						return mResult.Fail(Error);
					}
				}
				Result = mResult.OK(mVM_Type.Tuple(Types));
				break;
			}
			case mSPO_AST.tIdentNode<tPos> Ident: {
				Result = aScope.Where(_ => _.Ident == Ident.Name).TryFirst(
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
		return Result.ThenDo(
			_ => { aExpression.TypeAnnotation = mMaybe.Some(_); }
		).ElseTry(
			_ => throw mError.Error(_)
		);
	}
}
