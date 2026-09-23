using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace ArTest;
using static AnsiColors;

public partial class TestHandler : Godot.Node
{
	private struct TestClassResult
	{
		public int TestCount;
		public int PassingTestCount;
		public int IgnoredTestCount;
	}

	private static TestHandler s_Instance;


	public bool IsShowPassingTests 
	{ 
		get => m_bShowPassingTests;
		set => m_bShowPassingTests = value; 
	}

	public bool IsPrintOrphanNodes
	{ 
		get => m_bPrintOrphanNodes;
		set => m_bPrintOrphanNodes = value; 
	}

	private bool m_bShowPassingTests = false;
	private Task m_WholeProgramTask;

	private bool m_bPrintOrphanNodes = false;
	private int m_OrphanCount = 0;


	public static TestHandler GetInstance() => s_Instance;


	/// <summary>
	/// Must call <see cref="RunTests"/> on _Ready, and <see cref="OnProcess"/> in _Process!
	/// </summary>
	public TestHandler()
	{
		s_Instance = this;
	}

	public override void _Ready()
	{
		m_WholeProgramTask = RunTestsImpl();
	}

	public override void _Process(double dt)
	{
		if (m_WholeProgramTask is { IsCompleted: true })
		{
			GetTree().Quit();
		}
	}

	private async Task RunTestsImpl()
	{
		Asserts.TreePtr = GetTree();

		var totalTestCount = 0;
		var totalPassingTestCount = 0;
		var totalIgnoredTests = 0;

		var testClasses = Assembly.GetExecutingAssembly().GetTypes()
			.Where(c => c.IsSubclassOf(typeof(TestSuit)))
			.ToArray();
		
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
			GD.Print($"{Green}ALL {totalTestCount} TESTS PASS! 🥳{Reset}{ignoredMsg}.");
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

		// Used to print the intro before the first failing test when passing tests are not shown.
		// We don't wanna print intros for classes with no failing tests in that case.
		bool bPrintClassIntro = true;

		if (m_bShowPassingTests)
		{
			GD.Print("========================================================");
			GD.Print($"{Pink}{tclass.Name}:{Reset}");
			bPrintClassIntro = false;
		}

		tclass.GetMethod("BeforeAll")?.Invoke(instance, []);
		foreach (var tmeth in tMethods)
		{
			res.TestCount += 1;
			if (tmeth.GetCustomAttribute<IgnoreAttribute>() != null)
			{
				res.IgnoredTestCount += 1;
				continue;
			}

			Asserts.CurrTestMethod = tmeth.Name;
			if (tmeth.GetParameters().Length > 0)
			{
				throw new InvalidProgramException($"Found a test ({Pink}{tmeth}{Reset}) that takes parameters, remove them.");
			}

			var bAsync = tmeth.GetCustomAttribute<AsyncStateMachineAttribute>() is not null;
			if (bAsync)
			{
				if (tmeth.ReturnType != typeof(Task))
				{
					throw new InvalidProgramException($"Async test {Pink}{tmeth}{Reset} must return Task");
				}
			}
			else 
			{
				if (tmeth.ReturnType != typeof(void))
				{
					throw new InvalidProgramException($"Sync test {Pink}{tmeth}{Reset} must return void");
				}
			}
			
			try
			{
				DoBeforeEachTest(tmeth);
				tclass.GetMethod("BeforeEach")?.Invoke(instance, []);
				if (bAsync)
				{
					await (Task)tmeth.Invoke(instance, []);
				}
				else
				{
					tmeth.Invoke(instance, []);
				}

				// Wait for the frame to end first.
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				// Then call the after test stuff.
				tclass.GetMethod("AfterEach")?.Invoke(instance, []);
				DoAfterEachTest(tmeth);

				res.PassingTestCount += 1;
				if (m_bShowPassingTests)
				{
					GD.Print($"    {Cyan}{tmeth.Name} {Green}passes!{Reset}");
				}
			}
			catch (Exception ex)
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
		tclass.GetMethod("AfterAll")?.Invoke(instance, []);

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

	private void DoBeforeEachTest(MethodInfo meth)
	{
	}

	private void DoAfterEachTest(MethodInfo meth)
	{
		var allOrphanBros = GetOrphanNodeIds();
		var nCurrTestOrphan = allOrphanBros.Count - m_OrphanCount;
		if (nCurrTestOrphan > 0)
		{
			GD.Print($"    {nCurrTestOrphan} {Red}orphan nodes detected{Reset}");
		}

		if (m_bPrintOrphanNodes)
		{
			PrintOrphanNodes();
		}

		m_OrphanCount = allOrphanBros.Count;
	}
}
