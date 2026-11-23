#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Exe"
#:property PublishAot = "false"
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../src/mStartup.cs
#:ref ../../Runtime/mRuntime.cs

using tFloat64 = System.Double;

public static class
m2048_Compare {
	private sealed record tSnapshot(tInt32[] Tiles, tInt32 Score, tInt32 RandomState);
	private sealed record tVariant(tText Name, mVM_Data.tData Logic);
	private sealed record tCase(tText Name, tText Field, mVM_Data.tData Argument);
	private sealed record tMeasurement(tFloat64 Microseconds, tFloat64 Bytes);
	private sealed record tSummary(
		tText Operation,
		tText Variant,
		tInt32 Iterations,
		tFloat64 MedianMicroseconds,
		tFloat64 MinimumMicroseconds,
		tFloat64 MaximumMicroseconds,
		tFloat64 MedianBytes,
		tMeasurement[] Samples
	);
	
	private static tInt32
	Int(
		this mVM_Data.tData aValue
	) {
		mAssert.IsTrue(aValue.IsInt(out var Value));
		return Value;
	}
	
	private static mVM_Data.tData
	ToData(
		this tSnapshot aState
	) {
		var Rows = new mVM_Data.tData[4];
		
		for (var Row = 0; Row < 4; Row += 1) {
			var Cells = new mVM_Data.tData[4];
			
			for (var Column = 0; Column < 4; Column += 1) {
				Cells[Column] = mVM_Data.Int(aState.Tiles[Row * 4 + Column]);
			}
			
			Rows[Row] = mVM_Data.Tuple(Cells);
		}
		
		return mVM_Data.Record(
			[
				("_Board", mVM_Data.Tuple(Rows)),
				("_Score", mVM_Data.Int(aState.Score)),
				("_RandomState", mVM_Data.Int(aState.RandomState))
			]
		);
	}
	
	private static tSnapshot
	Snapshot(
		this mVM_Data.tData aState
	) {
		var Board = aState.Field("Board");
		var Tiles = new tInt32[16];
		
		for (var Row = 3; Row >= 0; Row -= 1) {
			mAssert.IsTrue(Board.IsPair(out Board, out var Cells));
			
			for (var Column = 3; Column >= 0; Column -= 1) {
				mAssert.IsTrue(Cells.IsPair(out Cells, out var Tile));
				Tiles[Row * 4 + Column] = Tile.Int();
			}
			
			mAssert.IsTrue(Cells.IsEmpty(), "Each row must contain exactly four cells.");
		}
		
		mAssert.IsTrue(Board.IsEmpty(), "Each board must contain exactly four rows.");
		return new(Tiles, aState.Field("Score").Int(), aState.Field("RandomState").Int());
	}
	
	private static void
	AssertSame(
		this tSnapshot aExpected,
		tSnapshot aActual,
		tText aContext
	) {
		for (var Index = 0; Index < 16; Index += 1) {
			mAssert.IsTrue(
				aExpected.Tiles[Index] == aActual.Tiles[Index],
				$"{aContext}: tile {Index}, expected {aExpected.Tiles[Index]}, got {aActual.Tiles[Index]}."
			);
		}
		
		mAssert.IsTrue(aExpected.Score == aActual.Score, aContext + ": score differs.");
		mAssert.IsTrue(aExpected.RandomState == aActual.RandomState, aContext + ": random state differs.");
	}
	
	private static tSnapshot
	WithReferenceTile(
		this tSnapshot aState
	) {
		static tInt32 NextRandom(tInt32 aState) {
			const tInt64 cModulus = tInt32.MaxValue;
			var Seed = aState % (cModulus - 1);
			
			if (Seed <= 0) {
				Seed += cModulus - 1;
			}
			
			return (tInt32)(Seed * 16807 % cModulus);
		}
		
		var Free = 0;
		
		foreach (var Tile in aState.Tiles) {
			Free += Tile == 0 ? 1 : 0;
		}
		
		if (Free == 0) {
			return aState;
		}
		
		var PositionRandom = NextRandom(aState.RandomState);
		var ValueRandom = NextRandom(PositionRandom);
		var Skip = (PositionRandom - 1) % Free;
		var Tiles = (tInt32[])aState.Tiles.Clone();
		
		for (var Index = 0; Index < 16; Index += 1) {
			if (Tiles[Index] == 0 && Skip-- == 0) {
				Tiles[Index] = ValueRandom % 10 == 0 ? 4 : 2;
				return new(Tiles, aState.Score, ValueRandom);
			}
		}
		
		throw new System.InvalidOperationException("Reference did not find the selected empty cell.");
	}
	
	private static tInt32
	Position(
		tText aDirection,
		tInt32 aLine,
		tInt32 aOffset
	) => aDirection switch {
		"Left" => aLine * 4 + aOffset,
		"Right" => aLine * 4 + 3 - aOffset,
		"Up" => aOffset * 4 + aLine,
		"Down" => (3 - aOffset) * 4 + aLine,
		_ => throw new System.ArgumentOutOfRangeException(nameof(aDirection))
	};
	
	private static tSnapshot
	AfterReferenceMove(
		this tSnapshot aState,
		tText aDirection
	) {
		var Tiles = new tInt32[16];
		var Score = aState.Score;
		
		for (var Line = 0; Line < 4; Line += 1) {
			var Nonzero = new tInt32[4];
			var Count = 0;
			
			for (var Offset = 0; Offset < 4; Offset += 1) {
				var Value = aState.Tiles[Position(aDirection, Line, Offset)];
				
				if (Value != 0) {
					Nonzero[Count++] = Value;
				}
			}
			
			var Target = 0;
			
			for (var Offset = 0; Offset < Count; Offset += 1) {
				var Value = Nonzero[Offset];
				
				if (Offset + 1 < Count && Value == Nonzero[Offset + 1]) {
					Value *= 2;
					Score += Value;
					Offset += 1;
				}
				
				Tiles[Position(aDirection, Line, Target++)] = Value;
			}
		}
		
		return (
			System.MemoryExtensions.SequenceEqual(Tiles.AsSpan(), aState.Tiles.AsSpan())
			? aState
			: new tSnapshot(Tiles, Score, aState.RandomState).WithReferenceTile()
		);
	}
	
	private static mVM_Data.tData
	MoveArgument(
		this mVM_Data.tData aState,
		tText aDirection
	) => mVM_Data.Tuple([aState, mVM_Data.Prefix("_" + aDirection, mVM_Data.Empty())]);
	
	private static void
	VerifyMove(
		tVariant[] aVariants,
		tText aName,
		tSnapshot aBefore,
		tText aDirection,
		tSnapshot aExpected
	) {
		var Input = aBefore.ToData();
		var Argument = Input.MoveArgument(aDirection);
		
		foreach (var Variant in aVariants) {
			var Actual = Variant.Logic.Field("...AfterMove...").Call(Argument).Snapshot();
			aExpected.AssertSame(Actual, Variant.Name + "/" + aName + "/" + aDirection);
			aBefore.AssertSame(Input.Snapshot(), Variant.Name + ": immutable input");
		}
	}
	
	private static tInt32
	Verify(
		tVariant[] aVariants
	) {
		var Cases = 0;
		
		foreach (var Seed in new[] {1, 13, 19, tInt32.MaxValue, 0, -1, tInt32.MinValue}) {
			var Expected = new tSnapshot(new tInt32[16], 0, Seed).WithReferenceTile().WithReferenceTile();
			
			foreach (var Variant in aVariants) {
				var Actual = Variant.Logic.Field("NewGameWithSeed...").Call(mVM_Data.Int(Seed)).Snapshot();
				Expected.AssertSame(Actual, Variant.Name + "/initial seed " + Seed);
			}
			
			Cases += 1;
		}
		
		foreach (
			var (Before, After, Score) in new (tInt32[], tInt32[], tInt32)[] {
				([2, 2, 2, 2], [4, 4, 0, 0], 8),
				([2, 2, 4, 0], [4, 4, 0, 0], 4),
				([4, 0, 2, 2], [4, 4, 0, 0], 4),
				([0, 2, 0, 2], [4, 0, 0, 0], 4),
				([2, 2, 2, 0], [4, 2, 0, 0], 4),
				([2, 4, 4, 4], [2, 8, 4, 0], 8),
				([0, 0, 0, 4], [4, 0, 0, 0], 0),
				([1024, 1024, 0, 0], [2048, 0, 0, 0], 2048),
				([65536, 65536, 0, 0], [131072, 0, 0, 0], 131072)
			}
		) {
			foreach (var Direction in new[] {"Left", "Right", "Up", "Down"}) {
				var Input = new tInt32[16];
				var Output = new tInt32[16];
				
				for (var Offset = 0; Offset < 4; Offset += 1) {
					var Line = Direction is "Left" or "Right" ? 2 : 1;
					var Index = Position(Direction, Line, Offset);
					Input[Index] = Before[Offset];
					Output[Index] = After[Offset];
				}
				
				var Start = new tSnapshot(Input, 17, 1);
				var Expected = new tSnapshot(Output, 17 + Score, 1).WithReferenceTile();
				Expected.AssertSame(Start.AfterReferenceMove(Direction), "C# reference against fixed row result");
				VerifyMove(aVariants, "row " + tText.Join(',', Before), Start, Direction, Expected);
				Cases += 1;
			}
		}
		
		foreach (
			var (Name, Tiles, CanMove) in new (tText, tInt32[], tBool)[] {
				("empty", new tInt32[16], true),
				("packed", [2, 4, 8, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], true),
				("blocked", [2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 2], false),
				("horizontal", [2, 2, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], true),
				("vertical", [2, 4, 8, 16, 2, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], true),
				("last-free", [2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 0], true),
				("mixed", [2, 2, 4, 4, 8, 8, 8, 8, 0, 0, 2, 4, 4, 2, 2, 8], true)
			}
		) {
			var Start = new tSnapshot(Tiles, 42, 13);
			var Input = Start.ToData();
			var Board = Input.Field("Board");
			var Largest = 0;
			
			foreach (var Tile in Tiles) {
				Largest = System.Math.Max(Largest, Tile);
			}
			
			foreach (var Variant in aVariants) {
				mAssert.AreEquals(Largest, Variant.Logic.Field("...LargestTile").Call(Board).Int());
				var Available = Variant.Logic.Field("...CanMove").Call(Board);
				mAssert.IsTrue(Available.IsBool(out var Actual) && Actual == CanMove, Variant.Name + "/" + Name);
				
				for (var Index = 0; Index < 20; Index += 1) {
					var Column = Index < 16 ? Index % 4 : new[] {-1, 4, 0, 0}[Index - 16];
					var Row = Index < 16 ? Index / 4 : new[] {0, 0, -1, 4}[Index - 16];
					var Expected = Index < 16 ? Tiles[Index] : 0;
					
					mAssert.AreEquals(
						Expected,
						Variant.Logic.Field("...TileAt...").Call(
							mVM_Data.Tuple([Board, mVM_Data.Tuple([mVM_Data.Int(Column), mVM_Data.Int(Row)])])
						).Int()
					);
				}
			}
			
			foreach (var Direction in new[] {"Left", "Right", "Up", "Down"}) {
				VerifyMove(aVariants, Name, Start, Direction, Start.AfterReferenceMove(Direction));
				Cases += 1;
			}
		}
		
		var Replay = new tSnapshot(new tInt32[16], 0, 19).WithReferenceTile().WithReferenceTile();
		
		for (var Step = 0; Step < 16; Step += 1) {
			var Direction = new[] {"Up", "Left", "Down", "Right"}[Step % 4];
			var Expected = Replay.AfterReferenceMove(Direction);
			VerifyMove(aVariants, "replay " + Step, Replay, Direction, Expected);
			Replay = Expected;
			Cases += 1;
		}
		
		System.Console.WriteLine(
			$"OK: {Cases} initial/move fixtures, queries and input preservation for both variants."
		);
		
		return Cases;
	}
	
	private static tVariant
	Load(
		tText aFolder,
		tText aName,
		out tMeasurement aCheck,
		out tMeasurement aInitialization
	) {
		var Allocated = System.GC.GetAllocatedBytesForCurrentThread();
		var Started = Stopwatch.GetTimestamp();
		var Setup = mStartup.Load(System.IO.Path.Combine(aFolder, aName + ".jni"));
		Setup.Entry.Check(_ => {}).ElseThrow();
		var Elapsed = Stopwatch.GetTimestamp() - Started;
		
		aCheck = new(
			Elapsed * 1000000.0 / Stopwatch.Frequency,
			System.GC.GetAllocatedBytesForCurrentThread() - Allocated
		);
		
		Allocated = System.GC.GetAllocatedBytesForCurrentThread();
		Started = Stopwatch.GetTimestamp();
		var Logic = Setup.Entry.Init(_ => {}).ElseThrow().Data;
		Elapsed = Stopwatch.GetTimestamp() - Started;
		
		aInitialization = new(
			Elapsed * 1000000.0 / Stopwatch.Frequency,
			System.GC.GetAllocatedBytesForCurrentThread() - Allocated
		);
		
		return new(aName, Logic);
	}
	
	private static tMeasurement
	Measure(
		mVM_Data.tData aFunction,
		mVM_Data.tData aArgument,
		tInt32 aIterations
	) {
		mVM_Data.tData? Last = null;
		var Allocated = System.GC.GetAllocatedBytesForCurrentThread();
		var Started = Stopwatch.GetTimestamp();
		
		for (var Iteration = 0; Iteration < aIterations; Iteration += 1) {
			Last = aFunction.Call(aArgument);
		}
		
		var Elapsed = Stopwatch.GetTimestamp() - Started;
		var Bytes = System.GC.GetAllocatedBytesForCurrentThread() - Allocated;
		System.GC.KeepAlive(Last);
		return new(Elapsed * 1000000.0 / Stopwatch.Frequency / aIterations, (tFloat64)Bytes / aIterations);
	}
	
	private static tSummary
	Summarize(
		tText aOperation,
		tText aVariant,
		tInt32 aIterations,
		tMeasurement[] aSamples
	) {
		var Times = new tFloat64[aSamples.Length];
		var Bytes = new tFloat64[aSamples.Length];
		
		for (var Index = 0; Index < aSamples.Length; Index += 1) {
			Times[Index] = aSamples[Index].Microseconds;
			Bytes[Index] = aSamples[Index].Bytes;
		}
		
		System.Array.Sort(Times);
		System.Array.Sort(Bytes);
		
		return new(
			aOperation,
			aVariant,
			aIterations,
			Times[Times.Length / 2],
			Times[0],
			Times[^1],
			Bytes[Bytes.Length / 2],
			aSamples
		);
	}
	
	private static tCase[]
	Cases() {
		var Mixed = new tSnapshot([2, 2, 4, 4, 8, 8, 8, 8, 0, 0, 2, 4, 4, 2, 2, 8], 17, 13).ToData();
		var Packed = new tSnapshot([2, 4, 8, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 42, 1).ToData();
		var Blocked = new tSnapshot([2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 2], 42, 1).ToData();
		
		var Horizontal = new tSnapshot(
			[2, 2, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536],
			42,
			1
		).ToData();
		
		var Vertical = new tSnapshot(
			[2, 4, 8, 16, 2, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536],
			42,
			1
		).ToData();
		
		var LastFree = new tSnapshot([2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 0], 42, 1).ToData();
		
		return [
			new("NewGame/1", "NewGameWithSeed...", mVM_Data.Int(1)),
			new("NewGame/13", "NewGameWithSeed...", mVM_Data.Int(13)),
			new(
				"TileAt/first",
				"...TileAt...",
				mVM_Data.Tuple([Mixed.Field("Board"), mVM_Data.Tuple([mVM_Data.Int(0), mVM_Data.Int(0)])])
			),
			new(
				"TileAt/last",
				"...TileAt...",
				mVM_Data.Tuple([Mixed.Field("Board"), mVM_Data.Tuple([mVM_Data.Int(3), mVM_Data.Int(3)])])
			),
			new(
				"TileAt/outside",
				"...TileAt...",
				mVM_Data.Tuple([Mixed.Field("Board"), mVM_Data.Tuple([mVM_Data.Int(4), mVM_Data.Int(0)])])
			),
			new("Largest/dense", "...LargestTile", Horizontal.Field("Board")),
			new("Largest/sparse", "...LargestTile", Packed.Field("Board")),
			new("CanMove/sparse", "...CanMove", Packed.Field("Board")),
			new("CanMove/blocked", "...CanMove", Blocked.Field("Board")),
			new("CanMove/horizontal", "...CanMove", Horizontal.Field("Board")),
			new("CanMove/vertical", "...CanMove", Vertical.Field("Board")),
			new("CanMove/last-free", "...CanMove", LastFree.Field("Board")),
			new("Move/mixed-left", "...AfterMove...", Mixed.MoveArgument("Left")),
			new("Move/mixed-right", "...AfterMove...", Mixed.MoveArgument("Right")),
			new("Move/mixed-up", "...AfterMove...", Mixed.MoveArgument("Up")),
			new("Move/mixed-down", "...AfterMove...", Mixed.MoveArgument("Down")),
			new("Move/packed-noop", "...AfterMove...", Packed.MoveArgument("Left")),
			new("Move/blocked-noop", "...AfterMove...", Blocked.MoveArgument("Left")),
			new("Move/last-cell", "...AfterMove...", Horizontal.MoveArgument("Left"))
		];
	}
	
	private static void
	WriteResults(
		tText aFolder,
		tText aBuild,
		tInt32 aFixtures,
		tSummary[] aSummaries
	) {
		static tUnknown SourceStats(tText aPath) {
			var Source = System.IO.File.ReadAllText(aPath);
			var Lines = Source.Replace("\r", "").TrimEnd('\n').Split('\n');
			var ImportEnd = System.Array.FindIndex(Lines, __ => __ == "}");
			var Nonblank = 0;
			var Body = 0;
			
			for (var Index = 0; Index < Lines.Length; Index += 1) {
				if (!tText.IsNullOrWhiteSpace(Lines[Index])) {
					Nonblank += 1;
					Body += Index > ImportEnd ? 1 : 0;
				}
			}
			
			return new {
				Path = aPath,
				Sha256 = System.Convert.ToHexString(
					System.Security.Cryptography.SHA256.HashData(
						System.Text.Encoding.UTF8.GetBytes(Source)
					)
				),
				Lines = Lines.Length,
				NonblankLines = Nonblank,
				NonblankAfterImports = Body,
				TypeAnnotations = System.Text.RegularExpressions.Regex.Matches(Source, "€").Count
			};
		}
		
		var Report = new {
			MeasuredAtUtc = System.DateTimeOffset.UtcNow,
			Runtime = RuntimeInformation.FrameworkDescription,
			Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
			OperatingSystem = RuntimeInformation.OSDescription,
			ProcessorCount = System.Environment.ProcessorCount,
			BuildConfiguration = aBuild,
			SamplesPerCase = 7,
			MinimumCalibrationMilliseconds = 100,
			BatchCalibration = "Per variant; at least 100 ms before seven samples",
			Fixtures = aFixtures,
			SourceStats = new[] {
				SourceStats(System.IO.Path.GetFullPath(System.IO.Path.Combine(aFolder, "../../Apps/2048/Logic.spo"))),
				SourceStats(System.IO.Path.Combine(aFolder, "Logic.spo"))
			},
			DependencyHashes = new[] {"Compare.cs", "../../Libs/List.SPO", "../../src/Std.ILT"}.AsStream().Map(
				aPath => {
					var Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(aFolder, aPath));
					
					return new {
						Path,
						Sha256 = System.Convert.ToHexString(
							System.Security.Cryptography.SHA256.HashData(System.IO.File.ReadAllBytes(Path))
						)
					};
				}
			).ToArrayList().ToArray(),
			Summaries = aSummaries
		};
		
		System.IO.File.WriteAllText(
			System.IO.Path.Combine(aFolder, "results.json"),
			System.Text.Json.JsonSerializer.Serialize(
				Report,
				new System.Text.Json.JsonSerializerOptions {WriteIndented = true}
			)
		);
		
		var Markdown = new System.Text.StringBuilder();
		Markdown.AppendLine("# Gemessene Ergebnisse");
		Markdown.AppendLine();
		Markdown.AppendLine($"{Report.MeasuredAtUtc:O}; {Report.Runtime}; {Report.Architecture}; {aBuild}.");
		Markdown.AppendLine();
		Markdown.AppendLine($"Korrektheit: {aFixtures} Initialisierungs-/Zugfälle plus Abfragen für beide Varianten.");
		
		Markdown.AppendLine(
			"Sieben Stichproben; Zeitangaben pro Aufruf in Mikrosekunden, Speicher in zugewiesenen Bytes."
		);
		
		Markdown.AppendLine();
		
		Markdown.AppendLine(
			"| Operation | Variante | Wiederholungen je Stichprobe | Median µs | Min–Max µs | Median Bytes |"
		);
		
		Markdown.AppendLine("| --- | --- | ---: | ---: | ---: | ---: |");
		
		foreach (var Summary in aSummaries) {
			Markdown.AppendLine(
				$"| {Summary.Operation} | {Summary.Variant} | {Summary.Iterations} | " +
				$"{Summary.MedianMicroseconds:F3} | {Summary.MinimumMicroseconds:F3}–" +
				$"{Summary.MaximumMicroseconds:F3} | {Summary.MedianBytes:F0} |"
			);
		}
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(aFolder, "Results.md"), Markdown.ToString());
	}
	
	public static tInt32
	Main(
		tText[] aArgs
	) => mRuntime.WithCompilerStack(
		() => {
			if (aArgs.Length > 1 || (aArgs.Length == 1 && aArgs[0] != "--check")) {
				System.Console.Error.WriteLine("Compare.cs [--check]");
				return 1;
			}
			
			var CheckOnly = aArgs.Length == 1;
			
			var Build = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<
				System.Reflection.AssemblyConfigurationAttribute
			>(typeof(m2048_Compare).Assembly)?.Configuration ?? "unknown";
			
			if (!CheckOnly && Build != "Release") {
				System.Console.Error.WriteLine("Measurements require --configuration Release.");
				return 1;
			}
			
			var Folder = System.IO.Path.GetDirectoryName(mStd.File())!;
			var Variants = new tVariant[2];
			
			for (var Index = 0; Index < 2; Index += 1) {
				var Name = Index == 0 ? "tuple" : "list";
				Variants[Index] = Load(Folder, Name, out _, out _);
			}
			
			var Fixtures = Verify(Variants);
			
			if (CheckOnly) {
				return 0;
			}
			
			var Summaries = mArrayList.List<tSummary>();
			var Checks = new[] {new tMeasurement[7], new tMeasurement[7]};
			var Initializations = new[] {new tMeasurement[7], new tMeasurement[7]};
			
			for (var Round = 0; Round < 7; Round += 1) {
				for (var Order = 0; Order < 2; Order += 1) {
					var Index = (Round + Order) % 2;
					
					Variants[Index] = Load(
						Folder,
						Variants[Index].Name,
						out Checks[Index][Round],
						out Initializations[Index][Round]
					);
				}
				
				System.Console.WriteLine($"Startup sample {Round + 1}/7 complete.");
			}
			
			for (var Index = 0; Index < 2; Index += 1) {
				Summaries.Push(Summarize("Startup/check", Variants[Index].Name, 1, Checks[Index]));
				Summaries.Push(Summarize("Startup/init", Variants[Index].Name, 1, Initializations[Index]));
			}
			
			foreach (var Case in Cases()) {
				var Functions = new[] {Variants[0].Logic.Field(Case.Field), Variants[1].Logic.Field(Case.Field)};
				var Iterations = new[] {1, 1};
				
				for (var Index = 0; Index < 2; Index += 1) {
					while (true) {
						var Sample = Measure(Functions[Index], Case.Argument, Iterations[Index]);
						
						if (Sample.Microseconds * Iterations[Index] >= 100000) {
							break;
						}
						
						Iterations[Index] = checked(Iterations[Index] * 2);
					}
				}
				
				var Samples = new[] {new tMeasurement[7], new tMeasurement[7]};
				
				for (var Round = 0; Round < 7; Round += 1) {
					for (var Order = 0; Order < 2; Order += 1) {
						var Index = (Round + Order) % 2;
						Samples[Index][Round] = Measure(Functions[Index], Case.Argument, Iterations[Index]);
					}
				}
				
				for (var Index = 0; Index < 2; Index += 1) {
					Summaries.Push(Summarize(Case.Name, Variants[Index].Name, Iterations[Index], Samples[Index]));
				}
				
				System.Console.WriteLine(
					$"Measured {Case.Name}: tuple {Iterations[0]}, list {Iterations[1]} calls x 7."
				);
			}
			
			WriteResults(Folder, Build, Fixtures, Summaries.ToArray());
			System.Console.WriteLine("Wrote results.json and Results.md.");
			return 0;
		}
	);
}