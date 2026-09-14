using Godot;
using GdUnit4;

namespace ArFactory;
using static Assertions;

[TestSuite]
[RequireGodotRuntime]
public class TestUnit
{
	// private static ISceneRunner s_SceneRunner;
	

	[Before] public void Before()
	{
		// s_SceneRunner = ISceneRunner.Load("res://scenes/level.tscn");
	}


	[TestCase] public void TestSomething()
	{
		AssertThat(5).IsEqual(5);
		AssertThat(new int[5]).HasSize(5);	
	}

	[TestCase] [RequireGodotRuntime] public void TestGodotStuff()
	{
		var LevelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		var lv = LevelScene.Instantiate<Level>();
		AddNode(lv);
		lv.World.PlaceSupplier(new(0, 6), Direction.East, 1, [1, 2, 3]);
		// AssertThat(lv.World.GetUnit(new(0, 6))).IsNotNull();
	}
}
