// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mStream
// IMPORT Common/mResult
// IMPORT Common/mAssert
// IMPORT Common/mLazy
// IMPORT Common/mFS
// IMPORT mVM_Data
// IMPORT mVM_Type
// IMPORT mIL_Parser
// IMPORT mSPO_Interpreter
// IMPORT mSPO_Parser

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
	> Init(
		this tModuleSetup aModuleSetup,
		mStd.tAction<tText> aLogger
	) {
		if (aModuleSetup.Module.IsSome(out var X)) {
			return X;
		}
		
		var Data = mVM_Data.Empty();
		var Type = mVM_Type.Empty();
		foreach (var Dependency in aModuleSetup.Dependencies) {
			if (!Dependency.Module.Module.IsSome(out var Dep)) {
				Dep = Dependency.Module.Init(aLogger);
				Dependency.Module.Module = Dep;
			}
			
			if (!Dep.Match(out var Dep_, out var Error)) {
				return mResult.Fail(Error);
			}
			
			var Module = Dep_;
			Data = mVM_Data.Record(
				Data,
				mVM_Data.Prefix(
					"_" + Dependency.Key,
					Module.Data
				)
			);
			Type = mVM_Type.Record(
				Type,
				mVM_Type.Prefix(
					"_" + Dependency.Key,
					Module.Type
				)
			);
		}
		
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
					
					var IL_Text = ILT_File.Exists()
					? mLazy.Lazy(() => ILT_File.TryReadText().ElseThrow())
					: "";
					
					var IL_TextNew = mSPO_Parser.Module.ParseText(
						SPO_Text,
						(Folder._Path / SPO_File.Name).ToText(),
						_ => aLogger(_())
					).ToILT();
					
					if (IL_TextNew.Replace("\r", "") != IL_Text.Value.Replace("\r", "")) {
						Folder.GetFile(
							ILT_File.Name + ".new"
						).TryCreate(
							IL_TextNew
						);
						mAssert.Fail(
							mAssert.DiffText(
								IL_TextNew,
								IL_Text.Value
							)
						);
					}
				}
				#endif
				
				var Y = mSPO_Interpreter.Run(
					SPO_Text,
					(Folder._Path / SPO_File.Name).ToText(),
					(Data, Type),
					_ => aLogger(_())
				).Then(
					_ => (_.Data, _.Type)
				).ModifyError(
					_ => "" + _
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
							_ => aLogger(_())
						),
						(Data, Type),
						mTextParser.ToText,
						_ => aLogger(_())
					);
					
					aModuleSetup.Module = mResult.OK(Res).WithErrorType<tText>();
					
					return (Res.Data, Res.Type);
				} catch	(System.Exception e) {
					var Error = e.ToString();
					aModuleSetup.Module = (mResult.tResult<(mVM_Data.tData, mVM_Type.tType), tText>)mResult.Fail(Error);
					return mResult.Fail(Error);
				}
			}
			default: {
				return mResult.Fail($"file '{aModuleSetup.ModulePath.ToText()}' wrong extension. (expect .SPO or .ILT)");
			}
		}
	}
	
	private static readonly mFS.tPath
	ModuleFolder = mFS.Path(
		mStd.File()
	).Parent.Deref.AssertNotEmpty() / "../Modules";
	
	public static tModuleSetup
	Module_Std = ModuleSetup(
		ModuleFolder / "Std.ILT",
		mStd.cEmpty
	);
	
	public static tModuleSetup
	Module_Char = ModuleSetup(
		ModuleFolder / "Char.SPO",
		mStream.Stream(
			[
				("Std", Module_Std),
			]
		)
	);
	
	public static tModuleSetup
	Module_Text = ModuleSetup(
		ModuleFolder / "Text.SPO",
		mStream.Stream(
			[
				("Std", Module_Std),
				("Char", Module_Char),
			]
		)
	);
	
	public static tModuleSetup
	Module_Maybe = ModuleSetup(
		ModuleFolder / "Maybe.SPO",
		mStd.cEmpty
	);
}