using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace FakeTestAdapter;

[ExtensionUri(cExecutorUriString)]
public sealed class
FakeTestExecutor : ITestExecutor {
	public const tText
	cExecutorUriString = "executor://FakeTestAdapter";
	
	private tBool
	_Cancelled = false;
	
	public void
	RunTests(
		IEnumerable<TestCase> aTests,
		IRunContext aRunContext,
		IFrameworkHandle aFrameworkHandle
	) {
		var Watch = Stopwatch.StartNew();
		
		foreach (var Test in aTests) {
			aFrameworkHandle.RecordStart(Test);
			
			if (this._Cancelled) {
				aFrameworkHandle.RecordEnd(Test, TestOutcome.Skipped);
				continue;
			}
			
			TestResult Result;
			
			try {
				var Outcome = TestOutcome.Passed;
				Watch.Restart();
				var Log = "";
				try {
					Log = (Test.LocalExtensionData as Func<tText>)?.Invoke();
				} catch {
					Outcome = TestOutcome.Failed;
				}
				Watch.Stop();
				
				Result = new TestResult(Test) {
					Outcome = TestOutcome.Passed,
					StartTime = DateTime.UtcNow - Watch.Elapsed,
					EndTime = DateTime.UtcNow,
					Duration = TimeSpan.FromMilliseconds(Watch.ElapsedMilliseconds),
					Messages = { new TestResultMessage("Log", Log) },
				};
			} catch (TargetInvocationException Exception) {
				var InnerException = Exception.InnerException;
				Result = new TestResult(Test) {
					Outcome = TestOutcome.Failed,
					ErrorMessage = InnerException.Message,
					ErrorStackTrace = InnerException.StackTrace,
					
				};
			}
			
			aFrameworkHandle.RecordResult(Result);
			aFrameworkHandle.RecordEnd(Test, Result.Outcome);
		}
	}
	
	public void
	RunTests(
		IEnumerable<tText> aTestDllPaths,
		IRunContext aRunContext,
		IFrameworkHandle aFrameworkHandle
	) {
		this.RunTests(
			FakeTestDiscoverer.GetTests(aTestDllPaths),
			aRunContext,
			aFrameworkHandle
		);
	}
	
	public void
	Cancel(
	) {
		this._Cancelled = true;
	}
}
