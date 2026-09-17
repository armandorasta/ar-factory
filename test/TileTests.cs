using Godot;
using System;

namespace ArFactory.Tests;
using static Asserts;

public class TileTests
{
	public static void TestWalls(Level lv)
	{
		var tl = lv.World.InstallTile(new Tile(new(0, 0)));
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeSolid(Direction.North);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.AddInput(Direction.East);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsInput(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.AddOutput(Direction.South);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsInput(Direction.East));
		AssertTrue(tl.IsOutput(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeSolid(Direction.West);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsInput(Direction.East));
		AssertTrue(tl.IsOutput(Direction.South));
		AssertTrue(tl.IsSolid(Direction.West));
	}

	public static void FailingTest(Level lv)
	{
		AssertTrue(false);
	}
}
