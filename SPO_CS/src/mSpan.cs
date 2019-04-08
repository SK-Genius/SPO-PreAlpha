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
mSpan {
	public struct
	tSpan<tPos> {
		public tPos Start;
		public tPos End;

		public override tText
		ToString(
		) => $"{this.Start}..{this.End}";
	}

	public static tSpan<tPos>
	Span<tPos>(
		tPos aStart,
		tPos aEnd
	) => new tSpan<tPos> {
		Start = aStart,
		End = aEnd
	};

	public static tSpan<tPos>
	Span<tPos>(
		tPos aPos
	) => Span(aPos, aPos);

	public static tSpan<tPos>
	Merge<tPos>(
		tSpan<tPos> a1,
		tSpan<tPos> a2
	) {
		if (a1.Equals(default(tSpan<tPos>))) {
			return a2;
		}
		if (a2.Equals(default(tSpan<tPos>))) {
			return a1;
		}
		return new tSpan<tPos> {
			Start = a1.Start,
			End = a2.End
		};
	}
}
