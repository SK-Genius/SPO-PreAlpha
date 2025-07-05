// IMPORT mStd
// IMPORT mRef
// IMPORT mStream
// IMPORT mMaybe
// IMPORT mError

using System.Linq;

public static class
mFS {
	public static readonly tPath cIdentPath = new (".");
	public static readonly tPath cBackPath = new ("..");
	
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
			aChild.IsIdent()                	? aBasePath :
			aChild.Parent.Is(out var Parent)	? aBasePath / Parent / aChild.Name :
			aBasePath / aChild.Name
		);
		
		public static tPath
		operator /(
			tPath aBasePath,
			tText aChild
		) => aBasePath / aChild.Split(['/', '\\']);
		
		public static tPath
		operator /(
			tPath aBasePath,
			System.ReadOnlySpan<tText> aChildren
		) {
			if (aChildren.Length is 0) {
				return aBasePath;
			}
			if (aBasePath.IsIdent()) {
				return new tPath(aChildren[0]) / aChildren[1..];
			}
			
			var First = aChildren[0];
			
			if (First is ".") {
				return aBasePath / aChildren[1..];
			} else if (First is "..") {;
				if (aBasePath.Name is "..") {
					return new tPath(aBasePath, "..") / aChildren[1..];
				} else if (aBasePath.Parent.Is(out var Parent)) {
					return Parent / aChildren[1..];
				} else {
					return cIdentPath / aChildren[1..];
				}
			} else if (First.IsAbsoluteRootName()) {
				throw mError.Error($"can't append absolute path '{First}'");
			} else {
				return new tPath(aBasePath, aChildren[0]) / aChildren[1..];
			}
		}
		
		public static mMaybe.tMaybe<tPath>
		operator >>(
			tPath aSrcPath,
			tPath aDesPath
		) {
			if (aSrcPath.IsIdent()) {
				return aDesPath;
			}
			
			if (aDesPath.IsIdent()) {
				return (
					aSrcPath.Parent.IsEmpty()
					? cBackPath
					: (aSrcPath[1..].AssertNotEmpty() >> aDesPath).ThenDo(_ => cBackPath / _)
				);
			}
			
			if (aSrcPath.GetRootName() == aDesPath.GetRootName()) {
				return aSrcPath[1..].AssertNotEmpty() >> aDesPath[1..].AssertNotEmpty();
			}
			
			var ParentSrcPath = aSrcPath.Parent.Deref.Else(cIdentPath);
			var RemindingPath = ParentSrcPath >> aDesPath;
			
			return RemindingPath.Match(
				_ => {
					return mMaybe.Some(cBackPath / _);
				},
				() => mStd.cEmpty
			);
		}
		
		public static tBool
		operator ==(
			tPath aPath1,
			tPath aPath2
		) {
			return (
				aPath1.Name == aPath2.Name &&
				aPath1.Parent == aPath2.Parent
			);
		}
		
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
					if (!aRange.End.IsFromEnd) {
						return this[..^(this.Length() - aRange.End.Value)];
					}
					
					if (aRange.End.Value is 0) {
						return this;
					}
					
					if (this.Parent.Is(out var Parent)) {
						return Parent[..^(aRange.End.Value - 1)];
					}
					
					return cIdentPath;
				}
				
				tInt32 StartFromEnd;
				tInt32 EndFromEnd; 
				if (!aRange.Start.IsFromEnd || !aRange.End.IsFromEnd) {
					var Length = this.Length();
					StartFromEnd = aRange.Start.IsFromEnd ? aRange.Start.Value : Length - aRange.Start.Value;
					EndFromEnd = aRange.End.IsFromEnd ? aRange.End.Value : Length - aRange.End.Value;
					
					if (StartFromEnd == Length) {
						return this[..^EndFromEnd];
					}
					
					return (
						StartFromEnd > Length || EndFromEnd > Length
						? cIdentPath
						: this[^StartFromEnd..^EndFromEnd]
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
							? Parent[^(StartFromEnd - 1)..].ThenDo(_ => _ / Name_)
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
		
		public override tText
		ToString(
		) => (
			this.Parent.Is(out var Parent)
			? $"{Parent}/{this.Name}"
			: this.Name
		);
	}
	
	public static readonly tPath?
	cEmptyPath = default;
	
	public static tPath
	Path(
		tText aPath
	) => cIdentPath / aPath.Split(['/', '\\']);
	
	public static tInt32
	Length(
		this tPath aPath
	) => aPath.Parent.Is(out var Parent)
	? Length(Parent) + 1
	: 1;
	
	public static tPath
	Normalize(
		this tPath aPath
	) => aPath.Name switch {
		"." => aPath.Parent.Is(out var Parent) ? Parent : aPath,
		".." => aPath.Parent.Deref.Match(
			() => aPath,
			Parent => Parent.Parent.Deref.Match(
				() => (
					Parent.IsAbsolutePath()
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
				static _ => (
					_.Path.Parent.IsRefEqual(_.NormalizedParent)
					? _.Path
					: _.NormalizedParent / _.Path.Name
				)
			)
			: aPath
		),
	};
	
	public static tText
	ToText(
		this tPath aPath
	) => (
		aPath.Parent.Is(out var Parent) ? $"{Parent.ToText()}/{aPath.Name}" :
		aPath.Name
	);
	
	public readonly struct
	tFS {
		internal readonly tPath
		_BasePath;
		
		internal tFS(
			tPath aBasePath
		) {
			this._BasePath = aBasePath;
		}
		
		public static tFS
		operator /(
			tFS aFS,
			tPath aPath
		) => new tFS(aFS._BasePath / aPath);
	}
	
	public static tFS
	Create(
		this tPath aPath
	) => new tFS(aPath);
	
	public static tFS
	FSFromCWD(
	) => Create(
		Path(System.IO.Directory.GetCurrentDirectory())
	);
	
	public static tBool
	FolderExists(
		this tFS aFS,
		tPath aPath
	) => System.IO.Directory.Exists(
		(aFS._BasePath / aPath).ToText()
	);
	
	public static tBool
	FileExists(
		this tFS aFS,
		tPath aPath
	) => System.IO.File.Exists(
		(aFS._BasePath / aPath).ToText()
	);
	
	public static mStream.tStream<tPath>
	GetFiles(
		this tFS aFS,
		tPath aPAth
	) => System.IO.Directory.GetFiles(
		(aFS._BasePath / aPAth).ToText(),
		"*",
		System.IO.SearchOption.AllDirectories
	).ToArray(
	).AsStream(
	).Map(
		_ => (aFS._BasePath >> Path(_)).AssertNotEmpty()
	);
	
	public static mStream.tStream<tPath>
	GetFolders(
		this tFS aFS,
		tPath aPAth
	) => System.IO.Directory.GetDirectories(
		(aFS._BasePath / aPAth).ToText(),
		"*",
		System.IO.SearchOption.AllDirectories
	).ToArray(
	).AsStream(
	).Map(
		_ => (aFS._BasePath >> Path(_)).AssertNotEmpty()
	);
	
	public static tText
	ReadAllText(
		this tFS aFS,
		tPath aPath
	) => System.IO.File.ReadAllText(
		(aFS._BasePath / aPath).ToText()
	);
	
	public static mStream.tStream<tText>
	ReadAllLines(
		this tFS aFS,
		tPath aPath
	) => System.IO.File.ReadAllLines(
		(aFS._BasePath / aPath).ToText()
	).AsStream(
	);
	
	public static void
	NewFolder(
		this tFS aFS,
		tPath aPath
	) {
		if (!aFS.FolderExists(aPath)) {
			System.IO.Directory.CreateDirectory(
				(aFS._BasePath / aPath).ToText()
			);
		}
	}
	
	public static void
	RemoveFolder(
		this tFS aFS,
		tPath aPath
	) {
		if (aFS.FolderExists(aPath)) {
			System.IO.Directory.Delete(
				(aFS._BasePath / aPath).ToText()
			);
		}
	}
	
	public static tBool
	IsIdent(
		this tPath a
	) => a.Name is "." && a.Parent.IsEmpty();
	
	public static tText
	GetRootName(
		this tPath a
	) => (
		a.Parent.Is(out var Parent)
		? Parent.GetRootName()
		: a.Name
	);
	
	public static tBool
	IsAbsoluteRootName(
		this tText a
	) => (
		a is [var DriveLetter, ':'] &&
		(
			('a' <= DriveLetter && DriveLetter <= 'z') ||
			('A' <= DriveLetter && DriveLetter <= 'Z')
		)
	);
	
	public static tBool
	IsAbsolutePath(
		this tPath a
	) => a.GetRootName().IsAbsoluteRootName();
	
}
