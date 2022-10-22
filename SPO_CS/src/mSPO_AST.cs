//IMPORT mArrayList.cs
//IMPORT mVM_Type.cs

#nullable enable

public static class
mSPO_AST {
	private const tText cDebuggerDisplay = "{this.ToText(10)}";
	
	public interface
	tNode<out tPos> {
		tPos Pos { get; }
	}
	
	public interface
	tMatchItemNode<tPos> : tNode<tPos> {
		mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	public interface
	tExpressionNode<tPos> : tNode<tPos> {
		mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	public interface
	tLiteralNode<tPos> : tExpressionNode<tPos>, tMatchItemNode<tPos> {}
	
	public interface
	tTypeNode<tPos> : tExpressionNode<tPos> {}
	
	public interface
	tCommandNode<tPos> : tNode<tPos> {}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tEmptyNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tFalseNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTrueNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tEmptyTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tBoolTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	} 
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIntTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTypeTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTextNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Value = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIntNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tInt32 Value = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIgnoreMatchNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIdNode<tPos> : tTypeNode<tPos>, tExpressionNode<tPos>, tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Id = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMatchFreeIdNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Id = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMatchTupleNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tMatchNode<tPos>>? Items;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMatchNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tMatchItemNode<tPos> Pattern = default!;
		public mMaybe.tMaybe<tExpressionNode<tPos>> Type;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPrefixNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Prefix = default!;
		public tExpressionNode<tPos> Element = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecordNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<(tIdNode<tPos> Key, tExpressionNode<tPos> Value)>? Elements;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMatchRecordNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<(tIdNode<tPos> Id, tMatchNode<tPos> Match)>? Elements;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMatchPrefixNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Prefix = default!;
		public tMatchNode<tPos> Match = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMatchGuardNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tMatchNode<tPos> Match = default!;
		public tExpressionNode<tPos> Guard = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tLambdaNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mMaybe.tMaybe<tMatchNode<tPos>> Generic;
		public tMatchNode<tPos> Head = default!;
		public tExpressionNode<tPos> Body = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMethodNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tMatchNode<tPos> Obj = default!;
		public tMatchNode<tPos> Arg = default!;
		public tBlockNode<tPos> Body = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tBlockNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tCommandNode<tPos>>? Commands;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tCallNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Func = default!;
		public tExpressionNode<tPos> Arg = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tDefNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tMatchNode<tPos> Des = default!;
		public tExpressionNode<tPos> Src = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecLambdaItemNode<tPos> : tNode<tPos>{
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tMatchFreeIdNode<tPos> Id = default!;
		public tLambdaNode<tPos> Lambda = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecLambdasNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public mStream.tStream<tRecLambdaItemNode<tPos>>? List;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tReturnIfNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public tExpressionNode<tPos> Result = default!;
		public tExpressionNode<tPos> Condition = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIfNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<(tExpressionNode<tPos> Cond, tExpressionNode<tPos> Result)>? Cases;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIfMatchNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Expression = default!;
		public mStream.tStream<(tMatchNode<tPos> Match, tExpressionNode<tPos> Expression)>? Cases;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPrefixTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tIdNode<tPos> Prefix = default!;
		public mStream.tStream<tTypeNode<tPos>>? Expressions;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTupleTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tTypeNode<tPos>>? Expressions;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tSetTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tTypeNode<tPos>>? Expressions;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tLambdaTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tTypeNode<tPos> EnvType = default!;
		public tTypeNode<tPos> ArgType = default!;
		public tTypeNode<tPos> ResType = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecursiveTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tIdNode<tPos> HeadType = default!;
		public tTypeNode<tPos> BodyType = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tInterfaceTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tIdNode<tPos> HeadType = default!;
		public tTypeNode<tPos> BodyType = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tGenericTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tIdNode<tPos> HeadType = default!;
		public tTypeNode<tPos> BodyType = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tDefVarNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tIdNode<tPos> Id = default!;
		public tExpressionNode<tPos> Expression = default!;
		public mStream.tStream<tMethodCallNode<tPos>>? MethodCalls;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tVarToValNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Obj = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMethodCallNode<tPos> : tNode<tPos> {
		public tPos Pos { get; init; }
		public tIdNode<tPos> Method = default!;
		public tExpressionNode<tPos> Argument = default!;
		public mMaybe.tMaybe<tMatchNode<tPos>> Result;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMethodCallsNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Object = default!;
		public mStream.tStream<tMethodCallNode<tPos>>? MethodCalls;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPipeToRightNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Left = default!;
		public tExpressionNode<tPos> Right = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPipeToLeftNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Left = default!;
		public tExpressionNode<tPos> Right = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTupleNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tExpressionNode<tPos>>? Items;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tImportNode<tPos> : tNode<tPos> {
		public tPos Pos { get; init; }
		public tMatchNode<tPos> Match = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tExportNode<tPos> : tNode<tPos> {
		public tPos Pos { get; init; }
		public tExpressionNode<tPos> Expression = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tModuleNode<tPos> : tNode<tPos> {
		public tPos Pos { get; init; }
		public tImportNode<tPos> Import = default!;
		public tExportNode<tPos> Export = default!;
		public mStream.tStream<tCommandNode<tPos>>? Commands;
	}
	
	public static tEmptyNode<tPos>
	Empty<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tFalseNode<tPos>
	False<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tTrueNode<tPos>
	True<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tEmptyTypeNode<tPos>
	EmptyType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tBoolTypeNode<tPos>
	BoolType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tIntTypeNode<tPos>
	IntType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tTypeTypeNode<tPos>
	TypeType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tIntNode<tPos>
	Int<tPos>(
		tPos aPos,
		tInt32 aValue
	) => new() {
		Pos = aPos,
		Value = aValue
	};
	
	public static tTextNode<tPos>
	Text<tPos>(
		tPos aPos,
		tText aValue
	) => new() {
		Pos = aPos,
		Value = aValue
	};
	
	public static tIgnoreMatchNode<tPos>
	IgnoreMatch<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos
	};
	
	public static tIdNode<tPos>
	Id<tPos>(
		tPos aPos,
		tText aId
	) => new() {
		Pos = aPos,
		Id = "_" + aId
	};
	
	public static tMatchFreeIdNode<tPos>
	MatchFreeId<tPos>(
		tPos aPos,
		tText aId
	) => new() {
		Pos = aPos,
		Id = "_" + aId
	};
	
	public static tExpressionNode<tPos>
	Tuple<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>>? aItems
	) => aItems.Take(2).ToArrayList().Size() switch {
		0 => Empty(aPos),
		1 => mStd.Call(
			() => {
				mAssert.IsTrue(aItems.Match(out var Head, out var _));
				return Head;
			}
		),
		_ => new tTupleNode<tPos> {
			Pos = aPos,
			Items = aItems
		},
	};
	
	public static tPrefixTypeNode<tPos>
	PrefixType<tPos>(
		tPos aPos,
		tIdNode<tPos> aPrefix,
		mStream.tStream<tTypeNode<tPos>>? aTypes
	) => new() {
		Pos = aPos,
		Prefix = aPrefix,
		Expressions = aTypes,
	};
	
	public static tTupleTypeNode<tPos>
	TupleType<tPos>(
		tPos aPos,
		mStream.tStream<tTypeNode<tPos>>? aTypes
	) => new() {
		Pos = aPos,
		Expressions = aTypes,
	};
	
	public static tSetTypeNode<tPos>
	SetType<tPos>(
		tPos aPos,
		mStream.tStream<tTypeNode<tPos>>? aTypes
	) => new() {
		Pos = aPos,
		Expressions = aTypes
	};
	
	public static tLambdaTypeNode<tPos>
	LambdaType<tPos>(
		tPos aPos,
		tTypeNode<tPos> aEnvType,
		tTypeNode<tPos> aArgType,
		tTypeNode<tPos> aResType
	) => new() {
		Pos = aPos,
		EnvType = aEnvType,
		ArgType = aArgType,
		ResType = aResType
	};
	
	public static tRecursiveTypeNode<tPos>
	RecursiveType<tPos>(
		tPos aPos,
		tIdNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new() {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	public static tInterfaceTypeNode<tPos>
	InterfaceType<tPos>(
		tPos aPos,
		tIdNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new() {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	public static tGenericTypeNode<tPos>
	GenericType<tPos>(
		tPos aPos,
		tIdNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new() {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	public static tCallNode<tPos>
	Call<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aFunc,
		tExpressionNode<tPos> aArg
	) => new() {
		Pos = aPos,
		Func = aFunc,
		Arg = aArg
	};
	
	public static tPrefixNode<tPos>
	Prefix<tPos>(
		tPos aPos,
		tIdNode<tPos> aPrefix,
		tExpressionNode<tPos> aElement
	) => new() {
		Pos = aPos,
		Prefix = aPrefix.Id,
		Element = aElement
	};
	
	public static tMatchPrefixNode<tPos>
	MatchPrefix<tPos>(
		tPos aPos,
		tIdNode<tPos>aPrefix,
		tMatchNode<tPos> aMatch
	) => new() {
		Pos = aPos,
		Prefix = aPrefix.Id,
		Match = aMatch
	};
	
	public static tRecordNode<tPos>
	Record<tPos>(
		tPos aPos,
		mStream.tStream<(tIdNode<tPos> Key, tExpressionNode<tPos> Value)>? aRecordItems
	) => new() {
		Pos = aPos,
		Elements = aRecordItems
	};
	
	public static tMatchRecordNode<tPos>
	MatchRecord<tPos>(
		tPos aPos,
		mStream.tStream<(tIdNode<tPos> Key, tMatchNode<tPos> Match)>? aRecordItems
	) => new() {
		Pos = aPos,
		Elements = aRecordItems
	};
	
	public static tMatchGuardNode<tPos>
	MatchGuard<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aGuard
	) => new() {
		Pos = aPos,
		Match = aMatch,
		Guard = aGuard
	};
	
	public static tLambdaNode<tPos>
	Lambda<tPos>(
		tPos aPos,
		mMaybe.tMaybe<tMatchNode<tPos>> aStaticMatch,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aBody
	) => new() {
		Pos = aPos,
		Generic = aStaticMatch,
		Head = aMatch,
		Body = aBody
	};
	
	public static tMethodNode<tPos>
	Method<tPos>(
		tPos aPos,
		tMatchNode<tPos> aObjMatch,
		tMatchNode<tPos> aArgMatch,
		tBlockNode<tPos> aBody
	) => new() {
		Pos = aPos,
		Obj = aObjMatch,
		Arg = aArgMatch,
		Body = aBody
	};
	
	public static tRecLambdaItemNode<tPos>
	RecLambdaItem<tPos>(
		tPos aPos,
		tMatchFreeIdNode<tPos> aId,
		tLambdaNode<tPos> aLambda
	) => new() {
		Pos = aPos,
		Id = aId,
		Lambda = aLambda
	};
	
	public static tRecLambdasNode<tPos>
	RecLambdas<tPos>(
		tPos aPos,
		mStream.tStream<tRecLambdaItemNode<tPos>>? aList
	) => new() {
		Pos = aPos,
		List = aList
	};
	
	public static tMatchItemNode<tPos>
	MatchTuple<tPos>(
		tPos aPos,
		mStream.tStream<tMatchNode<tPos>>? aItems
	) => aItems.Take(2).Count() switch {
		0 => throw mError.Error("impossible"),
		1 => aItems.TryFirst().ElseThrow(),
		_ => new tMatchTupleNode<tPos> {
			Pos = aPos,
			Items = aItems
		},
	};
	
	public static tMatchNode<tPos>
	Match<tPos>(
		tPos aPos,
		tMatchItemNode<tPos> aMatch,
		mMaybe.tMaybe<tExpressionNode<tPos>> aType
	) => new() {
		Pos = aPos,
		Pattern = aMatch,
		Type = aType,
		TypeAnnotation = aMatch.TypeAnnotation,
	};
	
	public static tMatchNode<tPos>
	UnTypedMatch<tPos>(
		tPos aPos,
		tMatchItemNode<tPos> aMatch
	) => Match(aPos, aMatch, mStd.cEmpty);
	
	public static tDefNode<tPos>
	Def<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aExpression
	) => new() {
		Pos = aPos,
		Des = aMatch,
		Src = aExpression
	};
	
	public static tReturnIfNode<tPos>
	ReturnIf<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aResult,
		tExpressionNode<tPos> aCondition
	) => new() {
		Pos = aPos,
		Result = aResult,
		Condition = aCondition
	};
	
	public static tIfNode<tPos>
	If<tPos>(
		tPos aPos,
		mStream.tStream<(tExpressionNode<tPos>, tExpressionNode<tPos>)>? aCases
	) => new() {
		Pos = aPos,
		Cases = aCases
	};
	
	public static tIfMatchNode<tPos>
	IfMatch<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<(tMatchNode<tPos>, tExpressionNode<tPos>)>? aCases
	) => new() {
		Pos = aPos,
		Expression = aExpression,
		Cases = aCases
	};
	
	public static tDefVarNode<tPos>
	DefVar<tPos>(
		tPos aPos,
		tIdNode<tPos> aVar,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<tMethodCallNode<tPos>>? aMethodCalls
	) => new() {
		Pos = aPos,
		Id = aVar,
		Expression = aExpression,
		MethodCalls = aMethodCalls
	};
	
	public static tVarToValNode<tPos>
	VarToVal<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aObj
	) => new() {
		Pos = aPos,
		Obj = aObj,
	};
	
	public static tMethodCallsNode<tPos>
	MethodCallStatement<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aObject,
		mStream.tStream<tMethodCallNode<tPos>>? aMethodCalls
	) => new() {
		Pos = aPos,
		Object = aObject,
		MethodCalls = aMethodCalls,
	};
	
	public static tExpressionNode<tPos>
	PipeToRight<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aLeft,
		tExpressionNode<tPos> aRight
	) => new tPipeToRightNode<tPos> {
		Pos = aPos,
		Left = aLeft,
		Right = aRight,
	};
	
	public static tExpressionNode<tPos>
	PipeToLeft<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aLeft,
		tExpressionNode<tPos> aRight
	) => new tPipeToLeftNode<tPos> {
		Pos = aPos,
		Left = aLeft,
		Right = aRight,
	};
	
	public static tMethodCallNode<tPos>
	MethodCall<tPos>(
		tPos aPos,
		tIdNode<tPos> aMethod,
		tExpressionNode<tPos> aArgument,
		mMaybe.tMaybe<tMatchNode<tPos>> aResult
	) => new() {
		Pos = aPos,
		Method = aMethod,
		Argument = aArgument,
		Result = aResult,
	};
	
	public static tBlockNode<tPos>
	Block<tPos>(
		tPos aPos,
		mStream.tStream<tCommandNode<tPos>>? aCommands
	) => new() {
		Pos = aPos,
		Commands = aCommands,
	};
	
	public static tModuleNode<tPos>
	Module<tPos>(
		tPos aPos,
		tImportNode<tPos> aImport,
		mStream.tStream<tCommandNode<tPos>>? aCommands,
		tExportNode<tPos> aExport
	) => new() {
		Pos = aPos,
		Import = aImport,
		Export = aExport,
		Commands = aCommands,
	};
	
	public static tImportNode<tPos>
	Import<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch
	) => new() {
		Pos = aPos,
		Match = aMatch,
	};
	
	public static tExportNode<tPos>
	Export<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression
	) => new() {
		Pos = aPos,
		Expression = aExpression,
	};
	
	public static tBool
	AreEqual<tPos>(
		tNode<tPos> a1,
		tNode<tPos> a2
	) {
		if (ReferenceEquals(a1, a2)) {
			return true;
		}
		if (!Equals(a1.Pos, a2.Pos)) {
			return false;
		}
		
		switch (a1) {
			case tEmptyNode<tPos> _: {
				return a2 is tEmptyNode<tPos>;
			}
			case tTrueNode<tPos> _: {
				return a2 is tTrueNode<tPos>;
			}
			case tFalseNode<tPos> _: {
				return a2 is tFalseNode<tPos>;
			}
			case tIntNode<tPos> Node1: {
				return a2 is tIntNode<tPos> Node2 && Node1.Value == Node2.Value;
			}
			case tEmptyTypeNode<tPos> _: {
				return a2 is tEmptyTypeNode<tPos>;
			}
			case tBoolTypeNode<tPos> _: {
				return a2 is tBoolTypeNode<tPos>;
			}
			case tIntTypeNode<tPos> _: {
				return a2 is tIntTypeNode<tPos>;
			}
			case tTypeTypeNode<tPos> _: {
				return a2 is tTypeTypeNode<tPos>;
			}
			case tTextNode<tPos> Node1: {
				return a2 is tTextNode<tPos> Node2 && Node1.Value == Node2.Value;
			}
			case tIgnoreMatchNode<tPos> _: {
				return a2 is tIgnoreMatchNode<tPos>;
			}
			case tIdNode<tPos> Node1: {
				return a2 is tIdNode<tPos> Node2 && Node1.Id == Node2.Id;
			}
			case tMatchFreeIdNode<tPos> Node1: {
				return a2 is tMatchFreeIdNode<tPos> Node2 && Node1.Id == Node2.Id;
			}
			case tMatchTupleNode<tPos> Node1: {
				return (
					a2 is tMatchTupleNode<tPos> Node2 &&
					mStream.Zip(Node1.Items, Node2.Items).All(a => AreEqual(a._1, a._2))
				);
			}
			case tMatchNode<tPos> Node1: {
				return (
					a2 is tMatchNode<tPos> Node2 &&
					AreEqual(Node1.Pattern, Node2.Pattern) &&
					(
						Node1.Type.Match(
							Some: Type1 => Node2.Type.Match(
								Some: Type2 => AreEqual(Type1, Type2),
								None: () => false
							),
							None: () => Node2.Type.IsNone()
						)
					)
				);
			}
			case tPrefixNode<tPos> _: {
				break;
			}
			case tRecordNode<tPos> Node1: {
				return (
					a2 is tRecordNode<tPos> Node2 &&
					mStream.Zip(
						Node1.Elements,
						Node2.Elements
					).All(
						a => (
							AreEqual(a._1.Key, a._2.Key) &&
							AreEqual(a._1.Value, a._2.Value)
						)
					)
				);
			}
			case tMatchRecordNode<tPos> Node1: {
				return (
					a2 is tMatchRecordNode<tPos> Node2 &&
					mStream.Zip(
						Node1.Elements,
						Node2.Elements
					).All(
						a => (
							AreEqual(a._1.Id, a._2.Id) &&
							AreEqual(a._1.Match, a._2.Match)
						)
					)
				);
			}
			case tMatchPrefixNode<tPos> Node1: {
				return (
					a2 is tMatchPrefixNode<tPos> Node2 &&
					Node1.Prefix == Node2.Prefix &&
					AreEqual(Node1.Match, Node2.Match)
				);
			}
			case tMatchGuardNode<tPos> _: {
				break;
			}
			case tLambdaNode<tPos> Node1: {
				return (
					a2 is tLambdaNode<tPos> Node2 &&
					AreEqual(Node1.Head , Node2.Head) &&
					AreEqual(Node1.Body, Node2.Body)
				);
			}
			case tMethodNode<tPos> _: {
				break;
			}
			case tBlockNode<tPos> _: {
				break;
			}
			case tCallNode<tPos> Node1: {
				return (
					a2 is tCallNode<tPos> Node2 &&
					AreEqual(Node1.Func, Node2.Func) &&
					AreEqual(Node1.Arg, Node2.Arg)
				);
			}
			case tDefNode<tPos> Node1: {
				return (
					a2 is tDefNode<tPos> Node2 &&
					AreEqual(Node1.Src, Node2.Src) &&
					AreEqual(Node1.Des, Node2.Des)
				);
			}
			case tRecLambdaItemNode<tPos> Node1: {
				return (
					a2 is tRecLambdaItemNode<tPos> Node2 &&
					AreEqual(Node1.Id, Node2.Id) &&
					AreEqual(Node1.Lambda, Node2.Lambda)
				);
			}
			case tRecLambdasNode<tPos> Node1: {
				return (
					a2 is tRecLambdasNode<tPos> Node2 &&
					mStream.Zip(Node1.List, Node2.List).All(a => AreEqual(a._1, a._2))
				);
			}
			case tReturnIfNode<tPos> _: {
				break;
			}
			case tIfNode<tPos> _: {
				break;
			}
			case tIfMatchNode<tPos> _: {
				break;
			}
			case tPrefixTypeNode<tPos> _: {
				break;
			}
			case tTupleTypeNode<tPos> _: {
				break;
			}
			case tSetTypeNode<tPos> _: {
				break;
			}
			case tLambdaTypeNode<tPos> _: {
				break;
			}
			case tRecursiveTypeNode<tPos> _: {
				break;
			}
			case tInterfaceTypeNode<tPos> _: {
				break;
			}
			case tGenericTypeNode<tPos> _: {
				break;
			}
			case tDefVarNode<tPos> _: {
				break;
			}
			case tVarToValNode<tPos> Node1: {
				return (
					a2 is tVarToValNode<tPos> Node2 &&
					AreEqual(Node1.Obj, Node2.Obj)
				);
			}
			case tMethodCallNode<tPos> Node1: {
				return (
					a2 is tMethodCallNode<tPos> Node2 &&
					AreEqual(Node1.Method, Node2.Method) &&
					AreEqual(Node1.Argument, Node2.Argument) &&
					(
						Node1.Result.IsSome(out var Result1)
						? (Node2.Result.IsSome(out var Result2) && AreEqual(Result1, Result2))
						: Node2.Result.IsNone()
					)
				);
			}
			case tMethodCallsNode<tPos> Node1: {
				return (
					a2 is tMethodCallsNode<tPos> Node2 &&
					AreEqual(Node1.Object, Node2.Object) &&
					mStream.Zip(Node1.MethodCalls, Node2.MethodCalls).All(a => AreEqual(a._1, a._2))
				);
			}
			case tPipeToRightNode<tPos> _: {
				break;
			}
			case tPipeToLeftNode<tPos> _: {
				break;
			}
			case tTupleNode<tPos> Node1: {
				return (
					a2 is tTupleNode<tPos> Node2 &&
					mStream.Zip(Node1.Items, Node2.Items).All(a => AreEqual(a._1, a._2))
				);
			}
			case tImportNode<tPos> _: {
				break;
			}
			case tExportNode<tPos> _: {
				break;
			}
			case tModuleNode<tPos> _: {
				break;
			}
			default: {
				break;
			}
		}
		throw mError.Error("not implemented: " + a1.GetType().Name);
	}
	
	public static tText
	ToText<t>(
		this tNode<t> aNode,
		tInt32 aLimit
	) {
		if (aLimit == 0) {
			return "(...)";
		}
		
		aLimit -= 1;
		return aNode switch {
			// Expressions
			tIdNode<t> Node => Node.Id,
			tEmptyNode<t> Node => "()",
			tTrueNode<t> Node => "#True",
			tFalseNode<t> Node => "#False",
			tIntNode<t> Node => "" + Node.Value,
			tTextNode<t> Node => $"\"{Node.Value}\"",
			tPrefixNode<t> Node => $"(#{Node.Prefix} {Node.Element.ToText(aLimit)})",
			tTupleNode<t> Node => $"({Node.Items.Map(a => a.ToText(aLimit)).Join((a1, a2) => a1 + ", " + a2, "")})",
			tLambdaNode<t> Node => $"({Node.Head.ToText(aLimit)} => {Node.Body.ToText(aLimit)})",
			tCallNode<t> Node => $"(.{Node.Func.ToText(aLimit)} {Node.Arg.ToText(aLimit)})",
			tMethodNode<t> Node => $"({Node.Obj.ToText(aLimit)} : {Node.Arg.ToText(aLimit)} => {Node.Body.ToText(aLimit)})",
			tIfMatchNode<t> Node => (
				$"§If {Node.Expression} Match " + Node.Cases.Map(
					a => a.Match.ToText(aLimit) + " => " + a.Expression.ToText(aLimit)
				).Reduce(
					"",
					(a1, a2) => a1 + "; " + a2
				)
			),
			// Matches
			tMatchNode<t> Node => (
				Node.Type.Match(
					Some: Type => $"({Node.Pattern.ToText(aLimit)} € {Type.ToText(aLimit)})",
					None: () => Node.Pattern.ToText(aLimit)
				)
			),
			tIgnoreMatchNode<t> Node => "_",
			tMatchFreeIdNode<t> Node => "§DEF " + Node.Id,
			tMatchPrefixNode<t> Node => $"(#{Node.Prefix} {Node.Match.ToText(aLimit)})",
			tMatchTupleNode<t> Node => $"({Node.Items.Map(a => a.ToText(aLimit)).Join((a1, a2) => a1 + ", " + a2, "")})",
			tMatchGuardNode<t> Node => $"({Node.Match.ToText(aLimit)} & {Node.Guard.ToText(aLimit)})",
			// Types
			tEmptyTypeNode<t> Node => "[]",
			tBoolTypeNode<t> Node => "§Bool",
			tIntTypeNode<t> Node => "§Int",
			tTypeTypeNode<t> Node => "§Type",
			tPrefixTypeNode<t> Node => $"[#{Node.Prefix} {Node.Expressions.Map(a => a.ToText(aLimit)).Join((a1, a2) => a1 + ", " + a2, "")}]",
			tTupleTypeNode<t> Node => $"[{Node.Expressions.Map(a => a.ToText(aLimit)).Join((a1, a2) => a1 + ", " + a2, "")}]",
			tSetTypeNode<t> Node => $"[{Node.Expressions.Map(a => a.ToText(aLimit)).Join((a1, a2) => a1 + " | " + a2, "")}]",
			// Commands
			tBlockNode<t> Node => "{...}",
			tMethodCallsNode<t> Node => $"{Node.Object.ToText(aLimit)} : {Node.MethodCalls.Map(a => a.ToText(aLimit)).Join((a1, a2) => a1 + ", " + a2, "")} .",
			tMethodCallNode<t> Node => $"{Node.Method.ToText(aLimit)} {Node.Argument.ToText(aLimit)} => {Node.Result.Match(Some: a => a.ToText(aLimit), None: () => "()")}",
			tReturnIfNode<t> Node => $"§Return {Node.Result.ToText(aLimit)} If {Node.Condition.ToText(aLimit)}",
			tDefNode<t> Node => $"§Def {Node.Des.ToText(aLimit)} = {Node.Src.ToText(aLimit)}",
			_ => "(???)",
		};
	}
}
