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
	
	public interface tExpressionNode {}
	public interface tMatchItemNode {}
	public interface tLiteralNode : tExpressionNode, tMatchItemNode {}
	public interface tCommandNode {}
	
	public struct tEmptyNode : tLiteralNode {
		override public tText ToString() => $"()";
	}
	
	public struct tFalseNode : tLiteralNode {
		override public tText ToString() => $"§FALSE";
	}
	
	public struct tTrueNode : tLiteralNode {
		override public tText ToString() => $"§TRUE";
	}
	
	public struct tTextNode : tLiteralNode {
		public tText Value;
		
		override public tText ToString() => $"('{Value}')";
	}
	
	public struct tNumberNode : tLiteralNode {
		public tInt32 Value;
		
		override public tText ToString() => $"({Value})";
	}
	
	public struct tIdentNode : tExpressionNode, tMatchItemNode {
		public tText Name;
		
		override public tText ToString() => $"(Ident: {Name})";
	}
	
	public struct tMatchTupleNode : tMatchItemNode {
		public mList.tList<tMatchNode> Items;
		
		override public tText ToString() => $"({Items.Map(a => a.ToString()).Join((aAkku, aItem) => $"{aAkku},{aItem}")})";
	}
	
	public struct tMatchNode : tMatchItemNode {
		public tMatchItemNode Pattern;
		public tExpressionNode Type;
		
		override public tText ToString()=> Pattern + (Type.IsNull() ? "" : " € " + Type);
	}
	
	public struct tPrefixNode : tExpressionNode {
		public tText Prefix;
		public tExpressionNode Element;
		
		override public tText ToString() => $"(#{Prefix} {Element})";
	}
	
	public struct tMatchPrefixNode : tMatchItemNode {
		public tText Prefix;
		public tMatchNode Match;
		
		override public tText ToString() => $"(#{Prefix} {Match})";
	}
	
	public struct tMatchGuardNode : tMatchItemNode {
		public tMatchNode Match;
		public tExpressionNode Guard;
		
		override public tText ToString() => $"({Match} | {Guard})";
	}
	
	public struct tLambdaNode : tExpressionNode {
		public tMatchNode Head;
		public tExpressionNode Body;
		
		override public tText ToString() => $"({Head} => {Body})";
	}
	
	public struct tMethodNode : tExpressionNode {
		public tMatchNode Obj;
		public tMatchNode Arg;
		public tBlockNode Body;
		
		override public tText ToString() => $"({Obj} : {Arg} {Body})";
	}
	
	public struct tBlockNode : tExpressionNode {
		public mList.tList<tCommandNode> Commands;
		
		override public tText ToString() => $"{{{Commands}}}";
	}
	
	public struct tCallNode : tExpressionNode {
		public tExpressionNode Func;
		public tExpressionNode Arg;
		
		override public tText ToString() => $"(Call: {Func}, {Arg})";
	}
	
	public struct tDefNode : tCommandNode {
		public tMatchNode Des;
		public tExpressionNode Src;
		
		override public tText ToString() => $"({Des} := {Src})";
	}
	
	public struct tRecLambdaItemNode {
		public tIdentNode Ident;
		public tLambdaNode Lambda;
		
		override public tText ToString() => $"({Ident} := {Lambda})";
	}
	
	public struct tRecLambdasNode : tCommandNode {
		public mList.tList<tRecLambdaItemNode> List;
		
		override public tText ToString() => $"§REC {{{List}}}";
	}
	
	public struct tReturnIfNode : tCommandNode {
		public tExpressionNode Result;
		public tExpressionNode Condition;
		
		override public tText ToString() => $"RETURN {Result} IF {Condition}";
	}
	
	public struct tIfNode : tExpressionNode {
		public mList.tList<(tExpressionNode, tExpressionNode)> Cases;
		
		override public tText ToString() => $"If {{ {Cases} }}";
	}
	
	public struct tIfMatchNode : tExpressionNode {
		public tExpressionNode Expression;
		public mList.tList<(tMatchNode, tExpressionNode)> Cases;
		
		override public tText ToString() => $"If {Expression} MATCH {{ {Cases} }}";
	}
	
	public struct tDefVarNode : tCommandNode {
		public tIdentNode Ident;
		public tExpressionNode Expression;
		public mList.tList<tMethodCallNode> MethodCalls;
		
		override public tText ToString() => $"$VAR {Ident} := {Expression}, {MethodCalls}";
	}
	
	public struct tVarToValNode : tExpressionNode {
		public tExpressionNode Obj;
		
		override public tText ToString() => $"{Obj}:=>";
	}
	
	public struct tMethodCallNode {
		public tIdentNode Method;
		public tExpressionNode Argument;
		public tMatchNode? Result;
		
		override public tText ToString() => $" {Method} {Argument} => {Result}";
	}
	
	public struct tMethodCallsNode : tCommandNode {
		public tExpressionNode Object;
		public mList.tList<tMethodCallNode> MethodCalls;
		
		override public tText ToString() => $" {Object} : {MethodCalls}";
	}
	
	public struct tTupleNode : tExpressionNode {
		public mList.tList<tExpressionNode> Items;
		
		override public tText ToString() => $"({Items.Map(a => a.ToString()).Join((a1, a2) => $"{a1}, {a2}")})";
	}
	
	public struct tImportNode {
		public tMatchNode Match;
	}
	
	public struct tExportNode {
		public tExpressionNode Expression;
	}
	
	public struct tModuleNode {
		public tImportNode Import;
		public tExportNode Export;
		public mList.tList<tCommandNode> Commands;
		
		override public tText ToString() => Commands.Map(
			a => a.ToString()
		).Join(
			(a1, a2) => $"{a1}\n{a2}"
		) + "\n";
	}
	
	//================================================================================
	public static readonly mStd.tFunc<tEmptyNode>
	Empty = (
	//================================================================================
	) => new tEmptyNode();
	
	//================================================================================
	public static readonly mStd.tFunc<tFalseNode>
	False = (
	//================================================================================
	) => new tFalseNode();
	
	//================================================================================
	public static readonly mStd.tFunc<tTrueNode>
	True = (
	//================================================================================
	) => new tTrueNode();
	
	//================================================================================
	public static readonly mStd.tFunc<tNumberNode, tInt32>
	Number = (
		aValue
	//================================================================================
	) => new tNumberNode {
		Value = aValue
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tTextNode, tText>
	Text = (
		aValue
	//================================================================================
	) => new tTextNode {
		Value = aValue
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tIdentNode, tText>
	Ident = (
		aName
	//================================================================================
	) => new tIdentNode {
		Name = "_" + aName
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tExpressionNode, mList.tList<tExpressionNode>>
	Tuple = (
		aItems
	//================================================================================
	) => {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				return Empty();
			}
			case 1: {
				mStd.Assert(aItems.Match(out var Head, out var _));
				return Head;
			}
			default: {
				return new tTupleNode {
					Items = aItems
				};
			}
		}
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tCallNode, tExpressionNode, tExpressionNode>
	Call = (
		aFunc,
		aArg
	//================================================================================
	) => new tCallNode {
		Func = aFunc,
		Arg = aArg
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tPrefixNode, tIdentNode, tExpressionNode>
	Prefix = (
		aPrefix,
		aElement
	//================================================================================
	) => new tPrefixNode {
		Prefix = aPrefix.Name,
		Element = aElement
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMatchPrefixNode, tIdentNode, tMatchNode>
	MatchPrefix = (
		aPrefix,
		aMatch
	//================================================================================
	) => new tMatchPrefixNode {
		Prefix = aPrefix.Name,
		Match = aMatch
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMatchGuardNode, tMatchNode, tExpressionNode>
	MatchGuard = (
		aMatch,
		aGuard
	//================================================================================
	) => new tMatchGuardNode {
		Match = aMatch,
		Guard = aGuard
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tLambdaNode, tMatchNode, tExpressionNode>
	Lambda = (
		aMatch,
		aBody
	//================================================================================
	) => new tLambdaNode {
		Head = aMatch,
		Body = aBody
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMethodNode, tMatchNode, tMatchNode, tBlockNode>
	Method = (
		aObjMatch,
		aArgMatch,
		aBody
	//================================================================================
	) => new tMethodNode {
		Obj = aObjMatch,
		Arg = aArgMatch,
		Body = aBody
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tRecLambdaItemNode, tIdentNode, tLambdaNode>
	RecLambdaItem = (
		aIdent,
		aLambda
	//================================================================================
	) => new tRecLambdaItemNode {
		Ident = aIdent,
		Lambda = aLambda
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tRecLambdasNode, mList.tList<tRecLambdaItemNode>>
	RecLambdas = (
		aList
	//================================================================================
	) => new tRecLambdasNode {
		List = aList
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMatchItemNode, mList.tList<tMatchNode>>
	MatchTuple = (
		aItems
	//================================================================================
	) => {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				throw null; // TODO
			}
			case 1: {
				mStd.Assert(aItems.Match(out var Head, out var _));
				return Head.Pattern;
			}
			default: {
				return new tMatchTupleNode {
					Items = aItems
				};
			}
		}
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMatchNode, tMatchItemNode, tExpressionNode>
	Match = (
		aMatch,
		aType
	//================================================================================
	) => new tMatchNode {
		Pattern = aMatch,
		Type = aType
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMatchNode, tMatchItemNode>
	MatchUntyped = (
		aMatch
	//================================================================================
	) => Match(aMatch, null);
	
	//================================================================================
	public static readonly mStd.tFunc<tDefNode, tMatchNode, tExpressionNode>
	Def = (
		aMatch,
		aExpression
	//================================================================================
	) => new tDefNode {
		Des = aMatch,
		Src = aExpression
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tReturnIfNode, tExpressionNode, tExpressionNode>
	ReturnIf = (
		aResult,
		aCondition
	//================================================================================
	) => new tReturnIfNode {
		Result = aResult,
		Condition = aCondition
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tIfNode, mList.tList<(tExpressionNode, tExpressionNode)>>
	If = (
		aCases
	//================================================================================
	) => new tIfNode {
		Cases = aCases
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tIfMatchNode, tExpressionNode, mList.tList<(tMatchNode, tExpressionNode)>>
	IfMatch = (
		aExpression,
		aCases
	//================================================================================
	) => new tIfMatchNode {
		Expression = aExpression,
		Cases = aCases
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tDefVarNode, tIdentNode, tExpressionNode, mList.tList<tMethodCallNode>>
	DefVar = (
		aVar,
		aExpression,
		aMethodCalls
	//================================================================================
	) => new tDefVarNode {
		Ident = aVar,
		Expression = aExpression,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tVarToValNode, tExpressionNode>
	VarToVal = (
		aObj
	//================================================================================
	) => new tVarToValNode {
		Obj = aObj,
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMethodCallsNode, tExpressionNode, mList.tList<tMethodCallNode>>
	MethodCallStatment = (
		aObject,
		aMethodCalls
	//================================================================================
	) => new tMethodCallsNode {
		Object = aObject,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMethodCallNode, tIdentNode, tExpressionNode, tMatchNode?>
	MethodCall = (
		aMethod,
		aAgument,
		aResult
	//================================================================================
	) => new tMethodCallNode {
		Method = aMethod,
		Argument = aAgument,
		Result = aResult
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tBlockNode, mList.tList<tCommandNode>>
	Block = (
		aCommands
	//================================================================================
	) => new tBlockNode {
		Commands = aCommands
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tModuleNode, tImportNode, mList.tList<tCommandNode>, tExportNode>
	Module = (
		aImport,
		aCommands,
		aExport
	//================================================================================
	) => new tModuleNode {
		Import = aImport,
		Export = aExport,
		Commands = aCommands
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tImportNode, tMatchNode>
	Import = (
		aMatch
	//================================================================================
	) => new tImportNode {
		Match = aMatch 
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tExportNode, tExpressionNode>
	Export = (
		aExpression
	//================================================================================
	) => new tExportNode {
		Expression = aExpression 
	};
	
	#region TEST
	
	public static readonly mTest.tTest
	Test = mTest.Tests(
		nameof(mSPO_AST),
		mTest.Test(
			"TODO",
			DebugStream => {
			}
		)
	);
	
	#endregion
}