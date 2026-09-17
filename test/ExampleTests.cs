using Godot;
using GdUnit4;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArFactory.Tests;

using static Assertions;

// [TestSuite]
[RequireGodotRuntime]
public class ExampleTests
{
	private ISceneRunner m_Runner;
	private Level m_Level;


	// [Before]
	// public void Before()
	// {
	// 	m_Runner = ISceneRunner.Load("res://scenes/level.tscn");
	// 	var LevelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
	// 	m_Level = LevelScene.Instantiate<Level>();
	// 	AddNode(m_Level, false);
	// }

	// [After]
	// public void After()
	// {
	// 	m_Level.QueueFree();
	// }

	// [TestCase]
	// public void TestSomething()
	// {
	// 	AssertThat(5).IsEqual(5);
	// 	AssertThat(new int[5]).HasSize(5);
	// }

	// [TestCase]
	// [GodotExceptionMonitor]
	// public void TestGodotStuff()
	// {
	// 	AssertBool(m_Level.World.IsWithin(new(0, 6))).IsTrue();
	// 	m_Level.World.PlaceSupplier(new(0, 0), Direction.East, 1, [1, 2, 3]);
	// 	AssertObject(m_Level.World.GetUnit(new(0, 0))).IsNotNull();
	// }

	// public static IEnumerable<object[]> GenerateAdditionTestData(int addedVal) => [
	// 	[1, 2, 3 + addedVal, (6 + addedVal).ToString()],
	// 	[3, 4, 5 + addedVal, (12 + addedVal).ToString()],
	// 	[6, 7, 8 + addedVal, (21 + addedVal).ToString()],
	// ];

	// [TestCase]
	// [DataPoint(nameof(GenerateAdditionTestData), 5)]
	// public void TestAddition(int a, double b, int c, string expect)
	// {
	// 	AssertThat((a + b + c).ToString()).IsEqual(expect);
	// }
}
