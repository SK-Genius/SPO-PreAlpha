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

static class mProgram {
	static void Main() {
		var DebugOut = mStd.Action<tText>(a => System.Console.WriteLine(a));
		
		var StdLib = mStdLib.GetImportData();
		
		var StackModule = mSPO_Interpreter.Run(
			$@"
				§IMPORT ()
				
				§DEF EmptyStack = #Empty ()
				
				§DEF tStack... = t => [§RECURSIV tStack_ [[#Empty []] | [#Stack [t, tStack_]]]]
				
				§DEF Push...To... = (
					Head € t
					Tail € (.tStack t)
				) => #Stack (Head, Tail)
				
				§RECURSIV Map...With... = (
					Stack
					Func...
				) => §IF Stack MATCH {{
					(#Stack (Head, Tail)) => .Push (.Func Head) To (.Map Tail With Func...)
					(EmptyStack) => EmptyStack
				}}
				
				§EXPORT {{Empty: EmptyStack, PushTo: Push...To..., Map: Map...With...}}
			",
			mVM_Data.Empty(),
			DebugOut
		);
		
		var Test = mSPO_Interpreter.Run(
			$@"
				§IMPORT (
					{{...*...: ...*...}}
					{{Empty: EmptyStack, PushTo: Push...To..., Map: Map...With...}}
				)
				
				§EXPORT .Map (.Push 3 To (.Push 2 To (.Push 1 To EmptyStack))) With (x => x .* x)
			",
			mVM_Data.Tuple(StdLib, StackModule),
			DebugOut
		);
		
		mStd.Assert(Test.MatchPrefix("_Stack...", out var x1ab));
		mStd.Assert(x1ab.MatchTuple(out var x1a, out var x1b));
		mStd.Assert(x1a.MatchInt(out var x1a_) && x1a_ == 9);
		mStd.Assert(x1b.MatchPrefix("_Stack...", out var x2ab));
		mStd.Assert(x2ab.MatchTuple(out var x2a, out var x2b));
		mStd.Assert(x2a.MatchInt(out var x2a_) && x2a_ == 4);
		mStd.Assert(x2b.MatchPrefix("_Stack...", out var x3ab));
		mStd.Assert(x3ab.MatchTuple(out var x3a, out var x3b));
		mStd.Assert(x3a.MatchInt(out var x3a_) && x3a_ == 1);
		mStd.Assert(x3b.MatchPrefix("_Empty...", out var x4));
		mStd.Assert(x4.MatchEmpty());
		
		DebugOut("OK");
	}
}
