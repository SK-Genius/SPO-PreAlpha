#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref mVM_Data.cs

public static class
mVM_View {
	private static mVM_Data.tData AdapterClosure<tPos>(mVM_Data.tProcDef<tPos> aAdapter, mVM_Data.tData aFunction, mVM_Type.tTypeResolver? aTypes) => mVM_Data.Closure(
		aAdapter,
		aFunction.CaptureTypes(aAdapter.TypeCaptures, aTypes)
	);
	// A closed opaque identity changes no storage layout. This only selects a
	// prepared converter; Matches checks the actual visible identity separately.
	private static tBool FitsSchema(mVM_Type.tType aSchema, mVM_Type.tType aActual) {
		if (ReferenceEquals(aSchema, aActual)) { return true; }
		var Cache = aSchema.SchemaMatches ??= new();
		if (Cache.TryGetValue(aActual, out var Previous)) { return Previous.Item1; }
		var Result = Compare(aSchema, aActual);
		Cache.AddOrUpdate(aActual, System.Tuple.Create(Result));
		return Result;
		
		static tBool Compare(mVM_Type.tType aSchema, mVM_Type.tType aActual) {
			if (aSchema.SameType(aActual)) { return true; }
			if (aSchema.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free or mVM_Type.tKind.TypeApply) { return aActual.Kind is mVM_Type.tKind.Opaque; }
			if (aSchema.Kind != aActual.Kind || aSchema.Prefix != aActual.Prefix) { return false; }
			
			if (aSchema.IsRecord(out var Fields)) {
				return Fields.ToStream().Count() == aActual.Fields.ToStream().Count() && Fields.ToStream().All(__ => aActual.Fields.TryGet(__.Key).IsSome(out var Field) && FitsSchema(__.Value, Field));
			}
			
			if (aSchema.Kind is mVM_Type.tKind.Generic or mVM_Type.tKind.Sig or mVM_Type.tKind.Interface or mVM_Type.tKind.Recursive) {
				return FitsSchema(aSchema.Refs[0].KindType(), aActual.Refs[0].KindType()) && FitsSchema(aSchema.Refs[1].Substitute(aSchema.Refs[0], aActual.Refs[0]), aActual.Refs[1]);
			}
			
			return aSchema.Refs.Length == aActual.Refs.Length && mStream.ZipShort(mStream.Stream(aSchema.Refs), mStream.Stream(aActual.Refs)).All(__ => FitsSchema(__.Item1, __.Item2));
		}
	}
	
	private static mVM_Type.tTypeResolver? BindSchema(mVM_Type.tTypeResolver? aTypes, mVM_Type.tType aSchema, mVM_Type.tType aActual) {
		if (aSchema.Resolve(aTypes).SameType(aActual)) { return aTypes; }
		if (aSchema.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free or mVM_Type.tKind.TypeApply && aActual.Kind is mVM_Type.tKind.Opaque) { return aTypes.WithWitness(aSchema, aActual.Refs[0]); }
		
		if (aSchema.IsRecord(out var Fields)) {
			foreach (var Field in Fields.ToStream()) { aTypes = BindSchema(aTypes, Field.Value, aActual.GetFieldType(Field.Key)); }
		} else if (aSchema.Kind is mVM_Type.tKind.Generic or mVM_Type.tKind.Sig or mVM_Type.tKind.Interface or mVM_Type.tKind.Recursive) {
			aTypes = BindSchema(aTypes, aSchema.Refs[1].Substitute(aSchema.Refs[0], aActual.Refs[0]), aActual.Refs[1]);
		} else {
			for (var Index = 0; Index < aSchema.Refs.Length; Index += 1) { aTypes = BindSchema(aTypes, aSchema.Refs[Index], aActual.Refs[Index]); }
		}
		
		return aTypes;
	}
	
	private static (mVM_Type.tType Type, mVM_Data.tConversion Plan)[] CandidateSchemas(mVM_Data.tConversion aPlan, mVM_Type.tType aActual) {
		var Cache = aPlan.CandidateSchemas ??= new();
		if (Cache.TryGetValue(aActual, out var Previous) && Previous.Item1 == aPlan.Alternatives.Size) { return Previous.Item2; }
		// Bound TYPE symbols may resolve differently in each call. Keep all their
		// schemas here; the caller still checks them with its explicit witnesses.
		var Candidates = aPlan.Alternatives.ToStream().Where(__ => __.Type.HasAbstractSymbols() || __.Type.HasUnboundParameter() || FitsSchema(__.Type, aActual)).ToArrayList().ToArray();
		Cache.AddOrUpdate(aActual, System.Tuple.Create(aPlan.Alternatives.Size, Candidates));
		return Candidates;
	}
	
	// Structural casts consume only the descriptor supplied by ANY/union. Their
	// target is fixed by generated code; no executable specialization is created.
	public static mVM_Data.tData Structural(mVM_Data.tData aData, mVM_Type.tType aSource, mVM_Type.tType aTarget, mVM_Data.tConversionMemo aMemo) {
		if (aSource.SameType(aTarget)) { return aData; }
		if (!aSource.IsImmutable || !aTarget.IsImmutable) { return Convert(); }
		var Cache = aMemo.Structural ??= new();
		if (Cache.TryGetValue((aData, aSource, aTarget), out var Previous)) { return Previous; }
		var Result = mVM_Data.Empty();
		Cache.Add((aData, aSource, aTarget), Result);
		var Value = Convert();
		Result._Value = Value._Value;
		Result._Fields = Value._Fields;
		return Result;
		
		mVM_Data.tData Convert() {
			if (aTarget.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free or mVM_Type.tKind.TypeApply) { return new mVM_Data.tData {_Value = mAny.Any(new mVM_Data.tOpaqueValue(aData))}; }
			if (aTarget.Kind is mVM_Type.tKind.Opaque) { return new mVM_Data.tData {_Value = mAny.Any(new mVM_Data.tOpaqueValue(Structural(aData, aSource, aTarget.Refs[0], aMemo)))}; }
			if (aTarget.IsAny()) { return mVM_Data.Dynamic(aData, aSource); }
			if (aSource.Kind is mVM_Type.tKind.Opaque && aSource.Refs[0].SameType(aTarget)) { return aData._Value.To<mVM_Data.tOpaqueValue>().Data; }
			if (aSource.IsRecursive(out var Head, out var Body)) { return Structural(aData, Body.Substitute(Head, aSource), aTarget, aMemo); }
			
			if (aSource.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
				mVM_Data.tData? Result = null;
				var TargetShape = aTarget;
				while (TargetShape.IsRecursive(out Head, out Body)) { TargetShape = Body.Substitute(Head, TargetShape); }
				
				foreach (var Alternative in aData._Value.To<mVM_Data.tDynamicValue>().Alternatives()) {
					if (!Alternative.Data.Matches(Alternative.Type, aTarget, true)) { continue; }
					var Value = Structural(Alternative.Data, Alternative.Type, aTarget, aMemo);
					if (TargetShape.Kind is not (mVM_Type.tKind.Set or mVM_Type.tKind.Any)) { return Value; }
					Result = Result is null ? Value : mVM_Data.JoinDynamic(Result, Value);
				}
				
				return Result ?? throw mError.Error("No matching visible structural alternative.");
			}
			
			if (aTarget.IsRecursive(out Head, out Body)) { return Structural(aData, aSource, Body.Substitute(Head, aTarget), aMemo); }
			
			if (aTarget.IsSet(out _, out _)) {
				mVM_Data.tData? Result = null;
				
				void Branch(mVM_Type.tType aType) {
					if (aType.IsSet(out var Left, out var Right)) { Branch(Left); Branch(Right); return; }
					if (!aData.Matches(aSource, aType, true)) { return; }
					var Visible = aSource.LimitVisibility(aType);
					var Value = mVM_Data.Dynamic(Structural(aData, aSource, Visible, aMemo), Visible);
					Result = Result is null ? Value : mVM_Data.JoinDynamic(Result, Value);
				}
				
				Branch(aTarget);
				return Result ?? throw mError.Error("No matching visible structural alternative.");
			}
			
			if (aSource.IsRecord(out var Fields) && aTarget.IsRecord(out var TargetFields)) {
				var Values = aData._Fields;
				
				foreach (var Field in TargetFields.ToStream()) {
					var Key = Field.Key.PrefixHash();
					Values = Values.Set(Key, Structural(aData.GetField(Key), Fields.TryGet(Field.Key).AssertNotEmpty(), Field.Value, aMemo));
				}
				
				return new mVM_Data.tData {_Fields = Values};
			}
			
			if (aSource.IsPair(out var First, out var Second) && aTarget.IsPair(out var TargetFirst, out var TargetSecond)) {
				var Pair = aData._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				return mVM_Data.Pair(Structural(Pair.Item1, First, TargetFirst, aMemo), Structural(Pair.Item2, Second, TargetSecond, aMemo));
			}
			
			if (aSource.IsPrefix(out var Prefix, out var Inner) && aTarget.IsPrefix(Prefix, out var TargetInner)) { return mVM_Data.Prefix(Prefix, Structural(aData._Value.To<(tNat32, mVM_Data.tData)>().Item2, Inner, TargetInner, aMemo)); }
			
			if (aSource.IsSig(out var SourceHead, out var SourceBody) && aTarget.IsSig(out var TargetHead, out var TargetBody)) {
				var Sig = aData._Value.To<mVM_Data.tSig>();
				var Witness = mVM_Type.Opaque(Sig.Head.TypeExpressionValue());
				if (SourceHead.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply) { SourceBody = SourceBody.Substitute(SourceHead, Witness); }
				if (TargetHead.Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply) { TargetBody = TargetBody.Substitute(TargetHead, Witness); }
				return mVM_Data.Sig(Sig.Head, Structural(Sig.Body, SourceBody, TargetBody, aMemo));
			}
			
			if (aSource.IsInterface(out _, out SourceBody) && aTarget.IsInterface(out _, out TargetBody)) {
				var Value = aData._Value.To<mVM_Data.tInterface>();
				return mVM_Data.Interface(Value.Object, Structural(Value.Methods, SourceBody, TargetBody, aMemo));
			}
			
			if (aSource.Kind is mVM_Type.tKind.Var or mVM_Type.tKind.Ref && aTarget.Kind is mVM_Type.tKind.Ref) {
				return new mVM_Data.tData {_Value = mAny.Any(new mVM_Data.tCellView(aData, aValue => Structural(aValue, aSource.Refs[0], aTarget.Refs[0], new()), null))};
			}
			
			// Calls use the visible signature and link the original implementation
			// before execution. A function pointer itself has no argument layout.
			if (aSource.Kind == aTarget.Kind && aTarget.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) { return aData; }
			mAssert.IsTrue(aSource.Kind == aTarget.Kind && aTarget.Kind is mVM_Type.tKind.Empty or mVM_Type.tKind.Int or mVM_Type.tKind.True or mVM_Type.tKind.False or mVM_Type.tKind.Type, "Unsupported structural representation conversion.");
			return aData;
		}
	}
	
	public static tBool
	Matches(
		this mVM_Data.tData aData,
		mVM_Type.tType aSource,
		mVM_Type.tType aTarget,
		tBool aAlternatives = false,
		mStream.tStream<(tNat64 Data, tNat64 Source, tNat64 Target)> aVisited = default
	) {
		if (!aAlternatives && aSource.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set) {
			var Dynamic = aData._Value.To<mVM_Data.tDynamicValue>();
			foreach (var Alternative in Dynamic.Alternatives()) { if (Alternative.Data.Matches(Alternative.Type, aTarget, true, aVisited)) { return true; } }
			return false;
		}
		
		if (aTarget.IsAny() || aSource.SameType(aTarget)) {
			return true;
		}
		
		if (!aSource.HasUnboundParameter() && !aTarget.HasUnboundParameter()) {
			var Relations = aSource.MatchSubtypes ??= new();
			
			if (!Relations.TryGetValue(aTarget, out var Relation)) {
				var Proven = aSource.IsSubType(aTarget, aDescribeError: false).Match(out var Mappings, out _) && Mappings.Deep() == 0;
				Relation = System.Tuple.Create(Proven);
				Relations.AddOrUpdate(aTarget, Relation);
			}
			
			if (Relation.Item1) { return true; }
		}
		
		if (aTarget.Kind is mVM_Type.tKind.Opaque) {
			return aSource.Kind is mVM_Type.tKind.Opaque
			? aSource.Refs[0].SameType(aTarget.Refs[0])
			: aData.Matches(aSource, aTarget.Refs[0], aAlternatives, aVisited);
		}
		
		if (aSource.IsSet(out var Left, out var Right)) {
			return aData.Matches(Left, aTarget, true, aVisited) || aData.Matches(Right, aTarget, true, aVisited);
		}
		
		if (aTarget.IsSet(out Left, out Right)) {
			return aData.Matches(aSource, Left, true, aVisited) || aData.Matches(aSource, Right, true, aVisited);
		}
		
		if (aSource.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Free or mVM_Type.tKind.Opaque || aTarget.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.Free) {
			return false;
		}
		
		foreach (var Previous in aVisited) { if (Previous.Data == aData._DebugId && Previous.Source == aSource.DebugId && Previous.Target == aTarget.DebugId) { return true; } }
		
		aVisited = mStream.Stream((aData._DebugId, aSource.DebugId, aTarget.DebugId), aVisited);
		
		if (aSource.IsRecursive(out var Head, out var Body)) {
			return aData.Matches(Body.Substitute(Head, aSource), aTarget, false, aVisited);
		}
		
		if (aTarget.IsRecursive(out Head, out Body)) {
			return aData.Matches(aSource, Body.Substitute(Head, aTarget), true, aVisited);
		}
		
		if (aSource.Kind != aTarget.Kind) {
			return false;
		}
		
		switch (aSource.Kind) {
			case mVM_Type.tKind.Record: {
				foreach (var Field in aTarget.Fields.ToStream()) {
					if (
						!aSource.Fields.TryGet(Field.Key).IsSome(out var SourceType) ||
						!aData.GetField(Field.Key.PrefixHash()).Matches(SourceType, Field.Value, false, aVisited)
					) {
						return false;
					}
				}
				
				return true;
			}
			case mVM_Type.tKind.Pair: {
				var Pair = aData._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				
				return Pair.Item1.Matches(aSource.Refs[0], aTarget.Refs[0], false, aVisited) &&
				Pair.Item2.Matches(aSource.Refs[1], aTarget.Refs[1], false, aVisited);
			}
			case mVM_Type.tKind.Prefix: {
				return aSource.Prefix == aTarget.Prefix &&
				aData._Value.To<(tNat32, mVM_Data.tData)>().Item2.Matches(
					aSource.Refs[0],
					aTarget.Refs[0],
					false,
					aVisited
				);
			}
			default:
			return aSource.IsSubType(aTarget, aDescribeError: false).Match(out _, out _);
		}
	}
	
	public static mVM_Data.tConversion
	Compile<tPos>(
		this mVM_Data.tProcDef<tPos> aCode,
		mVM_Type.tType aSource,
		mVM_Type.tType aTarget,
		tBool aRevealOpaque = false,
		mVM_Type.tWitnessMap? aProof = null
	) {
		var Key = (aSource.GetHashCode(), aTarget.GetHashCode(), -1, aRevealOpaque);
		if (!aCode.ConversionIndex.TryGetValue(Key, out var Candidates)) { Candidates = []; aCode.ConversionIndex.Add(Key, Candidates); }
		
		foreach (var Existing in Candidates) {
			if (Existing.Source.SameType(aSource) && Existing.Target.SameType(aTarget) && Existing.Proof.SameWitnesses(aProof)) {
				return Existing;
			}
		}
		
		var Plan = new mVM_Data.tConversion {
			Source = aSource,
			Target = aTarget,
			RevealOpaque = aRevealOpaque,
			Proof = aProof,
			Memoize = aSource.IsImmutable && aTarget.IsImmutable && aSource.Kind is mVM_Type.tKind.Recursive or mVM_Type.tKind.Set or mVM_Type.tKind.Pair or mVM_Type.tKind.Record or mVM_Type.tKind.Prefix && !aSource.HasAbstractSymbols() && !aTarget.HasAbstractSymbols() && !aSource.HasUnboundParameter() && !aTarget.HasUnboundParameter()
		};
		
		aCode.Conversions.Push(Plan);
		Candidates.Add(Plan);
		
		if (aSource.SameType(aTarget)) {
			return Plan;
		}
		
		var SourceOpaque = aSource.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Free or mVM_Type.tKind.Opaque;
		var TargetOpaque = aTarget.Kind is mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply or mVM_Type.tKind.Free or mVM_Type.tKind.Opaque;
		
		if (TargetOpaque && (aRevealOpaque || aSource.Kind is not (mVM_Type.tKind.Any or mVM_Type.tKind.Set))) {
			if (!SourceOpaque) { Plan.Apply = (aData, aTypes, aMemo) => Structural(aData, aSource.Resolve(aTypes), aTarget.Resolve(aTypes), aMemo); }
			return Plan;
		}
		
		if (SourceOpaque && aTarget.Kind is not (mVM_Type.tKind.Any or mVM_Type.tKind.Set)) {
			var Reified = aRevealOpaque ? aSource.ResolveWitnesses(aProof, false) : aSource;
			
			if (!aSource.SameType(Reified) && !Reified.SameType(aTarget)) {
				var Before = aCode.Compile(aSource, Reified, true, aProof);
				var After = aCode.Compile(Reified, aTarget, true, aProof);
				Plan.Before = Before;
				Plan.After = After;
				Plan.Apply = (aData, aTypes, aMemo) => After.Convert(Before.Convert(aData, aTypes, aMemo), aTypes, aMemo);
				return Plan;
			}
			
			if (aTarget.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) {
				var Adapter = aCode.AdapterFactory!(aTarget, aTarget, true, aProof);
				Plan.Adapter = Adapter;
				Plan.Apply = (aData, aTypes, aMemo) => AdapterClosure(Adapter, aData._Value.To<mVM_Data.tOpaqueValue>().Data.Normalize(aTarget.Resolve(aTypes), "Argument", true), aTypes);
			} else {
				Plan.Apply = (aData, aTypes, aMemo) => aData._Value.To<mVM_Data.tOpaqueValue>().Data.Normalize(aTarget.Resolve(aTypes), "Argument", true);
			}
			
			return Plan;
		}
		
		if (SourceOpaque && aRevealOpaque) {
			var Reified = aSource.ResolveWitnesses(aProof, false);
			
			if (!aSource.SameType(Reified)) {
				if (Reified.Kind is mVM_Type.tKind.Any or mVM_Type.tKind.Set && Reified.SameType(aTarget)) {
					Plan.Apply = (aData, aTypes, aMemo) => aData._Value.To<mVM_Data.tOpaqueValue>().Data.Normalize(Reified.Resolve(aTypes), "Argument", true);
				} else {
					var Before = aCode.Compile(aSource, Reified, true, aProof);
					var After = aCode.Compile(Reified, aTarget, false, aProof);
					Plan.Apply = (aData, aTypes, aMemo) => After.Convert(Before.Convert(aData, aTypes, aMemo), aTypes, aMemo);
				}
				
				return Plan;
			}
		}
		
		if (aSource.IsAny() || aSource.IsSet(out _, out _)) {
			if (aTarget.IsAny()) { return Plan; }
			
			if (TargetOpaque) {
				// Bound TYPE parameters share one opaque ABI, including native ANY inputs.
				Plan.Apply = (aData, aTypes, aMemo) => {
					var Target = aTarget.Resolve(aTypes);
					
					foreach (var Alternative in aData._Value.To<mVM_Data.tDynamicValue>().Alternatives()) {
						if (!Alternative.Data.Matches(Alternative.Type, Target, true)) { continue; }
						return Structural(Alternative.Data, Alternative.Type, Target, aMemo);
					}
					
					throw mError.Error("No matching visible alternative for the bound type.");
				};
				
				return Plan;
			}
			
			static tBool StructuralTarget(mVM_Type.tType aType) {
				if (aType.IsRecursive(out var Head, out var Body)) { return StructuralTarget(Body.Substitute(Head, mVM_Type.Empty())); }
				if (aType.IsRecord(out var Fields)) { return Fields.ToStream().All(__ => StructuralTarget(__.Value)); }
				return aType.Kind is mVM_Type.tKind.Empty or mVM_Type.tKind.Int or mVM_Type.tKind.True or mVM_Type.tKind.False or mVM_Type.tKind.Type or mVM_Type.tKind.Any || aType.Kind is mVM_Type.tKind.Pair or mVM_Type.tKind.Prefix or mVM_Type.tKind.Set && aType.Refs.All(StructuralTarget);
			}
			
			var StructuralResult = aProof is null && StructuralTarget(aTarget);
			
			void Register(mVM_Type.tType aType) {
				if (aType.IsSet(out var Left, out var Right)) { Register(Left); Register(Right); }
				else { aCode.RegisterActual!(aType); }
			}
			
			if (!aSource.IsAny()) { Register(aSource); }
			Plan.Dynamic = true;
			var MatchTarget = aTarget.ResolveWitnesses(aProof, true);
			var TargetShape = aTarget;
			while (TargetShape.IsRecursive(out var Head, out var Body)) { TargetShape = Body.Substitute(Head, TargetShape); }
			var DynamicResult = TargetShape.Kind is mVM_Type.tKind.Set or mVM_Type.tKind.Any;
			
			Plan.Apply = (aData, aTypes, aMemo) => {
				var Dynamic = aData._Value.To<mVM_Data.tDynamicValue>();
				
				if (aTarget.IsAny()) {
					return aData;
				}
				
				mVM_Data.tData? Result = null;
				
				foreach (var Alternative in Dynamic.Alternatives()) {
					var Candidates = mStream.Stream(CandidateSchemas(Plan, Alternative.Type));
					
					if (!Candidates.Where(__ => FitsSchema(__.Type.Resolve(aTypes), Alternative.Type)).TryFirst().IsSome(out var Prepared)) {
						if (!StructuralResult || !Alternative.Data.Matches(Alternative.Type, MatchTarget.Resolve(aTypes), true)) { continue; }
						var Value = Structural(Alternative.Data, Alternative.Type, aTarget.Resolve(aTypes), aMemo);
						if (!DynamicResult) { return Value; }
						Result = Result is null ? Value : mVM_Data.JoinDynamic(Result, Value);
						continue;
					}
					
					var Bound = BindSchema(aTypes, Prepared.Type, Alternative.Type);
					var Before = Prepared.Plan.Before;
					var Input = Before is null ? Alternative.Data : Before.Convert(Alternative.Data, Bound, aMemo);
					if (!Input.Matches(Before?.Target.Resolve(Bound) ?? Alternative.Type, MatchTarget.Resolve(Bound), true)) { continue; }
					var Converted = (Before is null ? Prepared.Plan : Prepared.Plan.After!).Convert(Input, Bound, aMemo);
					if (!DynamicResult) { return Converted; }
					Result = Result is null ? Converted : mVM_Data.JoinDynamic(Result, Converted);
				}
				
				return Result ?? throw mError.Error("No matching visible alternative.\nSource: " + aSource.ToText() + "\nTarget: " + aTarget.ToText() + "\nVisible: " + Dynamic.ActualType.ToText());
			};
			
			return Plan;
		}
		
		if (aTarget.IsAny()) {
			aCode.RegisterActual!(aSource);
			Plan.Apply = (aData, aTypes, aMemo) => mVM_Data.Dynamic(aData, aSource.Resolve(aTypes));
			return Plan;
		}
		
		if (aTarget.IsSet(out var LeftTarget, out var RightTarget)) {
			mStd.tFunc<mVM_Data.tData, mVM_Type.tTypeResolver?, mVM_Data.tConversionMemo, mVM_Data.tData?> Branch(mVM_Type.tType aTo) {
				if (aTo.IsSet(out var A, out var B)) {
					var Left = Branch(A);
					var Right = Branch(B);
					
					return (aValue, aTypes, aMemo) => {
						var First = Left(aValue, aTypes, aMemo);
						var Second = Right(aValue, aTypes, aMemo);
						return First is null ? Second : Second is null ? First : mVM_Data.JoinDynamic(First, Second);
					};
				}
				
				var Known = aTo.ResolveWitnesses(aProof, true);
				if (!mVM_Type.CanMatch(aSource, Known)) { return (_, _, _) => null; }
				var Actual = aSource.SealVisibility(aTo, aProof);
				aCode.RegisterActual!(Actual);
				var Inner = aCode.Compile(aSource, Actual, aRevealOpaque, aProof);
				return (aValue, aTypes, aMemo) => !aValue.Matches(aSource.Resolve(aTypes), Known.Resolve(aTypes), true) ? null : mVM_Data.Dynamic(Inner.Convert(aValue, aTypes, aMemo), Actual.Resolve(aTypes));
			}
			
			var Convert = Branch(aTarget);
			Plan.Apply = (aData, aTypes, aMemo) => Convert(aData, aTypes, aMemo) ?? throw mError.Error("No visible union alternative.");
			return Plan;
		}
		
		if (aSource.IsRecursive(out var SourceHead, out var SourceBody)) {
			var Inner = aCode.Compile(SourceBody.Substitute(SourceHead, aSource), aTarget, aRevealOpaque, aProof);
			Plan.Apply = (aData, aTypes, aMemo) => Inner.Convert(aData, aTypes, aMemo);
			return Plan;
		}
		
		if (aTarget.IsRecursive(out var TargetHead, out var TargetBody)) {
			var Inner = aCode.Compile(aSource, TargetBody.Substitute(TargetHead, aTarget), aRevealOpaque, aProof);
			Plan.Apply = (aData, aTypes, aMemo) => Inner.Convert(aData, aTypes, aMemo);
			return Plan;
		}
		
		if (aSource.IsRecord(out var SourceFields) && aTarget.IsRecord(out var TargetFields)) {
			var Fields = TargetFields.ToStream().Map(
				__ => (Key: __.Key.PrefixHash(), Plan: aCode.Compile(SourceFields.TryGet(__.Key).AssertNotEmpty(), __.Value, aRevealOpaque, aProof))
			).Where(__ => __.Plan.Apply is not null).ToArrayList().ToArray();
			
			if (Fields.Length != 0) {
				Plan.Apply = (aData, aTypes, aMemo) => {
					var Values = aData._Fields;
					
					foreach (var Field in Fields) {
						Values = Values.Set(Field.Key, Field.Plan.Convert(aData.GetField(Field.Key), aTypes, aMemo));
					}
					
					return new mVM_Data.tData { _Fields = Values};
				};
			}
			
			return Plan;
		}
		
		if (aSource.IsPair(out var SourceFirst, out var SourceSecond) && aTarget.IsPair(out var TargetFirst, out var TargetSecond)) {
			var First = aCode.Compile(SourceFirst, TargetFirst, aRevealOpaque, aProof);
			var Second = aCode.Compile(SourceSecond, TargetSecond, aRevealOpaque, aProof);
			
			if (First.Apply is not null || Second.Apply is not null) {
				Plan.Apply = (aData, aTypes, aMemo) => {
					var Pair = aData._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
					return mVM_Data.Pair(First.Convert(Pair.Item1, aTypes, aMemo), Second.Convert(Pair.Item2, aTypes, aMemo));
				};
			}
			
			return Plan;
		}
		
		if (aSource.IsPrefix(out var Prefix, out var SourceInner) && aTarget.IsPrefix(Prefix, out var TargetInner)) {
			var Inner = aCode.Compile(SourceInner, TargetInner, aRevealOpaque, aProof);
			
			if (Inner.Apply is not null) {
				Plan.Apply = (aData, aTypes, aMemo) => mVM_Data.Prefix(Prefix, Inner.Convert(aData._Value.To<(tNat32, mVM_Data.tData)>().Item2, aTypes, aMemo));
			}
			
			return Plan;
		}
		
		if (aSource.Kind is mVM_Type.tKind.Var or mVM_Type.tKind.Ref && aTarget.Kind is mVM_Type.tKind.Var or mVM_Type.tKind.Ref) {
			var Read = aCode.Compile(aSource.Refs[0], aTarget.Refs[0], aRevealOpaque, aProof);
			var Write = aTarget.Kind is mVM_Type.tKind.Var ? aCode.Compile(aTarget.Refs[0], aSource.Refs[0], aRevealOpaque, aProof.ReverseWitnesses()) : null;
			if (Read.Apply is null && (Write is null || Write.Apply is null)) { return Plan; }
			mAssert.IsTrue(aRevealOpaque || aTarget.Kind is mVM_Type.tKind.Ref, "Mutable element types are invariant.");
			Plan.Apply = (aData, aTypes, aMemo) => new mVM_Data.tData {_Value = mAny.Any(new mVM_Data.tCellView(aData, aValue => Read.Convert(aValue, aTypes), Write is null ? null : aValue => Write.Convert(aValue, aTypes)))};
			return Plan;
		}
		
		if (aSource.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic && aTarget.Kind is mVM_Type.tKind.Proc or mVM_Type.tKind.Generic) {
			var Adapter = aCode.AdapterFactory!(aSource, aTarget, aRevealOpaque, aProof);
			Plan.Adapter = Adapter;
			
			Plan.Apply = (aData, aTypes, aMemo) => AdapterClosure(Adapter, aData, aTypes);
			return Plan;
		}
		
		if (aSource.IsSig(out _, out _) && aTarget.IsSig(out _, out _)) {
			var SourceSigHead = aSource.Refs[0].ErasedHead();
			var TargetSigHead = aTarget.Refs[0].ErasedHead();
			aCode.RegisterWitness!(SourceSigHead);
			aCode.RegisterWitness!(TargetSigHead);
			var Source = aSource.Refs[1].Substitute(aSource.Refs[0], SourceSigHead);
			var Target = aTarget.Refs[1].Substitute(aTarget.Refs[0], TargetSigHead);
			var SourceSigHeadOpaque = aSource.Refs[0].Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply;
			var TargetSigHeadOpaque = aTarget.Refs[0].Kind is mVM_Type.tKind.Free or mVM_Type.tKind.Abstract or mVM_Type.tKind.TypeApply;
			mVM_Data.tConversion Inner;
			mVM_Data.tConversion? After = null;
			
			if (SourceSigHeadOpaque && !TargetSigHeadOpaque) {
				var Revealed = aSource.Refs[1].Substitute(aSource.Refs[0], aTarget.Refs[0]);
				Inner = aCode.Compile(Source, Revealed, true, new(SourceSigHead, aTarget.Refs[0], false, aProof));
				After = aCode.Compile(Revealed, Target, false, aProof);
			} else if (!SourceSigHeadOpaque && TargetSigHeadOpaque) {
				var Visible = aTarget.Refs[1].Substitute(aTarget.Refs[0], aSource.Refs[0]);
				Inner = aCode.Compile(Source, Visible, false, aProof);
				After = aCode.Compile(Visible, Target, true, new(TargetSigHead, aSource.Refs[0], true, aProof));
			} else { Inner = aCode.Compile(Source, Target, aRevealOpaque || SourceSigHeadOpaque, SourceSigHeadOpaque ? new(SourceSigHead, TargetSigHead, false, aProof) : aProof); }
			
			Plan.Inner = Inner;
			Plan.After = After;
			
			Plan.Apply = (aData, aTypes, aMemo) => {
				var Sig = aData._Value.To<mVM_Data.tSig>();
				var Bound = aTypes.WithWitness(SourceSigHead, Sig.Head.TypeExpressionValue()).WithWitness(TargetSigHead, Sig.Head.TypeExpressionValue());
				var Body = Inner.Convert(Sig.Body, Bound, aMemo);
				return mVM_Data.Sig(Sig.Head, After is null ? Body : After.Convert(Body, Bound, aMemo));
			};
			
			return Plan;
		}
		
		if (aSource.IsInterface(out _, out _) && aTarget.IsInterface(out _, out _)) {
			var SourceReceiver = aSource.Refs[0].ErasedHead();
			var TargetReceiver = aTarget.Refs[0].ErasedHead();
			var Methods = aCode.Compile(aSource.Refs[1].Substitute(aSource.Refs[0], SourceReceiver), aTarget.Refs[1].Substitute(aTarget.Refs[0], TargetReceiver), true, new(SourceReceiver, TargetReceiver, false, aProof));
			Plan.Inner = Methods;
			
			// The bound object and non-visible methods remain physically present.
			Plan.Apply = (aData, aTypes, aMemo) => {
				var Value = aData._Value.To<mVM_Data.tInterface>();
				return mVM_Data.Interface(Value.Object, Methods.Convert(Value.Methods, aTypes, aMemo));
			};
			
			return Plan;
		}
		
		throw mError.Error("Unsupported static representation conversion:\n" + aSource.ToText() + "\n=>\n" + aTarget.ToText());
	}
	
	public static mVM_Data.tData
	Convert(
		this mVM_Data.tConversion aPlan,
		mVM_Data.tData aData,
		mVM_Type.tTypeResolver? aTypes = null,
		mVM_Data.tConversionMemo? aMemo = null
	) {
		if (aPlan.Apply is null) { return aData; }
		aMemo ??= new();
		if (aPlan.Memoize && aMemo.Values.TryGetValue((aPlan, aData), out var Previous)) { return Previous; }
		var Result = aPlan.Apply(aData, aTypes.WithProof(aPlan.Proof), aMemo);
		if (aPlan.Memoize) { aMemo.Values[(aPlan, aData)] = Result; }
		return Result;
	}
	
	public static mVM_Data.tConversion Sequence(mVM_Data.tConversion aBefore, mVM_Data.tConversion aAfter) => new() {
		Source = aBefore.Source,
		Target = aAfter.Target,
		Before = aBefore,
		After = aAfter,
		Apply = (aData, aTypes, aMemo) => aAfter.Convert(aBefore.Convert(aData, aTypes, aMemo), aTypes, aMemo)
	};
	
	public static mVM_Data.tConversion
	Project<tPos>(this mVM_Data.tProcDef<tPos> aCode, mVM_Type.tType aSource, mVM_Type.tType aTarget, tInt32 aIndex) {
		var Key = (aSource.GetHashCode(), aTarget.GetHashCode(), aIndex, false);
		if (!aCode.ConversionIndex.TryGetValue(Key, out var Candidates)) { Candidates = []; aCode.ConversionIndex.Add(Key, Candidates); }
		
		foreach (var Existing in Candidates) {
			if (Existing.Source.SameType(aSource) && Existing.Target.SameType(aTarget)) { return Existing; }
		}
		
		var Plan = new mVM_Data.tConversion {Source = aSource, Target = aTarget, Projection = aIndex};
		aCode.Conversions.Push(Plan);
		Candidates.Add(Plan);
		var Source = aSource;
		while (Source.IsRecursive(out var Head, out var Body)) { Source = Body.Substitute(Head, Source); }
		
		if (Source.Kind is mVM_Type.tKind.Set or mVM_Type.tKind.Any) {
			Plan.Dynamic = true;
			
			Plan.Apply = (aData, aTypes, aMemo) => {
				mVM_Data.tData? Result = null;
				
				foreach (var Alternative in aData._Value.To<mVM_Data.tDynamicValue>().Alternatives()) {
					var Candidates = mStream.Stream(CandidateSchemas(Plan, Alternative.Type));
					var Prepared = Candidates.Where(__ => FitsSchema(__.Type.Resolve(aTypes), Alternative.Type)).TryFirst().AssertNotEmpty();
					var Value = Prepared.Plan.Convert(Alternative.Data, BindSchema(aTypes, Prepared.Type, Alternative.Type), aMemo);
					if (aTarget.Kind is not (mVM_Type.tKind.Set or mVM_Type.tKind.Any)) { return Value; }
					Result = Result is null ? Value : mVM_Data.JoinDynamic(Result, Value);
				}
				
				return Result ?? throw mError.Error("No visible pair alternative.");
			};
		} else {
			mAssert.IsTrue(Source.IsPair(out _, out _));
			Plan.Inner = aCode.Compile(Source.Refs[aIndex], aTarget);
			
			Plan.Apply = (aData, aTypes, aMemo) => {
				var Pair = aData._Value.To<(mVM_Data.tData, mVM_Data.tData)>();
				return Plan.Inner.Convert(aIndex == 0 ? Pair.Item1 : Pair.Item2, aTypes, aMemo);
			};
		}
		
		return Plan;
	}
}