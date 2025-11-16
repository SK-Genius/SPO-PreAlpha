public static class
mVector {
	[System.Flags]
	public enum t8Bits : tNat8 {
		None = 0,
		Bit0 = 1 << 0,
		Bit1 = 1 << 1,
		Bit2 = 1 << 2,
		Bit3 = 1 << 3,
		Bit4 = 1 << 4,
		Bit5 = 1 << 5,
		Bit6 = 1 << 6,
		Bit7 = 1 << 7,
	}
	
	extension (t8Bits __) {
		public static t8Bits operator|(
			t8Bits a1,
			t8Bits a2
		) => (t8Bits)(tNat8)(((tNat8)a1) | ((tNat8)a2));
		
		public static t8Bits operator&(
			t8Bits a1,
			t8Bits a2
		) => (t8Bits)(tNat8)(((tNat8)a1) & ((tNat8)a2));
		
		public static t8Bits operator^(
			t8Bits a1,
			t8Bits a2
		) => (t8Bits)(tNat8)(((tNat8)a1) ^ ((tNat8)a2));
		
		public static t8Bits operator~(
			t8Bits a
		) => (t8Bits)(tNat8)~(tNat8)a;
		
		public tBool
		All() => __ is (t8Bits.Bit0 | t8Bits.Bit1);
		
		public tBool
		Any() => __ is not t8Bits.None;
		
		public tBool
		None() => __ is t8Bits.None;
	}
	
	public readonly struct
	t2xBool {
		internal readonly t8Bits Bits;
		public tBool X => Bits.HasFlag(t8Bits.Bit0);
		public tBool Y => Bits.HasFlag(t8Bits.Bit1);
		
		internal t2xBool(
			t8Bits a
		) {
			this.Bits = a;
		}
		
		public static
		implicit operator t2xBool(
			(tBool X, tBool Y) a
		) => new (
			(a.X ? t8Bits.Bit0 : t8Bits.None) |
			(a.Y ? t8Bits.Bit1 : t8Bits.None)
		);
	}
	
	extension (t2xBool __) {
		public static t2xBool
		operator&(
			t2xBool a1,
			t2xBool a2
		) => new(a1.Bits & a2.Bits);
		
		public static t2xBool
		operator|(
			t2xBool a1,
			t2xBool a2
		) => new(a1.Bits | a2.Bits);
		
		public static t2xBool
		operator^(
			t2xBool a1,
			t2xBool a2
		) => new(a1.Bits ^ a2.Bits);
		
		public static t2xBool
		operator~(
			t2xBool a
		) => new(~a.Bits);
		
		public tBool
		All() => (__.Bits ^ (t8Bits.Bit0 | t8Bits.Bit1)).None();
		
		public tBool
		Any() => __.Bits.Any();
		
		public tBool
		None() => __.Bits.None();
	}
	
	public static t2xBool
	V2(
		tBool aBit0,
		tBool aBit1
	) => (aBit0, aBit1);
	
	public readonly struct
	t2xInt32 {
		public readonly tInt32 X;
		public readonly tInt32 Y;
		
		private t2xInt32(tInt32 aX, tInt32 aY) {
			this.X = aX;
			this.Y = aY;
		}
		
		public static
		implicit operator t2xInt32(
			(tInt32 X, tInt32 Y) __
		) => new (__.X, __.Y);
	}
	
	public static t2xInt32
	V2(
		tInt32 aX,
		tInt32 aY
	) => (aX, aY);
	
	public static t2xInt32
	V2(
		tInt32 a
	) => (a, a);
	
	public static t2xInt32
	Min(
		t2xInt32 a1,
		t2xInt32 a2
	) => (
		mMath.Min(a1.X, a2.X),
		mMath.Min(a1.Y, a2.Y)
	);
	
	public static t2xInt32
	Max(
		t2xInt32 a1,
		t2xInt32 a2
	) => (
		mMath.Max(a1.X, a2.X),
		mMath.Max(a1.Y, a2.Y)
	);
	
	public static t2xInt32
	Clamp(
		this t2xInt32 a,
		t2xInt32 aMin,
		t2xInt32 aMax
	) => Min(Max(aMin, a), aMax);
	
	extension (t2xInt32 a) {
		public static t2xInt32
		operator+(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X + a2.X,
			a1.Y + a2.Y
		);
		
		public static t2xInt32
		operator-(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X - a2.X,
			a1.Y - a2.Y
		);
		
		public static t2xInt32
		operator*(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X * a2.X,
			a1.Y * a2.Y
		);
		
		public static t2xInt32
		operator/(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X / a2.X,
			a1.Y / a2.Y
		);
		
		public static t2xInt32
		operator*(
			t2xInt32 a1,
			tInt32 a2
		) => a1 * V2(a2);
		
		public static t2xInt32
		operator*(
			tInt32 a1,
			t2xInt32 a2
		) => a2 * a1;
		
		public static t2xInt32
		operator/(
			t2xInt32 a1,
			tInt32 a2
		) => a1 / V2(a2);
		
		public static t2xInt32
		operator/(
			tInt32 a1,
			t2xInt32 a2
		) => V2(a1) / a2;
		
		public static t2xBool
		operator>=(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X >= a2.X,
			a1.Y >= a2.Y
		);
		
		public static t2xBool
		operator==(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X == a2.X,
			a1.Y == a2.Y
		);
		
		public static t2xBool
		operator!=(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X != a2.X,
			a1.Y != a2.Y
		);
		
		public static t2xBool
		operator<=(
			t2xInt32 a1,
			t2xInt32 a2
		) => a2 >= a1;
		
		public static t2xBool
		operator>(
			t2xInt32 a1,
			t2xInt32 a2
		) => (
			a1.X > a2.X,
			a1.Y > a2.Y
		);
		
		public static t2xBool
		operator<(
			t2xInt32 a1,
			t2xInt32 a2
		) => a2 > a1;
		
		public tInt32
		Prod() => a.X * a.Y;
		
		public tInt32
		Sum() => a.X + a.Y;
		
		public t2xInt32
		Abs() => (
			a.X.Abs(),
			a.Y.Abs()
		);
	}
}
