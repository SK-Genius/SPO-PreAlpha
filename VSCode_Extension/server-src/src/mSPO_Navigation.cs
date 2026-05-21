#:include Common/mStd.cs
#:include Common/mMaybe.cs
#:include Common/mStream.cs
#:include Common/mSpan.cs
#:include Common/mTextParser.cs
#:include Common/mTextStream.cs
#:include mSPO_AST.cs
#:include mSPO_Parser.cs

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO_Navigation {
	public enum
	tSymbolKind {
		None = 0,
		File = 1,
		Module = 2,
		Namespace = 3,
		Method = 6,
		Function = 12,
		Variable = 13,
		Operator = 25,
		TypeParameter = 26,
	}

	public readonly struct
	tDocumentSymbol {
		public readonly tText Name;
		public readonly tText Detail;
		public readonly tSymbolKind Kind;
		public readonly tSpan Range;
		public readonly tSpan SelectionRange;
		public readonly mStream.tStream<tDocumentSymbol> Children;

		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tDocumentSymbol(
			tText aName,
			tText aDetail,
			tSymbolKind aKind,
			tSpan aRange,
			tSpan aSelectionRange,
			mStream.tStream<tDocumentSymbol> aChildren
		) {
			this.Name = aName;
			this.Detail = aDetail;
			this.Kind = aKind;
			this.Range = aRange;
			this.SelectionRange = aSelectionRange;
			this.Children = aChildren;
		}
	}

	public readonly struct
	tLocation {
		public readonly tText Uri;
		public readonly tSpan Range;

		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tLocation(
			tText aUri,
			tSpan aRange
		) {
			this.Uri = aUri;
			this.Range = aRange;
		}
	}

	private readonly struct
	tBinding {
		public readonly tText Id;
		public readonly tSpan Span;
		public readonly tSymbolKind Kind;

		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tBinding(
			tText aId,
			tSpan aSpan,
			tSymbolKind aKind
		) {
			this.Id = aId;
			this.Span = aSpan;
			this.Kind = aKind;
		}
	}

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tDocumentSymbol
	DocumentSymbol(
		tText aName,
		tText aDetail,
		tSymbolKind aKind,
		tSpan aRange,
		tSpan aSelectionRange,
		mStream.tStream<tDocumentSymbol> aChildren
	) => new(
		aName,
		aDetail,
		aKind,
		aRange,
		aSelectionRange,
		aChildren
	);

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tLocation
	Location(
		tText aUri,
		tSpan aRange
	) => new(
		aUri,
		aRange
	);

	public static mStream.tStream<tDocumentSymbol>
	GetDocumentSymbols(
		tText aCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) => TryParseModule(
		aCode,
		aId,
		aDebugStream,
		out var Module
	)
		? GetModuleSymbols(Module)
		: mStd.cEmpty;

	public static mMaybe.tMaybe<tLocation>
	GetDefinition(
		tText aCode,
		tText aId,
		tPos aPos,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		if (
			!TryParseModule(
				aCode,
				aId,
				aDebugStream,
				out var Module
			) ||
			!TryResolveModule(
				Module,
				aPos,
				[],
				out var DefinitionSpan
			)
		) {
			return mStd.cEmpty;
		}

		return Location(DefinitionSpan.Start.Id, DefinitionSpan);
	}

	private static mStream.tStream<tDocumentSymbol>
	GetModuleSymbols(
		mSPO_AST.tModuleNode<tSpan> aModule
	) {
		var Result = new System.Collections.Generic.List<tDocumentSymbol>();

		var ImportChildren = GetPatternSymbols(
			aModule.Import.Pattern,
			aModule.Import.Pos,
			"",
			tSymbolKind.Variable,
			mStd.cEmpty
		);
		if (!ImportChildren.IsEmpty()) {
			Result.Add(
				DocumentSymbol(
					"import",
					"",
					tSymbolKind.Namespace,
					aModule.Import.Pos,
					aModule.Import.Pos,
					ImportChildren
				)
			);
		}

		foreach (var Command in aModule.Commands) {
			AddSymbols(
				Result,
				GetCommandSymbols(Command)
			);
		}

		return mStream.Stream(Result.ToArray());
	}

	private static mStream.tStream<tDocumentSymbol>
	GetCommandSymbols(
		mSPO_AST.tCommandNode<tSpan> aCommand
	) => aCommand switch {
		mSPO_AST.tDefNode<tSpan> Def => GetPatternSymbols(
			Def.Des,
			Def.Pos,
			"",
			GetBindingKind(Def.Src, TryGetPatternDisplayId(Def.Des)),
			GetExpressionSymbols(Def.Src)
		),
		mSPO_AST.tDefVarNode<tSpan> DefVar => mStream.Stream(
			DocumentSymbol(
				DisplayId(DefVar.Id.Id),
				"var",
				GetBindingKind(DefVar.Expression, DefVar.Id.Id),
				DefVar.Pos,
				DefVar.Id.Pos,
				JoinSymbolStreams(
					mStream.Stream(
						GetExpressionSymbols(DefVar.Expression),
						GetMethodCallSymbols(DefVar.MethodCalls)
					)
				)
			)
		),
		mSPO_AST.tMethodCallsNode<tSpan> MethodCalls => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(MethodCalls.Object),
				GetMethodCallSymbols(MethodCalls.MethodCalls)
			)
		),
		mSPO_AST.tRecLambdasNode<tSpan> RecLambdas => RecLambdas.List.Map(
			aItem => DocumentSymbol(
				DisplayId(aItem.Id.Id),
				"recursive",
				GetBindingKind(aItem.Lambda, aItem.Id.Id),
				aItem.Pos,
				aItem.Id.Pos,
				GetExpressionSymbols(aItem.Lambda)
			)
		),
		mSPO_AST.tReturnIfNode<tSpan> ReturnIf => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(ReturnIf.Result),
				GetExpressionSymbols(ReturnIf.Condition)
			)
		),
		_ => mStd.cEmpty,
	};

	private static mStream.tStream<tDocumentSymbol>
	GetExpressionSymbols(
		mSPO_AST.tExpressionNode<tSpan> aExpression
	) => aExpression switch {
		mSPO_AST.tLambdaNode<tSpan> Lambda => GetLambdaSymbols(Lambda),
		mSPO_AST.tMethodNode<tSpan> Method => GetMethodSymbols(Method),
		mSPO_AST.tBlockNode<tSpan> Block => GetBlockSymbols(Block),
		mSPO_AST.tIfNode<tSpan> If => JoinSymbolStreams(
			If.Cases.Map(
				aCase => JoinSymbolStreams(
					GetExpressionSymbols(aCase.Cond),
					GetExpressionSymbols(aCase.Result)
				)
			)
		),
		mSPO_AST.tIfMatchNode<tSpan> IfMatch => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(IfMatch.Expression),
				JoinSymbolStreams(
					IfMatch.Cases.Map(
						aCase => JoinSymbolStreams(
							GetPatternSymbols(
								aCase.Pattern,
								aCase.Pattern.Pos,
								"",
								tSymbolKind.Variable,
								mStd.cEmpty
							),
							GetExpressionSymbols(aCase.Expression)
						)
					)
				)
			)
		),
		mSPO_AST.tCallNode<tSpan> Call => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(Call.Func),
				GetExpressionSymbols(Call.Arg)
			)
		),
		mSPO_AST.tPrefixNode<tSpan> Prefix => GetExpressionSymbols(Prefix.Element),
		mSPO_AST.tRecordNode<tSpan> Record => JoinSymbolStreams(
			Record.Elements.Map(
				aElement => GetExpressionSymbols(aElement.Value)
			)
		),
		mSPO_AST.tPairNode<tSpan> Pair => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(Pair.Tail),
				GetExpressionSymbols(Pair.Head)
			)
		),
		mSPO_AST.tTupleNode<tSpan> Tuple => JoinSymbolStreams(
			Tuple.Items.Map(
				GetExpressionSymbols
			)
		),
		mSPO_AST.tIsNode<tSpan> Is => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(Is.Expression),
				GetPatternSymbols(
					Is.Pattern,
					Is.Pattern.Pos,
					"",
					tSymbolKind.Variable,
					mStd.cEmpty
				)
			)
		),
		mSPO_AST.tVarToValNode<tSpan> VarToVal => GetExpressionSymbols(VarToVal.Obj),
		mSPO_AST.tPipeToRightNode<tSpan> Pipe => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(Pipe.Head),
				JoinSymbolStreams(Pipe.Pipe.Map(GetExpressionSymbols))
			)
		),
		mSPO_AST.tPipeToLeftNode<tSpan> Pipe => JoinSymbolStreams(
			mStream.Stream(
				JoinSymbolStreams(Pipe.Pipe.Map(GetExpressionSymbols)),
				GetExpressionSymbols(Pipe.Head)
			)
		),
		mSPO_AST.tRecursiveTypeNode<tSpan> RecursiveType => mStream.Stream(
			DocumentSymbol(
				DisplayId(RecursiveType.HeadType.Id),
				"type parameter",
				tSymbolKind.TypeParameter,
				RecursiveType.Pos,
				RecursiveType.HeadType.Pos,
				GetExpressionSymbols(RecursiveType.BodyType)
			)
		),
		mSPO_AST.tGenericTypeNode<tSpan> GenericType => mStream.Stream(
			DocumentSymbol(
				DisplayId(GenericType.HeadType.Id),
				"type parameter",
				tSymbolKind.TypeParameter,
				GenericType.Pos,
				GenericType.HeadType.Pos,
				GetExpressionSymbols(GenericType.BodyType)
			)
		),
		mSPO_AST.tInterfaceTypeNode<tSpan> InterfaceType => mStream.Stream(
			DocumentSymbol(
				DisplayId(InterfaceType.HeadType.Id),
				"type parameter",
				tSymbolKind.TypeParameter,
				InterfaceType.Pos,
				InterfaceType.HeadType.Pos,
				GetExpressionSymbols(InterfaceType.BodyType)
			)
		),
		mSPO_AST.tGenericApplyTypeNode<tSpan> GenericApplyType => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(GenericApplyType.GenericType),
				GetExpressionSymbols(GenericApplyType.ArgType)
			)
		),
		mSPO_AST.tVarTypeNode<tSpan> VarType => GetExpressionSymbols(VarType.Type),
		mSPO_AST.tPrefixTypeNode<tSpan> PrefixType => JoinSymbolStreams(
			PrefixType.Expressions.Map(GetExpressionSymbols)
		),
		mSPO_AST.tTupleTypeNode<tSpan> TupleType => JoinSymbolStreams(
			TupleType.ItemTypes.Map(GetExpressionSymbols)
		),
		mSPO_AST.tPairTypeNode<tSpan> PairType => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(PairType.TailType),
				GetExpressionSymbols(PairType.HeadType)
			)
		),
		mSPO_AST.tSetTypeNode<tSpan> SetType => JoinSymbolStreams(
			SetType.Expressions.Map(GetExpressionSymbols)
		),
		mSPO_AST.tLambdaTypeNode<tSpan> LambdaType => JoinSymbolStreams(
			mStream.Stream(
				GetExpressionSymbols(LambdaType.EnvType),
				GetExpressionSymbols(LambdaType.ArgType),
				GetExpressionSymbols(LambdaType.ResType)
			)
		),
		_ => mStd.cEmpty,
	};

	private static mStream.tStream<tDocumentSymbol>
	GetLambdaSymbols(
		mSPO_AST.tLambdaNode<tSpan> aLambda
	) {
		var Result = new System.Collections.Generic.List<tDocumentSymbol>();

		if (aLambda.Generic.IsSome(out var GenericPattern)) {
			AddSymbols(
				Result,
				GetPatternSymbols(
					GenericPattern,
					GenericPattern.Pos,
					"type parameter",
					tSymbolKind.TypeParameter,
					mStd.cEmpty
				)
			);
		}

		AddSymbols(
			Result,
			GetPatternSymbols(
				aLambda.Head,
				aLambda.Head.Pos,
				"",
				tSymbolKind.Variable,
				mStd.cEmpty
			)
		);
		AddSymbols(
			Result,
			GetExpressionSymbols(aLambda.Body)
		);

		return mStream.Stream(Result.ToArray());
	}

	private static mStream.tStream<tDocumentSymbol>
	GetMethodSymbols(
		mSPO_AST.tMethodNode<tSpan> aMethod
	) {
		var Result = new System.Collections.Generic.List<tDocumentSymbol>();

		AddSymbols(
			Result,
			GetPatternSymbols(
				aMethod.Obj,
				aMethod.Obj.Pos,
				"self",
				tSymbolKind.Variable,
				mStd.cEmpty
			)
		);
		AddSymbols(
			Result,
			GetPatternSymbols(
				aMethod.Arg,
				aMethod.Arg.Pos,
				"",
				tSymbolKind.Variable,
				mStd.cEmpty
			)
		);
		AddSymbols(
			Result,
			GetExpressionSymbols(aMethod.Body)
		);

		return mStream.Stream(Result.ToArray());
	}

	private static mStream.tStream<tDocumentSymbol>
	GetBlockSymbols(
		mSPO_AST.tBlockNode<tSpan> aBlock
	) => JoinSymbolStreams(
		aBlock.Commands.Map(GetCommandSymbols)
	);

	private static mStream.tStream<tDocumentSymbol>
	GetMethodCallSymbols(
		mStream.tStream<mSPO_AST.tMethodCallNode<tSpan>> aMethodCalls
	) => JoinSymbolStreams(
		aMethodCalls.Map(GetMethodCallSymbols)
	);

	private static mStream.tStream<tDocumentSymbol>
	GetMethodCallSymbols(
		mSPO_AST.tMethodCallNode<tSpan> aMethodCall
	) {
		var Result = new System.Collections.Generic.List<tDocumentSymbol>();

		AddSymbols(
			Result,
			GetExpressionSymbols(aMethodCall.Argument)
		);

		if (aMethodCall.Result.IsSome(out var ResultPattern)) {
			AddSymbols(
				Result,
				GetPatternSymbols(
					ResultPattern,
					ResultPattern.Pos,
					"",
					tSymbolKind.Variable,
					mStd.cEmpty
				)
			);
		}

		return mStream.Stream(Result.ToArray());
	}

	private static mStream.tStream<tDocumentSymbol>
	GetPatternSymbols(
		mSPO_AST.tPatternNode<tSpan> aPattern,
		tSpan aRange,
		tText aDetail,
		tSymbolKind aKind,
		mStream.tStream<tDocumentSymbol> aChildren
	) {
		var Bindings = new System.Collections.Generic.List<tBinding>();
		CollectPatternBindings(
			aPattern,
			aKind,
			Bindings
		);

		if (Bindings.Count == 0) {
			return mStd.cEmpty;
		}

		if (Bindings.Count == 1) {
			var Binding = Bindings[0];
			return mStream.Stream(
				DocumentSymbol(
					DisplayId(Binding.Id),
					aDetail,
					Binding.Kind,
					aRange,
					Binding.Span,
					aChildren
				)
			);
		}

		return mStream.Stream(Bindings.ToArray()).Map(
			aBinding => DocumentSymbol(
				DisplayId(aBinding.Id),
				aDetail,
				aBinding.Kind,
				aRange,
				aBinding.Span,
				mStd.cEmpty
			)
		);
	}

	private static void
	CollectPatternBindings(
		mSPO_AST.tPatternNode<tSpan> aPattern,
		tSymbolKind aKind,
		System.Collections.Generic.List<tBinding> aBindings
	) {
		switch (aPattern) {
			case mSPO_AST.tTypedPatternNode<tSpan> Pattern: {
				CollectPatternBindings(Pattern.Pattern, aKind, aBindings);
				break;
			}
			case mSPO_AST.tGuardPatternNode<tSpan> Pattern: {
				CollectPatternBindings(Pattern.Pattern, aKind, aBindings);
				break;
			}
			case mSPO_AST.tPrefixPatternNode<tSpan> Pattern: {
				CollectPatternBindings(Pattern.Pattern, aKind, aBindings);
				break;
			}
			case mSPO_AST.tTuplePatternNode<tSpan> Pattern: {
				foreach (var Item in Pattern.Items) {
					CollectPatternBindings(Item, aKind, aBindings);
				}
				break;
			}
			case mSPO_AST.tPairPatternNode<tSpan> Pattern: {
				CollectPatternBindings(Pattern.Tail, aKind, aBindings);
				CollectPatternBindings(Pattern.Head, aKind, aBindings);
				break;
			}
			case mSPO_AST.tRecordPatternNode<tSpan> Pattern: {
				foreach (var Element in Pattern.Elements) {
					CollectPatternBindings(Element.Pattern, aKind, aBindings);
				}
				break;
			}
			case mSPO_AST.tFreeIdPatternNode<tSpan> Pattern: {
				aBindings.Add(Binding(Pattern.Id, Pattern.Pos, aKind));
				break;
			}
			case mSPO_AST.tVarPatternNode<tSpan> Pattern: {
				aBindings.Add(Binding(Pattern.Id, Pattern.Pos, tSymbolKind.Variable));
				break;
			}
			case mSPO_AST.tIdNode<tSpan> Pattern: {
				aBindings.Add(Binding(Pattern.Id, Pattern.Pos, aKind));
				break;
			}
		}
	}

	private static tBool
	TryResolveModule(
		mSPO_AST.tModuleNode<tSpan> aModule,
		tPos aQueryPos,
		System.Collections.Generic.List<tBinding> aScope,
		out tSpan aDefinitionSpan
	) {
		if (
			TryResolvePattern(
				aModule.Import.Pattern,
				aQueryPos,
				aScope,
				out aDefinitionSpan,
				out var Scope
			)
		) {
			return true;
		}

		foreach (var Command in aModule.Commands) {
			if (
				TryResolveCommand(
					Command,
					aQueryPos,
					Scope,
					out aDefinitionSpan,
					out Scope
				)
			) {
				return true;
			}
		}

		return TryResolveExpression(
			aModule.Export.Expression,
			aQueryPos,
			Scope,
			out aDefinitionSpan
		);
	}

	private static tBool
	TryResolveCommand(
		mSPO_AST.tCommandNode<tSpan> aCommand,
		tPos aQueryPos,
		System.Collections.Generic.List<tBinding> aScope,
		out tSpan aDefinitionSpan,
		out System.Collections.Generic.List<tBinding> aNewScope
	) {
		aNewScope = CopyScope(aScope);

		switch (aCommand) {
			case mSPO_AST.tDefNode<tSpan> Def: {
				if (
					TryResolvePattern(
						Def.Des,
						aQueryPos,
						aScope,
						out aDefinitionSpan,
						out aNewScope
					)
				) {
					return true;
				}

				if (
					TryResolveExpression(
						Def.Src,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}

				return false;
			}

			case mSPO_AST.tDefVarNode<tSpan> DefVar: {
				if (
					TryResolveExpression(
						DefVar.Expression,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}

				var Scope = CopyScope(aScope);
				foreach (var MethodCall in DefVar.MethodCalls) {
					if (
						TryResolveMethodCall(
							MethodCall,
							aQueryPos,
							Scope,
							out aDefinitionSpan,
							out Scope
						)
					) {
						aNewScope = Scope;
						return true;
					}
				}

				Scope.Add(Binding(DefVar.Id.Id, DefVar.Id.Pos, tSymbolKind.Variable));
				aNewScope = Scope;

				if (Contains(DefVar.Id.Pos, aQueryPos)) {
					aDefinitionSpan = DefVar.Id.Pos;
					return true;
				}

				break;
			}

			case mSPO_AST.tMethodCallsNode<tSpan> MethodCalls: {
				if (
					TryResolveExpression(
						MethodCalls.Object,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}

				var Scope = CopyScope(aScope);
				foreach (var MethodCall in MethodCalls.MethodCalls) {
					if (
						TryResolveMethodCall(
							MethodCall,
							aQueryPos,
							Scope,
							out aDefinitionSpan,
							out Scope
						)
					) {
						aNewScope = Scope;
						return true;
					}
				}

				aNewScope = Scope;
				break;
			}

			case mSPO_AST.tRecLambdasNode<tSpan> RecLambdas: {
				var Scope = CopyScope(aScope);
				foreach (var Item in RecLambdas.List) {
					Scope.Add(Binding(Item.Id.Id, Item.Id.Pos, GetBindingKind(Item.Lambda, Item.Id.Id)));
					if (Contains(Item.Id.Pos, aQueryPos)) {
						aDefinitionSpan = Item.Id.Pos;
						aNewScope = Scope;
						return true;
					}
				}

				foreach (var Item in RecLambdas.List) {
					if (
						TryResolveExpression(
							Item.Lambda,
							aQueryPos,
							Scope,
							out aDefinitionSpan
						)
					) {
						aNewScope = Scope;
						return true;
					}
				}

				aNewScope = Scope;
				break;
			}

			case mSPO_AST.tReturnIfNode<tSpan> ReturnIf: {
				if (
					TryResolveExpression(
						ReturnIf.Condition,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					) ||
					TryResolveExpression(
						ReturnIf.Result,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}
				break;
			}
		}

		aDefinitionSpan = default;
		return false;
	}

	private static tBool
	TryResolveMethodCall(
		mSPO_AST.tMethodCallNode<tSpan> aMethodCall,
		tPos aQueryPos,
		System.Collections.Generic.List<tBinding> aScope,
		out tSpan aDefinitionSpan,
		out System.Collections.Generic.List<tBinding> aNewScope
	) {
		aNewScope = CopyScope(aScope);

		if (
			TryResolveReference(
				aMethodCall.Method,
				aQueryPos,
				aScope,
				out aDefinitionSpan
			) ||
			TryResolveExpression(
				aMethodCall.Argument,
				aQueryPos,
				aScope,
				out aDefinitionSpan
			)
		) {
			return true;
		}

		if (!aMethodCall.Result.IsSome(out var ResultPattern)) {
			return false;
		}

		return TryResolvePattern(
			ResultPattern,
			aQueryPos,
			aScope,
			out aDefinitionSpan,
			out aNewScope
		);
	}

	private static tBool
	TryResolvePattern(
		mSPO_AST.tPatternNode<tSpan> aPattern,
		tPos aQueryPos,
		System.Collections.Generic.List<tBinding> aScope,
		out tSpan aDefinitionSpan,
		out System.Collections.Generic.List<tBinding> aNewScope
	) {
		aNewScope = CopyScope(aScope);

		switch (aPattern) {
			case mSPO_AST.tTypedPatternNode<tSpan> Pattern: {
				if (
					Pattern.TypeExpression.IsSome(out var TypeExpression) &&
					TryResolveExpression(
						TypeExpression,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}

				return TryResolvePattern(
					Pattern.Pattern,
					aQueryPos,
					aScope,
					out aDefinitionSpan,
					out aNewScope
				);
			}

			case mSPO_AST.tGuardPatternNode<tSpan> Pattern: {
				if (
					TryResolvePattern(
						Pattern.Pattern,
						aQueryPos,
						aScope,
						out aDefinitionSpan,
						out aNewScope
					)
				) {
					return true;
				}

				return TryResolveExpression(
					Pattern.Guard,
					aQueryPos,
					aNewScope,
					out aDefinitionSpan
				);
			}

			case mSPO_AST.tPrefixPatternNode<tSpan> Pattern: {
				return TryResolvePattern(
					Pattern.Pattern,
					aQueryPos,
					aScope,
					out aDefinitionSpan,
					out aNewScope
				);
			}

			case mSPO_AST.tTuplePatternNode<tSpan> Pattern: {
				var Scope = CopyScope(aScope);
				foreach (var Item in Pattern.Items) {
					if (
						TryResolvePattern(
							Item,
							aQueryPos,
							Scope,
							out aDefinitionSpan,
							out Scope
						)
					) {
						aNewScope = Scope;
						return true;
					}
				}

				aNewScope = Scope;
				break;
			}

			case mSPO_AST.tPairPatternNode<tSpan> Pattern: {
				if (
					TryResolvePattern(
						Pattern.Tail,
						aQueryPos,
						aScope,
						out aDefinitionSpan,
						out var TailScope
					) ||
					TryResolvePattern(
						Pattern.Head,
						aQueryPos,
						TailScope,
						out aDefinitionSpan,
						out aNewScope
					)
				) {
					return true;
				}

				break;
			}

			case mSPO_AST.tRecordPatternNode<tSpan> Pattern: {
				var Scope = CopyScope(aScope);
				foreach (var Element in Pattern.Elements) {
					if (
						TryResolvePattern(
							Element.Pattern,
							aQueryPos,
							Scope,
							out aDefinitionSpan,
							out Scope
						)
					) {
						aNewScope = Scope;
						return true;
					}
				}

				aNewScope = Scope;
				break;
			}

			case mSPO_AST.tFreeIdPatternNode<tSpan> Pattern: {
				aNewScope.Add(Binding(Pattern.Id, Pattern.Pos, tSymbolKind.Variable));
				if (Contains(Pattern.Pos, aQueryPos)) {
					aDefinitionSpan = Pattern.Pos;
					return true;
				}
				break;
			}

			case mSPO_AST.tVarPatternNode<tSpan> Pattern: {
				aNewScope.Add(Binding(Pattern.Id, Pattern.Pos, tSymbolKind.Variable));
				if (Contains(Pattern.Pos, aQueryPos)) {
					aDefinitionSpan = Pattern.Pos;
					return true;
				}
				break;
			}

			case mSPO_AST.tIdNode<tSpan> Pattern: {
				aNewScope.Add(Binding(Pattern.Id, Pattern.Pos, tSymbolKind.Variable));
				if (Contains(Pattern.Pos, aQueryPos)) {
					aDefinitionSpan = Pattern.Pos;
					return true;
				}
				break;
			}
		}

		aDefinitionSpan = default;
		return false;
	}

	private static tBool
	TryResolveExpression(
		mSPO_AST.tExpressionNode<tSpan> aExpression,
		tPos aQueryPos,
		System.Collections.Generic.List<tBinding> aScope,
		out tSpan aDefinitionSpan
	) {
		switch (aExpression) {
			case mSPO_AST.tIdNode<tSpan> Id: {
				return TryResolveReference(Id, aQueryPos, aScope, out aDefinitionSpan);
			}

			case mSPO_AST.tLambdaNode<tSpan> Lambda: {
				var Scope = CopyScope(aScope);
				if (
					Lambda.Generic.IsSome(out var GenericPattern) &&
					TryResolvePattern(
						GenericPattern,
						aQueryPos,
						Scope,
						out aDefinitionSpan,
						out Scope
					)
				) {
					return true;
				}

				if (
					TryResolvePattern(
						Lambda.Head,
						aQueryPos,
						Scope,
						out aDefinitionSpan,
						out Scope
					)
				) {
					return true;
				}

				return TryResolveExpression(Lambda.Body, aQueryPos, Scope, out aDefinitionSpan);
			}

			case mSPO_AST.tMethodNode<tSpan> Method: {
				if (
					TryResolvePattern(
						Method.Obj,
						aQueryPos,
						aScope,
						out aDefinitionSpan,
						out var ObjScope
					) ||
					TryResolvePattern(
						Method.Arg,
						aQueryPos,
						ObjScope,
						out aDefinitionSpan,
						out var ArgScope
					)
				) {
					return true;
				}

				return TryResolveExpression(Method.Body, aQueryPos, ArgScope, out aDefinitionSpan);
			}

			case mSPO_AST.tBlockNode<tSpan> Block: {
				var Scope = CopyScope(aScope);
				foreach (var Command in Block.Commands) {
					if (
						TryResolveCommand(
							Command,
							aQueryPos,
							Scope,
							out aDefinitionSpan,
							out Scope
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tCallNode<tSpan> Call: {
				if (
					TryResolveExpression(Call.Func, aQueryPos, aScope, out aDefinitionSpan) ||
					TryResolveExpression(Call.Arg, aQueryPos, aScope, out aDefinitionSpan)
				) {
					return true;
				}
				break;
			}

			case mSPO_AST.tPrefixNode<tSpan> Prefix: {
				return TryResolveExpression(Prefix.Element, aQueryPos, aScope, out aDefinitionSpan);
			}

			case mSPO_AST.tRecordNode<tSpan> Record: {
				foreach (var Element in Record.Elements) {
					if (
						TryResolveExpression(
							Element.Value,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tPairNode<tSpan> Pair: {
				if (
					TryResolveExpression(Pair.Tail, aQueryPos, aScope, out aDefinitionSpan) ||
					TryResolveExpression(Pair.Head, aQueryPos, aScope, out aDefinitionSpan)
				) {
					return true;
				}
				break;
			}

			case mSPO_AST.tTupleNode<tSpan> Tuple: {
				foreach (var Item in Tuple.Items) {
					if (
						TryResolveExpression(
							Item,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tIfNode<tSpan> If: {
				foreach (var Case in If.Cases) {
					if (
						TryResolveExpression(Case.Cond, aQueryPos, aScope, out aDefinitionSpan) ||
						TryResolveExpression(Case.Result, aQueryPos, aScope, out aDefinitionSpan)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tIfMatchNode<tSpan> IfMatch: {
				if (
					TryResolveExpression(
						IfMatch.Expression,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}

				foreach (var Case in IfMatch.Cases) {
					if (
						TryResolvePattern(
							Case.Pattern,
							aQueryPos,
							aScope,
							out aDefinitionSpan,
							out var CaseScope
						) ||
						TryResolveExpression(
							Case.Expression,
							aQueryPos,
							CaseScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tIsNode<tSpan> Is: {
				if (
					TryResolveExpression(
						Is.Expression,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					) ||
					TryResolvePattern(
						Is.Pattern,
						aQueryPos,
						aScope,
						out aDefinitionSpan,
						out _
					)
				) {
					return true;
				}
				break;
			}

			case mSPO_AST.tVarToValNode<tSpan> VarToVal: {
				return TryResolveExpression(VarToVal.Obj, aQueryPos, aScope, out aDefinitionSpan);
			}

			case mSPO_AST.tPipeToRightNode<tSpan> Pipe: {
				if (
					TryResolveExpression(Pipe.Head, aQueryPos, aScope, out aDefinitionSpan)
				) {
					return true;
				}

				foreach (var Item in Pipe.Pipe) {
					if (
						TryResolveExpression(
							Item,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tPipeToLeftNode<tSpan> Pipe: {
				foreach (var Item in Pipe.Pipe) {
					if (
						TryResolveExpression(
							Item,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}

				return TryResolveExpression(Pipe.Head, aQueryPos, aScope, out aDefinitionSpan);
			}

			case mSPO_AST.tVarTypeNode<tSpan> VarType: {
				return TryResolveExpression(VarType.Type, aQueryPos, aScope, out aDefinitionSpan);
			}

			case mSPO_AST.tPrefixTypeNode<tSpan> PrefixType: {
				if (
					TryResolveReference(
						PrefixType.Prefix,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}

				foreach (var Item in PrefixType.Expressions) {
					if (
						TryResolveExpression(
							Item,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tTupleTypeNode<tSpan> TupleType: {
				foreach (var Item in TupleType.ItemTypes) {
					if (
						TryResolveExpression(
							Item,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tPairTypeNode<tSpan> PairType: {
				if (
					TryResolveExpression(PairType.TailType, aQueryPos, aScope, out aDefinitionSpan) ||
					TryResolveExpression(PairType.HeadType, aQueryPos, aScope, out aDefinitionSpan)
				) {
					return true;
				}
				break;
			}

			case mSPO_AST.tSetTypeNode<tSpan> SetType: {
				foreach (var Item in SetType.Expressions) {
					if (
						TryResolveExpression(
							Item,
							aQueryPos,
							aScope,
							out aDefinitionSpan
						)
					) {
						return true;
					}
				}
				break;
			}

			case mSPO_AST.tLambdaTypeNode<tSpan> LambdaType: {
				if (
					TryResolveExpression(LambdaType.EnvType, aQueryPos, aScope, out aDefinitionSpan) ||
					TryResolveExpression(LambdaType.ArgType, aQueryPos, aScope, out aDefinitionSpan) ||
					TryResolveExpression(LambdaType.ResType, aQueryPos, aScope, out aDefinitionSpan)
				) {
					return true;
				}
				break;
			}

			case mSPO_AST.tRecursiveTypeNode<tSpan> RecursiveType: {
				var Scope = CopyScope(aScope);
				Scope.Add(Binding(RecursiveType.HeadType.Id, RecursiveType.HeadType.Pos, tSymbolKind.TypeParameter));
				if (Contains(RecursiveType.HeadType.Pos, aQueryPos)) {
					aDefinitionSpan = RecursiveType.HeadType.Pos;
					return true;
				}

				return TryResolveExpression(RecursiveType.BodyType, aQueryPos, Scope, out aDefinitionSpan);
			}

			case mSPO_AST.tInterfaceTypeNode<tSpan> InterfaceType: {
				var Scope = CopyScope(aScope);
				Scope.Add(Binding(InterfaceType.HeadType.Id, InterfaceType.HeadType.Pos, tSymbolKind.TypeParameter));
				if (Contains(InterfaceType.HeadType.Pos, aQueryPos)) {
					aDefinitionSpan = InterfaceType.HeadType.Pos;
					return true;
				}

				return TryResolveExpression(InterfaceType.BodyType, aQueryPos, Scope, out aDefinitionSpan);
			}

			case mSPO_AST.tGenericTypeNode<tSpan> GenericType: {
				var Scope = CopyScope(aScope);
				Scope.Add(Binding(GenericType.HeadType.Id, GenericType.HeadType.Pos, tSymbolKind.TypeParameter));
				if (Contains(GenericType.HeadType.Pos, aQueryPos)) {
					aDefinitionSpan = GenericType.HeadType.Pos;
					return true;
				}

				return TryResolveExpression(GenericType.BodyType, aQueryPos, Scope, out aDefinitionSpan);
			}

			case mSPO_AST.tGenericApplyTypeNode<tSpan> GenericApplyType: {
				if (
					TryResolveExpression(
						GenericApplyType.GenericType,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					) ||
					TryResolveExpression(
						GenericApplyType.ArgType,
						aQueryPos,
						aScope,
						out aDefinitionSpan
					)
				) {
					return true;
				}
				break;
			}
		}

		aDefinitionSpan = default;
		return false;
	}

	private static tBool
	TryResolveReference(
		mSPO_AST.tIdNode<tSpan> aId,
		tPos aQueryPos,
		System.Collections.Generic.List<tBinding> aScope,
		out tSpan aDefinitionSpan
	) {
		if (
			!Contains(aId.Pos, aQueryPos) ||
			!TryFindBinding(aScope, aId.Id, out var Binding)
		) {
			aDefinitionSpan = default;
			return false;
		}

		aDefinitionSpan = Binding.Span;
		return true;
	}

	private static mStream.tStream<tDocumentSymbol>
	JoinSymbolStreams(
		params System.ReadOnlySpan<mStream.tStream<tDocumentSymbol>> aSymbolStreams
	) => JoinSymbolStreams(
		mStream.Stream(aSymbolStreams)
	);

	private static mStream.tStream<tDocumentSymbol>
	JoinSymbolStreams(
		mStream.tStream<mStream.tStream<tDocumentSymbol>> aSymbolStreams
	) {
		var Result = new System.Collections.Generic.List<tDocumentSymbol>();
		foreach (var Symbols in aSymbolStreams) {
			AddSymbols(Result, Symbols);
		}
		return mStream.Stream(Result.ToArray());
	}

	private static tBinding
	Binding(
		tText aId,
		tSpan aSpan,
		tSymbolKind aKind
	) => new(
		aId,
		aSpan,
		aKind
	);

	private static void
	AddSymbols(
		System.Collections.Generic.List<tDocumentSymbol> aTarget,
		mStream.tStream<tDocumentSymbol> aSymbols
	) {
		foreach (var Symbol in aSymbols) {
			aTarget.Add(Symbol);
		}
	}

	private static System.Collections.Generic.List<tBinding>
	CopyScope(
		System.Collections.Generic.List<tBinding> aScope
	) => [.. aScope];

	private static tBool
	TryFindBinding(
		System.Collections.Generic.List<tBinding> aScope,
		tText aId,
		out tBinding aBinding
	) {
		for (var I = aScope.Count - 1; I >= 0; I -= 1) {
			if (aScope[I].Id == aId) {
				aBinding = aScope[I];
				return true;
			}
		}

		aBinding = default;
		return false;
	}

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tText
	DisplayId(
		tText aId
	) => aId.StartsWith('_')
		? aId[1..]
		: aId;

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tSymbolKind
	GetBindingKind(
		mSPO_AST.tExpressionNode<tSpan> aExpression,
		mMaybe.tMaybe<tText> aId
	) => aId.Match(
		aName => aName.Contains("...")
			? tSymbolKind.Operator
			: aExpression switch {
				mSPO_AST.tLambdaNode<tSpan> => tSymbolKind.Function,
				mSPO_AST.tMethodNode<tSpan> => tSymbolKind.Method,
				_ => tSymbolKind.Variable,
			},
		() => aExpression switch {
			mSPO_AST.tLambdaNode<tSpan> => tSymbolKind.Function,
			mSPO_AST.tMethodNode<tSpan> => tSymbolKind.Method,
			_ => tSymbolKind.Variable,
		}
	);

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static mMaybe.tMaybe<tText>
	TryGetPatternDisplayId(
		mSPO_AST.tPatternNode<tSpan> aPattern
	) => aPattern switch {
		mSPO_AST.tTypedPatternNode<tSpan> Pattern => TryGetPatternDisplayId(Pattern.Pattern),
		mSPO_AST.tFreeIdPatternNode<tSpan> Pattern => Pattern.Id,
		mSPO_AST.tVarPatternNode<tSpan> Pattern => Pattern.Id,
		mSPO_AST.tIdNode<tSpan> Pattern => Pattern.Id,
		_ => mStd.cEmpty,
	};

	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tBool
	Contains(
		tSpan aSpan,
		tPos aPos
	) => aSpan.Start.Id == aPos.Id &&
		mTextParser.ComparePos(aSpan.Start, aPos) <= 0 &&
		mTextParser.ComparePos(aPos, aSpan.End) <= 0;

	[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "navigation should degrade gracefully on parse failures")]
	private static tBool
	TryParseModule(
		tText aCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream,
		[MaybeNullWhen(false)] out mSPO_AST.tModuleNode<tSpan> aModule
	) {
		try {
			aModule = mSPO_Parser.Module.ParseText(aCode, aId, aDebugStream);
			return true;
		} catch {
			aModule = default!;
			return false;
		}
	}
}
