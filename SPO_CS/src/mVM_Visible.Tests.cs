#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include _GlobalUsings.cs
#:ref Common/mTest.cs
#:ref mVM_Visible.cs
#:ref mIL_Specialize.cs

public static class
mVM_Visible_Tests {
	private sealed class
	tSecret {
		public override tBool Equals(tUnknown? aOther) => throw new System.InvalidOperationException("Secret compared.");
		public override tInt32 GetHashCode() => throw new System.InvalidOperationException("Secret hashed.");
		public override tText ToString() => throw new System.InvalidOperationException("Secret formatted.");
	}
	
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"Visible value comparison",
		[
			mTest.Test(
				"Native signatures preserve ordinary receiver contracts and reveal only existential representations",
				_ => {
					var Empty = mVM_Type.Empty();
					var Integer = mVM_Type.Int();
					var Function = mVM_Data.ExternProc(mVM_Type.Proc(Integer, Empty, Integer), (_, _, _, _) => mVM_Data.Int(7), mVM_Data.Empty());
					Function.Normalize(mVM_Type.Proc(Integer, Empty, Integer));
					Function.Normalize(mVM_Type.Proc(mVM_Type.Opaque(Integer), Empty, Integer));
					
					foreach (var Receiver in new[] {Empty, mVM_Type.Any(), mVM_Type.Bool(), mVM_Type.Opaque(mVM_Type.Bool())}) {
						mAssert.ThrowsError(() => Function.Normalize(mVM_Type.Proc(Receiver, Empty, Integer)));
					}
					
					var Parameter = mVM_Type.Free("Receiver", mVM_Type.Type());
					mAssert.ThrowsError(() => Function.Normalize(mVM_Type.Generic(Parameter, mVM_Type.Proc(Parameter, Empty, Integer))));
					var Constructor = mVM_Type.Free("Receiver constructor", mVM_Type.Proc(Empty, mVM_Type.Type(), mVM_Type.Type()));
					mAssert.ThrowsError(() => Function.Normalize(mVM_Type.Generic(Constructor, mVM_Type.Proc(Constructor.ApplyType(Integer), Empty, Integer))));
					var Hidden = mVM_Type.Abstract("Private receiver", mVM_Type.Type());
					Function.Normalize(mVM_Type.Proc(Hidden, Empty, Integer));
					mAssert.ThrowsError(() => Function.Normalize(mVM_Type.Proc(mVM_Type.Pair(Empty, Hidden), Empty, Integer)));
				}
			),
			mTest.Test(
				"Native closure validation carries explicit constructor witnesses into nested functions",
				_ => {
					var Empty = mVM_Type.Empty();
					var Kind = mVM_Type.Proc(Empty, mVM_Type.Type(), mVM_Type.Type());
					var SourceHead = mVM_Type.Abstract("Source", Kind);
					var TargetHead = mVM_Type.Abstract("Target", Kind);
					var Parameter = mVM_Type.Free("Element", mVM_Type.Type());
					var Source = mVM_Type.Generic(Parameter, mVM_Type.Proc(Empty, SourceHead.ApplyType(Parameter), mVM_Type.Int()));
					var Target = mVM_Type.Generic(Parameter, mVM_Type.Proc(Empty, TargetHead.ApplyType(Parameter), mVM_Type.Int()));
					var Original = mVM_Data.Closure(new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(Empty, Empty, Source)), mVM_Data.Empty());
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(Empty, mVM_Type.Pair(mVM_Type.Tuple([Kind, Kind]), Target), Target)) {TypeCaptures = [SourceHead, TargetHead]};
					var Integer = mVM_Type.Generic(Parameter, mVM_Type.Pair(Parameter, mVM_Type.Int()));
					var Boolean = mVM_Type.Generic(Parameter, mVM_Type.Pair(Parameter, mVM_Type.Bool()));
					var Expected = Target.Substitute(TargetHead, mVM_Type.Opaque(Integer));
					mVM_Data.tData Closure(mVM_Type.tType aSource) => mVM_Data.Closure(Code, mVM_Data.Pair(mVM_Data.Tuple([mVM_Data.TypeExpression(mVM_Type.Opaque(aSource)), mVM_Data.TypeExpression(mVM_Type.Opaque(Integer))]), Original));
					Closure(Integer).Normalize(Expected);
					mAssert.ThrowsError(() => Closure(Boolean).Normalize(Expected));
				}
			),
			mTest.Test(
				"Structural ANY casts retain separate visible alternatives and the hidden payload",
				_ => {
					var Empty = mVM_Type.Empty();
					var First = mVM_Type.Record([("First", mVM_Type.Int())]);
					var Second = mVM_Type.Record([("Second", mVM_Type.Int())]);
					var Both = mVM_Type.Record([("First", mVM_Type.Int()), ("Second", mVM_Type.Int())]);
					var Target = mVM_Type.Set(First, Second);
					var Compiler = new mIL_Specialize.tContext<mStd.tEmpty>();
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(Empty, Empty, mVM_Type.Proc(Empty, Empty, Target)));
					Compiler.Configure(Code);
					var Secret = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var Data = mVM_Data.Record([("First", mVM_Data.Int(7)), ("Second", mVM_Data.Int(9)), ("Hidden", Secret)]);
					var Value = Code.Compile(mVM_Type.Any(), Target).Convert(mVM_Data.Dynamic(Data, Both));
					mAssert.IsTrue(Value.Matches(Target, First));
					mAssert.IsTrue(Value.Matches(Target, Second));
					mAssert.IsFalse(Value.Matches(Target, Both));
					mAssert.IsFalse(Value.Matches(Target, mVM_Type.Record([("Hidden", mVM_Type.Any())])));
					foreach (var Alternative in Value._Value.To<mVM_Data.tDynamicValue>().Alternatives()) { mAssert.IsTrue(ReferenceEquals(Alternative.Data.GetField("Hidden".PrefixHash()), Secret)); }
					mAssert.IsTrue((Value, Target).SameVisibleValue((Value, Target)));
				}
			),
			mTest.Test(
				"A concrete SIG head does not make ordinary static values opaque",
				_ => {
					var Empty = mVM_Type.Empty();
					var Compiler = new mIL_Specialize.tContext<mStd.tEmpty>();
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(Empty, Empty, mVM_Type.Proc(Empty, Empty, mVM_Type.Any())));
					Compiler.Configure(Code);
					var Box = Code.Compile(mVM_Type.Int(), mVM_Type.Any(), aProof: new(mVM_Type.Int(), mVM_Type.Int(), true));
					var Value = Box.Convert(mVM_Data.Int(7));
					mAssert.IsTrue(Value._Value.To<mVM_Data.tDynamicValue>().ActualType.SameType(mVM_Type.Int()));
					mAssert.IsTrue(Value.Matches(mVM_Type.Any(), mVM_Type.Int()));
				}
			),
			mTest.Test(
				"Escaping SIG adapters capture the explicit constructor witness",
				_ => {
					var Empty = mVM_Type.Empty();
					var Kind = mVM_Type.Proc(Empty, mVM_Type.Type(), mVM_Type.Type());
					var Parameter = mVM_Type.Free("Element", mVM_Type.Type());
					var Constructor = mVM_Type.Generic(Parameter, mVM_Type.Tuple([Parameter, Parameter]));
					var Head = mVM_Type.Free("Constructor", Kind);
					var SourceFunction = mVM_Type.Proc(Empty, Empty, Constructor.ApplyType(mVM_Type.Int()));
					var TargetFunction = mVM_Type.Proc(Empty, Empty, Head.ApplyType(mVM_Type.Int()));
					var Compiler = new mIL_Specialize.tContext<mStd.tEmpty>();
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(Empty, Empty, SourceFunction));
					Compiler.Configure(Code);
					var Conversion = Code.Compile(mVM_Type.Sig(Constructor, SourceFunction), mVM_Type.Sig(Head, TargetFunction));
					var Function = mVM_Data.ExternProc(SourceFunction, (_, _, _, _) => mVM_Data.Tuple([mVM_Data.Int(7), mVM_Data.Int(7)]), mVM_Data.Empty());
					var Package = Conversion.Convert(mVM_Data.Sig(mVM_Data.TypeExpression(Constructor), Function))._Value.To<mVM_Data.tSig>();
					mAssert.IsTrue(Package.Body.IsProc<mStd.tEmpty>(out var Adapter, out var Environment));
					mAssert.IsTrue(Adapter.TypeCaptures.Length > 0);
					var Types = Environment.OpenTypeCaptures(Adapter.TypeCaptures, null).Types;
					var Result = Head.ErasedHead().ApplyType(mVM_Type.Int()).Resolve(Types);
					mAssert.IsTrue(Result.SameType(mVM_Type.Opaque(Constructor.ApplyType(mVM_Type.Int()))));
				}
			),
			mTest.Test(
				"Cyclic recursive records compare only visible fields and terminate",
				_ => {
					var Head = mVM_Type.Free("Node", mVM_Type.Type());
					var Node = mVM_Type.Recursive(Head, mVM_Type.Record([("Value", mVM_Type.Int()), ("Next", Head)]));
					
					static mVM_Data.tData Cycle(tInt32 aValue) {
						var Result = mVM_Data.Record([("Value", mVM_Data.Int(aValue)), ("Secret", new mVM_Data.tData {_Value = mAny.Any(new tSecret())})]);
						Result._Fields = Result._Fields.Set("Next".PrefixHash(), Result);
						return Result;
					}
					
					mAssert.IsTrue((Cycle(7), Node).SameVisibleValue((Cycle(7), Node)));
					mAssert.IsFalse((Cycle(7), Node).SameVisibleValue((Cycle(9), Node)));
				}
			),
			mTest.Test(
				"Recursive alternatives retain only their visible intersection",
				aDebug => {
					var Head = mVM_Type.Free("List", mVM_Type.Type());
					var List = mVM_Type.Recursive(Head, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Pair(Head, mVM_Type.Int())));
					var View = List.LimitVisibility(mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Int()));
					mAssert.IsTrue(View.SameType(mVM_Type.Empty()));
				}
			),
			mTest.Test(
				"Function alternatives retain the restricted public signature",
				aDebug => {
					var Original = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Int());
					var Restricted = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Int(), mVM_Type.Any());
					var Compiler = new mIL_Specialize.tContext<mStd.tEmpty>();
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), Restricted));
					Compiler.Configure(Code);
					var Box = Code.Compile(Original, mVM_Type.Set(mVM_Type.Empty(), Restricted));
					var Value = Box.Convert(mVM_Data.Empty());
					mAssert.IsTrue(Value.Matches(mVM_Type.Any(), Restricted));
					mAssert.IsFalse(Value.Matches(mVM_Type.Any(), Original));
				}
			),
			mTest.Test(
				"Closed opaque views compare their visible identity without reading data",
				aDebug => {
					var Integer = mVM_Type.Opaque(mVM_Type.Int());
					var OtherInteger = mVM_Type.Opaque(mVM_Type.Int());
					var Boolean = mVM_Type.Opaque(mVM_Type.Bool());
					var Secret = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var Value = mVM_Data.Dynamic(Secret, Integer);
					mAssert.IsTrue(Value.Matches(mVM_Type.Any(), OtherInteger));
					mAssert.IsFalse(Value.Matches(mVM_Type.Any(), Boolean));
					mAssert.IsFalse(Value.Matches(mVM_Type.Any(), mVM_Type.Int()));
					mAssert.IsTrue((Secret, Integer).SameVisibleValue((Secret, OtherInteger)));
					var Source = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), Integer);
					var Target = mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), Boolean);
					mAssert.IsFalse(Secret.Matches(Source, Target));
				}
			),
			mTest.Test(
				"Cached SIG conversions preserve their separate opaque storage schemas",
				aDebug => {
					static mVM_Type.tType Contract() {
						var Head = mVM_Type.Free("Head", mVM_Type.Type());
						return mVM_Type.Sig(Head, mVM_Type.Set(mVM_Type.Empty(), mVM_Type.Prefix("Value", Head)));
					}
					
					static mVM_Data.tData Package() {
						var Payload = new mVM_Data.tData {_Value = mAny.Any(new mVM_Data.tOpaqueValue(mVM_Data.Int(7)))};
						var Actual = mVM_Type.Prefix("Value", mVM_Type.Opaque(mVM_Type.Int()));
						return mVM_Data.Sig(mVM_Data.TypeExpression(mVM_Type.Int()), mVM_Data.Dynamic(mVM_Data.Prefix("Value", Payload), Actual));
					}
					
					var Source1 = Contract();
					var Target1 = Contract();
					var Source2 = Contract();
					var Target2 = Contract();
					var Compiler = new mIL_Specialize.tContext<mStd.tEmpty>();
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty())));
					Compiler.Configure(Code);
					var First = Code.Compile(Source1, Target1);
					var Second = Code.Compile(Source2, Target2);
					Compiler.Complete();
					var Value1 = First.Convert(Package())._Value.To<mVM_Data.tSig>().Body._Value.To<mVM_Data.tDynamicValue>();
					var Value2 = Second.Convert(Package())._Value.To<mVM_Data.tSig>().Body._Value.To<mVM_Data.tDynamicValue>();
					var Expected = mVM_Type.Prefix("Value", mVM_Type.Opaque(mVM_Type.Int()));
					mAssert.IsTrue(Value1.ActualType.SameType(Expected));
					mAssert.IsTrue(Value2.ActualType.SameType(Expected));
				}
			),
			mTest.Test(
				"Overlapping union alternatives remain separate and retain hidden storage",
				_ => {
					var First = mVM_Type.Record([("First", mVM_Type.Int())]);
					var Second = mVM_Type.Record([("Second", mVM_Type.Int())]);
					var Both = mVM_Type.Record([("First", mVM_Type.Int()), ("Second", mVM_Type.Int())]);
					var Union = mVM_Type.Set(First, Second);
					var Secret = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var Original = mVM_Data.Record([("First", mVM_Data.Int(1)), ("Second", mVM_Data.Int(2)), ("Secret", Secret)]);
					var Compiler = new mIL_Specialize.tContext<mStd.tEmpty>();
					var Code = new mVM_Data.tProcDef<mStd.tEmpty>(mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Proc(mVM_Type.Empty(), mVM_Type.Empty(), mVM_Type.Empty())));
					Compiler.Configure(Code);
					var Box = Code.Compile(Both, Union);
					var Narrow = Code.Compile(Union, First);
					Compiler.Complete();
					var Value = Box.Convert(Original);
					mAssert.IsTrue(Value.Matches(Union, First));
					mAssert.IsTrue(Value.Matches(Union, Second));
					mAssert.IsFalse(Value.Matches(Union, Both));
					mAssert.IsFalse(Value.Matches(Union, mVM_Type.Record([("Secret", mVM_Type.Any())])));
					var Limited = Narrow.Convert(Value);
					mAssert.IsTrue(ReferenceEquals(Secret, Limited.GetField("Secret".PrefixHash())));
					mAssert.IsFalse(Limited._Value.Is(out mVM_Data.tDynamicValue _));
					mAssert.AreEquals("{First: 1}", (Limited, First).ToVisibleText());
					mAssert.IsTrue((Value, Union).SameVisibleValue((Box.Convert(mVM_Data.Record([("First", mVM_Data.Int(1)), ("Second", mVM_Data.Int(2))])), Union)));
					var Native = mVM_Data.Dynamic(Original, Both).Normalize(Union);
					mAssert.IsTrue(Native.Matches(Union, First));
					mAssert.IsTrue(Native.Matches(Union, Second));
					mAssert.IsFalse(Native.Matches(Union, Both));
					mAssert.IsTrue((Native, Union).SameVisibleValue((Value, Union)));
					mAssert.IsTrue(ReferenceEquals(Secret, Narrow.Convert(Native).GetField("Secret".PrefixHash())));
					var Repeated = mVM_Data.JoinDynamic(Value, Native);
					mAssert.IsTrue((Repeated, Union).SameVisibleValue((Value, Union)));
				}
			),
			mTest.Test(
				"Hidden fields do not affect equality and are never examined",
				_ => {
					var View = mVM_Type.Record([("Value", mVM_Type.Int())]);
					var Secret = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var Left = mVM_Data.Record([("Value", mVM_Data.Int(7)), ("Secret", Secret)]);
					var Right = mVM_Data.Record([("Value", mVM_Data.Int(7))]);
					mAssert.IsTrue((Left, View).SameVisibleValue((Right, View)));
					mAssert.IsTrue((Right, View).SameVisibleValue((Left, View)));
					var Different = mVM_Data.Record([("Value", mVM_Data.Int(9)), ("Secret", Secret)]);
					mAssert.IsFalse((Left, View).SameVisibleValue((Different, View)));
					mAssert.AreEquals("{Value: 7}", (Left, View).ToVisibleText());
				}
			),
			mTest.Test(
				"Nested views hide secret data recursively",
				_ => {
					var Inner = mVM_Type.Record([("Value", mVM_Type.Int())]);
					var View = mVM_Type.Record([("Item", mVM_Type.Prefix("Box", Inner))]);
					var Secret = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var LeftItem = mVM_Data.Record([("Value", mVM_Data.Int(7)), ("Secret", Secret)]);
					var RightItem = mVM_Data.Record([("Value", mVM_Data.Int(7))]);
					var Left = mVM_Data.Record([("Item", mVM_Data.Prefix("Box", LeftItem)), ("Secret", Secret)]);
					var Right = mVM_Data.Record([("Item", mVM_Data.Prefix("Box", RightItem))]);
					mAssert.IsTrue((Left, View).SameVisibleValue((Right, View)));
					mAssert.AreEquals((Left, View).ToVisibleText(), (Right, View).ToVisibleText());
				}
			),
			mTest.Test(
				"ANY compares only the visibility of its actualType",
				_ => {
					var View = mVM_Type.Record([("Value", mVM_Type.Int())]);
					var Secret = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var Left = mVM_Data.Dynamic(mVM_Data.Record([("Value", mVM_Data.Int(7)), ("Secret", Secret)]), View);
					var Right = mVM_Data.Dynamic(mVM_Data.Record([("Value", mVM_Data.Int(7))]), View);
					mAssert.IsTrue((Left, mVM_Type.Any()).SameVisibleValue((Right, mVM_Type.Any())));
					mAssert.AreEquals("{Value: 7}", (Left, mVM_Type.Any()).ToVisibleText());
				}
			),
			mTest.Test(
				"An opaque view never inspects its representation",
				_ => {
					var View = mVM_Type.Abstract("Secret", mVM_Type.Type());
					var Left = new mVM_Data.tData {_Value = mAny.Any(new tSecret())};
					var Right = new mVM_Data.tData();
					mAssert.IsTrue((Left, View).SameVisibleValue((Right, View)));
					mAssert.AreEquals("<opaque>", (Left, View).ToVisibleText());
				}
			)
		]
	);
}