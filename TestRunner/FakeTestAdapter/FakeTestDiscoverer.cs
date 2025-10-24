using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace FakeTestAdapter;

[FileExtension(".dll")]
[DefaultExecutorUri(FakeTestExecutor.cExecutorUriString)]
public sealed class
FakeTestDiscoverer : ITestDiscoverer {
	private static readonly Uri
	cExecutorUri = new Uri(FakeTestExecutor.cExecutorUriString);
	
	public void
	DiscoverTests(
		IEnumerable<tText> aTestDllPaths,
		IDiscoveryContext aDiscoveryContext,
		IMessageLogger aTestLogger,
		ITestCaseDiscoverySink aDiscoverySink
	) {
		aTestLogger.SendMessage(
			TestMessageLevel.Informational,
			"FakeTestDiscoverer started"
		);
		
		foreach (var Test in GetTests(aTestDllPaths)) {
			aDiscoverySink.SendTestCase(Test);
		}
	}
	
	internal static List<TestCase>
	GetTests(
		IEnumerable<tText> aTestDllPaths
	) {
		var Tests = new List<TestCase>();
		
		static void DebugLog(tText message) {
			Console.WriteLine(message);
		}
		
		DebugLog($"> tests discovery started");
		foreach (var TestDllPath in aTestDllPaths) {
			DebugLog($">> Loading assembly: {TestDllPath}");
			foreach (var Type in Assembly.LoadFrom(TestDllPath).GetTypes().Where(_ => _.IsClass && _.IsPublic)) {
				DebugLog($">>> Found type: {Type.FullName}");
				foreach (var Field in Type.GetFields(BindingFlags.Static | BindingFlags.Public)) {
					DebugLog($">>>> Found field: {Field.Name}");
					if (Field.FieldType == typeof(mTest.tTest)) {
						DebugLog($">>>> Adding test: {Field.Name}");
						
						var Results = new List<(tText Prefix, mTest.tTestRun Run)>();
						
						AddTests(Results, Field.Name, (mTest.tTest)Field.GetValue(null));
						
						foreach (var Test in Results) {
							Tests.Add(
								new TestCase {
									ExecutorUri = cExecutorUri,
									DisplayName = Test.Run.Name,
									Source = TestDllPath,
									FullyQualifiedName = $"{Test.Prefix}.{Test.Run.Name}",
									CodeFilePath = Test.Run.File,
									LineNumber = Test.Run.Line,
									LocalExtensionData = (Func<tText>)(
										() => {
											var Log = new System.Text.StringBuilder();
											Test.Run.TestFunc(_ => Log.AppendLine(_));
											return Log.ToString();
										}
									),
								}
							);
						}
					}
				}
			}
		}
		DebugLog($"> Total tests discovered: {Tests.Count}");
		
		return Tests;
	}
	
	private static void
	AddTests(
		List<(tText Prefix, mTest.tTestRun Run)> aList,
		tText aPrefix,
		mTest.tTest aTests
	) {
		switch (aTests) {
			case mTest.tTestRun Run: {
				aList.Add(
					(
						aPrefix,
						Run
					)
				);
				break;
			}
			case mTest.tTestCollection Collection: {
				foreach (var Test in Collection.Tests) {
					AddTests(aList, aPrefix + "." + Collection.Name, Test);
				}
				break;
			}
		}
	}
}
