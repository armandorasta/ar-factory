using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static AnsiColors;

public class TestHandler
{
	private struct TestClassResult
	{
		public int TestCount;
		public int PassingTestCount;
		public int IgnoredTestCount;
	}


	private Level m_Level;
	private bool m_bShowPassingTests;
	private Task m_WholeProgramTask;

	/// <summary>
	/// Must call <see cref="RunTests"/> on _Ready, and <see cref="OnProcess"/> in _Process!
	/// </summary>
	public TestHandler(Level lv, bool bShowPassingTests)
	{
		m_Level = lv;
		m_bShowPassingTests = bShowPassingTests;
	}
	/// <summary>
	/// Must be called in _Ready.
	/// </summary>
	public void RunTests()
	{
		m_WholeProgramTask = RunTestsImpl();
	}
	/// <summary>
	/// Must be called in _Process.
	/// </summary>
	public void OnProcess()
	{
		if (m_WholeProgramTask is { IsCompleted: true })
		{
			m_Level.GetTree().Quit();
		}
	}


	private async Task RunTestsImpl()
	{
		Asserts.TreePtr = m_Level.GetTree();

		var totalTestCount = 0;
		var totalPassingTestCount = 0;
		var totalIgnoredTests = 0;

		var testClasses = Assembly.GetExecutingAssembly()
			.GetTypes().Where((c) => c.GetCustomAttribute<TestSuiteAttribute>() != null).ToArray();
		
		var runFirstMeths = testClasses
			.SelectMany(c => c.GetMethods().Where(m => m.GetCustomAttribute<RunThisOnlyAttribute>() != null))
			.ToArray();
		
		MethodInfo onlyTest = null;
		if (runFirstMeths.Length > 0)
		{
			if (runFirstMeths.Length > 1)
			{
				GD.Print($"Found a {Yellow}{nameof(RunThisOnlyAttribute)}{Reset} on more than 1 test:");
				for (var i = 0; i < runFirstMeths.Length; ++i)
				{
					var meth = runFirstMeths[i];
					GD.Print($"[{i+1}] {Cyan}{meth.Name}{Reset} from {Pink}{meth.DeclaringType.Name}{Reset}");
				}

				return;
			}

			onlyTest = runFirstMeths[0];
			GD.Print($"Only running test: {Cyan}{onlyTest.Name}{Reset} from {Pink}{onlyTest.DeclaringType.Name}{Reset}.");

			testClasses = [onlyTest.DeclaringType];
		}

		foreach (var tclass in testClasses)
		{
			if (tclass.GetCustomAttribute<IgnoreAttribute>() != null)
			{
				totalIgnoredTests += tclass.GetMethods().Length;
				continue;
			}
			
			var tres = await HandleTestClass(tclass, onlyTest);
			totalTestCount += tres.TestCount;
			totalPassingTestCount += tres.PassingTestCount;
			totalIgnoredTests += tres.IgnoredTestCount;
		}

		var ignoredMsg = (totalIgnoredTests > 0) ? $" ({Yellow}{totalIgnoredTests}{Reset} ignored)" : "";

		if (totalTestCount == 0 && totalIgnoredTests == 0)
		{
			GD.Print($"{Yellow}Zero tests were ran...{Reset}");
		}
		else if (totalTestCount == totalIgnoredTests)
		{
			GD.Print($"{Yellow}All {totalIgnoredTests} tests were ignored...{Reset}");
		}
		else if (totalPassingTestCount + totalIgnoredTests == totalTestCount)
		{
			GD.Print($"{Green}ALL TESTS PASS! 🥳{Reset}{ignoredMsg}.");
		}
		else
		{
			GD.Print($"Overall {Green}{totalPassingTestCount}{Reset}/{totalTestCount} tests pass{ignoredMsg}.");
		}
	}

	private async Task<TestClassResult> HandleTestClass(Type tclass, MethodInfo onlyRunThis = null)
	{
		var instance = tclass.GetConstructor([]).Invoke([]);
		var res = new TestClassResult();
		
		Asserts.CurrTestClass = tclass.Name;
		var allMethods = tclass.GetMethods();
		var tMethods = (onlyRunThis != null) 
			? [onlyRunThis] 
			: allMethods.Where(m => m.GetCustomAttribute<TestAttribute>() != null);
		var beforeAllMethod  = Array.Find(allMethods, m => m.Name == "BeforeAll");
		var afterAllMethod   = Array.Find(allMethods, m => m.Name == "AfterAll");
		var beforeEachMethod = Array.Find(allMethods, m => m.Name == "BeforeEach");
		var afterEachMethod  = Array.Find(allMethods, m => m.Name == "AfterEach");

		// Used to print the intro before the first failing test when passing tests are not shown.
		// We don't wanna print intros for classes with no failing tests in that case.
		bool bPrintClassIntro = true;

		if (m_bShowPassingTests)
		{
			GD.Print("========================================================");
			GD.Print($"{Pink}{tclass.Name}:{Reset}");
			bPrintClassIntro = false;
		}

		beforeAllMethod?.Invoke(instance, [m_Level]);
		foreach (var tmeth in tMethods)
		{
			res.TestCount += 1;
			if (tmeth.GetCustomAttribute<IgnoreAttribute>() != null)
			{
				res.IgnoredTestCount += 1;
				continue;
			}

			Asserts.CurrTestMethod = tmeth.Name;
			Debug.AssertEq(tmeth.GetParameters().Length, 1);
			Debug.AssertEq(tmeth.GetParameters()[0].ParameterType, typeof(Level));

			var isAsync = tmeth.GetCustomAttribute<AsyncStateMachineAttribute>() is not null;
			Debug.Assert(isAsync && tmeth.ReturnType == typeof(Task) || tmeth.ReturnType == typeof(void));
			
			DoBeforeEachTest();
			try
			{
				Debug.DisableAsserts();
				beforeEachMethod?.Invoke(instance, [m_Level]);
				if (isAsync)
				{
					await (Task)tmeth.Invoke(instance, [m_Level]);
				}
				else
				{
					tmeth.Invoke(instance, [m_Level]);
				}
				afterEachMethod?.Invoke(instance, [m_Level]);
				Debug.EnableAsserts();

				res.PassingTestCount += 1;
				if (m_bShowPassingTests)
				{
					GD.Print($"    {Cyan}{tmeth.Name} {Green}passes!{Reset}");
				}
			}
			catch (TargetInvocationException ex)
			{
				if (bPrintClassIntro)
				{
					GD.Print("========================================================");
					GD.Print($"{Pink}{tclass.Name}:{Reset}");
					bPrintClassIntro = false;
				}

				GD.Print(ex.InnerException.Message.Indent("    "));

				if (ex.InnerException is not TestAssertFailedException)
				{
					throw;
				}
			}
		}
		afterAllMethod?.Invoke(instance, [m_Level]);

		if (res.TestCount == 0 && res.IgnoredTestCount == 0)
		{
			GD.Print($"{Yellow}Zero tests were ran in of {Pink}{tclass.Name}{Yellow}...{Reset}");
			GD.Print("========================================================\n");
		}
		else if (res.TestCount == res.IgnoredTestCount)
		{
			GD.Print($"{Yellow}All {res.IgnoredTestCount} tests of {Pink}{tclass.Name}{Yellow} were ignored...{Reset}");
			GD.Print("========================================================\n");
		}
		else if (res.PassingTestCount + res.IgnoredTestCount == res.TestCount)
		{
			if (m_bShowPassingTests)
			{
				GD.Print($"{Green}All tests of {Pink}{tclass.Name} {Green}pass!{Reset}");
				GD.Print("========================================================\n");
			}
		}
		else
		{
			GD.Print($"{Green}{res.PassingTestCount}{Reset}/{res.TestCount} tests of {Pink}{tclass.Name}{Reset} pass.");
			GD.Print("========================================================\n");
		}

		return res;
	}

	private void DoBeforeEachTest()
	{
		Debug.Assert(!m_Level.IsSimRunning());
		m_Level.World.Reset();
		m_Level.World.SetDims(new(15, 10));
	}
}
