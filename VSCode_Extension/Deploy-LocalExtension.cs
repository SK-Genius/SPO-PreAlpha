using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

return Run(args);

static int
Run(
	string[] args
) {
	var stagingDir = "";
	
	try {
		var options = ParseArguments(args);
		if (options.ShowHelp) {
			PrintUsage();
			return 0;
		}
		
		var scriptPath = Path.GetFullPath(GetSourceFilePath());
		var scriptDir = Path.GetDirectoryName(scriptPath)
			?? throw new InvalidOperationException("Could not determine the script directory.");
		var packageJsonPath = Path.Combine(scriptDir, "package.json");
		
		if (options.PublishServerOnly) {
			PublishServer(scriptDir);
			return 0;
		}
		
		using var packageJson = JsonDocument.Parse(File.ReadAllText(packageJsonPath));
		var packageRoot = packageJson.RootElement;
		var extensionName = GetRequiredString(packageRoot, "name");
		var extensionPublisher = GetRequiredString(packageRoot, "publisher");
		var extensionVersion = GetRequiredString(packageRoot, "version");
		var extensionDirectoryName = $"{extensionPublisher}.{extensionName}-{extensionVersion}";
		var extensionsRootDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".vscode",
			"extensions"
		);
		var targetDir = options.TargetDir.Length > 0
			? options.TargetDir
			: Path.Combine(extensionsRootDir, extensionDirectoryName);
		
		var targetParentDir = Path.GetDirectoryName(targetDir)
			?? throw new InvalidOperationException("Could not determine the target directory.");
		
		var shouldCleanupVersions = PathsEqual(targetParentDir, extensionsRootDir) && !options.KeepOldVersions;
		
		if (!options.SkipServerBuild) {
			PublishServer(scriptDir);
		}
		
		Directory.CreateDirectory(targetParentDir);
		
		stagingDir = Path.Combine(
			targetParentDir,
			"." + Path.GetFileName(targetDir) + ".staging." + Guid.NewGuid().ToString("N")
		);
		Directory.CreateDirectory(stagingDir);
		
		InstallFilesFromManifest(
			scriptDir: scriptDir,
			targetDir: stagingDir,
			packageJson: packageRoot
		);
		
		ValidateInstall(stagingDir);
		ReplaceDirectory(stagingDir, targetDir);
		stagingDir = "";
		
		if (shouldCleanupVersions) {
			DeleteOtherInstalledVersions(
				extensionsRootDir: extensionsRootDir,
				extensionPrefix: extensionPublisher + "." + extensionName + "-",
				currentTargetDir: targetDir
			);
		}
		
		Console.WriteLine($"SPO extension installed to \"{targetDir}\"");
		return 0;
	} catch (Exception exception) {
		Console.Error.WriteLine(exception);
		return 1;
	} finally {
		DeleteDirectoryIfExists(stagingDir);
	}
}

static string
GetSourceFilePath(
	[CallerFilePath] string path = ""
) => path;

static tOptions
ParseArguments(
	string[] args
) {
	var showHelp = false;
	var publishServerOnly = false;
	var skipServerBuild = false;
	var keepOldVersions = false;
	(string[] positional, string[] options) = PartitionArguments(args);
	
	foreach (var option in options) {
		switch (option) {
			case "-h":
			case "--help":
				showHelp = true;
				break;
			
			case "--skip-server-build":
				skipServerBuild = true;
				break;
			
			case "--publish-server-only":
				publishServerOnly = true;
				break;
			
			case "--keep-old-versions":
				keepOldVersions = true;
				break;
			
			default:
				throw new InvalidOperationException($"Unknown option \"{option}\".");
		}
	}
	
	if (positional.Length > 1) {
		throw new InvalidOperationException("Expected at most one target directory argument.");
	}
	
	if (publishServerOnly && skipServerBuild) {
		throw new InvalidOperationException("The options --publish-server-only and --skip-server-build cannot be combined.");
	}
	
	if (publishServerOnly && positional.Length > 0) {
		throw new InvalidOperationException("A target directory cannot be used with --publish-server-only.");
	}
	
	return new tOptions(
		ShowHelp: showHelp,
		PublishServerOnly: publishServerOnly,
		SkipServerBuild: skipServerBuild,
		KeepOldVersions: keepOldVersions,
		TargetDir: positional.Length == 0 ? "" : Path.GetFullPath(positional[0])
	);
}

static (string[] positional, string[] options)
PartitionArguments(
	string[] args
) {
	var positional = new List<string>();
	var options = new List<string>();
	
	foreach (var argument in args) {
		if (argument.StartsWith("-", StringComparison.Ordinal)) {
			options.Add(argument);
			continue;
		}
		
		positional.Add(argument);
	}
	
	return (positional.ToArray(), options.ToArray());
}

static void
PrintUsage(
) {
	Console.WriteLine("Usage: dotnet run ./Deploy-LocalExtension.cs [targetDir] [--skip-server-build] [--keep-old-versions] [--publish-server-only]");
	Console.WriteLine("If no target directory is provided, the extension is installed into %USERPROFILE%\\.vscode\\extensions.");
	Console.WriteLine("Use --publish-server-only to only publish VSCode_Extension/server without installing the extension.");
}

static void
PublishServer(
	string scriptDir
) {
	var configuration = "Release";
	var runtimeIdentifier = "win-x64";
	var serverSourceProject = Path.Combine(scriptDir, "server-src", "SPO.LSP.Server.cs");
	var runtimeDir = Path.Combine(scriptDir, "server");
	var publishDir = Path.Combine(scriptDir, ".publish-server");
	var publishedExecutable = Path.Combine(publishDir, "SPO.LSP.Server.exe");
	
	DeleteDirectoryIfExists(publishDir);
	
	var publishExitCode = RunProcess(
		fileName: "dotnet",
		workingDirectory: scriptDir,
		arguments: [
			"publish",
			serverSourceProject,
			"-c", configuration,
			"-r", runtimeIdentifier,
			"-o", publishDir,
			"/p:PublishSingleFile=true",
			"/p:SelfContained=true",
			"/p:DebugType=None",
			"/p:DebugSymbols=false",
			"/p:EnableSourceControlManagerQueries=false",
			"/p:ExperimentalFileBasedProgramEnableRefDirective=true"
		]
	);
	if (publishExitCode != 0) {
		throw new InvalidOperationException($"Publishing the language server failed with exit code {publishExitCode}.");
	}
	
	if (!File.Exists(publishedExecutable)) {
		throw new FileNotFoundException($"SPO.LSP.Server.exe was not found at \"{publishedExecutable}\".");
	}
	
	RecreateDirectory(runtimeDir);
	RetryFileSystem(
		() => File.Copy(
			publishedExecutable,
			Path.Combine(runtimeDir, "SPO.LSP.Server.exe"),
			overwrite: true
		)
	);
	
	Console.WriteLine($"SPO language server published to \"{runtimeDir}\"");
}

static void
InstallFilesFromManifest(
	string scriptDir,
	string targetDir,
	JsonElement packageJson
) {
	var fileEntries = GetStringList(packageJson, "files");
	if (fileEntries.Count == 0) {
		throw new InvalidOperationException("The package.json file list is empty.");
	}
	
	var installNodeModules = false;
	
	foreach (var fileEntry in fileEntries) {
		if (IsNodeModulesEntry(fileEntry)) {
			installNodeModules = true;
			continue;
		}
		
		InstallFileEntry(
			scriptDir: scriptDir,
			targetDir: targetDir,
			fileEntry: fileEntry
		);
	}
	
	if (installNodeModules) {
		InstallNodeModules(
			scriptDir: scriptDir,
			targetDir: targetDir,
			packageJson: packageJson
		);
	}
}

static void
InstallFileEntry(
	string scriptDir,
	string targetDir,
	string fileEntry
) {
	if (IsDirectoryGlob(fileEntry)) {
		var relativeDirectory = GetDirectoryGlobPath(fileEntry);
		var sourceDir = Path.Combine(scriptDir, relativeDirectory);
		var destinationDir = Path.Combine(targetDir, relativeDirectory);
		
		if (!Directory.Exists(sourceDir)) {
			throw new DirectoryNotFoundException($"Required directory \"{sourceDir}\" was not found.");
		}
		
		CopyDirectory(sourceDir, destinationDir);
		return;
	}
	
	if (ContainsWildcard(fileEntry)) {
		throw new InvalidOperationException($"Unsupported file pattern \"{fileEntry}\".");
	}
	
	var normalizedRelativePath = NormalizeManifestPath(fileEntry);
	var sourceFile = Path.Combine(scriptDir, normalizedRelativePath);
	var targetFile = Path.Combine(targetDir, normalizedRelativePath);
	
	if (!File.Exists(sourceFile)) {
		throw new FileNotFoundException($"Required file \"{sourceFile}\" was not found.");
	}
	
	EnsureParentDirectory(targetFile);
	RetryFileSystem(() => File.Copy(sourceFile, targetFile, overwrite: true));
}

static void
InstallNodeModules(
	string scriptDir,
	string targetDir,
	JsonElement packageJson
) {
	var sourceNodeModulesDir = Path.Combine(scriptDir, "node_modules");
	var targetNodeModulesDir = Path.Combine(targetDir, "node_modules");
	
	if (Directory.Exists(sourceNodeModulesDir) && HasDirectoryContent(sourceNodeModulesDir)) {
		CopyDirectory(sourceNodeModulesDir, targetNodeModulesDir);
		return;
	}
	
	if (!HasDependencies(packageJson)) {
		return;
	}
	
	if (TryResolveNpmExecutable() is { } npmExecutable) {
		var exitCode = RunProcess(
			fileName: npmExecutable,
			workingDirectory: targetDir,
			arguments: [
				"install",
				"--omit=dev",
				"--no-fund",
				"--no-audit",
				"--package-lock=false"
			]
		);
		if (exitCode != 0) {
			throw new InvalidOperationException($"npm install failed with exit code {exitCode}.");
		}
		
		return;
	}
	
	InstallDependenciesFromRegistry(
		packageJson: packageJson,
		nodeModulesDir: targetNodeModulesDir
	);
}

static bool
HasDependencies(
	JsonElement packageJson
) => packageJson.TryGetProperty("dependencies", out var dependencies) &&
	dependencies.ValueKind == JsonValueKind.Object &&
	dependencies.EnumerateObject().Any();

static string?
TryResolveNpmExecutable(
) {
	var executableNames = OperatingSystem.IsWindows()
		? new[] { "npm.cmd", "npm.exe" }
		: new[] { "npm" };
	
	foreach (var executableName in executableNames) {
		var directCandidate = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
			"nodejs",
			executableName
		);
		if (File.Exists(directCandidate)) {
			return directCandidate;
		}
	}
	
	foreach (var pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? "")
		.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
	) {
		foreach (var executableName in executableNames) {
			var candidate = Path.Combine(pathEntry, executableName);
			if (File.Exists(candidate)) {
				return candidate;
			}
		}
	}
	
	return null;
}

static void
InstallDependenciesFromRegistry(
	JsonElement packageJson,
	string nodeModulesDir
) {
	Directory.CreateDirectory(nodeModulesDir);
	
	var tempRoot = Path.Combine(
		Path.GetTempPath(),
		"spo-lang-deploy-" + Guid.NewGuid().ToString("N")
	);
	var installedPackages = new HashSet<string>(StringComparer.Ordinal);
	
	Directory.CreateDirectory(tempRoot);
	
	try {
		foreach (var dependency in GetStringMap(packageJson, "dependencies")) {
			InstallPackage(
				packageName: dependency.Key,
				version: ResolveExactVersion(dependency.Value),
				nodeModulesDir: nodeModulesDir,
				tempRoot: tempRoot,
				installedPackages: installedPackages
			);
		}
	} finally {
		DeleteDirectoryIfExists(tempRoot);
	}
}

static void
InstallPackage(
	string packageName,
	string version,
	string nodeModulesDir,
	string tempRoot,
	HashSet<string> installedPackages
) {
	var packageKey = packageName + "@" + version;
	if (!installedPackages.Add(packageKey)) {
		return;
	}
	
	var packageMetadata = DownloadPackageMetadata(packageName, version);
	var archivePath = Path.Combine(
		tempRoot,
		packageName.Replace("/", "__", StringComparison.Ordinal) + "-" + version + ".tgz"
	);
	var packagePath = Path.Combine(
		nodeModulesDir,
		Path.Combine(packageName.Split('/', StringSplitOptions.RemoveEmptyEntries))
	);
	
	DeleteDirectoryIfExists(packagePath);
	Directory.CreateDirectory(packagePath);
	
	DownloadFile(packageMetadata.TarballUrl, archivePath);
	ExtractPackageArchive(archivePath, packagePath);
	
	foreach (var dependency in packageMetadata.Dependencies) {
		InstallPackage(
			packageName: dependency.Key,
			version: ResolveExactVersion(dependency.Value),
			nodeModulesDir: nodeModulesDir,
			tempRoot: tempRoot,
			installedPackages: installedPackages
		);
	}
}

static tPackageMetadata
DownloadPackageMetadata(
	string packageName,
	string version
) {
	using var response = Globals.Http.Send(
		new HttpRequestMessage(
			HttpMethod.Get,
			$"https://registry.npmjs.org/{Uri.EscapeDataString(packageName)}/{version}"
		)
	);
	response.EnsureSuccessStatusCode();
	
	using var responseStream = response.Content.ReadAsStream();
	using var document = JsonDocument.Parse(responseStream);
	var root = document.RootElement;
	
	if (
		!root.TryGetProperty("dist", out var dist) ||
		!dist.TryGetProperty("tarball", out var tarball) ||
		tarball.ValueKind != JsonValueKind.String ||
		tarball.GetString() is not { } tarballUrl
	) {
		throw new InvalidOperationException(
			$"Package metadata for \"{packageName}@{version}\" did not contain a tarball URL."
		);
	}
	
	return new tPackageMetadata(
		TarballUrl: tarballUrl,
		Dependencies: new Dictionary<string, string>(
			GetStringMap(root, "dependencies"),
			StringComparer.Ordinal
		)
	);
}

static void
DownloadFile(
	string url,
	string targetPath
) {
	using var response = Globals.Http.Send(
		new HttpRequestMessage(HttpMethod.Get, url)
	);
	response.EnsureSuccessStatusCode();
	
	EnsureParentDirectory(targetPath);
	
	using var sourceStream = response.Content.ReadAsStream();
	using var targetStream = File.Create(targetPath);
	sourceStream.CopyTo(targetStream);
}

static void
ExtractPackageArchive(
	string archivePath,
	string packagePath
) {
	using var archiveStream = File.OpenRead(archivePath);
	using var gzipStream = new GZipStream(archiveStream, CompressionMode.Decompress);
	using var tarReader = new TarReader(gzipStream, leaveOpen: false);
	
	while (tarReader.GetNextEntry() is { } entry) {
		var targetPath = TryResolveArchivePath(packagePath, entry.Name);
		if (targetPath is null) {
			continue;
		}
		
		if (entry.EntryType == TarEntryType.Directory) {
			Directory.CreateDirectory(targetPath);
			continue;
		}
		
		if (entry.EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink) {
			continue;
		}
		
		if (entry.DataStream is null) {
			continue;
		}
		
		EnsureParentDirectory(targetPath);
		using var targetStream = File.Create(targetPath);
		entry.DataStream.CopyTo(targetStream);
	}
}

static string?
TryResolveArchivePath(
	string packagePath,
	string entryName
) {
	var parts = entryName.Split('/', StringSplitOptions.RemoveEmptyEntries);
	if (parts.Length < 2) {
		return null;
	}
	
	var relativeParts = parts.Skip(1).ToArray();
	if (relativeParts.Any(__ => __ == "..")) {
		throw new InvalidOperationException($"Invalid archive entry \"{entryName}\".");
	}
	
	return Path.Combine(packagePath, Path.Combine(relativeParts));
}

static string
ResolveExactVersion(
	string versionRange
) {
	var match = Globals.VersionPattern.Match(versionRange);
	if (!match.Success) {
		throw new InvalidOperationException($"Unsupported npm version range \"{versionRange}\".");
	}
	
	return match.Groups[1].Value;
}

static void
ValidateInstall(
	string targetDir
) {
	var packageJsonPath = Path.Combine(targetDir, "package.json");
	if (!File.Exists(packageJsonPath)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{packageJsonPath}\".");
	}
	
	var languageConfigurationPath = Path.Combine(targetDir, "SPO.configuration.json");
	if (!File.Exists(languageConfigurationPath)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{languageConfigurationPath}\".");
	}
	
	var grammarPath = Path.Combine(targetDir, "SPO.tmLanguage.json");
	if (!File.Exists(grammarPath)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{grammarPath}\".");
	}
	
	var wikiLanguageConfigurationPath = Path.Combine(targetDir, "wiki.configuration.json");
	if (!File.Exists(wikiLanguageConfigurationPath)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{wikiLanguageConfigurationPath}\".");
	}
	
	var wikiGrammarPath = Path.Combine(targetDir, "wiki.tmLanguage.json");
	if (!File.Exists(wikiGrammarPath)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{wikiGrammarPath}\".");
	}
	
	var extensionEntryPoint = Path.Combine(targetDir, "src", "extension.js");
	if (!File.Exists(extensionEntryPoint)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{extensionEntryPoint}\".");
	}
	
	var serverExecutable = Path.Combine(targetDir, "server", "SPO.LSP.Server.exe");
	if (!File.Exists(serverExecutable)) {
		throw new FileNotFoundException($"The deployed extension is missing \"{serverExecutable}\".");
	}
	
	if (!HasLanguageClientEntry(Path.Combine(targetDir, "node_modules"))) {
		throw new FileNotFoundException("The deployed extension is missing vscode-languageclient.");
	}
}

static IReadOnlyList<string>
GetStringList(
	JsonElement element,
	string name
) {
	if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array) {
		return [];
	}
	
	return property.EnumerateArray()
		.Where(__ => __.ValueKind == JsonValueKind.String)
		.Select(__ => __.GetString())
		.Where(__ => __ is { Length: > 0 })
		.Cast<string>()
		.ToArray();
}

static IReadOnlyDictionary<string, string>
GetStringMap(
	JsonElement element,
	string name
) {
	if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Object) {
		return new Dictionary<string, string>(StringComparer.Ordinal);
	}
	
	var values = new Dictionary<string, string>(StringComparer.Ordinal);
	foreach (var item in property.EnumerateObject()) {
		if (item.Value.ValueKind != JsonValueKind.String) {
			continue;
		}
		
		values[item.Name] = item.Value.GetString() ?? "";
	}
	
	return values;
}

static string
GetRequiredString(
	JsonElement element,
	string name
) => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
	? value.GetString() ?? ""
	: throw new InvalidOperationException($"Required string property \"{name}\" was not found.");

static bool
IsNodeModulesEntry(
	string fileEntry
) => NormalizeGlob(fileEntry).Equals("node_modules/**", StringComparison.OrdinalIgnoreCase);

static bool
IsDirectoryGlob(
	string fileEntry
) => NormalizeGlob(fileEntry).EndsWith("/**", StringComparison.Ordinal);

static string
GetDirectoryGlobPath(
	string fileEntry
) {
	var normalized = NormalizeGlob(fileEntry);
	return NormalizeManifestPath(normalized[..^3]);
}

static bool
ContainsWildcard(
	string fileEntry
) => fileEntry.IndexOfAny(['*', '?']) >= 0;

static string
NormalizeManifestPath(
	string path
) => path.Replace('/', Path.DirectorySeparatorChar)
	.Replace('\\', Path.DirectorySeparatorChar);

static string
NormalizeGlob(
	string path
) => path.Replace('\\', '/');

static bool
PathsEqual(
	string left,
	string right
) => string.Equals(
	Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
	Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
	StringComparison.OrdinalIgnoreCase
);

static bool
HasDirectoryContent(
	string path
) => Directory.EnumerateFileSystemEntries(path).Any();

static bool
HasLanguageClientEntry(
	string nodeModulesDir
) => File.Exists(Path.Combine(nodeModulesDir, "vscode-languageclient", "node.js")) ||
	File.Exists(Path.Combine(nodeModulesDir, "vscode-languageclient", "lib", "node", "main.js"));

static void
DeleteOtherInstalledVersions(
	string extensionsRootDir,
	string extensionPrefix,
	string currentTargetDir
) {
	if (!Directory.Exists(extensionsRootDir)) {
		return;
	}
	
	foreach (var directory in Directory.GetDirectories(extensionsRootDir)) {
		var directoryName = Path.GetFileName(directory);
		if (!directoryName.StartsWith(extensionPrefix, StringComparison.OrdinalIgnoreCase)) {
			continue;
		}
		
		if (PathsEqual(directory, currentTargetDir)) {
			continue;
		}
		
		DeleteDirectoryIfExists(directory);
	}
}

static void
CopyDirectory(
	string sourceDir,
	string targetDir
) {
	Directory.CreateDirectory(targetDir);
	
	foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories)) {
		var relativePath = Path.GetRelativePath(sourceDir, directory);
		Directory.CreateDirectory(Path.Combine(targetDir, relativePath));
	}
	
	foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories)) {
		var relativePath = Path.GetRelativePath(sourceDir, file);
		var targetPath = Path.Combine(targetDir, relativePath);
		
		EnsureParentDirectory(targetPath);
		RetryFileSystem(() => File.Copy(file, targetPath, overwrite: true));
	}
}

static void
RecreateDirectory(
	string path
) {
	DeleteDirectoryIfExists(path);
	Directory.CreateDirectory(path);
}

static void
ReplaceDirectory(
	string sourceDir,
	string targetDir
) {
	var targetParentDir = Path.GetDirectoryName(targetDir)
		?? throw new InvalidOperationException("Could not determine the target parent directory.");
	
	var backupDir = "";
	
	Directory.CreateDirectory(targetParentDir);
	
	if (Directory.Exists(targetDir)) {
		backupDir = targetDir + ".backup." + Guid.NewGuid().ToString("N");
		RetryFileSystem(() => Directory.Move(targetDir, backupDir));
	}

	try {
		RetryFileSystem(() => Directory.Move(sourceDir, targetDir));
	} catch {
		if (
			backupDir.Length > 0 &&
			Directory.Exists(backupDir) &&
			!Directory.Exists(targetDir)
		) {
			RetryFileSystem(() => Directory.Move(backupDir, targetDir));
		}
		
		throw;
	}
	
	DeleteDirectoryIfExists(backupDir);
}

static void
EnsureParentDirectory(
	string path
) {
	var parentDir = Path.GetDirectoryName(path)
		?? throw new InvalidOperationException($"Could not determine the target directory for \"{path}\".");
	Directory.CreateDirectory(parentDir);
}

static int
RunProcess(
	string fileName,
	string workingDirectory,
	IEnumerable<string> arguments
) {
	var startInfo = new ProcessStartInfo {
		FileName = fileName,
		WorkingDirectory = workingDirectory,
		UseShellExecute = false
	};
	
	foreach (var argument in arguments) {
		startInfo.ArgumentList.Add(argument);
	}
	
	using var process = Process.Start(startInfo)
		?? throw new InvalidOperationException($"Could not start process \"{fileName}\".");
	process.WaitForExit();
	return process.ExitCode;
}

static void
DeleteDirectoryIfExists(
	string path
) {
	if (path.Length == 0 || !Directory.Exists(path)) {
		return;
	}
	
	RetryFileSystem(() => Directory.Delete(path, recursive: true));
}

static void
RetryFileSystem(
	Action action
) {
	const int Attempts = 5;
	
	for (var attempt = 1; ; attempt++) {
		try {
			action();
			return;
		} catch (Exception exception) when (
			exception is IOException or UnauthorizedAccessException &&
			attempt < Attempts
		) {
			Thread.Sleep(200 * attempt);
		}
	}
}

sealed record
tOptions(
	bool ShowHelp,
	bool PublishServerOnly,
	bool SkipServerBuild,
	bool KeepOldVersions,
	string TargetDir
);

sealed record
tPackageMetadata(
	string TarballUrl,
	Dictionary<string, string> Dependencies
);

static class
Globals {
	public static readonly HttpClient Http = new();
	public static readonly Regex
	VersionPattern = new(@"^[\^~]?(\d+\.\d+\.\d+)$", RegexOptions.Compiled);
}
