#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Exe"
#:property PublishAot = "false"
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../src/mStartup.cs
#:ref ../Runtime/Terminal/mTerminal.cs

public static class
m2048_Tests {
	private static tInt32
	Int(
		mVM_Data.tData aValue
	) {
		mAssert.IsTrue(aValue.IsInt(out var Value));
		return Value;
	}
	
	private static mVM_Data.tData
	State(
		tInt32[] aTiles,
		tInt32 aScore = 0,
		tInt32 aSeed = 1
	) {
		mAssert.AreEquals(16, aTiles.Length);
		var Rows = new mVM_Data.tData[4];
		
		for (var Row = 0; Row < 4; Row += 1) {
			var Offset = Row * 4;
			
			Rows[Row] = mVM_Data.Tuple(
				[
					mVM_Data.Int(aTiles[Offset]),
					mVM_Data.Int(aTiles[Offset + 1]),
					mVM_Data.Int(aTiles[Offset + 2]),
					mVM_Data.Int(aTiles[Offset + 3])
				]
			);
		}
		
		return mVM_Data.Record(
			[
				("_Board", mVM_Data.Tuple(Rows)),
				("_Score", mVM_Data.Int(aScore)),
				("_RandomState", mVM_Data.Int(aSeed))
			]
		);
	}
	
	private static tInt32[]
	Tiles(
		mVM_Data.tData aState
	) {
		var Board = aState.Field("Board");
		var Result = new tInt32[16];
		
		for (var Row = 3; Row >= 0; Row -= 1) {
			mAssert.IsTrue(Board.IsPair(out Board, out var Cells));
			
			for (var Column = 3; Column >= 0; Column -= 1) {
				mAssert.IsTrue(Cells.IsPair(out Cells, out var Cell));
				Result[Row * 4 + Column] = Int(Cell);
			}
			
			mAssert.IsTrue(Cells.IsEmpty());
		}
		
		mAssert.IsTrue(Board.IsEmpty());
		return Result;
	}
	
	private static mVM_Data.tData
	Key(
		tText aKey,
		tBool aDown = true,
		tBool aControl = false
	) => mVM_Data.Prefix(
		"_Key...",
		mVM_Data.Record(
			[
				(
					"_Key",
					aKey.Length == 1
					? mVM_Data.Prefix("_Character...", mVM_Data.Prefix("_Char...", mVM_Data.Int(aKey[0])))
					: mVM_Data.Prefix("_" + aKey, mVM_Data.Empty())
				),
				("_Character", mVM_Data.Prefix("_Char...", mVM_Data.Int(aKey.Length == 1 ? aKey[0] : 0))),
				("_IsDown", mVM_Data.Bool(aDown)),
				("_IsRepeat", mVM_Data.Bool(false)),
				("_RepeatCount", mVM_Data.Int(1)),
				(
					"_Modifiers",
					mVM_Data.Record(
						[
							("_Shift", mVM_Data.Bool(false)),
							("_Alt", mVM_Data.Bool(false)),
							("_Control", mVM_Data.Bool(aControl)),
							("_CapsLock", mVM_Data.Bool(false)),
							("_NumLock", mVM_Data.Bool(false)),
							("_ScrollLock", mVM_Data.Bool(false))
						]
					)
				)
			]
		)
	);
	
	private static tBool
	HasSameStateAs(
		this mVM_Data.tData aState,
		mVM_Data.tData aOther
	) => (
		System.MemoryExtensions.SequenceEqual(Tiles(aState).AsSpan(), Tiles(aOther).AsSpan()) &&
		Int(aState.Field("Score")) == Int(aOther.Field("Score")) &&
		Int(aState.Field("RandomState")) == Int(aOther.Field("RandomState"))
	);
	
	public static tInt32
	Main() => mRuntime.WithCompilerStack(
		() => {
			var Startup = mStartup.Load(
				System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mStd.File())!, "../Apps/2048/startup.jni")
			);
			
			var App = Startup.Entry.Init(_ => {}).ElseThrow().Data;
			mAssert.IsTrue(App.IsSig(out var Contract, out _, out var Body));
			mAssert.IsTrue(Contract.SameType(mTerminal.LoadSignature("App")));
			var Modules = Startup.Modules.Where(__ => __.Key is "Logic" or "Render").Init(_ => {}).ElseThrow();
			var Logic = Modules.Data.Field("Logic");
			var Move = Logic.Field("...AfterMove...");
			var CanMove = Logic.Field("...CanMove");
			var Initial = Body.Field("Initial");
			System.Console.WriteLine("OK: 2048 module graph and App.SIG.");
			
			{
				// Independent reference for both random draws, including a 4-valued tile.
				foreach (var Seed in new[] {1, 13, 19, tInt32.MaxValue}) {
					var Expected = new tInt32[16];
					var Random = Seed;
					
					for (var Spawn = 0; Spawn < 2; Spawn += 1) {
						const tInt64 cModulus = tInt32.MaxValue;
						var Normalized = Random % (cModulus - 1);
						
						if (Normalized <= 0) {
							Normalized += cModulus - 1;
						}
						
						var PositionRandom = Normalized * 16807 % cModulus;
						Random = (tInt32)(PositionRandom * 16807 % cModulus);
						var Skip = (PositionRandom - 1) % (16 - Spawn);
						
						for (var Index = 0; Index < 16; Index += 1) {
							if (Expected[Index] != 0) {
								continue;
							}
							
							if (Skip-- == 0) {
								Expected[Index] = Random % 10 == 0 ? 4 : 2;
								break;
							}
						}
					}
					
					var New = Logic.Field("NewGameWithSeed...").Call(mVM_Data.Int(Seed));
					mAssert.IsTrue(System.MemoryExtensions.SequenceEqual(Expected.AsSpan(), Tiles(New).AsSpan()));
					mAssert.AreEquals(0, Int(New.Field("Score")));
					mAssert.AreEquals(Random, Int(New.Field("RandomState")));
					mAssert.IsTrue(New.HasSameStateAs(Logic.Field("NewGameWithSeed...").Call(mVM_Data.Int(Seed))));
				}
				
				System.Console.WriteLine("OK: two initial tiles, random positions/values, seeds and replay.");
			}
			
			{
				foreach (
					var (Input, Output, Score) in new (tInt32[], tInt32[], tInt32)[] {
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
						var Before = new tInt32[16];
						var Expected = new tInt32[16];
						
						for (var Index = 0; Index < 4; Index += 1) {
							var Position = Direction switch {
								"Left" => Index,
								"Right" => 15 - Index,
								"Up" => Index * 4 + 1,
								_ => (3 - Index) * 4 + 2
							};
							
							Before[Position] = Input[Index];
							Expected[Position] = Output[Index];
						}
						
						var Start = State(Before, 17);
						
						var Next = Move.Call(
							mVM_Data.Tuple([Start, mVM_Data.Prefix("_" + Direction, mVM_Data.Empty())])
						);
						
						var Actual = Tiles(Next);
						var Spawned = 0;
						
						for (var Index = 0; Index < 16; Index += 1) {
							if (Expected[Index] != 0) {
								mAssert.IsTrue(
									Expected[Index] == Actual[Index],
									$"{Direction} [{tText.Join(',', Input)}], cell {Index}: " +
									$"expected {Expected[Index]}, got {Actual[Index]}"
								);
							} else if (Actual[Index] != 0) {
								mAssert.IsTrue(Actual[Index] is 2 or 4);
								Spawned += 1;
							}
						}
						
						mAssert.AreEquals(1, Spawned);
						mAssert.AreEquals(17 + Score, Int(Next.Field("Score")));
						mAssert.AreEquals(1, Int(Start.Field("RandomState")));
						mAssert.IsTrue(System.MemoryExtensions.SequenceEqual(Before.AsSpan(), Tiles(Start).AsSpan()));
						mAssert.IsFalse(Start.Field("RandomState").Equals(Next.Field("RandomState")));
						
						mAssert.IsTrue(
							Next.HasSameStateAs(
								Move.Call(
									mVM_Data.Tuple([Start, mVM_Data.Prefix("_" + Direction, mVM_Data.Empty())])
								)
							)
						);
					}
				}
				
				System.Console.WriteLine("OK: 36 directional slides, gaps, single merges, scoring and immutability.");
			}
			
			var Blocked = State([2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 2], 42, 123);
			
			{
				foreach (var Direction in new[] {"Left", "Right", "Up", "Down"}) {
					mAssert.IsTrue(
						Blocked.HasSameStateAs(
							Move.Call(
								mVM_Data.Tuple([Blocked, mVM_Data.Prefix("_" + Direction, mVM_Data.Empty())])
							)
						)
					);
				}
				
				var Packed = State([2, 4, 8, 16, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], 42, 123);
				
				mAssert.IsTrue(
					Packed.HasSameStateAs(
						Move.Call(
							mVM_Data.Tuple([Packed, mVM_Data.Prefix("_Left", mVM_Data.Empty())])
						)
					)
				);
				
				mAssert.IsTrue(CanMove.Call(Blocked.Field("Board")).IsBool(out var Available) && !Available);
				
				foreach (
					var Cells in new tInt32[][] {
						[2, 2, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536],
						[2, 4, 8, 16, 2, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536],
						[2, 4, 2, 4, 4, 2, 4, 2, 2, 4, 2, 4, 4, 2, 4, 0]
					}
				) {
					mAssert.IsTrue(CanMove.Call(State(Cells).Field("Board")).IsBool(out Available) && Available);
				}
				
				var Full = State([2, 2, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536]);
				var Next = Move.Call(mVM_Data.Tuple([Full, mVM_Data.Prefix("_Left", mVM_Data.Empty())]));
				var Values = Tiles(Next);
				mAssert.AreEquals(4, Values[0]);
				mAssert.AreEquals(8, Values[1]);
				mAssert.AreEquals(16, Values[2]);
				mAssert.IsTrue(Values[3] is 2 or 4);
				mAssert.AreEquals(4, Int(Next.Field("Score")));
				mAssert.IsTrue(System.MemoryExtensions.SequenceEqual(Tiles(Full).AsSpan(4), Values.AsSpan(4)));
				
				System.Console.WriteLine(
					"OK: unchanged moves preserve score/random state; game over and last free cell."
				);
			}
			
			{
				var Size = new mTerminal.tSize((80, 25), (0, 0), (80, 25));
				var Screen = new tChar[80 * 25];
				var Backgrounds = new mTerminal.tColor[80 * 25];
				var Visible = true;
				var Draws = 0;
				
				var Terminal = new mTerminal.tTerminal {
					Size = () => Size,
					Cursor = (_, _) => mAssert.Fail("Drawing must not move the cursor."),
					CursorVisible = () => Visible,
					SetCursorVisible = aVisible => Visible = aVisible,
					Clear = () => System.Array.Fill(Screen, ' '),
					Present = () => {},
					ReadEvent = _ => mVM_Data.Empty(),
					Draw = (aText, aPosition, aStyle) => {
						Draws += 1;
						mAssert.IsFalse(aStyle.Blink);
						
						for (var Index = 0; Index < aText.Length; Index += 1) {
							var Offset = aPosition.Y * 80 + aPosition.X + Index;
							Screen[Offset] = aText[Index];
							Backgrounds[Offset] = aStyle.Background;
						}
					}
				};
				
				Terminal.WithCapability(
					aCapability => {
						var Frame = Body.Field("Frame...");
						var Current = Initial;
						
						mStd.tFunc<mVM_Data.tData, tBool> Send = aEvent => {
							var Result = Frame.Call(mVM_Data.Tuple([Current, aEvent]), aCapability);
							mAssert.IsTrue(Result.IsTuple(out Current, out var Continue));
							mAssert.IsTrue(Continue.IsBool(out var Running));
							return Running;
						};
						
						mAssert.IsTrue(Send(mVM_Data.Prefix("_Resize...", Size.ToData())));
						mAssert.IsFalse(Visible);
						mAssert.AreEquals("2048 in SPO", new tText(Screen, 0, 11));
						mAssert.AreEquals('+', Screen[4 * 80]);
						mAssert.AreEquals('+', Screen[12 * 80 + 36]);
						
						for (var Index = 0; Index < 16; Index += 1) {
							var Background = Backgrounds[(5 + Index / 4 * 2) * 80 + 1 + Index % 4 * 9];
							mAssert.AreEquals(Tiles(Initial)[Index] == 0, Background == mTerminal.tColor.Black);
						}
						
						Draws = 0;
						Send(mVM_Data.Prefix("_Tick...", mVM_Data.Int(60000)));
						Send(Key("ArrowLeft", aDown: false));
						mAssert.AreEquals(0, Draws);
						mAssert.IsTrue(Current.HasSameStateAs(Initial));
						Size = new((80, 25), (0, 0), (20, 10));
						Send(mVM_Data.Prefix("_Resize...", Size.ToData()));
						mAssert.IsTrue(new tText(Screen, 0, 20).Contains("Fenster mindestens"));
						Send(Key("ArrowLeft"));
						mAssert.IsTrue(Current.HasSameStateAs(Initial));
						Size = new((80, 25), (0, 0), (80, 25));
						Send(mVM_Data.Prefix("_Resize...", Size.ToData()));
						Send(Key("ArrowLeft"));
						mAssert.IsFalse(Current.HasSameStateAs(Initial));
						Current = Blocked;
						Send(mVM_Data.Prefix("_Resize...", Size.ToData()));
						mAssert.IsTrue(new tText(Screen, 2 * 80, 80).Contains("Keine Zuege mehr"));
						Send(Key("R"));
						mAssert.AreEquals(0, Int(Current.Field("Score")));
						mAssert.IsFalse(Current.Field("Board").Equals(Initial.Field("Board")));
						Current = State([131072, 2048, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);
						Send(mVM_Data.Prefix("_Resize...", Size.ToData()));
						mAssert.IsTrue(new tText(Screen, 2 * 80, 80).Contains("2048 erreicht"));
						mAssert.AreEquals(" 131072 ", new tText(Screen, 5 * 80 + 1, 8));
						mAssert.IsTrue(Send(Key("D")));
						mAssert.IsFalse(Current.Field("RandomState").Equals(mVM_Data.Int(1)));
						mAssert.IsFalse(Send(Key("Escape")));
						mAssert.IsFalse(Send(Key("Q")));
						mAssert.IsTrue(Send(Key("C")));
						mAssert.IsFalse(Send(Key("C", aControl: true)));
					}
				);
				
				System.Console.WriteLine("OK: keys, resize, static colored rendering, win/continue, restart and quit.");
			}
			
			return 0;
		}
	);
}