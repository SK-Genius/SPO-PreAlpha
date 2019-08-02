//IMPORT mArrayList.cs
//IMPORT mVM_Type.cs

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

public static class
mSPO_AST {
	
	public interface
	tNode<tPos> {
		tPos Pos { get; set; }
	}
	
	public interface
	tMatchItemNode<tPos> : tNode<tPos> {
		mVM_Type.tType TypeAnnotation { get; set; }
	}
	
	public interface
	tExpressionNode<tPos> : tNode<tPos> {
		mVM_Type.tType TypeAnnotation { get; set; }
	}
	
	public interface
	tLiteralNode<tPos> : tExpressionNode<tPos>, tMatchItemNode<tPos> {}
	
	public interface
	tTypeNode<tPos> : tExpressionNode<tPos> {}

	public interface
	tCommandNode<tPos> : tNode<tPos> {}
	
	public class
	tEmptyNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tFalseNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tTrueNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tEmptyTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tBoolTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tIntTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tTypeTypeNode<tPos> : tTypeNode<tPos>, tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}

	public class
	tTextNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tText Value;
	}
	
	public class
	tIntNode<tPos> : tLiteralNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tInt32 Value;
	}
	
	public class
	tIgnoreMatchNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
	}
	
	public class
	tIdentNode<tPos> : tTypeNode<tPos>, tExpressionNode<tPos>, tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tText Name;
	}
	
	public class
	tMatchFreeIdentNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tText Name;
	}
	
	public class
	tMatchTupleNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<tMatchNode<tPos>> Items;
	}
	
	public class
	tMatchNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tMatchItemNode<tPos> Pattern;
		public tExpressionNode<tPos> Type;
	}
	
	public class
	tPrefixNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tText Prefix;
		public tExpressionNode<tPos> Element;
	}
	
	public class
	tRecordNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<(tIdentNode<tPos> Key, tExpressionNode<tPos> Value)> Elements;
	}
	
	public class
	tMatchRecordNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<(tIdentNode<tPos> Key, tMatchNode<tPos> Match)> Elements;
	}
	
	public class
	tMatchPrefixNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tText Prefix;
		public tMatchNode<tPos> Match;
	}
	
	public class
	tMatchGuardNode<tPos> : tMatchItemNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tMatchNode<tPos> Match;
		public tExpressionNode<tPos> Guard;
	}
	
	public class
	tLambdaNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tMatchNode<tPos> Generic;
		public tMatchNode<tPos> Head;
		public tExpressionNode<tPos> Body;
	}
	
	public class
	tMethodNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tMatchNode<tPos> Obj;
		public tMatchNode<tPos> Arg;
		public tBlockNode<tPos> Body;
	}
	
	public class
	tBlockNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<tCommandNode<tPos>> Commands;
	}
	
	public class
	tCallNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Func;
		public tExpressionNode<tPos> Arg;
	}
	
	public class
	tDefNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tMatchNode<tPos> Des;
		public tExpressionNode<tPos> Src;
	}
	
	public class
	tRecLambdaItemNode<tPos> : tNode<tPos>{
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tMatchFreeIdentNode<tPos> Ident;
		public tLambdaNode<tPos> Lambda;
	}
	
	public class
	tRecLambdasNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public mStream.tStream<tRecLambdaItemNode<tPos>> List;
	}
	
	public class
	tReturnIfNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Result;
		public tExpressionNode<tPos> Condition;
	}
	
	public class
	tIfNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<(tExpressionNode<tPos> Cond, tExpressionNode<tPos> Result)> Cases;
	}
	
	public class
	tIfMatchNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Expression;
		public mStream.tStream<(tMatchNode<tPos> Match, tExpressionNode<tPos> Expression)> Cases;
	}
	
	public class
	tPrefixTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tIdentNode<tPos> Prefix;
		public mStream.tStream<tTypeNode<tPos>> Expressions;
	}
	
	public class
	tTupleTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<tTypeNode<tPos>> Expressions;
	}
	
	public class
	tSetTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<tTypeNode<tPos>> Expressions;
	}
	
	public class
	tLambdaTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tTypeNode<tPos> ArgType;
		public tTypeNode<tPos> ResType;
	}
	
	public class
	tRecursiveTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tIdentNode<tPos> HeadType;
		public tTypeNode<tPos> BodyType;
	}
	
	public class
	tInterfaceTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tIdentNode<tPos> HeadType;
		public tTypeNode<tPos> BodyType;
	}
	
	public class
	tGenericTypeNode<tPos> : tTypeNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tIdentNode<tPos> HeadType;
		public tTypeNode<tPos> BodyType;
	}
	
	public class
	tDefVarNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tIdentNode<tPos> Ident;
		public tExpressionNode<tPos> Expression;
		public mStream.tStream<tMethodCallNode<tPos>> MethodCalls;
	}
	
	public class
	tVarToValNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Obj;
	}
	
	public class
	tMethodCallNode<tPos> : tNode<tPos> {
		public tPos Pos { get; set; }
		public tIdentNode<tPos> Method;
		public tExpressionNode<tPos> Argument;
		public tMatchNode<tPos> Result;
	}
	
	public class
	tMethodCallsNode<tPos> : tCommandNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Object;
		public mStream.tStream<tMethodCallNode<tPos>> MethodCalls;
	}
	
	public class
	tPipeToRightNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Left;
		public tExpressionNode<tPos> Right;
	}
	
	public class
	tPipeToLeftNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public tExpressionNode<tPos> Left;
		public tExpressionNode<tPos> Right;
	}
	
	public class
	tTupleNode<tPos> : tExpressionNode<tPos> {
		public tPos Pos { get; set; }
		public mVM_Type.tType TypeAnnotation { get; set; }
		public mStream.tStream<tExpressionNode<tPos>> Items;
	}
	
	public class
	tImportNode<tPos> : tNode<tPos> {
		public tPos Pos { get; set; }
		public tMatchNode<tPos> Match;
	}
	
	public class
	tExportNode<tPos> : tNode<tPos> {
		public tPos Pos { get; set; }
		public tExpressionNode<tPos> Expression;
	}
	
	public class
	tModuleNode<tPos> : tNode<tPos> {
		public tPos Pos { get; set; }
		public tImportNode<tPos> Import;
		public tExportNode<tPos> Export;
		public mStream.tStream<tCommandNode<tPos>> Commands;
	}
	
	public static tEmptyNode<tPos>
	Empty<tPos>(
		tPos aPos
	) => new tEmptyNode<tPos> {
		Pos = aPos
	};
	
	public static tFalseNode<tPos>
	False<tPos>(
		tPos aPos
	) => new tFalseNode<tPos> {
		Pos = aPos
	};
	
	public static tTrueNode<tPos>
	True<tPos>(
		tPos aPos
	) => new tTrueNode<tPos> {
		Pos = aPos
	};
	
	public static tEmptyTypeNode<tPos>
	EmptyType<tPos>(
		tPos aPos
	) => new tEmptyTypeNode<tPos> {
		Pos = aPos
	};
	
	public static tBoolTypeNode<tPos>
	BoolType<tPos>(
		tPos aPos
	) => new tBoolTypeNode<tPos> {
		Pos = aPos
	};
	
	public static tIntTypeNode<tPos>
	IntType<tPos>(
		tPos aPos
	) => new tIntTypeNode<tPos> {
		Pos = aPos
	};
	
	public static tTypeTypeNode<tPos>
	TypeType<tPos>(
		tPos aPos
	) => new tTypeTypeNode<tPos> {
		Pos = aPos
	};
	
	public static tIntNode<tPos>
	Int<tPos>(
		tPos aPos,
		tInt32 aValue
	) => new tIntNode<tPos> {
		Pos = aPos,
		Value = aValue
	};
	
	public static tTextNode<tPos>
	Text<tPos>(
		tPos aPos,
		tText aValue
	) => new tTextNode<tPos> {
		Pos = aPos,
		Value = aValue
	};
	
	public static tIgnoreMatchNode<tPos>
	IgnoreMatch<tPos>(
		tPos aPos
	) => new tIgnoreMatchNode<tPos> {
		Pos = aPos
	};
	
	public static tIdentNode<tPos>
	Ident<tPos>(
		tPos aPos,
		tText aName
	) => new tIdentNode<tPos> {
		Pos = aPos,
		Name = "_" + aName
	};
	
	public static tMatchFreeIdentNode<tPos>
	MatchFreeIdent<tPos>(
		tPos aPos,
		tText aName
	) => new tMatchFreeIdentNode<tPos> {
		Pos = aPos,
		Name = "_" + aName
	};
	
	public static tExpressionNode<tPos>
	Tuple<tPos>(
		tPos aPos,
		mStream.tStream<tExpressionNode<tPos>> aItems
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
					Pos = aPos,
					Items = aItems
				};
			}
		}
	}
	
	public static tPrefixTypeNode<tPos>
	PrefixType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aPrefix,
		mStream.tStream<tTypeNode<tPos>> aTypes
	) => new tPrefixTypeNode<tPos> {
		Pos = aPos,
		Prefix = aPrefix,
		Expressions = aTypes,
	};
	
	public static tTupleTypeNode<tPos>
	TupleType<tPos>(
		tPos aPos,
		mStream.tStream<tTypeNode<tPos>> aTypes
	) => new tTupleTypeNode<tPos> {
		Pos = aPos,
		Expressions = aTypes,
	};
	
	public static tSetTypeNode<tPos>
	SetType<tPos>(
		tPos aPos,
		mStream.tStream<tTypeNode<tPos>> aTypes
	) => new tSetTypeNode<tPos> {
		Pos = aPos,
		Expressions = aTypes
	};
	
	public static tLambdaTypeNode<tPos>
	LambdaType<tPos>(
		tPos aPos,
		tTypeNode<tPos> aArgType,
		tTypeNode<tPos> aResType
	) => new tLambdaTypeNode<tPos> {
		Pos = aPos,
		ArgType = aArgType,
		ResType = aResType
	};
	
	public static tRecursiveTypeNode<tPos>
	RecursiveType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new tRecursiveTypeNode<tPos> {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	public static tInterfaceTypeNode<tPos>
	InterfaceType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new tInterfaceTypeNode<tPos> {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	public static tGenericTypeNode<tPos>
	GenericType<tPos>(
		tPos aPos,
		tIdentNode<tPos> aHeadType,
		tTypeNode<tPos> aBodyType
	) => new tGenericTypeNode<tPos> {
		Pos = aPos,
		HeadType = aHeadType,
		BodyType = aBodyType
	};
	
	public static tCallNode<tPos>
	Call<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aFunc,
		tExpressionNode<tPos> aArg
	) => new tCallNode<tPos> {
		Pos = aPos,
		Func = aFunc,
		Arg = aArg
	};
	
	public static tPrefixNode<tPos>
	Prefix<tPos>(
		tPos aPos,
		tIdentNode<tPos> aPrefix,
		tExpressionNode<tPos> aElement
	) => new tPrefixNode<tPos> {
		Pos = aPos,
		Prefix = aPrefix.Name,
		Element = aElement
	};
	
	public static tMatchPrefixNode<tPos>
	MatchPrefix<tPos>(
		tPos aPos,
		tIdentNode<tPos>aPrefix,
		tMatchNode<tPos> aMatch
	) => new tMatchPrefixNode<tPos> {
		Pos = aPos,
		Prefix = aPrefix.Name,
		Match = aMatch
	};
	
	public static tRecordNode<tPos>
	Record<tPos>(
		tPos aPos,
		mStream.tStream<(tIdentNode<tPos> Key, tExpressionNode<tPos> Value)> aRecordItems
	) => new tRecordNode<tPos> {
		Pos = aPos,
		Elements = aRecordItems
	};
	
	public static tMatchRecordNode<tPos>
	MatchRecord<tPos>(
		tPos aPos,
		mStream.tStream<(tIdentNode<tPos> Key, tMatchNode<tPos> Match)> aRecordItems
	) => new tMatchRecordNode<tPos> {
		Pos = aPos,
		Elements = aRecordItems
	};
	
	public static tMatchGuardNode<tPos>
	MatchGuard<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aGuard
	) => new tMatchGuardNode<tPos> {
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
	) => new tLambdaNode<tPos> {
		Pos = aPos,
		Generic = aStaticMatch.Else(null),
		Head = aMatch,
		Body = aBody
	};
	
	public static tMethodNode<tPos>
	Method<tPos>(
		tPos aPos,
		tMatchNode<tPos> aObjMatch,
		tMatchNode<tPos> aArgMatch,
		tBlockNode<tPos> aBody
	) => new tMethodNode<tPos> {
		Pos = aPos,
		Obj = aObjMatch,
		Arg = aArgMatch,
		Body = aBody
	};
	
	public static tRecLambdaItemNode<tPos>
	RecLambdaItem<tPos>(
		tPos aPos,
		tMatchFreeIdentNode<tPos> aIdent,
		tLambdaNode<tPos> aLambda
	) => new tRecLambdaItemNode<tPos> {
		Pos = aPos,
		Ident = aIdent,
		Lambda = aLambda
	};
	
	public static tRecLambdasNode<tPos>
	RecLambdas<tPos>(
		tPos aPos,
		mStream.tStream<tRecLambdaItemNode<tPos>> aList
	) => new tRecLambdasNode<tPos> {
		Pos = aPos,
		List = aList
	};
	
	public static tMatchItemNode<tPos>
	MatchTuple<tPos>(
		tPos aPos,
		mStream.tStream<tMatchNode<tPos>> aItems
	) {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				throw mError.Error("impossible");
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
	
	public static tMatchNode<tPos>
	Match<tPos>(
		tPos aPos,
		tMatchItemNode<tPos> aMatch,
		tExpressionNode<tPos> aType
	) => new tMatchNode<tPos> {
		Pos = aPos,
		Pattern = aMatch,
		Type = aType
	};
	
	public static tMatchNode<tPos>
	UnTypedMatch<tPos>(
		tPos aPos,
		tMatchItemNode<tPos> aMatch
	) => Match(aPos, aMatch, null);
	
	public static tDefNode<tPos>
	Def<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch,
		tExpressionNode<tPos> aExpression
	) => new tDefNode<tPos> {
		Pos = aPos,
		Des = aMatch,
		Src = aExpression
	};
	
	public static tReturnIfNode<tPos>
	ReturnIf<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aResult,
		tExpressionNode<tPos> aCondition
	) => new tReturnIfNode<tPos> {
		Pos = aPos,
		Result = aResult,
		Condition = aCondition
	};
	
	public static tIfNode<tPos>
	If<tPos>(
		tPos aPos,
		mStream.tStream<(tExpressionNode<tPos>, tExpressionNode<tPos>)> aCases
	) => new tIfNode<tPos> {
		Pos = aPos,
		Cases = aCases
	};
	
	public static tIfMatchNode<tPos>
	IfMatch<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<(tMatchNode<tPos>, tExpressionNode<tPos>)> aCases
	) => new tIfMatchNode<tPos> {
		Pos = aPos,
		Expression = aExpression,
		Cases = aCases
	};
	
	public static tDefVarNode<tPos>
	DefVar<tPos>(
		tPos aPos,
		tIdentNode<tPos> aVar,
		tExpressionNode<tPos> aExpression,
		mStream.tStream<tMethodCallNode<tPos>> aMethodCalls
	) => new tDefVarNode<tPos> {
		Pos = aPos,
		Ident = aVar,
		Expression = aExpression,
		MethodCalls = aMethodCalls
	};
	
	public static tVarToValNode<tPos>
	VarToVal<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aObj
	) => new tVarToValNode<tPos> {
		Pos = aPos,
		Obj = aObj,
	};
	
	public static tMethodCallsNode<tPos>
	MethodCallStatment<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aObject,
		mStream.tStream<tMethodCallNode<tPos>> aMethodCalls
	) => new tMethodCallsNode<tPos> {
		Pos = aPos,
		Object = aObject,
		MethodCalls = aMethodCalls
	};
	
	public static tExpressionNode<tPos>
	PipeToRight<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aLeft,
		tExpressionNode<tPos> aRight
	) => new tPipeToRightNode<tPos> {
		Pos = aPos,
		Left = aLeft,
		Right = aRight
	};
	
	public static tExpressionNode<tPos>
	PipeToLeft<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aLeft,
		tExpressionNode<tPos> aRight
	) => new tPipeToLeftNode<tPos> {
		Pos = aPos,
		Left = aLeft,
		Right = aRight
	};
	
	public static tMethodCallNode<tPos>
	MethodCall<tPos>(
		tPos aPos,
		tIdentNode<tPos> aMethod,
		tExpressionNode<tPos> aAgument,
		tMatchNode<tPos> aResult
	) => new tMethodCallNode<tPos> {
		Pos = aPos,
		Method = aMethod,
		Argument = aAgument,
		Result = aResult
	};
	
	public static tBlockNode<tPos>
	Block<tPos>(
		tPos aPos,
		mStream.tStream<tCommandNode<tPos>> aCommands
	) => new tBlockNode<tPos> {
		Pos = aPos,
		Commands = aCommands
	};
	
	public static tModuleNode<tPos>
	Module<tPos>(
		tPos aPos,
		tImportNode<tPos> aImport,
		mStream.tStream<tCommandNode<tPos>> aCommands,
		tExportNode<tPos> aExport
	) => new tModuleNode<tPos> {
		Pos = aPos,
		Import = aImport,
		Export = aExport,
		Commands = aCommands
	};
	
	public static tImportNode<tPos>
	Import<tPos>(
		tPos aPos,
		tMatchNode<tPos> aMatch
	) => new tImportNode<tPos> {
		Pos = aPos,
		Match = aMatch 
	};
	
	public static tExportNode<tPos>
	Export<tPos>(
		tPos aPos,
		tExpressionNode<tPos> aExpression
	) => new tExportNode<tPos> {
		Pos = aPos,
		Expression = aExpression 
	};

	public static tBool
	AreEqual<tPos>(
		tNode<tPos> a1,
		tNode<tPos> a2
	) {
		if (ReferenceEquals(a1, a2)) {
			return true;
		}
		if (a1 is null || a2 is null || !a1.Pos.Equals(a2.Pos)) {
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
			case tIdentNode<tPos> Node1: {
				return a2 is tIdentNode<tPos> Node2 && Node1.Name == Node2.Name;
			}
			case tMatchFreeIdentNode<tPos> Node1: {
				return a2 is tMatchFreeIdentNode<tPos> Node2 && Node1.Name == Node2.Name;
			}
			case tMatchTupleNode<tPos> Node1: {
				return (
					a2 is tMatchTupleNode<tPos> Node2 &&
					mStream.Zip(Node1.Items, Node2.Items).All(_ => AreEqual(_._1, _._2))
				);
			}
			case tMatchNode<tPos> Node1: {
				return (
					a2 is tMatchNode<tPos> Node2 &&
					AreEqual(Node1.Pattern, Node2.Pattern) &&
					AreEqual(Node1.Type, Node2.Type)
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
						_ => (
							AreEqual(_._1.Key, _._2.Key) &&
							AreEqual(_._1.Value, _._2.Value)
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
						_ => (
							AreEqual(_._1.Key, _._2.Key) &&
							AreEqual(_._1.Match, _._2.Match)
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
					AreEqual(Node1.Ident, Node2.Ident) &&
					AreEqual(Node1.Lambda, Node2.Lambda)
				);
			}
			case tRecLambdasNode<tPos> Node1: {
				return (
					a2 is tRecLambdasNode<tPos> Node2 &&
					mStream.Zip(Node1.List, Node2.List).All(_ => AreEqual(_._1, _._2))
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
					AreEqual(Node1.Result, Node2.Result)
				);
			}
			case tMethodCallsNode<tPos> Node1: {
				return (
					a2 is tMethodCallsNode<tPos> Node2 &&
					AreEqual(Node1.Object, Node2.Object) &&
					mStream.Zip(Node1.MethodCalls, Node2.MethodCalls).All(_ => AreEqual(_._1, _._2))
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
					mStream.Zip(Node1.Items, Node2.Items).All(_ => AreEqual(_._1, _._2))
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
				throw mError.Error("not implemented: " + a1.GetType().Name);
			}
		}
		throw mError.Error("not implemented: " + a1.GetType().Name);
	}
}
