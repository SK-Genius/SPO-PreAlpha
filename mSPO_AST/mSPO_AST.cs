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

public static class mSPO_AST {
	
	public interface tNode<tPos> {
		mStd.tSpan<tPos> Span { get; set; }
	}
	
	public interface tExpressionNode<tPos> : tNode<tPos> {}
	public interface tMatchItemNode<tPos> : tNode<tPos> {}
	public interface tLiteralNode<tPos> : tExpressionNode<tPos>, tMatchItemNode<tPos> {}
	public interface tCommandNode<tPos> : tNode<tPos> {}
	
	public struct tEmptyNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tFalseNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tTrueNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tEmptyTypeNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tBoolTypeNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tIntTypeNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tTypeTypeNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tTextNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tText Value;
	}
	
	public struct tNumberNode<tPos> : tLiteralNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tInt32 Value;
	}
	
	public struct tIgnoreMatchNode<tPos> : tMatchItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
	}
	
	public struct tIdentNode<tPos> : tExpressionNode<tPos>, tMatchItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tText Name;
	}
	
	public struct tMatchTupleNode<tPos> : tMatchItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<tMatchNode<tPos>> Items;
	}
	
	public struct tMatchNode<tPos> : tMatchItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tMatchItemNode<tPos> Pattern;
		public tExpressionNode<tPos> Type;
	}
	
	public struct tPrefixNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tText Prefix;
		public tExpressionNode<tPos> Element;
	}
	
	public struct tMatchPrefixNode<tPos> : tMatchItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tText Prefix;
		public tMatchNode<tPos> Match;
	}
	
	public struct tMatchGuardNode<tPos> : tMatchItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tMatchNode<tPos> Match;
		public tExpressionNode<tPos> Guard;
	}
	
	public struct tLambdaNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tMatchNode<tPos> Head;
		public tExpressionNode<tPos> Body;
	}
	
	public struct tMethodNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tMatchNode<tPos> Obj;
		public tMatchNode<tPos> Arg;
		public tBlockNode<tPos> Body;
	}
	
	public struct tBlockNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<tCommandNode<tPos>> Commands;
	}
	
	public struct tCallNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> Func;
		public tExpressionNode<tPos> Arg;
	}
	
	public struct tDefNode<tPos> : tCommandNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tMatchNode<tPos> Des;
		public tExpressionNode<tPos> Src;
	}
	
	public struct tRecLambdaItemNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tIdentNode<tPos> Ident;
		public tLambdaNode<tPos> Lambda;
	}
	
	public struct tRecLambdasNode<tPos> : tCommandNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<tRecLambdaItemNode<tPos>> List;
	}
	
	public struct tReturnIfNode<tPos> : tCommandNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> Result;
		public tExpressionNode<tPos> Condition;
	}
	
	public struct tIfNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<(tExpressionNode<tPos>, tExpressionNode<tPos>)> Cases;
	}
	
	public struct tIfMatchNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> Expression;
		public mList.tList<(tMatchNode<tPos>, tExpressionNode<tPos>)> Cases;
	}
	
	public struct tPrefixTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tIdentNode<tPos> Prefix;
		public mList.tList<tExpressionNode<tPos>> Expressions;
	}
	
	public struct tTupleTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<tExpressionNode<tPos>> Expressions;
	}
	
	public struct tSetTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<tExpressionNode<tPos>> Expressions;
	}
	
	public struct tLambdaTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> ArgType;
		public tExpressionNode<tPos> ResType;
	}
	
	public struct tRecursiveTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tIdentNode<tPos> HeadType;
		public tExpressionNode<tPos> BodyType;
	}
	
	public struct tInterfaceTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> HeadType;
		public tExpressionNode<tPos> BodyType;
	}
	
	public struct tGenericTypeNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> HeadType;
		public tExpressionNode<tPos> BodyType;
	}
	
	public struct tDefVarNode<tPos> : tCommandNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tIdentNode<tPos> Ident;
		public tExpressionNode<tPos> Expression;
		public mList.tList<tMethodCallNode<tPos>> MethodCalls;
	}
	
	public struct tVarToValNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> Obj;
	}
	
	public struct tMethodCallNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tIdentNode<tPos> Method;
		public tExpressionNode<tPos> Argument;
		public tMatchNode<tPos>? Result;
	}
	
	public struct tMethodCallsNode<tPos> : tCommandNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> Object;
		public mList.tList<tMethodCallNode<tPos>> MethodCalls;
	}
	
	public struct tTupleNode<tPos> : tExpressionNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public mList.tList<tExpressionNode<tPos>> Items;
	}
	
	public struct tImportNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tMatchNode<tPos> Match;
	}
	
	public struct tExportNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tExpressionNode<tPos> Expression;
	}
	
	public struct tModuleNode<tPos> {
		public mStd.tSpan<tPos> Span { get; set; }
		public tImportNode<tPos> Import;
		public tExportNode<tPos> Export;
		public mList.tList<tCommandNode<tPos>> Commands;
	}
	
	//================================================================================
	public static tEmptyNode<tPos>
	Empty<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tEmptyNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tFalseNode<tPos>
	False<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tFalseNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tTrueNode<tPos>
	True<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tTrueNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tEmptyTypeNode<tPos>
	EmptyType<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tEmptyTypeNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tBoolTypeNode<tPos>
	BoolType<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tBoolTypeNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tIntTypeNode<tPos>
	IntType<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tIntTypeNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tTypeTypeNode<tPos>
	TypeType<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tTypeTypeNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tNumberNode<tPos>
	Number<tPos>(
		mStd.tSpan<tPos> aSpan,
		tInt32 aValue
	//================================================================================
	) => new tNumberNode<tPos> {
		Span = aSpan,
		Value = aValue
	};
	
	//================================================================================
	public static tTextNode<tPos>
	Text<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aValue
	//================================================================================
	) => new tTextNode<tPos> {
		Span = aSpan,
		Value = aValue
	};
	
	//================================================================================
	public static tIgnoreMatchNode<tPos>
	IgnoreMatch<tPos>(
		mStd.tSpan<tPos> aSpan
	//================================================================================
	) => new tIgnoreMatchNode<tPos> {
		Span = aSpan
	};
	
	//================================================================================
	public static tIdentNode<tPos>
	Ident<tPos>(
		mStd.tSpan<tPos> aSpan,
		tText aName
	//================================================================================
	) => new tIdentNode<tPos> {
		Span = aSpan,
		Name = "_" + aName
	};
	
	//================================================================================
	public static tExpressionNode<tPos>
	Tuple<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<tExpressionNode<tPos>> aItems
	//================================================================================
	) {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				return Empty(aSpan);
			}
			case 1: {
				mDebug.Assert(aItems.Match(out var Head, out var _));
				return Head;
			}
			default: {
				return new tTupleNode<tPos> {
					Items = aItems
				};
			}
		}
	}
	
	//================================================================================
	public static tPrefixTypeNode<tPos>
	PrefixType<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aPrefix,
		mList.tList<tExpressionNode<tPos>> aTypes
	//================================================================================
	) => new tPrefixTypeNode<tPos> {
		Span = aSpan,
		Prefix = aPrefix,
		Expressions = aTypes,
	};
	
	//================================================================================
	public static tTupleTypeNode<tPos>
	TupleType<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<tExpressionNode<tPos>> aTypes
	//================================================================================
	) => new tTupleTypeNode<tPos> {
		Span = aSpan,
		Expressions = aTypes,
	};
	
	//================================================================================
	public static tSetTypeNode<tPos>
	SetType<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<tExpressionNode<tPos>> aTypes
	//================================================================================
	) => new tSetTypeNode<tPos> {
		Span = aSpan,
		Expressions = aTypes
	};
	
	//================================================================================
	public static tLambdaTypeNode<tPos>
	LambdaType<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aArgType,
		tExpressionNode<tPos> aResType
	//================================================================================
	) => new tLambdaTypeNode<tPos> {
		Span = aSpan,
		ArgType = aArgType,
		ResType = aResType
	};
	
	//================================================================================
	public static tRecursiveTypeNode<tPos>
	RecursiveType<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aHeadType,
		tExpressionNode<tPos> aBodyType
	//================================================================================
	) => new tRecursiveTypeNode<tPos> {
		Span = aSpan,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	//================================================================================
	public static tInterfaceTypeNode<tPos>
	InterfaceType<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aHeadType,
		tExpressionNode<tPos> aBodyType
	//================================================================================
	) => new tInterfaceTypeNode<tPos> {
		Span = aSpan,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	//================================================================================
	public static tGenericTypeNode<tPos>
	GenericType<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aHeadType,
		tExpressionNode<tPos> aBodyType
	//================================================================================
	) => new tGenericTypeNode<tPos> {
		Span = aSpan,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	//================================================================================
	public static tCallNode<tPos>
	Call<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aFunc,
		tExpressionNode<tPos> aArg
	//================================================================================
	) => new tCallNode<tPos> {
		Span = aSpan,
		Func = aFunc,
		Arg = aArg
	};
	
	//================================================================================
	public static tPrefixNode<tPos>
	Prefix<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aPrefix,
		tExpressionNode<tPos> aElement
	//================================================================================
	) => new tPrefixNode<tPos> {
		Span = aSpan,
		Prefix = aPrefix.Name,
		Element = aElement
	};
	
	//================================================================================
	public static tMatchPrefixNode<tPos>
	MatchPrefix<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos>aPrefix,
		tMatchNode<tPos> aMatch
	//================================================================================
	) => new tMatchPrefixNode<tPos> {
		Span = aSpan,
		Prefix = aPrefix.Name,
		Match = aMatch
	};
	
	//================================================================================
	public static tMatchGuardNode<tPos>
	MatchGuard<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aGuard
	//================================================================================
	) => new tMatchGuardNode<tPos> {
		Span = aSpan,
		Match = aMatch,
		Guard = aGuard
	};
	
	//================================================================================
	public static tLambdaNode<tPos>
	Lambda<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aBody
	//================================================================================
	) => new tLambdaNode<tPos> {
		Span = aSpan,
		Head = aMatch,
		Body = aBody
	};
	
	//================================================================================
	public static tMethodNode<tPos>
	Method<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchNode<tPos> aObjMatch,
		tMatchNode<tPos> aArgMatch,
		tBlockNode<tPos> aBody
	//================================================================================
	) => new tMethodNode<tPos> {
		Span = aSpan,
		Obj = aObjMatch,
		Arg = aArgMatch,
		Body = aBody
	};
	
	//================================================================================
	public static tRecLambdaItemNode<tPos>
	RecLambdaItem<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aIdent,
		tLambdaNode<tPos> aLambda
	//================================================================================
	) => new tRecLambdaItemNode<tPos> {
		Span = aSpan,
		Ident = aIdent,
		Lambda = aLambda
	};
	
	//================================================================================
	public static tRecLambdasNode<tPos>
	RecLambdas<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<tRecLambdaItemNode<tPos>> aList
	//================================================================================
	) => new tRecLambdasNode<tPos> {
		Span = aSpan,
		List = aList
	};
	
	//================================================================================
	public static tMatchItemNode<tPos>
	MatchTuple<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<tMatchNode<tPos>> aItems
	//================================================================================
	) {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				throw mStd.Error("impossible");
			}
			case 1: {
				mDebug.Assert(aItems.Match(out var Head, out var _));
				return Head;
			}
			default: {
				return new tMatchTupleNode<tPos> {
					Span = aSpan,
					Items = aItems
				};
			}
		}
	}
	
	//================================================================================
	public static tMatchNode<tPos>
	Match<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchItemNode<tPos> aMatch,
		tExpressionNode<tPos> aType
	//================================================================================
	) => new tMatchNode<tPos> {
		Span = aSpan,
		Pattern = aMatch,
		Type = aType
	};
	
	//================================================================================
	public static tMatchNode<tPos>
	UnTypedMatch<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchItemNode<tPos> aMatch
	//================================================================================
	) => Match(aSpan, aMatch, null);
	
	//================================================================================
	public static tDefNode<tPos>
	Def<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aExpression
	//================================================================================
	) => new tDefNode<tPos> {
		Span = aSpan,
		Des = aMatch,
		Src = aExpression
	};
	
	//================================================================================
	public static tReturnIfNode<tPos>
	ReturnIf<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aResult,
		tExpressionNode<tPos> aCondition
	//================================================================================
	) => new tReturnIfNode<tPos> {
		Span = aSpan,
		Result = aResult,
		Condition = aCondition
	};
	
	//================================================================================
	public static tIfNode<tPos>
	If<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<(tExpressionNode<tPos>, tExpressionNode<tPos>)> aCases
	//================================================================================
	) => new tIfNode<tPos> {
		Span = aSpan,
		Cases = aCases
	};
	
	//================================================================================
	public static tIfMatchNode<tPos>
	IfMatch<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aExpression,
		mList.tList<(tMatchNode<tPos>, tExpressionNode<tPos>)> aCases
	//================================================================================
	) => new tIfMatchNode<tPos> {
		Span = aSpan,
		Expression = aExpression,
		Cases = aCases
	};
	
	//================================================================================
	public static tDefVarNode<tPos>
	DefVar<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aVar,
		tExpressionNode<tPos> aExpression,
		mList.tList<tMethodCallNode<tPos>> aMethodCalls
	//================================================================================
	) => new tDefVarNode<tPos> {
		Span = aSpan,
		Ident = aVar,
		Expression = aExpression,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static tVarToValNode<tPos>
	VarToVal<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aObj
	//================================================================================
	) => new tVarToValNode<tPos> {
		Span = aSpan,
		Obj = aObj,
	};
	
	//================================================================================
	public static tMethodCallsNode<tPos>
	MethodCallStatment<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aObject,
		mList.tList<tMethodCallNode<tPos>> aMethodCalls
	//================================================================================
	) => new tMethodCallsNode<tPos> {
		Span = aSpan,
		Object = aObject,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static tMethodCallNode<tPos>
	MethodCall<tPos>(
		mStd.tSpan<tPos> aSpan,
		tIdentNode<tPos> aMethod,
		tExpressionNode<tPos> aAgument,
		tMatchNode<tPos>? aResult
	//================================================================================
	) => new tMethodCallNode<tPos> {
		Span = aSpan,
		Method = aMethod,
		Argument = aAgument,
		Result = aResult
	};
	
	//================================================================================
	public static tBlockNode<tPos>
	Block<tPos>(
		mStd.tSpan<tPos> aSpan,
		mList.tList<tCommandNode<tPos>> aCommands
	//================================================================================
	) => new tBlockNode<tPos> {
		Span = aSpan,
		Commands = aCommands
	};
	
	//================================================================================
	public static tModuleNode<tPos>
	Module<tPos>(
		mStd.tSpan<tPos> aSpan,
		tImportNode<tPos> aImport,
		mList.tList<tCommandNode<tPos>> aCommands,
		tExportNode<tPos> aExport
	//================================================================================
	) => new tModuleNode<tPos> {
		Span = aSpan,
		Import = aImport,
		Export = aExport,
		Commands = aCommands
	};
	
	//================================================================================
	public static tImportNode<tPos>
	Import<tPos>(
		mStd.tSpan<tPos> aSpan,
		tMatchNode<tPos> aMatch
	//================================================================================
	) => new tImportNode<tPos> {
		Span = aSpan,
		Match = aMatch 
	};
	
	//================================================================================
	public static tExportNode<tPos>
	Export<tPos>(
		mStd.tSpan<tPos> aSpan,
		tExpressionNode<tPos> aExpression
	//================================================================================
	) => new tExportNode<tPos> {
		Span = aSpan,
		Expression = aExpression 
	};
}
