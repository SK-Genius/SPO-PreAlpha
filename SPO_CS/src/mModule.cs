#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mStream.cs
#:ref Common/mResult.cs
#:ref Common/mAssert.cs
#:ref Common/mLazy.cs
#:ref Common/mFS.cs
#:ref mVM_Data.cs
#:ref mVM_Type.cs
#:ref mIL_Parser.cs
#:ref mSPO_Interpreter.cs
#:ref mSPO_Parser.cs

public static class
mModule {
	public class tModuleSetup {
		public mFS.tPath ModulePath;
		public mStream.tStream<(tText Key, tModuleSetup Module)> Dependencies;
		public mMaybe.tMaybe<
			mResult.tResult<
				(mVM_Data.tData Data, mVM_Type.tType Type),
				tText
			>
		> Module;
	}
	
	public static tModuleSetup
	ModuleSetup(
		mFS.tPath aModulePath,
		mStream.tStream<(tText Key, tModuleSetup Module)> aDependencies
	) => new () {
		ModulePath = aModulePath,
		Dependencies = aDependencies,
	};
	
	public static mResult.tResult<
		(mVM_Data.tData Data, mVM_Type.tType Type),
		tText
	>
	Init(
		this mStream.tStream<(tText Key, tModuleSetup Module)> aModules,
		mStd.tAction<tText> aLogger
	) {
		var Data = mVM_Data.Empty();
		var Type = mVM_Type.Empty();
		foreach (var (Key, ModuleSetup) in aModules) {
			if (!ModuleSetup.Init(aLogger).Match(out var Module, out var Error)) {
				return mResult.Fail(Error);
			}
			Data = mVM_Data.Record(Data, mVM_Data.Prefix("_" + Key, Module.Data));
			Type = mVM_Type.Record(Type, mVM_Type.Prefix("_" + Key, Module.Type));
		}
		return (Data, Type);
	}
	
	public static mResult.tResult<
		(mVM_Data.tData Data, mVM_Type.tType Type),
		tText
	>
	Init(
		this tModuleSetup aModuleSetup,
		mStd.tAction<tText> aLogger
	) {
		if (aModuleSetup.Module.IsSome(out var X)) {
			return X;
		}
		
		if (!aModuleSetup.Dependencies.Init(aLogger).Match(out var Dependencies, out var DependencyError)) {
			return mResult.Fail(DependencyError);
		}
		
		var (Data, Type) = Dependencies;
		
		var Folder = mFS.Folder(aModuleSetup.ModulePath.Parent.Deref.AssertNotEmpty());
		var FileExtension = mStream.Stream(aModuleSetup.ModulePath.ToText().Split('.')).TryLast().AssertNotEmpty();
		switch (FileExtension) {
			case "SPO": {
				var SPO_File = mFS.File(aModuleSetup.ModulePath);
				
				if (SPO_File.TryReadText().IsFail(out var ReadError, out var SPO_Text)) {
					return ReadError;
				}
				
				#if true
				{
					var ILT_File = Folder.GetFile(
						SPO_File.Name.Replace(".SPO", ".ILT")
					);
					
					var IL_Text = (
						ILT_File.Exists()
						? mLazy.Lazy(() => ILT_File.TryReadText().ElseThrow())
						: ""
					);
					
					var IL_Result = mSPO_Parser.Module.TryParseText(
						SPO_Text,
						(Folder._Path / SPO_File.Name).ToText(),
						__ => aLogger(__())
					).ThenTry(
						__ => __.TryToILT()
					);
					
					if (IL_Result.IsFail(out var Error, out var IL_TextNew)) {
						return Error;
					}
					
					if (IL_TextNew.Replace("\r", "") != IL_Text.Value.Replace("\r", "")) {
						ILT_File.TryCreate(IL_TextNew);
					}
				}
				#endif
				
				var Y = mSPO_Interpreter.Run(
					SPO_Text,
					(Folder._Path / SPO_File.Name).ToText(),
					(Data, Type),
					__ => aLogger(__())
				).Then(
					__ => (__.Data, __.Type)
				).ModifyError(
					__ => "" + __
				);
				
				aModuleSetup.Module = Y;
				
				return Y;
			}
			case "ILT": {
				var ILT_File = mFS.File(aModuleSetup.ModulePath);
				
				var Result = ILT_File.TryReadText(
				).ThenTry(
					aSource => mIL_Parser.Module.TryParseText(
						aSource,
						ILT_File.Path.ToText(),
						__ => aLogger(__())
					)
				).ThenTry(
					aModule => mVM.TryRun(
						aModule,
						(Data, Type),
						mTextParser.ToText,
						__ => aLogger(__())
					)
				);
				
				aModuleSetup.Module = Result;
				
				return Result;
			}
			default: {
				return mResult.Fail(
					$"file '{aModuleSetup.ModulePath.ToText()}' wrong extension. (expect .SPO or .ILT)"
				);
			}
		}
	}
	
}
