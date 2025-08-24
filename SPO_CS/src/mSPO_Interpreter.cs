// IMPORT Common/mStd
// IMPORT Common/mSpan
// IMPORT Common/mMaybe
// IMPORT Common/mResult
// IMPORT Common/mStream
// IMPORT Common/mTextStream
// IMPORT Common/mArrayList
// IMPORT Common/mParserGen
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
	public static mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
	Run(
		tText aCode,
		tText aId,
		(mVM_Data.tData Data, mVM_Type.tType Type) aImport,
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
		);
		
		return ModuleNode.Commands.Reduce(
			InitScope,
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
			)
		).ThenTry(
			aNewScope => mSPO2IL.MapModule(ModuleNode, mSpan.Merge, aNewScope)
		).Then(
			aModule => {
				return mVM.Run(
					mIL_AST.Module(
						aModule.TypeDef.ToStream(),
						aModule.Defs.ToStream(
						).MapWithIndex(
							(aIndex, aDef) => mIL_AST.Def(
								mSPO2IL.GetDefId(aIndex),
								aDef.TypeId,
								aDef.Commands.ToStream()
							)
						)
					),
					aImport,
					_ => $"{_.Start.Id}:{_.Start.Row}|{_.Start.Col}..{_.Start.Row}|{_.Start.Col}",
					aDebugStream
				);
			}
		).ModifyError(
			_ => {
				var Lines = aCode.Split("\n");
				return (
					_.ToText() +
					"\n" +
					mStream.Nat32StartWith(
						_.Pos.Start.Row
					).Take(
						_.Pos.End.Row - _.Pos.Start.Row + 1
					).Map(
						_ => $"  {_:2}: {Lines[_].TrimEnd()}"
					).Join((a1, a2) => a1 + "\n" + a2, "")
				);
			}
		);
	}
	
	public static tText
	ToText(
		this (mSpan.tSpan<mTextStream.tPos> Pos, tText ErrorText) a
	) => $"{a.Pos.Start.Id}:{a.Pos.Start.Row},{a.Pos.Start.Col}..{a.Pos.End.Row},{a.Pos.End.Col}: {a.ErrorText}";
}