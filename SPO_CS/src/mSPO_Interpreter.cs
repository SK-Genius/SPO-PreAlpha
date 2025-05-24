// IMPORT Common/mStd
// IMPORT Common/mSpan
// IMPORT Common/mResult
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT Common/mArrayList
// IMPORT mTokenizer
// IMPORT mVM_Type
// IMPORT mVM_Data
// IMPORT mVM
// IMPORT mIL_AST
// IMPORT mSPO2IL
// IMPORT mSPO_AST
// IMPORT mSPO_AST_Types
// IMPORT mSPO_Parser

public static class
mSPO_Interpreter {
	// TODO: return tResult
	public static mVM_Data.tData
	Run(
		tText aCode,
		tText aId,
		mVM_Data.tData aImport,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var ModuleNode = mSPO_Parser.Module.ParseText(aCode, aId, aDebugStream);
		
		var TypeArg = mVM_Type.Free();
		var InitScope = mSPO_AST_Types.UpdateMatchTypes(
			ModuleNode.Import.Match,
			mStd.cEmpty,
			mSPO_AST_Types.tTypeRelation.Sub,
			mStream.Stream(
				[
					(
						"_=...",
						mVM_Type.Generic(
							TypeArg,
							mVM_Type.Proc(
								mVM_Type.Var(TypeArg),
								TypeArg,
								mVM_Type.Empty()
							)
						)
					)
				]
			)
		).Then(
			_ => _.Scope
		).ElseThrow(
		);
		
		ModuleNode.Commands.Reduce(
			mResult.OK(InitScope).AsResult<tText>(),
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
			)
		).ElseThrow(
		);
		
		var ModuleConstructor = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, InitScope);
		
		return mVM.Run(
			mIL_AST.Module(
				ModuleConstructor.TypeDef.ToStream(),
				ModuleConstructor.Defs.ToStream(
				).MapWithIndex(
					(aIndex, aDef) => mIL_AST.Def(mSPO2IL.GetDefId(aIndex), aDef.TypeId, aDef.Commands.ToStream())
				)
			),
			aImport,
			_ => $"{_.Start.Id}({_.Start.Row}:{_.Start.Col} .. {_.Start.Row}:{_.Start.Col})",
			aDebugStream
		);
	}
}