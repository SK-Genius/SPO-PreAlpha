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
				var SPO_Text = SPO_File.TryReadText().ElseThrow();
				
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
					
					var IL_TextNew = mSPO_Parser.Module.ParseText(
						SPO_Text,
						(Folder._Path / SPO_File.Name).ToText(),
						__ => aLogger(__())
					).ToILT();
					
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
				try {
					var ILT_File = mFS.File(aModuleSetup.ModulePath);
					var Res = mVM.Run(
						mIL_Parser.Module.ParseText(
							ILT_File.TryReadText().ElseThrow(),
							(Folder._Path / ILT_File.Name).ToText(),
							__ => aLogger(__())
						),
						(Data, Type),
						mTextParser.ToText,
						__ => aLogger(__())
					);
					
					aModuleSetup.Module = mResult.OK(Res).WithErrorType<tText>();
					
					return (Res.Data, Res.Type);
				} catch	(System.Exception e) {
					var Error = e.ToString();
					aModuleSetup.Module = mResult.Fail(Error).AsResult<(mVM_Data.tData, mVM_Type.tType)>();
					return mResult.Fail(Error);
				}
			}
			default: {
				return mResult.Fail(
					$"file '{aModuleSetup.ModulePath.ToText()}' wrong extension. (expect .SPO or .ILT)"
				);
			}
		}
	}
	
	private static readonly mFS.tPath
	ModuleFolder = mFS.Path(mStd.File()) / ".." / ".." / "Modules";
	
	public static readonly tModuleSetup
	Module_Std = ModuleSetup(
		ModuleFolder / "Std.ILT",
		mStd.cEmpty
	);
	
	public static readonly tModuleSetup
	Module_Char = ModuleSetup(
		ModuleFolder / "Char.SPO",
		mStream.Stream(
			("Std", Module_Std)
		)
	);
	
	public static readonly tModuleSetup
	Module_Text = ModuleSetup(
		ModuleFolder / "Text.SPO",
		mStream.Stream(
			("Std", Module_Std),
			("Char", Module_Char)
		)
	);
	
	public static readonly tModuleSetup
	Module_Maybe = ModuleSetup(
		ModuleFolder / "Maybe.SPO",
		mStd.cEmpty
	);
	
	public static readonly tModuleSetup
	Module_mMaybe = ModuleSetup(
		ModuleFolder / "mMaybe.SPO",
		mStd.cEmpty
	);
	
	public static readonly tModuleSetup
	Module_Result = ModuleSetup(
		ModuleFolder / "Result.SPO",
		mStd.cEmpty
	);
	
	public static readonly tModuleSetup
	Module_mResult = ModuleSetup(
		ModuleFolder / "mResult.SPO",
		mStream.Stream(
			("mMaybe", Module_mMaybe)
		)
	);
	
	public static readonly tModuleSetup
	Module_mMath = ModuleSetup(
		ModuleFolder / "mMath.SPO",
		mStream.Stream(
			("Std", Module_Std)
		)
	);
	
	public static readonly tModuleSetup
	Module_mSpan = ModuleSetup(
		ModuleFolder / "mSpan.SPO",
		mStream.Stream(
			("Std", Module_Std)
		)
	);
	
	public static readonly tModuleSetup
	Module_mList = ModuleSetup(
		ModuleFolder / "mList.SPO",
		mStream.Stream(
			("Std", Module_Std)
		)
	);
	
	public static readonly mStream.tStream<(tText Key, tModuleSetup Module)>
	Modules = mStream.Stream(
		("Std", Module_Std),
		("Char", Module_Char),
		("Text", Module_Text),
		("Maybe", Module_Maybe),
		("Result", Module_Result),
		("mMaybe", Module_mMaybe),
		("mResult", Module_mResult),
		("mMath", Module_mMath),
		("mSpan", Module_mSpan),
		("mList", Module_mList)
	);
}
