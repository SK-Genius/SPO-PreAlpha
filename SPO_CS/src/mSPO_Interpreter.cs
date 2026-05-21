#:include Common/mStd.cs
#:include Common/mSpan.cs
#:include Common/mMaybe.cs
#:include Common/mResult.cs
#:include Common/mStream.cs
#:include Common/mTextStream.cs
#:include Common/mArrayList.cs
#:include Common/mParserGen.cs
#:include mTokenizer.cs
#:include mVM_Type.cs
#:include mVM_Data.cs
#:include mVM.cs
#:include mIL_AST.cs
#:include mSPO2IL.cs
#:include mSPO_AST.cs
#:include mSPO_AST_Types.cs
#:include mSPO_Parser.cs
#:include mSPO_Desugar.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

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
		if (!mSPO_Desugar.DesugarModule(ModuleNode).Match(out var DesugaredModule, out var Error)) {
			return mResult.Fail(Error.ToText());
		}
		
		var TypeArg = mVM_Type.Free();
		
		var InitScope = mSPO_AST_Types.UpdatePatternTypes(
			DesugaredModule.Import.Pattern,
			mStd.cEmpty,
			mSPO_AST_Types.tTypeRelation.Sub,
			mStream.Stream(
				mSPO_AST_Types.ScopeItem(
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
			__ => __.Scope
		);
		
		return DesugaredModule.Commands.Reduce(
			InitScope,
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
			)
		).ThenTry(
			aNewScope => mSPO2IL.MapModule(DesugaredModule, mSpan.Merge, aNewScope)
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
	ToILT(
		this mSPO_AST.tModuleNode<tSpan> aModule
	) {
		var Desugared = mSPO_Desugar.DesugarModule(aModule).AssertNotError(__ => __.ToText());
		
		var InitScope = mSPO_AST_Types.UpdatePatternTypes(
			Desugared.Import.Pattern,
			mStd.cEmpty,
			mSPO_AST_Types.tTypeRelation.Sub,
			mStd.cEmpty
		).Then(__ => __.Scope).AssertNotError(__ => __.ToText());
		
		var Scope = Desugared.Commands.Reduce(
			mResult.OK(InitScope).WithErrorType<(tSpan Pos, tText ErrorText)>(),
			(aResScope, aCommand) => aResScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
			)
		).AssertNotError(__ => __.ToText());
		
		var Module = mSPO2IL.MapModule(Desugared, mSpan.Merge, Scope).AssertNotError(__ => __.ToText());
		var SB = new System.Text.StringBuilder();
		var DefIndex = 0u;
		SB.Append("§TYPES").Append('\n');
		
		var Map = mTreeMap.Tree<tText, tNat32>((tText a1, tText a2) => tText.CompareOrdinal(a1, a2).Sign(), []);
		var TypeIndex = 0u;
		foreach (var TypeCommand in Module.TypeDef.ToStream()) {
			mAssert.IsTrue(TypeCommand.NodeType >= mIL_AST.tCommandNodeType._BeginTypes_);
			mAssert.IsTrue(TypeCommand.NodeType < mIL_AST.tCommandNodeType._EndTypes_);
			
			Map = Map.Set(TypeCommand._1, TypeIndex);
			
			var TypeCommand_ = TypeCommand;
			TypeCommand_._1 = mSPO2IL.GetTypeId(TypeIndex);
			TypeCommand_._2 = TypeCommand_._2.Then(
				__ => Map.TryGet(__).Match(
					() => __,
					mSPO2IL.GetTypeId
				)
			);
			TypeCommand_._3 = TypeCommand_._3.Then(
				__ => Map.TryGet(__).Match(
					() => __,
					mSPO2IL.GetTypeId
				)
			);
			
			SB.Append("\t" + TypeCommand_.ToText()).Append('\n');
			TypeIndex += 1;
		}
		
		foreach (var (TypeId, Commands) in Module.Defs.ToStream()) {
			SB.Append('\n');
			SB.Append($"§DEF {mSPO2IL.GetDefId(DefIndex)} € {mSPO2IL.GetTypeId(Map.TryGet(TypeId).AssertNotEmpty(() => "Unknown type " + TypeId))}").Append('\n');
			foreach (var Cmd in Commands.ToStream()) {
				SB.Append("\t" + Cmd.ToText()).Append('\n');
			}
			DefIndex += 1;
		}
		
		return SB.ToString();
	}
}
