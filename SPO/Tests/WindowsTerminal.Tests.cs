#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../Runtime/Terminal/mWindowsTerminal.cs
#:ref ../src/Program.cs

public static class
mWindowsTerminal_Tests {
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tCoord {
		public tInt16 X, Y;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tRect {
		public tInt16 Left, Top, Right, Bottom;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tScreenInfo {
		public tCoord Size, Cursor;
		public tNat16 Attributes;
		public tRect Window;
		public tCoord MaximumWindowSize;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tCursorInfo {
		public tNat32 Size;
		public tInt32 Visible;
	}
	
	[StructLayout(LayoutKind.Explicit, Size = 20)]
	private struct
	tInput {
		[FieldOffset(0)]
		public tNat16 Type;
		[FieldOffset(4)]
		public tInt32 Down;
		[FieldOffset(8)]
		public tNat16 Repeat;
		[FieldOffset(10)]
		public tNat16 Key;
		[FieldOffset(14)]
		public tNat16 Character;
		[FieldOffset(16)]
		public tNat32 KeyModifiers;
		[FieldOffset(4)]
		public tCoord Position;
		[FieldOffset(8)]
		public tNat32 Buttons;
		[FieldOffset(12)]
		public tNat32 MouseModifiers;
		[FieldOffset(16)]
		public tNat32 MouseFlags;
	}
	
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern tCPtr
	CreateFileW(
		tText aName,
		tNat32 aAccess,
		tNat32 aShare,
		tCPtr aSecurity,
		tNat32 aCreation,
		tNat32 aFlags,
		tCPtr aTemplate
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	CloseHandle(
		tCPtr aHandle
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	GetConsoleMode(
		tCPtr aHandle,
		out tNat32 aMode
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	GetConsoleCursorInfo(
		tCPtr aHandle,
		out tCursorInfo aInfo
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	SetConsoleCursorInfo(
		tCPtr aHandle,
		in tCursorInfo aInfo
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	FlushConsoleInputBuffer(
		tCPtr aHandle
	);
	
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern tBool
	WriteConsoleInputW(
		tCPtr aHandle,
		in tInput aInput,
		tNat32 aLength,
		out tNat32 aWritten
	);
	
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern tBool
	ReadConsoleOutputCharacterW(
		tCPtr aHandle,
		[Out] tChar[] aText,
		tNat32 aLength,
		tCoord aPosition,
		out tNat32 aRead
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	ReadConsoleOutputAttribute(
		tCPtr aHandle,
		[Out] tNat16[] aAttributes,
		tNat32 aLength,
		tCoord aPosition,
		out tNat32 aRead
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	GetConsoleScreenBufferInfo(
		tCPtr aHandle,
		out tScreenInfo aInfo
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tBool
	SetConsoleScreenBufferSize(
		tCPtr aHandle,
		tCoord aSize
	);
	
	private static void
	Check(
		tBool aSuccess
	) {
		if (!aSuccess) {
			throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
		}
	}
	
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
			var Input = CreateFileW("CONIN$", 0xC0000000, 3, 0, 3, 0, 0);
			Check(Input != -1);
			Check(GetConsoleMode(Input, out var OriginalMode));
			mTerminal.tTerminal? SavedTerminal = null;
			try {
				mWindowsTerminal.Run(
					Terminal => {
						SavedTerminal = Terminal;
						var Output = CreateFileW("CONOUT$", 0xC0000000, 3, 0, 3, 0, 0);
						Check(Output != -1);
						try {
							tChar
							Character(
								tInt16 aX = 2,
								tInt16 aY = 2
							) {
								var Text = new tChar[1];
								Check(
									ReadConsoleOutputCharacterW(
										Output,
										Text,
										1,
										new() {
											X = aX,
											Y = aY
										},
										out var Read
									)
								);
								mAssert.AreEquals(1u, Read);
								return Text[0];
							}
							
							mVM_Data.tData
							Send(
								tInput aInput,
								tText aPrefix
							) {
								Check(FlushConsoleInputBuffer(Input));
								Check(WriteConsoleInputW(Input, aInput, 1, out var Written));
								mAssert.AreEquals(1u, Written);
								mAssert.IsTrue(Terminal.ReadEvent(100).IsPrefix(aPrefix, out var Body));
								return Body;
							}
							
							Terminal.Clear();
							Terminal.Present();
							// Query native changes and preserve cursor size when changing visibility.
							Check(
								SetConsoleCursorInfo(
									Output,
									new() {
										Size = 65,
										Visible = 1
									}
								)
							);
							mAssert.IsTrue(Terminal.CursorVisible());
							Terminal.Cursor(2, 2);
							mAssert.IsTrue(Terminal.CursorVisible());
							Terminal.SetCursorVisible(false);
							mAssert.IsFalse(Terminal.CursorVisible());
							Check(GetConsoleCursorInfo(Output, out var Cursor));
							mAssert.AreEquals(65u, Cursor.Size);
							mAssert.AreEquals(0, Cursor.Visible);
							Terminal.Draw(
								"A\u001b",
								(2, 2),
								new() {
									Foreground = mTerminal.tColor.Green,
									Background = mTerminal.tColor.DarkBlue,
									Underline = true,
									Inverse = true
								}
							);
							Terminal.Cursor(4, 3);
							Terminal.Draw("B", (4, 3), new() { Foreground = mTerminal.tColor.Yellow });
							// Text and cursor changes remain private until the complete frame is presented.
							mAssert.AreEquals(' ', Character());
							mAssert.AreEquals(' ', Character(4, 3));
							Check(GetConsoleScreenBufferInfo(Output, out var ScreenInfo));
							mAssert.AreEquals((tInt16)0, ScreenInfo.Cursor.X);
							mAssert.AreEquals((tInt16)0, ScreenInfo.Cursor.Y);
							Terminal.Present();
							mAssert.AreEquals('A', Character());
							mAssert.AreEquals('B', Character(4, 3));
							Check(GetConsoleScreenBufferInfo(Output, out ScreenInfo));
							mAssert.AreEquals((tInt16)4, ScreenInfo.Cursor.X);
							mAssert.AreEquals((tInt16)3, ScreenInfo.Cursor.Y);
							var Attributes = new tNat16[2];
							Check(
								ReadConsoleOutputAttribute(
									Output,
									Attributes,
									2,
									new() {
										X = 2,
										Y = 2
									},
									out _
								)
							);
							mAssert.AreEquals((tNat16)0x80A1, Attributes[0]);
							var Text = new tChar[2];
							Check(
								ReadConsoleOutputCharacterW(
									Output,
									Text,
									2,
									new() {
										X = 2,
										Y = 2
									},
									out _
								)
							);
							mAssert.AreEquals(' ', Text[1]);
							// Clearing and redrawing must not expose the empty intermediate frame.
							Terminal.Clear();
							mAssert.AreEquals('A', Character());
							mAssert.AreEquals('B', Character(4, 3));
							Terminal.Cursor(2, 2);
							Terminal.Draw("C", (2, 2), new() { Foreground = mTerminal.tColor.Cyan });
							mAssert.AreEquals('A', Character());
							Terminal.Present();
							mAssert.AreEquals('C', Character());
							mAssert.AreEquals(' ', Character(4, 3));
							var Key = Send(
								new() {
									Type = 1,
									Down = 1,
									Key = 65,
									Character = 'A',
									Repeat = 3,
									KeyModifiers = 16 | 8
								},
								"_Key..."
							);
							mAssert.IsTrue(Key.Field("Key").IsPrefix("_Character...", out var Letter));
							mAssert.IsTrue(Letter.IsPrefix("_Char...", out var Ordinal));
							mAssert.AreEquals((tInt32)'A', Int(Ordinal));
							mAssert.AreEquals(3, Int(Key.Field("RepeatCount")));
							mAssert.IsTrue(
								Key.Field("Modifiers").Field("Control").IsBool(out var Control) &&
								Control
							);
							mAssert.IsTrue(Key.Field("Modifiers").Field("Shift").IsBool(out var Shift) && Shift);
							mAssert.IsTrue(Key.Field("IsDown").IsBool(out var Down) && Down);
							mAssert.IsTrue(Key.Field("IsRepeat").IsBool(out var IsRepeat) && !IsRepeat);
							Key = Send(
								new() {
									Type = 1,
									Down = 1,
									Key = 65,
									Repeat = 1
								},
								"_Key..."
							);
							mAssert.IsTrue(Key.Field("IsRepeat").IsBool(out IsRepeat) && IsRepeat);
							Key = Send(
								new() {
									Type = 1,
									Down = 0,
									Key = 65,
									Repeat = 1
								},
								"_Key..."
							);
							mAssert.IsTrue(Key.Field("IsDown").IsBool(out Down) && !Down);
							var Mouse = Send(
								new() {
									Type = 2,
									Position = new() {
										X = 7,
										Y = 9
									},
									Buttons = 0xFF880001,
									MouseFlags = 8,
									MouseModifiers = 2
								},
								"_Mouse..."
							);
							mAssert.IsTrue(Mouse.Field("Position").IsTuple(out var MouseX, out var MouseY));
							mAssert.AreEquals(7 - Terminal.Size().WindowOrigin.X, Int(MouseX));
							mAssert.AreEquals(9 - Terminal.Size().WindowOrigin.Y, Int(MouseY));
							mAssert.IsTrue(Mouse.Field("Buttons").Field("Left").IsBool(out var Left) && Left);
							mAssert.IsTrue(
								Mouse.Field("PressedButtons").Field("Left").IsBool(out var Pressed) &&
								Pressed
							);
							mAssert.AreEquals(-120, Int(Mouse.Field("WheelDelta")));
							mAssert.IsTrue(Mouse.Field("Modifiers").Field("Alt").IsBool(out var Alt) && Alt);
							mAssert.IsTrue(
								Mouse.Field("IsHorizontalWheel").IsBool(out var Horizontal) && Horizontal
							);
							Mouse = Send(
								new() {
									Type = 2,
									Buttons = 1,
									MouseFlags = 1
								},
								"_Mouse..."
							);
							mAssert.IsTrue(
								Mouse.Field("PressedButtons").Field("Left").IsBool(out Pressed) &&
								!Pressed
							);
							Mouse = Send(new() { Type = 2 }, "_Mouse...");
							mAssert.IsTrue(
								Mouse.Field("ReleasedButtons").Field("Left").IsBool(out var Released) &&
								Released
							);
							Terminal.Cursor(2, 2);
							mAssert.IsFalse(Terminal.CursorVisible());
							Terminal.Draw(
								"*",
								(2, 2),
								new() {
									Foreground = mTerminal.tColor.Red,
									Blink = true
								}
							);
							var Clock = System.Diagnostics.Stopwatch.StartNew();
							var SawStar = false;
							var SawSpace = false;
							while (Clock.ElapsedMilliseconds < 1100) {
								Terminal.ReadEvent(25);
								SawStar |= Character() == '*';
								SawSpace |= Character() == ' ';
							}
							
							mAssert.IsTrue(SawStar && SawSpace);
							Terminal.Cursor(2, 2);
							Terminal.Draw("Z", (2, 2), new() { Foreground = mTerminal.tColor.Gray });
							Terminal.Present();
							Clock.Restart();
							while (Clock.ElapsedMilliseconds < 600) {
								Terminal.ReadEvent(25);
								mAssert.AreEquals('Z', Character());
							}
							
							var Size = Terminal.Size();
							Check(
								SetConsoleScreenBufferSize(
									Output,
									new() {
										X = checked((tInt16)(Size.BufferSize.X + 1)),
										Y = checked((tInt16)(Size.BufferSize.Y + 1))
									}
								)
							);
							mAssert.IsTrue(Terminal.ReadEvent(100).IsPrefix("_Resize...", out var Resized));
							mAssert.AreEquals(Terminal.Size().WindowSize.X, Int(Resized.Field("Width")));
							mAssert.AreEquals(Terminal.Size().WindowSize.Y, Int(Resized.Field("Height")));
							// Resize preserves coordinates despite the new row stride.
							mAssert.AreEquals('Z', Character());
							mAssert.AreEquals(' ', Character((tInt16)Size.BufferSize.X, (tInt16)Size.BufferSize.Y));
							Terminal.Cursor(Size.BufferSize.X, Size.BufferSize.Y);
							Terminal.Draw(
								"Gone",
								(Size.BufferSize.X, Size.BufferSize.Y),
								new() { Foreground = mTerminal.tColor.Green }
							);
							Terminal.Present();
							mAssert.AreEquals('G', Character((tInt16)Size.BufferSize.X, (tInt16)Size.BufferSize.Y));
							Terminal.Cursor(0, 0);
							Terminal.Present(); // Keep the native viewport inside the size we shrink to.
							Terminal.Cursor(Size.BufferSize.X, Size.BufferSize.Y);
							Check(
								SetConsoleScreenBufferSize(
									Output,
									new() {
										X = (tInt16)Size.BufferSize.X,
										Y = (tInt16)Size.BufferSize.Y
									}
								)
							);
							Terminal.Size();
							Terminal.Present(); // The virtual cursor is clamped to the smaller buffer.
							Check(
								SetConsoleScreenBufferSize(
									Output,
									new() {
										X = (tInt16)(Size.BufferSize.X + 1),
										Y = (tInt16)(Size.BufferSize.Y + 1)
									}
								)
							);
							Terminal.Present();
							mAssert.AreEquals('Z', Character());
							mAssert.AreEquals(' ', Character((tInt16)Size.BufferSize.X, (tInt16)Size.BufferSize.Y));
							Terminal.Cursor(5, 5);
							Terminal.Draw(
								"*",
								(5, 5),
								new() {
									Foreground = mTerminal.tColor.Red,
									Blink = true
								}
							);
							Terminal.Clear();
							mAssert.AreEquals('Z', Character());
							Terminal.Present();
							mAssert.AreEquals(' ', Character());
							Clock.Restart();
							while (Clock.ElapsedMilliseconds < 600) {
								Terminal.ReadEvent(25);
								mAssert.AreEquals(' ', Character()); // Clear also removes blink metadata.
								mAssert.AreEquals(' ', Character(5, 5));
							}
						} finally {
							CloseHandle(Output);
						}
					}
				);
				Check(GetConsoleMode(Input, out var RestoredMode));
				mAssert.AreEquals(OriginalMode, RestoredMode);
				// Exercise the complete host, SPO compiler and native backend together.
				const tInt32 cHostTimeoutMilliseconds = 300000;
				var LastHostLine = "";
				var Quit = System.Threading.Tasks.Task.Run(
					() => {
						var Clock = System.Diagnostics.Stopwatch.StartNew();
						var SawSnake = false;
						while (Clock.ElapsedMilliseconds < cHostTimeoutMilliseconds && !SawSnake) {
							var Output = CreateFileW("CONOUT$", 0xC0000000, 3, 0, 3, 0, 0);
							try {
								Check(GetConsoleScreenBufferInfo(Output, out var Info));
								var Line = new tChar[System.Math.Min(32, Info.Window.Right - Info.Window.Left + 1)];
								Check(
									ReadConsoleOutputCharacterW(
										Output,
										Line,
										(tNat32)Line.Length,
										new() {
											X = Info.Window.Left,
											Y = Info.Window.Top
										},
										out _
									)
								);
								LastHostLine = new tText(Line);
								SawSnake = LastHostLine.StartsWith("SNAKE in SPO") ||
								LastHostLine.StartsWith("Resize to at least ");
							} finally {
								CloseHandle(Output);
							}
							
							System.Threading.Thread.Sleep(25);
						}
						
						Check(
							WriteConsoleInputW(
								Input,
								new() {
									Type = 1,
									Down = 1,
									Key = 81,
									Character = 'q',
									Repeat = 1
								},
								1,
								out _
							)
						);
						return SawSnake;
					}
				);
				using (var App = System.Diagnostics.Process.Start(
						new System.Diagnostics.ProcessStartInfo {
							FileName = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Program.exe"),
							UseShellExecute = false,
							RedirectStandardOutput = true,
							RedirectStandardError = true
						}
					)!) {
					var StandardOutput = App.StandardOutput.ReadToEndAsync();
					var StandardError = App.StandardError.ReadToEndAsync();
					try {
						mAssert.IsTrue(App.WaitForExit(cHostTimeoutMilliseconds), "Snake host did not exit.");
						mAssert.IsTrue(App.ExitCode == 0, StandardError.GetAwaiter().GetResult());
						StandardOutput.GetAwaiter().GetResult();
					} finally {
						if (!App.HasExited) {
							App.Kill(entireProcessTree: true);
						}
					}
				}
				
				mAssert.IsTrue(
					Quit.GetAwaiter().GetResult(),
					"Snake did not draw its title/resize hint: " +
					LastHostLine
				);
				System.Console.WriteLine("Host frame: " + LastHostLine.TrimEnd());
				Check(GetConsoleMode(Input, out RestoredMode));
				mAssert.AreEquals(OriginalMode, RestoredMode);
				foreach (var Operation in new mStd.tAction[] {
						() => SavedTerminal!.Size(),
						() => SavedTerminal!.Present(),
						() => SavedTerminal!.Draw("expired", (0, 0), default),
						() => SavedTerminal!.Clear(),
						() => SavedTerminal!.Cursor(0, 0),
						() => SavedTerminal!.CursorVisible(),
						() => SavedTerminal!.SetCursorVisible(true)
					}
				
				) {
					try {
						Operation();
						mAssert.Fail("Closed terminal was accepted.");
					} catch (System.InvalidOperationException) {}
				}
				
				System.Console.WriteLine(
					(
						"OK: buffered frames, deferred text/clear/cursor, native attributes, blink, " +
						"resize preservation, input, cleanup and Snake host startup/exit."
					)
				);
			} finally {
				CloseHandle(Input);
			}
			
			return 0;
		}
	);
}