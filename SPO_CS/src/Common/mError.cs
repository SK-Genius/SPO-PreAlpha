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
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tError<t>
	Error<t>(
		tText aMsg,
		t aData
	) => new(aMsg, aData);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static tError<mStd.tEmpty>
	Error(
		tText aMsg
	) => Error(aMsg, mStd.cEmpty);
}
