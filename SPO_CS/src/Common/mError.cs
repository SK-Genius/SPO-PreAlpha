//IMPORT mStd.cs

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

public static class
mError {
	public sealed class
	tError<t> : System.Exception {
		public readonly t Value;
		
		internal
		tError(
			tText aMsg,
			t aValue
		) : base(aMsg) {
			this.Value = aValue;
		}
	}
	
	public static tError<t>
	Error<t>(
		tText aMsg,
		t aData
	) => new tError<t>(aMsg, aData);
	
	public static tError<mStd.tEmpty>
	Error(
		tText aMsg
	) => Error(aMsg, mStd.cEmpty);
}
