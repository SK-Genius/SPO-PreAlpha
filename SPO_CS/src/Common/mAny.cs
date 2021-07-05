//IMPORT mError.cs

#nullable enable

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

[System.Diagnostics.DebuggerStepThrough]
public static class
mAny {
	public readonly struct
	tAny {
		internal readonly object? _Value;
		
		internal tAny(object? aValue) {
			this._Value = aValue;
		}
		
		public tBool
		Equals(
			tAny a
		) => (
			this._Value is not null &&
			this._Value.Equals(a._Value)
		);
		
		public override tBool
		Equals(
			object? a
		) => a is tAny X && this.Equals(X);
	}
	
	public static tAny
	Any<t>(
		t a
	) => new tAny(a);
	
	internal static tBool
	Match<t>(
		this tAny a,
		out t aValue
	) {
#if DEBUG
		if (typeof(t) == typeof(tAny)) {
			throw mError.Error("");
		}
#endif
		mAssert.IsNotNull(a._Value);
		
		if (a._Value is t Value) {
			aValue = Value;
			return true;
		} else {
			aValue = default!;
			return false;
		}
	}
	
	internal static tBool
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
}
