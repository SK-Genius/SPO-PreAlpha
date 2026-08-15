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

using tInferenceState = mStream.tStream<(mVM_Type.tType Variable, mMaybe.tMaybe<mVM_Type.tType> Solution)>;

public static class
mSPO_AST_Types {
	public struct tScopeItem {
		public System.String Id;
		public mMaybe.tMaybe<mVM_Type.tType> TypeValue;
		public mVM_Type.tType Type;
	}
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType,
		mMaybe.tMaybe<mVM_Type.tType> aTypeValue
	) => new () {
		Id = aId,
		TypeValue = aTypeValue,
		Type = aType,
	};
	
	public static tScopeItem
	ScopeItem(
		tText aId,
		mVM_Type.tType aType
	) => new () {
		Id = aId,
		TypeValue = mStd.cEmpty,
		Type = aType,
	};
	
	public static mResult.tResult<mMaybe.tMaybe<mVM_Type.tType>, (tPos Pos, tText ErrorText)>
	TryGetTypeValue<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aExpression,
		mStream.tStream<tScopeItem> aScope
	) => aExpression switch {
		mSPO_AST.tIdNode<tPos> Id => aScope.Where(
			__ => __.Id == Id.Id
		).TryFirst(
		).Then(
			__ => __.TypeValue
		).ElseUse(
			mMaybe.None<mVM_Type.tType>()
		),
		mSPO_AST.tTypeNode<tPos> Type => Type.AsVM_Type(aScope).Then(mMaybe.Some),
		_ => mResult.OK(mMaybe.None<mVM_Type.tType>()).WithErrorType<(tPos Pos, tText ErrorText)>(),
	};
	
	public static (
		mMaybe.tMaybe<mVM_Type.tType> Matched,
		mMaybe.tMaybe<mVM_Type.tType> Remaining
	)
	SplitForPatternType<tPos>(
		this mVM_Type.tType aType,
		mSPO_AST.tPatternNode<tPos> aPattern
	) => SplitForPatternType(aType, aPattern, mStream.Stream<mVM_Type.tType>());
	
	private static (
		mMaybe.tMaybe<mVM_Type.tType> Matched,
		mMaybe.tMaybe<mVM_Type.tType> Remaining
	)
	SplitForPatternType<tPos>(
		this mVM_Type.tType aType,
		mSPO_AST.tPatternNode<tPos> aPattern,
		mStream.tStream<mVM_Type.tType> aExpandedRecursiveTypes
	) {
		switch (aPattern) {
			case mSPO_AST.tGuardPatternNode<tPos> Guard: {
				return (
					aType.SplitForPatternType(Guard.Pattern, aExpandedRecursiveTypes).Matched,
					aType
				);
			}
			case mSPO_AST.tSigPatternNode<tPos> Sig: {
				var Split = aType.SplitBy(
					__ => __.IsSig(out _, out _, out _) &&
						(
							!Sig.Contract.TypeAnnotation.IsSome(out var Contract) ||
							__.IsSubType(Contract).Match(out _, out _)
						)
				);
				return (
					Split.Matched,
					Sig.Head is mSPO_AST.tTypePatternNode<tPos> ? aType : Split.Remainder
				);
			}
			case mSPO_AST.tFreeIdPatternNode<tPos>:
			case mSPO_AST.tIgnorePatternNode<tPos>:
			case mSPO_AST.tIdNode<tPos>:
			case mSPO_AST.tVarPatternNode<tPos>: {
				return (aType, mStd.cEmpty);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsAny():
			case mSPO_AST.tPatternNode<tPos> when aType.IsTypeVariable(out _, out _): {
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
				if (aExpandedRecursiveTypes.Any(__ => mStd.RefEq(__, aType))) {
					return (aType, aType);
				}
				var Expanded = RecursiveBody.Substitute(RecursiveHead, aType);
				
				return (
					mStd.RefEq(Expanded, aType)
					? (aType, aType)
					: Expanded.SplitForPatternType(aPattern, mStream.Stream(aType, aExpandedRecursiveTypes))
				);
			}
			case mSPO_AST.tPatternNode<tPos> when aType.IsSet(out var Type1, out var Type2): {
				var Coverage1 = Type1.SplitForPatternType(aPattern, aExpandedRecursiveTypes);
				var Coverage2 = Type2.SplitForPatternType(aPattern, aExpandedRecursiveTypes);
				
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
					if (aType.IsSubType(MatchType).Match(out _, out _)) {
						return (aType, mStd.cEmpty);
					}
					
					return (
						MatchType.IsSubType(aType).Match(out _, out _)
						? (MatchType, aType)
						: (mStd.cEmpty, aType)
					);
				}
				return aType.SplitForPatternType(Typed.Pattern, aExpandedRecursiveTypes);
			}
			case mSPO_AST.tPairPatternNode<tPos> Pair: {
				if (!aType.IsPair(out var FirstType, out var SecondType)) {
					return (mStd.cEmpty, aType);
				}
				
				var FirstCoverage = FirstType.SplitForPatternType(Pair.Tail, aExpandedRecursiveTypes);
				var SecondCoverage = SecondType.SplitForPatternType(Pair.Head, aExpandedRecursiveTypes);
				
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
				
				var Coverage = InnerType.SplitForPatternType(Prefix.Pattern, aExpandedRecursiveTypes);
				
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
						aType.IsTypeVariable(out _, out _)
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
					
					var Coverage = FieldType.SplitForPatternType(Pattern, aExpandedRecursiveTypes);
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
					aType.IsTypeVariable(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tEmptyNode<tPos>: {
				return (
					aType.IsEmpty() ? (aType, mStd.cEmpty) :
					aType.IsTypeVariable(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tTrueNode<tPos>: {
				return (
					aType.Kind is mVM_Type.tKind.True ? (aType, mStd.cEmpty) :
					aType.IsTypeVariable(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			case mSPO_AST.tFalseNode<tPos>: {
				return (
					aType.Kind is mVM_Type.tKind.False ? (aType, mStd.cEmpty) :
					aType.IsTypeVariable(out _, out _) ? (aType, aType) :
					(mStd.cEmpty, aType)
				);
			}
			default: {
				return (aType, aType);
			}
		}
	}
	
	private static mVM_Type.tType
	GenericPatternType<tPos>(
		mSPO_AST.tPatternNode<tPos> aPattern
	) => (
		aPattern is mSPO_AST.tTuplePatternNode<tPos> Tuple
		? mVM_Type.Tuple(Tuple.Items.Map(_ => mVM_Type.Type()))
		: mVM_Type.Type()
	);
	
	private static tInferenceState
	RegisterInferenceVariables(
		this tInferenceState aInferenceState,
		mVM_Type.tType aType,
		mStream.tStream<tScopeItem> aScope
	) => RegisterInferenceVariables(
		aInferenceState,
		aType,
		aScope,
		mStream.Stream<mVM_Type.tType>()
	);
	
	private static tInferenceState
	RegisterInferenceVariables(
		this tInferenceState aInferenceState,
		mVM_Type.tType aType,
		mStream.tStream<tScopeItem> aScope,
		mStream.tStream<mVM_Type.tType> aBound
	) {
		if (aType.Kind is mVM_Type.tKind.TypeVariable) {
			return (
				aBound.Any(__ => mStd.RefEq(__, aType)) ||
				aScope.Any(
					__ => __.Type.ContainsVariable(aType) ||
						__.TypeValue.IsSome(out var TypeValue) && TypeValue.ContainsVariable(aType)
				)
				? aInferenceState
				: aInferenceState.AddVar(aType)
			);
		} else if (aType.Kind is mVM_Type.tKind.Record) {
			return aType.Fields.ToStream().Reduce(
				aInferenceState,
				(Inference, Field) => Inference.RegisterInferenceVariables(
					Field.Value,
					aScope,
					aBound
				)
			);
		} else if (
			aType.Kind is mVM_Type.tKind.Recursive or
			mVM_Type.tKind.Generic or
			mVM_Type.tKind.Interface or
			mVM_Type.tKind.Sig
		) {
			var Bound = mStream.Stream(aType.Refs[0], aBound);
			for (var I = 1; I < aType.Refs.Length; I += 1) {
				aInferenceState = aInferenceState.RegisterInferenceVariables(aType.Refs[I], aScope, Bound);
			}
			return aInferenceState;
		} else {
			foreach (var Child in aType.Refs) {
				aInferenceState = aInferenceState.RegisterInferenceVariables(Child, aScope, aBound);
			}
			return aInferenceState;
		}
	}

	private static mResult.tResult<
		(mVM_Type.tType Type, tInferenceState Mappings),
		(tPos Pos, tText ErrorText)
	>
	InferCall<tPos>(
		mVM_Type.tType aCallableType,
		mSPO_AST.tExpressionNode<tPos> aArgument,
		mStream.tStream<tScopeItem> aScope,
		tInferenceState aMappings,
		(tPos Pos, tText ErrorText) aNotProcError
	) {
		var LocalVariables = mStream.Stream<mVM_Type.tType>();
		while (aCallableType.IsGeneric(out var Binder, out var Body)) {
			var Variable = mVM_Type.TypeVariable(Binder.Id, Binder.KindOf());
			LocalVariables = mStream.Stream(Variable, LocalVariables);
			aMappings = aMappings.AddVar(Variable);
			aCallableType = Body.Substitute(Binder, Variable);
		}
		if (!aCallableType.IsProc(out _, out var ArgumentType, out var ResultType)) {
			return mResult.Fail(aNotProcError);
		}
		return aArgument.TryInferArguments(
			ArgumentType,
			aScope,
			aMappings
		).ThenTry(
			aInferred => {
				var Result = ResultType.ApplyInference(aInferred.Mappings);
				foreach (var Variable in LocalVariables) {
					if (
						Result.ContainsVariable(Variable) ||
						aInferred.Mappings.Any(
							__ => !mStd.RefEq(__.Variable, Variable) &&
								__.Solution.IsSome(out var Solution) &&
								Solution.ApplyInference(aInferred.Mappings).ContainsVariable(Variable)
						)
					) {
						return mResult.Fail((aArgument.Pos, "unresolved call-local type variable escapes the call"));
					}
				}
				return mResult.OK(
					(
						Type: Result,
						Mappings: aInferred.Mappings.Where(
							Entry => !LocalVariables.Any(
								Variable => mStd.RefEq(Variable, Entry.Variable)
							)
						).Map(
							Entry => (
								Entry.Variable,
								Entry.Solution.Then(
									Solution => Solution.ApplyInference(aInferred.Mappings)
								)
							)
						)
					)
				).WithErrorType<(tPos Pos, tText ErrorText)>();
			}
		);
	}
	
	private static mResult.tResult<
		(mVM_Type.tType Type, tInferenceState Mappings),
		(tPos Pos, tText ErrorText)
	>
	TryInferArgument<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aArgument,
		mVM_Type.tType aExpectedType,
		tInferenceState aMappings,
		mStream.tStream<tScopeItem> aScope
	) {
		static mResult.tResult<
			(mVM_Type.tType Type, tInferenceState Mappings),
			(tPos Pos, tText ErrorText)
		>
		RefineLambda(
			mSPO_AST.tLambdaNode<tPos> aLambda,
			mVM_Type.tType aExpected,
			tInferenceState aInferenceState,
			mStream.tStream<tScopeItem> aLambdaScope
		) {
			var Expected = aExpected.ApplyInference(aInferenceState);
			if (!Expected.IsProc(out var ExpectedObject, out var ExpectedArgument, out var ExpectedResult)) {
				return mResult.Fail((aLambda.Pos, $"expected Proc but is:\n{Expected.ToText()}"));
			}
			
			var Empty = mVM_Type.Empty();
			if (!Empty.IsSubTypeOf(
					ExpectedObject,
					aInferenceState
				).Match(out var Inference, out var Error)
			) {
				return mResult.Fail((aLambda.Pos, Error));
			}
			if (!ExpectedObject.ApplyInference(Inference).IsSubTypeOf(
					Empty,
					Inference
				).Match(out Inference, out Error)
			) {
				return mResult.Fail((aLambda.Pos, Error));
			}
			if (!UpdatePatternTypes(
					aLambda.Head,
					ExpectedArgument.ApplyInference(Inference),
					aLambdaScope
				).Match(out var Argument, out var PatternError)
			) {
				return mResult.Fail(PatternError);
			}
			
			mResult.tResult<
				(mVM_Type.tType Type, tInferenceState Mappings),
				(tPos Pos, tText ErrorText)
			> RefinedBody;
			if (
				aLambda.Body is mSPO_AST.tLambdaNode<tPos> BodyLambda &&
				!BodyLambda.Generic.IsSome(out _)
			) {
				RefinedBody = RefineLambda(
					BodyLambda,
					ExpectedResult,
					Inference,
					Argument.Scope
				);
			} else {
				RefinedBody = aLambda.Body.UpdateTypes(
					Argument.Scope,
					Inference
				).ThenTry(
					aBody => (
						aBody.Type.ApplyInference(aBody.Mappings)
					).IsSubTypeOf(
						ExpectedResult.ApplyInference(aBody.Mappings),
						aBody.Mappings
					).Then(
						__ => (
							ExpectedResult.ApplyInference(__),
							__
						)
					).ModifyError(
						__ => (aLambda.Body.Pos, __)
					)
				);
			}
			
			return RefinedBody.Then(
				Body => {
					var Type = Expected.ApplyInference(Body.Mappings);
					aLambda.TypeAnnotation = Type;
					return (Type, Body.Mappings);
				}
			);
		}
		
		if (
			aArgument is mSPO_AST.tLambdaNode<tPos> Lambda &&
			!Lambda.Generic.IsSome(out _)
		) {
			return RefineLambda(Lambda, aExpectedType, aMappings, aScope);
		}
		
		return aArgument.UpdateTypes(aScope, aMappings).ThenTry(
			Expression => Expression.Type.ApplyInference(Expression.Mappings).IsSubTypeOf(
				aExpectedType.ApplyInference(Expression.Mappings),
				Expression.Mappings
			).ModifyError(
				__ => (aArgument.Pos, __)
			).Then(
				Inference => (
					Expression.Type.ApplyInference(Inference),
					Inference
				)
			)
		);
	}
	
	private static mResult.tResult<
		(mVM_Type.tType Type, tInferenceState Mappings),
		(tPos Pos, tText ErrorText)
	>
	TryInferArguments<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aArgument,
		mVM_Type.tType aExpectedType,
		mStream.tStream<tScopeItem> aScope,
		tInferenceState aMappings
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
		var Mappings = aMappings;
		var Remaining = ArgumentCount;
		var ArgumentsWithTypes = mStream.ZipShort(Arguments, ExpectedTypes).MapWithIndex().Reverse();
		
		while (Remaining > 0) {
			var Progress = false;
			foreach (var (Index, ArgumentAndType) in ArgumentsWithTypes) {
				if (Done[Index]) {
					continue;
				}
				
				var Expected = ArgumentAndType._2.ApplyInference(Mappings);
				if (
					ArgumentAndType._1.TryInferArgument(
						Expected,
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
		
		foreach (var (Index, Argument) in Arguments.MapWithIndex()) {
			ArgumentTypes[Index] = ArgumentTypes[Index].ApplyInference(Mappings);
			var ExpectedType = ExpectedTypes.Skip(Index).TryFirst().AssertNotEmpty().ApplyInference(Mappings);
			if (!ArgumentTypes[Index].IsSubType(ExpectedType).Match(out _, out var TypeError)) {
				return mResult.Fail((Argument.Pos, TypeError));
			}
			
			Argument.TypeAnnotation = ArgumentTypes[Index];
			if (
				Argument is mSPO_AST.tLambdaNode<tPos> Lambda &&
				!Lambda.Generic.IsSome(out _) &&
				ArgumentTypes[Index].IsProc(out _, out var ArgType, out _)
			) {
				if (!UpdatePatternTypes(
					Lambda.Head,
					ArgType,
					aScope
				).Match(out _, out var Error)) {
					return mResult.Fail(Error);
				}
				Lambda.TypeAnnotation = ArgumentTypes[Index];
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
	) => aNode.UpdateTypes(
		aScope,
		mVM_Type.NewInferenceState()
	).Then(__ => __.Type);
	
	private static mResult.tResult<
		(mVM_Type.tType Type, tInferenceState Mappings),
		(tPos Pos, tText ErrorText)
	>
	UpdateTypes<tPos>(
		this mSPO_AST.tExpressionNode<tPos> aNode,
		mStream.tStream<tScopeItem> aScope,
		tInferenceState aMappings
	) => (
		aNode switch {
			mSPO_AST.tEmptyNode<tPos> => (mVM_Type.Empty(), aMappings),
			mSPO_AST.tTrueNode<tPos> => (mVM_Type.True(), aMappings),
			mSPO_AST.tFalseNode<tPos> => (mVM_Type.False(), aMappings),
			mSPO_AST.tIntNode<tPos> => (mVM_Type.Int(), aMappings),
			mSPO_AST.tTextNode<tPos> => (mVM_Type.Text(), aMappings),
			mSPO_AST.tCharNode<tPos> => (mVM_Type.Char(), aMappings),
			mSPO_AST.tIdNode<tPos> IdNode => (
				(IdNode.Id == "_=...") ? (
					mStd.With(
						mVM_Type.TypeVariable(),
						aTypeVariable => mResult.OK(
							(
								Type: mVM_Type.Proc(
									aTypeVariable,
									aTypeVariable,
									mVM_Type.Empty()
								),
								Mappings: aMappings.AddVar(aTypeVariable)
							)
						).WithErrorType<(tPos Pos, tText ErrorText)>()
					)
				) : (
					aScope.Where(
						__ => __.Id == IdNode.Id
					).TryFirst(
					).ElseFail(
						() => (IdNode.Pos, $"No Identifier '{IdNode.Id}' in scope")
					).Then(
						__ => (Type: __.Type, Mappings: aMappings)
					)
				)
			),
			mSPO_AST.tTypeNode<tPos> Type => Type.AsVM_Type(
				aScope
			).Then(
				__ => (__.KindOf(), aMappings)
			),
			mSPO_AST.tTupleNode<tPos> Tuple => Tuple.Items.Reduce(
				mResult.OK(
					(Types: mStream.Stream<mVM_Type.tType>(), Mappings: aMappings)
				).WithErrorType<(tPos Pos, tText ErrorText)>(),
				(Result, Item) => Result.ThenTry(
					__ => Item.UpdateTypes(aScope, __.Mappings).Then(
						ItemType => (
							Types: mStream.Stream(ItemType.Type, __.Types),
							ItemType.Mappings
						)
					)
				)
			).Then(
				__ => (Type: mVM_Type.Tuple(__.Types.Reverse()), __.Mappings)
			),
			mSPO_AST.tPairNode<tPos> Pair => (
				Pair.Tail.UpdateTypes(
					aScope,
					aMappings
				).ThenTry(
					aTail => Pair.Head.UpdateTypes(aScope, aTail.Mappings).Then(
						aHead => (
							Type: mVM_Type.Pair(aTail.Type, aHead.Type),
							aHead.Mappings
						)
					)
				)
			),
			mSPO_AST.tSigNode<tPos> Sig => mStd.Call(
				() => {
					if (!Sig.Contract.AsVM_Type(aScope).Match(out var Contract, out var Error)) {
						return mResult.Fail(Error);
					}
					Sig.Contract.TypeAnnotation = Contract;
					if (!Contract.IsSig(out var Binder, out var BinderKind, out var BodyType)) {
						return mResult.Fail(
							(Sig.Contract.Pos, $"expected §SIG_WITH contract but is '{Contract.ToText()}'")
						);
					}
					if (!Sig.Head.TryGetTypeValue(aScope).Match(out var Head, out Error)) {
						return mResult.Fail(Error);
					}
					if (!Head.IsSome(out var HeadType)) {
						return mResult.Fail((Sig.Head.Pos, "§SIG witness has to be a type value"));
					}
					Sig.Head.TypeAnnotation = BinderKind;
					if (
						!HeadType.KindOf().IsSubType(BinderKind).Match(out _, out var TypeError)
					) {
						return mResult.Fail((Sig.Head.Pos, TypeError));
					}
					if (!Sig.Body.UpdateTypes(aScope, aMappings).Match(out var Body, out Error)) {
						return mResult.Fail(Error);
					}
					if (
						!Body.Type.IsSubTypeOf(
							BodyType.Substitute(Binder, HeadType),
							Body.Mappings
						).Match(out var Mappings, out TypeError)
					) {
						return mResult.Fail((Sig.Body.Pos, TypeError));
					}
					return mResult.OK(
						(Type: Contract, Mappings)
					).WithErrorType<(tPos Pos, tText ErrorText)>();
				}
			),
			mSPO_AST.tPrefixNode<tPos> Prefix => (
				Prefix.Element.UpdateTypes(
					aScope,
					aMappings
				).Then(
					__ => (
						Type: mVM_Type.Prefix(Prefix.Prefix, __.Type),
						__.Mappings
					)
				)
			),
			mSPO_AST.tRecordNode<tPos> Record => Record.Elements.Reduce(
				mResult.OK(
					(Type: mVM_Type.Empty(), Mappings: aMappings)
				).WithErrorType<(tPos Pos, tText ErrorText)>(),
				(Result, Field) => Result.ThenTry(
					aRecord => Field.Value.UpdateTypes(aScope, aRecord.Mappings).Then(
						aField => (
							Type: mVM_Type.Record(
								aRecord.Type,
								mVM_Type.Prefix(Field.Key.Id, aField.Type)
							),
							aField.Mappings
						)
					)
				)
			),
			mSPO_AST.tLambdaNode<tPos> Lambda => mStd.Call(
				() => {
					var HeadScope = aScope;
					var Mappings = aMappings;
					var GenericScopeItems = mStream.Stream<tScopeItem>();
					if (Lambda.Generic.IsSome(out var GenericPattern)) {
						if (
							!UpdatePatternTypes(
								GenericPattern,
								GenericPatternType(GenericPattern),
								aScope
							).Match(out var Generic, out var Error)
						) {
							return mResult.Fail(Error);
						}
						HeadScope = Generic.Scope;
						GenericScopeItems = HeadScope.Take(HeadScope.Count() - aScope.Count());
					}
					
					if (
						!UpdatePatternTypes(
							Lambda.Head,
							mStd.cEmpty,
							HeadScope
						).Match(out var Argument, out var ArgumentError)
					) {
						return mResult.Fail(ArgumentError);
					}
					Mappings = Mappings.RegisterInferenceVariables(Argument.Type, HeadScope);
					if (
						!Lambda.Body.UpdateTypes(
							Argument.Scope,
							Mappings
						).Match(out var Body, out ArgumentError)
					) {
						return mResult.Fail(ArgumentError);
					}
					
					var Type = mVM_Type.Proc(
						mVM_Type.Empty(),
						Argument.Type.ApplyInference(Body.Mappings),
						Body.Type.ApplyInference(Body.Mappings)
					);
					foreach (var Item in GenericScopeItems) {
						if (Item.TypeValue.IsSome(out var Variable)) {
							Type = mVM_Type.Generic(Variable, Type);
						}
					}
					return mResult.OK(
						(Type, Body.Mappings)
					).WithErrorType<(tPos Pos, tText ErrorText)>();
				}
			),
			mSPO_AST.tMethodNode<tPos> Method => mStd.Call(
				() => {
					var Mappings = aMappings;
					
					if (
						!UpdatePatternTypes(
							Method.Obj,
							mStd.cEmpty,
							aScope
						).Match(out var Obj, out var Error)
					) {
						return mResult.Fail(Error);
					}
					Mappings = Mappings.RegisterInferenceVariables(Obj.Type, aScope);
					if (
						!UpdatePatternTypes(
							Method.Arg,
							mStd.cEmpty,
							Obj.Scope
						).Match(out var Arg, out Error)
					) {
						return mResult.Fail(Error);
					} else {
						Mappings = Mappings.RegisterInferenceVariables(Arg.Type, Obj.Scope);
						return Method.Body.UpdateTypes(Arg.Scope, Mappings).Then(
							Body => (
								Type: mVM_Type.Proc(
									Obj.Type.ApplyInference(Body.Mappings),
									Arg.Type.ApplyInference(Body.Mappings),
									Body.Type.ApplyInference(Body.Mappings)
								),
								Body.Mappings
							)
						);
					}
				}
			),
			mSPO_AST.tBlockNode<tPos> Block => mStd.Call(
				() => {
					var Types = mStream.Stream<mVM_Type.tType>();
					var BlockScope = aScope;
					var Mappings = aMappings;
					foreach (var Command in Block.Commands) {
						if (
							!UpdateCommandTypes(
								Command,
								BlockScope,
								Mappings
							).Match(out var CommandResult, out var Error)
						) {
							return mResult.Fail(Error);
						}
						BlockScope = CommandResult.Scope;
						Mappings = CommandResult.Mappings;
						if (Command is mSPO_AST.tReturnIfNode<tPos> ReturnIf) {
							var Type = ReturnIf.Result.TypeAnnotation.AssertNotEmpty(
							).ApplyInference(Mappings);
							if (Types.All(__ => __ != Type)) {
								Types = mStream.Stream(Type, Types);
							}
						}
					}
					return mResult.OK(
						(
							Type: Types.Join((a1, a2) => mVM_Type.Set(a2, a1), mVM_Type.Empty()),
							Mappings
						)
					).WithErrorType<(tPos Pos, tText ErrorText)>();
				}
			),
			mSPO_AST.tCallNode<tPos> Call => Call.Func.UpdateTypes(
				aScope,
				aMappings
			).ThenTry(
				aFunc => InferCall(
					aFunc.Type,
					Call.Arg,
					aScope,
					aFunc.Mappings,
					(Call.Func.Pos, $"expect proc but is:\n{aFunc.Type.ToText()}")
				)
			),
			mSPO_AST.tIfMatchNode<tPos> IfMatch => (
				IfMatch.Expression.UpdateTypes(aScope, aMappings).ThenTry(
					aMatchedExpression => mStd.Call(
						() => {
							var TypePattern = aMatchedExpression.Type;
							var Mappings = aMatchedExpression.Mappings;
							var Remaining = mMaybe.Some(TypePattern);
							var CaseTypes = mStream.Stream<mVM_Type.tType>();
							
							foreach (var Case in IfMatch.Cases) {
								var CaseInputType = Remaining.ElseUse(TypePattern);
								
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
								if (
									!Case.Expression.UpdateTypes(
										Pattern.Scope,
										Mappings
									).Match(out var CaseResult, out Error)
								) {
									return mResult.Fail(Error);
								}
								var CaseType = CaseResult.Type;
								Mappings = CaseResult.Mappings;
								var NewScopeCount = Pattern.Scope.Count() - aScope.Count();
								foreach (var Item in Pattern.Scope.Take(NewScopeCount)) {
									if (
										Item.TypeValue.IsSome(out var OpenVariable) &&
										OpenVariable.IsTypeVariable(out _, out _) &&
										!aScope.Any(
											__ => __.TypeValue.IsSome(out var Existing) && mStd.RefEq(Existing, OpenVariable)
										) &&
										CaseType.ContainsVariable(OpenVariable)
									) {
										return mResult.Fail((Case.Expression.Pos, "opened type witness escapes its match branch"));
									}
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
								(
									Type: CaseTypes.Reduce(
										(mVM_Type.tType)null!,
										(aTypes, aType) => (
											aTypes is null || aTypes == aType
											? aType
											: mVM_Type.Set(aType, aTypes)
										)
									),
									Mappings
								)
							).WithErrorType<(tPos Pos, tText ErrorText)>();
						}
					)
				)
			),
			mSPO_AST.tIsNode<tPos> Is => (
				Is.Expression.UpdateTypes(
					aScope,
					aMappings
				).ThenTry(
					aValue => UpdatePatternTypes(
						Is.Pattern,
						aValue.Type,
						aScope
					).Then(
						_ => (mVM_Type.Bool(), aValue.Mappings)
					)
				)
			),
			mSPO_AST.tVarToValNode<tPos> VarToVal => (
				VarToVal.Obj.UpdateTypes(
					aScope,
					aMappings
				).ThenTry(
					aObject => (
						aObject.Type.IsVar(out var ValType)
						? mResult.OK(
							(ValType, aObject.Mappings)
						).WithErrorType<(tPos Pos, tText ErrorText)>()
						: mResult.Fail(
							(VarToVal.Pos, $"the type '{aObject.Type}' in not from type '[§VAR ...]'")
						)
					)
				)
			),
			mSPO_AST.tIfNode<tPos> If => mStd.Call(
				() => {
					var Types = mStream.Stream<mVM_Type.tType>();
					var Mappings = aMappings;
					foreach (var Case in If.Cases) {
						if (
							!Case.Cond.UpdateTypes(
								aScope,
								Mappings
							).Match(out var Condition, out var Error)
						) {
							return mResult.Fail(Error);
						}
						if (!Condition.Type.IsSubTypeOf(
							mVM_Type.Bool(),
							Condition.Mappings
						).Match(out var ConditionMappings, out _)) {
							return mResult.Fail(
								(
									Case.Cond.Pos,
									$"condition '{Case.Cond.ToText()}' has to be [§TRUE | §FALSE] " +
									$"but is of type:\n  (DebugId: {Condition.Type.DebugId})" +
									Condition.Type.ToText()
								)
							);
						}
						if (
							!Case.Result.UpdateTypes(
								aScope,
								ConditionMappings
							).Match(out var CaseResult, out Error)
						) {
							return mResult.Fail(Error);
						}
						Mappings = CaseResult.Mappings;
						if (Types.All(__ => __ != CaseResult.Type)) {
							Types = mStream.Stream(CaseResult.Type, Types);
						}
					}
					var Type = Types.Count() switch {
						0 => mVM_Type.Empty(),
						1 => Types.TryFirst().AssertNotEmpty(),
						_ => Types.Reduce(
							mVM_Type.Empty(),
							(aTypeSet, aType) => mVM_Type.Set(aType, aTypeSet)
						)
					};
					return mResult.OK(
						(Type, Mappings)
					).WithErrorType<(tPos Pos, tText ErrorText)>();
				}
			),
			mSPO_AST.tPipeToLeftNode<tPos> Pipe => throw mError.Error(
				$"'{aNode.GetType().Name}' should be desugared at this point!"
			),
			_ => throw mError.Error(
				"not implemented: " + aNode.GetType().Name
			),
		}
	).Then(
		__ => (
			Type: __.Item1.ApplyInference(__.Item2),
			Mappings: __.Item2
		)
	).ThenDo(
		__ => {
			if (
				aNode is
					mSPO_AST.tIdNode<tPos> or
					mSPO_AST.tTrueNode<tPos> or
					mSPO_AST.tFalseNode<tPos> or
					not mSPO_AST.tTypeNode<tPos>
			) {
				aNode.TypeAnnotation = __.Type;
			}
		}
	);
	
	public static mResult.tResult<
		(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope),
		(tPos Pos, tText ErrorText)
	>
	UpdatePatternTypes<tPos>(
		mSPO_AST.tPatternNode<tPos> aPattern,
		mMaybe.tMaybe<mVM_Type.tType> aType,
		mStream.tStream<tScopeItem> aScope
	) {
		mResult.tResult<(mVM_Type.tType Type, mStream.tStream<tScopeItem> Scope), (tPos Pos, tText ErrorText)> Result;
		switch (aPattern) {
			case mSPO_AST.tSigPatternNode<tPos> SigPattern: {
				Result = SigPattern.Contract.AsVM_Type(aScope).ThenTry(
					aContract => {
						SigPattern.Contract.TypeAnnotation = aContract;
						
						if (!aContract.IsSig(out var Binder, out var BinderKind, out var ContractBody)) {
							return mResult.Fail(
								(
									SigPattern.Contract.Pos,
									$"expected §SIG_WITH contract but is '{aContract.ToText()}'"
								)
							);
						}
						
						var (Compatible, _) = aType.ElseUse(aContract).SplitBy(
							__ => (
								__.IsSig(out _, out _, out _) &&
								__.IsSubType(aContract).Match(out _, out _)
							)
						);
						
						if (!Compatible.IsSome(out _)) {
							return mResult.Fail(
								(
									SigPattern.Pos,
									$"pattern cannot match {aType.ElseUse(aContract).ToText()} against {aContract.ToText()}"
								)
							);
						}
						
						var OpenType = mVM_Type.TypeVariable("sig_" + Binder.Id, BinderKind);
						var HeadScope = aScope;
						var BodyWitness = OpenType;
						
						switch (SigPattern.Head) {
							case mSPO_AST.tTypedPatternNode<tPos> {
								Pattern: mSPO_AST.tFreeIdPatternNode<tPos> Head,
								TypeExpression: var KindExpression,
							} TypedHead:
								if (!KindExpression.IsSome(out var KindNode)) {
									return mResult.Fail((TypedHead.Pos, "missing §SIG head kind"));
								}
								
								if (!KindNode.AsVM_Type(aScope).Match(out var ExpectedKind, out var KindNodeError)) {
									return mResult.Fail(KindNodeError);
								}
								
								if (
									!ExpectedKind.IsSubType(BinderKind).Match(out _, out _) ||
									!BinderKind.IsSubType(ExpectedKind).Match(out _, out _)
								) {
									return mResult.Fail(
										(
											KindNode.Pos,
											$"§SIG head kind '{ExpectedKind.ToText()}' does not equal '{BinderKind.ToText()}'"
										)
									);
								}
								
								TypedHead.TypeAnnotation = BinderKind;
								Head.TypeAnnotation = BinderKind;
								HeadScope = mStream.Stream(ScopeItem(Head.Id, BinderKind, OpenType), HeadScope);
								
								if (BinderKind.IsProc(out _, out _, out _)) {
									HeadScope = mStream.Stream(ScopeItem(Head.Id + "...", BinderKind, OpenType), HeadScope);
								}
								
								break;
							case mSPO_AST.tFreeIdPatternNode<tPos> Head:
								Head.TypeAnnotation = BinderKind;
								HeadScope = mStream.Stream(ScopeItem(Head.Id, BinderKind, OpenType), HeadScope);
								
								if (BinderKind.IsProc(out _, out _, out _)) {
									HeadScope = mStream.Stream(ScopeItem(Head.Id + "...", BinderKind, OpenType), HeadScope);
								}
								
								break;
							case mSPO_AST.tIgnorePatternNode<tPos> Head:
								Head.TypeAnnotation = BinderKind;
								HeadScope = mStream.Stream(ScopeItem("$sig_" + OpenType.Id, BinderKind, OpenType), HeadScope);
								break;
							case mSPO_AST.tTypePatternNode<tPos> Head:
								if (!Head.Type.AsVM_Type(aScope).Match(out BodyWitness, out var HeadError)) {
									return mResult.Fail(HeadError);
								}
								
								if (!BodyWitness.KindOf().IsSubType(BinderKind).Match(out _, out var KindError)) {
									return mResult.Fail((Head.Pos, KindError));
								}
								
								Head.TypeAnnotation = BinderKind;
								
								break;
							default:
								return mResult.Fail((SigPattern.Head.Pos, "invalid §SIG head pattern"));
						}
						return UpdatePatternTypes(
							SigPattern.Body,
							ContractBody.Substitute(Binder, BodyWitness),
							HeadScope
						).Then(__ => (aContract, __.Scope));
					}
				);
				
				break;
			}
			case mSPO_AST.tTypePatternNode<tPos> TypePattern: {
				Result = TypePattern.Type.AsVM_Type(aScope).Then(
					__ => (__.KindOf(), aScope)
				);
				break;
			}
			case mSPO_AST.tTypedPatternNode<tPos> Pattern: {
				Result = Pattern.TypeExpression.Match(
					__ => mStd.Call(
						() => __.AsVM_Type(aScope).ThenTry(
							aType => UpdatePatternTypes(
								Pattern.Pattern,
								aType,
								aScope
							)
						)
					),
					() => UpdatePatternTypes(Pattern.Pattern, aType, aScope)
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
									mVM_Type.TypeVariable(FreePatternId.Id)
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
				Result = UpdatePatternTypes(PrefixPattern.Pattern, SubType, aScope).Then(
					__ => (mVM_Type.Prefix(PrefixPattern.Prefix, __.Type), __.Scope)
				);
				break;
			}
			case mSPO_AST.tTuplePatternNode<tPos> TuplePattern: {
				var Types = mStream.Stream<mVM_Type.tType>([]);
				var NewScope = aScope;
				if (!aType.IsSome(out var Type)) {
					foreach (var Pattern in TuplePattern.Items) {
						if (!UpdatePatternTypes(Pattern, mStd.cEmpty, NewScope).Match(out var TS, out var Error)) {
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
						var PatternCount = TuplePattern.Items.Count();
						if (
							PatternCount == 0 && !Type.IsEmpty() ||
							PatternCount > 1
						) {
							return mResult.Fail(
								(
									TuplePattern.Pos,
									$"can't unify '{TuplePattern.ToText()}' and '{Type.ToText()}'"
								)
							);
						}
						if (PatternCount == 1) {
							if (!UpdatePatternTypes(
								TuplePattern.Items.TryFirst().AssertNotEmpty(),
								Type,
								NewScope
							).Match(out var TS, out var Error)) {
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
							if (!UpdatePatternTypes(Pattern, ItemType, NewScope).Match(out var TS, out var Error)) {
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
						aScope
					).Match(
						out var TailRes,
						out var Error
					) ||
					!UpdatePatternTypes(
						PairPattern.Head,
						HeadType,
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
					!BoolRes.IsSubType(mVM_Type.Bool()).Match(out _, out _)
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
				var Type = aType.IsSome(out var T) ? T : mVM_Type.TypeVariable();
				
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
	) => UpdateMethodCallTypes(
		aMethodCall,
		aScope,
		mVM_Type.NewInferenceState()
	).Then(__ => __.Scope);
	
	private static mResult.tResult<
		(mStream.tStream<tScopeItem> Scope, tInferenceState Mappings),
		(tPos Pos, tText ErrorText)
	>
	UpdateMethodCallTypes<tPos>(
		mSPO_AST.tMethodCallNode<tPos> aMethodCall,
		mStream.tStream<tScopeItem> aScope,
		tInferenceState aMappings
	) => aMethodCall.Method.UpdateTypes(aScope, aMappings).ThenTry(
		aMethod => InferCall(
			aMethod.Type,
			aMethodCall.Argument,
			aScope,
			aMethod.Mappings,
			(aMethodCall.Argument.Pos, $"'{aMethod.Type.ToText()}' is not a Proc")
		).ThenTry(
			aCall => (
				!aMethodCall.Result.IsSome(out var Result)
				? mResult.OK(
					(Scope: aScope, aCall.Mappings)
				).WithErrorType<(tPos Pos, tText ErrorText)>()
				: UpdatePatternTypes(
					Result,
					aCall.Type,
					aScope
				).Then(__ => (__.Scope, aCall.Mappings))
			)
		)
	);
	
	public static mResult.tResult<mStream.tStream<tScopeItem>, (tPos Pos, tText ErrorText)>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<tScopeItem> aScope
	) => UpdateCommandTypes(
		aCommand,
		aScope,
		mVM_Type.NewInferenceState()
	).Then(__ => __.Scope);
	
	private static mResult.tResult<
		(mStream.tStream<tScopeItem> Scope, tInferenceState Mappings),
		(tPos Pos, tText ErrorText)
	>
	UpdateCommandTypes<tPos>(
		mSPO_AST.tCommandNode<tPos> aCommand,
		mStream.tStream<tScopeItem> aScope,
		tInferenceState aMappings
	) {
		static mResult.tResult<
			(mStream.tStream<tScopeItem> Scope, tInferenceState Mappings),
			(tPos Pos, tText ErrorText)
		>
		UpdateMethodCalls(
			mStream.tStream<mSPO_AST.tMethodCallNode<tPos>> aMethodCalls,
			tInferenceState aMethodMappings,
			mStream.tStream<tScopeItem> aScope
		) => aMethodCalls.Reduce(
			mResult.OK(
				(Scope: aScope, Mappings: aMethodMappings)
			).WithErrorType<(tPos Pos, tText ErrorText)>(),
			(Result, MethodCall) => Result.ThenTry(
				__ => UpdateMethodCallTypes(MethodCall, __.Scope, __.Mappings)
			)
		);
		
		switch (aCommand) {
			case mSPO_AST.tDefNode<tPos> Def: {
				return Def.Src.TryGetTypeValue(aScope).ThenTry(
					aTypeValue => Def.Src.UpdateTypes(aScope, aMappings).ThenTry(
						aSrc => UpdatePatternTypes(
							Def.Des,
							aSrc.Type,
							aScope
						).ThenTry(
							aType => aSrc.Type.IsSubTypeOf(
								aType.Type,
								aSrc.Mappings
							).Then(
								Mappings => (
									Scope: (
										aTypeValue.IsSome(out var TypeValue) &&
										Def.Des.TryGetId().IsSome(out var Id)
										? mStream.Stream(
											ScopeItem(
												Id,
											aSrc.Type.ApplyInference(Mappings),
												TypeValue
											),
											aType.Scope
										)
										: aType.Scope
									),
									Mappings
								)
							).ModifyError(
								__ => (Def.Src.Pos, __)
							)
						)
					)
				);
			}
			case mSPO_AST.tReturnIfNode<tPos> ReturnIf: {
				return ReturnIf.Condition.UpdateTypes(
					aScope,
					aMappings
				).ThenTry(
					aCondition => aCondition.Type.IsSubTypeOf(
						mVM_Type.Bool(),
						aCondition.Mappings
					).ModifyError(
						__ => (
							ReturnIf.Pos,
							$"(DebugId: {aCondition.Type.DebugId}) " +
							$"{aCondition.Type.ToText()} != [§TRUE | §FALSE]"
						)
					).ThenTry(
						Mappings => ReturnIf.Result.UpdateTypes(
							aScope,
							Mappings
						)
					)
				).Then(
					aResult => (aScope, aResult.Mappings)
				);
			}
			case mSPO_AST.tDefVarNode<tPos> DefVar: {
				return DefVar.Expression.UpdateTypes(aScope, aMappings).ThenTry(
					aValue => UpdateMethodCalls(DefVar.MethodCalls, aValue.Mappings, aScope).Then(
						aMethods => {
							var Type = mVM_Type.Var(aValue.Type.ApplyInference(aMethods.Mappings));
							var NewScope = mStream.Stream(
								ScopeItem(DefVar.Id.Id, Type),
								aMethods.Scope
							);
							DefVar.Id.UpdateTypes(NewScope);
							return (NewScope, aMethods.Mappings);
						}
					)
				);
			}
			case mSPO_AST.tRecLambdasNode<tPos> RecLambdas: {
				var NewScope = aScope;
				var Mappings = aMappings;
				var ResultVariables = mStream.Stream<(tText Id, mVM_Type.tType Variable)>();
				foreach (var Item in RecLambdas.List) {
					var HeadScope = NewScope;
					if (Item.Lambda.Generic.IsSome(out var GenericPattern)) {
						if (
							!UpdatePatternTypes(
								GenericPattern,
								GenericPatternType(GenericPattern),
								HeadScope
							).Match(out var GenScope, out var GenError)
						) {
							return mResult.Fail(GenError);
						}
						HeadScope = GenScope.Scope;
					}
					
					if (
						!UpdatePatternTypes(
							Item.Lambda.Head,
							mStd.cEmpty,
							HeadScope
						).Match(out var Result, out var Error)
					) {
						return mResult.Fail(Error);
					}
					Mappings = Mappings.RegisterInferenceVariables(Result.Type, HeadScope);
					
					var ResultVariable = mVM_Type.TypeVariable("__" + Item.Id.Id + "_Res__");
					var ObjectVariable = mVM_Type.TypeVariable("__" + Item.Id.Id + "_Obj__");
					Mappings = Mappings.AddVar(ResultVariable).AddVar(ObjectVariable);
					var ProcType = mVM_Type.Proc(
						ObjectVariable,
						Result.Type,
						ResultVariable
					);
					var GenericCount = HeadScope.Count() - NewScope.Count();
					foreach (var ScopeItem in HeadScope.Take(GenericCount)) {
						if (ScopeItem.TypeValue.IsSome(out var Variable)) {
							ProcType = mVM_Type.Generic(Variable, ProcType);
						}
					}
					NewScope = mStream.Stream(
						ScopeItem(Item.Id.Id, ProcType),
						NewScope
					);
					ResultVariables = mStream.Stream((Item.Id.Id, ResultVariable), ResultVariables);
				}
				
				var ClosedTypes = mStream.Stream<(tText Id, mVM_Type.tType Type)>();
				foreach (var Item in RecLambdas.List) {
					if (
						!Item.Lambda.UpdateTypes(
							NewScope,
							Mappings
						).Match(out var Inferred, out var Error)
					) {
						return mResult.Fail(Error);
					}
					Mappings = Inferred.Mappings;
					
					var ResultVariable = ResultVariables.Where(
						__ => __.Id == Item.Id.Id
					).TryFirst(
					).AssertNotEmpty(
					).Variable;
					
					static mVM_Type.tType
					Close(
						mVM_Type.tType aType,
						mVM_Type.tType aResultVariable
					) {
						if (aType.IsGeneric(out var Binder, out var Body)) {
							return mVM_Type.Generic(Binder, Close(Body, aResultVariable));
						}
						
						mAssert.IsTrue(aType.IsProc(out var Obj, out var Arg, out var Res));
						
						static mMaybe.tMaybe<mVM_Type.tType>
						RemoveUnchangedResult(
							mVM_Type.tType aResult,
							mVM_Type.tType aResultVariable
						) {
							if (mStd.RefEq(aResult, aResultVariable)) {
								return mStd.cEmpty;
							}
							
							if (!aResult.IsSet(out var Type1, out var Type2)) {
								return mMaybe.Some(aResult);
							}
							
							return mVM_Type.Union(
								RemoveUnchangedResult(Type1, aResultVariable),
								RemoveUnchangedResult(Type2, aResultVariable)
							);
						}
						
						var ClosedResult = RemoveUnchangedResult(Res, aResultVariable).ElseUse(Res);
						
						return mVM_Type.Proc(
							Obj,
							Arg,
							ClosedResult.ContainsVariable(aResultVariable)
							? mVM_Type.Recursive(aResultVariable, ClosedResult)
							: ClosedResult
						);
					}
					var Closed = Close(Inferred.Type, ResultVariable);
					ClosedTypes = mStream.Stream((Item.Id.Id, Closed), ClosedTypes);
					NewScope = mStream.Stream(ScopeItem(Item.Id.Id, Closed), NewScope);
				}
				
				foreach (var Item in RecLambdas.List) {
					if (
						!Item.Lambda.UpdateTypes(
							NewScope,
							Mappings
						).Match(out var Inferred, out var Error)
					) {
						return mResult.Fail(Error);
					}
					Mappings = Inferred.Mappings;
					Item.Lambda.TypeAnnotation = ClosedTypes.Where(__ => __.Id == Item.Id.Id).TryFirst().AssertNotEmpty().Type;
				}
				
				return (NewScope, Mappings);
			}
			case mSPO_AST.tMethodCallsNode<tPos> MethodCalls: {
				return MethodCalls.Object.UpdateTypes(aScope, aMappings).ThenTry(
					aObject => UpdateMethodCalls(MethodCalls.MethodCalls, aObject.Mappings, aScope)
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
					() => (IdNode.Pos, $"unknown type of Identifier '{IdNode.Id}' in [{aScope.Map(__ => __.Id).Reduce("", (a, b) => a + ", " + b)}]")
				).Then(
					__ => __.TypeValue.ElseUse(__.Type)
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
				var RecursiveVar = mVM_Type.TypeVariable(Name, mVM_Type.Type());
				
				Result = RecursiveType.BodyType.AsVM_Type(
					mStream.Stream(
						ScopeItem(Name, mVM_Type.Type(), RecursiveVar),
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
				var GenericVar = mVM_Type.TypeVariable(Name, mVM_Type.Type());
				
				Result = GenericType.BodyType.AsVM_Type(
					mStream.Stream(
						ScopeItem(Name, mVM_Type.Type(), GenericVar),
						aScope
					)
				).Then(
					__ => mVM_Type.Generic(GenericVar, __)
				);
				break;
			}
			case mSPO_AST.tGenericApplyTypeNode<tPos> GenericApplyType: {
				Result = GenericApplyType.GenericType.AsVM_Type(aScope).ThenTry(
					aGenericType => GenericApplyType.ArgType.AsVM_Type(aScope).Then(
						aArgType => mVM_Type.TypeApply(aGenericType, aArgType)
					)
				);
				break;
			}
			case mSPO_AST.tSigTypeNode<tPos> SigType: {
				Result = SigType.HeadType.AsVM_Type(aScope).ThenTry(
					aHeadKind => mStd.Call(
						() => {
							var Binder = mVM_Type.TypeVariable(SigType.Head.Id, aHeadKind);
							var ScopeId = aHeadKind.IsProc(out _, out _, out _) ? SigType.Head.Id + "..." : SigType.Head.Id;
							return SigType.BodyType.AsVM_Type(
								mStream.Stream(
									ScopeItem(ScopeId, aHeadKind, Binder),
									aScope
								)
							).Then(aBody => mVM_Type.Sig(Binder, aHeadKind, aBody));
						}
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
