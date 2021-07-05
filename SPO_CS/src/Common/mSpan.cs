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
mSpan {
	public readonly struct
	tSpan<tPos> {
		public readonly tPos Start;
		public readonly tPos End;
		
		internal tSpan(
			tPos aStart,
			tPos aEnd
		) {
			this.Start = aStart;
			this.End = aEnd;
		}
		
		public override readonly tText
		ToString(
		) => $"{this.Start}..{this.End}";
	}
	
	public static tSpan<tPos>
	Span<tPos>(
		tPos aStart,
		tPos aEnd
	) => new tSpan<tPos>(
		aStart: aStart,
		aEnd: aEnd
	);
	
	public static tSpan<tPos>
	Span<tPos>(
		tPos aPos
	) => Span(aPos, aPos);
	
	public static tSpan<tPos>
	Merge<tPos>(
		tSpan<tPos> a1,
		tSpan<tPos> a2
	) => (
		a1.Equals(default(tSpan<tPos>)) ? a2 :
		a2.Equals(default(tSpan<tPos>)) ? a1 :
		Span(a1.Start, a2.End)
	);
}
