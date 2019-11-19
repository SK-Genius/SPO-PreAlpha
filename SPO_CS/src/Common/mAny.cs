//IMPORT mError.cs

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

public static class
mAny {
		#region tVar
		
		public readonly struct
		tAny {
			internal readonly object _Value;
			
			internal tAny(object aValue) {
				_Value = aValue;
			}
			
			public tBool
			Equals(
				tAny a
			) => (
				!(this._Value is null) &&
				this._Value.Equals(a._Value)
			);
			
			override public tBool
			Equals(
				object a
			) => (a is tAny X) && this.Equals(X);
		}
		
		public static tAny
		Any<t>(
			t a
		) => new tAny(a);
		
		public static tBool
		Match<t>(
			this tAny a,
			out t aValue
		) {
#if DEBUG
			if (typeof(t) == typeof(tAny)) {
				throw mError.Error("");
			}
#endif
			
			if (a._Value is null || a._Value is t) {
				aValue = (t)a._Value;
				return true;
			} else {
				aValue = default;
				return false;
			}
		}
		
		public static tBool
		Match(
			this tAny a
		) => a._Value is null;
		
		public static t
		To<t>(
			this tAny a
		) => (
			a.Match(out t Result)
			? Result
			: throw mError.Error($"To: {typeof(t).FullName} <- {a}")
		);
		
	#endregion
}
