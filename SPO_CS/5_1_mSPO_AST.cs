using System.Runtime.InteropServices;
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
	
	public class tEmptyNode : tLiteralNode {
		//================================================================================
		public tBool
		Equals(
			tEmptyNode a
		//================================================================================
		) => !a.IsNull();
		
		override public tBool Equals(object a) => this.Equals(a as tEmptyNode);
		override public tText ToString() => $"()";
	}
	
	public class tFalseNode : tLiteralNode {
		//================================================================================
		public tBool
		Equals(
			tFalseNode a
		//================================================================================
		) => !a.IsNull();
		
		override public tBool Equals(object a) => this.Equals(a as tFalseNode);
		override public tText ToString() => $"§FALSE";
	}
	
	public class tTrueNode : tLiteralNode {
		//================================================================================
		public tBool
		Equals(
			tTrueNode a
		//================================================================================
		) => !a.IsNull();
		
		override public tBool Equals(object a) => this.Equals(a as tTrueNode);
		override public tText ToString() => $"§TRUE";
	}
	
	public class tTextNode : tLiteralNode {
		internal tText _Value;
		
		//================================================================================
		public tBool
		Equals(
			tTextNode a
		//================================================================================
		) => !a.IsNull() && a._Value.Equals(_Value);
		
		override public tBool Equals(object a) => this.Equals(a as tTextNode);
		override public tText ToString() => $"('{_Value}')";
	}
	
	public class tNumberNode : tLiteralNode {
		internal tInt32 _Value;
		
		//================================================================================
		public tBool
		Equals(
			tNumberNode a
		//================================================================================
		) => !a.IsNull() && a._Value.Equals(_Value);
		
		override public tBool Equals(object a) => this.Equals(a as tNumberNode);
		override public tText ToString() => $"({_Value})";
	}
	
	public class tIdentNode : tExpressionNode, tMatchItemNode {
		internal tText _Name;
		
		//================================================================================
		public tBool
		Equals(
			tIdentNode a
		//================================================================================
		) => !a.IsNull() && a._Name.Equals(_Name);
		
		override public tBool Equals(object a) => this.Equals(a as tIdentNode);
		override public tText ToString() => $"(Ident: {_Name})";
	}
	
	public class tMatchTupleNode : tMatchItemNode {
		internal mList.tList<tMatchNode> _Items;
		
		//================================================================================
		public tBool
		Equals(
			tMatchTupleNode a
		//================================================================================
		) => !a.IsNull() && a._Items.Equals(_Items);
		
		override public tBool Equals(object a) => this.Equals(a as tMatchTupleNode);
		override public tText ToString() => $"({_Items.Map(a => a.ToString()).Join((aAkku, aItem) => $"{aAkku},{aItem}")})";
	}
	
	public class tMatchNode : tMatchItemNode {
		internal tMatchItemNode _Pattern;
		internal tExpressionNode _Type;
		
		//================================================================================
		public tBool
		Equals(
			tMatchNode a
		//================================================================================
		) => !a.IsNull() && a._Pattern.Equals(_Pattern) && (a._Type.IsNull() ? _Type.IsNull() : a._Type.Equals(_Type));
		
		override public tBool Equals(object a) => this.Equals(a as tMatchNode);
		override public tText ToString()=> _Pattern + (_Type.IsNull() ? "" : " € " + _Type);
	}
	
	public class tPrefixNode : tExpressionNode {
		internal tText _Prefix;
		internal tExpressionNode _Element;
		
		//================================================================================
		public tBool
		Equals(
			tPrefixNode a
		//================================================================================
		) => !a.IsNull() && a._Prefix.Equals(_Prefix) && a._Element.Equals(_Element);
		
		override public tBool Equals(object a) => this.Equals(a as tPrefixNode);
		override public tText ToString() => $"(#{_Prefix} {_Element})";
	}
	
	public class tMatchPrefixNode : tMatchItemNode {
		internal tText _Prefix;
		internal tMatchNode _Match;
		
		//================================================================================
		public tBool
		Equals(
			tMatchPrefixNode a
		//================================================================================
		) => !a.IsNull() && a._Prefix.Equals(_Prefix) && a._Match.Equals(_Match);
		
		override public tBool Equals(object a) => this.Equals(a as tMatchPrefixNode);
		override public tText ToString() => $"(#{_Prefix} {_Match})";
	}
	
	public class tMatchGuardNode : tMatchItemNode {
		internal tMatchNode _Match;
		internal tExpressionNode _Guard;
		
		//================================================================================
		public tBool
		Equals(
			tMatchGuardNode a
		//================================================================================
		) => !a.IsNull() && a._Match.Equals(_Match) && a._Guard.Equals(_Guard);
		
		override public tBool Equals(object a) => this.Equals(a as tMatchGuardNode);
		override public tText ToString() => $"({_Match} | {_Guard})";
	}
	
	public class tLambdaNode : tExpressionNode {
		internal tMatchNode _Head;
		internal tExpressionNode _Body;
		
		//================================================================================
		public tBool
		Equals(
			tLambdaNode a
		//================================================================================
		) => !a.IsNull() && a._Head.Equals(_Head) && a._Body.Equals(_Body);
		
		override public tBool Equals(object a) => this.Equals(a as tLambdaNode);
		override public tText ToString() => $"({_Head} => {_Body})";
	}
	
	public class tBlockNode : tExpressionNode {
		internal mList.tList<tCommandNode> _Commands;
		
		//================================================================================
		public tBool
		Equals(
			tBlockNode a
		//================================================================================
		) => !a.IsNull() && a._Commands.Equals(_Commands);
		
		override public tBool Equals(object a) => this.Equals(a as tBlockNode);
		override public tText ToString() => $"{{{_Commands}}}";
	}
	
	public class tCallNode : tExpressionNode {
		internal tExpressionNode _Func;
		internal tExpressionNode _Arg;
		
		//================================================================================
		public tBool
		Equals(
			tCallNode a
		//================================================================================
		) => !a.IsNull() && a._Func.Equals(_Func) && a._Arg.Equals(_Arg);
		
		override public tBool Equals(object a) => this.Equals(a as tCallNode);
		override public tText ToString() => $"(Call: {_Func}, {_Arg})";
	}
	
	public class tAssignmantNode : tCommandNode {
		internal tMatchNode _Des;
		internal tExpressionNode _Src;
		
		//================================================================================
		public tBool
		Equals(
			tAssignmantNode a
		//================================================================================
		) => !a.IsNull() && a._Des.Equals(_Des) && a._Src.Equals(_Src);
		
		override public tBool Equals(object a) => this.Equals(a as tAssignmantNode);
		override public tText ToString() => $"({_Des} := {_Src})";
	}
	
	public class tRecLambdaItemNode {
		internal tIdentNode _Ident;
		internal tLambdaNode _Lambda;
		
		//================================================================================
		public tBool
		Equals(
			tRecLambdaItemNode a
		//================================================================================
		) => !a.IsNull() && a._Ident.Equals(_Ident) && a._Lambda.Equals(_Lambda);
		
		override public tBool Equals(object a) => this.Equals(a as tRecLambdaItemNode);
		override public tText ToString() => $"({_Ident} := {_Lambda})";
	}
	
	public class tRecLambdasNode : tCommandNode {
		internal mList.tList<tRecLambdaItemNode> _List;
		
		//================================================================================
		public tBool
		Equals(
			tRecLambdasNode a
		//================================================================================
		) => !a.IsNull() && a._List.Equals(_List);
		
		override public tBool Equals(object a) => this.Equals(a as tRecLambdasNode);
		override public tText ToString() => $"§REC {{{_List}}}";
	}
	
	public class tReturnIfNode : tCommandNode {
		internal tExpressionNode _Result;
		internal tExpressionNode _Condition;
		
		//================================================================================
		public tBool
		Equals(
			tReturnIfNode a
		//================================================================================
		) => !a.IsNull() && a._Result.Equals(_Result) && a._Condition.Equals(_Condition);
		
		override public tBool Equals(object a) => this.Equals(a as tReturnIfNode);
		override public tText ToString() => $"RETURN {_Result} IF {_Condition}";
	}
	
	public class tIfNode : tExpressionNode {
		internal mList.tList<mStd.tTuple<tExpressionNode, tExpressionNode>> _Cases;
		
		//================================================================================
		public tBool
		Equals(
			tIfNode a
		//================================================================================
		) => !a.IsNull() && a._Cases.Equals(_Cases);
		
		override public tBool Equals(object a) => this.Equals(a as tIfNode);
		override public tText ToString() => $"If {{ {_Cases} }}";
	}
	
	public class tIfMatchNode : tExpressionNode {
		internal tExpressionNode _Expression;
		internal mList.tList<mStd.tTuple<tMatchNode, tExpressionNode>> _Cases;
		
		//================================================================================
		public tBool
		Equals(
			tIfMatchNode a
		//================================================================================
		) => !a.IsNull() && a._Cases.Equals(_Cases) && a._Expression.Equals(_Expression);
		
		override public tBool Equals(object a) => this.Equals(a as tIfMatchNode);
		override public tText ToString() => $"If {_Expression} MATCH {{ {_Cases} }}";
	}
	
	public class tTupleNode : tExpressionNode {
		internal mList.tList<tExpressionNode> _Items;
		
		//================================================================================
		public tBool
		Equals(
			tTupleNode a
		//================================================================================
		) => !a.IsNull() && a._Items.Equals(_Items);
		
		override public tBool Equals(object a) => this.Equals(a as tTupleNode);
		override public tText ToString() => $"({_Items.Map(a => a.ToString()).Join((a1, a2) => $"{a1}, {a2}")})";
	}
	
	public class tImportNode {
		internal mSPO_AST.tMatchNode _Match;
	}
	
	public class tExportNode {
		internal mSPO_AST.tExpressionNode _Expression;
	}
	
	public class tModuleNode {
		internal tImportNode _Import;
		internal tExportNode _Export;
		internal mList.tList<tCommandNode> _Commands;
		
		//================================================================================
		public tBool
		Equals(
			tModuleNode a
		//================================================================================
		) => !a.IsNull() && a._Commands.Equals(_Commands);
		
		override public tBool Equals(object a) => this.Equals(a as tModuleNode);
		override public tText ToString() => _Commands.Map(
			a => a.ToString()
		).Join(
			(a1, a2) => $"{a1}\n{a2}"
		) + "\n";
	}
	
	//================================================================================
	public static mStd.tFunc<tEmptyNode>
	Empty = (
	//================================================================================
	) => new tEmptyNode();
	
	//================================================================================
	public static mStd.tFunc<tFalseNode>
	False = (
	//================================================================================
	) => new tFalseNode();
	
	//================================================================================
	public static mStd.tFunc<tTrueNode>
	True = (
	//================================================================================
	) => new tTrueNode();
	
	//================================================================================
	public static mStd.tFunc<tNumberNode, tInt32>
	Number = (
		aValue
	//================================================================================
	) => new tNumberNode {
		_Value = aValue
	};
	
	//================================================================================
	public static mStd.tFunc<tTextNode, tText>
	Text = (
		aValue
	//================================================================================
	) => new tTextNode {
		_Value = aValue
	};
	
	//================================================================================
	public static mStd.tFunc<tIdentNode, tText>
	Ident = (
		aName
	//================================================================================
	) => new tIdentNode {
		_Name = "_" + aName
	};
	
	//================================================================================
	public static mStd.tFunc<tExpressionNode, mList.tList<tExpressionNode>>
	Tuple = (
		aItems
	//================================================================================
	) => {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				return Empty();
			}
			case 1: {
				return aItems._Head;
			}
			default: {
				return new tTupleNode {
					_Items = aItems
				};
			}
		}
	};
	
	//================================================================================
	public static mStd.tFunc<tCallNode, tExpressionNode, tExpressionNode>
	Call = (
		aFunc,
		aArg
	//================================================================================
	) => new tCallNode {
		_Func = aFunc,
		_Arg = aArg
	};
	
	//================================================================================
	public static mStd.tFunc<tPrefixNode, tIdentNode, tExpressionNode>
	Prefix = (
		aPrefix,
		aElement
	//================================================================================
	) => new tPrefixNode {
		_Prefix = aPrefix._Name,
		_Element = aElement
	};
	
	//================================================================================
	public static mStd.tFunc<tMatchPrefixNode, tIdentNode, tMatchNode>
	MatchPrefix = (
		aPrefix,
		aMatch
	//================================================================================
	) => new tMatchPrefixNode {
		_Prefix = aPrefix._Name,
		_Match = aMatch
	};
	
	//================================================================================
	public static mStd.tFunc<tMatchGuardNode, tMatchNode, tExpressionNode>
	MatchGuard = (
		aMatch,
		aGuard
	//================================================================================
	) => new tMatchGuardNode {
		_Match = aMatch,
		_Guard = aGuard
	};
	
	//================================================================================
	public static mStd.tFunc<tLambdaNode, tMatchNode, tExpressionNode>
	Lambda = (
		aMatch,
		aBody
	//================================================================================
	) => new tLambdaNode {
		_Head = aMatch,
		_Body = aBody
	};
	
	//================================================================================
	public static mStd.tFunc<tRecLambdaItemNode, tIdentNode, tLambdaNode>
	RecLambdaItem = (
		aIdent,
		aLambda
	//================================================================================
	) => new tRecLambdaItemNode {
		_Ident = aIdent,
		_Lambda = aLambda
	};
	
	//================================================================================
	public static mStd.tFunc<tRecLambdasNode, mList.tList<tRecLambdaItemNode>>
	RecLambdas = (
		aList
	//================================================================================
	) => new tRecLambdasNode {
		_List = aList
	};
	
	//================================================================================
	public static mStd.tFunc<tMatchItemNode, mList.tList<tMatchNode>>
	MatchTuple = (
		aItems
	//================================================================================
	) => {
		switch (aItems.Take(2).ToArrayList().Size()) {
			case 0: {
				mStd.Assert(false); // TODO
				return null;
			}
			case 1: {
				return aItems._Head._Pattern;
			}
			default: {
				return new tMatchTupleNode {
					_Items = aItems
				};
			}
		}
	};
	
	//================================================================================
	public static mStd.tFunc<tMatchNode, tMatchItemNode, tExpressionNode>
	Match = (
		aMatch,
		aType
	//================================================================================
	) => new tMatchNode {
		_Pattern = aMatch,
		_Type = aType
	};
	
	//================================================================================
	public static mStd.tFunc<tMatchNode, tMatchItemNode>
	MatchUntyped = (
		aMatch
	//================================================================================
	) => Match(aMatch, null);
	
	//================================================================================
	public static mStd.tFunc<tAssignmantNode, tMatchNode, tExpressionNode>
	Assignment = (
		aMatch,
		aExpression
	//================================================================================
	) => new tAssignmantNode {
		_Des = aMatch,
		_Src = aExpression
	};
	
	//================================================================================
	public static mStd.tFunc<tReturnIfNode, tExpressionNode, tExpressionNode>
	ReturnIf = (
		aResult,
		aCondition
	//================================================================================
	) => new tReturnIfNode {
		_Result = aResult,
		_Condition = aCondition
	};
	
	//================================================================================
	public static mStd.tFunc<tIfNode, mList.tList<mStd.tTuple<tExpressionNode, tExpressionNode>>>
	If = (
		aCases
	//================================================================================
	) => new tIfNode {
		_Cases = aCases
	};
	
	//================================================================================
	public static mStd.tFunc<tIfMatchNode, tExpressionNode, mList.tList<mStd.tTuple<tMatchNode, tExpressionNode>>>
	IfMatch = (
		aExpression,
		aCases
	//================================================================================
	) => new tIfMatchNode {
		_Expression = aExpression,
		_Cases = aCases
	};
	
	//================================================================================
	public static mStd.tFunc<tBlockNode, mList.tList<tCommandNode>>
	Block = (
		aCommands
	//================================================================================
	) => new tBlockNode {
		_Commands = aCommands
	};
	
	//================================================================================
	public static mStd.tFunc<tModuleNode, tImportNode, mList.tList<tCommandNode>, tExportNode>
	Module = (
		aImport,
		aCommands,
		aExport
	//================================================================================
	) => new tModuleNode {
		_Import = aImport,
		_Export = aExport,
		_Commands = aCommands
	};
	
	//================================================================================
	public static mStd.tFunc<tImportNode, tMatchNode>
	Import = (
		aMatch
	//================================================================================
	) => new tImportNode {
		_Match = aMatch 
	};
	
	//================================================================================
	public static mStd.tFunc<tExportNode, tExpressionNode>
	Export = (
		aExpression
	//================================================================================
	) => new tExportNode {
		_Expression = aExpression 
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