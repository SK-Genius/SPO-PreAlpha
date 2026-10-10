#:property ExperimentalFileBasedProgramEnableRefDirective = true
#:property ExperimentalFileBasedProgramEnableIncludeDirective = true
#:property OutputType = Library
#:include _GlobalUsings.cs
#:ref Common/mStd.cs
#:ref Common/mTest.cs
#:ref Common/mAssert.cs
#:ref Common/mStream.cs
#:ref Common/mMaybe.cs
#:ref Common/mRecordFields.cs
#:ref mVM_Data.cs
#:ref mVM_Type.cs

public static class
mRecord_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		"Records",
		[
			mTest.Test("Field storage is sorted for every insertion order",
				aDebug => {
					var Keys = new[] { 3, -1, 7, 0 };
					var CheckedOrders = 0;
					void CheckOrder(tInt32 aIndex) {
						if (aIndex == Keys.Length) {
							var Fields = mRecordFields.Empty<tInt32, tInt32>(
								(aLeft, aRight) => aLeft.CompareTo(aRight)
							);
							foreach (var Key in Keys) {
								Fields = Fields.Set(Key, Key);
							}
							mAssert.AreEquals(
								Fields.ToStream(),
								mStream.Stream((-1, -1), (0, 0), (3, 3), (7, 7))
							);
							foreach (var Key in Keys) {
								mAssert.AreEquals(Fields.TryGet(Key).AssertNotEmpty(), Key);
							}
							mAssert.IsTrue(Fields.TryGet(-2).IsNone());
							mAssert.IsTrue(Fields.TryGet(4).IsNone());
							mAssert.IsTrue(Fields.TryGet(8).IsNone());
							CheckedOrders += 1;
							return;
						}
						for (var Index = aIndex; Index < Keys.Length; Index += 1) {
							(Keys[aIndex], Keys[Index]) = (Keys[Index], Keys[aIndex]);
							CheckOrder(aIndex + 1);
							(Keys[aIndex], Keys[Index]) = (Keys[Index], Keys[aIndex]);
						}
					}
					CheckOrder(0);
					mAssert.AreEquals(CheckedOrders, 24);
				}
			),
			mTest.Test("Field insertion and replacement preserve earlier snapshots",
				aDebug => {
					var Empty = mRecordFields.Empty<tInt32, tInt32>((aLeft, aRight) => aLeft.CompareTo(aRight));
					mAssert.IsTrue(Empty.TryGet(0).IsNone());
					var Original = Empty.Set(10, 10).Set(30, 30);
					var Added = Original.Set(20, 20).Set(0, 0).Set(40, 40);
					var Replaced = Added.Set(20, 99);
					mAssert.AreEquals(
						Replaced.ToStream(),
						mStream.Stream((0, 0), (10, 10), (20, 99), (30, 30), (40, 40))
					);
					mAssert.AreEquals(Original.ToStream(), mStream.Stream((10, 10), (30, 30)));
					mAssert.AreEquals(Added.TryGet(20).AssertNotEmpty(), 20);
					mAssert.IsTrue(Empty.ToStream().IsEmpty());
				}
			),
			mTest.Test("Record type field order uses ordinal names",
				aDebug => {
					var Record = mVM_Type.Record([
						("ı", mVM_Type.Int()),
						("i", mVM_Type.Int()),
						("İ", mVM_Type.Int()),
						("I", mVM_Type.Int())
					]);
					mAssert.AreEquals(
						Record.Fields.ToStream().Map(aField => aField.Key),
						mStream.Stream("I", "i", "İ", "ı")
					);
				}
			),
			mTest.Test("Values compare fields independently of construction order",
				aDebug => {
					var AB = mVM_Data.Record([("A", mVM_Data.Int(1)), ("B", mVM_Data.Int(2))]);
					var BA = mVM_Data.Record([("B", mVM_Data.Int(2)), ("A", mVM_Data.Int(1))]);
					var AnotherAB = mVM_Data.Record([("A", mVM_Data.Int(1)), ("B", mVM_Data.Int(2))]);
					mAssert.IsTrue(AB.Equals(BA));
					mAssert.IsTrue(BA.Equals(AB));
					mAssert.IsTrue(AB.Equals(AnotherAB));
					mAssert.IsTrue(AB.Equals(AB));
				}
			),
			mTest.Test("Values distinguish field names, counts, values and data kinds",
				aDebug => {
					var Record = mVM_Data.Record([("A", mVM_Data.Int(1)), ("B", mVM_Data.Int(2))]);
					mAssert.IsFalse(
						Record.Equals(mVM_Data.Record([("A", mVM_Data.Int(1)), ("C", mVM_Data.Int(2))]))
					);
					mAssert.IsFalse(Record.Equals(mVM_Data.Record([("A", mVM_Data.Int(1))])));
					mAssert.IsFalse(
						Record.Equals(mVM_Data.Record([("A", mVM_Data.Int(1)), ("B", mVM_Data.Int(3))]))
					);
					mAssert.IsFalse(Record.Equals(mVM_Data.Empty()));
					mAssert.IsFalse(Record.Equals((tUnknown?)null));
					mAssert.IsFalse(Record.Equals((tUnknown)"record"));
				}
			),
			mTest.Test("Nested records compare inside records, pairs and prefixes",
				aDebug => {
					var AB = mVM_Data.Record([("A", mVM_Data.Int(1)), ("B", mVM_Data.Int(2))]);
					var BA = mVM_Data.Record([("B", mVM_Data.Int(2)), ("A", mVM_Data.Int(1))]);
					var Changed = mVM_Data.Record([("A", mVM_Data.Int(1)), ("B", mVM_Data.Int(3))]);
					mAssert.IsTrue(
						mVM_Data.Record([("Inner", AB)]).Equals(mVM_Data.Record([("Inner", BA)]))
					);
					mAssert.IsFalse(
						mVM_Data.Record([("Inner", AB)]).Equals(mVM_Data.Record([("Inner", Changed)]))
					);
					mAssert.IsTrue(mVM_Data.Pair(AB, mVM_Data.Int(3)).Equals(mVM_Data.Pair(BA, mVM_Data.Int(3))));
					mAssert.IsTrue(mVM_Data.Prefix("Tag", AB).Equals(mVM_Data.Prefix("Tag", BA)));
				}
			),
			mTest.Test("Adding fields preserves earlier record snapshots",
				aDebug => {
					var A = mVM_Data.Record([("A", mVM_Data.Int(1))]);
					var AB = mVM_Data.Record(A, mVM_Data.Prefix("B", mVM_Data.Int(2)));
					var BA = mVM_Data.Record([("B", mVM_Data.Int(2)), ("A", mVM_Data.Int(1))]);
					mAssert.IsTrue(AB.Equals(BA));
					mAssert.IsTrue(A.IsRecord(out var OriginalFields));
					mAssert.AreEquals(OriginalFields.ToStream().Count(), 1u);
					mAssert.IsTrue(OriginalFields.TryGet("B".PrefixHash()).IsNone());
				}
			),
			mTest.Test("Type equality compares field names, counts and types",
				aDebug => {
					var AB = mVM_Type.Record([("A", mVM_Type.Int()), ("B", mVM_Type.Bool())]);
					var BA = mVM_Type.Record([("B", mVM_Type.Bool()), ("A", mVM_Type.Int())]);
					mAssert.IsTrue(AB == BA);
					mAssert.IsTrue(AB.SameType(BA));
					foreach (var Different in new[] {
						mVM_Type.Record([("A", mVM_Type.Int())]),
						mVM_Type.Record([("A", mVM_Type.Int()), ("C", mVM_Type.Bool())]),
						mVM_Type.Record([("A", mVM_Type.Int()), ("B", mVM_Type.Int())])
					}) {
						mAssert.IsTrue(AB != Different);
						mAssert.IsFalse(AB.SameType(Different));
					}
				}
			),
			mTest.Test("Nested record types compare their fields",
				aDebug => {
					var AB = mVM_Type.Record([("A", mVM_Type.Int()), ("B", mVM_Type.Bool())]);
					var BA = mVM_Type.Record([("B", mVM_Type.Bool()), ("A", mVM_Type.Int())]);
					var Changed = mVM_Type.Record([("A", mVM_Type.Int()), ("B", mVM_Type.Int())]);
					mAssert.IsTrue(
						mVM_Type.Record([("Inner", AB)]) == mVM_Type.Record([("Inner", BA)])
					);
					mAssert.IsTrue(
						mVM_Type.Record([("Inner", AB)]) != mVM_Type.Record([("Inner", Changed)])
					);
				}
			)
		]
	);
}
