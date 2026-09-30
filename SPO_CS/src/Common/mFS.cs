#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStd.cs
#:ref mRef.cs
#:ref mStream.cs
#:ref mMaybe.cs
#:ref mError.cs
#:ref mResult.cs
#:ref mArrayList.cs

using System.Linq;

public static class
mFS {
	public static readonly tPath cIdentPath = new (".");
	public static readonly tPath cParentPath = new ("..");
	
	public readonly struct
	tPath {
		public readonly mRef.tRef<tPath> Parent;
		public readonly tText Name;
		
		public tPath(
			tText aName
		) {
			this.Parent = mRef.NullRef<tPath>();
			this.Name = aName;
		}
		
		public tPath(
			tPath aParent,
			tText aName
		) {
			this.Parent = aParent;
			this.Name = aName;
		}
		
		public static tPath
		operator /(
			tPath aBasePath,
			tPath aChild
		) => (
			aChild.IsAbsolutePath ? aChild :
			aBasePath == cIdentPath ? aChild :
			aChild == cIdentPath ? aBasePath :
			aChild == cParentPath && aBasePath.Name == ".." ? new (aBasePath, "..") :
			aChild == cParentPath ? aBasePath.Parent.Deref.ElseUse(aBasePath.IsAbsolutePath ? aBasePath : cIdentPath) :
			aChild.Parent.Is(out var Parent) ? (aBasePath / Parent) / aChild.Name :
			new (aBasePath, aChild.Name)
		);
		
		public static mMaybe.tMaybe<tPath>
		operator >>(
			tPath aSrcPath,
			tPath aDesPath
		) {
			if (aSrcPath.IsIdent) {
				return aDesPath;
			}
			
			if (aDesPath.IsIdent) {
				return (
					aSrcPath.Parent.IsEmpty()
					? cParentPath
					: (aSrcPath[1..].AssertNotEmpty() >> aDesPath).Then(__ => cParentPath / __)
				);
			}
			
			if (aSrcPath.GetRootName() == aDesPath.GetRootName()) {
				return aSrcPath[1..].AssertNotEmpty() >> aDesPath[1..].AssertNotEmpty();
			}
			
			var ParentSrcPath = aSrcPath.Parent.Deref.ElseUse(cIdentPath);
			var RemindingPath = ParentSrcPath >> aDesPath;
			
			return RemindingPath.Match(
				__ => {
					return mMaybe.Some(cParentPath / __);
				},
				() => mStd.cEmpty
			);
		}
		
		public static tBool
		operator ==(
			tPath aPath1,
			tPath aPath2
		) => (
			aPath1.Name == aPath2.Name &&
			aPath1.Parent.Deref.IsSome(out var Parent1) == aPath2.Parent.Deref.IsSome(out var Parent2) &&
			(aPath1.Parent.IsEmpty() || Parent1 == Parent2)
		);
		
		public static tBool
		operator !=(
			tPath aPath1,
			tPath aPath2
		) => !(aPath1 == aPath2);
		
		public mMaybe.tMaybe<tPath>
		this[
			System.Range aRange
		] {
			get {
				if (!aRange.Start.IsFromEnd && aRange.Start.Value is 0) {
					return (
						!aRange.End.IsFromEnd ? this[..^(this.Length() - aRange.End.Value)] :
						aRange.End.Value is 0 ? this :
						this.Parent.Is(out var Parent) ? Parent[..^(aRange.End.Value - 1)] :
						cIdentPath
					);
				}
				
				tInt32 StartFromEnd;
				tInt32 EndFromEnd; 
				if (!aRange.Start.IsFromEnd || !aRange.End.IsFromEnd) {
					var Length = this.Length();
					StartFromEnd = aRange.Start.IsFromEnd ? aRange.Start.Value : Length - aRange.Start.Value;
					EndFromEnd = aRange.End.IsFromEnd ? aRange.End.Value : Length - aRange.End.Value;
					
					return (
						StartFromEnd == Length ? this[..^EndFromEnd] :
						(StartFromEnd > Length || EndFromEnd > Length) ? cIdentPath :
						this[^StartFromEnd..^EndFromEnd]
					);
				} else {
					StartFromEnd = aRange.Start.Value;
					EndFromEnd = aRange.End.Value;
				}
				
				var Name_ = this.Name;
				
				return EndFromEnd switch {
					< 0 => cIdentPath,
					0 => StartFromEnd switch {
						< 0 => cIdentPath,
						0 => cIdentPath,
						1 => new tPath(this.Name),
						> 1 => (
							this.Parent.Is(out var Parent)
							? Parent[^(StartFromEnd - 1)..].Then(__ => __ / Name_)
							: cIdentPath
						),
					},
					> 0 => (
						this.Parent.Is(out var Parent)
						? Parent[^(StartFromEnd - 1)..^(EndFromEnd - 1)]
						: cIdentPath
					),
				};
			}
		}
		
		public static implicit operator tPath(
			tText aPath
		) => Path(aPath);
		
		public override tText
		ToString(
		) => this.ToText();
	}
	
	public readonly struct
	tFolder {
		public readonly tPath
		_Path;
		
		internal tFolder(
			tPath aPath
		) {
			this._Path = aPath;
		}
		
		public static tFolder
		operator /(
			tFolder aFolder,
			tPath aPath
		) => new (aFolder._Path / aPath);
		
		public static mMaybe.tMaybe<tPath>
		operator >> (
			tFolder aFolder1,
			tFolder aFolder2
		) => aFolder1._Path >> aFolder2._Path;
		
		public static mMaybe.tMaybe<tPath>
		operator >> (
			tFolder aFolder,
			tFile aFile
		) => aFolder._Path >> aFile.Path;
	}
	
	public readonly struct
	tFile {
		internal readonly mRef.tRef<tFolder>
		_Folder;
		
		public readonly tText
		Name;
		
		internal tFile(
			tFolder aFolder,
			tText aName
		) {
			this._Folder = aFolder;
			this.Name = aName;
		}
	}
	
	public static readonly tPath?
	cEmptyPath = default;
	
	public static tPath
	Path(
		tText aPath
	) {
		if (System.Uri.TryCreate(aPath, System.UriKind.Absolute, out var Uri) && Uri.IsFile) {
			aPath = Uri.LocalPath;
		}

		if (aPath is [_, ':']) {
			return new (aPath);
		}

		var Path = cIdentPath;
		if (System.IO.Path.IsPathRooted(aPath)) {
			aPath = System.IO.Path.GetFullPath(aPath);
			var Root = System.IO.Path.GetPathRoot(aPath)!;
			var RootName = Root.Replace('\\', '/').TrimEnd('/');
			Path = new (RootName.Length == 0 ? "/" : RootName);
			aPath = aPath[Root.Length..];
		}

		foreach (var Child in aPath.Split(['/', '\\'], System.StringSplitOptions.RemoveEmptyEntries)) {
			if (Child.IsAbsoluteRootName()) {
				throw mError.Error($"can't append absolute path '{Child}'");
			}
			Path /= new tPath(Child);
		}

		return Path;
	}
	
	public static tFolder
	Folder(
		tPath aPath
	) => new (aPath);
	
	public static tFile
	File(
		tPath aPath
	) => new (mFS.Folder(aPath.Parent.Deref.ElseUse(cIdentPath)), aPath.Name);
	
	public static tFolder
	CWD(
	) => new (System.IO.Directory.GetCurrentDirectory());
	
	public static tBool
	IsAbsoluteRootName(
		this tText a
	) => a == "/" || a.StartsWith("//", System.StringComparison.Ordinal) || (
		a is [var DriveLetter, ':'] &&
		(
			('a' <= DriveLetter && DriveLetter <= 'z') ||
			('A' <= DriveLetter && DriveLetter <= 'Z')
		)
	);
	
	extension (tPath aPath) {
		public tPath
		ToAbsolute(
		) => Path(System.IO.Path.GetFullPath(aPath.ToText()));

		public tText
		ToUri(
		) => new System.Uri(aPath.ToAbsolute().ToText()).AbsoluteUri;

		public tInt32
		Length(
		) => aPath.Parent.Is(out var Parent)
		? Parent.Length() + 1
		: 1;
		
		public tPath
		Normalize(
		) => aPath.Name switch {
			"." => aPath.Parent.Is(out var Parent) ? Parent : aPath,
			".." => aPath.Parent.Deref.Match(
				() => aPath,
				Parent => Parent.Parent.Deref.Match(
					() => (
						Parent.IsAbsolutePath
						? throw mError.Error($"can't go to parent from absolut root path '{Parent}'")
						: cIdentPath
					),
					ParentParent => ParentParent
				)
			),
			_ => (
				aPath.Parent.Is(out var Parent)
				? mStd.With(
					(
						Path: aPath,
						NormalizedParent: Parent.Normalize()
					),
					static __ => (
						__.Path.Parent.IsRefEqual(__.NormalizedParent)
						? __.Path
						: __.NormalizedParent / __.Path.Name
					)
				)
				: aPath
			),
		};
		
		public tText
		ToText(
		) => (
			aPath.Parent.Is(out var Parent)
			? $"{Parent.ToText().TrimEnd('/')}/{aPath.Name}"
			: aPath.Name is [_, ':'] ? aPath.Name + "/" : aPath.Name
		);
		
		public tBool
		IsIdent => aPath.Name is "." && aPath.Parent.IsEmpty();
		
		public tText
		GetRootName(
		) => (
			aPath.Parent.Is(out var Parent)
			? Parent.GetRootName()
			: aPath.Name
		);
		
		public tBool
		IsAbsolutePath => aPath.GetRootName().IsAbsoluteRootName();
	}
	
	extension (tFolder aFolder) {
		public tBool
		Exists(
		) => System.IO.Directory.Exists(
			aFolder._Path.ToText()
		);
		
		public tBool
		FileExists(
			tPath aPath
		) => System.IO.File.Exists(
			(aFolder._Path / aPath).ToText()
		);
		
		public tFile
		GetFile(
			tPath aPath
		) => File(aFolder._Path / aPath);
		
		public mStream.tStream<tFile>
		GetFiles(
		) => System.IO.Directory.GetFiles(
			aFolder._Path.ToText(),
			"*",
			System.IO.SearchOption.AllDirectories
		).ToArray(
		).AsStream(
		).Map(
			__ => new tFile(aFolder, (aFolder._Path >> Path(__)).AssertNotEmpty().ToText())
		);
		
		public mStream.tStream<tFolder>
		GetFolders(
		) => System.IO.Directory.GetDirectories(
			aFolder._Path.ToText(),
			"*",
			System.IO.SearchOption.AllDirectories
		).ToArray(
		).AsStream(
		).Map(
			__ => aFolder / __
		);
		
		public tBool
		TryCreate(
		) {
			if (!aFolder.Exists()) {
				System.IO.Directory.CreateDirectory(
					aFolder._Path.ToText()
				);
			}
			
			return aFolder.Exists();
		}
		
		public tBool
		TryRemove(
		) {
			if (aFolder.Exists()) {
				System.IO.Directory.Delete(
					aFolder._Path.ToText(),
					true
				);
			}
			
			return !aFolder.Exists();
		}
	}
	
	extension (tFile aFile) {
		public tPath Path => aFile._Folder.Deref.AssertNotEmpty()._Path / aFile.Name;

		public tFolder Folder => mFS.Folder(aFile.Path.Parent.Deref.ElseUse(cIdentPath));

		public tText Extension => System.IO.Path.GetExtension(aFile.Name).TrimStart('.');

		public tText NameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(aFile.Name);

		public tBool
		IsSameFile(
			tFile aOther
		) => tText.Equals(
			aFile.Path.ToAbsolute().ToText(),
			aOther.Path.ToAbsolute().ToText(),
			System.OperatingSystem.IsWindows() ? System.StringComparison.OrdinalIgnoreCase : System.StringComparison.Ordinal
		);
		
		public tBool
		Exists(
		) => System.IO.File.Exists(
			aFile.Path.ToText()
		);
		
		public tBool
		TryRemove(
		) {
			if (aFile.Exists()) {
				System.IO.File.Delete(
					aFile.Path.ToText()
				);
			}
			
			return !aFile.Exists();
		}
		
		public mResult.tResult<tText, tText>
		TryReadText(
		) => System.IO.File.ReadAllText(
			aFile.Path.ToText()
		);
		
		public mResult.tResult<mStream.tStream<tText>, tText>
		TryReadLines(
		) => System.IO.File.ReadAllLines(
			aFile.Path.ToText()
		).AsStream(
		);
		
		public tBool
		TryCreate(
			tText aText
		) {
			System.IO.File.WriteAllText(
				aFile.Path.ToText(),
				aText
			);
			
			return true;
		}
		
		public tBool
		TryCreate(
			mStream.tStream<tText> aLines
		) {
			System.IO.File.WriteAllLines(
				aFile.Path.ToText(),
				aLines.ToArrayList().ToArray()
			);
			
			return true;
		}
	}
}
