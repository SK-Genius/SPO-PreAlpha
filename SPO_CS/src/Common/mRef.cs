// IMPORT mStd
// IMPORT mMaybe

public static class
mRef {
	public sealed class
	tRefBox<t> {
		public t _Value;
	}
	
	public readonly struct
	tRef<t> {
		public readonly tRefBox<t>? _Box;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tRef(
		) {
			this._Box = null;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tRef(
			t aValue
		) {
			this._Box = new () { _Value = aValue };
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static implicit operator tRef<t>(
			t a
		) => new(a);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public static implicit operator tRef<t>(
			mStd.tEmpty _
		) => new();
		
		public readonly mMaybe.tMaybe<t> Deref {
			[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
			get => this.Is(out var Value) ? Value : mStd.cEmpty;
		}
		
		public static tBool operator==(
			tRef<t> a1,
			tRef<t> a2
		) => a1._Box == a2._Box;
		
		public static tBool operator!=(
			tRef<t> a1,
			tRef<t> a2
		) => a1._Box != a2._Box;
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tRef<t>
	Ref<t>(
		t a
	) => new(a);
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tRef<t>
	NullRef<t>(
	) => new();
	
	extension<t> (tRef<t> a) {
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		IsRefEqual(
			tRef<t> a2
		) => ReferenceEquals(a._Box, a2._Box);
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		Is(
			out t aValue
		) {
			if (a._Box is null) {
				aValue = default!;
				return false;
			} else {
				aValue = a._Box._Value;
				return true;
			}
		}
		
		[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		public tBool
		IsEmpty(
		) => a._Box is null;
	}
}
