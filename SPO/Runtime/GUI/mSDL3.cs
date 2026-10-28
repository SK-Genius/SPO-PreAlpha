#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref mGUI.cs

public static class
mSDL3 {
	// SDL3's stable 128-byte event union; offsets follow SDL_events.h (64-bit ABI).
	[StructLayout(LayoutKind.Explicit, Size = 128)]
	public struct
	tEvent {
		[FieldOffset(0)]
		public tNat32 Type;
		[FieldOffset(16)]
		public tNat32 WindowId;
		[FieldOffset(24)]
		public tCPtr Text;
		[FieldOffset(24)]
		public tNat8 Button;
		[FieldOffset(28)]
		public tNat32 Key;
		[FieldOffset(28)]
		public float X;
		[FieldOffset(28)]
		public float WheelY;
		[FieldOffset(32)]
		public float Y;
		[FieldOffset(32)]
		public tNat16 Modifiers;
		[FieldOffset(32)]
		public tNat32 WheelDirection;
		[FieldOffset(37)]
		public tNat8 Repeat;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tFloatRect {
		public float X, Y, Width, Height;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	private struct
	tIntRect {
		public tInt32 X, Y, Width, Height;
	}
	
	public static mVM_Data.tData
	Translate(
		this tEvent aEvent
	) {
		mVM_Data.tData
		Tag(
			tText aName,
			mVM_Data.tData? aBody = null
		) => mVM_Data.Prefix("_" + aName + (aBody is null ? "" : "..."), aBody ?? mVM_Data.Empty());
		
		mVM_Data.tData
		Pointer(
			tText aAction
		) => Tag(
			"Pointer",
			mVM_Data.Record(
				[
					(
						"_Position",
						mVM_Data.Tuple([mVM_Data.Int((tInt32)aEvent.X), mVM_Data.Int((tInt32)aEvent.Y)])
					),
					("_Action", Tag(aAction))
				]
			)
		);
		
		return aEvent.Type switch {
			0x100 or 0x210 => Tag("Quit"), // SDL_EVENT_QUIT / WINDOW_CLOSE_REQUESTED
			0x204 or 0x206 or 0x207 or 0x214 => Tag("Redraw"),
			0x20e => Tag("Focus", mVM_Data.Bool(true)),
			0x20f => Tag("Focus", mVM_Data.Bool(false)),
			0x300 => Tag(
				"Key",
				mVM_Data.Record(
					[
						(
							"_Key",
							Tag(
								aEvent.Key switch {
									9 => "Tab",
									13 => "Enter",
									32 => "Space",
									8 => "Backspace",
									27 => "Escape",
									_ => "Other"
								}
							)
						),
						("_Shift", mVM_Data.Bool((aEvent.Modifiers & 0x0003) != 0)),
						("_IsRepeat", mVM_Data.Bool(aEvent.Repeat != 0))
					]
				)
			),
			0x303 => Tag("Text", mGUI.Text(Marshal.PtrToStringUTF8(aEvent.Text) ?? "")),
			0x400 => Pointer("Move"),
			0x401 when aEvent.Button == 1 => Pointer("Press"),
			0x402 when aEvent.Button == 1 => Pointer("Release"),
			0x403 => Tag(
				"Scroll",
				mVM_Data.Int((tInt32)System.Math.Round(aEvent.WheelY * (aEvent.WheelDirection == 1 ? -48 : 48)))
			),
			_ => mVM_Data.Empty()};
	}
	
	static mSDL3() {
		NativeLibrary.SetDllImportResolver(
			typeof(mSDL3).Assembly,
			(aName, _, _) => {
				if (aName != "SDL3") {
					return tCPtr.Zero;
				}
				
				var LibraryPath = System.Environment.GetEnvironmentVariable("SDL3_LIBRARY");
				if (!tText.IsNullOrEmpty(LibraryPath)) {
					return NativeLibrary.Load(System.IO.Path.GetFullPath(LibraryPath));
				}
				
				if (NativeLibrary.TryLoad("SDL3", out var Library)) {
					return Library;
				}
				
				// Reuse an installed native package, without referencing its managed GUI library.
				var Cache = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES") ?? System.IO.Path.Combine(
					System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
					".nuget/packages"
				);
				var Package = System.IO.Path.Combine(Cache, "sdl3-cs.native");
				var RID = RuntimeInformation.RuntimeIdentifier;
				var Filename = (
					System.OperatingSystem.IsWindows() ? "SDL3.dll" :
					System.OperatingSystem.IsMacOS() ? "libSDL3.dylib" : "libSDL3.so"
				);
				if (System.IO.Directory.Exists(Package)) {
					var Versions = System.IO.Directory.GetDirectories(Package);
					System.Array.Sort(Versions, System.StringComparer.Ordinal);
					System.Array.Reverse(Versions);
					foreach (var Version in Versions) {
						LibraryPath = System.IO.Path.Combine(Version, "runtimes", RID, "native", Filename);
						if (System.IO.File.Exists(LibraryPath)) {
							return NativeLibrary.Load(LibraryPath);
						}
					}
				}
				
				throw new System.DllNotFoundException(
					"Install SDL3 >= 3.2 or set SDL3_LIBRARY to its native library."
				);
			}
		);
	}
	
	public static void
	WithWindow(
		mStd.tAction<mGUI.tSurface> aRun
	) {
		if (tCPtr.Size != 8) {
			throw new System.PlatformNotSupportedException("The SDL3 event bridge requires a 64-bit process.");
		}
		
		void
		Check(
			tBool aSucceeded
		) {
			if (!aSucceeded) {
				throw new System.InvalidOperationException(Marshal.PtrToStringUTF8(SDL_GetError()));
			}
		}
		
		SDL_SetMainReady();
		var Window = tCPtr.Zero;
		var Renderer = tCPtr.Zero;
		try {
			Check(SDL_Init(0x00000020)); // SDL_INIT_VIDEO
			Window = SDL_CreateWindow("Immediate UI in SPO", 1024, 768, 0x00000020); // SDL_WINDOW_RESIZABLE
			Check(Window != tCPtr.Zero);
			Renderer = SDL_CreateRenderer(Window, null);
			Check(Renderer != tCPtr.Zero);
			Check(SDL_StartTextInput(Window));
			
			void
			Color(
				(tNat8 R, tNat8 G, tNat8 B) aColor
			) => Check(SDL_SetRenderDrawColor(Renderer, aColor.R, aColor.G, aColor.B, 255));
			
			aRun(
				new() {
					Size = () => {
						Check(SDL_GetWindowSize(Window, out var Width, out var Height));
						Check(SDL_GetRenderOutputSize(Renderer, out var PixelWidth, out var PixelHeight));
						Check(SDL_SetRenderScale(Renderer, (float)PixelWidth / Width, (float)PixelHeight / Height));
						return (Width, Height);
					},
					Clear = aColor => {
						Color(aColor);
						Check(SDL_RenderClear(Renderer));
					},
					Rectangle = (aBounds, aColor, aFilled) => {
						Color(aColor);
						var Rect = new tFloatRect {
							X = aBounds.X,
							Y = aBounds.Y,
							Width = aBounds.Width,
							Height = aBounds.Height
						};
						Check(
							aFilled
							? SDL_RenderFillRect(Renderer, ref Rect)
							: SDL_RenderRect(Renderer, ref Rect)
						);
					},
					Text = (aText, aPosition, aColor) => {
						Color(aColor);
						Check(SDL_RenderDebugText(Renderer, aPosition.X, aPosition.Y, aText));
					},
					Clip = aBounds => {
						if (aBounds is {} Bounds) {
							var Rect = new tIntRect {
								X = Bounds.X,
								Y = Bounds.Y,
								Width = Bounds.Width,
								Height = Bounds.Height
							};
							Check(SDL_SetRenderClipRect(Renderer, ref Rect));
						} else {
							Check(SDL_ResetRenderClipRect(Renderer, tCPtr.Zero));
						}
					},
					Title = aTitle => Check(SDL_SetWindowTitle(Window, aTitle)),
					Present = () => Check(SDL_RenderPresent(Renderer)),
					ReadEvent = () => {
						while (true) {
							Check(SDL_WaitEvent(out var Event));
							if (Event.Type != 0x100 && Event.WindowId != SDL_GetWindowID(Window)) {
								continue;
							}
							
							var Input = Event.Translate();
							if (!Input.IsEmpty()) {
								return Input;
							}
						}
					}
				}
			);
		} finally {
			if (Renderer != tCPtr.Zero) {
				SDL_DestroyRenderer(Renderer);
			}
			
			if (Window != tCPtr.Zero) {
				SDL_DestroyWindow(Window);
			}
			
			SDL_Quit();
		}
	}
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_SetMainReady();
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_Init(
		tNat32 aFlags
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tCPtr
	SDL_GetError();
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tCPtr
	SDL_CreateWindow(
		[MarshalAs(UnmanagedType.LPUTF8Str)] tText aTitle,
		tInt32 aWidth,
		tInt32 aHeight,
		tNat64 aFlags
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tCPtr
	SDL_CreateRenderer(
		tCPtr aWindow,
		[MarshalAs(UnmanagedType.LPUTF8Str)] tText? aName
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_StartTextInput(
		tCPtr aWindow
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_GetWindowSize(
		tCPtr aWindow,
		out tInt32 aWidth,
		out tInt32 aHeight
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_GetRenderOutputSize(
		tCPtr aRenderer,
		out tInt32 aWidth,
		out tInt32 aHeight
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_SetRenderScale(
		tCPtr aRenderer,
		float aX,
		float aY
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_SetRenderDrawColor(
		tCPtr aRenderer,
		tNat8 aR,
		tNat8 aG,
		tNat8 aB,
		tNat8 aAlpha
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_RenderClear(
		tCPtr aRenderer
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_RenderFillRect(
		tCPtr aRenderer,
		ref tFloatRect aRect
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_RenderRect(
		tCPtr aRenderer,
		ref tFloatRect aRect
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_RenderDebugText(
		tCPtr aRenderer,
		float aX,
		float aY,
		[MarshalAs(UnmanagedType.LPUTF8Str)] tText aText
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_SetRenderClipRect(
		tCPtr aRenderer,
		ref tIntRect aRect
	);
	
	[DllImport("SDL3", EntryPoint = "SDL_SetRenderClipRect", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_ResetRenderClipRect(
		tCPtr aRenderer,
		tCPtr aRect
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_SetWindowTitle(
		tCPtr aWindow,
		[MarshalAs(UnmanagedType.LPUTF8Str)] tText aTitle
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_RenderPresent(
		tCPtr aRenderer
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_WaitEvent(
		out tEvent aEvent
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tNat32
	SDL_GetWindowID(
		tCPtr aWindow
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_DestroyRenderer(
		tCPtr aRenderer
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_DestroyWindow(
		tCPtr aWindow
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_Quit();
}