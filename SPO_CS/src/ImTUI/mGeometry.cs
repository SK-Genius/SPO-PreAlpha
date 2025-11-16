public static class
mGeometry {
	public readonly struct
	tRect_Int32 {
		public readonly mVector.t2xInt32 Pos;
		public readonly mVector.t2xInt32 Size;
		
		public tRect_Int32(
			mVector.t2xInt32 aPos,
			mVector.t2xInt32 aSize
		) {
			this.Pos = aPos;
			this.Size = aSize;
		}
	}
	
	public static tRect_Int32
	BB(
		mVector.t2xInt32 a1,
		mVector.t2xInt32 a2
	) {
		var Min = mVector.Min(a1, a2);
		var Max = mVector.Max(a1, a2);
		return new (
			Min,
			Max - Min + (1, 1)
		);
	}
	
	extension (tRect_Int32 aRect) {
		public mVector.t2xInt32
		Min => mVector.Min(aRect.Pos, aRect.Pos + aRect.Size);
		
		public mVector.t2xInt32
		Max => mVector.Max(aRect.Pos, aRect.Pos + aRect.Size);
		
		public mVector.t2xInt32
		SizeAbs => aRect.Max - aRect.Min + (1, 1);
	}
}
