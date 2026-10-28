#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref mTerminal.cs

public static class
mWindowsTerminal {
	private const tNat16 cKeyEvent = 0x0001;
	private const tNat16 cMouseEvent = 0x0002;
	private const tNat16 cFocusEvent = 0x0010;
	
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
	
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tCell {
		public tNat16 Character, Attributes;
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
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	AllocConsole();
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	FreeConsole();
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	CloseHandle(
		tCPtr aHandle
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tCPtr
	CreateConsoleScreenBuffer(
		tNat32 aAccess,
		tNat32 aShare,
		tCPtr aSecurity,
		tNat32 aFlags,
		tCPtr aReserved
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	SetConsoleActiveScreenBuffer(
		tCPtr aHandle
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	GetConsoleMode(
		tCPtr aHandle,
		out tNat32 aMode
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	SetConsoleMode(
		tCPtr aHandle,
		tNat32 aMode
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	GetConsoleScreenBufferInfo(
		tCPtr aHandle,
		out tScreenInfo aInfo
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	SetConsoleCursorPosition(
		tCPtr aHandle,
		tCoord aPosition
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	GetConsoleCursorInfo(
		tCPtr aHandle,
		out tCursorInfo aInfo
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	SetConsoleCursorInfo(
		tCPtr aHandle,
		in tCursorInfo aInfo
	);
	
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	WriteConsoleOutputW(
		tCPtr aHandle,
		[In] tCell[] aCells,
		tCoord aSize,
		tCoord aOrigin,
		ref tRect aTarget
	);
	
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern tBool
	ReadConsoleInputW(
		tCPtr aHandle,
		out tInput aInput,
		tNat32 aLength,
		out tNat32 aRead
	);
	
	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern tNat32
	WaitForSingleObject(
		tCPtr aHandle,
		tNat32 aMilliseconds
	);
	
	private static void
	Check(
		tBool aSuccess
	) {
		if (!aSuccess) {
			throw new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError());
		}
	}
	
	private static mVM_Data.tData
	ToEvent(
		this tInput aInput,
		tNat32 aPreviousButtons,
		tBool aIsRepeat,
		(tInt32 X, tInt32 Y) aWindowPosition
	) {
		// Win32 masks are decoded here; applications match named record fields.
		static mVM_Data.tData
		Modifiers(
			tNat32 aFlags
		) => mVM_Data.Record(
			[
				("_Shift", mVM_Data.Bool((aFlags & 0x10) != 0)),
				("_Alt", mVM_Data.Bool((aFlags & 0x03) != 0)),
				("_Control", mVM_Data.Bool((aFlags & 0x0C) != 0)),
				("_CapsLock", mVM_Data.Bool((aFlags & 0x80) != 0)),
				("_NumLock", mVM_Data.Bool((aFlags & 0x20) != 0)),
				("_ScrollLock", mVM_Data.Bool((aFlags & 0x40) != 0))
			]
		);
		
		static mVM_Data.tData
		Buttons(
			tNat32 aFlags
		) => mVM_Data.Record(
			[
				("_Left", mVM_Data.Bool((aFlags & 0x01) != 0)),
				("_Right", mVM_Data.Bool((aFlags & 0x02) != 0)),
				("_Middle", mVM_Data.Bool((aFlags & 0x04) != 0)),
				("_Extra1", mVM_Data.Bool((aFlags & 0x08) != 0)),
				("_Extra2", mVM_Data.Bool((aFlags & 0x10) != 0))
			]
		);
		
		var Key = aInput.Key switch {
			0x08 => "Backspace",
			0x09 => "Tab",
			0x0D => "Enter",
			0x1B => "Escape",
			0x20 => "Space",
			0x21 => "PageUp",
			0x22 => "PageDown",
			0x23 => "End",
			0x24 => "Home",
			0x25 => "ArrowLeft",
			0x26 => "ArrowUp",
			0x27 => "ArrowRight",
			0x28 => "ArrowDown",
			0x2D => "Insert",
			0x2E => "Delete",
			>= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A => "Character",
			>= 0x70 and <= 0x87 => "Function",
			_ => "Unknown"
		};
		var KeyValue = Key switch {
			"Character" => mVM_Data.Prefix("_Char...", mVM_Data.Int(aInput.Key)),
			"Function" => mVM_Data.Int(aInput.Key - 0x70 + 1),
			_ => mVM_Data.Empty()};
		return aInput.Type switch {
			cKeyEvent => mVM_Data.Prefix(
				"_Key...",
				mVM_Data.Record(
					[
						("_Key", mVM_Data.Prefix("_" + Key + (KeyValue.IsEmpty() ? "" : "..."), KeyValue)),
						("_Character", mVM_Data.Prefix("_Char...", mVM_Data.Int(aInput.Character))),
						("_IsDown", mVM_Data.Bool(aInput.Down != 0)),
						("_IsRepeat", mVM_Data.Bool(aIsRepeat)),
						("_RepeatCount", mVM_Data.Int(aInput.Repeat)),
						("_Modifiers", Modifiers(aInput.KeyModifiers))
					]
				)
			),
			cMouseEvent => mVM_Data.Prefix(
				"_Mouse...",
				mVM_Data.Record(
					[
						(
							"_Position",
							mVM_Data.Tuple(
								[
									mVM_Data.Int(aInput.Position.X - aWindowPosition.X),
									mVM_Data.Int(aInput.Position.Y - aWindowPosition.Y)
								]
							)
						),
						("_Buttons", Buttons(aInput.Buttons)),
						("_PressedButtons", Buttons(aInput.Buttons & ~aPreviousButtons)),
						("_ReleasedButtons", Buttons(aPreviousButtons & ~aInput.Buttons)),
						(
							"_WheelDelta",
							mVM_Data.Int((aInput.MouseFlags & 12) != 0 ? (tInt16)(aInput.Buttons >> 16) : 0)
						),
						("_IsHorizontalWheel", mVM_Data.Bool((aInput.MouseFlags & 8) != 0)),
						("_HasMoved", mVM_Data.Bool((aInput.MouseFlags & 1) != 0)),
						("_IsDoubleClick", mVM_Data.Bool((aInput.MouseFlags & 2) != 0)),
						("_Modifiers", Modifiers(aInput.MouseModifiers))
					]
				)
			),
			cFocusEvent => mVM_Data.Prefix("_Focus...", mVM_Data.Bool(aInput.Down != 0)),
			_ => mVM_Data.Empty()};
	}
	
	public static void
	Run(
		mStd.tAction<mTerminal.tTerminal> aRun
	) {
		if (!System.OperatingSystem.IsWindows()) {
			throw new System.PlatformNotSupportedException("This terminal backend requires Windows.");
		}
		
		var Allocated = false;
		const tNat32 cReadWriteAccess = 0xC0000000;
		const tNat32 cShareReadWrite = 3;
		const tNat32 cOpenExisting = 3;
		const tNat32 cTextModeBuffer = 1;
		var Input = CreateFileW("CONIN$", cReadWriteAccess, cShareReadWrite, 0, cOpenExisting, 0, 0);
		if (Input == -1) {
			Check(AllocConsole());
			Allocated = true;
			Input = CreateFileW("CONIN$", cReadWriteAccess, cShareReadWrite, 0, cOpenExisting, 0, 0);
		}
		
		var OriginalOutput = (tCPtr)(-1);
		var Output = (tCPtr)(-1);
		var OriginalMode = 0u;
		var RestoreMode = false;
		var Active = true;
		try {
			Check(Input != -1);
			OriginalOutput = CreateFileW("CONOUT$", cReadWriteAccess, cShareReadWrite, 0, cOpenExisting, 0, 0);
			Check(OriginalOutput != -1);
			Output = CreateConsoleScreenBuffer(cReadWriteAccess, cShareReadWrite, 0, cTextModeBuffer, 0);
			Check(Output != -1);
			Check(GetConsoleMode(Input, out OriginalMode));
			RestoreMode = true;
			// Raw key down/up, mouse and buffer-size events; disable Quick Edit,
			// line/echo input and VT input. Ctrl+C becomes an ordinary key event.
			{
				const tNat32 cProcessedInput = 0x0001, cLineInput = 0x0002, cEchoInput = 0x0004;
				const tNat32 cWindowInput = 0x0008, cMouseInput = 0x0010;
				const tNat32 cQuickEdit = 0x0040, cExtendedFlags = 0x0080, cVirtualTerminalInput = 0x0200;
				var Disabled = cProcessedInput | cLineInput | cEchoInput | cQuickEdit | cVirtualTerminalInput;
				var Enabled = cWindowInput | cMouseInput | cExtendedFlags;
				Check(SetConsoleMode(Input, (OriginalMode & ~Disabled) | Enabled));
			}
			
			Check(SetConsoleActiveScreenBuffer(Output));
			
			void
			EnsureActive() {
				if (!Active) {
					throw new System.InvalidOperationException("Terminal is closed.");
				}
			}
			
			tScreenInfo
			Info() {
				EnsureActive();
				Check(GetConsoleScreenBufferInfo(Output, out var Result));
				return Result;
			}
			
			tCursorInfo
			CursorInfo() {
				EnsureActive();
				Check(GetConsoleCursorInfo(Output, out var Result));
				return Result;
			}
			
			var BufferSize = default(tCoord);
			var Buffer = System.Array.Empty<(tCell Cell, tBool Blink)>();
			var Frame = System.Array.Empty<tCell>();
			var Cursor = Info().Cursor;
			var EmptyCell = new tCell {
				Character = ' ',
				Attributes = (tNat16)mTerminal.tColor.Gray
			};
			const tInt32 cBlinkPeriodMilliseconds = 500;
			var Clock = System.Diagnostics.Stopwatch.StartNew();
			
			void
			ResizeBuffer(
				tCoord aSize
			) {
				if (aSize.X == BufferSize.X && aSize.Y == BufferSize.Y) {
					return;
				}
				
				var Resized = new (tCell Cell, tBool Blink)[aSize.X * aSize.Y];
				System.Array.Fill(Resized, (EmptyCell, false));
				for (var Row = 0; Row < System.Math.Min(aSize.Y, BufferSize.Y); Row += 1) {
					System.Array.Copy(
						Buffer,
						Row * BufferSize.X,
						Resized,
						Row * aSize.X,
						System.Math.Min(aSize.X, BufferSize.X)
					);
				}
				
				Buffer = Resized;
				BufferSize = aSize;
				Frame = new tCell[Buffer.Length];
				Cursor.X = (tInt16)System.Math.Min(Cursor.X, aSize.X - 1);
				Cursor.Y = (tInt16)System.Math.Min(Cursor.Y, aSize.Y - 1);
			}
			
			mTerminal.tSize
			Size() {
				var Current = Info();
				ResizeBuffer(Current.Size);
				return new(
					(Current.Size.X, Current.Size.Y),
					(Current.Window.Left, Current.Window.Top),
					(
						Current.Window.Right - Current.Window.Left + 1,
						Current.Window.Bottom - Current.Window.Top + 1
					)
				);
			}
			
			var PreviousSize = Size();
			var PreviousButtons = 0u;
			var HeldKeys = new tBool[256];
			
			void
			Present() {
				var Current = Info();
				ResizeBuffer(Current.Size);
				var BlinkVisible = (Clock.ElapsedMilliseconds / cBlinkPeriodMilliseconds) % 2 == 0;
				var Changed = false;
				for (var Index = 0; Index < Buffer.Length; Index += 1) {
					var(Cell, Blink) = Buffer[Index];
					if (Blink && !BlinkVisible) {
						Cell.Character = ' ';
					}
					
					Changed |= Cell.Character != Frame[Index].Character ||
					Cell.Attributes != Frame[Index].Attributes;
					Frame[Index] = Cell;
				}
				
				if (Changed) {
					var Target = new tRect {
						Right = (tInt16)(BufferSize.X - 1),
						Bottom = (tInt16)(BufferSize.Y - 1)
					};
					Check(WriteConsoleOutputW(Output, Frame, BufferSize, default, ref Target));
					if (Target.Right != BufferSize.X - 1 || Target.Bottom != BufferSize.Y - 1) {
						// A concurrent resize clipped the copy; repaint on the next presentation.
						System.Array.Clear(Frame);
					}
				}
				
				if (Cursor.X != Current.Cursor.X || Cursor.Y != Current.Cursor.Y) {
					Check(SetConsoleCursorPosition(Output, Cursor));
				}
			}
			
			aRun(
				new() {
					Size = Size,
					Cursor = (aX, aY) => {
						EnsureActive();
						if (aX < 0 || aY < 0 || aX >= BufferSize.X || aY >= BufferSize.Y) {
							throw new System.ArgumentOutOfRangeException(
								nameof(aX),
								"Cursor is outside the buffer."
							);
						}
						
						Cursor = new() {
							X = (tInt16)aX,
							Y = (tInt16)aY
						};
					},
					CursorVisible = () => CursorInfo().Visible != 0,
					SetCursorVisible = aVisible => {
						var Current = CursorInfo();
						if ((Current.Visible != 0) != aVisible) {
							Current.Visible = aVisible ? 1 : 0;
							Check(SetConsoleCursorInfo(Output, Current));
						}
					},
					Draw = (aText, aPosition, aStyle) => {
						EnsureActive();
						if (
							aPosition.X < 0 || aPosition.Y < 0 || aPosition.X >= BufferSize.X ||
							aPosition.Y >= BufferSize.Y
						) {
							throw new System.ArgumentOutOfRangeException(
								nameof(aPosition),
								"Draw position is outside the buffer."
							);
						}
						
						var Length = System.Math.Min(aText.Length, BufferSize.X - aPosition.X);
						var Foreground = aStyle.Foreground;
						var Background = aStyle.Background;
						if (aStyle.Inverse) {
							(Foreground, Background) = (Background, Foreground);
						}
						
						var Attributes = (tNat16)(
							(tInt32)Foreground | ((tInt32)Background << 4) | (
								aStyle.Underline
								? 0x8000
								: 0
							)
						);
						for (var Index = 0; Index < Length; Index += 1) {
							var Cell = new tCell {
								Character = tChar.IsControl(aText[Index]) ? (tNat16)' ' : aText[Index],
								Attributes = Attributes
							};
							Buffer[aPosition.Y * BufferSize.X + aPosition.X + Index] = (Cell, aStyle.Blink);
						}
					},
					Clear = () => {
						EnsureActive();
						System.Array.Fill(Buffer, (EmptyCell, false));
						Cursor = default;
					},
					Present = Present,
					ReadEvent = aWait => {
						const tNat32 cWaitSignaled = 0, cWaitTimeout = 258;
						System.ArgumentOutOfRangeException.ThrowIfNegative(aWait);
						var Deadline = Clock.ElapsedMilliseconds + aWait;
						while (true) {
							Present();
							var CurrentSize = Size();
							if (CurrentSize != PreviousSize) {
								PreviousSize = CurrentSize;
								return mVM_Data.Prefix("_Resize...", CurrentSize.ToData());
							}
							
							var Now = Clock.ElapsedMilliseconds;
							var Remaining = System.Math.Max(0, Deadline - Now);
							var UntilBlink = cBlinkPeriodMilliseconds - Now % cBlinkPeriodMilliseconds;
							var Wait = (tNat32)System.Math.Min(Remaining, UntilBlink);
							var Status = WaitForSingleObject(Input, Wait);
							if (Status == cWaitSignaled) {
								Check(ReadConsoleInputW(Input, out var InputEvent, 1, out var Read));
								var IsRepeat = (
									InputEvent.Type == cKeyEvent && InputEvent.Down != 0 &&
									InputEvent.Key < HeldKeys.Length &&
									HeldKeys[InputEvent.Key]
								);
								var Event = InputEvent.ToEvent(PreviousButtons, IsRepeat, CurrentSize.WindowOrigin);
								if (InputEvent.Type == cKeyEvent && InputEvent.Key < HeldKeys.Length) {
									HeldKeys[InputEvent.Key] = InputEvent.Down != 0;
								} else if (InputEvent.Type == cMouseEvent) {
									PreviousButtons = InputEvent.Buttons & 0x1F;
								} else if (InputEvent.Type == cFocusEvent && InputEvent.Down == 0) {
									System.Array.Clear(HeldKeys);
									PreviousButtons = 0;
								}
								
								if (Read == 1 && !Event.IsEmpty()) {
									return Event;
								}
							} else if (Status != cWaitTimeout) {
								Check(false);
							}
							
							if (Clock.ElapsedMilliseconds >= Deadline) {
								return mVM_Data.Empty();
							}
						}
					}
				}
			);
		} finally {
			Active = false;
			if (OriginalOutput != -1) {
				SetConsoleActiveScreenBuffer(OriginalOutput);
			}
			
			if (RestoreMode) {
				SetConsoleMode(Input, OriginalMode);
			}
			
			if (Output != -1) {
				CloseHandle(Output);
			}
			
			if (OriginalOutput != -1) {
				CloseHandle(OriginalOutput);
			}
			
			if (Input != -1) {
				CloseHandle(Input);
			}
			
			if (Allocated) {
				FreeConsole();
			}
		}
	}
}