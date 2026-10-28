#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../src/mStartup.cs
#:ref ../Runtime/GUI/mSDL3.cs

public static class
mSDL3_Tests {
	public static tInt32
	Main() => mRuntime.WithCompilerStack(
		() => {
			// Exercise the real SDL software renderer without opening a desktop window.
			System.Environment.SetEnvironmentVariable("SDL_VIDEODRIVER", "dummy");
			var Screenshot = System.IO.Path.GetFullPath(".tmp/GUI_Demo.bmp");
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Screenshot)!);
			mSDL3.WithWindow(
				aSurface => aSurface.WithCapability(
					aCapability => {
						var Startup = mStartup.Load(
							System.IO.Path.Combine(
								System.IO.Path.GetDirectoryName(mStd.File())!,
								"../Apps/GUI_Demo/startup.jni"
							)
						);
						var App = Startup.Entry.Init(
							_ => {}
						).ElseThrow().Data;
						mAssert.IsTrue(App.IsSig(out _, out _, out var Body));
						var State = Body.Field("Initial");
						
						void
						Send(
							mVM_Data.tData aEvent
						) {
							var Result = Body.Field("Frame...").Call(mVM_Data.Tuple([State, aEvent]), aCapability);
							mAssert.IsTrue(Result.IsTuple(out State, out var Continue));
							mAssert.IsTrue(Continue.IsBool(out var Running) && Running);
							if (!aEvent.IsPrefix("_Redraw", out _)) {
								Send(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
							}
						}
						
						Send(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
						var Windows = SDL_GetWindows(out var Count);
						mAssert.IsTrue(Count == 1 && Windows != tCPtr.Zero);
						var Window = Marshal.ReadIntPtr(Windows);
						SDL_free(Windows);
						var WindowId = SDL_GetWindowID(Window);
						var Renderer = SDL_GetRenderer(Window);
						var Pixels = SDL_RenderReadPixels(Renderer, tCPtr.Zero);
						mAssert.IsTrue(Pixels != tCPtr.Zero, "SDL produced a readable frame.");
						try {
							mAssert.IsTrue(SDL_SaveBMP(Pixels, Screenshot));
						} finally {
							SDL_DestroySurface(Pixels);
						}
						
						aSurface.Present();
						
						void
						Push(
							mSDL3.tEvent aEvent
						) {
							SDL_FlushEvents(0, 0xffff);
							aEvent.WindowId = WindowId;
							mAssert.IsTrue(SDL_PushEvent(ref aEvent));
							Send(aSurface.ReadEvent());
						}
						
						Push(
							new() {
								Type = 0x300,
								Key = 9
							}
						);
						mAssert.AreEquals(1, State.Field("UI").Field("Interaction").Field("Focus").Int());
						var Text = Marshal.StringToCoTaskMemUTF8("Ada");
						try {
							Push(
								new() {
									Type = 0x303,
									Text = Text
								}
							);
						} finally {
							Marshal.FreeCoTaskMem(Text);
						}
						
						Push(
							new() {
								Type = 0x300,
								Key = 9
							}
						);
						Push(
							new() {
								Type = 0x300,
								Key = 13
							}
						);
						mAssert.AreEquals("Saved: Ada", State.Field("Status").Text());
						Push(
							new() {
								Type = 0x401,
								Button = 1,
								X = 25,
								Y = 25
							}
						);
						mAssert.AreEquals(0, State.Field("UI").Field("Interaction").Field("Focus").Int());
						mAssert.IsTrue(SDL_SetWindowSize(Window, 640, 400));
						Send(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
						mAssert.AreEquals((640, 400), aSurface.Size());
						aSurface.Present();
						System.Console.WriteLine(
							"OK: native SDL drawing, clip, text, title, input events and window resize."
						);
					}
				)
			);
			// A second initialization verifies teardown and reusable library binding.
			mSDL3.WithWindow(
				aSurface => {
					mAssert.AreEquals((1024, 768), aSurface.Size());
				}
			);
			System.Console.WriteLine("OK: SDL cleanup and reinitialization. Screenshot: " + Screenshot);
			return 0;
		}
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tCPtr
	SDL_GetWindows(
		out tInt32 aCount
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_free(
		tCPtr aMemory
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tNat32
	SDL_GetWindowID(
		tCPtr aWindow
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tCPtr
	SDL_GetRenderer(
		tCPtr aWindow
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern tCPtr
	SDL_RenderReadPixels(
		tCPtr aRenderer,
		tCPtr aRect
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_SaveBMP(
		tCPtr aSurface,
		[MarshalAs(UnmanagedType.LPUTF8Str)] tText aPath
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_DestroySurface(
		tCPtr aSurface
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	private static extern void
	SDL_FlushEvents(
		tNat32 aMinType,
		tNat32 aMaxType
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_PushEvent(
		ref mSDL3.tEvent aEvent
	);
	
	[DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern tBool
	SDL_SetWindowSize(
		tCPtr aWindow,
		tInt32 aWidth,
		tInt32 aHeight
	);
}