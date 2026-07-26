#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mArrayList.cs
#:ref Common/mAssert.cs
#:ref Common/mStream.cs
#:ref Common/mMaybe.cs
#:ref Common/mError.cs
#:ref mVM_Type.cs

public static class
mSPO_AST {
	private const tText cDebuggerDisplay = "{this.ToText()}";
	
	public interface
	tNode<out tPos> {
		tPos Pos { get; }
	}
	
	public interface
	tPatternNode<tPos> : tNode<tPos> {
		mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	public interface
	tExpressionNode<tPos> : tNode<tPos> {
		mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	public interface
	tLiteralNode<tPos> : tExpressionNode<tPos>, tPatternNode<tPos> {}
	
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
	tFalseNode<tPos> : tLiteralNode<tPos>, tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTrueNode<tPos> : tLiteralNode<tPos>, tTypeNode<tPos> {
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
	tIntTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tCharTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTextTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tAnyTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
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
	tCharNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tChar Value = default!;
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
	tIgnorePatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIdNode<tPos> : tTypeNode<tPos>, tExpressionNode<tPos>, tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Id = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tFreeIdPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Id = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tVarPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Id = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTuplePatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tPatternNode<tPos>> Items;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPairPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tPatternNode<tPos> Tail = default!;
		public tPatternNode<tPos> Head = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTypedPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tPatternNode<tPos> Pattern = default!;
		public mMaybe.tMaybe<tExpressionNode<tPos>> TypeExpression;
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
		public mStream.tStream<(tIdNode<tPos> Key, tExpressionNode<tPos> Value)> Elements;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecordPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<(tIdNode<tPos> Id, tPatternNode<tPos> Pattern)> Elements;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPrefixPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tText Prefix = default!;
		public tPatternNode<tPos> Pattern = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tGuardPatternNode<tPos> : tPatternNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tPatternNode<tPos> Pattern = default!;
		public tExpressionNode<tPos> Guard = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tLambdaNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mMaybe.tMaybe<tPatternNode<tPos>> Generic;
		public tPatternNode<tPos> Head = default!;
		public tExpressionNode<tPos> Body = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMethodNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tPatternNode<tPos> Obj = default!;
		public tPatternNode<tPos> Arg = default!;
		public tBlockNode<tPos> Body = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tBlockNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tCommandNode<tPos>> Commands;
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
		public tPatternNode<tPos> Des = default!;
		public tExpressionNode<tPos> Src = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecLambdaItemNode<tPos> : tNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tFreeIdPatternNode<tPos> Id = default!;
		public tLambdaNode<tPos> Lambda = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecLambdasNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public mStream.tStream<tRecLambdaItemNode<tPos>> List;
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
		public mStream.tStream<(tExpressionNode<tPos> Cond, tExpressionNode<tPos> Result)> Cases;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIfMatchNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Expression = default!;
		public mStream.tStream<(tPatternNode<tPos> Pattern, tExpressionNode<tPos> Expression)> Cases;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tIsNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Expression = default!;
		public tPatternNode<tPos> Pattern = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPrefixTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tIdNode<tPos> Prefix = default!;
		public mStream.tStream<tTypeNode<tPos>> Expressions;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tVarTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tTypeNode<tPos> Type;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTupleTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tTypeNode<tPos>> ItemTypes;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPairTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tTypeNode<tPos> TailType = default!;
		public tTypeNode<tPos> HeadType = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tRecordTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<(tIdNode<tPos> Key, tTypeNode<tPos> Type)> Elements;
	}

	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tSetTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tTypeNode<tPos>> Expressions;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tProcTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tTypeNode<tPos> ObjType = default!;
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
	tGenericApplyTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tTypeNode<tPos> GenericType = default!;
		public tTypeNode<tPos> ArgType = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tDefVarNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public tIdNode<tPos> Id = default!;
		public tExpressionNode<tPos> Expression = default!;
		public mStream.tStream<tMethodCallNode<tPos>> MethodCalls;
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
		public mMaybe.tMaybe<tPatternNode<tPos>> Result;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tMethodCallsNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; init; }
		public tExpressionNode<tPos> Object = default!;
		public mStream.tStream<tMethodCallNode<tPos>> MethodCalls;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPipeToRightNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Head = default!;
		public mStream.tStream<tExpressionNode<tPos>> Pipe = mStd.cEmpty;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPipeToLeftNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tExpressionNode<tPos>> Pipe = mStd.cEmpty;
		public tExpressionNode<tPos> Head = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tTupleNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public mStream.tStream<tExpressionNode<tPos>> Items;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tPairNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; init; }
		public mMaybe.tMaybe<mVM_Type.tType> TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Tail = default!;
		public tExpressionNode<tPos> Head = default!;
	}
	
	[DebuggerDisplay(cDebuggerDisplay)]
	public sealed record
	tImportNode<tPos> : tNode<tPos> {
		public tPos Pos { get; init; }
		public tPatternNode<tPos> Pattern = default!;
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
		public mStream.tStream<tCommandNode<tPos>> Commands;
	}
	
	public static tEmptyNode<tPos>
	Empty<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tFalseNode<tPos>
	False<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tTrueNode<tPos>
	True<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tEmptyTypeNode<tPos>
	EmptyType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tIntTypeNode<tPos>
	IntType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tCharTypeNode<tPos>
	CharType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tTextTypeNode<tPos>
	TextType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tAnyTypeNode<tPos>
	AnyType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tTypeTypeNode<tPos>
	TypeType<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tIntNode<tPos>
	Int<tPos>(
		tPos aPos,
		tInt32 aValue
	) => new() {
		Pos = aPos,
		Value = aValue,
	};
	
	public static tCharNode<tPos>
	Char<tPos>(
		tPos aPos,
		tChar aValue
	) => new() {
		Pos = aPos,
		Value = aValue,
	};
	
	public static tTextNode<tPos>
	Text<tPos>(
		tPos aPos,
		tText aValue
	) => new() {
		Pos = aPos,
		Value = aValue,
	};
	
	public static tIgnorePatternNode<tPos>
	IgnorePattern<tPos>(
		tPos aPos
	) => new() {
		Pos = aPos,
	};
	
	public static tIdNode<tPos>
	Id<tPos>(
		tPos aPos,
		tText aId
	) => new() {
		Pos = aPos,
		Id = "_" + aId,
	};
	
	public static tFreeIdPatternNode<tPos>
	FreeIdPattern<tPos>(
		tPos aPos,
		tText aId
	) => new() {
		Pos = aPos,
		Id = "_" + aId,
	};
	
	public static tVarPatternNode<tPos>
	VarPattern<tPos>(
		tPos aPos,
		tText aId
	) => new() {
		Pos = aPos,
		Id = "_" + aId,
	};
	
	public static tExpressionNode<tPos>
	Tuple<tPos>(
		tPos aPos,
		System.Span<tExpressionNode<tPos>> aItems
	) => Tuple(aPos, mStream.Stream(aItems));
	
	public static tExpressionNode<tPos>
	Tuple<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>> aItems
	) => aItems.Take(2).ToArrayList().Size switch {
		0 => Empty(aPos),
		1 => mStd.Call(
			() => {
				mAssert.IsTrue(aItems.Is(out var Head, out var _));
				return Head;
			}
		),
		_ => new tTupleNode<tPos> {
			Pos = aPos,
			Items = aItems
		},
	};
	
	public static tPairNode<tPos>
	Pair<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aTail,
		tExpressionNode<tPos> aHead
	) => new() {
		Pos = aPos,
		Tail = aTail,
		Head = aHead,
	};
	
	public static tPrefixTypeNode<tPos>
	PrefixType<tPos>(
		tPos aPos,
		tIdNode<tPos> aPrefix,
		mStream.tStream<tTypeNode<tPos>> aTypes
	) => new() {
		Pos = aPos,
		Prefix = aPrefix,
		Expressions = aTypes,
	};
	
	public static tVarTypeNode<tPos>
	VarType<tPos>(
		tPos aPos,
		tTypeNode<tPos> aType
	) => new() {
		Pos = aPos,
		Type = aType,
	};
	
	public static tTupleTypeNode<tPos>
	TupleType<tPos>(
		tPos aPos,
		mStream.tStream<tTypeNode<tPos>> aTypes
	) => new() {
		Pos = aPos,
		ItemTypes = aTypes,
	};
	
	public static tPairTypeNode<tPos>
	PairType<tPos>(
		tPos aPos,
		tTypeNode<tPos> aTailType,
		tTypeNode<tPos> aHeadType
	) => new() {
		Pos = aPos,
		TailType = aTailType,
		HeadType = aHeadType,
	};
	
	public static tRecordTypeNode<tPos>
	RecordType<tPos>(
		tPos aPos,
		mStream.tStream<(tIdNode<tPos> Key, tTypeNode<tPos> Type)> aElements
	) => new() {
		Pos = aPos,
		Elements = aElements,
	};

	public static tSetTypeNode<tPos>
	SetType<tPos>(
		tPos aPos,
		mStream.tStream<tTypeNode<tPos>> aTypes
	) => new() {
		Pos = aPos,
		Expressions = aTypes,
	};
	
	public static tProcTypeNode<tPos>
	ProcType<tPos>(
		tPos aPos,
		tTypeNode<tPos> aObjType,
		tTypeNode<tPos> aArgType,
		tTypeNode<tPos> aResType
	) => new() {
		Pos = aPos,
		ObjType = aObjType,
		ArgType = aArgType,
		ResType = aResType,
	};
	
	public static tRecursiveTypeNode<tPos>
	RecursiveType<tPos>(
		tPos aPos,
		tIdNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new() {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType,
	};
	
	public static tInterfaceTypeNode<tPos>
	InterfaceType<tPos>(
		tPos aPos,
		tIdNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new() {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType,
	};
	
	public static tGenericTypeNode<tPos>
	GenericType<tPos>(
		tPos aPos,
		tIdNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new() {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType,
	};
	
	public static tGenericApplyTypeNode<tPos>
	GenericApplyType<tPos>(
		tPos aPos,
		tTypeNode<tPos> aGenericType,
		tTypeNode<tPos> aArgType
	) => new() {
		Pos = aPos,
		GenericType = aGenericType,
		ArgType = aArgType,
	};
	
	public static tCallNode<tPos>
	Call<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aFunc,
		tExpressionNode<tPos> aArg
	) => new() {
		Pos = aPos,
		Func = aFunc,
		Arg = aArg,
	};
	
	public static tPrefixNode<tPos>
	Prefix<tPos>(
		tPos aPos,
		tText aPrefix,
		tExpressionNode<tPos> aElement
	) => new() {
		Pos = aPos,
		Prefix = aPrefix,
		Element = aElement,
	};
	
	public static tPrefixPatternNode<tPos>
	PrefixPattern<tPos>(
		tPos aPos,
		tText aPrefix,
		tPatternNode<tPos> aPattern
	) => new() {
		Pos = aPos,
		Prefix = aPrefix,
		Pattern = aPattern,
	};
	
	public static tRecordNode<tPos>
	Record<tPos>(
		tPos aPos,
		System.Span<(tIdNode<tPos> Key, tExpressionNode<tPos> Value)> aRecordItems
	) => Record(aPos, mStream.Stream(aRecordItems));
	
	public static tRecordNode<tPos>
	Record<tPos>(
		tPos aPos,
		mStream.tStream<(tIdNode<tPos> Key, tExpressionNode<tPos> Value)> aRecordItems
	) => new() {
		Pos = aPos,
		Elements = aRecordItems,
	};
	
	public static tRecordPatternNode<tPos>
	RecordPattern<tPos>(
		tPos aPos,
		mStream.tStream<(tIdNode<tPos> Key, tPatternNode<tPos> Pattern)> aRecordItems
	) => new() {
		Pos = aPos,
		Elements = aRecordItems,
	};
	
	public static tGuardPatternNode<tPos>
	GuardPattern<tPos>(
		tPos aPos,
		tPatternNode<tPos> aPattern,
		tExpressionNode<tPos> aGuard
	) => new() {
		Pos = aPos,
		Pattern = aPattern,
		Guard = aGuard,
	};
	
	public static tLambdaNode<tPos>
	Lambda<tPos>(
		tPos aPos,
		mMaybe.tMaybe<tPatternNode<tPos>> aStaticPattern,
		tPatternNode<tPos> aPattern,
		tExpressionNode<tPos> aBody
	) => new() {
		Pos = aPos,
		Generic = aStaticPattern,
		Head = aPattern,
		Body = aBody,
	};
	
	public static tMethodNode<tPos>
	Method<tPos>(
		tPos aPos,
		tPatternNode<tPos> aObjPattern,
		tPatternNode<tPos> aArgPattern,
		tBlockNode<tPos> aBody
	) => new() {
		Pos = aPos,
		Obj = aObjPattern,
		Arg = aArgPattern,
		Body = aBody,
	};
	
	public static tRecLambdaItemNode<tPos>
	RecLambdaItem<tPos>(
		tPos aPos,
		tFreeIdPatternNode<tPos> aId,
		tLambdaNode<tPos> aLambda
	) => new() {
		Pos = aPos,
		Id = aId,
		Lambda = aLambda,
	};
	
	public static tRecLambdasNode<tPos>
	RecLambdas<tPos>(
		tPos aPos,
		mStream.tStream<tRecLambdaItemNode<tPos>> aList
	) => new() {
		Pos = aPos,
		List = aList,
	};
	
	public static tPatternNode<tPos>
	TuplePattern<tPos>(
		tPos aPos,
		mStream.tStream<tPatternNode<tPos>> aItems
	) => aItems.Take(2).Count() switch {
		0 => throw mError.Error("impossible"),
		1 => aItems.TryFirst().AssertNotEmpty(),
		_ => new tTuplePatternNode<tPos> {
			Pos = aPos,
			Items = aItems,
		},
	};
	
	public static tPairPatternNode<tPos>
	PairPattern<tPos>(
		tPos aPos,
		tPatternNode<tPos> aTail,
		tPatternNode<tPos> aHead
	) => new() {
		Pos = aPos,
		Tail = aTail,
		Head = aHead,
	};
	
	public static tTypedPatternNode<tPos>
	Pattern<tPos>(
		tPos aPos,
		tPatternNode<tPos> aPattern,
		mMaybe.tMaybe<tExpressionNode<tPos>> aType
	) => new() {
		Pos = aPos,
		Pattern = aPattern,
		TypeExpression = aType,
		TypeAnnotation = aPattern.TypeAnnotation,
	};
	
	public static tTypedPatternNode<tPos>
	UnTypedPattern<tPos>(
		tPos aPos,
		tPatternNode<tPos> aPattern
	) => Pattern(aPos, aPattern, mStd.cEmpty);
	
	public static tDefNode<tPos>
	Def<tPos>(
		tPos aPos,
		tPatternNode<tPos> aPattern,
		tExpressionNode<tPos> aExpression
	) => new() {
		Pos = aPos,
		Des = aPattern,
		Src = aExpression,
	};
	
	public static tReturnIfNode<tPos>
	ReturnIf<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aCondition,
		tExpressionNode<tPos> aResult
	) => new() {
		Pos = aPos,
		Result = aResult,
		Condition = aCondition,
	};
	
	public static tIfNode<tPos>
	If<tPos>(
		tPos aPos,
		mStream.tStream<(tExpressionNode<tPos>, tExpressionNode<tPos>)> aCases
	) => new() {
		Pos = aPos,
		Cases = aCases,
	};
	
	public static tIfMatchNode<tPos>
	IfMatch<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<(tPatternNode<tPos>, tExpressionNode<tPos>)> aCases
	) => new() {
		Pos = aPos,
		Expression = aExpression,
		Cases = aCases,
	};
	
	public static tIsNode<tPos>
	Is<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aValue,
		tPatternNode<tPos> aPattern
	) => new() {
		Pos = aPos,
		Expression = aValue,
		Pattern = aPattern,
	};
	
	public static tDefVarNode<tPos>
	DefVar<tPos>(
		tPos aPos,
		tIdNode<tPos> aVar,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<tMethodCallNode<tPos>> aMethodCalls
	) => new() {
		Pos = aPos,
		Id = aVar,
		Expression = aExpression,
		MethodCalls = aMethodCalls,
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
		mStream.tStream<tMethodCallNode<tPos>> aMethodCalls
	) => new() {
		Pos = aPos,
		Object = aObject,
		MethodCalls = aMethodCalls,
	};
	
	public static tExpressionNode<tPos>
	PipeToRight<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aHead,
		mStream.tStream<tExpressionNode<tPos>> aPipe
	) => new tPipeToRightNode<tPos> {
		Pos = aPos,
		Head = aHead,
		Pipe = aPipe,
	};
	
	public static tExpressionNode<tPos>
	PipeToLeft<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>> aPipe,
		tExpressionNode<tPos> aHead
	) => new tPipeToLeftNode<tPos> {
		Pos = aPos,
		Pipe = aPipe,
		Head = aHead,
	};
	
	public static tMethodCallNode<tPos>
	MethodCall<tPos>(
		tPos aPos,
		tIdNode<tPos> aMethod,
		tExpressionNode<tPos> aArgument,
		mMaybe.tMaybe<tPatternNode<tPos>> aResult
	) => new() {
		Pos = aPos,
		Method = aMethod,
		Argument = aArgument,
		Result = aResult,
	};
	
	public static tBlockNode<tPos>
	Block<tPos>(
		tPos aPos,
		mStream.tStream<tCommandNode<tPos>> aCommands
	) => new() {
		Pos = aPos,
		Commands = aCommands,
	};
	
	public static tModuleNode<tPos>
	Module<tPos>(
		tPos aPos,
		tImportNode<tPos> aImport,
		mStream.tStream<tCommandNode<tPos>> aCommands,
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
		tPatternNode<tPos> aPattern
	) => new() {
		Pos = aPos,
		Pattern = aPattern,
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
			case tEmptyNode<tPos>: {
				return a2 is tEmptyNode<tPos>;
			}
			case tTrueNode<tPos>: {
				return a2 is tTrueNode<tPos>;
			}
			case tFalseNode<tPos>: {
				return a2 is tFalseNode<tPos>;
			}
			case tIntNode<tPos> Node1: {
				return a2 is tIntNode<tPos> Node2 && Node1.Value == Node2.Value;
			}
			case tEmptyTypeNode<tPos>: {
				return a2 is tEmptyTypeNode<tPos>;
			}
			case tIntTypeNode<tPos>: {
				return a2 is tIntTypeNode<tPos>;
			}
			case tCharTypeNode<tPos>: {
				return a2 is tCharTypeNode<tPos>;
			}
			case tTextTypeNode<tPos>: {
				return a2 is tTextTypeNode<tPos>;
			}
			case tTypeTypeNode<tPos>: {
				return a2 is tTypeTypeNode<tPos>;
			}
			case tPairPatternNode<tPos> Node1: {
				return (
					a2 is tPairPatternNode<tPos> Node2 &&
					AreEqual(Node1.Tail, Node2.Tail) &&
					AreEqual(Node1.Head, Node2.Head)
				);
			}
			case tCharNode<tPos> Node1: {
				return a2 is tCharNode<tPos> Node2 && Node1.Value == Node2.Value;
			}
			case tTextNode<tPos> Node1: {
				return a2 is tTextNode<tPos> Node2 && Node1.Value == Node2.Value;
			}
			case tIgnorePatternNode<tPos>: {
				return a2 is tIgnorePatternNode<tPos>;
			}
			case tIdNode<tPos> Node1: {
				return a2 is tIdNode<tPos> Node2 && Node1.Id == Node2.Id;
			}
			case tFreeIdPatternNode<tPos> Node1: {
				return a2 is tFreeIdPatternNode<tPos> Node2 && Node1.Id == Node2.Id;
			}
			case tVarPatternNode<tPos> Node1: {
				return a2 is tVarPatternNode<tPos> Node2 && Node1.Id == Node2.Id;
			}
			case tTuplePatternNode<tPos> Node1: {
				return (
					a2 is tTuplePatternNode<tPos> Node2 &&
					mStream.ZipExtend(Node1.Items, Node2.Items).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1, a2)
						)
					)
				);
			}
			case tTypedPatternNode<tPos> Node1: {
				return (
					a2 is tTypedPatternNode<tPos> Node2 &&
					AreEqual(Node1.Pattern, Node2.Pattern) &&
					(
						Node1.TypeExpression.Match(
							Type1 => Node2.TypeExpression.Match(
								Type2 => AreEqual(Type1, Type2),
								() => false
							),
							() => Node2.TypeExpression.IsNone()
						)
					)
				);
			}
			case tPrefixNode<tPos>: {
				break;
			}
			case tRecordNode<tPos> Node1: {
				return (
					a2 is tRecordNode<tPos> Node2 &&
					mStream.ZipExtend(
						Node1.Elements,
						Node2.Elements
					).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1.Key, a2.Key) &&
							AreEqual(a1.Value, a2.Value)
						)
					)
				);
			}
			case tRecordPatternNode<tPos> Node1: {
				return (
					a2 is tRecordPatternNode<tPos> Node2 &&
					mStream.ZipExtend(
						Node1.Elements,
						Node2.Elements
					).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1.Id, a2.Id) &&
							AreEqual(a1.Pattern, a2.Pattern)
						)
					)
				);
			}
			case tPrefixPatternNode<tPos> Node1: {
				return (
					a2 is tPrefixPatternNode<tPos> Node2 &&
					Node1.Prefix == Node2.Prefix &&
					AreEqual(Node1.Pattern, Node2.Pattern)
				);
			}
			case tGuardPatternNode<tPos>: {
				break;
			}
			case tLambdaNode<tPos> Node1: {
				return (
					a2 is tLambdaNode<tPos> Node2 &&
					AreEqual(Node1.Head , Node2.Head) &&
					AreEqual(Node1.Body, Node2.Body)
				);
			}
			case tMethodNode<tPos>: {
				break;
			}
			case tBlockNode<tPos>: {
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
					mStream.ZipExtend(Node1.List, Node2.List).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1, a2)
						)
					)
				);
			}
			case tReturnIfNode<tPos>: {
				break;
			}
			case tIfNode<tPos>: {
				break;
			}
			case tIfMatchNode<tPos>: {
				break;
			}
			case tIsNode<tPos> Node1: {
				return (
					a2 is tIsNode<tPos> Node2 &&
					AreEqual(Node1.Expression, Node2.Expression) &&
					AreEqual(Node1.Pattern, Node2.Pattern)
				);
			}
			case tPrefixTypeNode<tPos>: {
				break;
			}
			case tTupleTypeNode<tPos>: {
				break;
			}
			case tRecordTypeNode<tPos> Node1: {
				return (
					a2 is tRecordTypeNode<tPos> Node2 &&
					mStream.ZipExtend(Node1.Elements, Node2.Elements).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1.Key, a2.Key) &&
							AreEqual(a1.Type, a2.Type)
						)
					)
				);
			}
			case tSetTypeNode<tPos>: {
				break;
			}
			case tProcTypeNode<tPos>: {
				break;
			}
			case tRecursiveTypeNode<tPos>: {
				break;
			}
			case tInterfaceTypeNode<tPos>: {
				break;
			}
			case tGenericTypeNode<tPos>: {
				break;
			}
			case tDefVarNode<tPos>: {
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
					mStream.ZipExtend(Node1.MethodCalls, Node2.MethodCalls).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1, a2)
						)
					)
				);
			}
			case tPipeToRightNode<tPos>: {
				break;
			}
			case tPipeToLeftNode<tPos>: {
				break;
			}
			case tTupleNode<tPos> Node1: {
				return (
					a2 is tTupleNode<tPos> Node2 &&
					mStream.ZipExtend(Node1.Items, Node2.Items).All(
						__ => (
							__._1.IsSome(out var a1) &&
							__._2.IsSome(out var a2) &&
							AreEqual(a1, a2)
						)
					)
				);
			}
			case tPairNode<tPos> Node1: {
				return (
					a2 is tPairNode<tPos> Node2 &&
					AreEqual(Node1.Tail, Node2.Tail) &&
					AreEqual(Node1.Head, Node2.Head)
				);
			}
			case tImportNode<tPos>: {
				break;
			}
			case tExportNode<tPos>: {
				break;
			}
			case tModuleNode<tPos>: {
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
		tText aIndent = "\n"
	) {
		tText __;
		tText ____;
		if (aIndent.StartsWith('\n')) {
			var OneLiner = aNode.ToText("");
			if (OneLiner.Length <= 80) {
				return OneLiner;
			}
			
			__ = aIndent;
			____ = aIndent + "  ";
		} else {
			__ = "";
			____ = " ";
		}
		
		return aNode switch {
			// Expressions
			tIdNode<t> Node => Node.Id,
			tEmptyNode<t> Node => "()",
			tTrueNode<t> Node => "#TRUE",
			tFalseNode<t> Node => "#FALSE",
			tIntNode<t> Node => "" + Node.Value,
			tCharNode<t> Node => $"§{Node.Value}",
			tTextNode<t> Node => $"\"{Node.Value}\"",
			tPrefixNode<t> Node => $"({____}#{Node.Prefix} {Node.Element.ToText(____)}{__})",
			tVarToValNode<t> Node => $"({____}({__}§VAR_TO_VAL {Node.Obj.ToText(____)}{__})",
			tTupleNode<t> Node => $"({Node.Items.Map(aChild => ____ + aChild.ToText(____)).Join((a1, a2) => a1 + ", " + a2, "")}{__})",
			tRecordNode<t> Node => $"{{ {Node.Elements.Map(aChild => ____ + aChild.Key.Id + ": " + aChild.Value.ToText(____)).Join((a1, a2) => a1 + ", " + a2, "")}{__} }}",
			tPairNode<t> Node => $"[{____}{Node.Tail.ToText(____)} ; {Node.Head.ToText(____)}{__}]",
			tLambdaNode<t> Node => $"({____}{Node.Head.ToText(____)} => {Node.Body.ToText(____)}{__})",
			tCallNode<t> Node => $"({____}.{Node.Func.ToText(____)} {Node.Arg.ToText(____)}{__})",
			tMethodNode<t> Node => $"({____}{Node.Obj.ToText(____)} : {Node.Arg.ToText(____)} => {Node.Body.ToText(____)}{__})",
			tIfMatchNode<t> Node => (
				$"§IF {Node.Expression} MATCH {{" + ____ + Node.Cases.Map(
					_ => _.Pattern.ToText(____) + " : " + _.Expression.ToText(____)
				).Reduce(
					"",
					(a1, a2) => a1 + "; " + a2
				) + ____ + "}"
			),
			tIfNode<t> Node => (
				$"§IF {{" + ____ + Node.Cases.Map(
					_ => _.Cond.ToText(____) + " : " + _.Result.ToText(____)
				).Reduce(
					"",
					(a1, a2) => a1 + "; " + a2
				) + ____ + "}"
			),
			tIsNode<t> Node => $"({____}{Node.Expression.ToText(____)} §IS {Node.Pattern.ToText(____)}{__})",
			tPipeToLeftNode<t> Node => $"({____} {Node.Pipe.Map(_ => $"{_.ToText()} §<")}{Node.Head.ToText()}{__})",
			
			// Matches
			tTypedPatternNode<t> Node => Node.TypeExpression.Match(
				Type => $"({____}{Node.Pattern.ToText(____)} € {Type.ToText(____)}{__})",
				() => Node.Pattern.ToText(____)
			),
			tIgnorePatternNode<t> Node => "_",
			tFreeIdPatternNode<t> Node => "§DEF " + Node.Id,
			tVarPatternNode<t> Node => "§VAR " + Node.Id,
			tPrefixPatternNode<t> Node => $"({____}#{Node.Prefix} {Node.Pattern.ToText(____)}{__})",
			tTuplePatternNode<t> Node => $"({____}{Node.Items.Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + ", " + a2, "")}{__})",
			tGuardPatternNode<t> Node => $"({____}{Node.Pattern.ToText(____)} & {Node.Guard.ToText(____)}{__})",
			tRecordPatternNode<t> Node => @$"{{ {
				Node.Elements.Map(
					_ => $"{_.Id}: {_.Pattern.ToText(____)}"
				).Join(
					(a1, a2) => a1 + "\n" + a2,
					""
				)
			} }}",
			
			// Types
			tEmptyTypeNode<t> Node => "[]",
			tIntTypeNode<t> Node => "§INT",
			tCharTypeNode<t> Node => "§CHAR",
			tTextTypeNode<t> Node => "§TEXT",
			tProcTypeNode<t> Node => (Node.ObjType, Node.ArgType, Node.ResType) switch {
				(tEmptyTypeNode<t> _, tEmptyTypeNode<t> _, var ResType) => $"[=> {ResType.ToText(____)}]]",
				(tEmptyTypeNode<t> _, var ArgType, var ResType) => $"[{____}{ArgType.ToText (____)} => {ResType.ToText(____)}{__}]",
				(var ObjType, tEmptyTypeNode<t> _, var ResType) => $"[{____}{ObjType.ToText(____)} : => {ResType.ToText(____)}]]",
				(var ObjType, var ArgType, tEmptyTypeNode<t> _) => $"[{____}{ObjType.ToText(____)} : {____}{ArgType.ToText (____)}]",
				(var ObjType, var ArgType, var ResType) => $"[{____}{ObjType.ToText(____)} : {ArgType.ToText(____)} => {ResType.ToText(____)}{__}]",
			},
			tTypeTypeNode<t> Node => "§TYPE",
			tPrefixTypeNode<t> Node => $"[{____}#{Node.Prefix} {Node.Expressions.Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + ", " + a2, "")}{__}]",
			tTupleTypeNode<t> Node => $"[{____}{Node.ItemTypes.Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + ", " + a2, "")}{__}]",
			tPairTypeNode<t> Node => $"[{____}{Node.TailType.ToText(____)} ; {Node.HeadType.ToText(____)}{__}]",
			tRecordTypeNode<t> Node => $"[<{Node.Elements.Map(aChild => ____ + aChild.Key.Id + ": " + aChild.Type.ToText(____)).Join((a1, a2) => a1 + ", " + a2, "")}{__}>]",
			tPairPatternNode<t> Node => $"({____}{Node.Tail.ToText(____)} ; {Node.Head.ToText(____)}{__})",
			tSetTypeNode<t> Node => $"[{____}{Node.Expressions.Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + " | " + a2, "")}{__}]",
			tVarTypeNode<t> Node => $"[{____}§VAR {Node.Type}]",
			tGenericTypeNode<t> Node => $"[{____}{Node.HeadType.ToText(____)} <=> {Node.BodyType.ToText(____)}{__}]",
			tGenericApplyTypeNode<t> Node => $"[{____}.{Node.GenericType.ToText(____)} {Node.ArgType.ToText(____)}{__}]",
			tRecursiveTypeNode<t> Node => $"[{____}§RECURSIVE {Node.HeadType.ToText(____)} {Node.BodyType.ToText(____)}{__}]",
			
			// Commands
			tBlockNode<t> Node => $"{{{____}{Node.Commands.Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + "," + ____ + a2, "")}{__}}}",
			tMethodCallsNode<t> Node => $"{Node.Object.ToText(____)} :{____ + Node.MethodCalls.Map(aChild => aChild.ToText(____)).Join((a1, a2) => a1 + "," + ____ + a2, "")}{__}.",
			tMethodCallNode<t> Node => $"{Node.Method.ToText(____)} {Node.Argument.ToText(____)} => {Node.Result.Match(aChild => aChild.ToText(____), () => "()")}",
			tReturnIfNode<t> Node => $"RETURN {Node.Result.ToText(____)} IF {Node.Condition.ToText(____)}",
			tDefNode<t> Node => $"DEF {Node.Des.ToText(____)} = {Node.Src.ToText(____)}",
			tDefVarNode<t> Node => $"DEF {Node.Id.ToText(____)} := {____}{Node.Expression.ToText(____)}{Node.MethodCalls.Map(_ => "," + ____ + _.ToText(____)).Join((a1, a2) => a1 + a2, "")}{____}.",
			tRecLambdasNode<t> Node => $@"""
			§REC {{
			{Node.List.Map(_ => tText.Join('\n',  _.ToText(____)))}
			}}
			""",
				
			// Module
			tImportNode<t> Node => $"§IMPORT {Node.Pattern.ToText(____)}",
			tExportNode<t> Node => $"§EXPORT {Node.Expression.ToText(____)}",
			tModuleNode<t> Node => $"""
			{Node.Import.ToText(____)}
			
			{Node.Commands.Map(__ => __.ToText(____)).Join((a1, a2) => a1 + "\n" + a2, "")}
			
			{Node.Export.ToText(____)}
			""",
			
			// Fallback
			_ => throw new System.NotImplementedException(aNode.GetType().Name),
		};
	}
}
