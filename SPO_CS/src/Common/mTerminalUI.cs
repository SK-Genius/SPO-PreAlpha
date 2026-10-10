// IMPORT mStd
// IMPORT mMath
// IMPORT mError

public static class
mTerminalUI {
	public sealed class
	tContext {
		internal readonly System.Collections.Generic.Dictionary<tText, tWindowState> _Windows;
		internal tNat32 _FrameIndex;
		internal tText? _FocusedWindowId;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tContext() {
			this._Windows = new System.Collections.Generic.Dictionary<tText, tWindowState>(System.StringComparer.Ordinal);
			this._FrameIndex = 0;
			this._FocusedWindowId = null;
		}
	}
	
	private sealed class
	tWindowState {
		internal tInt32 X = 1;
		internal tInt32 Y = 1;
		internal tInt32 Width = 32;
		internal tInt32 Height = 8;
	}
	
	public readonly struct
	tFrameInput {
		public readonly tInt32 PointerX;
		public readonly tInt32 PointerY;
		public readonly tBool PointerDown;
		public readonly tBool PointerClicked;
		public readonly tBool PointerReleased;
		public readonly tText? Command;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tFrameInput(
		tInt32 aPointerX,
		tInt32 aPointerY,
		tBool aPointerDown,
		tBool aPointerClicked,
		tBool aPointerReleased,
		tText? aCommand
		) {
			this.PointerX = aPointerX;
			this.PointerY = aPointerY;
			this.PointerDown = aPointerDown;
			this.PointerClicked = aPointerClicked;
			this.PointerReleased = aPointerReleased;
			this.Command = aCommand;
		}
	}
	
	public readonly struct
	tWindowOptions {
		public readonly tText? Title;
		public readonly tInt32? X;
		public readonly tInt32? Y;
		public readonly tInt32? Width;
		public readonly tInt32? Height;
		public readonly tBool LockWithinFrame;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tWindowOptions(
		tText? aTitle,
		tInt32? aX,
		tInt32? aY,
		tInt32? aWidth,
		tInt32? aHeight,
		tBool aLockWithinFrame
		) {
			this.Title = aTitle;
			this.X = aX;
			this.Y = aY;
			this.Width = aWidth;
			this.Height = aHeight;
			this.LockWithinFrame = aLockWithinFrame;
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static tWindowOptions
		Default(
		tText? aTitle = null
		) => new(
		aTitle,
		null,
		null,
		null,
		null,
		true
		);
	}
	
	public sealed class
	tFrame {
		internal readonly tContext _Context;
		internal readonly tFrameInput _Input;
		internal readonly tNat32 _Width;
		internal readonly tNat32 _Height;
		internal readonly tChar[] _Buffer;
		internal readonly System.Collections.Generic.List<tWindowInfo> _WindowInfos;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tFrame(
		tContext aContext,
		tNat32 aWidth,
		tNat32 aHeight,
		tFrameInput aInput
		) {
			this._Context = aContext;
			this._Input = aInput;
			this._Width = aWidth;
			this._Height = aHeight;
			var BufferSize = (tInt32)(aWidth * aHeight);
			this._Buffer = new tChar[BufferSize];
			for (var i = 0; i < BufferSize; ++i) {
				this._Buffer[i] = ' ';
			}
			this._WindowInfos = new System.Collections.Generic.List<tWindowInfo>();
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tWindow
		BeginWindow(
		tText aId,
		tWindowOptions aOptions
		) {
			if (!this._Context._Windows.TryGetValue(aId, out var State)) {
				State = new tWindowState();
				this._Context._Windows[aId] = State;
			}
			
			if (aOptions.Width.HasValue) {
				State.Width = aOptions.Width.Value;
			}
			
			if (aOptions.Height.HasValue) {
				State.Height = aOptions.Height.Value;
			}
			
			if (aOptions.X.HasValue) {
				State.X = aOptions.X.Value;
			}
			
			if (aOptions.Y.HasValue) {
				State.Y = aOptions.Y.Value;
			}
			
			State.Width = mMath.Max(8, State.Width);
			State.Height = mMath.Max(4, State.Height);
			
			var FrameWidth = (tInt32)this._Width;
			var FrameHeight = (tInt32)this._Height;
			
			if (aOptions.LockWithinFrame) {
				var MaxX = FrameWidth - State.Width;
				var MaxY = FrameHeight - State.Height;
				State.X = mMath.Max(0, mMath.Min(State.X, MaxX));
				State.Y = mMath.Max(0, mMath.Min(State.Y, MaxY));
			}
			
			
			var WindowTitle = aOptions.Title ?? aId;
			
			this.DrawWindowFrame(State.X, State.Y, State.Width, State.Height, WindowTitle);
			
			var ContentX = State.X + 1;
			var ContentY = State.Y + 1;
			var ContentWidth = State.Width - 2;
			var ContentHeight = State.Height - 2;
			
			var IsHovered = this.IsPointerInside(State.X, State.Y, State.Width, State.Height);
			
			if (IsHovered && this._Input.PointerClicked) {
				this._Context._FocusedWindowId = aId;
			}
			
			
			var InfoIndex = this._WindowInfos.Count;
			this._WindowInfos.Add(new tWindowInfo(
			aId,
			State.X,
			State.Y,
			State.Width,
			State.Height,
			this._Context._FocusedWindowId == aId,
			IsHovered
			));
			
			return new tWindow(
			this,
			aId,
			State,
			aOptions,
			ContentX,
			ContentY,
			ContentWidth,
			ContentHeight,
			IsHovered,
			InfoIndex
			);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal void
		WriteChar(
		tInt32 aX,
		tInt32 aY,
		tChar aChar
		) {
			var Width = (tInt32)this._Width;
			var Height = (tInt32)this._Height;
			if (aX < 0 || aY < 0 || aX >= Width || aY >= Height) {
				return;
			}
			
			this._Buffer[aX + aY * Width] = aChar;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal void
		WriteText(
		tInt32 aX,
		tInt32 aY,
		tText aText,
		tInt32 aMaxWidth
		) {
			if (aMaxWidth <= 0) {
				return;
			}
			
			var Width = (tInt32)this._Width;
			var Height = (tInt32)this._Height;
			if (aX < 0 || aY < 0 || aX >= Width || aY >= Height) {
				return;
			}
			
			var CharCount = mMath.Min(aMaxWidth, aText.Length);
			var Offset = aX + aY * Width;
			var MaxOffset = Width * Height;
			for (var i = 0; i < CharCount; ++i) {
				var Index = Offset + i;
				if (Index >= MaxOffset) {
					break;
				}
				this._Buffer[Index] = aText[i];
			}
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tBool
		IsPointerInside(
		tInt32 aX,
		tInt32 aY,
		tInt32 aWidth,
		tInt32 aHeight
		) {
			if (!this._Input.PointerDown && !this._Input.PointerClicked && !this._Input.PointerReleased) {
				return false;
			}
			
			var Px = this._Input.PointerX;
			var Py = this._Input.PointerY;
			if (Px < 0 || Py < 0) {
				return false;
			}
			
			return (
			Px >= aX &&
			Py >= aY &&
			Px < aX + aWidth &&
			Py < aY + aHeight
			);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		private void
		DrawWindowFrame(
		tInt32 aX,
		tInt32 aY,
		tInt32 aWidth,
		tInt32 aHeight,
		tText? aTitle
		) {
			if (aWidth <= 0 || aHeight <= 0) {
				return;
			}
			
			var Right = aX + aWidth - 1;
			var Bottom = aY + aHeight - 1;
			
			this.WriteChar(aX, aY, '┌');
			this.WriteChar(Right, aY, '┐');
			this.WriteChar(aX, Bottom, '└');
			this.WriteChar(Right, Bottom, '┘');
			
			for (var x = aX + 1; x < Right; ++x) {
				this.WriteChar(x, aY, '─');
				this.WriteChar(x, Bottom, '─');
			}
			
			for (var y = aY + 1; y < Bottom; ++y) {
				this.WriteChar(aX, y, '│');
				this.WriteChar(Right, y, '│');
			}
			
			if (!tText.IsNullOrEmpty(aTitle) && aWidth > 2) {
				var Title = " " + aTitle + " ";
				var MaxWidth = aWidth - 2;
				if (Title.Length > MaxWidth) {
					Title = Title[..MaxWidth];
				}
				var TitleStart = aX + ((MaxWidth - Title.Length) / 2) + 1;
				this.WriteText(TitleStart, aY, Title, Title.Length);
			}
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tFrameResult
		EndFrame(
		) {
			var Width = (tInt32)this._Width;
			var Height = (tInt32)this._Height;
			
			var Lines = new tText[Height];
			for (var y = 0; y < Height; ++y) {
				Lines[y] = new string(this._Buffer, y * Width, Width);
			}
			
			var Focused = this._Context._FocusedWindowId;
			var HasFocusedWindow = false;
			for (var i = 0; i < this._WindowInfos.Count; ++i) {
				if (Focused is not null && this._WindowInfos[i].Id == Focused) {
					HasFocusedWindow = true;
					break;
				}
			}
			
			if (!HasFocusedWindow) {
				Focused = null;
				this._Context._FocusedWindowId = null;
			}
			
			var WindowCount = this._WindowInfos.Count;
			var Windows = new tWindowInfo[WindowCount];
			for (var i = 0; i < WindowCount; ++i) {
				Windows[i] = this._WindowInfos[i];
			}
			
			return new tFrameResult(
			this._Context._FrameIndex,
			Focused,
			Windows,
			Lines
			);
		}
	}
	
	public sealed class
	tWindow {
		private readonly tFrame _Frame;
		private readonly tText _Id;
		private readonly tWindowState _State;
		private readonly tWindowOptions _Options;
		private readonly tInt32 _BodyX;
		private readonly tInt32 _BodyY;
		private readonly tInt32 _BodyWidth;
		private readonly tInt32 _BodyHeight;
		private readonly tBool _IsHovered;
		private readonly tInt32 _InfoIndex;
		private tInt32 _CursorX;
		private tInt32 _CursorY;
		private tBool _IsOpen;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal tWindow(
		tFrame aFrame,
		tText aId,
		tWindowState aState,
		tWindowOptions aOptions,
		tInt32 aBodyX,
		tInt32 aBodyY,
		tInt32 aBodyWidth,
		tInt32 aBodyHeight,
		tBool aIsHovered,
		tInt32 aInfoIndex
		) {
			this._Frame = aFrame;
			this._Id = aId;
			this._State = aState;
			this._Options = aOptions;
			this._BodyX = aBodyX;
			this._BodyY = aBodyY;
			this._BodyWidth = aBodyWidth;
			this._BodyHeight = aBodyHeight;
			this._IsHovered = aIsHovered;
			this._InfoIndex = aInfoIndex;
			this._CursorX = 0;
			this._CursorY = 0;
			this._IsOpen = true;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		private void
		AssertOpen(
		) {
			if (!this._IsOpen) {
				throw mError.Error("Window already closed.");
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		private tBool
		HasSpace(
		) => this._BodyWidth > 0 && this._BodyHeight > 0;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		Label(
		tText aText
		) {
			this.AssertOpen();
			if (!this.HasSpace()) {
				return;
			}
			
			var MaxWidth = this._BodyWidth - this._CursorX;
			if (MaxWidth <= 0) {
				this.NextLine();
				return;
			}
			
			this._Frame.WriteText(
			this._BodyX + this._CursorX,
			this._BodyY + this._CursorY,
			aText,
			MaxWidth
			);
			
			this.NextLine();
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		Spacing(
		tInt32 aCount = 1
		) {
			this.AssertOpen();
			if (aCount <= 0) {
				return;
			}
			
			for (var i = 0; i < aCount; ++i) {
				this.NextLine();
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		Separator(
		) {
			this.AssertOpen();
			if (!this.HasSpace()) {
				return;
			}
			
			var Line = new string('─', mMath.Max(0, this._BodyWidth));
			this._Frame.WriteText(
			this._BodyX,
			this._BodyY + this._CursorY,
			Line,
			this._BodyWidth
			);
			this.NextLine();
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Button(
		tText aLabel
		) {
			this.AssertOpen();
			if (!this.HasSpace()) {
				return false;
			}
			
			var ButtonText = "[" + aLabel + "]";
			var MaxWidth = this._BodyWidth - this._CursorX;
			if (MaxWidth <= 0) {
				this.NextLine();
				return false;
			}
			
			if (ButtonText.Length > MaxWidth) {
				ButtonText = ButtonText[..MaxWidth];
			}
			
			var X = this._BodyX + this._CursorX;
			var Y = this._BodyY + this._CursorY;
			var Hovered = this._Frame.IsPointerInside(X, Y, ButtonText.Length, 1);
			var Decorated = Hovered ? ">" + aLabel + "<" : ButtonText;
			if (Decorated.Length > MaxWidth) {
				Decorated = Decorated[..MaxWidth];
			}
			
			this._Frame.WriteText(
			X,
			Y,
			Decorated,
			MaxWidth
			);
			
			this.NextLine();
			
			return Hovered && this._Frame._Input.PointerClicked;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		SameLine(
		tInt32 aSpacing = 1
		) {
			this.AssertOpen();
			this._CursorX = mMath.Max(0, this._CursorX + aSpacing);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public void
		End(
		) {
			this.AssertOpen();
			this._IsOpen = false;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		private void
		NextLine(
		) {
			this._CursorX = 0;
			this._CursorY += 1;
			if (this._CursorY >= this._BodyHeight) {
				this._CursorY = this._BodyHeight - 1;
			}
		}
	}
	
	public readonly struct
	tWindowInfo {
		public readonly tText Id;
		public readonly tInt32 X;
		public readonly tInt32 Y;
		public readonly tInt32 Width;
		public readonly tInt32 Height;
		public readonly tBool IsFocused;
		public readonly tBool IsHovered;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tWindowInfo(
		tText aId,
		tInt32 aX,
		tInt32 aY,
		tInt32 aWidth,
		tInt32 aHeight,
		tBool aIsFocused,
		tBool aIsHovered
		) {
			this.Id = aId;
			this.X = aX;
			this.Y = aY;
			this.Width = aWidth;
			this.Height = aHeight;
			this.IsFocused = aIsFocused;
			this.IsHovered = aIsHovered;
		}
	}
	
	public readonly struct
	tFrameResult {
		public readonly tNat32 FrameIndex;
		public readonly tText? FocusedWindowId;
		public readonly tWindowInfo[] Windows;
		public readonly tText[] Lines;
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tFrameResult(
		tNat32 aFrameIndex,
		tText? aFocusedWindowId,
		tWindowInfo[] aWindows,
		tText[] aLines
		) {
			this.FrameIndex = aFrameIndex;
			this.FocusedWindowId = aFocusedWindowId;
			this.Windows = aWindows;
			this.Lines = aLines;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tContext
	Context(
	) => new();
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tFrame
	BeginFrame(
	tContext aContext,
	tNat32 aWidth,
	tNat32 aHeight,
	tFrameInput aInput
	) {
		var FrameIndex = aContext._FrameIndex + 1;
		aContext._FrameIndex = FrameIndex;
		return new tFrame(aContext, aWidth, aHeight, aInput);
	}
}
