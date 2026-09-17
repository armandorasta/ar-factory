using System;
using System.Linq;
using System.Reflection;
using Godot;
using Microsoft.CodeAnalysis;

namespace ArFactory.Tests;

public static class TestHandler
{
	public static void RunTests(Level lv, bool bShowPassingTests = false)
	{
		var totalTestCount = 0;
		var totalPassingTestCount = 0;
		Asserts.TreePtr = lv.GetTree();
		foreach (var tclass in Assembly.GetExecutingAssembly().GetTypes()
			.Where((t) => t.Name.EndsWith("Tests") && t.Namespace == "ArFactory.Tests"))
		{
			Asserts.CurrTestClass = tclass.Name;
			var tMethods = tclass.GetMethods(BindingFlags.Public | BindingFlags.Static);
			var currClassTestCount = tMethods.Length;
			totalTestCount += currClassTestCount;
			
			if (currClassTestCount == 0)
			{
				continue;
			}

			bool bPrintClassIntro = true;
			if (bShowPassingTests)
			{
				GD.Print("========================================================");
				GD.Print($"{tclass.Name}:");
				bPrintClassIntro = false;
			}
			var currClassPassingTestCount = 0;
			for (var i = 0; i < tMethods.Length; ++i)
			{
				var tmeth = tMethods[i];
				Asserts.CurrTestMethod = tmeth.Name;
				Debug.AssertEq(tmeth.GetParameters().Length, 1);
				Debug.AssertEq(tmeth.GetParameters()[0].ParameterType, typeof(Level));
				
				DoBeforeEachTest(lv);
				try
				{
					tmeth.Invoke(null, [lv]);
					if (bShowPassingTests)
					{
						GD.Print($"{tmeth.Name} passes!");
					}
					currClassPassingTestCount += 1;
				}
				catch (TargetInvocationException ex)
				{
					if (bPrintClassIntro)
					{
						GD.Print("========================================================");
						GD.Print($"{tclass.Name}:");
						bPrintClassIntro = false;
					}
					GD.Print(ex.InnerException.Message);

					if (ex.InnerException is not AssertFailedException)
					{
						throw;
					}
				}
			}

			totalPassingTestCount += currClassPassingTestCount;
			if (currClassPassingTestCount == currClassTestCount)
			{
				if (bShowPassingTests)
				{
					GD.Print($"{tclass.Name}: All tests pass!");
				}
			}
			else
			{
				GD.Print($"{tclass.Name}: {currClassPassingTestCount}/{currClassTestCount} tests pass.");
			}
			GD.Print("========================================================\n");
		}

		if (totalPassingTestCount == totalTestCount)
		{
			GD.Print("All tests pass!");
		}
		else
		{
			GD.Print($"Overall {totalPassingTestCount}/{totalTestCount} tests pass.");
		}

		lv.GetTree().Quit();
	}

	private static void DoBeforeEachTest(Level lv)
	{
		Debug.Assert(!lv.IsSimRunning());
		lv.World.Reset();
	}
}
