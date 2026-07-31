#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mAssert.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mError.cs
#:ref Common/mStream.cs
#:ref mVM_Type.cs
#:ref mSPO_AST.cs
#:ref mIL_GenerateOpcodes.cs

public static class
mSPO_AST_Types {
	public struct tScopeItem {
		public System.String Id;
		public mMaybe.tMaybe<mVM_Type.tType> FreeType;
		public mVM_Type.tType Type;
	}
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType,
		mMaybe.tMaybe<mVM_Type.tType> aFreeType
	) => new () {
		Id = aId,
		FreeType = aFreeType,
		Type = aType,
	};
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType
	) => new () {
		Id = aId,
		FreeType = mStd.cEmpty,
		Type = aType,
	};
	
	public enum tTypeRelation {
		Sub,
		Equal,
		Super,
	}
	
	public static (
		mMaybe.tMaybe<mVM_Type.tType> Matched,
		mMaybe.tMaybe<mVM_Type.tType> Remaining
	)
	SplitForPatternType<tPos>(
		this mVM_Type.tType aType,
		mSPO_AST.tPatternNode<tPos> aPattern
	) {
		switch (aPattern) {
			case mSPO_AST.tGuardPatternNode<tPos> Guard: {
				return (
					aType.SplitForPatternType(Guard.Pattern).Matched,
					aType
				);
			}
			case mSPO_AST.tFreeIdPatternNode<tPos>:
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tIdNode<tPos>:
			case mSPO_AST.tVarPatternNode<tPos>: {
				return (aType, mStd.cEmpty);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsAny():
			case mSPO_AST.tPatternNode<tPos> when aType.IsFree(out _, out var Ref) && ReferenceEquals(aType, Ref): {
				static mVM_Type.tType
				PatternType(
					mSPO_AST.tPatternNode<tPos> aPattern_,
					mVM_Type.tType aFallback
				) => aPattern_.TypeAnnotation.ElseUse(
					aPattern_ switch {
						mSPO_AST.tTypedPatternNode<tPos> Typed_ => PatternType(Typed_.Pattern, aFallback),
						mSPO_AST.tGuardPatternNode<tPos> Guard_ => PatternType(Guard_.Pattern, aFallback),
						mSPO_AST.tEmptyNode<tPos> => mVM_Type.Empty(),
						mSPO_AST.tTrueNode<tPos> => mVM_Type.True(),
						mSPO_AST.tFalseNode<tPos> => mVM_Type.False(),
						mSPO_AST.tIntNode<tPos> => mVM_Type.Int(),
						mSPO_AST.tPairPatternNode<tPos> Pair_ => mVM_Type.Pair(
							PatternType(Pair_.Tail, aFallback),
							PatternType(Pair_.Head, aFallback)
						),
						mSPO_AST.tPrefixPatternNode<tPos> Prefix_ => mVM_Type.Prefix(
							Prefix_.Prefix,
							PatternType(Prefix_.Pattern, aFallback)
						),
						mSPO_AST.tRecordPatternNode<tPos> Record_ => Record_.Elements.Reduce(
							mVM_Type.Empty(),
							(Type_, Field) => mVM_Type.Record(
								Type_,
								mVM_Type.Prefix(
									Field.Id.Id,
									PatternType(Field.Pattern, aFallback)
								)
							)
						),
						_ => aFallback,
					}
				);
				
				var Matched = PatternType(aPattern, aType);
				return (
					Matched,
					Matched == aType ? mStd.cEmpty : aType
				);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsRecursive(out var RecursiveHead, out var RecursiveBody): {
				var Expanded = RecursiveBody.Substitute(RecursiveHead, aType);
				
				return (
					ReferenceEquals(Expanded, aType)
					? (aType, aType)
					: Expanded.SplitForPatternType(aPattern)
				);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsSet(out var Type1, out var Type2): {
				var Coverage1 = Type1.SplitForPatternType(aPattern);
				var Coverage2 = Type2.SplitForPatternType(aPattern);
				
				return (
					mVM_Type.Union(Coverage1.Matched, Coverage2.Matched),
					mVM_Type.Union(Coverage1.Remaining, Coverage2.Remaining)
				);
			}
			case mSPO_AST.tTypedPatternNode<tPos> Typed: {
				if (
					Typed.TypeExpression.IsSome(out _) &&
					Typed.Pattern is mSPO_AST.tFreeIdPatternNode<tPos> &&
					Typed.TypeAnnotation.IsSome(out var MatchType)
				) {
					if (aType.IsSubType(MatchType, mStd.cEmpty).Match(out _, out _)) {
						return (aType, mStd.cEmpty);
					}
					
					return (
						MatchType.IsSubType(aType, mStd.cEmpty).Match(out _, out _)
						? (MatchType, aType)
						: (mStd.cEmpty, aType)
					);
				}
				return aType.SplitForPatternType(Typed.Pattern);
			}
			case mSPO_AST.tPairPatternNode<tPos> Pair: {
				if (!aType.IsPair(out var FirstType, out var SecondType)) {
					return (mStd.cEmpty, aType);
				}
				
				var FirstCoverage = FirstType.SplitForPatternType(Pair.Tail);
				var SecondCoverage = SecondType.SplitForPatternType(Pair.Head);
				
				if (
					!FirstCoverage.Matched.IsSome(out var MatchedFirst) ||
					!SecondCoverage.Matched.IsSome(out var MatchedSecond)
				) {
					return (mStd.cEmpty, aType);
				}
				
				var MatchedPair = mVM_Type.Pair(MatchedFirst, MatchedSecond);
				
				if (
					(FirstCoverage.Remaining.IsSome(out var RemainingFirst) && RemainingFirst == FirstType) ||
					(SecondCoverage.Remaining.IsSome(out var RemainingSecond) && RemainingSecond == SecondType)
				) {
					return (MatchedPair, aType);
				}
				
				var Remaining = FirstCoverage.Remaining.Then(__ => mVM_Type.Pair(__, SecondType));
				if (SecondCoverage.Remaining.IsSome(out RemainingSecond)) {
					Remaining = mVM_Type.Union(
						Remaining,
						mVM_Type.Pair(MatchedFirst, RemainingSecond)
					);
				}
				
				return (MatchedPair, Remaining);
			}
			case mSPO_AST.tPrefixPatternNode<tPos> Prefix: {
				if (!aType.IsPrefix(Prefix.Prefix, out var InnerType)) {
					return (mStd.cEmpty, aType);
				}
				
				var Coverage = InnerType.SplitForPatternType(Prefix.Pattern);
				
				return (
					Coverage.Matched.Then(__ => mVM_Type.Prefix(Prefix.Prefix, __)),
					(
						Coverage.Remaining.IsSome(out var RemainingInner) && RemainingInner == InnerType
						? mMaybe.Some(aType)
						: Coverage.Remaining.Then(__ => mVM_Type.Prefix(Prefix.Prefix, __))
					)
				);
			}
			case mSPO_AST.tRecordPatternNode<tPos> Record: {
				if (!aType.IsRecord(out var Fields)) {
					return (
						aType.IsFree(out _, out _)
						? (aType, aType)
						: (mStd.cEmpty, aType)
					);
				}
				
				var MatchedFields = Fields;
				var Remaining = mMaybe.None<mVM_Type.tType>();
				
				foreach (var (Id, Pattern) in Record.Elements) {
					if (!Fields.TryGet(Id.Id).IsSome(out var FieldType)) {
						return (mStd.cEmpty, aType);
					}
					
					var Coverage = FieldType.SplitForPatternType(Pattern);
					if (!Coverage.Matched.IsSome(out var MatchedField)) {
						return (mStd.cEmpty, aType);
					}
					
					if (Coverage.Remaining.IsSome(out var RemainingField)) {
						Remaining = mVM_Type.Union(
							Remaining,
							mVM_Type.Record(
								MatchedFields.Set(Id.Id, RemainingField).ToStream(
								).Map(
									__ => (__.Key, __.Value)
								).ToArrayList(
								).ToArray()
							)
						);
					}
					
					MatchedFields = MatchedFields.Set(Id.Id, MatchedField);
				}
				
				return (
					mVM_Type.Record(
						MatchedFields.ToStream(
						).Map(
							__ => (__.Key, __.Value)
						).ToArrayList(
						).ToArray()
					),
					Remaining
				);
			}
			case mSPO_AST.tIntNode<tPos>: {
				return (
					aType.IsInt() ? (aType, aType) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tEmptyNode<tPos>: {
				return (
					aType.IsEmpty() ? (aType, mStd.cEmpty) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tTrueNode<tPos>: {
				return (
					aType.Kind is mVM_Type.tKind.True ? (aType, mStd.cEmpty) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tFalseNode<tPos>: {
				return (
					aType.Kind is mVM_Type.tKind.False ? (aType, mStd.cEmpty) :
					aType.IsFree(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			default: {
				return (aType, aType);
			}
		}
	}
	
	private static mResult.tResult<
		(
			mVM_Type.tType Type,
			mStream.tStream<(mVM_Type.tType Free, mVM_Type.tType Ref)> Mappings
		),
		(tPos Pos, tText ErrorText)
	>
	TryInferArgument<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aArgument,
		mVM_Type.tType aExpectedType,
		mStream.tStream<(mVM_Type.tType Free, mVM_Type.tType Ref)> aMappings,
		mStream.tStream<tScopeItem> aScope
	) {
		static tBool
		HasUnresolvedFreeType(
			mVM_Type.tType aType,
			mStream.tStream<mVM_Type.tType> aBoundTypes
		) {
			switch (aType.Kind) {
				case mVM_Type.tKind.Free: {
					return ReferenceEquals(aType, aType.Refs[0])
					? !aBoundTypes.Any(__ => ReferenceEquals(__, aType))
					: HasUnresolvedFreeType(aType.Refs[0], aBoundTypes);
				}
				case mVM_Type.tKind.Recursive:
				case mVM_Type.tKind.Generic:
				case mVM_Type.tKind.Interface: {
					return HasUnresolvedFreeType(
						aType.Refs[1],
						mStream.Stream(aType.Refs[0], aBoundTypes)
					);
				}
				case mVM_Type.tKind.Record: {
					return aType.Fields.ToStream().Any(
						__ => HasUnresolvedFreeType(__.Value, aBoundTypes)
					);
				}
				default: {
					return mStream.Stream(
						System.MemoryExtensions.AsSpan(aType.Refs)
					).Any(
						__ => HasUnresolvedFreeType(__, aBoundTypes)
					);
				}
			}
		}
		
		var MappedExpectedType = aExpectedType.ApplyMappings(aMappings);
		if (!aArgument.UpdateTypes(aScope).Match(out var ExpressionType, out var Error)) {
			var LambdaType = MappedExpectedType;
			while (LambdaType.IsGeneric(out _, out var InnerType)) {
				LambdaType = InnerType;
			}
			
			if (
				aArgument is not mSPO_AST.tLambdaNode<tPos> Lambda ||
				Lambda.Generic.IsSome(out _) ||
				!LambdaType.IsProc(out _, out var LambdaArgType, out var LambdaExpectedResultType) ||
				HasUnresolvedFreeType(LambdaArgType, mStd.cEmpty)
			) {
				return mResult.Fail(Error);
			}
			
			if (
				!UpdatePatternTypes(
					Lambda.Head,
					LambdaArgType,
					tTypeRelation.Sub,
					aScope
				).Match(out var LambdaArg, out Error) ||
				!Lambda.Body.TryInferArgument(
					LambdaExpectedResultType,
					aMappings,
					LambdaArg.Scope
				).Match(out var LambdaResult, out Error)
			) {
				return mResult.Fail(Error);
			}
			
			ExpressionType = mVM_Type.Proc(
				mVM_Type.Empty(),
				LambdaArg.Type,
				LambdaResult.Type
			);
			Lambda.TypeAnnotation = ExpressionType;
			aMappings = LambdaResult.Mappings;
		}
		
		return ExpressionType.IsSubType(
			MappedExpectedType,
			aMappings
		).Then(
			__ => (ExpressionType, __)
		).ModifyError(
			__ => (aArgument.Pos, __)
		);
	}
	
	private static mResult.tResult<
		(
			mVM_Type.tType Type,
			mStream.tStream<(mVM_Type.tType Free, mVM_Type.tType Ref)> Mappings
		),
		(tPos Pos, tText ErrorText)
	>
	TryInferArguments<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aArgument,
		mVM_Type.tType aExpectedType,
		mStream.tStream<tScopeItem> aScope
	) {
		var Arguments = mStream.Stream(aArgument);
		var ExpectedTypes = mStream.Stream(aExpectedType);
		
		if (aArgument is mSPO_AST.tTupleNode<tPos> Tuple) {
			var TupleTypes = mStream.Stream<mVM_Type.tType>();
			var Type = aExpectedType;
			while (Type.IsPair(out var TailType, out var Head)) {
				TupleTypes = mStream.Stream(Head, TupleTypes);
				Type = TailType;
			}
			
			if (Type.IsEmpty() && TupleTypes.Count() == Tuple.Items.Count()) {
				Arguments = Tuple.Items;
				ExpectedTypes = TupleTypes;
			}
		}
		
		var ArgumentCount = Arguments.Count();
		var Done = new tBool[ArgumentCount];
		var ArgumentTypes = new mVM_Type.tType[ArgumentCount];
		var Errors = new mMaybe.tMaybe<(tPos Pos, tText ErrorText)>[ArgumentCount];
		var Mappings = mStream.Stream<(mVM_Type.tType Free, mVM_Type.tType Ref)>();
		var Remaining = ArgumentCount;
		var ArgumentsWithTypes = mStream.ZipShort(Arguments, ExpectedTypes).MapWithIndex().Reverse();
		
		while (Remaining > 0) {
			var Progress = false;
			foreach (var (Index, ArgumentAndType) in ArgumentsWithTypes) {
				if (Done[Index]) {
					continue;
				}
				
				if (
					ArgumentAndType._1.TryInferArgument(
						ArgumentAndType._2,
						Mappings,
						aScope
					).Match(out var Inferred, out var Error)
				) {
					ArgumentTypes[Index] = Inferred.Type;
					Mappings = Inferred.Mappings;
					Done[Index] = true;
					Errors[Index] = mStd.cEmpty;
					Remaining -= 1;
					Progress = true;
				} else {
					Errors[Index] = Error;
				}
			}
			
			if (!Progress) {
				foreach (var Error in Errors) {
					if (Error.IsSome(out var Error_)) {
						return mResult.Fail(Error_);
					}
				}
				throw mError.Error("missing inference error");
			}
		}
		
		var ExpressionType = mVM_Type.Tuple(System.MemoryExtensions.AsSpan(ArgumentTypes));
		aArgument.TypeAnnotation = ExpressionType;
		return (ExpressionType, Mappings);
	}
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	UpdateTypes<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aNode,
		mStream.tStream<tScopeItem> aScope
	) => (
		aNode switch {
			mSPO_AST.tEmptyNode<tPos> => mVM_Type.Empty(),
			mSPO_AST.tTrueNode<tPos> => mVM_Type.True(),
			mSPO_AST.tFalseNode<tPos> => mVM_Type.False(),
			mSPO_AST.tIntNode<tPos> => mVM_Type.Int(),
			mSPO_AST.tTextNode<tPos> => mVM_Type.Text(),
			mSPO_AST.tCharNode<tPos> => mVM_Type.Char(),
			mSPO_AST.tIdNode<tPos> IdNode => (
				IdNode.TypeAnnotation.Match(
					Annotation => aScope.Where(
						__ => __.Id == IdNode.Id
					).TryFirst(
					).Then(
						__ => __.Type
					).ElseUse(
						Annotation
					),
					() => (
						IdNode.Id == "_=..."
					) ? (
						mStd.With(mVM_Type.Free(), FreeType => mVM_Type.Proc(FreeType, FreeType, mVM_Type.Empty()))
					) : (
						aScope.Where(
							__ => __.Id == IdNode.Id
						).TryFirst(
						).ElseFail(
							() => (IdNode.Pos, $"No Identifier '{IdNode.Id}' in scope")
						).Then(
							__ => __.Type
						)
					)
				)
			),
			mSPO_AST.tTypeNode<tPos> Type => (
				Type.AsVM_Type(aScope)
			),
			mSPO_AST.tTupleNode<tPos> Tuple => (
				Tuple.Items.Map(
					__ => __.UpdateTypes(aScope)
				).WhenAllThen(
					mVM_Type.Tuple
				)
			),
			mSPO_AST.tPairNode<tPos> Pair => (
				Pair.Tail.UpdateTypes(
					aScope
				).ThenTry(
					aTail => Pair.Head.UpdateTypes(aScope).Then(
						aHead => mVM_Type.Pair(aTail, aHead)
					)
				)
			),
			mSPO_AST.tPrefixNode<tPos> Prefix => (
				Prefix.Element.UpdateTypes(
					aScope
				).Then(
					__ => mVM_Type.Prefix(Prefix.Prefix, __)
				)
			),
			mSPO_AST.tRecordNode<tPos> Record => (
				Record.Elements.Map(
					__ => __.Value.UpdateTypes(
						aScope
					).Then(
						aType => mVM_Type.Prefix(__.Key.Id, aType)
					)
				).WhenAllThen(
					__ => __.Reduce(
						mVM_Type.Empty(),
						(aTail, aHead) => mVM_Type.Record(aTail, aHead)
					)
				)
			),
			mSPO_AST.tLambdaNode<tPos> Lambda => mStd.Call(
				() => {
					if (Lambda.Generic.IsSome(out var GenericPattern)) {
						// TODO: AI generated code has to be reviewed
						return UpdatePatternTypes(GenericPattern, mVM_Type.Type(), tTypeRelation.Equal, aScope).ThenTry(
							aGenTypeScope => UpdatePatternTypes(
								Lambda.Head,
								mStd.cEmpty,
								tTypeRelation.Sub,
								aGenTypeScope.Scope
							).ThenTry(
								aArgTypeScope => Lambda.Body.UpdateTypes(
									aArgTypeScope.Scope
								).Then(
									aResTypeScope => {
										var Proc = mVM_Type.Proc(mVM_Type.Empty(), aArgTypeScope.Type, aResTypeScope);
										
										if (aGenTypeScope.Type.IsType()) {
											var T = aGenTypeScope.Scope.Where(
												__ => __.Id == GenericPattern.TryGetId().AssertNotEmpty()
											).TryFirst(
											).AssertNotEmpty(
											).FreeType.AssertNotEmpty(
											);
											
											return mVM_Type.Generic(T, Proc);
										} else {
											return Proc;
										}
									}
								)
							)
						);
					}
					
					return UpdatePatternTypes(
						Lambda.Head,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aScope
					).ThenTry(
						aArg => Lambda.Body.UpdateTypes(
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
				UpdatePatternTypes(
					Method.Obj,
					mStd.cEmpty,
					tTypeRelation.Equal,
					aScope
				).ThenTry(
					aObj => UpdatePatternTypes(
						Method.Arg,
						mStd.cEmpty,
						tTypeRelation.Sub,
						aObj.Scope
					).ThenTry(
						aArg => Method.Body.UpdateTypes(
							aArg.Scope
						).Then(
							aResType => mVM_Type.Proc(aObj.Type, aArg.Type, aResType)
						)
					)
				)
			),
			mSPO_AST.tBlockNode<tPos> Block => (
				mStd.Call(
					() => {
						var Types = mStream.Stream<mVM_Type.tType>([]);
						var BlockScope = aScope;
						foreach (var Command in Block.Commands) {
							if (!UpdateCommandTypes(Command, BlockScope).Match(out BlockScope, out var Error)) {
								return mResult.Fail(Error);
							}
							
							if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
								var Type = ReturnIf.Result.TypeAnnotation.AssertNotEmpty();
								if (Types.All(__ => !Equals(__, Type))) {
									Types = mStream.Stream(Type, Types);
								}
							}
						}
						return mResult.OK(
							Types.Join((a1, a2) => mVM_Type.Set(a2, a1), mVM_Type.Empty())
						).WithErrorType<(tPos Pos, tText ErrorText)>(
						);
					}
				)
			),
			mSPO_AST.tCallNode<tPos> Call => Call.Func.UpdateTypes(
				aScope
			).ThenTry(
				aFuncType => mStd.Call(
					() => {
						var ProcType = aFuncType;
						while (ProcType.IsGeneric(out _, out var InnerType)) {
							ProcType = InnerType;
						}

						if (!ProcType.IsProc(out _, out var FormalArgType, out var FormalResultType)) {
							return mResult.Fail(
								(Call.Func.Pos, $"expect proc but is:\n{aFuncType.ToText()}")
							);
						}

						return Call.Arg.TryInferArguments(
							FormalArgType,
							aScope
						).Then(
							aArg => FormalResultType.ApplyMappings(aArg.Mappings)
						);
					}
				)
			),
			mSPO_AST.tIfMatchNode<tPos> IfMatch => (
				IfMatch.Expression.UpdateTypes(aScope).ThenTry(
					aTypePattern => mStd.Call(
						() => {
							var Remaining = mMaybe.Some(aTypePattern);
							var CaseTypes = mStream.Stream<mVM_Type.tType>();
							
							foreach (var Case in IfMatch.Cases) {
								var CaseInputType = Remaining.ElseUse(aTypePattern);
								
								// first try
								var CoverageCandidate = CaseInputType.SplitForPatternType(Case.Pattern);
								if (!CoverageCandidate.Matched.IsSome(out var CandidateType)) {
									return mResult.Fail(
										(
											Case.Pattern.Pos,
											$"pattern '{Case.Pattern.ToText()}' cannot match '{CaseInputType.ToText()}'"
										)
									);
								}
								
								// update
								if (
									!UpdatePatternTypes(
										Case.Pattern,
										CandidateType,
										tTypeRelation.Super,
										aScope
									).Match(out var Pattern, out var Error)
								) {
									return mResult.Fail(Error);
								}
								
								// final try
								var Coverage = CaseInputType.SplitForPatternType(Case.Pattern);
								if (!Coverage.Matched.IsSome(out _)) {
									return mResult.Fail(
										(
											Case.Pattern.Pos,
											$"pattern '{Case.Pattern.ToText()}' cannot match '{CaseInputType.ToText()}'"
										)
									);
								}
								
								// update
								if (!Case.Expression.UpdateTypes(Pattern.Scope).Match(out var CaseType, out Error)) {
									return mResult.Fail(Error);
								}
								
								CaseTypes = mStream.Stream(CaseType, CaseTypes);
								
								if (Remaining.IsSome(out _)) {
									Remaining = Coverage.Remaining;
								}
							}
							
							if (Remaining.IsSome(out var RemainingType)) {
								return mResult.Fail(
									(
										IfMatch.Pos,
										$"""
										non-exhaustive match; unmatched type:
										{RemainingType.ToText()}
										"""
									)
								);
							}
							
							return mResult.OK(
								CaseTypes.Reduce(
									(mVM_Type.tType)null!,
									(aTypes, aType) => (
										aTypes is null || aTypes == aType
										? aType
										: mVM_Type.Set(aType, aTypes)
									)
								)
							).WithErrorType<(tPos Pos, tText ErrorText)>();
						}
					)
				)
			),
			mSPO_AST.tIsNode<tPos> Is => (
				Is.Expression.UpdateTypes(
					aScope
				).ThenTry(
					aValueType => UpdatePatternTypes(
						Is.Pattern,
						aValueType,
						tTypeRelation.Super,
						aScope
					)
				).Then(
					_ => mVM_Type.Bool()
				)
			),
			mSPO_AST.tVarToValNode<tPos> VarToVal => (
				VarToVal.Obj.UpdateTypes(
					aScope
				).ThenTry(
					__ => (
						__.IsVar(out var ValType)
						? mResult.OK(ValType).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail((VarToVal.Pos, $"the type '{__}' in not from type '[§VAR ...]'"))
					)
				)
			),
			mSPO_AST.tIfNode<tPos> If => (
				If.Cases.Map(
					aCase => aCase.Cond.UpdateTypes(
						aScope
					).FailIfNot(
						__ => __.IsSubType(
							mVM_Type.Bool(),
							mStd.cEmpty
						).Match(out _, out _),
						__ => (aCase.Cond.Pos, $"condition '{aCase.Cond.ToText()}' has to be [§TRUE | §FALSE] but is of type:\n  {__.ToText()}")
					).ThenTry(
						_ => aCase.Result.UpdateTypes(aScope)
					)
				).WhenAllThen(
					a => {
						var X = a.Reduce(
							mStream.Stream<mVM_Type.tType>([]),
							(aList, aItem) => aList.All(__ => __ != aItem)
							? mStream.Stream(aItem, aList)
							: aList
						);
						
						return X.Count() switch {
							0 => mVM_Type.Empty(),
							1 => X.TryFirst().AssertNotEmpty(),
							_ => X.Reduce(
								mVM_Type.Empty(),
								(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
							)
						};
					}
				)
			),
			mSPO_AST.tPipeToLeftNode<tPos> Pipe => throw mError.Error($"'{aNode.GetType().Name}' should be desugared at this point!"),
			_ => throw mError.Error("not implemented: " + aNode.GetType().Name),
		}
	).ThenDo(
		__ => { aNode.TypeAnnotation = __; }
	);
	
	public static mResult.tResult<(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope), (tPos Pos, tText ErrorText)>
	UpdatePatternTypes<tPos>(
		mSPO_AST.tPatternNode<tPos> aPattern,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		tTypeRelation aTypeRelation,
		mStream.tStream<tScopeItem> aScope
	) {
		mResult.tResult<(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope), (tPos Pos, tText ErrorText)> Result;
		switch (aPattern) {
			case mSPO_AST.tTypedPatternNode<tPos> Pattern: {
				Result = Pattern.TypeExpression.Match(
					__ => mStd.Call(
						() => __.AsVM_Type(aScope).ThenTry(
							aType => UpdatePatternTypes(
								Pattern.Pattern,
								aType,
								aTypeRelation,
								aScope
							)
						)
					),
					() => UpdatePatternTypes(Pattern.Pattern, aType, aTypeRelation, aScope)
				);
				break;
			}
			case mSPO_AST.tFreeIdPatternNode<tPos> FreePatternId: {
				Result = aType.Then(
					__ => (
						__.IsType()
						? (
							__,
							mStream.Stream(
								ScopeItem(
									FreePatternId.Id,
									__,
									mVM_Type.Free(FreePatternId.Id)
								),
								aScope
							)
						)
						: (
							__,
							mStream.Stream(
								ScopeItem(
									FreePatternId.Id,
									__
								),
								aScope
							)
						)
					)
				).ElseFail(
					() => (FreePatternId.Pos, $"missing type for '{FreePatternId.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tVarPatternNode<tPos> VarPattern: {
				Result = aType.Then(
					__ => mStd.Call(
						() => {
							var NewTypeScope = aScope.Where(
								__ => __.Id == VarPattern.Id
							).TryFirst(
							).Match(
								() => {
									var NewType = __.IsVar(out _)
										? __
										: mVM_Type.Var(__);
									return (Type: NewType, Scope: mStream.Stream(ScopeItem(VarPattern.Id, NewType), aScope));
								},
								aScopeItem => {
									var ExistingType = aScopeItem.Type;
									mAssert.IsTrue(
										ExistingType.IsVar(out _),
										() => $"'{ExistingType.ToText()}' is not a var type"
									);
									return (Type: ExistingType, Scope: aScope);
								}
							);
							
							VarPattern.TypeAnnotation = NewTypeScope.Type;
							
							return NewTypeScope;
						}
					)
				).ElseFail(
					() => (VarPattern.Pos, $"missing type for '{VarPattern.Id}'")
				);
				break;
			}
			
			case mSPO_AST.tIgnorePatternNode<tPos> IgnorePattern: {
				Result = aType.Then(__ => (__, aScope)).ElseFail(() => (IgnorePattern.Pos, "unknown type"));
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tPos> PrefixPattern: {
				var SubType = mMaybe.None<mVM_Type.tType>();
				if (aType.IsSome(out var Type_)) {
					while (Type_.IsSet(out var Type, out var Types)) {
						if (Type.IsPrefix(out var Prefix, out var SubType_) && Prefix == PrefixPattern.Prefix) {
							SubType = SubType_;
							Type_ = Type;
							break;
						}
						Type_ = Types;
					}
					{
						mAssert.IsTrue(Type_.IsPrefix(out var Prefix, out var SubType__));
						SubType = SubType__;
						mAssert.AreEquals(Prefix, PrefixPattern.Prefix);
					}
				}
				Result = UpdatePatternTypes(PrefixPattern.Pattern, SubType, aTypeRelation, aScope).Then(
					__ => (mVM_Type.Prefix(PrefixPattern.Prefix, __.Type), __.Scope)
				);
				break;
			}
			case mSPO_AST.tTuplePatternNode<tPos> TuplePattern: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				var NewScope = aScope;
				if (!aType.IsSome(out var Type)) {
					foreach (var Pattern in TuplePattern.Items) {
						if (!UpdatePatternTypes(Pattern, mStd.cEmpty, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
							return mResult.Fail(Error);
						}
						
						NewScope = TS.Scope;
						Types = mStream.Stream(TS.Type, Types);
					}
				} else {
					var WalkType = Type;
					var TypeStack = mStream.Stream<mVM_Type.tType>();
					while (WalkType.IsPair(out var Tail_, out var Head_)) {
						TypeStack = mStream.Stream(Head_, TypeStack);
						WalkType = Tail_;
					}
					if (TypeStack.IsEmpty()) {
						// TODO: this part looks wrong. i expect TypeStack is never empty.
						//   and why should i use aType for each item in the list?
						foreach (var Pattern in TuplePattern.Items) {
							if (!UpdatePatternTypes(Pattern, Type, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							NewScope = TS.Scope;
						}
					} else {
						if (!WalkType.IsEmpty() || TypeStack.Count() != TuplePattern.Items.Count()) {
							return mResult.Fail((TuplePattern.Pos, $"can't unify '{TuplePattern.ToText()} and '{Type.ToText()}'"));
						}
						
						foreach (var (Pattern, ItemType) in mStream.ZipShort(TuplePattern.Items, TypeStack)) {
							if (!UpdatePatternTypes(Pattern, ItemType, aTypeRelation, NewScope).Match(out var TS, out var Error)) {
								return mResult.Fail(Error);
							}
							
							Types = mStream.Stream(TS.Type, Types);
							NewScope = TS.Scope;
						}
					}
				}
				Result = (mVM_Type.Tuple(Types.Reverse()), NewScope);
				break;
			}
			case mSPO_AST.tPairPatternNode<tPos> PairPattern: {
				var TailType = mMaybe.None<mVM_Type.tType>();
				var HeadType = mMaybe.None<mVM_Type.tType>();
				
				if (aType.IsSome(out var Type)) {
					if (!Type.TryProjectPair(out var Tail, out var Head)) {
						return mResult.Fail(
							(
								PairPattern.Pos,
								$"cant unify '{PairPattern.ToText()}' and '{Type.ToText()}'"
							)
						);
					}
					
					TailType = Tail;
					HeadType = Head;
				}
				
				if (
					!UpdatePatternTypes(
						PairPattern.Tail,
						TailType,
						aTypeRelation,
						aScope
					).Match(
						out var TailRes,
						out var Error
					) ||
					!UpdatePatternTypes(
						PairPattern.Head,
						HeadType,
						aTypeRelation,
						TailRes.Scope
					).Match(
						out var HeadRes,
						out Error
					)
				) {
					return mResult.Fail(Error);
				}
				
				Result = (mVM_Type.Pair(TailRes.Type, HeadRes.Type), HeadRes.Scope);
				break;
			}
			case mSPO_AST.tRecordPatternNode<tPos> RecordPattern: {
				Result = (mVM_Type.Empty(), aScope);
				foreach (var Item in RecordPattern.Elements) {
					var Type = aType.IsSome(out var RecordType)
					? RecordType.GetFieldType(Item.Id.Id)
					: mMaybe.None<mVM_Type.tType>();
					
					Result = Result.ThenTry(
						a1 => UpdatePatternTypes(
							Item.Pattern,
							Type,
							aTypeRelation,
							a1.Scope
						).Then(
							a2 => (
								mVM_Type.Record(
									a1.Type,
									mVM_Type.Prefix(Item.Id.Id, a2.Type)
								),
								a2.Scope
							)
						)
					);
				}
				break;
			}
			case mSPO_AST.tGuardPatternNode<tPos> GuardPattern: {
				if (
					!UpdatePatternTypes(
						GuardPattern.Pattern,
						aType,
						tTypeRelation.Super,
						aScope
					).Match(
						out var Res,
						out var Error
					) ||
					!GuardPattern.Guard.UpdateTypes(
						Res.Scope
					).Match(
						out var BoolRes,
						out Error
					)
				) {
					return mResult.Fail(Error);
				}
				
				if (
					!BoolRes.IsSubType(
						mVM_Type.Bool(),
						mStd.cEmpty
					).Match(out _, out _)
				) {
					return mResult.Fail(
						(
							GuardPattern.Pos,
							$"""
							return type has to be boolean but is:
								{BoolRes.ToText()}
							"""
						)
					);
				}
				
				Result = Res;
				// TODO: Result = mVM_Type.Guard(Result, ...);
				break;
			}
			case mSPO_AST.tIdNode<tPos> Id: {
				var Type = aType.IsSome(out var T) ? T : mVM_Type.Free();
				
				Result = (
					Type,
					mStream.Stream(ScopeItem(Id.Id, Type), aScope)
				);
				break;
			}
			case mSPO_AST.tExpressionNode<tPos> Expression: {
				Result = Expression.UpdateTypes(aScope).Then(__ => (__, aScope));
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aPattern.GetType().Name);
			}
		}
		return Result.ThenDo(__ => { aPattern.TypeAnnotation = __.Type; });
	}
	
	public static mResult.tResult<mStream.tStream<tScopeItem>, (tPos Pos, tText ErrorText)>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		mStream.tStream<tScopeItem> aScope
	) => aMethodCall.Method.UpdateTypes(aScope).ThenTry(
		aMethodType => mStd.Call(
			() => {
				var ProcType = aMethodType;
				while (ProcType.IsGeneric(out _, out var InnerType)) {
					ProcType = InnerType;
				}

				if (!ProcType.IsProc(out _, out var MethArgType, out var MethResType)) {
					return mResult.Fail(
						(aMethodCall.Argument.Pos, $"'{aMethodType.ToText()}' is not a Proc")
					);
				}

				return aMethodCall.Argument.TryInferArguments(
					MethArgType,
					aScope
				).ThenTry(
					aArgument => (
						!aMethodCall.Result.IsSome(out var Result)
						? aScope
						: UpdatePatternTypes(
							Result,
							MethResType.ApplyMappings(aArgument.Mappings),
							tTypeRelation.Sub,
							aScope
						).Then(__ => __.Scope)
					)
				);
			}
		)
	);
	
	public static mResult.tResult<mStream.tStream<tScopeItem>, (tPos Pos, tText ErrorText)>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<tScopeItem> aScope
	) {
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return Def.Src.UpdateTypes(aScope).ThenTry(
					aSrcType => UpdatePatternTypes(
						Def.Des,
						aSrcType,
						tTypeRelation.Equal,
						aScope
					).ThenTry(
						aType => aSrcType.IsSubType(
							aType.Type,
							mStd.cEmpty
						).Then(
							_ => aType.Scope
						).ModifyError(
							__ => (Def.Src.Pos, __)
						)
					)
				);
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return ReturnIf.Condition.UpdateTypes(
					aScope
				).FailIfNot(
					aConditionType => aConditionType.IsSubType(
						mVM_Type.Bool(),
						mStd.cEmpty
					).Match(out _, out _),
					__ => (ReturnIf.Pos, $"{__.ToText()} != [§TRUE | §FALSE]")
				).ThenTry(
					_ => ReturnIf.Result.UpdateTypes(aScope)
				).Then(
					_ => aScope
				);
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				return DefVar.Expression.UpdateTypes(aScope).ThenTry(
					aValueType => DefVar.MethodCalls.Reduce(
						mResult.OK(aScope).WithErrorType<(tPos Pos, tText ErrorText)>(),
						(Scope, MethodCall) => Scope.ThenTry(a => UpdateMethodCallTypes(MethodCall, a))
					).Then(
						aScope => {
							var Type = mVM_Type.Var(aValueType);
							var NewScope = mStream.Stream(ScopeItem(DefVar.Id.Id, Type), aScope);
							DefVar.Id.UpdateTypes(NewScope);
							return NewScope;
						}
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				var NewScope = aScope;
				foreach (var Item in RecLambdas.List) {
					var HeadScope = NewScope;
					if (Item.Lambda.Generic.IsSome(out var GenericPattern)) {
						if (!UpdatePatternTypes(GenericPattern, mVM_Type.Type(), tTypeRelation.Equal, HeadScope).Match(out var GenScope, out var GenError)) {
							return mResult.Fail(GenError);
						}
						HeadScope = GenScope.Scope;
					}
					
					if (!UpdatePatternTypes(Item.Lambda.Head, mStd.cEmpty, tTypeRelation.Equal, HeadScope).Match(out var Result, out var Error)) {
						return mResult.Fail(Error);
					}
					
					NewScope = mStream.Stream(
						ScopeItem(
							Item.Id.Id,
							mVM_Type.Proc(
								mVM_Type.Free("__" + Item.Id.Id + "_Obj__"),
								Result.Type,
								mVM_Type.Free("__" + Item.Id.Id + "_Res__")
							)
						),
						NewScope
					);
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!NewScope.Where(
							__ => __.Id == Item.Id.Id
						).TryFirst(
						).ElseFail(
							() => (Item.Pos, $"unknown Id '{Item.Id.Id}'")
						).Then(
							__ => __.Type
						).ThenTry(
							DesType => Item.Lambda.UpdateTypes(NewScope).ThenDo(
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
						!Item.Lambda.UpdateTypes(NewScope).Then(
							__ => mStream.Stream(
								ScopeItem(
									Item.Id.Id,
									__
								),
								NewScope
							)
						).Match(out NewScope, out var Error)
					) {
						return mResult.Fail(Error);
					}
				}
				
				return NewScope;
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return MethodCalls.Object.UpdateTypes(aScope).ThenTry(
					aObjType => MethodCalls.MethodCalls.Reduce(
						mResult.OK(aScope).WithErrorType<(tPos Pos, tText ErrorText)>(),
						(Scope, MethodCall) => Scope.ThenTry(__ => UpdateMethodCallTypes(MethodCall, __))
					)
				);
			}
			default: {
				throw mError.Error("not implemented: " + aCommand.GetType().Name);
			}
		}
	}
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tTypedPatternNode<tPos> aPattern
	) => TryGetId(aPattern.Pattern);
	
	public static mMaybe.tMaybe<tText>
	TryGetId<tPos>(
		this mSPO_AST.tPatternNode<tPos> aPattern
	) => aPattern switch {
		mSPO_AST.tFreeIdPatternNode<tPos> Free => Free.Id,
		mSPO_AST.tIdNode<tPos> IdNode => IdNode.Id,
		mSPO_AST.tTypedPatternNode<tPos> Pattern => TryGetId(Pattern),
		_ => mStd.cEmpty,
	};
	
	public static mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)>
	AsVM_Type<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<tScopeItem> aScope
	) {
		mResult.tResult<mVM_Type.tType, (tPos Pos, tText ErrorText)> Result;
		
		switch (aExpression) {
			case mSPO_AST.tEmptyTypeNode<tPos>: {
				Result = mVM_Type.Empty();
				break;
			}
			case mSPO_AST.tTrueNode<tPos>: {
				Result = mVM_Type.True();
				break;
			}
			case mSPO_AST.tFalseNode<tPos>: {
				Result = mVM_Type.False();
				break;
			}
			case mSPO_AST.tIntTypeNode<tPos>: {
				Result = mVM_Type.Int();
				break;
			}
			case mSPO_AST.tCharTypeNode<tPos>: {
				Result = mVM_Type.Prefix("_Char...", mVM_Type.Int());
				break;
			}
			case mSPO_AST.tTextTypeNode<tPos>: {
				Result = mVM_Type.Text();
				break;
			}
			case mSPO_AST.tTypeTypeNode<tPos>: {
				Result = mVM_Type.Type();
				break;
			}
			case mSPO_AST.tAnyTypeNode<tPos>: {
				Result = mVM_Type.Any();
				break;
			}
			case mSPO_AST.tTupleTypeNode<tPos> TupleType: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				foreach (var Expression in TupleType.ItemTypes.Reverse()) {
					if (Expression.AsVM_Type(aScope).Match(out var Type, out var Error)) {
						Types = mStream.Stream(Type, Types);
					} else {
						return mResult.Fail(Error);
					}
				}
				Result = mVM_Type.Tuple(Types);
				break;
			}
			case mSPO_AST.tPairTypeNode<tPos> PairType: {
				Result = PairType.TailType.AsVM_Type(
					aScope
				).ThenTry(
					aTail => PairType.HeadType.AsVM_Type(
						aScope
					).Then(
						aHead => mVM_Type.Pair(aTail, aHead)
					)
				);
				break;
			}
			case mSPO_AST.tRecordTypeNode<tPos> RecordType: {
				Result = RecordType.Elements.Reduce(
					mResult.OK(
						mStream.Stream<(tText Key, mVM_Type.tType Type)>()
					).WithErrorType<(tPos Pos, tText ErrorText)>(
					),
					(aResult, aElement) => aResult.ThenTry(
						aStream => aElement.Type.AsVM_Type(aScope).Then(
							aType => mStream.Stream((Key: aElement.Key.Id, Type: aType), aStream)
						)
					)
				).Then(__ => mVM_Type.Record(__.ToArrayList().ToArray()));
				break;
			}
			case mSPO_AST.tIdNode<tPos> IdNode: {
				Result = aScope.Where(
					__ => __.Id == IdNode.Id
				).TryFirst(
				).ElseFail(
					() => (IdNode.Pos, $"unknown type of Identifier '{IdNode.Id}'")
				).ThenTry(
					__ => __.Type.IsType()
					? __.FreeType.ElseFail(() => (IdNode.Pos, "impossible ???"))
					: __.Type
				);
				break;
			}
			case mSPO_AST.tProcTypeNode<tPos> ProcType: {
				Result = ProcType.ObjType.AsVM_Type(aScope).ThenTry(
					aObjType => ProcType.ArgType.AsVM_Type(aScope).ThenTry(
						aArgType => ProcType.ResType.AsVM_Type(aScope).Then(
							aResType => mVM_Type.Proc(aObjType, aArgType, aResType)
						)
					)
				);
				break;
			}
			case mSPO_AST.tRecursiveTypeNode<tPos> RecursiveType: {
				var Name = RecursiveType.HeadType.Id;
				var RecursiveVar = mVM_Type.Free(Name);
				
				Result = RecursiveType.BodyType.UpdateTypes(
					mStream.Stream(
						ScopeItem(Name, RecursiveVar),
						aScope
					)
				).Then(
					_ => mVM_Type.Recursive(RecursiveVar, _)
				);
				break;
			}
			case mSPO_AST.tSetTypeNode<tPos> SetType: {
				Result = SetType.Expressions.Map(
					__ => __.AsVM_Type(aScope)
				).WhenAllThen(
					__ => __.Reduce(
						mStream.Stream<mVM_Type.tType>([]),
						(aList, aItem) => aList.All(__ => __ != aItem) ? mStream.Stream(aItem, aList) : aList
					).Match(
						() => mVM_Type.Empty(),
						(aHead1, aTail1) => aTail1.Match(
							() => aHead1,
							(aHead2, aTail2) => aTail2.Reduce(
								mVM_Type.Set(aHead1, aHead2),
								(aSet, aItem) => mVM_Type.Set(aItem, aSet)
							)
						)
					)
				);
				break;
			}
			case mSPO_AST.tPrefixTypeNode<tPos> PrefixType: {
				Result = PrefixType.Expressions.Map(
					__ => __.AsVM_Type(aScope)
				).WhenAllThen(
					__ => mVM_Type.Prefix(PrefixType.Prefix.Id, mVM_Type.Tuple(__))
				);
				break;
			}
			case mSPO_AST.tVarTypeNode<tPos> VarType: {
				Result = VarType.Type.AsVM_Type(aScope).Then(
					mVM_Type.Var
				);
				break;
			}
			case mSPO_AST.tGenericTypeNode<tPos> GenericType: {
				var Name = GenericType.HeadType.Id;
				var GenericVar = mVM_Type.Free(Name);
				
				Result = GenericType.BodyType.UpdateTypes(
					mStream.Stream(
						ScopeItem(Name, GenericVar),
						aScope
					)
				).Then(
					__ => mVM_Type.Generic(GenericVar, __)
				);
				break;
			}
			case mSPO_AST.tGenericApplyTypeNode<tPos> GenericApplyType: {
				Result = GenericApplyType.GenericType.AsVM_Type(aScope).ThenTry(
					aGenericType => GenericApplyType.ArgType.AsVM_Type(aScope).ThenTry(
						aArgType => aGenericType.IsGeneric(out var Head, out var Body)
						? mResult.OK(Body.Substitute(Head, aArgType)).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail((GenericApplyType.GenericType.Pos, $"expected generic type but '{GenericApplyType.GenericType.ToText()}'"))
					)
				);
				break;
			}
			default: {
				throw mError.Error("not implemented: " + aExpression.GetType().Name);
			}
		}
		return Result.ThenDo(
			__ => { aExpression.TypeAnnotation = __; }
		);
	}
}
