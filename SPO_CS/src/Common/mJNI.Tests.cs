#:property ExperimentalFileBasedProgramEnableRefDirective = "true"
#:property ExperimentalFileBasedProgramEnableIncludeDirective = "true"
#:property OutputType = "Library"
#:include ../_GlobalUsings.cs
#:ref mTest.cs
#:ref mAssert.cs
#:ref mJNI.cs
#:ref mArrayList.cs

public static class
mJNI_Tests {
	public static readonly mTest.tTest
	Tests = mTest.Tests(
		nameof(mJNI),
		[
			mTest.Test(
				"Line conversion preserves empty lines and applies the offset",
				aDebug => {
					foreach (var Newline in new[] { "\n", "\r\n", "\r" }) {
						var Lines = mJNI.ToLines(
							"""
							 first 
							
							last
							
							""".Replace(
								"\n",
								Newline
							),
							20
						);
						
						mAssert.IsTrue(
							Lines.Eq(
								mStream.Stream<(tInt32 LineNr, tText Line)>(
									(21, " first "),
									(22, ""),
									(23, "last"),
									(24, "")
								),
								(aLeft, aRight) => aLeft == aRight
							)
						);
						
						mAssert.AreEquals(
							mJNI.ToText(Lines),
							"""
							 first 
							
							last
							
							""");
					}
					
					mAssert.IsTrue(mJNI.ToLines("", 0).TryFirst().AssertNotEmpty() == (1, ""));
					mAssert.AreEquals(
						mJNI.ToText(mStream.Stream<(tInt32 LineNr, tText Line)>()),
						""
					);
					
					mAssert.AreEquals(
						mJNI.ToText(
							mStream.Stream<(tInt32 LineNr, tText Line)>(
								(7, "first"),
								(42, "last")
							)
						),
						"""
						first
						last
						"""
					);
				}
			),
			mTest.Test(
				"Numbered values preserve their source positions and remove one prefix",
				aDebug => {
					var Entries = mJNI.TryParseList(
						mStream.Stream<(tInt32 LineNr, tText Line)>(
							(7, "name= value"),
							(12, "body:"),
							(15, "| first"),
							(17, "# Comment"),
							(22, "|"),
							(29, "||nested"),
							(35, "tail=last")
						),
						"config.jni"
					).ElseThrow();
					
					mAssert.IsTrue(
						Entries.Map(
							__ => __.Key
						).Eq(
							mStream.Stream("name", "body", "tail"),
							(a1, a2) => a1 == a2
						)
					);
					
					mAssert.IsTrue(
						Entries.TryGet("name").AssertNotEmpty().TryFirst().AssertNotEmpty() == (7, " value")
					);
					
					mAssert.IsTrue(
						Entries.TryGet(
							"body"
						).AssertNotEmpty(
						).Eq(
							mStream.Stream<(tInt32 LineNr, tText Line)>(
								(15, " first"),
								(22, ""),
								(29, "|nested")
							),
							(a1, a2) => a1 == a2
						)
					);
					mAssert.IsTrue(
						Entries.TryGet("tail").AssertNotEmpty().TryFirst().AssertNotEmpty() == (35, "last")
					);
				}
			),
			mTest.Test(
				"Empty inline and multiline values retain their distinct line shapes",
				aDebug => {
					var Entries = mJNI.TryParseList(
						mStream.Stream<(tInt32 LineNr, tText Line)>(
							(40, "inline="),
							(45, "block:")
						),
						"config.jni"
					).ElseThrow();
					
					mAssert.IsTrue(Entries.TryGet("inline").AssertNotEmpty().TryFirst().AssertNotEmpty() == (40, ""));
					mAssert.IsTrue(Entries.TryGet("block").AssertNotEmpty().IsEmpty());
					mAssert.IsTrue(Entries.TryGet("missing").IsNone());
				}
			),
			mTest.Test(
				"Nested errors keep the original document line after skipped comments",
				aDebug => {
					var Root = mJNI.TryParseList(
						mJNI.ToLines(
							"""
							outer:
							|inner:
							||key=first
							# Comment
							||key=second
							""",
							50
						),
						"config.jni"
					).ElseThrow();
					
					var Outer = mJNI.TryParseList(
						Root.TryGet("outer").AssertNotEmpty(),
						"config.jni"
					).ElseThrow();
					
					mAssert.IsFalse(
						mJNI.TryParseTree(
							Outer.TryGet("inner").AssertNotEmpty(),
							"config.jni"
						).Match(
							out _,
							out var Error
						)
					);
					
					mAssert.AreEquals("config.jni:55: Duplicate key 'key'.", Error);
				}
			),
			mTest.Test(
				"Syntax errors use supplied line numbers",
				aDebug => {
					foreach (
						var (Line, Message) in mStream.Stream(
							("invalid", "Expected key=value or key: followed by | lines."),
							(" =value", "Expected a nonempty key.")
						)
					) {
						mAssert.IsFalse(
							mJNI.TryParseList(
								mStream.Stream<(tInt32 LineNr, tText Line)>(
									(3, "key=first"),
									(87, Line)
								),
								"config.jni"
							).Match(
								out _,
								out var Error
							)
						);
						
						mAssert.AreEquals("config.jni:87: " + Message, Error);
					}
				}
			),
			mTest.Test(
				"Single-line values preserve order, whitespace and equals",
				aDebug => {
					foreach (var Newline in new[] { "\n", "\r\n", "\r" }) {
						var Source = """
						# Comment
						
						 first = value=tail 
						empty=
						First=distinct
						""";
						
						var Entries = mJNI.TryParseList(
							Source.Replace("\n", Newline),
							"JNI"
						).ElseThrow();
						
						mAssert.IsTrue(
							Entries.Eq(
								mStream.Stream<(tText Key, tText Value)>(
									("first", " value=tail "),
									("empty", ""),
									("First", "distinct")
								),
								(a1, a2) => a1 == a2
							)
						);
						
						mAssert.IsTrue(Entries.TryGet("missing").IsNone());
					}
				}
			),
			mTest.Test(
				"Multiline values preserve blank lines and nested documents",
				aDebug => {
					var Entries = mJNI.TryParseList(
						"""
						text:
						| first 
						# Comment
						|
						|last
						modules:
						|App:
						||path=App.*
						""",
						"JNI"
					).ElseThrow();
					
					mAssert.AreEquals(
						"""
						 first 
						
						last
						""",
						Entries.TryGet("text").AssertNotEmpty()
					);
					
					var Modules = mJNI.TryParseList(
						Entries.TryGet("modules").AssertNotEmpty(),
						"JNI"
					).ElseThrow();
					
					var App = mJNI.TryParseList(
						Modules.TryGet("App").AssertNotEmpty(),
						"JNI"
					).ElseThrow();
					
					mAssert.AreEquals("App.*", App.TryGet("path").AssertNotEmpty());
				}
			),
			mTest.Test(
				"Empty documents and empty multiline values succeed",
				aDebug => {
					mAssert.IsTrue(
						mJNI.TryParseList(
							"""
							 
							# Comment
							""",
							"JNI"
						).ElseThrow(
						).IsEmpty()
					);
					
					mAssert.AreEquals(
						mJNI.TryParseList("empty:", "JNI").ElseThrow().TryGet("empty").AssertNotEmpty(),
						""
					);
				}
			),
			mTest.Test(
				"Lists preserve duplicate keys and their values in input order",
				aDebug => {
					var Source = """
					key=first
					 key :
					|second
					key:
					key=
					""";
					
					var Entries = mJNI.TryParseList(mJNI.ToLines(Source, 10), "JNI").ElseThrow();
					
					mAssert.IsTrue(
						Entries.Map(aEntry => aEntry.Key).Eq(
							mStream.Stream("key", "key", "key", "key"),
							(aLeft, aRight) => aLeft == aRight
						)
					);
					mAssert.IsTrue(
						Entries.Map(aEntry => aEntry.Lines).Eq(
							mStream.Stream(
								mStream.Stream((11, "first")),
								mStream.Stream((13, "second")),
								mStream.Stream<(tInt32 LineNr, tText Line)>(),
								mStream.Stream((15, ""))
							),
							(aLeft, aRight) => aLeft.Eq(aRight, (a1, a2) => a1 == a2)
						)
					);
					mAssert.IsTrue(
						mJNI.TryParseList(Source, "JNI").ElseThrow().Eq(
							mStream.Stream(("key", "first"), ("key", "second"), ("key", ""), ("key", "")),
							(aLeft, aRight) => aLeft == aRight
						)
					);
				}
			),
			mTest.Test(
				"Trees use ordinal keys and preserve numbered values",
				aDebug => {
					var Tree = mJNI.TryParseTree(
						mJNI.ToLines(
							"""
							b:
							|first
							|second
							a=inline
							B:
							""",
							20
						),
						"JNI"
					).ElseThrow();
					
					mAssert.IsTrue(
						Tree.ToStream(
						).Map(
							__ => __.Key
						).Eq(
							mStream.Stream("B", "a", "b"),
							(a1, a2) => a1 == a2
						)
					);
					mAssert.IsTrue(Tree.TryGet("B").AssertNotEmpty().IsEmpty());
					mAssert.IsTrue(Tree.TryGet("a").AssertNotEmpty().TryFirst().AssertNotEmpty() == (24, "inline"));
					mAssert.IsTrue(
						Tree.TryGet("b").AssertNotEmpty().Eq(
							mStream.Stream((22, "first"), (23, "second")), (aLeft, aRight) => aLeft == aRight
						)
					);
					mAssert.IsTrue(Tree.TryGet("missing").IsNone());
					mAssert.IsTrue(
						mJNI.TryParseTree(mJNI.ToLines("# Empty", 0), "JNI").ElseThrow().ToStream().IsEmpty()
					);
				}
			),
			mTest.Test(
				"Tree duplicate errors refer to the key header rather than its value lines",
				aDebug => {
					mAssert.IsFalse(
						mJNI.TryParseTree(
							mStream.Stream((3, "key="), (87, "key:"), (99, "|value")), "config.jni"
						).Match(out _, out var Error)
					);
					mAssert.AreEquals("config.jni:87: Duplicate key 'key'.", Error);
				}
			),
			mTest.Tests(
				"Tree duplicate errors",
				mStream.Stream<(tText Name, tText Source, tText Error)>(
					(
						"Duplicate key",
						"""
						key=first
						 key =second
						""",
						"JNI:2: Duplicate key 'key'."
					),
					(
						"Duplicate multiline key",
						"""
						key:
						|first
						key:
						""",
						"JNI:3: Duplicate key 'key'."
					),
					(
						"Duplicate empty multiline key",
						"""
						key:
						key:
						""",
						"JNI:2: Duplicate key 'key'."
					)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						aDebug => {
							mAssert.IsTrue(mJNI.TryParseList(aCase.Source, "JNI").ElseThrow().Count() == 2);
							mAssert.IsFalse(
								mJNI.TryParseTree(mJNI.ToLines(aCase.Source, 0), "JNI").Match(out _, out var Error)
							);
							mAssert.AreEquals(aCase.Error, Error);
						}
					)
				).ToArrayList().ToArray()
			),
			mTest.Tests(
				"Syntax errors",
				mStream.Stream<(tText Name, tText Source, tText Error)>(
					(
						"Unexpected value line",
						"""
						|orphan
						""",
						"JNI:1: Expected key=value or key: followed by | lines."
					),
					(
						"Missing separator",
						"""
						# Comment
						invalid
						""",
						"JNI:2: Expected key=value or key: followed by | lines."
					),
					(
						"Empty key",
						"""
						 =value
						""",
						"JNI:1: Expected a nonempty key."
					),
					(
						"Empty multiline key",
						"""
						 :
						""",
						"JNI:1: Expected a nonempty key."
					),
					(
						"Invalid header after multiline",
						"""
						key:
						|value
						broken
						""",
						"JNI:3: Expected key=value or key: followed by | lines."
					)
				).Map(
					aCase => mTest.Test(
						aCase.Name,
						aDebug => {
							mAssert.IsFalse(mJNI.TryParseList(aCase.Source, "JNI").Match(out _, out var Error));
							mAssert.AreEquals(Error, aCase.Error);
							mAssert.IsFalse(mJNI.TryParseList(aCase.Source, "config.jni").Match(out _, out Error));
							mAssert.AreEquals(Error, aCase.Error.Replace("JNI:", "config.jni:"));
							mAssert.IsFalse(
								mJNI.TryParseTree(mJNI.ToLines(aCase.Source, 0), "JNI").Match(out _, out Error)
							);
							mAssert.AreEquals(Error, aCase.Error);
						}
					)
				).ToArrayList(
				).ToArray()
			)
		]
	);
}