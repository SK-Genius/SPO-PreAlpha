using System;
using System.IO;
using System.Linq;

using tBool = System.Boolean;
using tText = System.String;


public static class mCheckDep {
	#region tList
	
	internal delegate tRes tFunc<out tRes, in tArg>(tArg a);
	internal delegate tRes tFunc<out tRes, in tArg1, in tArg2>(tArg1 a1, tArg2 a2);
	
	internal class tList<t> {
		public t Head;
		public tList<t> Tail;
	}
	
	internal static tList<t>
	List<t>(
		t aHead
	) => new tList<t> {
		Head = aHead,
		Tail = null
	};
	
	internal static tList<t>
	List<t>(
		t aHead,
		tList<t> aTail
	) => new tList<t> {
		Head = aHead,
		Tail = aTail
	};
	
	internal static tList<t>
	List<t>(
		params t[] a
	) {
		var Result = default(tList<t>);
		for (var I = a.Length; I --> 0;) {
			Result = List(a[I], Result);
		}
		return Result;
	}
	
	internal static t[]
	ToArray<t>(
		this tList<t> a
	) {
		var List = new System.Collections.Generic.List<t>();
		while (!(a is null)) {
			List.Add(a.Head);
			a = a.Tail;
		}
		return List.ToArray();
	}
	
	internal static bool
	Any<t>(
		this tList<t> aList,
		tFunc<tBool, t> aCond
	) {
		while (!(aList is null)) {
			if (aCond(aList.Head)) {
				return true;
			}
			aList = aList.Tail;
		}
		return false;
	}
	
	internal static t
	First<t>(
		this tList<t> aList,
		tFunc<tBool, t> aCond
	) {
		while (!(aList is null)) {
			if (aCond(aList.Head)) {
				return aList.Head;
			}
			aList = aList.Tail;
		}
		throw new Exception();
	}
	
	internal static tList<t>
	While<t>(
		this tList<t> aList,
		tFunc<tBool, t> aCond
	) => (!(aList is null) && aCond(aList.Head))
	? List(aList.Head, aList.Tail.While(aCond))
	: null;
	
	internal static tList<tOut>
	Map<tOut, tIn>(
		this tList<tIn> aList,
		tFunc<tOut, tIn> aFunc
	) => (aList is null) ? null : List(aFunc(aList.Head), aList.Tail.Map(aFunc));
	
	internal static tOut
	Reduce<tIn, tOut>(
		this tList<tIn> aList,
		tOut aInit,
		tFunc<tOut, tOut, tIn> aFunc
	) => (aList is null) ? aInit : aList.Tail.Reduce(aFunc(aInit, aList.Head), aFunc);
	#endregion
	
	public static void
	Main(
		string[] aFiles
	) {
		var Modules = default(tList<(tText Module, tList<tText> SubModules)>);
		foreach (var File in aFiles) {
			AddTo(ref Modules, new FileInfo(File).FullName, null);
		}
	}
	
	internal static void
	AddTo(
		ref tList<(tText Module, tList<tText> SubModules)> aKnownModules,
		tText aNewModule,
		tList<tText> aModulePath
	) {
		if (aKnownModules.Any(_ => _.Module == aNewModule)) {
			return;
		}
		if(aModulePath.Any(_ => _ == aNewModule)) {
			Console.WriteLine(aModulePath.Reduce(aNewModule + " in path:", (aAll, aNew) => aAll + "\n  " + aNew));
			Environment.Exit(-1);
		}
		var ModuleFile = new FileInfo(aNewModule);
		if (!ModuleFile.Exists) {
			throw new Exception($"File '{ModuleFile}' not found.");
		}
		var ModuleFolder = ModuleFile.Directory.FullName;
		var SubModules = List(
			File.ReadAllLines(aNewModule)
		).While(
			_ => _.StartsWith("//IMPORT ")
		).Map(
			_ => new FileInfo(
				Path.Combine(
					ModuleFolder,
					_.Substring("//IMPORT ".Length)
				)
			).FullName
		);
		
		for (var Iter = SubModules; !(Iter is null); Iter = Iter.Tail) {
			AddTo(ref aKnownModules, Iter.Head, List(aNewModule, aModulePath));
		}
		
		aKnownModules = List((aNewModule, SubModules), aKnownModules);
		
		var HasErrors = false;
		Console.WriteLine($"== {aNewModule} ==");
		foreach (var Error in Compile(aNewModule, aKnownModules)) {
			Console.WriteLine("  " + Error);
			HasErrors = true;
		}
		if (HasErrors) {
			Environment.Exit(-1);
		}
	}
	
	internal static void
	AddSubmodulesFrom(
		this tList<(tText Module, tList<tText> SubModules)> aModules,
		tText aModule,
		ref tList<tText> aList
	) {
		if (aList.Any(_ => _ == aModule)) {
			return;
		}
		aList = List(aModule, aList);
		
		var SubModules = aModules.First(_ => _.Module == aModule).SubModules;
		while (!(SubModules is null)) {
			aModules.AddSubmodulesFrom(SubModules.Head, ref aList);
			SubModules = SubModules.Tail;
		}
	}
	
	internal static tText[]
	Compile(
		tText aModule,
		tList<(tText Module, tList<tText> SubModules)> aModules 
	) {
		var ModulesToCompile = default(tList<tText>);
		aModules.AddSubmodulesFrom(aModule, ref ModulesToCompile);
		var AssemblyFolder = new FileInfo(typeof(mCheckDep).Assembly.Location).Directory.FullName;
		var SrcFolder = Path.Combine(AssemblyFolder , "roslyn");
		if (Directory.Exists(SrcFolder)) {
			var DesFolder = Path.Combine(AssemblyFolder, "bin\\roslyn");
			if (Directory.Exists(DesFolder)) {
				Directory.Delete(SrcFolder, true);
			} else {
				Directory.CreateDirectory(Path.Combine(AssemblyFolder, "bin"));
				Directory.Move(SrcFolder, DesFolder);
			}
		}
		using (
			var Provider = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider()
		) {
			return Provider
				.CompileAssemblyFromFile(
					new System.CodeDom.Compiler.CompilerParameters{
						GenerateInMemory = true,
						GenerateExecutable = false,
						IncludeDebugInformation = false,
						CompilerOptions = "-langversion:7.2",
						ReferencedAssemblies = { "System.dll" },
					},
					ModulesToCompile.ToArray()
				)
				.Errors
				.Cast<System.CodeDom.Compiler.CompilerError>()
				.Where(_ => !_.IsWarning)
				.Select(_ => _.ToString())
				.ToArray();
		}
	}
}
