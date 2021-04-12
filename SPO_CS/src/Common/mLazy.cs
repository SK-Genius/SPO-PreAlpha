#nullable enable

using System.Diagnostics.CodeAnalysis;

using tBool = System.Boolean;

using tNat8 = System.Byte;
using tNat16 = System.UInt16;
using tNat32 = System.UInt32;
using tNat64 = System.UInt64;

using tInt8 = System.SByte;
using tInt16 = System.Int16;
using tInt32 = System.Int32;
using tInt64 = System.Int64;

using tChar = System.Char;
using tText = System.String;

public static class mLazy {
	public sealed class
	tLazy<t> {
		private mStd.tFunc<t>? _Func;
		private t _Value;
		
		public t Value {           
			get {                     
				if (this._Func is not null) {
					this._Value = this._Func();
					this._Func = null;      
				}                        
				return this._Value;      
			}
		}
		
		internal
		tLazy(
			t a
		) {
			this._Value = a;
			this._Func = null;
		}
		
		internal
		tLazy(
			mStd.tFunc<t> a
		) {
			this._Func = a;
			this._Value = default!;
		}
		
		public static implicit operator tLazy<t>(t a) => Lazy(() => a);
		public static implicit operator tLazy<t>(mStd.tFunc<t> a) => Lazy(a);
	}
	
	public static tLazy<t>
	NonLazy<t>(
		t a
	) => new tLazy<t>(a);
	
	public static tLazy<t>
	Lazy<t>(
		mStd.tFunc<t> a
	) => new tLazy<t>(a);
}