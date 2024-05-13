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
			)
		).Then(
			_ => _.Scope
		).ElseThrow();
		
		var Scope = ModuleNode.Commands.Reduce(
			mResult.OK(InitScope).AsResult<tText>(),
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
			)
		).ElseThrow();
		
		var Module = mSPO2IL.MapModule(ModuleNode, mSpan.Merge, Scope);
		
		return mVM.Run(
			mIL_AST.Module(
				Module.TypeDef.ToStream(),
				Module.Defs.ToStream(
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