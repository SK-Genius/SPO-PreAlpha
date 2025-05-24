var FS = mFS.FSFromCWD() / mFS.Path(args.Length > 0 ? args[0] : ".");
var SrcBaseFolder = mFS.Path("src");
var ProjBaseFolder = mFS.Path(".proj");
var BinBaseFolder = mFS.Path("bin");

var ProjectDependenciesMap = mTreeMap.Tree<tText, mStream.tStream<tText>>(
	(a1, a2) => mMath.Sign(tText.CompareOrdinal(a1, a2)),
	[]
);

if (FS.FolderExists(ProjBaseFolder)) {
	System.IO.Directory.Delete(ProjBaseFolder.ToText(), recursive: true);
	FS.NewFolder(ProjBaseFolder);
}

foreach (
	var CS_FilePath in FS.GetFiles(
		SrcBaseFolder
	).Where(
		_ => _.Name.EndsWith(".cs")
	)
) {
	var ProjName = System.IO.Path.GetFileNameWithoutExtension(CS_FilePath.Name);
	var ProjFolder = ProjBaseFolder / (SrcBaseFolder >> CS_FilePath).AssertNotEmpty() / new mFS.tPath("..") / ProjName;
	
	var ProjDep = FS.ReadAllLines(
		CS_FilePath
	).TakeWhile(
		_ => _.StartsWith("// IMPORT ")
	).Map(
		_ => _["// IMPORT ".Length..].Trim()
	);
	
	var ProjectReferences = ProjDep.Map(
		_ => $"""
					<ProjectReference Include="{"../" + _ + '/' + _.Split('/')[^1] + ".csproj"}" />
			"""
	);
	
	var CSProjPath = ProjFolder / (ProjName + ".csproj");
	var FullProjName = (ProjBaseFolder >> ProjFolder).AssertNotEmpty().ToText();
	
	// build csproj content
	var CSProjContent = $"""
		<Project Sdk="Microsoft.NET.Sdk">
			
			<PropertyGroup>
				{(ProjName == "mRunTests" ? "<OutputType>Exe</OutputType>" : "")}
				<OutputPath>{BinBaseFolder}</OutputPath>
				<Deterministic>true</Deterministic>
			</PropertyGroup>
			
			<ItemGroup>
				<Compile Include="{(ProjFolder >> CS_FilePath).AssertNotEmpty($"{ProjFolder.ToText()} >> {CS_FilePath.ToText()}").ToText()}" />
				<Compile Include="{(ProjFolder >> SrcBaseFolder / "_GlobalUsings.cs").AssertNotEmpty($"{ProjFolder.ToText()} >> {SrcBaseFolder.ToText()} / _GlobalUsings.cs").ToText()}" />
			</ItemGroup>
			
			<ItemGroup>
		{tText.Join("\n", ProjectReferences.ToArrayList().ToArray())}
			</ItemGroup>
			
		</Project>
		""";
	
	var LastSlashIndex = FullProjName.LastIndexOf('/');
	var DepPrefix = LastSlashIndex > 0 ? FullProjName[..(LastSlashIndex + 1)] : "";
	
	ProjectDependenciesMap = ProjectDependenciesMap.Set (
		FullProjName,
		ProjDep.Map(_ => DepPrefix + _)
	);
	
	if (!ProjFolder.IsIdent()) {
		FS.NewFolder(ProjFolder);
	}
	
	System.Console.WriteLine($"  Project: {CSProjPath.ToText()}");
	System.IO.File.WriteAllText(CSProjPath.ToText(), CSProjContent);
}

foreach (var Proj in ProjectDependenciesMap.ToStream()) {
	UpdateDeps(ref ProjectDependenciesMap, Proj.Key, mStd.cEmpty);
}

System.Console.WriteLine("END");

static mStream.tStream<tText> UpdateDeps(
	ref mTreeMap.tTree<tText, mStream.tStream<tText>> aTree,
	string aProj,
	mStream.tStream<tText> aDepPath
) {
	var Deps = aTree.TryGet(aProj).AssertNotEmpty();
	var NewDepPath = mStream.Stream(aProj, aDepPath);
	
	foreach (var Dep in Deps) {
		if (NewDepPath.Any(_ => _ == Dep)) {
			throw mError.Error(
				tText.Join(" -> ", mStream.Stream(Dep, NewDepPath).Reverse().ToArrayList().ToArray())
			);
		}
		
		var NewDeps = UpdateDeps(ref aTree, Dep, NewDepPath);
		Deps = Merge(Deps, NewDeps);
	}
	
	aTree = aTree.Set(aProj, Deps);
	
	return Deps;
}

static mStream.tStream<tText> Merge(
	mStream.tStream<tText> a1,
	mStream.tStream<tText> a2
) {
	if (a2.Is(out var Head, out var Tail)) {
		return Merge(
			a1.Any(_ => _ == Head) ? a1 : mStream.Stream(Head, a1),
			Tail
		);
	} else {
		return a1;
	}
}