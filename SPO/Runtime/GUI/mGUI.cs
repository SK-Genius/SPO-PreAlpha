#!dotnet
#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include ../../../SPO_CS/src/_GlobalUsings.cs
#:ref ../mRuntime.cs
#:ref ../../../SPO_CS/src/mSPO_Interpreter.cs

public static class
mGUI {
	public readonly record struct
	tRect(
		tInt32 X,
		tInt32 Y,
		tInt32 Width,
		tInt32 Height
	);
	
	public sealed record
	tSurface {
		public required mStd.tFunc<(tInt32 Width, tInt32 Height)> Size { get; init; }
		public required mStd.tAction<(tNat8 R, tNat8 G, tNat8 B)> Clear { get; init; }
		public required mStd.tAction<tRect, (tNat8 R, tNat8 G, tNat8 B), tBool> Rectangle { get; init; }
		public required mStd.tAction<tText, (tInt32 X, tInt32 Y), (tNat8 R, tNat8 G, tNat8 B)> Text { get; init; }
		public required mStd.tAction<tRect?> Clip { get; init; }
		public required mStd.tAction<tText> Title { get; init; }
		public required mStd.tAction Present { get; init; }
		public required mStd.tFunc<mVM_Data.tData> ReadEvent { get; init; }
	}
	
	public static tInt32
	Int(
		this mVM_Data.tData aValue
	) {
		mAssert.IsTrue(aValue.IsInt(out var Value));
		return Value;
	}
	
	public static tText
	Text(
		this mVM_Data.tData aValue
	) {
		var Buffer = new System.Text.StringBuilder();
		while (aValue.IsPair(out var Prefix, out var Character)) {
			mAssert.IsTrue(Character.IsPrefix("_Char...", out var Ordinal));
			Buffer.Append(checked((tChar)Ordinal.Int()));
			aValue = Prefix;
		}
		
		mAssert.IsTrue(aValue.IsEmpty());
		var Characters = Buffer.ToString().ToCharArray();
		System.Array.Reverse(Characters);
		return new(Characters);
	}
	
	public static mVM_Data.tData
	Text(
		tText aText
	) {
		var Result = mVM_Data.Empty();
		foreach (var Character in aText) {
			Result = mVM_Data.Pair(Result, mVM_Data.Prefix("_Char...", mVM_Data.Int(Character)));
		}
		
		return Result;
	}
	
	public static tRect
	Rect(
		this mVM_Data.tData aValue
	) => new(
		aValue.Field("X").Int(),
		aValue.Field("Y").Int(),
		aValue.Field("Width").Int(),
		aValue.Field("Height").Int()
	);
	
	public static mVM_Type.tType
	Signature(
		tText aName
	) => mSPO_Interpreter.Run(
		$"§EXPORT [§LOAD \"../../Libs/GUI/{aName}.SIG\"]",
		mStd.File(),
		(mVM_Data.Empty(), mVM_Type.Empty()),
		_ => {}
	).ElseThrow().Data.SignatureValue();
	
	public static void
	WithCapability(
		this tSurface aSurface,
		mStd.tAction<mVM_Data.tData> aRun
	) {
		var Active = true;
		var Handle = mVM_Data.Var(mVM_Data.Empty());
		
		mVM_Data.tData
		Bind(
			mStd.tFunc<mVM_Data.tData, mVM_Data.tData> aOperation
		) => mVM_Data.ExternProc(
			(_, aObject, aArgument, _) => {
				if (!Active || !ReferenceEquals(aObject, Handle)) {
					throw new System.InvalidOperationException("Invalid or expired GUI capability.");
				}
				
				return aOperation(aArgument);
			},
			mVM_Data.Empty()
		);
		
		(tNat8, tNat8, tNat8)
		Color(
			mVM_Data.tData aValue
		) {
			mAssert.IsTrue(aValue.IsTuple(out var R, out var G, out var B));
			return (checked((tNat8)R.Int()), checked((tNat8)G.Int()), checked((tNat8)B.Int()));
		}
		
		mVM_Data.tData
		Rectangle(
			mVM_Data.tData aArgument,
			tBool aFilled
		) {
			mAssert.IsTrue(aArgument.IsTuple(out var Bounds, out var RGB));
			var Rect = Bounds.Rect();
			if (Rect.Width > 0 && Rect.Height > 0) {
				aSurface.Rectangle(Rect, Color(RGB), aFilled);
			}
			
			return mVM_Data.Empty();
		}
		
		var Operations = mVM_Data.Record(
			[
				(
					"_Size",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							var Size = aSurface.Size();
							return mVM_Data.Record(
								[
									("_Width", mVM_Data.Int(Size.Width)),
									(
										"_Height",
										mVM_Data.Int(
											Size.Height
										)
									)
								]
							);
						}
					)
				),
				(
					"_ClearWith...",
					Bind(
						a => {
							aSurface.Clear(Color(a));
							return mVM_Data.Empty();
						}
					)
				),
				("_Fill...With...", Bind(a => Rectangle(a, true))),
				("_Stroke...With...", Bind(a => Rectangle(a, false))),
				(
					"_Draw...At...InColor...",
					Bind(
						a => {
							mAssert.IsTrue(a.IsTuple(out var Text, out var Position, out var RGB));
							mAssert.IsTrue(Position.IsTuple(out var X, out var Y));
							aSurface.Text(Text.Text(), (X.Int(), Y.Int()), Color(RGB));
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_ClipTo...",
					Bind(
						a => {
							var Bounds = a.Rect();
							aSurface.Clip(
								Bounds with {
									Width = System.Math.Max(0, Bounds.Width),
									Height = System.Math.Max(0, Bounds.Height)
								}
							);
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_ResetClip",
					Bind(
						a => {
							mAssert.IsTrue(a.IsEmpty());
							aSurface.Clip(null);
							return mVM_Data.Empty();
						}
					)
				),
				(
					"_SetTitle...",
					Bind(
						a => {
							aSurface.Title(a.Text());
							return mVM_Data.Empty();
						}
					)
				)
			]
		);
		try {
			aRun(mVM_Data.Interface(Signature("Surface"), Handle, Operations));
		} finally {
			Active = false;
		}
	}
}