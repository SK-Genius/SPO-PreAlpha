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
					mTextParser.ToText,
					aDebugStream
				);
			}
		).ModifyError(
			aError => {
				var Lines = aCode.Split("\n");
				return (
					aError.ToText() +
					"\n" +
					mStream.Nat32StartWith(
						aError.Pos.Start.Row
					).Take(
						aError.Pos.End.Row - aError.Pos.Start.Row + 1
					).Map(
						aRow => $"  {aRow}: {Lines[aRow - 1].Replace('\t', ' ').TrimEnd()}" + (
							aError.Pos.End.Row == aError.Pos.Start.Row
							? $"\n{new tText(' ', ("" + aRow).Length + (tInt32)aError.Pos.Start.Col + 3)}{new tText('~', (tInt32)aError.Pos.End.Col - (tInt32)aError.Pos.Start.Col + 1)}"
							: ""
						)
					).Join((a1, a2) => a1 + "\n" + a2, "")
				);
			}
		);
	}
	
	public static tText
	ToText(
		this (mSpan.tSpan<mTextStream.tPos> Pos, tText ErrorText) a
	) => $"{mTextParser.ToText(a.Pos)}: {a.ErrorText}";
}