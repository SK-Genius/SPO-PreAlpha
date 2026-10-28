#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref ../../../SPO_CS/src/mSPO_Interpreter.cs
#:ref ../../../SPO_CS/src/Common/mStd.cs
#:ref ../../../SPO_CS/src/Common/mAssert.cs
#:ref ../../../SPO_CS/src/Common/mResult.cs
#:ref ../../../SPO_CS/src/Common/mMaybe.cs
#:ref ../../../SPO_CS/src/Common/mTreeMap.cs
#:ref ../../../SPO_CS/src/mVM_Data.cs
#:ref ../../../SPO_CS/src/mVM_Type.cs
#:ref ../../../SPO_CS/src/mVM.cs
#:ref ../mRuntime.cs

public static class
mTerminal {
	// Values follow the Windows console palette order.
	public enum
	tColor {
		Black,
		DarkBlue,
		DarkGreen,
		DarkCyan,
		DarkRed,
		DarkMagenta,
		DarkYellow,
		Gray,
		DarkGray,
		Blue,
		Green,
		Cyan,
		Red,
		Magenta,
		Yellow,
		White
	}
	
	public readonly record struct
	tStyle {
		public tColor Foreground { get; init; }
		public tColor Background { get; init; }
		public tBool Blink { get; init; }
		public tBool Underline { get; init; }
		public tBool Inverse { get; init; }
	}
	
	public readonly record struct
	tSize(
		(tInt32 X, tInt32 Y) BufferSize,
		(tInt32 X, tInt32 Y) WindowOrigin,
		(tInt32 X, tInt32 Y) WindowSize
	);
	// A backend is data containing operations; it owns its native resources.
	public sealed record
	tTerminal {
		public required mStd.tFunc<tSize> Size { get; init; }
		public required mStd.tAction<tInt32, tInt32> Cursor { get; init; }
		public required mStd.tFunc<tBool> CursorVisible { get; init; }
		public required mStd.tAction<tBool> SetCursorVisible { get; init; }
		public required mStd.tAction<tText, (tInt32 X, tInt32 Y), tStyle> Draw { get; init; }
		public required mStd.tAction Clear { get; init; }
		// Host operation: publish the completed frame after an app update.
		public required mStd.tAction Present { get; init; }
		public required mStd.tFunc<tInt32, mVM_Data.tData> ReadEvent { get; init; }
	}
	
	public static mVM_Data.tData
	ToData(
		this tSize aSize
	) => mVM_Data.Record(
		[
			("_Width", mVM_Data.Int(aSize.WindowSize.X)),
			(
				"_Height",
				mVM_Data.Int(aSize.WindowSize.Y)
			)
		]
	);
	
	public static mVM_Type.tType
	LoadSignature(
		tText aName
	) => mSPO_Interpreter.Run(
		$"§EXPORT [§LOAD \"../../Libs/{aName}.SIG\"]",
		mStd.File(),
		(mVM_Data.Empty(), mVM_Type.Empty()),
		_ => {}
	).ElseThrow().Data.SignatureValue();
	
	public static void
	WithCapability(
		this tTerminal aTerminal,
		mStd.tAction<mVM_Data.tData> aRun
	) {
		var Active = true;
		
		tColor
		Color(
			mVM_Data.tData aValue
		) {
			foreach (var Color in System.Enum.GetValues<tColor>()) {
				if (aValue.IsPrefix("_" + Color, out var Body) && Body.IsEmpty()) {
					return Color;
				}
			}
			
			throw new System.ArgumentOutOfRangeException(nameof(aValue), "Invalid terminal color.");
		}
		
		// Only the interface binds this private receiver. Native pointers remain
		// in the backend; operations check receiver identity and lifetime.
		var Handle = mVM_Data.Var(mVM_Data.Empty());
		
		mVM_Data.tData
		Bind(
			mStd.tFunc<mVM_Data.tData, mVM_Data.tData> aOperation
		) => mVM_Data.ExternProc(
			(_, aObject, aArgument, _) => {
				if (!Active || !ReferenceEquals(aObject, Handle)) {
					throw new System.InvalidOperationException("Invalid or expired terminal capability.");
				}
				
				return aOperation(aArgument);
			},
			mVM_Data.Empty()
		);
		
		var Operations = mVM_Data.Record(
			[
				(
					"_Size",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							return aTerminal.Size().ToData();
						}
					)
				),
				(
					"_MoveCursorTo...",
					Bind(
						a => {
							mAssert.IsTrue(a.IsTuple(out var X, out var Y));
							mAssert.IsTrue(X.IsInt(out var Column));
							mAssert.IsTrue(Y.IsInt(out var Row));
							var Size = aTerminal.Size();
							if (Column < 0 || Row < 0 || Column >= Size.WindowSize.X || Row >= Size.WindowSize.Y) {
								throw new System.ArgumentOutOfRangeException(
									nameof(a),
									"Cursor is outside the window."
								);
							}
							
							aTerminal.Cursor(Column + Size.WindowOrigin.X, Row + Size.WindowOrigin.Y);
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_IsCursorVisible",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							return mVM_Data.Bool(aTerminal.CursorVisible());
						}
					)
				),
				(
					"_ShowCursor",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							aTerminal.SetCursorVisible(true);
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_HideCursor",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							aTerminal.SetCursorVisible(false);
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_Draw...At...WithStyle...",
					Bind(
						a => {
							mAssert.IsTrue(a.IsTuple(out var Text, out var Position, out var Style));
							mAssert.IsTrue(Position.IsTuple(out var X, out var Y));
							mAssert.IsTrue(X.IsInt(out var Column));
							mAssert.IsTrue(Y.IsInt(out var Row));
							var Foreground = Color(Style.Field("Foreground"));
							var Background = Color(Style.Field("Background"));
							mAssert.IsTrue(Style.Field("Blink").IsBool(out var Blink));
							mAssert.IsTrue(Style.Field("Underline").IsBool(out var Underline));
							mAssert.IsTrue(Style.Field("Inverse").IsBool(out var Inverse));
							var Buffer = new System.Text.StringBuilder();
							while (Text.IsPair(out var Prefix, out var Character)) {
								mAssert.IsTrue(Character.IsPrefix("_Char...", out var Code));
								mAssert.IsTrue(Code.IsInt(out var Ordinal));
								Buffer.Append(checked((tChar)Ordinal));
								Text = Prefix;
							}
							
							mAssert.IsTrue(Text.IsEmpty());
							var Characters = Buffer.ToString().ToCharArray();
							System.Array.Reverse(Characters);
							var Size = aTerminal.Size();
							// Clip to the viewport, including negative starting columns. Use long
							// arithmetic so even the minimum SPO integer is a harmless offscreen draw.
							var Start = (tInt32)System.Math.Min(
								Characters.Length,
								System.Math.Max(
									0L,
									-(tInt64)Column
								)
							);
							var VisibleColumn = System.Math.Max(0, Column);
							var Length = System.Math.Min(
								Characters.Length - Start,
								Size.WindowSize.X - VisibleColumn
							);
							if (Row < 0 || Row >= Size.WindowSize.Y || Length <= 0) {
								return mVM_Data.Empty();
							}
							
							aTerminal.Draw(
								new tText(Characters, Start, Length),
								(VisibleColumn + Size.WindowOrigin.X, Row + Size.WindowOrigin.Y),
								new() {
									Foreground = Foreground,
									Background = Background,
									Blink = Blink,
									Underline = Underline,
									Inverse = Inverse
								}
							);
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_Clear",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							aTerminal.Clear();
							return mVM_Data.Empty();
						}
					)
				)
			]
		);
		try {
			aRun(mVM_Data.Interface(LoadSignature("Terminal"), Handle, Operations));
		} finally {
			Active = false;
		}
	}
}