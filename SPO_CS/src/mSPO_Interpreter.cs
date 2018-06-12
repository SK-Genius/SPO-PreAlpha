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

public static class mSPO_Interpreter {
	
	//================================================================================
	public static mVM_Data.tData
	Run(
		tText aCode,
		mVM_Data.tData aImport,
		mStd.tAction<tText> aDebugStream
	//================================================================================
	) {
		mDebug.Assert(
			mSPO_Parser.Module.ParseText(
				aCode,
				aDebugStream
			).Match(out mSPO_AST.tModuleNode<mTextParser.tPos> ModuleNode)
		);
		return mIL_Interpreter.Run(
			mSPO2IL.MapModule(ModuleNode).Defs.ToLasyList().MapWithIndex(
				(aIndex, aCommands) => (mSPO2IL.TempDef(aIndex), aCommands.ToLasyList())
			),
			aImport,
			aDebugStream
		);
	}
}
