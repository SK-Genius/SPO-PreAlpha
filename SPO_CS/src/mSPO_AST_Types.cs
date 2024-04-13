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
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) => (
		aNode switch {
			mSPO_AST.tEmptyNode<tPos> _ => mResult.OK(mVM_Type.Empty()),
			mSPO_AST.tTrueNode<tPos> _ => mResult.OK(mVM_Type.Bool()),
			mSPO_AST.tFalseNode<tPos> _ => mResult.OK(mVM_Type.Bool()),
			mSPO_AST.tIntNode<tPos> _ => mResult.OK(mVM_Type.Int()),
			mSPO_AST.tIdNode<tPos> IdNode => (
				IdNode.Id == "_=..."
			) ? (
				mStd.Let(mVM_Type.Free(), FreeType => mResult.OK(mVM_Type.Proc(FreeType, FreeType, mVM_Type.Empty())))
			) : (
				aScope.Where(
					_ => _.Id == IdNode.Id
				).TryFirst(
				).ElseFail(
					() => $"No Identifier '{IdNode.Id}' in scope."
				).Then(
					_ => _.Type
				)
			),
			mSPO_AST.tTypeNode<tPos> Type => ResolveTypeExpression(Type, aScope),
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
					_ => UpdateExpressionTypes(_.Value, aScope).Then(aType => mVM_Type.Prefix(_.Key.Id, aType))
				).WhenAllThen(
					_ => _.Reduce(
						mVM_Type.Empty(),
						(aTail, aHead) => mVM_Type.Record(aTail, aHead)
					)
				)
			),
			mSPO_AST.tLambdaNode<tPos> Lambda => mStd.Call(
				() => {
					if (Lambda.Generic.IsSome(out _)) {
						throw new System.NotImplementedException();
					}
					
					return UpdateMatchTypes(
						Lambda.Head,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aScope
					).ThenTry(
						aArg => UpdateExpressionTypes(
							Lambda.Body,
							aArg.Scope
						).Then(
							aRes => mVM_Type.Proc(
								mVM_Type.Empty(),
								aArg.Type,
								aRes
							)
						)
					);
				}
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
									var Type = ReturnIf.Result.TypeAnnotation.ElseThrow();
									if (Types.All(a => !Equals(a, Type))) {
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
			mSPO_AST.tCallNode<tPos> Call => UpdateExpressionTypes(
				Call.Arg,
				aScope
			).ThenTry(
				aArgType => UpdateExpressionTypes(
					Call.Func,
					aScope
				).ThenTry(
					aFuncType => (
						aFuncType.IsProc(out var ObjType, out var ArgType, out var ResType)
						? mResult.OK((ObjType, ArgType, ResType)).AsResult<tText>()
						: mResult.Fail(mStd.FileLine())
					)
				).Then(
					aFuncType => (ArgType: aArgType, FuncType: aFuncType)
				)
			).ThenAssert(
				a => a.ArgType.IsSubType(a.FuncType.ArgType, null).Match(out _, out _),
				_ => $"Can't convert {_.ArgType.ToText()} to {_.FuncType.ArgType.ToText()}"
			).Then(
				_ => _.FuncType.ResType
			),
			mSPO_AST.tIfMatchNode<tPos> IfMatch => (
				UpdateExpressionTypes(IfMatch.Expression, aScope).ThenTry(
					aMatchType => IfMatch.Cases.Map(
						aCase => UpdateMatchTypes(aCase.Match, aMatchType, tTypeRelation.Super, aScope).ThenTry(
							_ => UpdateExpressionTypes(aCase.Expression, _.Scope)
						)
					).WhenAllThen(
						aCaseTypes => aCaseTypes.Reduce(
							mStream.Stream<mVM_Type.tType>(),
							(aList, aItem) => aList.All(_ => _ != aItem) ? mStream.Stream(aItem, aList) : aList
						).Reduce(
							(mVM_Type.tType)null!,
							(aTypeSet, aType) => aTypeSet is null ? aType : mVM_Type.Set(aType, aTypeSet)
						)
					)
				)
			),
			mSPO_AST.tVarToValNode<tPos> VarToVal => (
				mStd.Call(
					() => {
						var Type = VarToVal.Obj.TypeAnnotation.ElseThrow();
						mAssert.IsTrue(Type.IsVar(out var ValType));
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
						_ => mStd.FileLine()
					).ThenTry(
						_ => UpdateExpressionTypes(aCase.Result, aScope)
					)
				).WhenAllThen(
					a => {
						var X = a.Reduce(
							mStream.Stream<mVM_Type.tType>(),
							(aList, aItem) => aList.All(_ => _ != aItem) ? mStream.Stream(aItem, aList) : aList
						);
						
						return X.Count() switch {
							0 => mVM_Type.Empty(),
							1 => X.TryFirst().ElseThrow(() => "impossible"),
							_ => X.Reduce(
								(mVM_Type.tType)mVM_Type.Empty(),
								(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
							)
						};
					}
				)
			),
			_ => throw mError.Error("not implemented: " + aNode.GetType().Name),
		}
	).ThenDo(
		_ => { aNode.TypeAnnotation = _; }
	);
	
	public static mResult.tResult<(mVM_Type.tType Type, mStream.tStream<(tText Id, mVM_Type.tType Type)>? Scope), tText>
	UpdateMatchTypes<tPos>(
		mSPO_AST.tMatchItemNode<tPos> aMatch,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		mResult.tResult<(mVM_Type.tType Type, mStream.tStream<(tText Id, mVM_Type.tType Type)>? Scope), tText> Result;
		switch (aMatch) {
			case mSPO_AST.tMatchNode<tPos> Match: {
				Result = Match.Type.Match(
					aOnSome: aType_ => mStd.Call(
						() => ResolveTypeExpression(aType_, aScope).ThenTry(
							aType => UpdateMatchTypes(
								Match.Pattern,
								aType,
								aTypeRelation,
								aScope
							)
						)
					),
					aOnNone: () => UpdateMatchTypes(Match.Pattern, aType, aTypeRelation, aScope)
				);
				break;
			}
			case mSPO_AST.tMatchFreeIdNode<tPos> MatchFreeId: {
				Result = aType.ThenDo(
					_ => mStd.Call(
						() => {
							if (_.IsType(out var OfType)) {
								_ = OfType.Match(
									aOnNone: () => mVM_Type.Type(mVM_Type.Free(MatchFreeId.Id)),
									aOnSome: aType => _
								);
							}
							return (_, mStream.Stream((MatchFreeId.Id, _), aScope));
						}
					)
				).ElseFail(
					() => $"{MatchFreeId.Pos} : ERROR missing type for {MatchFreeId.Id}"
				);
				break;
			}
			case mSPO_AST.tIgnoreMatchNode<tPos> IgnoreMatch: {
				Result = aType.ThenDo(_ => (_, aScope)).ElseFail(() => mStd.FileLine());
				break;
			}
			case mSPO_AST.tMatchPrefixNode<tPos> MatchPrefix: {
				var SubType = mMaybe.None<mVM_Type.tType>();
				if (aType.IsSome(out var Type_)) {
					while (Type_.IsSet(out var Type, out var Types)) {
						if (Type.IsPrefix(out var Prefix, out var SubType_) && Prefix == MatchPrefix.Prefix) {
							SubType = SubType_;
							Type_ = Type;
							break;
						}
						Type_ = Types;
					}
					{
						mAssert.IsTrue(Type_.IsPrefix(out var Prefix, out var SubType__));
						SubType = SubType__;
						mAssert.AreEquals(Prefix, MatchPrefix.Prefix);
					}
				}
				Result = UpdateMatchTypes(MatchPrefix.Match, SubType, aTypeRelation, aScope).Then(
					_ => (mVM_Type.Prefix(MatchPrefix.Prefix, _.Type), _.Scope)
				);
				break;
			}
			case mSPO_AST.tMatchTupleNode<tPos> MatchTuple: {
				var Types = mStream.Stream<mVM_Type.tType>();
				var NewScope = aScope;
				if (aType.IsSome(out var TypeTail)) {
					foreach (var Item in MatchTuple.Items.Reverse()) {
						if (!TypeTail.IsPair(out TypeTail!, out var Type1)) {
							return mResult.Fail($"{Item.Pos}: ERROR expected pair");
						}
						
						if (UpdateMatchTypes(Item, Type1, aTypeRelation, NewScope).Match(out var Type_, out var Error)) {
							Types = mStream.Stream(Type_.Type, Types);
							NewScope = Type_.Scope; // TODO: now
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
						while (RecordType.IsRecord(out var Id, out var Type_, out RecordType!)) {
							Type = Type_;
							if (Id == Item.Id.Id) {
								break;
							}
						}
						Type.ElseThrow();
					}
					
					Result = Result.ThenTry(
						a1 => UpdateMatchTypes(
							Item.Match,
							Type,
							aTypeRelation,
							a1.Scope
						).Then(
							a2 => (mVM_Type.Record(a1.Type, mVM_Type.Prefix(Item.Id.Id, a2.Type)), a2.Scope)
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
			case mSPO_AST.tIdNode<tPos> Id: {
				// mResult.tResult<(mVM_Type.tType Type, mStream.tStream<(tText Id, mVM_Type.tType Type)>? Scope), tText> Result;
				Result = aType.ThenDo(
					_ => (
						Type: _,
						Scope: mStream.Stream(
							(Id: Id.Id, Type: _),
							aScope
						)
					)
				).ElseFail(() => "");
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
		return Result.ThenDo(_ => { aMatch.TypeAnnotation = _.Type; });
	}
	
	public static mResult.tResult<mStream.tStream<(tText Id, mVM_Type.tType Type)>?, tText>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) => UpdateExpressionTypes(aMethodCall.Argument, aScope).ThenTry(
		aArgType => UpdateExpressionTypes(aMethodCall.Method, aScope).ThenTry(
			aMethodType => (
				aMethodType.IsProc(out var MethObjType, out var MethArgType, out var MethResType)
				? (mResult.tResult<(mVM_Type.tType MethObjType, mVM_Type.tType MethArgType, mVM_Type.tType MethResType), tText>)mResult.OK((MethObjType, MethArgType, MethResType))
				: mResult.Fail(mStd.FileLine())
			)
		).ThenAssert(
			_ => aArgType == _.MethArgType,
			_ => mStd.FileLine()
		).ThenTry(
			_ => (
				!aMethodCall.Result.IsSome(out var T)
				? mResult.OK(aScope)
				: UpdateMatchTypes(
					T,
					_.MethResType,
					tTypeRelation.Sub,
					aScope
				).Then(_ => _.Scope)
			)
		)
	);
	
	public static mResult.tResult<mStream.tStream<(tText Id, mVM_Type.tType Type)>?, tText>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return (
					Def.Des.Type.Match(
						aOnNone: () => UpdateExpressionTypes(
							Def.Src,
							aScope
						).ThenTry(
							aType => UpdateMatchTypes(
								Def.Des,
								aType,
								tTypeRelation.Equal,
								aScope
							)
						).Then(
							_ => _.Scope
						),
						aOnSome: aDesType => ResolveTypeExpression(
							aDesType,
							aScope
						).ThenTry(
							aType => UpdateMatchTypes(
								Def.Des,
								aType,
								tTypeRelation.Super,
								aScope
							)
						).Then(
							_ => _.Scope
						)
					)
				);
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return UpdateExpressionTypes(
					ReturnIf.Condition,
					aScope
				).ThenAssert(
					aConditionType => aConditionType == mVM_Type.Bool(),
					_ => $"{_.ToText()} != {mIL_GenerateOpcodes.cBoolType} in {ReturnIf.ToText()} ({ReturnIf.Pos})"
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
						(Scope, MethodCall) => Scope.ThenTry(a => UpdateMethodCallTypes(MethodCall, a))
					).Then(
						aScope => mStream.Stream(
							(DefVar.Id.Id, mVM_Type.Var(aValueType)),
							aScope
						)
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				var NewScope = aScope;
				foreach (var Item in RecLambdas.List) {
					NewScope = mStream.Stream(
						(
							Id: Item.Id.Id,
							Type: mVM_Type.Proc(
								mVM_Type.Free("__" + Item.Id.Id + "_Obj__"),
								UpdateMatchTypes(Item.Lambda.Head, mStd.cEmpty, tTypeRelation.Equal, NewScope).ElseThrow().Type,
								mVM_Type.Free("__" + Item.Id.Id + "_Res__")
							)
						),
						NewScope
					);
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!NewScope.Where(
							_ => _.Id == Item.Id.Id
						).TryFirst(
						).ElseFail(
							() => mStd.FileLine()
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
						).Match(out _, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!UpdateExpressionTypes(Item.Lambda, NewScope).Then(
							Type => mStream.Stream(
								(
									Item.Id.Id,
									Type
								),
								NewScope
							)
						).Match(out NewScope, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				return mResult.OK(NewScope);
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return MethodCalls.MethodCalls.Reduce(
					mResult.OK(aScope).AsResult<tText>(),
					(Scope, MethodCall) => Scope.ThenTry(_ => UpdateMethodCallTypes(MethodCall, _))
				);
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
	}
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tMatchNode<tPos> aMatch
	) => TryGetId(aMatch.Pattern);
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tMatchItemNode<tPos> aMatch
	) => aMatch switch {
		mSPO_AST.tMatchFreeIdNode<tPos> Free => Free.Id,
		mSPO_AST.tIdNode<tPos> IdNode => IdNode.Id,
		mSPO_AST.tMatchNode<tPos> Match => TryGetId(Match),
		_ => mStd.cEmpty,
	};
	
	public static mResult.tResult<mVM_Type.tType, tText>
	ResolveTypeExpression<tPos>(
		mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<(tText Id, mVM_Type.tType Type)>? aScope
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
				Result = mResult.OK(mVM_Type.Type());
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
			case mSPO_AST.tIdNode<tPos> IdNode: {
				Result = aScope.Where(
					_ => _.Id == IdNode.Id
				).TryFirst(
				).ElseFail(
					() => $"{IdNode.Pos}: unknown type of Identifier '{IdNode.Id}'"
				).ThenTry(
					_ => _.Type.IsType(out var OfType)
					? OfType.ElseFail(() => "TODO")
					: mResult.Fail("TODO")
				);
				break;
			}
			case mSPO_AST.tLambdaTypeNode<tPos> LambdaType: {
				Result = ResolveTypeExpression(LambdaType.ArgType, aScope).ThenTry(               // §DEF ArgType = §TRY ResolveTypeExpression(LambdaType.ArgType, aScope)
					aArgType => ResolveTypeExpression(LambdaType.ResType, aScope).Then(        // §DEF ResType = §TRY ResolveTypeExpression(LambdaType.ResType, aScope)
						aResType => mVM_Type.Proc(mVM_Type.Empty(), aArgType, aResType)        // Result = mVM_Type.Proc(mVM_Type.Empty(), ArgType, ResType)
					)
				);
				// Result = mVM_Type.Proc(
				//   mVM_Type.Empty()
				//   §TRY ResolveTypeExpression(LambdaType.ArgType, aScope)
				//   §TRY ResolveTypeExpression(LambdaType.ResType, aScope)
				// )
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				var Name = RecursiveType.HeadType.Id;
				var RecursiveVar = mVM_Type.Free(Name);
				var TempScope = mStream.Stream((Name, RecursiveVar), aScope);
				
				Result = UpdateExpressionTypes(RecursiveType.BodyType, TempScope).Then(
					_ => mVM_Type.Recursive(RecursiveVar, _)
				);
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				Result = SetType.Expressions.Map(
					_ => ResolveTypeExpression(_, aScope)
				).WhenAllThen(
					_ => _.Reduce(
						mStream.Stream<mVM_Type.tType>(),
						(aList, aItem) => aList.All(_ => _ != aItem) ? mStream.Stream(aItem, aList) : aList
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
					_ => mVM_Type.Prefix(PrefixType.Prefix.Id, mVM_Type.Tuple(_))
				);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aExpression.GetType().Name);
			}
		}
		return Result.ThenDo(
			a => { aExpression.TypeAnnotation = a; }
		).ElseTry(
			a => throw mError.Error(a)
		);
	}
}
