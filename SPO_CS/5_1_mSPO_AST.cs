﻿using System.Runtime.InteropServices;
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
	
	public class tTextNode : tLiteralNode {
		internal tText _Value;
		
		//================================================================================
		public tBool
		Equals(
			tTextNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Value.Equals(_Value);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tTextNode); }
		public override tText ToString() { return "('"+_Value+"')"; }
	}
	
	public class tNumberNode : tLiteralNode {
		internal tInt32 _Value;
		
		//================================================================================
		public tBool
		Equals(
			tNumberNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Value.Equals(_Value);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tNumberNode); }
		public override tText ToString() { return "("+_Value+")"; }
	}
	
	public class tIdentNode : tExpressionNode, tMatchItemNode {
		internal tText _Name;
		
		//================================================================================
		public tBool
		Equals(
			tIdentNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Name.Equals(_Name);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tIdentNode); }
		public override tText ToString() { return "(Ident: "+_Name+")"; }
	}
	
	public class tMatchTupleNode : tMatchItemNode {
		internal mList.tList<tMatchNode> _Items;
		
		//================================================================================
		public tBool
		Equals(
			tMatchTupleNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Items.Equals(_Items);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tMatchTupleNode); }
		public override tText ToString() { return "("+_Items.Map(a => a.ToString()).Join((aAkku, aItem) => aAkku + "," + aItem)+")"; }
	}
	
	public class tMatchNode : tMatchItemNode {
		internal tMatchItemNode _Pattern;
		internal tExpressionNode _Type;
		
		//================================================================================
		public tBool
		Equals(
			tMatchNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Pattern.Equals(_Pattern) && (a._Type.IsNull() ? _Type.IsNull() : a._Type.Equals(_Type));
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tMatchNode); }
		public override tText ToString() {
			return _Pattern.ToString() + (_Type.IsNull() ? "" : " € " + _Type);
		}
	}
	
	public class tPrefixNode : tExpressionNode {
		internal tText _Prefix;
		internal tExpressionNode _Element;
		
		//================================================================================
		public tBool
		Equals(
			tPrefixNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Prefix.Equals(_Prefix) && a._Element.Equals(_Element);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tPrefixNode); }
		public override tText ToString() { return "(#"+_Prefix+" "+_Element+")"; }
	}
	
	public class tMatchPrefixNode : tMatchItemNode {
		internal tText _Prefix;
		internal tMatchNode _Match;
		
		//================================================================================
		public tBool
		Equals(
			tMatchPrefixNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Prefix.Equals(_Prefix) && a._Match.Equals(_Match);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tMatchPrefixNode); }
		public override tText ToString() { return "(#"+_Prefix+" "+_Match+")"; }
	}
	
	public class tLambdaNode : tExpressionNode {
		internal tMatchNode _Head;
		internal tExpressionNode _Body;
		
		//================================================================================
		public tBool
		Equals(
			tLambdaNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Head.Equals(_Head) && a._Body.Equals(_Body);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tLambdaNode); }
		public override tText ToString() { return "("+_Head+" => "+_Body+")"; }
	}
	
	public class tBlockNode : tExpressionNode {
		internal mList.tList<tCommandNode> _Commands;
		
		//================================================================================
		public tBool
		Equals(
			tBlockNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Commands.Equals(_Commands);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tBlockNode); }
		public override tText ToString() { return "{"+_Commands+"}"; }
	}
	
	public class tCallNode : tExpressionNode {
		internal tExpressionNode _Func;
		internal tExpressionNode _Arg;
		
		//================================================================================
		public tBool
		Equals(
			tCallNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Func.Equals(_Func) && a._Arg.Equals(_Arg);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tCallNode); }
		public override tText ToString() { return "(Call: "+_Func+", "+_Arg+")"; }
	}
	
	public class tAssignmantNode : tCommandNode {
		internal tMatchNode _Des;
		internal tExpressionNode _Src;
		
		//================================================================================
		public tBool
		Equals(
			tAssignmantNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Des.Equals(_Des) && a._Src.Equals(_Src);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tAssignmantNode); }
		public override tText ToString() { return "("+_Des+" := "+_Src+")"; }
	}
	
	public class tReturnNode : tCommandNode {
		internal tExpressionNode _Result;
		
		//================================================================================
		public tBool
		Equals(
			tReturnNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Result.Equals(_Result);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tReturnNode); }
		public override tText ToString() { return "RETURN "+_Result; }
	}
	
	public class tTupleNode : tExpressionNode {
		internal mList.tList<tExpressionNode> _Items;
		
		//================================================================================
		public tBool
		Equals(
			tTupleNode a
		//================================================================================
		) {
			return !a.IsNull() && a._Items.Equals(_Items);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tTupleNode); }
		public override tText ToString() {
			return "("+_Items.Map(a => a.ToString()).Join((a1, a2) => a1+", "+a2)+")";
		}
	}
	
	public class tImportNode {
		internal mSPO_AST.tMatchNode _Match;
	}
	
	public class tExportNode {
		// TODO: ExportNode
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
		) {
			return !a.IsNull() && a._Commands.Equals(_Commands);
		}
		
		public override tBool Equals(object a) { return this.Equals(a as tModuleNode); }
		public override tText ToString() {
			return _Commands.Map(a => a.ToString()).Join((a1, a2) => a1+"\n"+a2)+"\n";
		}
	}
	
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
	public static mStd.tFunc<tTupleNode, mList.tList<tExpressionNode>>
	Tuple = (
		aItems
	//================================================================================
	) => new tTupleNode {
		 _Items = aItems
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
	public static mStd.tFunc<tMatchTupleNode, mList.tList<tMatchNode>>
	MatchTuple = (
		aItems
	//================================================================================
	) => new tMatchTupleNode {
		_Items = aItems
	};
	
	//================================================================================
	public static mStd.tFunc<tMatchNode, tMatchItemNode, tExpressionNode>
	Match = (
		aMatch,
		aType
	//================================================================================
	) => {
		return new tMatchNode {
			_Pattern = aMatch,
			_Type = aType
		};
	};
	
	//================================================================================
	public static mStd.tFunc<tAssignmantNode, tMatchNode, tExpressionNode>
	Assignment = (
		aMatch,
		aExpression
	//================================================================================
	) => {
		return new tAssignmantNode {
			_Des = aMatch,
			_Src = aExpression
		};
	};
	
	//================================================================================
	public static mStd.tFunc<tReturnNode, tExpressionNode>
	Return = (
		aResult
	//================================================================================
	) => {
		return new tReturnNode {
			_Result = aResult
		};
	};
	
	//================================================================================
	public static mStd.tFunc<tBlockNode, mList.tList<tCommandNode>>
	Block = (
		aCommands
	//================================================================================
	) => {
		return new tBlockNode {
			_Commands = aCommands
		};
	};
	
	//================================================================================
	public static mStd.tFunc<tModuleNode, tImportNode, mList.tList<tCommandNode>>
	Module = (
		aImport,
		aCommands
	//================================================================================
	) => {
		// TODO: Module (Import- & Export- Args)
		return new tModuleNode {
			_Import = aImport,
			_Export = null,
			_Commands = aCommands
		};
	};
	
	//================================================================================
	public static mStd.tFunc<tImportNode, tMatchNode>
	Import = (
		aMatch
	//================================================================================
	) => {
		// TODO: Module (Import- & Export- Args)
		return new tImportNode {
			_Match = aMatch 
		};
	};
	
	#region TEST
	
	// TODO: add tests
	
	public static mStd.tFunc<mTest.tResult, mStd.tAction<tText>, mList.tList<tText>> Test = mTest.Tests(
		mStd.Tuple(
			"TODO",
			mTest.Test(
				(mStd.tAction<tText> DebugStream) => {
					return true;
				}
			)
		)
	);
	
	#endregion
}