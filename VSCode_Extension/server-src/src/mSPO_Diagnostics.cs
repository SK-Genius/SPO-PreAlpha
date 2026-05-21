#:include Common/mStd.cs
#:include Common/mSpan.cs
#:include Common/mMaybe.cs
#:include Common/mStream.cs
#:include Common/mResult.cs
#:include Common/mParserGen.cs
#:include Common/mTextStream.cs
#:include Common/mTextParser.cs
#:include mTokenizer.cs
#:include mVM_Type.cs
#:include mSPO_AST.cs
#:include mSPO_AST_Types.cs
#:include mSPO_Parser.cs
#:include mSPO_Desugar.cs
#:include mSPO2IL.cs

using tPos = mTextStream.tPos;
using tSpan = mSpan.tSpan<mTextStream.tPos>;
using tToken = mTokenizer.tToken;

public static class
mSPO_Diagnostics {
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
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
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
				__ => (__.Span, __)
			)
		).WithErrorType<mStream.tStream<tDiagnostic>>();
	}
	
	private static mResult.tResult<mSPO_AST.tModuleNode<tSpan>, mStream.tStream<tDiagnostic>>
	ParseModule(
		mStream.tStream<(tSpan Span, tToken Value)> aTokens,
		tPos aFallback,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var MaybeModule = mSPO_Parser.Module.StartParse(aTokens, aDebugStream);
		if (!MaybeModule.Match(out var ModuleResult, out var ParseErrors)) {
			return mResult.Fail(
				ParseErrors.Map(
					__ => Diagnostic(
						mSpan.Span(NormalizePos(__.Pos, aFallback)),
						tDiagnosticSeverity.Error,
						"parser",
						__.Message
					)
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
		if (!mSPO_Desugar.DesugarModule(aModule).Match(out var DesugaredModule, out var DesugarError)) {
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
		
		var TypeArg = mVM_Type.Free();
		
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
			)
		).Then(
			__ => __.Scope
		);
		
		if (!DesugaredModule.Commands.Reduce(
			InitScope,
			(aResultScope, aCommand) => aResultScope.ThenTry(
				aScope => mSPO_AST_Types.UpdateCommandTypes(aCommand, aScope)
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
		
		if (!mSPO2IL.MapModule(DesugaredModule, mSpan.Merge, Scope).Match(out _, out var LoweringError)) {
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
		
		return mResult.OK(mStd.cEmpty).WithErrorType<mStream.tStream<tDiagnostic>>();
	}
	
	public static mStream.tStream<tDiagnostic>
	GetModuleDiagnostics(
		tText aCode,
		tText aId,
		mStd.tAction<mStd.tFunc<tText>> aDebugStream
	) {
		var Fallback = GetEndPos(aCode, aId);
		
		var Result = Tokenize(
			aCode,
			aId,
			Fallback,
			aDebugStream
		).ThenTry(
			aTokens => ParseModule(aTokens, Fallback, aDebugStream)
		).ThenTry(
			AnalyzeModule
		);
		
		return Result.Match(out _, out var Diagnostics)
			? mStd.cEmpty
			: Diagnostics;
	}
}
