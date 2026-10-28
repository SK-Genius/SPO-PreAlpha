#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../src/mStartup.cs
#:ref ../Runtime/Terminal/mTerminal.cs

public static class
mRuntime_Tests {
	private static tInt32
	Int(
		mVM_Data.tData aData
	) {
		mAssert.IsTrue(aData.IsInt(out var Value));
		return Value;
	}
	
	public static tInt32
	Main() => mRuntime.WithCompilerStack(
		() => {
			{
				var Folder = System.IO.Path.GetTempPath();
				var Name = "SPO-startup-" + System.Guid.NewGuid().ToString("N");
				var ConfigPath = System.IO.Path.Combine(Folder, Name + ".jni");
				var ModulePath = System.IO.Path.Combine(Folder, Name + ".spo");
				var ILT_Path = System.IO.Path.ChangeExtension(ModulePath, ".ILT");
				var ValuePath = System.IO.Path.Combine(Folder, Name + "-value.SPO");
				try {
					System.IO.File.WriteAllText(ValuePath, "§EXPORT 37");
					System.IO.File.WriteAllText(
						ModulePath,
						(
							"§IMPORT {First: §DEF cFirst € §INT, Second: §DEF cSecond € §INT}\n§EXPORT " +
							"(cFirst, cSecond)"
						)
					);
					System.IO.File.WriteAllText(
						ConfigPath,
						$$"""
						# Startup test with nested JNI and comments between value lines.
						entry=Consumer
						modules:
						|Consumer:
						||path={{Name}}.*
						||imports:
						|||First=Value
						||# This comment belongs to the module document.
						|||Second=Value
						|Value:
						||path={{Name}}-value.*
						"""
					);
					var Setup = mStartup.Load(ConfigPath);
					mAssert.IsTrue(
						Setup.Entry.Init(
							_ => {}
						).ElseThrow().Data.Equals(mVM_Data.Tuple([mVM_Data.Int(37), mVM_Data.Int(37)]))
					);
					var Dependencies = Setup.Entry.Dependencies.ToArrayList().ToArray();
					mAssert.IsTrue(
						ReferenceEquals(Dependencies[0].Module, Dependencies[1].Module),
						"Shared dependency initialized once."
					);
					foreach (var (Config, ExpectedError) in new[] {
							("", "Startup requires"),
							("entry=Missing\nmodules:", "Unknown module 'Missing'"),
							(
								"""
								entry=A
								modules:
								|A:
								||path=a.*
								||imports:
								|||Next=B
								|B:
								||path=b.*
								||imports:
								|||Back=A
								""",
								"Circular module imports: A -> B -> A"
							),
							(
								"entry=A\nmodules:\n|A:\n||capability=Terminal",
								"unexpected property 'capability'"
							),
							(
								"entry=A\nmodules:\n|A:\n||path=a.*\n||capability=Terminal",
								"unexpected property 'capability'"
							),
							("entry=A\nmodules:\n|A:\n|A:", "Duplicate key 'A'"),
							(
								"entry=A\nmodules:\n|A:\n||path=a.*\n||imports:\n|||Missing=B",
								"Unknown module 'B'"
							),
							("entry=A\nmodules:\n|A:\n||path=", "expected a nonempty string"),
							(
								"entry=A\nmodules:\n|A:\n||capability=Terminal\n||imports:",
								"unexpected property 'capability'"
							),
							("entry=A\nmodules:\n|A:\n||other=value", "unexpected property 'other'"),
							("entry=A\nmodules:\n|A:", "expected 'path'"),
							(
								"entry=A\nmodules:\n|A:\n||path=a.*\n||imports=malformed",
								":5: Expected key=value"
							),
							(
								"entry=A\nmodules:\n|A:\n||path=a.*\n||imports:\n|||First=Missing\n|||First=Again",
								":7: Duplicate key 'First'"
							)
						}
					
					) {
						System.IO.File.WriteAllText(ConfigPath, Config);
						try {
							mStartup.Load(ConfigPath);
							mAssert.Fail("Invalid startup was accepted: " + Config);
						} catch (System.Exception Error)when (Error is System.FormatException or mError.tError) {
							mAssert.IsTrue(Error.Message.Contains(ExpectedError), Error.Message);
						}
					}
				} finally {
					System.IO.File.Delete(ConfigPath);
					System.IO.File.Delete(ModulePath);
					System.IO.File.Delete(ILT_Path);
					foreach (var Extension in new[] {
							".SPO",
							".ILT"
						}
					
					) {
						System.IO.File.Delete(System.IO.Path.ChangeExtension(ValuePath, Extension));
					}
				}
			}
			
			var Dimensions = new mTerminal.tSize((100, 70), (10, 20), (80, 25));
			var Screen = new tChar[100 * 70];
			var X = 0;
			var Y = 0;
			var CursorVisible = true;
			var SawBlink = false;
			var SawGreen = false;
			var LastStyle = default(mTerminal.tStyle);
			var Terminal = new mTerminal.tTerminal {
				Size = () => Dimensions,
				Cursor = (aX, aY) => {
					mAssert.IsTrue(aX >= 0 && aX < Dimensions.BufferSize.X);
					mAssert.IsTrue(aY >= 0 && aY < Dimensions.BufferSize.Y);
					(X, Y) = (aX, aY);
				},
				CursorVisible = () => CursorVisible,
				SetCursorVisible = aVisible => CursorVisible = aVisible,
				Draw = (aText, aPosition, aStyle) => {
					LastStyle = aStyle;
					SawBlink |= aStyle.Blink;
					SawGreen |= aStyle.Foreground == mTerminal.tColor.Green;
					var Column = aPosition.X;
					foreach (var Character in aText) {
						if (Column >= Dimensions.BufferSize.X) {
							break;
						}
						
						Screen[aPosition.Y * 100 + Column] = Character;
						Column += 1;
					}
				},
				Clear = () => System.Array.Fill(Screen, ' '),
				Present = () => {},
				ReadEvent = _ => mVM_Data.Empty()
			};
			var Path = System.IO.Path.GetFullPath(
				System.IO.Path.Combine(
					System.IO.Path.GetDirectoryName(
						mStd.File()
					)!,
					"../Apps/Snake/Snake.spo"
				)
			);
			mVM_Data.tData? SavedCapability = null;
			Terminal.WithCapability(
				aCapability => {
					var StartupPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Path)!, "startup.jni");
					var Startup = mStartup.Load(StartupPath);
					var Libraries = Startup.Modules.Where(
						aModule => aModule.Key is "Std" or "Math" or "Random" or "Char" or "Text" or "List" or
						"Vector2" or
						"Style"
					).Init(
						_ => {}
					).ElseThrow();
					{
						var NextRandom = Libraries.Data.Field("Random").Field("...NextRandom");
						var RandomState = 1;
						foreach (var Expected in new[] {
								16807,
								282475249,
								1622650073,
								984943658,
								1144108930
							}
						
						) {
							RandomState = Int(NextRandom.Call(mVM_Data.Int(RandomState)));
							mAssert.AreEquals(Expected, RandomState);
						}
						
						foreach (var Seed in new[] {
								tInt32.MinValue,
								-1,
								0,
								1,
								127773,
								tInt32.MaxValue - 1,
								tInt32.MaxValue
							}
						
						) {
							RandomState = Seed;
							for (var Step = 0; Step < 32; Step += 1) {
								// Independent 64-bit multiplication checks the overflow-free SPO calculation.
								const tInt64 cStateCount = tInt32.MaxValue - 1;
								var Normalized = RandomState % cStateCount;
								if (Normalized <= 0) {
									Normalized += cStateCount;
								}
								
								var Expected = (tInt32)(Normalized * 16807 % tInt32.MaxValue);
								RandomState = Int(NextRandom.Call(mVM_Data.Int(RandomState)));
								mAssert.AreEquals(Expected, RandomState);
								mAssert.IsTrue(RandomState > 0 && RandomState < tInt32.MaxValue);
							}
						}
						
						var First = NextRandom.Call(mVM_Data.Int(1));
						mAssert.IsTrue(
							First.Equals(NextRandom.Call(mVM_Data.Int(1))),
							"Generator state is explicit and immutable."
						);
						System.Console.WriteLine("OK: random generator reference sequence and integer boundaries.");
					}
					
					{
						var Code = """
						§IMPORT {
							Char: {...AsChar: §DEF ...AsChar, ...Ord: §DEF ...Ord} € [§LOAD "../Libs/Char.SIG"]
							Text: {
								...Trim: §DEF ...Trim
								...ToUpper: §DEF ...ToUpper
								...==...: §DEF ...Text==...
							} € [§LOAD "../Libs/Text.SIG"]
						}
						§EXPORT (((65 .AsChar) .Ord), ((" snake " .Trim) .ToUpper) .Text== "SNAKE")
						""";
						var Result = mSPO_Interpreter.Run(
							Code,
							mStd.File(),
							Libraries,
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(Result.Data.IsTuple(out var Ordinal, out var Equal));
						mAssert.AreEquals(65, Int(Ordinal));
						mAssert.IsTrue(Equal.IsBool(out var IsEqual) && IsEqual);
						foreach (var (Character, ExpectedSpace) in new[] {
								('\t', true),
								('\n', true),
								('\v', true),
								('\f', true),
								('\r', true),
								(' ', true),
								('\0', false),
								('\u007f', false),
								('A', false)
							}
						
						) {
							var ActualSpace = Libraries.Data.Field("Char").Field("...IsSpace").Call(
								mVM_Data.Prefix(
									"_Char...",
									mVM_Data.Int(
										Character
									)
								)
							);
							mAssert.IsTrue(
								ActualSpace.IsBool(out var Space) && Space == ExpectedSpace,
								"ASCII whitespace."
							);
						}
						
						var TextExamples = mSPO_Interpreter.Run(
							"""
							§IMPORT {
								Std: {...-...: §DEF ...-...} € [§LOAD "../Libs/Std.SIG"]
								Text: {
									...AsDecimalText: §DEF ...AsDecimalText
									...Repeated...: §DEF ...Repeated...
									...FollowedBy...: §DEF ...FollowedBy...
									...Length: §DEF ...Length
								} € [§LOAD "../Libs/Text.SIG"]
							}
							§EXPORT (
								0 .AsDecimalText
								12345 .AsDecimalText
								-42 .AsDecimalText
								(-2147483647 .- 1) .AsDecimalText
								2147483647 .AsDecimalText
								("ab" .Repeated 3) .FollowedBy "!"
								"ab" .Repeated 0
								"ab" .Repeated -1
								"" .Repeated 4
								("abc" .Repeated 3) .Length
							)
							""",
							mStd.File(),
							Libraries,
							_ => {}
						).ElseThrow();
						var ExpectedText = mSPO_Interpreter.Run(
							"""
							§EXPORT ("0", "12345", "-42", "-2147483648", "2147483647", "ababab!", "", "", "", 9)
							""",
							mStd.File(),
							(mVM_Data.Empty(), mVM_Type.Empty()),
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(
							TextExamples.Data.Equals(ExpectedText.Data),
							"Pure text operations and integer boundaries."
						);
						var ConsumerPath = System.IO.Path.GetFullPath(
							System.IO.Path.Combine(
								System.IO.Path.GetDirectoryName(mStd.File())!,
								"../../SPO_CS/TestFiles/SPO_Modules/00_03_ListConsumer.SPO"
							)
						);
						var Actual = mSPO_Interpreter.Run(
							System.IO.File.ReadAllText(ConsumerPath).Replace("Push", "FollowedBy").Replace(
								"TryPop",
								"TrySplitLast"
							).Replace(
								"_Modules/",
								"../Libs/"
							),
							mStd.File(),
							Libraries,
							_ => {}
						).ElseThrow();
						var Expected = mSPO_Interpreter.Run(
							System.IO.File.ReadAllText(ConsumerPath.Replace(".SPO", ".result.SPO")),
							ConsumerPath,
							(mVM_Data.Empty(), mVM_Type.Empty()),
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(Actual.Data.Equals(Expected.Data), "Copied List module API");
						var MathPath = ConsumerPath.Replace("00_03_ListConsumer.SPO", "00_04_MathConsumer.SPO");
						var MathActual = mSPO_Interpreter.Run(
							System.IO.File.ReadAllText(MathPath).Replace("_Modules/", "../Libs/"),
							mStd.File(),
							Libraries,
							_ => {}
						).ElseThrow();
						var MathExpected = mSPO_Interpreter.Run(
							System.IO.File.ReadAllText(MathPath.Replace(".SPO", ".result.SPO")),
							MathPath,
							(mVM_Data.Empty(), mVM_Type.Empty()),
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(
							MathActual.Data.Equals(MathExpected.Data),
							"Copied Math module and range boundaries"
						);
					}
					
					{
						mVM_Data.tData
						Point(
							tInt32 aX,
							tInt32 aY
						) => mVM_Data.Tuple([mVM_Data.Int(aX), mVM_Data.Int(aY)]);
						
						var A = Point(3, -4);
						var B = Point(-2, 5);
						foreach (var (Operation, Argument, Expected) in new (tText, mVM_Data.tData, mVM_Data.tData)[] {
								("...X", A, mVM_Data.Int(3)),
								("...Y", A, mVM_Data.Int(-4)),
								("-...", A, Point(-3, 4)),
								("...+...", mVM_Data.Tuple([A, B]), Point(1, 1)),
								("...-...", mVM_Data.Tuple([A, B]), Point(5, -9)),
								("...*...", mVM_Data.Tuple([A, mVM_Data.Int(-2)]), Point(-6, 8)),
								("...*...", mVM_Data.Tuple([A, mVM_Data.Int(0)]), Point(0, 0)),
								(".../...", mVM_Data.Tuple([A, mVM_Data.Int(2)]), Point(1, -2)),
								(".../...", mVM_Data.Tuple([Point(-3, 5), mVM_Data.Int(-2)]), Point(1, -2)),
								("...==...", mVM_Data.Tuple([A, Point(3, -4)]), mVM_Data.Bool(true)),
								("...==...", mVM_Data.Tuple([A, Point(3, 4)]), mVM_Data.Bool(false)),
								("...==...", mVM_Data.Tuple([A, Point(-3, -4)]), mVM_Data.Bool(false)),
								("...!=...", mVM_Data.Tuple([A, B]), mVM_Data.Bool(true)),
								("...!=...", mVM_Data.Tuple([A, A]), mVM_Data.Bool(false)),
								("...Dot...", mVM_Data.Tuple([A, B]), mVM_Data.Int(-26)),
								("...Dot...", mVM_Data.Tuple([A, Point(4, 3)]), mVM_Data.Int(0))
							}
						
						) {
							mAssert.IsTrue(
								Libraries.Data.Field("Vector2").Field(Operation).Call(Argument).Equals(Expected),
								"Vector2 " + Operation
							);
						}
					}
					
					mAssert.IsTrue(
						aCapability._IsMutable,
						"The interface carries authority to its hidden mutable receiver."
					);
					SavedCapability = aCapability;
					var Size = aCapability.CallMethod("Size", mVM_Data.Empty());
					mAssert.AreEquals(80, Int(Size.Field("Width")));
					mAssert.AreEquals(25, Int(Size.Field("Height")));
					{
						var CursorTest = mSPO_Interpreter.Run(
							"""
							§EXPORT (§DEF aTerminal € [§LOAD "../../Libs/Terminal.SIG"]) : () {
								aTerminal :
									ShowCursor
									MoveCursorTo (5, 3)
									IsCursorVisible => §DEF Shown
									HideCursor
									MoveCursorTo (8, 4)
									IsCursorVisible => §DEF Hidden
								.
								§RETURN (Shown, Hidden)
							}
							""",
							Path,
							(mVM_Data.Empty(), mVM_Type.Empty()),
							_ => {}
						).ElseThrow();
						mAssert.IsTrue(
							CursorTest.Data.Call(mVM_Data.Empty(), aCapability).Equals(
								mVM_Data.Tuple(
									[
										mVM_Data.Bool(
											true
										),
										mVM_Data.Bool(
											false
										)
									]
								)
							)
						);
						mAssert.AreEquals(18, X);
						mAssert.AreEquals(24, Y);
						mAssert.IsFalse(CursorVisible);
					}
					
					{
						var Imports = """
						§IMPORT {
							Style: {
								DefaultStyle: §DEF cDefaultStyle
								...WithForeground...: §DEF ...WithForeground...
								...WithBackground...: §DEF ...WithBackground...
								...WithBlink...: §DEF ...WithBlink...
								...WithUnderline...: §DEF ...WithUnderline...
								...WithInverse...: §DEF ...WithInverse...
							} € [§LOAD "../../Libs/Style.SIG"]
						}
						""" +
						"\n";
						var Styles = mSPO_Interpreter.Run(
							Imports +
							"""
							§EXPORT () => {
								§DEF cUnderlineStyle = cDefaultStyle .WithUnderline §TRUE
								§DEF cColored = (cUnderlineStyle .WithForeground (#Green)) .WithBackground (#DarkBlue)
								§DEF cOldStyle = (cColored .WithBlink §TRUE) .WithInverse §TRUE
								§DEF cNewStyle = cOldStyle .WithBlink §FALSE
								§DEF cNoUnderline = cNewStyle .WithUnderline §FALSE
								§DEF cNoInverse = cNewStyle .WithInverse §FALSE
								§DEF cRecolored = (cNewStyle .WithForeground (#Red)) .WithBackground (#Yellow)
								§DEF Draw = (§DEF aTerminal € [§LOAD "../../Libs/Terminal.SIG"]) : (
									§DEF aStyle € [§LOAD "../../Libs/TextStyle.SIG"]
								) {
									aTerminal : Draw "Bla" At (0, 0) WithStyle aStyle .
								}

								§RETURN {
									Draw: Draw
									DefaultStyle: cDefaultStyle
									UnderlineStyle: cUnderlineStyle
									OldStyle: cOldStyle
									NewStyle: cNewStyle
									NoUnderline: cNoUnderline
									NoInverse: cNoInverse
									Recolored: cRecolored
								}
							}
							""",
							Path,
							Libraries,
							_ => {}
						).ElseThrow().Data.Call(mVM_Data.Empty());
						Styles.Field("Draw").Call(Styles.Field("NewStyle"), aCapability);
						var Expected = new mTerminal.tStyle {
							Foreground = mTerminal.tColor.Green,
							Background = mTerminal.tColor.DarkBlue,
							Underline = true,
							Inverse = true
						};
						mAssert.AreEquals(Expected, LastStyle);
						foreach (var (Name, Style) in new (tText, mTerminal.tStyle)[] {
								("DefaultStyle", new() { Foreground = mTerminal.tColor.Gray }),
								(
									"UnderlineStyle",
									new() {
										Foreground = mTerminal.tColor.Gray,
										Underline = true
									}
								),
								("OldStyle", Expected with { Blink = true }),
								("NewStyle", Expected),
								("NoUnderline", Expected with { Underline = false }),
								("NoInverse", Expected with { Inverse = false }),
								(
									"Recolored",
									Expected with {
										Foreground = mTerminal.tColor.Red,
										Background = mTerminal.tColor.Yellow
									}
								)
							}
						
						) {
							var Actual = Styles.Field(Name);
							mAssert.IsTrue(
								Actual.Field("Foreground").IsPrefix("_" + Style.Foreground, out var FG) &&
								FG.IsEmpty()
							);
							mAssert.IsTrue(
								Actual.Field("Background").IsPrefix("_" + Style.Background, out var BG) &&
								BG.IsEmpty()
							);
							mAssert.IsTrue(Actual.Field("Blink").IsBool(out var Blink) && Blink == Style.Blink);
							mAssert.IsTrue(
								Actual.Field("Underline").IsBool(out var Underline) &&
								Underline == Style.Underline
							);
							mAssert.IsTrue(
								Actual.Field("Inverse").IsBool(out var Inverse) &&
								Inverse == Style.Inverse
							);
						}
						
						var ColorType = mTerminal.LoadSignature("Color");
						var OperationsStyle = Libraries.Data.Field("Style");
						foreach (var Color in System.Enum.GetValues<mTerminal.tColor>()) {
							var Value = mVM_Data.Prefix("_" + Color, mVM_Data.Empty());
							mAssert.IsTrue(
								mVM_Type.Prefix("_" + Color, mVM_Type.Empty()).IsSubType(ColorType).Match(
									out _,
									out _
								)
							);
							var Styled = OperationsStyle.Field("...WithForeground...").Call(
								mVM_Data.Tuple(
									[
										Styles.Field(
											"NewStyle"
										),
										Value
									]
								)
							);
							Styled = OperationsStyle.Field("...WithBackground...").Call(
								mVM_Data.Tuple(
									[
										Styled,
										Value
									]
								)
							);
							aCapability.CallMethod(
								"Draw...At...WithStyle...",
								mVM_Data.Tuple(
									[
										mVM_Data.Pair(
											mVM_Data.Empty(),
											mVM_Data.Prefix(
												"_Char...",
												mVM_Data.Int(
													'x'
												)
											)
										),
										mVM_Data.Tuple([mVM_Data.Int(0), mVM_Data.Int(0)]),
										Styled
									]
								)
							);
							mAssert.AreEquals(Expected with { Foreground = Color, Background = Color }, LastStyle);
						}
						
						{
							var Text = mSPO_Interpreter.Run(
								"§EXPORT \"ABC\"",
								Path,
								(mVM_Data.Empty(), mVM_Type.Empty()),
								_ => {}
							).ElseThrow().Data;
							
							void
							DrawAt(
								tInt32 aX,
								tInt32 aY
							) => aCapability.CallMethod(
								"Draw...At...WithStyle...",
								mVM_Data.Tuple(
									[
										Text,
										mVM_Data.Tuple([mVM_Data.Int(aX), mVM_Data.Int(aY)]),
										Styles.Field("DefaultStyle")
									]
								)
							);
							
							System.Array.Fill(Screen, '.');
							DrawAt(-1, 0);
							mAssert.AreEquals('B', Screen[20 * 100 + 10]);
							mAssert.AreEquals('C', Screen[20 * 100 + 11]);
							mAssert.AreEquals('.', Screen[20 * 100 + 9]);
							DrawAt(79, 24);
							mAssert.AreEquals('A', Screen[44 * 100 + 89]);
							mAssert.AreEquals('.', Screen[44 * 100 + 90]);
							var BeforeOffscreen = new tText(Screen);
							foreach (var (Column, Row) in new[] {
									(tInt32.MinValue, 0),
									(tInt32.MaxValue, 0),
									(0, -1),
									(0, 25),
									(80, 0)
								}
							
							) {
								DrawAt(Column, Row);
							}
							
							mAssert.AreEquals(BeforeOffscreen, new tText(Screen));
							mAssert.AreEquals(18, X); // Drawing is independent of the cursor.
							mAssert.AreEquals(24, Y);
						}
						
						foreach (var Invalid in new[] {
								"aTerminal : Draw \"Bla\" At (0, 0) WithStyle 3 .",
								"aTerminal : Draw \"Bla\" At (0, 0) WithStyle (cDefaultStyle .WithForeground 10) .",
								(
									"aTerminal : Draw \"Bla\" At (0, 0) WithStyle (cDefaultStyle .WithForeground " +
									"(#Orange ())) ."
								),
								(
									"aTerminal : Draw \"Bla\" At (0, 0) WithStyle (cDefaultStyle .WithBackground " +
									"(#Black 1)) ."
								),
								"aTerminal : Draw \"Bla\" At (0, 0) WithStyle (cDefaultStyle .WithBlink 1) ."
							}
						
						) {
							mAssert.IsFalse(
								mSPO_Interpreter.Run(
									Imports +
									"""
									§EXPORT (§DEF aTerminal € [§LOAD "../../Libs/Terminal.SIG"]) : () {
									""" +
									"\n" +
									Invalid +
									"\n§RETURN ()\n}",
									Path,
									Libraries,
									_ => {}
								).Match(out _, out _),
								Invalid
							);
						}
					}
					
					var App = Startup.Entry.Init(
						_ => {}
					).ElseThrow();
					{
						var Imports = Startup.Modules.Where(aModule => aModule.Key is "List" or "Render").Init(
							_ => {}
						).ElseThrow();
						var Code = """
						§IMPORT {
							List: §SIG [§LOAD "../../Libs/List.SIG"] WITH [§LOAD "../../Libs/ListType.SIG"] IN {
								List...: §DEF List...
							}
							Render: {
								DrawGame...: §DEF DrawGame...
							} € [§LOAD "Render.SIG"]
						}
						§DEF cState € [§LOAD "State.SIG"] = {
							SnakeBody: .List (2, 2)
							MoveDirection: #Right
							PendingDirection: #Right
							FoodPosition: (4, 4)
							RandomState: 1
							Phase: #Paused
						}
						§EXPORT (§DEF aTerminal € [§LOAD "../../Libs/Terminal.SIG"]) : () {
							aTerminal : DrawGame cState .
						}
						""";
						mSPO_Interpreter.Run(
							Code,
							Path,
							Imports,
							_ => {}
						).ElseThrow().Data.Call(mVM_Data.Empty(), aCapability);
						mAssert.AreEquals('@', Screen[25 * 100 + 12]);
						mAssert.IsFalse(
							mSPO_Interpreter.Run(
								Code.Replace(".List (2, 2)", ".List 2"),
								Path,
								Imports,
								_ => {}
							).Match(out _, out _),
							"The shared SnakeBody type requires positions, not integers."
						);
					}
					
					mAssert.IsTrue(App.Data.IsSig(out _, out _, out var Body));
					var Initial = Body.Field("Initial");
					var InitialRandomState = Initial.Field("RandomState");
					var State = Initial;
					var Frame = Body.Field("Frame...");
					var Tick = mVM_Data.Prefix("_Tick...", mVM_Data.Int(140));
					
					mVM_Data.tData
					Modifiers(
						tBool aControl = false
					) => mVM_Data.Record(
						[
							("_Shift", mVM_Data.Bool(false)),
							("_Alt", mVM_Data.Bool(false)),
							("_Control", mVM_Data.Bool(aControl)),
							("_CapsLock", mVM_Data.Bool(false)),
							("_NumLock", mVM_Data.Bool(false)),
							("_ScrollLock", mVM_Data.Bool(false))
						]
					);
					
					mVM_Data.tData
					Key(
						tText aKey,
						tBool aDown = true,
						tBool aControl = false,
						tBool aIsRepeat = false
					) => (
						mVM_Data.Prefix(
							"_Key...",
							mVM_Data.Record(
								[
									(
										"_Key",
										(
											aKey.Length == 1
											? mVM_Data.Prefix(
												"_Character...",
												mVM_Data.Prefix("_Char...", mVM_Data.Int(aKey[0]))
											)
											: mVM_Data.Prefix("_" + aKey, mVM_Data.Empty())
										)
									),
									("_Character", mVM_Data.Prefix("_Char...", mVM_Data.Int(0))),
									("_IsDown", mVM_Data.Bool(aDown)),
									("_RepeatCount", mVM_Data.Int(1)),
									("_IsRepeat", mVM_Data.Bool(aIsRepeat)),
									("_Modifiers", Modifiers(aControl))
								]
							)
						)
					);
					
					mVM_Data.tData
					Buttons(
						tBool aLeft = false
					) => mVM_Data.Record(
						[
							("_Left", mVM_Data.Bool(aLeft)),
							("_Right", mVM_Data.Bool(false)),
							("_Middle", mVM_Data.Bool(false)),
							("_Extra1", mVM_Data.Bool(false)),
							("_Extra2", mVM_Data.Bool(false))
						]
					);
					
					mVM_Data.tData
					Mouse(
						tBool aLeft = false,
						tBool aPressedLeft = false
					) => (
						mVM_Data.Prefix(
							"_Mouse...",
							mVM_Data.Record(
								[
									("_Position", mVM_Data.Tuple([mVM_Data.Int(2), mVM_Data.Int(3)])),
									("_Buttons", Buttons(aLeft)),
									("_PressedButtons", Buttons(aPressedLeft)),
									("_ReleasedButtons", Buttons()),
									("_WheelDelta", mVM_Data.Int(0)),
									("_IsHorizontalWheel", mVM_Data.Bool(false)),
									("_HasMoved", mVM_Data.Bool(false)),
									("_IsDoubleClick", mVM_Data.Bool(false)),
									("_Modifiers", Modifiers())
								]
							)
						)
					);
					
					tBool
					Send(
						mVM_Data.tData aEvent
					) {
						var Result = Frame.Call(mVM_Data.Tuple([State, aEvent]), aCapability);
						mAssert.IsTrue(Result.IsTuple(out State, out var Continue));
						mAssert.IsTrue(Continue.IsBool(out var Running));
						return Running;
					}
					
					void
					Head(
						tInt32 aX,
						tInt32 aY
					) {
						mAssert.IsTrue(State.Field("SnakeBody").IsPair(out _, out var Point));
						mAssert.IsTrue(Point.IsTuple(out var PX, out var PY));
						mAssert.AreEquals(aX, Int(PX));
						mAssert.AreEquals(aY, Int(PY));
					}
					
					tInt32
					Length() {
						var Items = State.Field("SnakeBody");
						var Count = 0;
						while (Items.IsPair(out Items, out _)) {
							Count += 1;
						}
						
						return Count;
					}
					
					CursorVisible = true;
					Send(mVM_Data.Prefix("_Resize...", Dimensions.ToData()));
					mAssert.IsFalse(CursorVisible);
					mAssert.AreEquals('@', Screen[31 * 100 + 17]);
					mAssert.AreEquals('*', Screen[31 * 100 + 30]);
					mAssert.AreEquals('+', Screen[23 * 100 + 10]);
					mAssert.AreEquals('+', Screen[23 * 100 + 39]);
					mAssert.AreEquals('+', Screen[40 * 100 + 10]);
					mAssert.AreEquals('+', Screen[40 * 100 + 39]);
					mAssert.AreEquals('|', Screen[24 * 100 + 10]);
					mAssert.AreEquals('|', Screen[24 * 100 + 39]);
					mAssert.IsTrue(SawBlink && SawGreen);
					Send(Key("ArrowLeft")); // Reversing into the neck is ignored.
					Send(Tick);
					Head(8, 8);
					Send(Key("ArrowUp"));
					Send(Key("ArrowLeft")); // A second key before the tick cannot enable reversal.
					Send(Tick);
					Head(8, 7);
					Send(Key("Space"));
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Paused", out _));
					Send(Tick);
					Head(8, 7);
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Paused", out _));
					Send(Key("Space", false));
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Paused", out _));
					Send(Key("Space", aIsRepeat: true));
					Send(Mouse(aLeft: true)); // Holding a mouse button is not another press.
					Send(Mouse());
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Paused", out _));
					Send(Mouse(aLeft: true, aPressedLeft: true));
					Send(Tick);
					Head(8, 6);
					Send(mVM_Data.Prefix("_Focus...", mVM_Data.Bool(false)));
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Paused", out _));
					Send(Key("R"));
					mAssert.AreEquals(InitialRandomState, State.Field("RandomState"));
					for (var I = 0; I < 12; I += 1) {
						Send(Tick);
					}
					
					mAssert.IsTrue(
						InitialRandomState.Equals(State.Field("RandomState")),
						"Movement does not consume randomness."
					);
					Send(Tick);
					Head(20, 8);
					mAssert.AreEquals(4, Length());
					mAssert.AreEquals(mVM_Data.Int(16807), State.Field("RandomState"));
					mAssert.AreEquals(
						mVM_Data.Tuple([mVM_Data.Int(19), mVM_Data.Int(14)]),
						State.Field(
							"FoodPosition"
						)
					);
					mAssert.AreEquals('1', Screen[21 * 100 + 17]);
					mAssert.IsTrue(State.Field("SnakeBody").IsPair(out _, out var Eaten));
					mAssert.IsFalse(State.Field("FoodPosition").Equals(Eaten));
					for (var I = 0; I < 9; I += 1) {
						Send(Tick);
					}
					
					Head(28, 8);
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_GameOver", out _));
					Send(Key("R"));
					mAssert.IsTrue(
						InitialRandomState.Equals(State.Field("RandomState")),
						"Restart restores the initial sequence."
					);
					Dimensions = new((100, 70), (10, 20), (30, 2));
					Send(mVM_Data.Prefix("_Resize...", Dimensions.ToData()));
					Send(Tick);
					Head(7, 8);
					Dimensions = new((100, 70), (10, 20), (80, 25));
					Send(mVM_Data.Prefix("_Resize...", Dimensions.ToData()));
					Send(Tick);
					Head(8, 8);
					State = Initial;
					foreach (var (Code, ExpectedX, ExpectedY) in new[] {
							("W", 7, 7),
							("A", 6, 7),
							("S", 6, 8),
							("D", 7, 8)
						}
					
					) {
						Send(Key(Code));
						Send(Tick);
						Head(ExpectedX, ExpectedY);
					}
					
					var BeforeIgnoredKey = State;
					Send(Key("F"));
					mAssert.AreEquals(BeforeIgnoredKey.ToText(1000), State.ToText(1000));
					mAssert.IsTrue(Initial.Field("MoveDirection").IsPrefix("_Right", out _));
					mAssert.IsTrue(Initial.Field("SnakeBody").IsPair(out _, out var InitialHead));
					mAssert.IsTrue(InitialHead.Equals(mVM_Data.Tuple([mVM_Data.Int(7), mVM_Data.Int(8)])));
					
					mVM_Data.tData
					MakeState(
						(tInt32 X, tInt32 Y)[] aPoints,
						(tInt32 X, tInt32 Y) aFood,
						tInt32 aRandomState = 1
					) {
						var Items = mVM_Data.Empty();
						foreach (var Point in aPoints) {
							Items = mVM_Data.Pair(
								Items,
								mVM_Data.Tuple(
									[
										mVM_Data.Int(Point.X),
										mVM_Data.Int(
											Point.Y
										)
									]
								)
							);
						}
						
						var Direction = mVM_Data.Prefix("_Left", mVM_Data.Empty());
						return mVM_Data.Record(
							[
								("_SnakeBody", Items),
								("_MoveDirection", Direction),
								("_PendingDirection", Direction),
								(
									"_FoodPosition",
									mVM_Data.Tuple([mVM_Data.Int(aFood.X), mVM_Data.Int(aFood.Y)])
								),
								("_RandomState", mVM_Data.Int(aRandomState)),
								("_Phase", mVM_Data.Prefix("_Playing", mVM_Data.Empty()))
							]
						);
					}
					
					State = MakeState([(2, 2), (2, 3), (3, 3), (3, 2)], (20, 8));
					Send(Tick);
					Head(2, 2); // The tail vacates this square in the same tick.
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Playing", out _));
					State = MakeState([(1, 2), (2, 2), (2, 3), (3, 3), (3, 2)], (20, 8));
					Send(Tick);
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_GameOver", out _));
					mAssert.IsTrue(
						InitialRandomState.Equals(State.Field("RandomState")),
						"A collision does not consume randomness."
					);
					{
						var Logic = Startup.Modules.Where(aModule => aModule.Key == "Logic").Init(
							_ => {}
						).ElseThrow();
						var After = Logic.Data.Field("Logic").Field("...After...");
						var Advance = mVM_Data.Prefix("_Advance", mVM_Data.Empty());
						
						mVM_Data.tData
						Move(
							mVM_Data.tData aState
						) => After.Call(mVM_Data.Tuple([aState, Advance]));
						
						var BeforeEating = MakeState([(3, 2), (2, 2)], (1, 2));
						var First = Move(BeforeEating);
						mAssert.AreEquals(First.ToText(1000), Move(BeforeEating).ToText(1000));
						mAssert.AreEquals(mVM_Data.Int(1), BeforeEating.Field("RandomState"));
						mAssert.IsFalse(
							First.Field("FoodPosition").Equals(
								Move(MakeState([(3, 2), (2, 2)], (1, 2), 2)).Field(
									"FoodPosition"
								)
							),
							"Different generator states select different cells in this fixture."
						);
						foreach (var Seed in new[] {
								tInt32.MinValue,
								-1,
								0,
								1,
								2,
								127773,
								tInt32.MaxValue
							}
						
						) {
							var Next = Move(MakeState([(3, 2), (2, 2)], (1, 2), Seed));
							var Food = Next.Field("FoodPosition");
							mAssert.IsTrue(Food.IsTuple(out var FoodX, out var FoodY));
							mAssert.IsTrue(Int(FoodX) >= 1 && Int(FoodX) <= 28);
							mAssert.IsTrue(Int(FoodY) >= 1 && Int(FoodY) <= 16);
							var Items = Next.Field("SnakeBody");
							while (Items.IsPair(out Items, out var Point)) {
								mAssert.IsFalse(Food.Equals(Point), "The apple must be on a free cell.");
							}
						}
						
						var SeededState = MakeState([(3, 2), (2, 2)], (20, 8), 16807);
						foreach (var Action in new[] {
								Advance,
								mVM_Data.Prefix("_Turn...", mVM_Data.Prefix("_Up", mVM_Data.Empty())),
								mVM_Data.Prefix("_TogglePause", mVM_Data.Empty()),
								mVM_Data.Prefix("_Pause", mVM_Data.Empty())
							}
						
						) {
							var Next = After.Call(mVM_Data.Tuple([SeededState, Action]));
							mAssert.AreEquals(SeededState.Field("RandomState"), Next.Field("RandomState"));
						}
						
						System.Console.WriteLine("OK: random apple selection, replay and state preservation.");
						var Points = new (tInt32 X, tInt32 Y)[446];
						var PointIndex = 0;
						for (var Row = 1; Row <= 16; Row += 1) {
							for (var Column = 1; Column <= 28; Column += 1) {
								if ((Row != 1 || Column > 2) && (Row != 16 || Column != 28)) {
									Points[PointIndex++] = (Column, Row);
								}
							}
						}
						
						Points[PointIndex] = (2, 1);
						var AlmostWon = Move(MakeState(Points, (1, 1), 16807));
						mAssert.AreEquals(
							mVM_Data.Tuple([mVM_Data.Int(28), mVM_Data.Int(16)]),
							AlmostWon.Field("FoodPosition")
						);
						mAssert.AreEquals(mVM_Data.Int(282475249), AlmostWon.Field("RandomState"));
						mAssert.IsTrue(AlmostWon.Field("Phase").IsPrefix("_Playing", out _));
						System.Console.WriteLine("OK: random apple on the last free cell.");
					}
					
					var AlmostFull = new (tInt32 X, tInt32 Y)[447];
					var Index = 0;
					for (var Row = 1; Row <= 16; Row += 1) {
						for (var Column = 1; Column <= 28; Column += 1) {
							if (Row != 1 || Column > 2) {
								AlmostFull[Index++] = (Column, Row);
							}
						}
					}
					
					AlmostFull[Index] = (2, 1);
					State = MakeState(AlmostFull, (1, 1));
					Send(Tick);
					Head(1, 1);
					mAssert.AreEquals(448, Length());
					mAssert.IsTrue(State.Field("Phase").IsPrefix("_Won", out _));
					mAssert.IsTrue(
						InitialRandomState.Equals(State.Field("RandomState")),
						"A full board needs no random choice."
					);
					mAssert.AreEquals(
						"445",
						new tText(
							[
								Screen[21 * 100 + 17],
								Screen[21 * 100 + 18],
								Screen[
									21 * 100 +
									19
								]
							]
						)
					);
					mAssert.IsFalse(Send(Key("Escape")));
					mAssert.IsFalse(Send(Key("Q")));
					mAssert.IsFalse(Send(Key("C", aControl: true)));
					mAssert.IsTrue(Send(Key("C")));
					// A copied interface retains authority; a different hidden receiver does not.
					mAssert.IsTrue(aCapability.IsInterface(out var Contract, out _, out var Methods));
					var Forged = mVM_Data.Interface(Contract, mVM_Data.Var(mVM_Data.Empty()), Methods);
					foreach (var Name in new[] {
							"Clear",
							"IsCursorVisible",
							"ShowCursor",
							"HideCursor"
						}
					
					) {
						try {
							Forged.CallMethod(Name, mVM_Data.Empty());
							mAssert.Fail("Forged receiver was accepted.");
						} catch (System.InvalidOperationException) {}
					}
				}
			);
			foreach (var Name in new[] {
					"Clear",
					"IsCursorVisible",
					"ShowCursor",
					"HideCursor"
				}
			
			) {
				try {
					SavedCapability!.CallMethod(Name, mVM_Data.Empty());
					mAssert.Fail("Expired capability was accepted.");
				} catch (System.InvalidOperationException) {}
			}
			
			System.Console.WriteLine(
				("OK: file-module startup and explicit Frame capabilities, " + "Char/Text/List/Math/Random, ") +
				"Vector2, Snake gameplay/replay, " +
				"cursor position/visibility, size pairs, input, colors/blink and capability lifetime."
			);
			return 0;
		}
	);
}