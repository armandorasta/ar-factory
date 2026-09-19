using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Microsoft.CodeAnalysis;

namespace ArFactory.Tests;
using static AnsiColors;

public class TestHandler
{
	private struct TestClassResult
	{
		public int TestCount;
		public int PassingTestCount;
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
		Debug.DisableAsserts();

		var totalTestCount = 0;
		var totalPassingTestCount = 0;
		foreach (var tclass in Assembly.GetExecutingAssembly().GetTypes()
			.Where((t) => t.IsAbstract 
				&& t.IsSealed 
				&& t.Name.EndsWith("Tests") 
				&& t.Namespace == "ArFactory.Tests"))
		{
			var tres = await HandleTestClass(tclass);
			totalTestCount += tres.TestCount;
			totalPassingTestCount += tres.PassingTestCount;
		}

		if (totalPassingTestCount == totalTestCount)
		{
			GD.Print($"{Green}ALL TESTS PASS! 🥳{Reset}");
		}
		else
		{
			GD.Print($"Overall {Green}{totalPassingTestCount}{Reset}/{totalTestCount} tests pass.");
		}
	}

	private async Task<TestClassResult> HandleTestClass(Type tclass)
	{
		var res = new TestClassResult();
		
		Asserts.CurrTestClass = tclass.Name;
		var tMethods = tclass.GetMethods(BindingFlags.Public | BindingFlags.Static);
		res.TestCount = tMethods.Length;

		if (res.TestCount == 0)
		{
			return res;
		}

		// Used to print the intro before the first failing test when passing tests are not shown.
		// We don't wanna print intros for classes with no failing tests in that case.
		bool bPrintClassIntro = true;

		if (m_bShowPassingTests)
		{
			GD.Print("========================================================");
			GD.Print($"{Pink}{tclass.Name}:{Reset}");
			bPrintClassIntro = false;
		}

		for (var i = 0; i < tMethods.Length; ++i)
		{
			var tmeth = tMethods[i];
			Asserts.CurrTestMethod = tmeth.Name;
			Debug.AssertEq(tmeth.GetParameters().Length, 1);
			Debug.AssertEq(tmeth.GetParameters()[0].ParameterType, typeof(Level));

			var isAsync = tmeth.GetCustomAttribute<AsyncStateMachineAttribute>() is not null;
			if (isAsync)
			{
				if (tmeth.ReturnType != typeof(Task))
				{
					throw new InvalidProgramException("Async tests must return Task");
				}
			}
			else if (tmeth.ReturnType != typeof(void))
			{
				throw new InvalidProgramException("Sync tests must return void");
			}

			DoBeforeEachTest();
			try
			{
				if (isAsync)
				{
					await (Task)tmeth.Invoke(null, [m_Level]);
				}
				else
				{
					tmeth.Invoke(null, [m_Level]);
				}

				res.PassingTestCount += 1;
				if (m_bShowPassingTests)
				{
					GD.Print($"    {Green}{tmeth.Name} passes!{Reset}");
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

		if (res.PassingTestCount == res.TestCount)
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
