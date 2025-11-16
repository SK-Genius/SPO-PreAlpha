// IMPORT Common/mStd

public static class
mImTUI {
	public enum
	tColor : tNat8 {
		Black,
		Red,
		Green,
		Yellow,
		Blue,
		Magenta,
		Cyan,
		Gray,
		BrightBlack,
		BrightRed,
		BrightGreen,
		BrightYellow,
		BrightBlue,
		BrightMagenta,
		BrightCyan,
		BrightWhite,
	}

	[System.Flags]
	public enum
	tModifiers : tNat8 {
		None = 0,
		Bold = 1 << 0,
		Underline = 1 << 1,
		Strikethrough = 1 << 2,
		Blink = 1 << 3,
	}

	public readonly struct
	tStyle {
		public readonly tColor Foreground;
		public readonly tColor Background;
		public readonly tModifiers Modifiers;

		public tStyle(
			tColor aForeground,
			tColor aBackground,
			tModifiers aModifiers
		) {
			this.Foreground = aForeground;
			this.Background = aBackground;
			this.Modifiers = aModifiers;
		}

		public static readonly tStyle Default = new(
			tColor.Gray,
			tColor.Black,
			tModifiers.None
		);

		public tStyle
		WithForeground(
			tColor aColor
		) => new(aColor, this.Background, this.Modifiers);

		public tStyle
		WithBackground(
			tColor aColor
		) => new(this.Foreground, aColor, this.Modifiers);

		public tStyle
		WithModifiers(
			tModifiers aModifiers
		) => new(this.Foreground, this.Background, aModifiers);

		public readonly tBool
		IsSame(
			in tStyle aOther
		) => this.Foreground == aOther.Foreground &&
		this.Background == aOther.Background &&
		this.Modifiers == aOther.Modifiers;
	}

	public readonly struct
	t2xInt32 {
		public readonly tInt32 X;
		public readonly tInt32 Y;

		public t2xInt32(
			tInt32 aX,
			tInt32 aY
		) {
			this.X = aX;
			this.Y = aY;
		}

		public static readonly t2xInt32 Zero = new(0, 0);

		public static t2xInt32
		operator +(
			t2xInt32 aLeft,
			t2xInt32 aRight
		) => new(aLeft.X + aRight.X, aLeft.Y + aRight.Y);

		public static t2xInt32
		operator -(
			t2xInt32 aLeft,
			t2xInt32 aRight
		) => new(aLeft.X - aRight.X, aLeft.Y - aRight.Y);

		public static t2xInt32
		Max(
			t2xInt32 aLeft,
			t2xInt32 aRight
		) => new(
			System.Math.Max(aLeft.X, aRight.X),
			System.Math.Max(aLeft.Y, aRight.Y)
		);

		public static t2xInt32
		Min(
			t2xInt32 aLeft,
			t2xInt32 aRight
		) => new(
			System.Math.Min(aLeft.X, aRight.X),
			System.Math.Min(aLeft.Y, aRight.Y)
		);

		public readonly t2xInt32
		Clamp(
			t2xInt32 aMin,
			t2xInt32 aMax
		) => new(
			System.Math.Clamp(this.X, aMin.X, aMax.X),
			System.Math.Clamp(this.Y, aMin.Y, aMax.Y)
		);
	}

	public readonly struct
	tRectInt32 {
		public readonly t2xInt32 Pos;
		public readonly t2xInt32 Size;

		public tRectInt32(
			t2xInt32 aPos,
			t2xInt32 aSize
		) {
			this.Pos = aPos;
			this.Size = new(
				System.Math.Max(0, aSize.X),
				System.Math.Max(0, aSize.Y)
			);
		}

		public static readonly tRectInt32 Empty = new(t2xInt32.Zero, t2xInt32.Zero);

		public readonly t2xInt32 Min => this.Pos;

		public readonly t2xInt32 Max => new(
			this.Pos.X + this.Size.X,
			this.Pos.Y + this.Size.Y
		);

		public readonly tBool
		HasArea => this.Size.X > 0 && this.Size.Y > 0;

		public readonly tBool
		Contains(
			in t2xInt32 aPos
		) => aPos.X >= this.Min.X && aPos.X < this.Max.X &&
		aPos.Y >= this.Min.Y && aPos.Y < this.Max.Y;

		public readonly tRectInt32
		Translate(
			t2xInt32 aDelta
		) => new(this.Pos + aDelta, this.Size);

		public static tRectInt32
		Intersect(
			tRectInt32 aLeft,
			tRectInt32 aRight
		) {
			var Min = new t2xInt32(
				System.Math.Max(aLeft.Min.X, aRight.Min.X),
				System.Math.Max(aLeft.Min.Y, aRight.Min.Y)
			);
			var Max = new t2xInt32(
				System.Math.Min(aLeft.Max.X, aRight.Max.X),
				System.Math.Min(aLeft.Max.Y, aRight.Max.Y)
			);
			var Size = new t2xInt32(
				System.Math.Max(0, Max.X - Min.X),
				System.Math.Max(0, Max.Y - Min.Y)
			);
			return Size.X == 0 || Size.Y == 0 ? Empty : new tRectInt32(Min, Size);
		}
	}

	public struct
	tCell {
		public tChar Char;
		public tStyle Style;
		public tInt32 LayerValue;

		public static tCell
		Create(
			tChar aChar,
			tStyle aStyle,
			tInt32 aLayerValue
		) => new() {
			Char = aChar,
			Style = aStyle,
			LayerValue = aLayerValue,
		};

		public readonly tBool
		IsSame(
			in tCell aOther
		) => this.Char == aOther.Char &&
		this.Style.IsSame(aOther.Style) &&
		this.LayerValue == aOther.LayerValue;
	}

	public sealed class
	tCellBuffer {
		private readonly tCell[] Cells;

		public readonly tInt32 Width;
		public readonly tInt32 Height;

		public tCellBuffer(
			tInt32 aWidth,
			tInt32 aHeight
		) {
			if (aWidth <= 0 || aHeight <= 0) {
				throw new System.ArgumentOutOfRangeException();
			}

			this.Width = aWidth;
			this.Height = aHeight;
			this.Cells = new tCell[aWidth * aHeight];
		}

		public readonly t2xInt32 Size => new(this.Width, this.Height);

		private readonly tInt32
		GetIndex(
			t2xInt32 aPos
		) => aPos.Y * this.Width + aPos.X;

		internal ref tCell
		GetCellRef(
			t2xInt32 aPos
		) => ref this.Cells[this.GetIndex(aPos)];

		internal ref tCell
		GetCellRef(
			tInt32 aIndex
		) => ref this.Cells[aIndex];

		public void
		Fill(
			tCell aCell
		) => System.Array.Fill(this.Cells, aCell);

		public void
		CopyFrom(
			tCellBuffer aSource
		) {
			if (aSource.Width != this.Width || aSource.Height != this.Height) {
				throw new System.ArgumentException("buffer size mismatch");
			}

			System.Array.Copy(aSource.Cells, this.Cells, this.Cells.Length);
		}

		public void
		Write(
			t2xInt32 aPos,
			tChar aChar,
			in tStyle aStyle,
			tInt32 aLayerValue
		) {
			ref var Cell = ref this.GetCellRef(aPos);
			Cell.Char = aChar;
			Cell.Style = aStyle;
			Cell.LayerValue = aLayerValue;
		}
	}

	public sealed class
	tFrame {
		public readonly tCellBuffer Buffer;
		public readonly tRectInt32 ClipRect;
		public readonly tInt32 LayerValue;
		public readonly tStyle DefaultStyle;
		public readonly tStyle BackgroundStyle;
		public t2xInt32 CurrentSize;

		internal tFrame(
			tCellBuffer aBuffer,
			tRectInt32 aClipRect,
			tStyle aDefaultStyle,
			tStyle aBackgroundStyle,
			tInt32 aLayerValue
		) {
			this.Buffer = aBuffer;
			this.ClipRect = aClipRect;
			this.LayerValue = aLayerValue;
			this.DefaultStyle = aDefaultStyle;
			this.BackgroundStyle = aBackgroundStyle;
			this.CurrentSize = t2xInt32.Zero;
		}

		public readonly tBool
		HasArea => this.ClipRect.HasArea;

		public tFrame
		Clip(
			tRectInt32 aLocalRect,
			tStyle? aDefaultStyle = null,
			tStyle? aBackgroundStyle = null,
			tInt32? aLayerOverride = null
		) {
			var NextDefault = aDefaultStyle ?? this.DefaultStyle;
			var NextBackground = aBackgroundStyle ?? this.BackgroundStyle;
			var NextLayer = aLayerOverride ?? (this.LayerValue + 1);
			if (!this.HasArea) {
				return new tFrame(
					this.Buffer,
					tRectInt32.Empty,
					NextDefault,
					NextBackground,
					NextLayer
				);
			}

			var GlobalRect = aLocalRect.Translate(this.ClipRect.Pos);
			var Intersected = tRectInt32.Intersect(GlobalRect, this.ClipRect);
			return !Intersected.HasArea
			? new tFrame(this.Buffer, tRectInt32.Empty, NextDefault, NextBackground, NextLayer)
			: new tFrame(this.Buffer, Intersected, NextDefault, NextBackground, NextLayer);
		}

		public void
		Write(
			t2xInt32 aLocalPos,
			tChar aChar,
			tStyle? aStyle = null
		) {
			var Style = aStyle ?? this.DefaultStyle;
			this.WriteInternal(aLocalPos, aChar, Style);
		}

		public void
		Write(
			t2xInt32 aLocalPos,
			System.ReadOnlySpan<tChar> aText,
			tStyle? aStyle = null
		) {
			var Style = aStyle ?? this.DefaultStyle;
			for (var Offset = 0; Offset < aText.Length; Offset += 1) {
				this.WriteInternal(new t2xInt32(aLocalPos.X + Offset, aLocalPos.Y), aText[Offset], Style);
			}
		}

		public void
		Write(
			t2xInt32 aLocalPos,
			tText aText,
			tStyle? aStyle = null
		) => this.Write(aLocalPos, aText.AsSpan(), aStyle);

		public void
		Fill(
			tRectInt32 aLocalRect,
			tChar aChar,
			tStyle? aStyle = null
		) {
			if (!this.HasArea || !aLocalRect.HasArea) {
				return;
			}

			var GlobalRect = aLocalRect.Translate(this.ClipRect.Pos);
			var Intersected = tRectInt32.Intersect(GlobalRect, this.ClipRect);
			if (!Intersected.HasArea) {
				return;
			}

			var Style = aStyle ?? this.DefaultStyle;
			for (var y = 0; y < Intersected.Size.Y; y += 1) {
				for (var x = 0; x < Intersected.Size.X; x += 1) {
					var Global = new t2xInt32(
						Intersected.Pos.X + x,
						Intersected.Pos.Y + y
					);
					this.Buffer.Write(Global, aChar, Style, this.LayerValue);
				}
			}

			this.UpdateCurrentSize(aLocalRect.Pos + aLocalRect.Size);
		}

		public t2xInt32
		Close(
			tChar aFillChar = ' '
		) {
			if (!this.HasArea) {
				return this.CurrentSize;
			}

			for (var y = 0; y < this.ClipRect.Size.Y; y += 1) {
				for (var x = 0; x < this.ClipRect.Size.X; x += 1) {
					var Global = new t2xInt32(
						this.ClipRect.Pos.X + x,
						this.ClipRect.Pos.Y + y
					);
					ref var Cell = ref this.Buffer.GetCellRef(Global);
					if (Cell.LayerValue < this.LayerValue) {
						Cell.Char = aFillChar;
						Cell.Style = this.BackgroundStyle;
						Cell.LayerValue = this.LayerValue;
					}
				}
			}

			return this.CurrentSize;
		}

		private void
		WriteInternal(
			t2xInt32 aLocalPos,
			tChar aChar,
			in tStyle aStyle
		) {
			if (!this.TryTransform(aLocalPos, out var Global)) {
				return;
			}

			this.Buffer.Write(Global, aChar, aStyle, this.LayerValue);
			this.UpdateCurrentSize(aLocalPos + new t2xInt32(1, 1));
		}

		private readonly tBool
		TryTransform(
			t2xInt32 aLocalPos,
			out t2xInt32 aGlobalPos
		) {
			if (!this.HasArea || aLocalPos.X < 0 || aLocalPos.Y < 0) {
				aGlobalPos = t2xInt32.Zero;
				return false;
			}

			if (aLocalPos.X >= this.ClipRect.Size.X || aLocalPos.Y >= this.ClipRect.Size.Y) {
				aGlobalPos = t2xInt32.Zero;
				return false;
			}

			aGlobalPos = new t2xInt32(
				this.ClipRect.Pos.X + aLocalPos.X,
				this.ClipRect.Pos.Y + aLocalPos.Y
			);
			return true;
		}

		private void
		UpdateCurrentSize(
			t2xInt32 aExtent
		) {
			var MaxExtent = new t2xInt32(
				System.Math.Clamp(aExtent.X, 0, this.ClipRect.Size.X),
				System.Math.Clamp(aExtent.Y, 0, this.ClipRect.Size.Y)
			);
			this.CurrentSize = t2xInt32.Max(this.CurrentSize, MaxExtent);
		}
	}

	public interface
	tAnsiSink {
		void Write(System.ReadOnlySpan<tChar> aText);
	}

	public sealed class
	tTextWriterSink : tAnsiSink {
		private readonly System.IO.TextWriter Writer;

		public tTextWriterSink(
			System.IO.TextWriter aWriter
		) => this.Writer = aWriter;

		public void
		Write(
			System.ReadOnlySpan<tChar> aText
		) => this.Writer.Write(aText);
	}

	public sealed class
	tDoubleBuffer {
		private readonly tCellBuffer Front;
		private readonly tCellBuffer Back;
		private tStyle CurrentTerminalStyle = tStyle.Default;

		public tDoubleBuffer(
			tInt32 aWidth,
			tInt32 aHeight
		) {
			this.Front = new tCellBuffer(aWidth, aHeight);
			this.Back = new tCellBuffer(aWidth, aHeight);
		}

		public readonly t2xInt32 Size => this.Front.Size;

		public tFrame
		BeginFrame(
			tStyle? aStyle = null,
			tStyle? aBackground = null,
			tInt32 aLayerValue = 0
		) {
			var DefaultStyle = aStyle ?? tStyle.Default;
			var BackgroundStyle = aBackground ?? DefaultStyle;
			this.Back.Fill(tCell.Create(' ', BackgroundStyle, aLayerValue - 1));
			return new tFrame(
				this.Back,
				new tRectInt32(t2xInt32.Zero, this.Back.Size),
				DefaultStyle,
				BackgroundStyle,
				aLayerValue
			);
		}

		public void
		Present(
			tAnsiSink aSink
		) {
			System.ArgumentNullException.ThrowIfNull(aSink);

			var Builder = new System.Text.StringBuilder();
			Builder.Append("\u001B[0m");
			this.CurrentTerminalStyle = tStyle.Default;

			var Width = this.Front.Width;
			var Height = this.Front.Height;
			for (var y = 0; y < Height; y += 1) {
				var RowIndex = y * Width;
				var x = 0;
				while (x < Width) {
					var Index = RowIndex + x;
					var BackCell = this.Back.GetCellRef(Index);
					var FrontCell = this.Front.GetCellRef(Index);
					if (FrontCell.IsSame(BackCell)) {
						x += 1;
						continue;
					}

					var RunStyle = BackCell.Style;
					var RunStart = x;
					var RunEnd = x + 1;
					while (RunEnd < Width) {
						var RunIndex = RowIndex + RunEnd;
						var NextBack = this.Back.GetCellRef(RunIndex);
						var NextFront = this.Front.GetCellRef(RunIndex);
						if (NextFront.IsSame(NextBack) || !NextBack.Style.IsSame(RunStyle)) {
							break;
						}
						RunEnd += 1;
					}

					this.EmitCursorMove(Builder, RunStart, y);
					this.EmitStyle(Builder, RunStyle);

					for (var i = RunStart; i < RunEnd; i += 1) {
						var CellIndex = RowIndex + i;
						var Cell = this.Back.GetCellRef(CellIndex);
						ref var FrontTarget = ref this.Front.GetCellRef(CellIndex);
						FrontTarget = Cell;
						Builder.Append(Cell.Char);
					}

					x = RunEnd;
				}
			}

			Builder.Append("\u001B[0m");
			aSink.Write(Builder.ToString());
		}

		private static void
		EmitCursorMove(
			System.Text.StringBuilder aBuilder,
			tInt32 aColumn,
			tInt32 aRow
		) {
			aBuilder.Append('\u001B');
			aBuilder.Append('[');
			AppendInt(aBuilder, aRow + 1);
			aBuilder.Append(';');
			AppendInt(aBuilder, aColumn + 1);
			aBuilder.Append('H');
		}

		private void
		EmitStyle(
			System.Text.StringBuilder aBuilder,
			in tStyle aStyle
		) {
			if (this.CurrentTerminalStyle.IsSame(aStyle)) {
				return;
			}

			this.CurrentTerminalStyle = aStyle;
			aBuilder.Append("\u001B[0m");
			aBuilder.Append('\u001B');
			aBuilder.Append('[');
			var First = true;

			void AppendCode(tInt32 aCode) {
				if (!First) {
					aBuilder.Append(';');
				}
				First = false;
				AppendInt(aBuilder, aCode);
			}

			AppendCode(this.GetAnsiCode(aStyle.Foreground, false));
			AppendCode(this.GetAnsiCode(aStyle.Background, true));

			if ((aStyle.Modifiers & tModifiers.Bold) != 0) {
				AppendCode(1);
			}
			if ((aStyle.Modifiers & tModifiers.Underline) != 0) {
				AppendCode(4);
			}
			if ((aStyle.Modifiers & tModifiers.Strikethrough) != 0) {
				AppendCode(9);
			}
			if ((aStyle.Modifiers & tModifiers.Blink) != 0) {
				AppendCode(5);
			}

			aBuilder.Append('m');
		}

		private tInt32
		GetAnsiCode(
			tColor aColor,
			tBool aIsBackground
		) {
			var Base = aIsBackground ? 40 : 30;
			var BrightBase = aIsBackground ? 100 : 90;
			var Index = (tInt32)aColor;
			return Index < 8 ? Base + Index : BrightBase + (Index - 8);
		}

		private static void
		AppendInt(
			System.Text.StringBuilder aBuilder,
			tInt32 aValue
		) => aBuilder.Append(aValue);
	}
}
