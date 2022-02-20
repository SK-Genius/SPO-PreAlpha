#nullable enable

[DebuggerStepThrough]
public static class mLazy {
	[Pure]
	[DebuggerStepThrough]
	public sealed class
	tLazy<t> {
		private mStd.tFunc<t>? _Func;
		private t _Value;
		
		[Pure]
		public t Value {           
			get {                     
				if (this._Func is not null) {
					this._Value = this._Func();
					this._Func = null;      
				}                        
				return this._Value;      
			}
		}
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal
		tLazy(
			t a
		) {
			this._Value = a;
			this._Func = null;
		}
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal
		tLazy(
			mStd.tFunc<t> a
		) {
			this._Func = a;
			this._Value = default!;
		}
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator tLazy<t>(t a) => Lazy(() => a);
		
		[Pure]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator tLazy<t>(mStd.tFunc<t> a) => Lazy(a);
	}
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tLazy<t>
	NonLazy<t>(
		t a
	) => new(a);
	
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tLazy<t>
	Lazy<t>(
		mStd.tFunc<t> a
	) => new(a);
}