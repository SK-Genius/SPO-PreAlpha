#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mFS.cs
#:ref Common/mSpan.cs
#:ref Common/mMaybe.cs
#:ref Common/mResult.cs
#:ref Common/mStream.cs
#:ref Common/mTextStream.cs
#:ref Common/mArrayList.cs
#:ref Common/mParserGen.cs
#:ref mTokenizer.cs
#:ref mVM_Type.cs
#:ref mVM_Data.cs
#:ref mVM.cs
#:ref mIL_AST.cs
#:ref mSPO2IL.cs
#:ref mSPO_AST.cs
#:ref mSPO_AST_Types.cs
#:ref mSPO_Parser.cs
#:ref mSPO_Desugar.cs

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
		if (mSPO_Parser.Module.TryParseText(aCode, aId, aDebugStream).IsFail(out var ParseError, out var ModuleNode)) {
			return ParseError;
		}
		
		if (!mSPO_Desugar.TryDesugarModule(ModuleNode).Match(out var DesugaredModule, out var Error)) {
			return mResult.Fail(Error.ToText());
		}
		
		var TypeArg = mVM_Type.Free(mVM_Type.Type());
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
			),
			new()
		).Then(
			__ => (__.Scope, __.State)
		);
		
		return DesugaredModule.Commands.Reduce(
			InitScope,
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope.Scope, aScope.State)
			)
		).ThenTry(
			aNewScope => mSPO2IL.MapModule(DesugaredModule, mSpan.Merge, aNewScope.Scope, aNewScope.State)
		).ModifyError(
			aError => {
				var ErrorSource = aError.Pos.Start.Id;
				var Lines = (ErrorSource != aId
					? mFS.File(ErrorSource).TryReadText().ElseThrow()
					: aCode).Split("\n");
				return (aError.Pos.Start, aError.ErrorText).ToText(Lines);
			}
		).ThenTry(
			aModule => {
				return mVM.TryRun(
					mIL_AST.Module(
						aModule.TypeDeclarations.TypeDef.ToStream(),
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
		);
	}
	
	public static mResult.tResult<tText, tText>
	TryToILT(
		this mSPO_AST.tModuleNode<tSpan> aModule
	) {
		static tText
		FormatError(
			(tSpan Pos, tText ErrorText) aError
		) {
			var File = mFS.File(aError.Pos.Start.Id);
			if (!File.Exists()) {
				return aError.ToText();
			}
			
			return (
				aError.Pos.Start,
				aError.ErrorText
			).ToText(
				File.TryReadText().ElseThrow().Split('\n')
			);
		}
		
		if (
			mSPO_Desugar.TryDesugarModule(
				aModule
			).ModifyError(
				FormatError
			).IsFail(
				out var DesugarError,
				out var Desugared
			)
		) {
			return DesugarError;
		}
		
		var InitScope = mSPO_AST_Types.UpdatePatternTypes(
			Desugared.Import.Pattern,
			mStd.cEmpty,
			mSPO_AST_Types.tTypeRelation.Sub,
			mStd.cEmpty,
			new()
		).Then(
			__ => (__.Scope, __.State)
		);
		
		var Scope = Desugared.Commands.Reduce(
			InitScope,
			(
				aResScope,
				aCommand
			) => aResScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(
					aCommand,
					aScope.Scope,
					aScope.State
				)
			)
		).ModifyError(
			FormatError
		);
		
		if (Scope.IsFail(out var TypeError, out var TypedScope)) {
			return TypeError;
		}
		
		if (
			mSPO2IL.MapModule(
				Desugared,
				mSpan.Merge,
				TypedScope.Scope,
				TypedScope.State
			).ModifyError(
				FormatError
			).IsFail(
				out var MapError,
				out var Module
			)
		) {
			return MapError;
		}
		
		var SB = new System.Text.StringBuilder();
		var DefIndex = 0u;
		SB.Append("§TYPES").Append('\n');
		
		var Map = mTreeMap.Tree<tText, tNat32>((tText a1, tText a2) => tText.CompareOrdinal(a1, a2).Sign(), []);
		var TypeIndex = 0u;
		
		foreach (var TypeCommand in Module.TypeDeclarations.TypeDef.ToStream()) {
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
			SB.Append(
				'\n'
			).Append(
				$"§DEF {mSPO2IL.GetDefId(DefIndex)} € {mSPO2IL.GetTypeId(Map.TryGet(TypeId).AssertNotEmpty(() => "Unknown type " + TypeId))}"
			).Append(
				'\n'
			);
			
			foreach (var Cmd in Commands.ToStream()) {
				var Command = Cmd;
				
				if (Command.NodeType is not (mIL_AST.tCommandNodeType.TypePrefix or mIL_AST.tCommandNodeType.PrefixApply or mIL_AST.tCommandNodeType.PrefixRemove)) {
					Command._2 = Command._2.Then(__ => Map.TryGet(__).Match(() => __, mSPO2IL.GetTypeId));
				}
				
				if (Command.NodeType is not (mIL_AST.tCommandNodeType.GetField or mIL_AST.tCommandNodeType.TryRemovePrefixFrom)) {
					Command._3 = Command._3.Then(__ => Map.TryGet(__).Match(() => __, mSPO2IL.GetTypeId));
				}
				
				SB.Append('\t').Append(Command.ToText()).Append('\n');
			}
			DefIndex += 1;
		}
		
		return SB.ToString();
	}
}
