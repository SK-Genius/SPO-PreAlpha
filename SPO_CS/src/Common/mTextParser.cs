//IMPORT mParserGen.cs
//IMPORT mTextStream.cs
//IMPORT mSpan.cs

#nullable enable

// TODO: create a function how generate a call stack output

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;

using tError = System.String;

public static class
mTextParser {
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tInt32
	ComparePos(
		tPos a1,
		tPos a2
	) => mMath.Sign(
		a1.Row != a2.Row
		? (tInt32)a1.Row - (tInt32)a2.Row
		: (tInt32)a1.Col - (tInt32)a2.Col
	);
	
	[Pure, DebuggerHidden]
	public static (tSpan Span, tOut Result)
	ParseText<tOut>(
		this mParserGen.tParser<tPos, tChar, tOut, tError> aParser,
		tText aText,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		using var _ = mPerf.Measure();
		var Stream = aText.ToStream(aId).Map(a => (mSpan.Span(a.Pos), a.Char));
		var MaybeResult = aParser.StartParse(Stream, aDebugStream);
		var Result = MaybeResult.ElseThrow(a => a.ToText(aText.Split('\n')));
		if (!Result.RemainingStream.IsEmpty()) {
			var Pos = Result.RemainingStream.TryFirst().ThenDo(a => a.Span.Start).ElseThrow();
			var Line = aText.Split('\n')[Pos.Row-1];
			var StartSpacesCount = Line.Length - Line.TrimStart().Length;
			throw mError.Error(
				$"{Pos.Id}({Pos.Row}, {Pos.Col}): expected end of text\n" +
				$"{Line}\n" +
				$"{Line[..StartSpacesCount] + new tText(' ', (tInt32)Pos.Col - StartSpacesCount - 1)}^"
			);
		}
		return Result.Result;
	}
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetChar(
		tChar aRefChar
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => a == aRefChar,
		a => (a.Span.Start, $"expect {aRefChar}"),
		ComparePos
	)
	.SetDebugName("'", aRefChar, "'");
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetNotChar(
		tChar aRefChar
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => a != aRefChar,
		a => (a.Span.Start, $"expect not {aRefChar}"),
		ComparePos
	)
	.SetDebugName("'^", aRefChar, "'");
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetCharIn(
		tText aRefChars
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		aChar => {
			foreach (var RefChar in aRefChars) {
				if (aChar == RefChar) {
					return true;
				}
			}
			return false;
		},
		a => (a.Span.Start, $"expect one of [{aRefChars}]"),
		ComparePos
	)
	.SetDebugName("[", aRefChars, "]");
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetCharNotIn(
		tText aRefChars
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		aChar => {
			foreach (var RefChar in aRefChars) {
				if (aChar == RefChar) {
					return false;
				}
			}
			return true;
		},
		a => (a.Span.Start, $"expect non of [{aRefChars}]"),
		ComparePos
	)
	.SetDebugName("[^", aRefChars, "]");
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tChar, tError>
	GetCharInRange(
		tChar aMinChar,
		tChar aMaxChar
	) => mParserGen.AtomParser<tPos, tChar, tError>(
		a => aMinChar <= a && a <= aMaxChar,
		a => (a.Span.Start, $"expect one in [{aMinChar}...{aMaxChar}]"),
		ComparePos
	)
	.SetDebugName("[", aMinChar, "..", aMaxChar, "]");
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tText, tError>
	GetToken(
		tText aToken
	) {
		mAssert.AreNotEquals(aToken.Length, 0);
		var I = aToken.Length - 1;
		var Parser = -GetChar(aToken[I]);
		while (I --> 0) {
			var Char = aToken[I];
			Parser = -GetChar(Char) -Parser;
		}
		return Parser
		.Modify(aSpan => aToken)
		.ModifyErrors((_, a) => mStream.Stream((a.Span.Start, $"expect '{aToken}'")))
		.SetDebugName("\"", aToken, "\"");
	}
	
	[Pure, DebuggerHidden]
	public static mParserGen.tParser<tPos, tChar, tOut, tError>
	SetName<tOut>(
		this mParserGen.tParser<tPos, tChar, tOut, tError> aParser,
		tText aName
	) => aParser.AddError(
		a => (a.Span.Start, $"invalid {aName}")
	)
	.SetDebugName(aName);
}
