//IMPORT mArrayList.cs

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
		tPos Pos { get; set; }
	}
	
	public interface tExpressionNode<tPos> : tNode<tPos> {}
	public interface tMatchItemNode<tPos> : tNode<tPos> {}
	public interface tLiteralNode<tPos> : tExpressionNode<tPos>, tMatchItemNode<tPos> {}
	public interface tCommandNode<tPos> : tNode<tPos> {}
	
	public struct tEmptyNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tFalseNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tTrueNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tEmptyTypeNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tBoolTypeNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tIntTypeNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tTypeTypeNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tTextNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public tText Value;
	}
	
	public struct tNumberNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public tInt32 Value;
	}
	
	public struct tIgnoreMatchNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
	}
	
	public struct tIdentNode<tPos> : tExpressionNode<tPos>, tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public tText Name;
	}
	
	public struct tMatchTupleNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tMatchNode<tPos>> Items;
	}
	
	public struct tMatchNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchItemNode<tPos> Pattern;
		public tExpressionNode<tPos> Type;
	}
	
	public struct tPrefixNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tText Prefix;
		public tExpressionNode<tPos> Element;
	}
	
	public struct tRecordNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<(tIdentNode<tPos> Key, tExpressionNode<tPos> Value)> Elements;
	}
	
	public struct tMatchRecordNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<(tIdentNode<tPos> Key, tMatchNode<tPos> Match)> Elements;
	}
	
	public struct tMatchPrefixNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public tText Prefix;
		public tMatchNode<tPos> Match;
	}
	
	public struct tMatchGuardNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchNode<tPos> Match;
		public tExpressionNode<tPos> Guard;
	}
	
	public struct tLambdaNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchNode<tPos> Head;
		public tExpressionNode<tPos> Body;
	}
	
	public struct tMethodNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchNode<tPos> Obj;
		public tMatchNode<tPos> Arg;
		public tBlockNode<tPos> Body;
	}
	
	public struct tBlockNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tCommandNode<tPos>> Commands;
	}
	
	public struct tCallNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Func;
		public tExpressionNode<tPos> Arg;
	}
	
	public struct tDefNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchNode<tPos> Des;
		public tExpressionNode<tPos> Src;
	}
	
	public struct tRecLambdaItemNode<tPos> {
		public tPos Pos { get; set; }
		public tIdentNode<tPos> Ident;
		public tLambdaNode<tPos> Lambda;
	}
	
	public struct tRecLambdasNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tRecLambdaItemNode<tPos>> List;
	}
	
	public struct tReturnIfNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Result;
		public tExpressionNode<tPos> Condition;
	}
	
	public struct tIfNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<(tExpressionNode<tPos>, tExpressionNode<tPos>)> Cases;
	}
	
	public struct tIfMatchNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Expression;
		public mStream.tStream<(tMatchNode<tPos>, tExpressionNode<tPos>)> Cases;
	}
	
	public struct tPrefixTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tIdentNode<tPos> Prefix;
		public mStream.tStream<tExpressionNode<tPos>> Expressions;
	}
	
	public struct tTupleTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tExpressionNode<tPos>> Expressions;
	}
	
	public struct tSetTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tExpressionNode<tPos>> Expressions;
	}
	
	public struct tLambdaTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> ArgType;
		public tExpressionNode<tPos> ResType;
	}
	
	public struct tRecursiveTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tIdentNode<tPos> HeadType;
		public tExpressionNode<tPos> BodyType;
	}
	
	public struct tInterfaceTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> HeadType;
		public tExpressionNode<tPos> BodyType;
	}
	
	public struct tGenericTypeNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> HeadType;
		public tExpressionNode<tPos> BodyType;
	}
	
	public struct tDefVarNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public tIdentNode<tPos> Ident;
		public tExpressionNode<tPos> Expression;
		public mStream.tStream<tMethodCallNode<tPos>> MethodCalls;
	}
	
	public struct tVarToValNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Obj;
	}
	
	public struct tMethodCallNode<tPos> {
		public tPos Pos { get; set; }
		public tIdentNode<tPos> Method;
		public tExpressionNode<tPos> Argument;
		public tMatchNode<tPos>? Result;
	}
	
	public struct tMethodCallsNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Object;
		public mStream.tStream<tMethodCallNode<tPos>> MethodCalls;
	}
	
	public struct tTupleNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tExpressionNode<tPos>> Items;
	}
	
	public struct tImportNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchNode<tPos> Match;
	}
	
	public struct tExportNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Expression;
	}
	
	public struct tModuleNode<tPos> {
		public tPos Pos { get; set; }
		public tImportNode<tPos> Import;
		public tExportNode<tPos> Export;
		public mStream.tStream<tCommandNode<tPos>> Commands;
	}
	
	//================================================================================
	public static tEmptyNode<tPos>
	Empty<tPos>(
		tPos aPos
	//================================================================================
	) => new tEmptyNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tFalseNode<tPos>
	False<tPos>(
		tPos aPos
	//================================================================================
	) => new tFalseNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tTrueNode<tPos>
	True<tPos>(
		tPos aPos
	//================================================================================
	) => new tTrueNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tEmptyTypeNode<tPos>
	EmptyType<tPos>(
		tPos aPos
	//================================================================================
	) => new tEmptyTypeNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tBoolTypeNode<tPos>
	BoolType<tPos>(
		tPos aPos
	//================================================================================
	) => new tBoolTypeNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tIntTypeNode<tPos>
	IntType<tPos>(
		tPos aPos
	//================================================================================
	) => new tIntTypeNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tTypeTypeNode<tPos>
	TypeType<tPos>(
		tPos aPos
	//================================================================================
	) => new tTypeTypeNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tNumberNode<tPos>
	Number<tPos>(
		tPos aPos,
		tInt32 aValue
	//================================================================================
	) => new tNumberNode<tPos> {
		Pos = aPos,
		Value = aValue
	};
	
	//================================================================================
	public static tTextNode<tPos>
	Text<tPos>(
		tPos aPos,
		tText aValue
	//================================================================================
	) => new tTextNode<tPos> {
		Pos = aPos,
		Value = aValue
	};
	
	//================================================================================
	public static tIgnoreMatchNode<tPos>
	IgnoreMatch<tPos>(
		tPos aPos
	//================================================================================
	) => new tIgnoreMatchNode<tPos> {
		Pos = aPos
	};
	
	//================================================================================
	public static tIdentNode<tPos>
	Ident<tPos>(
		tPos aPos,
		tText aName
	//================================================================================
	) => new tIdentNode<tPos> {
		Pos = aPos,
		Name = "_" + aName
	};
	
	//================================================================================
	public static tExpressionNode<tPos>
	Tuple<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>> aItems
	//================================================================================
	) {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				return Empty(aPos);
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
		tPos aPos,
		tIdentNode<tPos> aPrefix,
		mStream.tStream<tExpressionNode<tPos>> aTypes
	//================================================================================
	) => new tPrefixTypeNode<tPos> {
		Pos = aPos,
		Prefix = aPrefix,
		Expressions = aTypes,
	};
	
	//================================================================================
	public static tTupleTypeNode<tPos>
	TupleType<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>> aTypes
	//================================================================================
	) => new tTupleTypeNode<tPos> {
		Pos = aPos,
		Expressions = aTypes,
	};
	
	//================================================================================
	public static tSetTypeNode<tPos>
	SetType<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>> aTypes
	//================================================================================
	) => new tSetTypeNode<tPos> {
		Pos = aPos,
		Expressions = aTypes
	};
	
	//================================================================================
	public static tLambdaTypeNode<tPos>
	LambdaType<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aArgType,
		tExpressionNode<tPos> aResType
	//================================================================================
	) => new tLambdaTypeNode<tPos> {
		Pos = aPos,
		ArgType = aArgType,
		ResType = aResType
	};
	
	//================================================================================
	public static tRecursiveTypeNode<tPos>
	RecursiveType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aHeadType,
		tExpressionNode<tPos> aBodyType
	//================================================================================
	) => new tRecursiveTypeNode<tPos> {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	//================================================================================
	public static tInterfaceTypeNode<tPos>
	InterfaceType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aHeadType,
		tExpressionNode<tPos> aBodyType
	//================================================================================
	) => new tInterfaceTypeNode<tPos> {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	//================================================================================
	public static tGenericTypeNode<tPos>
	GenericType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aHeadType,
		tExpressionNode<tPos> aBodyType
	//================================================================================
	) => new tGenericTypeNode<tPos> {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	//================================================================================
	public static tCallNode<tPos>
	Call<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aFunc,
		tExpressionNode<tPos> aArg
	//================================================================================
	) => new tCallNode<tPos> {
		Pos = aPos,
		Func = aFunc,
		Arg = aArg
	};
	
	//================================================================================
	public static tPrefixNode<tPos>
	Prefix<tPos>(
		tPos aPos,
		tIdentNode<tPos> aPrefix,
		tExpressionNode<tPos> aElement
	//================================================================================
	) => new tPrefixNode<tPos> {
		Pos = aPos,
		Prefix = aPrefix.Name,
		Element = aElement
	};
	
	//================================================================================
	public static tMatchPrefixNode<tPos>
	MatchPrefix<tPos>(
		tPos aPos,
		tIdentNode<tPos>aPrefix,
		tMatchNode<tPos> aMatch
	//================================================================================
	) => new tMatchPrefixNode<tPos> {
		Pos = aPos,
		Prefix = aPrefix.Name,
		Match = aMatch
	};
	
	//================================================================================
	public static tRecordNode<tPos>
	Record<tPos>(
		tPos aPos,
		mStream.tStream<(tIdentNode<tPos> Key, tExpressionNode<tPos> Value)> aRecordItems
	//================================================================================
	) => new tRecordNode<tPos> {
		Pos = aPos,
		Elements = aRecordItems
	};
	
	//================================================================================
	public static tMatchRecordNode<tPos>
	MatchRecord<tPos>(
		tPos aPos,
		mStream.tStream<(tIdentNode<tPos> Key, tMatchNode<tPos> Match)> aRecordItems
	//================================================================================
	) => new tMatchRecordNode<tPos> {
		Pos = aPos,
		Elements = aRecordItems
	};
	
	//================================================================================
	public static tMatchGuardNode<tPos>
	MatchGuard<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aGuard
	//================================================================================
	) => new tMatchGuardNode<tPos> {
		Pos = aPos,
		Match = aMatch,
		Guard = aGuard
	};
	
	//================================================================================
	public static tLambdaNode<tPos>
	Lambda<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aBody
	//================================================================================
	) => new tLambdaNode<tPos> {
		Pos = aPos,
		Head = aMatch,
		Body = aBody
	};
	
	//================================================================================
	public static tMethodNode<tPos>
	Method<tPos>(
		tPos aPos,
		tMatchNode<tPos> aObjMatch,
		tMatchNode<tPos> aArgMatch,
		tBlockNode<tPos> aBody
	//================================================================================
	) => new tMethodNode<tPos> {
		Pos = aPos,
		Obj = aObjMatch,
		Arg = aArgMatch,
		Body = aBody
	};
	
	//================================================================================
	public static tRecLambdaItemNode<tPos>
	RecLambdaItem<tPos>(
		tPos aPos,
		tIdentNode<tPos> aIdent,
		tLambdaNode<tPos> aLambda
	//================================================================================
	) => new tRecLambdaItemNode<tPos> {
		Pos = aPos,
		Ident = aIdent,
		Lambda = aLambda
	};
	
	//================================================================================
	public static tRecLambdasNode<tPos>
	RecLambdas<tPos>(
		tPos aPos,
		mStream.tStream<tRecLambdaItemNode<tPos>> aList
	//================================================================================
	) => new tRecLambdasNode<tPos> {
		Pos = aPos,
		List = aList
	};
	
	//================================================================================
	public static tMatchItemNode<tPos>
	MatchTuple<tPos>(
		tPos aPos,
		mStream.tStream<tMatchNode<tPos>> aItems
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
					Pos = aPos,
					Items = aItems
				};
			}
		}
	}
	
	//================================================================================
	public static tMatchNode<tPos>
	Match<tPos>(
		tPos aPos,
		tMatchItemNode<tPos> aMatch,
		tExpressionNode<tPos> aType
	//================================================================================
	) => new tMatchNode<tPos> {
		Pos = aPos,
		Pattern = aMatch,
		Type = aType
	};
	
	//================================================================================
	public static tMatchNode<tPos>
	UnTypedMatch<tPos>(
		tPos aPos,
		tMatchItemNode<tPos> aMatch
	//================================================================================
	) => Match(aPos, aMatch, null);
	
	//================================================================================
	public static tDefNode<tPos>
	Def<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aExpression
	//================================================================================
	) => new tDefNode<tPos> {
		Pos = aPos,
		Des = aMatch,
		Src = aExpression
	};
	
	//================================================================================
	public static tReturnIfNode<tPos>
	ReturnIf<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aResult,
		tExpressionNode<tPos> aCondition
	//================================================================================
	) => new tReturnIfNode<tPos> {
		Pos = aPos,
		Result = aResult,
		Condition = aCondition
	};
	
	//================================================================================
	public static tIfNode<tPos>
	If<tPos>(
		tPos aPos,
		mStream.tStream<(tExpressionNode<tPos>, tExpressionNode<tPos>)> aCases
	//================================================================================
	) => new tIfNode<tPos> {
		Pos = aPos,
		Cases = aCases
	};
	
	//================================================================================
	public static tIfMatchNode<tPos>
	IfMatch<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<(tMatchNode<tPos>, tExpressionNode<tPos>)> aCases
	//================================================================================
	) => new tIfMatchNode<tPos> {
		Pos = aPos,
		Expression = aExpression,
		Cases = aCases
	};
	
	//================================================================================
	public static tDefVarNode<tPos>
	DefVar<tPos>(
		tPos aPos,
		tIdentNode<tPos> aVar,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<tMethodCallNode<tPos>> aMethodCalls
	//================================================================================
	) => new tDefVarNode<tPos> {
		Pos = aPos,
		Ident = aVar,
		Expression = aExpression,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static tVarToValNode<tPos>
	VarToVal<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aObj
	//================================================================================
	) => new tVarToValNode<tPos> {
		Pos = aPos,
		Obj = aObj,
	};
	
	//================================================================================
	public static tMethodCallsNode<tPos>
	MethodCallStatment<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aObject,
		mStream.tStream<tMethodCallNode<tPos>> aMethodCalls
	//================================================================================
	) => new tMethodCallsNode<tPos> {
		Pos = aPos,
		Object = aObject,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static tMethodCallNode<tPos>
	MethodCall<tPos>(
		tPos aPos,
		tIdentNode<tPos> aMethod,
		tExpressionNode<tPos> aAgument,
		tMatchNode<tPos>? aResult
	//================================================================================
	) => new tMethodCallNode<tPos> {
		Pos = aPos,
		Method = aMethod,
		Argument = aAgument,
		Result = aResult
	};
	
	//================================================================================
	public static tBlockNode<tPos>
	Block<tPos>(
		tPos aPos,
		mStream.tStream<tCommandNode<tPos>> aCommands
	//================================================================================
	) => new tBlockNode<tPos> {
		Pos = aPos,
		Commands = aCommands
	};
	
	//================================================================================
	public static tModuleNode<tPos>
	Module<tPos>(
		tPos aPos,
		tImportNode<tPos> aImport,
		mStream.tStream<tCommandNode<tPos>> aCommands,
		tExportNode<tPos> aExport
	//================================================================================
	) => new tModuleNode<tPos> {
		Pos = aPos,
		Import = aImport,
		Export = aExport,
		Commands = aCommands
	};
	
	//================================================================================
	public static tImportNode<tPos>
	Import<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch
	//================================================================================
	) => new tImportNode<tPos> {
		Pos = aPos,
		Match = aMatch 
	};
	
	//================================================================================
	public static tExportNode<tPos>
	Export<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression
	//================================================================================
	) => new tExportNode<tPos> {
		Pos = aPos,
		Expression = aExpression 
	};
}
