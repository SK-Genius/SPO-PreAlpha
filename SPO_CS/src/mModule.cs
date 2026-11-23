#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mStream.cs
#:ref Common/mResult.cs
#:ref Common/mFS.cs
#:ref mIL_Parser.cs
#:ref mSPO_Interpreter.cs
#:ref mIL_GenerateOpcodes.cs
#:ref mIL_TypeChecker.cs
#:ref mVM_Data.cs
#:ref mVM_Type.cs
#:ref mVM.cs
#:ref mSPO_Parser.cs

using tSpan = mSpan.tSpan<mTextStream.tPos>;

public static class
mModule {
	public class tModuleSetup {
		public mFS.tPath ModulePath;
		public mStream.tStream<(tText Key, tModuleSetup Module)> Dependencies;
		public mMaybe.tMaybe<mResult.tResult<mStd.tEmpty, tText>> Checked;
		public mIL_AST.tModule<tSpan>? IL;
		public mMaybe.tMaybe<mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>> Module;
	}
	
	public static tModuleSetup
	ModuleSetup(
		mFS.tPath aModulePath,
		mStream.tStream<(tText Key, tModuleSetup Module)> aDependencies
	) => new () {
		ModulePath = aModulePath,
		Dependencies = aDependencies,
	};
	
	public static mResult.tResult<mStd.tEmpty, tText>
	Check(
		this tModuleSetup aModule,
		mStd.tAction<tText> aLogger
	) {
		if (aModule.Checked.IsSome(out var Cached)) {
			return Cached;
		}
		
		foreach (var (_, Dependency) in aModule.Dependencies) {
			if (Dependency.Check(aLogger).IsFail(out var Error, out _)) {
				return Error;
			}
		}
		
		var File = mFS.File(aModule.ModulePath);
		
		var Result = File.TryReadText().ThenTry(
			aSource => {
				if (File.Extension.Equals("SPO", System.StringComparison.OrdinalIgnoreCase)) {
					return mSPO_Parser.Module.TryParseText(aSource, File.Path.ToText(), __ => aLogger(__())).ThenTry(
						__ => __.Check()
					).Then(
						__ => {
							var ILT = File.Folder.GetFile(File.NameWithoutExtension + ".ILT");
							var Text = __.ToILT();
							
							if (!ILT.Exists() || ILT.TryReadText().ElseThrow().Replace("\r", "") != Text.Replace("\r", "")) {
								ILT.TryCreate(Text);
							}
							
							aModule.IL = __;
							return mStd.cEmpty;
						}
					);
				}
				
				if (File.Extension.Equals("ILT", System.StringComparison.OrdinalIgnoreCase)) {
					return mIL_Parser.Module.TryParseText(aSource, File.Path.ToText(), __ => aLogger(__())).ThenTry(
						__ => {
							if (!mIL_TypeChecker.AreModuleTypesValid(__, out var Error)) {
								return mResult.Fail(mIL_TypeChecker.ErrorText(Error)).AsResult<mStd.tEmpty>();
							}
							
							aModule.IL = __;
							return mResult.OK(mStd.cEmpty).WithErrorType<tText>();
						}
					);
				}
				
				return mResult.Fail($"file '{File.Path.ToText()}' wrong extension. (expect .SPO or .ILT)");
			}
		);
		
		aModule.Checked = Result;
		return Result;
	}
	
	public static mResult.tResult<mStd.tEmpty, tText>
	Link(
		this tModuleSetup aModule,
		mStd.tAction<tText> aLogger
	) {
		if (aModule.Check(aLogger).IsFail(out var Error, out _)) {
			return Error;
		}
		
		static mResult.tResult<mStd.tEmpty, tText>
		CheckConnections(
			tModuleSetup aModule
		) {
			var Imports = mVM_Type.Empty();
			
			foreach (var (Key, Dependency) in aModule.Dependencies) {
				if (CheckConnections(Dependency).IsFail(out var Error, out _)) {
					return Error;
				}
				
				var Export = mIL_TypeChecker.ModuleSignature(Dependency.IL!).ModifyError(mIL_TypeChecker.ErrorText).ElseThrow().Export;
				Imports = mVM_Type.Record(Imports, mVM_Type.Prefix("_" + Key, Export));
			}
			
			return (
				mIL_TypeChecker.AreImportTypesValid(aModule.IL!, Imports, out var ImportError)
				? mResult.OK(mStd.cEmpty).WithErrorType<tText>()
				: mResult.Fail(mIL_TypeChecker.ErrorText(ImportError))
			);
		}
		
		return CheckConnections(aModule);
	}
	
	public static mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
	Init(
		this tModuleSetup aModule,
		mStd.tAction<tText> aLogger,
		mStream.tStream<(mVM_Type.tType Function, mVM_Type.tType Object, mVM_Type.tType Argument)> aNativeCalls = default
	) {
		if (aModule.Module.IsSome(out var Cached)) {
			return Cached;
		}
		
		if (aModule.Link(aLogger).IsFail(out var Error, out _)) {
			return Error;
		}
		
		var Compiler = new mIL_Specialize.tContext<tSpan>();
		var Prepared = new System.Collections.Generic.Dictionary<tModuleSetup, (mStream.tStream<mVM_Data.tProcDef<tSpan>> Definitions, mIL_Specialize.tFrame<tSpan> Frame)>();
		
		mResult.tResult<(mVM_Type.tType Type, mIL_Specialize.tFlow<tSpan> Value), tText>
		Prepare(
			tModuleSetup aCurrent
		) {
			if (aCurrent.Module.IsSome(out var CachedModule)) {
				return CachedModule.Then(
					aValue => {
						var Value = (Data: aValue.Data.Normalize(aValue.Type, "Import"), aValue.Type);
						aCurrent.Module = mResult.OK(Value).WithErrorType<tText>();
						return (Value.Type, Compiler.FromData(Value));
					}
				);
			}
			
			if (Prepared.TryGetValue(aCurrent, out var Existing)) {
				return (Existing.Frame.Code.DefType.Refs[2].Refs[2], Existing.Frame.Result);
			}
			
			var ImportType = mVM_Type.Empty();
			var Import = new mIL_Specialize.tFlow<tSpan>();
			
			foreach (var (Key, Dependency) in aCurrent.Dependencies) {
				if (!Prepare(Dependency).Match(out var Export, out var Error)) {
					return mResult.Fail(Error);
				}
				
				ImportType = mVM_Type.Record(ImportType, mVM_Type.Prefix("_" + Key, Export.Type));
				Import.Fields = Import.Fields.Set(("_" + Key).PrefixHash(), Export.Value);
			}
			
			var Definitions = mIL_GenerateOpcodes.GenerateTemplates(aCurrent.IL!).Defs.ToArrayList();
			var Frame = Compiler.PrepareModule(Definitions.ToStream(), ImportType, Import);
			Definitions.Set(Definitions.Size - 1, Frame.Code);
			Prepared.Add(aCurrent, (Definitions.ToStream(), Frame));
			return (Frame.Code.DefType.Refs[2].Refs[2], Frame.Result);
		}
		
		if (!Prepare(aModule).Match(out _, out var PrepareError)) { return mResult.Fail(PrepareError); }
		Compiler.PrepareExports(mStream.Stream(Prepared.Values.Select(__ => (__.Frame.Code.DefType.Refs[2].Refs[2], __.Frame.Result)).ToArray()), aNativeCalls);
		
		mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
		Initialize(
			tModuleSetup aModule,
			mStd.tAction<tText> aLogger
		) {
			if (aModule.Module.IsSome(out var Cached)) {
				return Cached;
			}
			
			var Imports = (Data: mVM_Data.Empty(), Type: mVM_Type.Empty());
			
			foreach (var (Key, Dependency) in aModule.Dependencies) {
				if (Initialize(Dependency, aLogger).IsFail(out var Error, out var Value)) {
					return Error;
				}
				
				Imports.Data = mVM_Data.Record(Imports.Data, mVM_Data.Prefix("_" + Key, Value.Data));
				Imports.Type = mVM_Type.Record(Imports.Type, mVM_Type.Prefix("_" + Key, Value.Type));
			}
			
			var Result = mVM.RunModule(Prepared[aModule].Definitions, Imports.Data, mTextParser.ToText, __ => aLogger(__()));
			aModule.Module = Result;
			return Result;
		}
		
		return Initialize(aModule, aLogger);
	}
}