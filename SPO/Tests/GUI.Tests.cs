#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Exe
#:property PublishAot = false
#:include ../../SPO_CS/src/_GlobalUsings.cs
#:ref ../src/mStartup.cs
#:ref ../Runtime/GUI/mGUI.cs
#:ref ../Runtime/GUI/mSDL3.cs

public static class
mGUI_Tests {
	public static tInt32
	Main() => mRuntime.WithCompilerStack(
		() => {
			var DrawCount = 0;
			var SelectedButtons = 0;
			mGUI.tRect? SelectedBounds = null;
			var Title = "";
			var Dimensions = (Width: 1024, Height: 768);
			var Surface = new mGUI.tSurface {
				Size = () => Dimensions,
				Clear = _ => {
					DrawCount = 0;
					SelectedButtons = 0;
					SelectedBounds = null;
				},
				Rectangle = (aBounds, aColor, aFilled) => {
					DrawCount += 1;
					if (aFilled && aColor == (53, 77, 98)) {
						SelectedButtons += 1;
						SelectedBounds = aBounds;
					}
				},
				Text = (_, _, _) => DrawCount += 1,
				Clip = _ => {},
				Title = a => Title = a,
				Present = () => {},
				ReadEvent = () => mVM_Data.Empty()
			};
			var StartupPath = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(mStd.File())!,
				"../Apps/GUI_Demo/startup.jni"
			);
			var Startup = mStartup.Load(StartupPath);
			var App = Startup.Entry.Init(
				_ => {}
			).ElseThrow().Data;
			mAssert.IsTrue(App.IsSig(out var Contract, out _, out var Body));
			mAssert.IsTrue(Contract.SameType(mGUI.Signature("App")));
			mAssert.IsTrue(DrawCount == 0, "App initialization needs no host operations.");
			mVM_Data.tData? SavedCapability = null;
			Surface.WithCapability(
				aCapability => {
					SavedCapability = aCapability;
					var State = Body.Field("Initial");
					var Frame = Body.Field("Frame...");
					
					tBool
					Send(
						mVM_Data.tData aEvent
					) {
						var Result = Frame.Call(mVM_Data.Tuple([State, aEvent]), aCapability);
						mAssert.IsTrue(Result.IsTuple(out State, out var Continue));
						mAssert.IsTrue(Continue.IsBool(out var Running));
						if (Running && !aEvent.IsPrefix("_Redraw", out _)) {
							return Send(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
						}
						
						return Running;
					}
					
					mVM_Data.tData
					Key(
						tText aKey,
						tBool aShift = false,
						tBool aRepeat = false
					) => mVM_Data.Prefix(
						"_Key...",
						mVM_Data.Record(
							[
								("_Key", mVM_Data.Prefix("_" + aKey, mVM_Data.Empty())),
								("_Shift", mVM_Data.Bool(aShift)),
								("_IsRepeat", mVM_Data.Bool(aRepeat))
							]
						)
					);
					
					var Imports = Startup.Modules.Where(a => a.Key is "GUI" or "Std").Init(
						_ => {}
					).ElseThrow();
					var ScopePath = System.IO.Path.Combine(
						System.IO.Path.GetDirectoryName(mStd.File())!,
						"GUI.Scopes.spo"
					);
					var Scopes = mSPO_Interpreter.Run(
						System.IO.File.ReadAllText(ScopePath),
						ScopePath,
						Imports,
						_ => {}
					).ElseThrow().Data.Call(mVM_Data.Empty());
					mAssert.AreEquals(2, Scopes.Field("Count").Int());
					mAssert.AreEquals(91, Scopes.Field("Other").Int());
					mAssert.AreEquals(42, Scopes.Field("Outcome").Field("Number").Int());
					mAssert.AreEquals("plain", Scopes.Field("Outcome").Field("Text").Text());
					var ScopeTargets = Scopes.Field("UI").Field("Targets");
					foreach (var Id in new[] {
							26,
							25,
							24,
							23,
							22,
							21
						}
					
					) {
						mAssert.IsTrue(ScopeTargets.IsPair(out ScopeTargets, out var Target));
						mAssert.AreEquals(Id, Target.Field("Id").Int());
						mAssert.AreEquals(new mGUI.tRect(0, (Id - 21) * 32, 300, 32), Target.Field("Bounds").Rect());
					}
					
					mAssert.IsTrue(ScopeTargets.IsEmpty());
					System.Console.WriteLine(
						"OK: nested list procedures, explicit contexts, returns and sibling layout."
					);
					// Consume widget results directly, including two independent text inputs.
					var Widgets = mSPO_Interpreter.Run(
						"""
						§IMPORT {
							GUI: {
								Initial: §DEF cInitial
								EmptyPanel: §DEF cEmptyPanel
								BeginFor...With...: §DEF BeginFor...With...
								AddTextInput...WithLabel...Value...: §DEF AddTextInput...WithLabel...Value...
								AddButton...WithText...Selected...: §DEF AddButton...WithText...Selected...
								AddListWith...Do...: §DEF AddListWith...Do...
								FinishWith...In...: §DEF FinishWith...In...
							} € [§LOAD "../Libs/GUI/GUI.SIG"]
						}
						§DEF tState = [<
							UI: [§LOAD "../Libs/GUI/State.SIG"]
							Name: §TEXT
							Other: §TEXT
							Clicked: §BOOL
						>]
						§DEF Frame... = (§DEF aState € tState, §DEF aEvent € [§LOAD "../Libs/GUI/Event.SIG"]) => {
							{UI: §DEF UI, Name: §DEF Name, Other: §DEF Other} = aState
							§VAR Panel := cEmptyPanel, BeginFor aEvent With UI .
							Panel :
								AddListWith {
									Axis: #Vertical
									Alignment: #Start
									CrossAlignment: #Stretch
									Padding: 0
									Spacing: 8
									Color: (0, 0, 0)
								} Do (
									(§VAR aPanel) : () {
										aPanel :
											AddTextInput 11 WithLabel "First" Value Name => §DEF NextName
											AddTextInput 12 WithLabel "Second" Value Other => §DEF NextOther
											AddButton 13 WithText "Save" Selected §FALSE => §DEF Clicked
										.
										§RETURN (NextName, NextOther, Clicked)
									}
								) => (§DEF NextName, §DEF NextOther, §DEF Clicked)
								FinishWith {
									Axis: #Vertical
									Alignment: #Start
									CrossAlignment: #Stretch
									Padding: 8
									Spacing: 0
									Color: (0, 0, 0)
								} In {Width: 400, Height: 300} => (§DEF NextUI, _)
							.
							§RETURN {UI: NextUI, Name: NextName, Other: NextOther, Clicked: Clicked}
						}
						§EXPORT {Initial: {UI: cInitial, Name: "", Other: "", Clicked: §FALSE}, Frame...: Frame...}
						""",
						mStd.File(),
						Imports,
						_ => {}
					).ElseThrow().Data;
					var WidgetState = Widgets.Field("Initial");
					var WidgetDrawCount = DrawCount;
					
					void
					WidgetSend(
						mVM_Data.tData aEvent
					) => WidgetState = Widgets.Field("Frame...").Call(mVM_Data.Tuple([WidgetState, aEvent]));
					
					static mGUI.tRect
					BoundsFor(
						mVM_Data.tData aUI,
						tInt32 aId
					) {
						var Targets = aUI.Field("Targets");
						while (Targets.IsPair(out Targets, out var Target)) {
							if (Target.Field("Id").Int() == aId) {
								return Target.Field("Bounds").Rect();
							}
						}
						
						throw new System.InvalidOperationException("Missing widget: " + aId);
					}
					
					mVM_Data.tData
					Pointer(
						tInt32 aId,
						tText aAction
					) {
						var Bounds = BoundsFor(WidgetState.Field("UI"), aId);
						return mVM_Data.Prefix(
							"_Pointer...",
							mVM_Data.Record(
								[
									(
										"_Position",
										mVM_Data.Tuple([mVM_Data.Int(Bounds.X + 1), mVM_Data.Int(Bounds.Y + 1)])
									),
									("_Action", mVM_Data.Prefix("_" + aAction, mVM_Data.Empty()))
								]
							)
						);
					}
					
					WidgetSend(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
					WidgetSend(Key("Tab"));
					WidgetSend(mVM_Data.Prefix("_Text...", mGUI.Text("Ada")));
					mAssert.AreEquals("Ada", WidgetState.Field("Name").Text());
					mAssert.AreEquals("", WidgetState.Field("Other").Text());
					WidgetSend(Key("Tab"));
					WidgetSend(mVM_Data.Prefix("_Text...", mGUI.Text("B")));
					mAssert.AreEquals("B", WidgetState.Field("Other").Text());
					mAssert.AreEquals("Ada", WidgetState.Field("Name").Text());
					WidgetSend(Key("Backspace"));
					WidgetSend(Key("Backspace"));
					mAssert.AreEquals("", WidgetState.Field("Other").Text());
					WidgetSend(Key("Tab", true));
					WidgetSend(mVM_Data.Prefix("_Text...", mGUI.Text("!")));
					mAssert.AreEquals("Ada!", WidgetState.Field("Name").Text());
					WidgetSend(Pointer(12, "Press"));
					WidgetSend(Pointer(12, "Release"));
					WidgetSend(mVM_Data.Prefix("_Text...", mGUI.Text("Other")));
					mAssert.AreEquals("Other", WidgetState.Field("Other").Text());
					WidgetSend(Pointer(13, "Press"));
					mAssert.AreEquals(mVM_Data.Bool(false), WidgetState.Field("Clicked"));
					WidgetSend(Pointer(11, "Release"));
					mAssert.AreEquals(mVM_Data.Bool(false), WidgetState.Field("Clicked"));
					WidgetSend(Pointer(13, "Press"));
					WidgetSend(Pointer(13, "Release"));
					mAssert.AreEquals(mVM_Data.Bool(true), WidgetState.Field("Clicked"));
					WidgetSend(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
					mAssert.AreEquals(mVM_Data.Bool(false), WidgetState.Field("Clicked"));
					WidgetSend(Key("Enter", aRepeat: true));
					mAssert.AreEquals(mVM_Data.Bool(false), WidgetState.Field("Clicked"));
					WidgetSend(Key("Space"));
					mAssert.AreEquals(mVM_Data.Bool(true), WidgetState.Field("Clicked"));
					WidgetSend(Pointer(13, "Press"));
					WidgetSend(mVM_Data.Prefix("_Focus...", mVM_Data.Bool(false)));
					WidgetSend(Pointer(13, "Release"));
					mAssert.AreEquals(mVM_Data.Bool(false), WidgetState.Field("Clicked"));
					mAssert.AreEquals(WidgetDrawCount, DrawCount);
					System.Console.WriteLine(
						"OK: direct widget results, independent text inputs, mouse and keyboard activation."
					);
					var BeforePureDrawCount = DrawCount;
					var Example = mSPO_Interpreter.Run(
						"""
						§IMPORT {
							GUI: {
								...LayoutWithin...: §DEF ...LayoutWithin...
							} € [§LOAD "../Libs/GUI/GUI.SIG"]
						}
						§EXPORT () => {
						§DEF cTree € [§LOAD "../Libs/GUI/Element.SIG"] = (#List {
							Axis: (#Horizontal), Alignment: (#SpaceBetween), CrossAlignment: (#Center)
							Padding: 10, Spacing: 8, Color: (0, 0, 0)
							Children: (
								(#Button {Id: 11, Text: "One", Selected: §FALSE})
								(#Button {Id: 12, Text: "Two", Selected: §FALSE})
							)
						})
						§DEF cFrame = cTree .LayoutWithin {X: 20, Y: 30, Width: 200, Height: 100}
						§DEF cParagraph € [§LOAD "../Libs/GUI/Element.SIG"] = (#Paragraph {
							Text: "one two three", Alignment: (#SpaceBetween), Color: (255, 255, 255)
						})
						§RETURN {
							Frame: cFrame
							Paragraph: cParagraph .LayoutWithin {X: 0, Y: 0, Width: 64, Height: 100}
						}
						}
						""",
						mStd.File(),
						Imports,
						_ => {}
					).ElseThrow().Data.Call(mVM_Data.Empty());
					mAssert.IsTrue(BeforePureDrawCount == DrawCount, "Pure GUI calculations do not draw.");
					var Items = Example.Field("Frame").Field("Items");
					mAssert.IsTrue(Items.IsPair(out var Before, out var Second));
					mAssert.IsTrue(Before.IsPair(out _, out var First));
					mAssert.AreEquals(new mGUI.tRect(30, 64, 54, 32), First.Field("Bounds").Rect());
					mAssert.AreEquals(new mGUI.tRect(156, 64, 54, 32), Second.Field("Bounds").Rect());
					var Words = Example.Field("Paragraph").Field("Items");
					mAssert.IsTrue(Words.IsPair(out var FirstLine, out var LastWord));
					mAssert.AreEquals(new mGUI.tRect(0, 16, 40, 16), LastWord.Field("Bounds").Rect());
					mAssert.IsTrue(FirstLine.IsPair(out _, out var SecondWord));
					mAssert.AreEquals(new mGUI.tRect(40, 0, 24, 16), SecondWord.Field("Bounds").Rect());
					System.Console.WriteLine("OK: pure layout, word wrapping and justification.");
					mAssert.IsTrue(Send(mVM_Data.Prefix("_Redraw", mVM_Data.Empty())));
					mAssert.IsTrue(DrawCount > 80, "Demo emits a complete frame through drawing primitives.");
					Send(Key("Tab"));
					mAssert.AreEquals(1, State.Field("UI").Field("Interaction").Field("Focus").Int());
					Send(mVM_Data.Prefix("_Text...", mGUI.Text("Ada")));
					mAssert.AreEquals("Ada", State.Field("Name").Text());
					mAssert.IsTrue(Title.EndsWith("Ada"));
					Send(Key("Backspace"));
					mAssert.AreEquals("Ad", State.Field("Name").Text());
					Send(mVM_Data.Prefix("_Text...", mGUI.Text("a")));
					Send(Key("Tab"));
					Send(Key("Enter"));
					mAssert.AreEquals("Saved: Ada", State.Field("Status").Text());
					Send(mVM_Data.Prefix("_Text...", mGUI.Text(" ignored")));
					mAssert.AreEquals("Ada", State.Field("Name").Text());
					Send(Key("Tab", true));
					mAssert.AreEquals(1, State.Field("UI").Field("Interaction").Field("Focus").Int());
					Send(Key("Tab", true));
					mAssert.AreEquals(6, State.Field("UI").Field("Interaction").Field("Focus").Int());
					Send(Key("Space"));
					mAssert.IsTrue(State.Field("Alignment").IsPrefix("_End", out _));
					mAssert.AreEquals(1, SelectedButtons);
					mAssert.AreEquals(BoundsFor(State.Field("UI"), 6), SelectedBounds!.Value);
					Dimensions = (320, 240);
					Send(mVM_Data.Prefix("_Redraw", mVM_Data.Empty()));
					Send(mVM_Data.Prefix("_Scroll...", mVM_Data.Int(-48)));
					mAssert.AreEquals(48, State.Field("UI").Field("Scroll").Int());
					mAssert.IsTrue(!Send(Key("Escape")), "Escape exits the GUI application.");
					System.Console.WriteLine(
						"OK: demo drawing, editing, save, focus, alignment, resize and scrolling."
					);
					try {
						mAssert.IsTrue(aCapability.IsInterface(out var SurfaceType, out _, out var Methods));
						var Forged = mVM_Data.Interface(SurfaceType, mVM_Data.Var(mVM_Data.Empty()), Methods);
						Forged.CallMethod("Size", mVM_Data.Empty());
						mAssert.Fail("Forged GUI handle was accepted.");
					} catch (System.InvalidOperationException) {}
				}
			);
			try {
				SavedCapability!.CallMethod("Size", mVM_Data.Empty());
				mAssert.Fail("Expired GUI capability was accepted.");
			} catch (System.InvalidOperationException) {}
			
			var KeyEvent = new mSDL3.tEvent {
				Type = 0x300,
				Key = 9,
				Modifiers = 1
			}.Translate();
			mAssert.IsTrue(KeyEvent.IsPrefix("_Key...", out var KeyData));
			mAssert.IsTrue(KeyData.Field("Shift").IsBool(out var Shift) && Shift);
			System.Console.WriteLine("OK: pure calculations, capability identity/lifetime and SDL keys.");
			return 0;
		}
	);
}