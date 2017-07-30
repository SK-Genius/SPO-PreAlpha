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
		//================================================================================
		public tBool
		Equals(
			tEmptyNode a
		//================================================================================
		) => !a.IsNull();
		
		override public tText ToString() => $"()";
	}
	
	public struct tFalseNode : tLiteralNode {
		//================================================================================
		public tBool
		Equals(
			tFalseNode a
		//================================================================================
		) => !a.IsNull();
		
		override public tText ToString() => $"§FALSE";
	}
	
	public struct tTrueNode : tLiteralNode {
		//================================================================================
		public tBool
		Equals(
			tTrueNode a
		//================================================================================
		) => !a.IsNull();
		
		override public tText ToString() => $"§TRUE";
	}
	
	public struct tTextNode : tLiteralNode {
		public tText Value;
		
		//================================================================================
		public tBool
		Equals(
			tTextNode a
		//================================================================================
		) => !a.IsNull() && a.Value.Equals(Value);
		
		override public tText ToString() => $"('{Value}')";
	}
	
	public struct tNumberNode : tLiteralNode {
		public tInt32 Value;
		
		//================================================================================
		public tBool
		Equals(
			tNumberNode a
		//================================================================================
		) => !a.IsNull() && a.Value.Equals(Value);
		
		override public tText ToString() => $"({Value})";
	}
	
	public struct tIdentNode : tExpressionNode, tMatchItemNode {
		public tText Name;
		
		//================================================================================
		public tBool
		Equals(
			tIdentNode a
		//================================================================================
		) => !a.IsNull() && a.Name.Equals(Name);
		
		override public tText ToString() => $"(Ident: {Name})";
	}
	
	public struct tMatchTupleNode : tMatchItemNode {
		public mList.tList<tMatchNode> Items;
		
		//================================================================================
		public tBool
		Equals(
			tMatchTupleNode a
		//================================================================================
		) => !a.IsNull() && a.Items.Equals(Items);
		
		override public tText ToString() => $"({Items.Map(a => a.ToString()).Join((aAkku, aItem) => $"{aAkku},{aItem}")})";
	}
	
	public struct tMatchNode : tMatchItemNode {
		public tMatchItemNode Pattern;
		public tExpressionNode Type;
		
		//================================================================================
		public tBool
		Equals(
			tMatchNode a
		//================================================================================
		) => !a.IsNull() && a.Pattern.Equals(Pattern) && (a.Type.IsNull() ? Type.IsNull() : a.Type.Equals(Type));
		
		override public tText ToString()=> Pattern + (Type.IsNull() ? "" : " € " + Type);
	}
	
	public struct tPrefixNode : tExpressionNode {
		public tText Prefix;
		public tExpressionNode Element;
		
		//================================================================================
		public tBool
		Equals(
			tPrefixNode a
		//================================================================================
		) => !a.IsNull() && a.Prefix.Equals(Prefix) && a.Element.Equals(Element);
		
		override public tText ToString() => $"(#{Prefix} {Element})";
	}
	
	public struct tMatchPrefixNode : tMatchItemNode {
		public tText Prefix;
		public tMatchNode Match;
		
		//================================================================================
		public tBool
		Equals(
			tMatchPrefixNode a
		//================================================================================
		) => !a.IsNull() && a.Prefix.Equals(Prefix) && a.Match.Equals(Match);
		
		override public tText ToString() => $"(#{Prefix} {Match})";
	}
	
	public struct tMatchGuardNode : tMatchItemNode {
		public tMatchNode Match;
		public tExpressionNode Guard;
		
		//================================================================================
		public tBool
		Equals(
			tMatchGuardNode a
		//================================================================================
		) => !a.IsNull() && a.Match.Equals(Match) && a.Guard.Equals(Guard);
		
		override public tText ToString() => $"({Match} | {Guard})";
	}
	
	public struct tLambdaNode : tExpressionNode {
		public tMatchNode Head;
		public tExpressionNode Body;
		
		//================================================================================
		public tBool
		Equals(
			tLambdaNode a
		//================================================================================
		) => !a.IsNull() && a.Head.Equals(Head) && a.Body.Equals(Body);
		
		override public tText ToString() => $"({Head} => {Body})";
	}
	
	public struct tMethodeNode : tExpressionNode {
		public tMatchNode Obj;
		public tMatchNode Arg;
		public tBlockNode Body;
		
		//================================================================================
		public tBool
		Equals(
			tMethodeNode a
		//================================================================================
		) => !a.IsNull() && a.Obj.Equals(Obj) && a.Arg.Equals(Arg) && a.Body.Equals(Body);
		
		override public tText ToString() => $"({Obj} : {Arg} {Body})";
	}
	
	public struct tBlockNode : tExpressionNode {
		public mList.tList<tCommandNode> Commands;
		
		//================================================================================
		public tBool
		Equals(
			tBlockNode a
		//================================================================================
		) => !a.IsNull() && a.Commands.Equals(Commands);
		
		override public tText ToString() => $"{{{Commands}}}";
	}
	
	public struct tCallNode : tExpressionNode {
		public tExpressionNode Func;
		public tExpressionNode Arg;
		
		//================================================================================
		public tBool
		Equals(
			tCallNode a
		//================================================================================
		) => !a.IsNull() && a.Func.Equals(Func) && a.Arg.Equals(Arg);
		
		override public tText ToString() => $"(Call: {Func}, {Arg})";
	}
	
	public struct tDefNode : tCommandNode {
		public tMatchNode Des;
		public tExpressionNode Src;
		
		//================================================================================
		public tBool
		Equals(
			tDefNode a
		//================================================================================
		) => !a.IsNull() && a.Des.Equals(Des) && a.Src.Equals(Src);
		
		override public tText ToString() => $"({Des} := {Src})";
	}
	
	public struct tRecLambdaItemNode {
		public tIdentNode Ident;
		public tLambdaNode Lambda;
		
		//================================================================================
		public tBool
		Equals(
			tRecLambdaItemNode a
		//================================================================================
		) => !a.IsNull() && a.Ident.Equals(Ident) && a.Lambda.Equals(Lambda);
		
		override public tText ToString() => $"({Ident} := {Lambda})";
	}
	
	public struct tRecLambdasNode : tCommandNode {
		public mList.tList<tRecLambdaItemNode> List;
		
		//================================================================================
		public tBool
		Equals(
			tRecLambdasNode a
		//================================================================================
		) => !a.IsNull() && a.List.Equals(List);
		
		override public tText ToString() => $"§REC {{{List}}}";
	}
	
	public struct tReturnIfNode : tCommandNode {
		public tExpressionNode Result;
		public tExpressionNode Condition;
		
		//================================================================================
		public tBool
		Equals(
			tReturnIfNode a
		//================================================================================
		) => !a.IsNull() && a.Result.Equals(Result) && a.Condition.Equals(Condition);
		
		override public tText ToString() => $"RETURN {Result} IF {Condition}";
	}
	
	public struct tIfNode : tExpressionNode {
		public mList.tList<(tExpressionNode, tExpressionNode)> Cases;
		
		//================================================================================
		public tBool
		Equals(
			tIfNode a
		//================================================================================
		) => !a.IsNull() && a.Cases.Equals(Cases);
		
		override public tText ToString() => $"If {{ {Cases} }}";
	}
	
	public struct tIfMatchNode : tExpressionNode {
		public tExpressionNode Expression;
		public mList.tList<(tMatchNode, tExpressionNode)> Cases;
		
		//================================================================================
		public tBool
		Equals(
			tIfMatchNode a
		//================================================================================
		) => !a.IsNull() && a.Cases.Equals(Cases) && a.Expression.Equals(Expression);
		
		override public tText ToString() => $"If {Expression} MATCH {{ {Cases} }}";
	}
	
	public struct tDefVarNode : tCommandNode {
		public tIdentNode Ident;
		public tExpressionNode Expression;
		public mList.tList<tMethodCallNode> MethodCalls;
		
		//================================================================================
		public tBool
		Equals(
			tDefVarNode a
		//================================================================================
		) => (
			!a.IsNull() &&
			a.Ident.Equals(Ident) &&
			a.Expression.Equals(Expression) &&
			a.MethodCalls.Equals(MethodCalls)
		);
		
		override public tText ToString() => $"$VAR {Ident} := {Expression}, {MethodCalls}";
	}
	
	public struct tMethodCallNode {
		public tIdentNode Method;
		public tExpressionNode Argument;
		
		//================================================================================
		public tBool
		Equals(
			tMethodCallNode a
		//================================================================================
		) => !a.IsNull() && a.Method.Equals(Method) && a.Argument.Equals(Argument);
		
		override public tText ToString() => $" {Method} {Argument}";
	}
	
	public struct tMethodCallsNode : tCommandNode {
		public tIdentNode Var;
		public mList.tList<tMethodCallNode> MethodCalls;
		
		//================================================================================
		public tBool
		Equals(
			tMethodCallsNode a
		//================================================================================
		) => !a.IsNull() && a.Var.Equals(Var) && a.MethodCalls.Equals(MethodCalls);
		
		override public tText ToString() => $" {Var} : {MethodCalls}";
	}
	
	public struct tTupleNode : tExpressionNode {
		public mList.tList<tExpressionNode> Items;
		
		//================================================================================
		public tBool
		Equals(
			tTupleNode a
		//================================================================================
		) => !a.IsNull() && a.Items.Equals(Items);
		
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
		
		//================================================================================
		public tBool
		Equals(
			tModuleNode a
		//================================================================================
		) => !a.IsNull() && a.Commands.Equals(Commands);
		
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
	public static readonly mStd.tFunc<tMethodeNode, tMatchNode, tMatchNode, tBlockNode>
	Methode = (
		aObjMatch,
		aArgMatch,
		aBody
	//================================================================================
	) => new tMethodeNode {
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
	public static readonly mStd.tFunc<tMethodCallsNode, tIdentNode, mList.tList<tMethodCallNode>>
	MethodCallStatment = (
		aVar,
		aMethodCalls
	//================================================================================
	) => new tMethodCallsNode {
		Var = aVar,
		MethodCalls = aMethodCalls
	};
	
	//================================================================================
	public static readonly mStd.tFunc<tMethodCallNode, tIdentNode, tExpressionNode>
	MethodCall = (
		aMethod,
		aAgument
	//================================================================================
	) => new tMethodCallNode {
		Method = aMethod,
		Argument = aAgument
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