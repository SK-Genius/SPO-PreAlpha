#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include ../_GlobalUsings.cs
#:ref mStream.cs
#:ref mResult.cs
#:ref mTreeMap.cs

public static class
mJNI {
	public static mMaybe.tMaybe<tValue>
	TryGet<tValue>(
		this mStream.tStream<(tText Key, tValue Value)> aEntries,
		tText aKey
	) => aEntries.Where(
		__ => __.Key == aKey
	).TryFirst(
	).Then(
		__ => __.Value
	);
	
	public static mStream.tStream<(tInt32 LineNr, tText Line)>
	ToLines(
		tText aText,
		tInt32 aLineOffset
	) => aText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').AsStream().MapWithIndex(
		(aIndex, aLine) => (aLineOffset + 1 + (tInt32)aIndex, aLine)
	);
	
	public static tText
	ToText(
		mStream.tStream<(tInt32 LineNr, tText Line)> aLines
	) => aLines.Map(aLine => aLine.Line).Join((aLeft, aRight) => aLeft + "\n" + aRight, "");
	
	public static mResult.tResult<mStream.tStream<(tText Key, tText Value)>, tText>
	TryParseList(
		tText aText,
		tText aSrcName
	) => TryParseList(ToLines(aText, 0), aSrcName).Then(
		aEntries => aEntries.Map(aEntry => (aEntry.Key, ToText(aEntry.Lines)))
	);
	
	public static mResult.tResult<
		mStream.tStream<(tText Key, mStream.tStream<(tInt32 LineNr, tText Line)> Lines)>,
		tText
	>
	TryParseList(
		mStream.tStream<(tInt32 LineNr, tText Line)> aLines,
		tText aSrcName
	) => TryParseEntries(aLines, aSrcName).Then(
		aEntries => aEntries.Map(aEntry => (aEntry.Key, aEntry.Lines))
	);
	
	public static mResult.tResult<
		mTreeMap.tTree<tText, mStream.tStream<(tInt32 LineNr, tText Line)>>,
		tText
	>
	TryParseTree(
		mStream.tStream<(tInt32 LineNr, tText Line)> aLines,
		tText aSrcName
	) {
		if (TryParseEntries(aLines, aSrcName).IsFail(out var Error, out var Entries)) {
			return Error;
		}
		var Tree = mTreeMap.Tree<tText, mStream.tStream<(tInt32 LineNr, tText Line)>>(
			(aLeft, aRight) => System.Math.Sign(tText.CompareOrdinal(aLeft, aRight)), []
		);
		foreach (var (Key, Lines, KeyLineNr) in Entries) {
			if (Tree.TryGet(Key).IsSome(out _)) {
				return mResult.Fail($"{aSrcName}:{KeyLineNr}: Duplicate key '{Key}'.");
			}
			Tree = Tree.Set(Key, Lines);
		}
		return Tree;
	}
	
	private static mResult.tResult<
		mStream.tStream<(tText Key, mStream.tStream<(tInt32 LineNr, tText Line)> Lines, tInt32 KeyLineNr)>,
		tText
	>
	TryParseEntries(
		mStream.tStream<(tInt32 LineNr, tText Line)> aLines,
		tText aSrcName
	) {
		var Entries = mStream.Stream<(
			tText Key, mStream.tStream<(tInt32 LineNr, tText Line)> Lines, tInt32 KeyLineNr
		)>();
		
		static tBool Ignored(
			tText aLine
		) => (
			tText.IsNullOrWhiteSpace(aLine) ||
			aLine.TrimStart().StartsWith('#')
		);
		
		mResult.tResultFail<tText>
		Error(
			tInt32 aLine,
			tText aMessage
		) => mResult.Fail($"{aSrcName}:{aLine}: {aMessage}");
		
		while (aLines.Is(out var Current, out aLines)) {
			var (LineNr, Line) = Current;
			
			if (Ignored(Line)) {
				continue;
			}
			
			var Header = Line.Trim();
			var Equals = Line.IndexOf('=');
			var Multiline = Equals < 0 && Header.EndsWith(':');
			
			if (Header.StartsWith('|') || (Equals < 0 && !Multiline)) {
				return Error(LineNr, "Expected key=value or key: followed by | lines.");
			}
			
			var Key = Multiline ? Header[..^1].Trim() : Line[..Equals].Trim();
			if (Key.Length == 0) {
				return Error(LineNr, "Expected a nonempty key.");
			}
			
			var Value = mStream.Stream<(tInt32 LineNr, tText Line)>();
			if (Multiline) {
				while (aLines.Is(out var Next, out var Rest)) {
					if (Next.Line.StartsWith('|')) {
						Value = mStream.Stream((Next.LineNr, Next.Line[1..]), Value);
					} else if (!Ignored(Next.Line)) {
						break;
					}
					aLines = Rest;
				}
				Value = Value.Reverse();
			} else {
				Value = mStream.Stream((LineNr, Line[(Equals + 1)..]), Value);
			}
			
			Entries = mStream.Stream((Key, Value, LineNr), Entries);
		}
		
		return Entries.Reverse();
	}
}