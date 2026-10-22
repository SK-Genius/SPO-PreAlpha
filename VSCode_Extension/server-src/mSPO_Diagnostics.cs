#:ref ../../SPO_CS/src/Common/mStd.cs
#:ref ../../SPO_CS/src/Common/mSpan.cs
#:ref ../../SPO_CS/src/Common/mMaybe.cs
#:ref ../../SPO_CS/src/Common/mStream.cs
#:ref ../../SPO_CS/src/Common/mResult.cs
#:ref ../../SPO_CS/src/Common/mParserGen.cs
#:ref ../../SPO_CS/src/Common/mTextStream.cs
#:ref ../../SPO_CS/src/Common/mTextParser.cs
#:ref ../../SPO_CS/src/mTokenizer.cs
#:ref ../../SPO_CS/src/mVM_Type.cs
#:ref ../../SPO_CS/src/mSPO_AST.cs
#:ref ../../SPO_CS/src/mSPO_AST_Types.cs
#:ref ../../SPO_CS/src/mSPO_Parser.cs
#:ref ../../SPO_CS/src/mSPO_Desugar.cs
#:ref ../../SPO_CS/src/mSPO2IL.cs
#:ref ../../SPO_CS/src/mIL_Parser.cs
#:ref ../../SPO_CS/src/mIL_TypeChecker.cs

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;
using tToken = mTokenizer.tToken;

public static class
mSPO_Diagnostics {
	public static mParserGen.tParser<tPos, tToken, mSPO_AST.tModuleNode<tSpan>, tText>
	DocumentParser(
		tText aId
	) => aId.EndsWith(".SIG", System.StringComparison.OrdinalIgnoreCase)
		? mSPO_Parser.Signature.ModifyS((aSpan, aType) => mSPO_AST.Module(
			aSpan,
			mSPO_AST.Import(aSpan, mSPO_AST.RecordPattern<tSpan>(aSpan, mStd.cEmpty)),
			mStd.cEmpty,
			mSPO_AST.Export(aSpan, aType)
		))
		: mSPO_Parser.Module;

	public enum
	tDiagnosticSeverity {
		Error = 1,
		Warning = 2,
		Info = 3,
		Hint = 4,
	}
	
	public readonly struct
	tDiagnostic {
		public readonly tSpan Pos;
		public readonly tDiagnosticSeverity Severity;
		public readonly tText Source;
		public readonly tText Message;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
		internal
		tDiagnostic(
			tSpan aPos,
			tDiagnosticSeverity aSeverity,
			tText aSource,
			tText aMessage
		) {
			this.Pos = aPos;
			this.Severity = aSeverity;
			this.Source = aSource;
			this.Message = aMessage;
		}
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	public static tDiagnostic
	Diagnostic(
		tSpan aPos,
		tDiagnosticSeverity aSeverity,
		tText aSource,
		tText aMessage
	) => new(
		aPos,
		aSeverity,
		aSource,
		aMessage
	);
	
	private static tPos
	GetEndPos(
		tText aCode,
		tText aId
	) {
		var Lines = aCode.Replace("\r", "").Split('\n');
		return mTextStream.Pos(
			aId,
			(tNat32)mMath.Max(Lines.Length, 1),
			(tNat32)(Lines.Length == 0 ? 1 : Lines[^1].Length + 1)
		);
	}
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tPos
	NormalizePos(
		tPos aPos,
		tPos aFallback
	) => aPos.Row > 0 && aPos.Col > 0
		? aPos
		: aFallback;
	
	[Pure, MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerHidden]
	private static tSpan
	NormalizeSpan(
		tSpan aPos,
		tPos aFallback
	) => aPos.Start.Row > 0 && aPos.Start.Col > 0 && aPos.End.Row > 0 && aPos.End.Col > 0
		? aPos
		: mSpan.Span(aFallback);
	
	private static mResult.tResult<mStream.tStream<(tSpan Span, tToken Value)>, mStream.tStream<tDiagnostic>>
	Tokenize(
		tText aCode,
		tText aId,
		tPos aFallback,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream,
		mTokenizer.tFileContext? aFileContext
	) {
		var CharStream = aCode.ToStream(aId).Map(
			__ => (mSpan.Span(__.Pos), __.Char)
		);
		var MaybeTokens = mTokenizer.Tokenizer.StartParse(CharStream, aDebugStream);
		if (!MaybeTokens.Match(out var TokenResult, out var TokenErrors)) {
			return mResult.Fail(
				TokenErrors.Map(
					__ => Diagnostic(
						mSpan.Span(NormalizePos(__.Pos, aFallback)),
						tDiagnosticSeverity.Error,
						"tokenizer",
						__.Message
					)
				)
			);
		}
		
		if (!TokenResult.RemainingStream.IsEmpty()) {
			var Remaining = TokenResult.RemainingStream.TryFirst().AssertNotEmpty();
			return mResult.Fail(
				mStream.Stream(
					Diagnostic(
						NormalizeSpan(Remaining.Span, aFallback),
						tDiagnosticSeverity.Error,
						"tokenizer",
						"invalid token"
					)
				)
			);
		}
		
		return mResult.OK(
			TokenResult.Result.Value.Map(
				__ => {
					__.FileContext = aFileContext;
					return (__.Span, __);
				}
			)
		).WithErrorType<mStream.tStream<tDiagnostic>>();
	}
	
	private static mResult.tResult<tModule, mStream.tStream<tDiagnostic>>
	ParseModule<tModule>(
		mStream.tStream<(tSpan Span, tToken Value)> aTokens,
		tPos aFallback,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream,
		mParserGen.tParser<tPos, tToken, tModule, tText> aParser
	) {
		var MaybeModule = aParser.StartParse(aTokens, aDebugStream);
		if (!MaybeModule.Match(out var ModuleResult, out var ParseErrors)) {
			var Furthest = ParseErrors.Map(
				a => a.Pos
			).Sort(
				(a, b) => mTextParser.ComparePos(b, a)
			).TryFirst().ElseUse(aFallback);
			return mResult.Fail(
				ParseErrors.Where(a => mTextParser.ComparePos(a.Pos, Furthest) == 0).Map(
					aError => {
						var Pos = NormalizePos(aError.Pos, aFallback);
						var Span = mSpan.Span(Pos);
						if (aError.Message.StartsWith("[§LOAD ")) {
							var Opening = aTokens.Where(
								a => a.Value.Type is mTokenizer.tTokenType.SpecialToken && a.Value.Text == "[" &&
								mTextParser.ComparePos(a.Span.Start, Pos) <= 0
							).TryLast();
							if (Opening.IsSome(out var Token)) {
								Span = mSpan.Span(Token.Span.Start, Pos);
							}
						}
						return Diagnostic(Span, tDiagnosticSeverity.Error, "parser", aError.Message);
					}
				)
			);
		}
		
		if (!ModuleResult.RemainingStream.IsEmpty()) {
			var Remaining = ModuleResult.RemainingStream.TryFirst().AssertNotEmpty();
			return mResult.Fail(
				mStream.Stream(
					Diagnostic(
						NormalizeSpan(Remaining.Span, aFallback),
						tDiagnosticSeverity.Error,
						"parser",
						"expected end of text"
					)
				)
			);
		}
		
		return mResult.OK(
			ModuleResult.Result.Value
		).WithErrorType<mStream.tStream<tDiagnostic>>();
	}
	
	private static mResult.tResult<mStd.tEmpty, mStream.tStream<tDiagnostic>>
	AnalyzeModule(
		mSPO_AST.tModuleNode<tSpan> aModule
	) {
		if (!mSPO_Desugar.TryDesugarModule(aModule).Match(out var DesugaredModule, out var DesugarError)) {
			return mResult.Fail(
				mStream.Stream(
					Diagnostic(
						DesugarError.Pos,
						tDiagnosticSeverity.Error,
						"desugar",
						DesugarError.ErrorText
					)
				)
			);
		}
		
		var TypeArg = mVM_Type.Free(mVM_Type.Type());
		
		var InitScope = mSPO_AST_Types.UpdatePatternTypes(
			DesugaredModule.Import.Pattern,
			mStd.cEmpty,
			mSPO_AST_Types.tTypeRelation.Sub,
			mStream.Stream(
				mSPO_AST_Types.ScopeItem(
					"_=...",
					mVM_Type.Generic(
						TypeArg,
						mVM_Type.Proc(
							mVM_Type.Var(TypeArg),
							TypeArg,
							mVM_Type.Empty()
						)
					)
				)
			),
			new()
		).Then(
			__ => (__.Scope, __.State)
		);
		
		if (!DesugaredModule.Commands.Reduce(
			InitScope,
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope.Scope, aScope.State)
			)
		).Match(out var Scope, out var TypeError)) {
			return mResult.Fail(
				mStream.Stream(
					Diagnostic(
						TypeError.Pos,
						tDiagnosticSeverity.Error,
						"types",
						TypeError.ErrorText
					)
				)
			);
		}
		
		if (!mSPO2IL.MapModule(DesugaredModule, mSpan.Merge, Scope.Scope, Scope.State).Match(out var IL, out var LoweringError)) {
			return mResult.Fail(
				mStream.Stream(
					Diagnostic(
						LoweringError.Pos,
						tDiagnosticSeverity.Error,
						"lowering",
						LoweringError.ErrorText
					)
				)
			);
		}
		if (!mIL_TypeChecker.AreModuleTypesValid(IL.ToIL(), out var Error)) {
			return mResult.Fail(mStream.Stream(Diagnostic(Error.Pos, tDiagnosticSeverity.Error, "types", Error.ErrorText)));
		}
		
		return mResult.OK(mStd.cEmpty).WithErrorType<mStream.tStream<tDiagnostic>>();
	}
	
	public static mStream.tStream<tDiagnostic>
	GetModuleDiagnostics(
		tText aCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream,
		mTokenizer.tFileContext? aFileContext = null
	) {
		var Fallback = GetEndPos(aCode, aId);
		if (aId.EndsWith(".ILT", System.StringComparison.OrdinalIgnoreCase) && !aCode.EndsWith('\n')) {
			aCode += "\n";
		}
		
		var Tokens = Tokenize(
			aCode,
			aId,
			Fallback,
			aDebugStream,
			aFileContext
		);
		if (aId.EndsWith(".ILT", System.StringComparison.OrdinalIgnoreCase)) {
			return (
				Tokens.ThenTry(
					aTokens => ParseModule(aTokens, Fallback, aDebugStream, mIL_Parser.Module)
				).ThenTry(
					aModule => (
						mIL_TypeChecker.AreModuleTypesValid(aModule, out var Error)
						? mResult.OK(mStd.cEmpty).WithErrorType<mStream.tStream<tDiagnostic>>()
						: mResult.Fail(
							mStream.Stream(
								Diagnostic(NormalizeSpan(Error.Pos, Fallback), tDiagnosticSeverity.Error, "types", Error.ErrorText)
							)
						)
					)
				).Match(out _, out var Errors)
				? mStd.cEmpty
				: Errors
			);
		}
		var Result = Tokens.ThenTry(
			aTokens => ParseModule(aTokens, Fallback, aDebugStream, DocumentParser(aId))
		).ThenTry(
			AnalyzeModule
		);
		
		return Result.Match(out _, out var Diagnostics)
			? mStd.cEmpty
			: Diagnostics;
	}
}