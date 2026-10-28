#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../SPO_CS/src/mModule.cs
#:ref ../../SPO_CS/src/Common/mTreeMap.cs
#:ref ../../SPO_CS/src/Common/mJNI.cs

public static class
mStartup {
	public static (mModule.tModuleSetup Entry, mStream.tStream<(tText Key, mModule.tModuleSetup Module)> Modules)
	Load(
		tText aPath
	) {
		var Path = System.IO.Path.GetFullPath(aPath);
		var Folder = System.IO.Path.GetDirectoryName(Path)!;
		
		tText
		Text(
			mStream.tStream<(tInt32 LineNr, tText Line)> aLines,
			tText aLocation
		) {
			var Value = mJNI.ToText(aLines);
			return (
				!tText.IsNullOrWhiteSpace(Value)
				? Value.Trim()
				: throw new System.FormatException($"{aLocation}: expected a nonempty string.")
			);
		}
		
		var Root = mJNI.TryParseTree(mJNI.ToLines(System.IO.File.ReadAllText(Path), 0), Path).ElseThrow();
		if (!Root.TryGet("entry").IsSome(out var EntryName) || !Root.TryGet("modules").IsSome(out var Definitions)) {
			throw new System.FormatException("Startup requires 'entry' and 'modules'.");
		}
		
		foreach (var (Key, _) in Root.ToStream()) {
			if (Key is not ("runtime" or "entry" or "modules")) {
				throw new System.FormatException($"Startup: unexpected property '{Key}'.");
			}
		}
		
		var Descriptions = mJNI.TryParseTree(Definitions, Path).ElseThrow();
		var Modules = mTreeMap.Tree<tText, mModule.tModuleSetup>(
			(aLeft, aRight) => System.Math.Sign(
				tText.CompareOrdinal(
					aLeft,
					aRight
				)
			),
			[]
		);
		
		mModule.tModuleSetup
		Resolve(
			tText aName,
			mStream.tStream<tText> aPath
		) {
			if (aPath.Any(aParent => aParent == aName)) {
				throw new System.FormatException(
					"Circular module imports: " +
					mStream.Stream(aName, aPath).Reverse().Join((aLeft, aRight) => aLeft + " -> " + aRight, "")
				);
			}
			
			if (Modules.TryGet(aName).IsSome(out var Existing)) {
				return Existing;
			}
			
			if (!Descriptions.TryGet(aName).IsSome(out var Definition)) {
				throw new System.FormatException($"Unknown module '{aName}'.");
			}
			
			var Description = mJNI.TryParseTree(Definition, Path).ElseThrow();
			foreach (var (Property, _) in Description.ToStream()) {
				if (Property is not ("path" or "imports")) {
					throw new System.FormatException($"Module '{aName}': unexpected property '{Property}'.");
				}
			}
			
			if (!Description.TryGet("path").IsSome(out var ModulePath)) {
				throw new System.FormatException($"Module '{aName}': expected 'path'.");
			}
			
			var Dependencies = mStream.Stream<(tText Key, mModule.tModuleSetup Module)>();
			if (Description.TryGet("imports").IsSome(out var Imports)) {
				foreach (var (Key, Value) in mJNI.TryParseTree(Imports, Path).ElseThrow().ToStream()) {
					var Dependency = Resolve(
						Text(Value, $"Module '{aName}'.imports.{Key}"),
						mStream.Stream(aName, aPath)
					);
					Dependencies = mStream.Stream((Key, Dependency), Dependencies);
				}
			}
			
			var File = System.IO.Path.GetFullPath(
				System.IO.Path.Combine(
					Folder,
					Text(
						ModulePath,
						$"Module '{aName}'.path"
					)
				)
			);
			if (System.IO.Path.GetExtension(File) == ".*") {
				foreach (var Extension in new[] {
						".spo",
						".ILT"
					}
				
				) {
					var Candidate = System.IO.Path.ChangeExtension(File, Extension);
					if (System.IO.File.Exists(Candidate)) {
						File = Candidate;
						break;
					}
				}
			}
			
			if (!System.IO.File.Exists(File)) {
				throw new System.IO.FileNotFoundException($"Module '{aName}' was not found: {File}", File);
			}
			
			var Setup = mModule.ModuleSetup(mFS.Path(File), Dependencies.Reverse());
			Modules = Modules.Set(aName, Setup);
			return Setup;
		}
		
		var Entry = Resolve(Text(EntryName, "entry"), mStd.cEmpty);
		foreach (var (Name, _) in Descriptions.ToStream()) {
			Resolve(Name, mStd.cEmpty);
		}
		
		return (Entry, Modules.ToStream());
	}
	
	public static mResult.tResult<(mVM_Data.tData Data, mVM_Type.tType Type), tText>
	Init(
		this mStream.tStream<(tText Key, mModule.tModuleSetup Module)> aModules,
		mStd.tAction<tText> aLogger
	) {
		var Data = mVM_Data.Empty();
		var Type = mVM_Type.Empty();
		foreach (var (Key, Module) in aModules) {
			if (Module.Init(aLogger).IsFail(out var Error, out var Value)) {
				return Error;
			}
			
			Data = mVM_Data.Record(Data, mVM_Data.Prefix("_" + Key, Value.Data));
			Type = mVM_Type.Record(Type, mVM_Type.Prefix("_" + Key, Value.Type));
		}
		
		return (Data, Type);
	}
}