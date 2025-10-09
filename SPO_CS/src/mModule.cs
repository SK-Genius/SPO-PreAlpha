// IMPORT Common/mStd
// IMPORT Common/mTest
// IMPORT Common/mAssert
// IMPORT mVM_Data
// IMPORT mStdLib
// IMPORT mSPO_Interpreter

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
		
		var FileExtension = mStream.Stream(aModuleSetup.ModulePath.ToText().Split('.')).TryLast().AssertNotEmpty();
		switch (FileExtension) {
			case "SPO": {
				var SPO_Path = aModuleSetup.ModulePath.ToText();
				var SPO_Text = System.IO.File.ReadAllText(SPO_Path);
				
				#if true
				{
					var IL_Path = SPO_Path.Replace(".SPO", ".ILT");
					var IL_Text = System.IO.File.Exists(IL_Path)
					? mLazy.Lazy(() => System.IO.File.ReadAllText(IL_Path))
					: "";
					
					var IL_TextNew = mSPO_Parser.Module.ParseText(
						SPO_Text,
						SPO_Path,
						_ => aLogger(_())
					).ToILT();
					
					if (IL_TextNew != IL_Text.Value) {
						System.IO.File.WriteAllText(
							IL_Path + ".new",
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
					SPO_Path,
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
					var ILT_Path = aModuleSetup.ModulePath.ToText();
					var Res = mVM.Run(
						mIL_Parser.Module.ParseText(
							System.IO.File.ReadAllText(ILT_Path),
							ILT_Path,
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
	
	private static mFS.tPath
	ModuleFolder = mFS.Path(
		System.IO.Directory.GetParent(
			System.IO.Path.GetDirectoryName(
				mStd.File()
			)
		).FullName
	) / "Modules";
	
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