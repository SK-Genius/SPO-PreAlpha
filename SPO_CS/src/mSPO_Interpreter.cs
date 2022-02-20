//IMPORT mSPO_Parser.cs
//IMPORT mIL_Interpreter.cs
//IMPORT mSPO2IL.cs
//IMPORT mSpan.cs

#nullable enable

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mSPO_Interpreter {
	
	public static mVM_Data.tData
	Run(
		tText aCode,
		tText aIdent,
		mVM_Data.tData aImport,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var ModuleNode = mSPO_Parser.Module.ParseText(aCode, aIdent, aDebugStream);
		
		var InitScope = mSPO_AST_Types.UpdateMatchTypes(
			ModuleNode.Import.Match,
			mStd.cEmpty,
			mSPO_AST_Types.tTypeRelation.Sub,
			mStream.Stream<(tText Ident, mVM_Type.tType Type)>()
		).Then(
			a => a.Scope
		).ElseThrow();
		
		var Scope = ModuleNode.Commands.Reduce(
			mResult.OK(InitScope).AsResult<tText>(),
			(aResultScope, aCommand) => aResultScope.ThenTry(aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope))
		);
		
		var Module = mSPO2IL.MapModule(ModuleNode, mSpan.Merge);
		
		return mIL_GenerateOpcodes.Run(
			mIL_AST.Module(
				Module.TypeDef.ToStream(),
				Module.Defs.ToStream(
				).MapWithIndex(
					(aIndex, aDef) => mIL_AST.Def(mSPO2IL.TempDef(aIndex), aDef.Type, aDef.Commands.ToStream())
				)
			),
			aImport,
			a => $"{a.Start.Ident}({a.Start.Row}:{a.Start.Col} .. {a.Start.Row}:{a.Start.Col})",
			aDebugStream
		);
	}
}