using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class TileTests : ArTest.TestSuit
{
	private Level m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		m_Lv = levelScene.Instantiate<Level>();
		AddNode(m_Lv);
		m_Lv.SetTickRate(50);
	}

	public override void BeforeEach()
	{
		m_Lv.World.Reset();
		m_Lv.World.SetDims(new(15, 10));
	}

	public override void AfterEach()
	{
		AssertFalse(m_Lv.IsSimRunning());
	}

	public override void AfterAll()
	{
		m_Lv.QueueFree();
		RemoveNode(m_Lv);
	}

	[ArTest.Test] public void Test_SetWall_and_GetWall()
	{
		var tl = m_Lv.World.InstallEmptyTile(Vector2I.Zero);
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeSolid(Direction.West);
		AssertTrue(tl.IsSolid(Direction.West));
		tl.MakeInput(Direction.West);
		AssertTrue(tl.IsInput(Direction.West));
		tl.MakeOutput(Direction.West);
		AssertTrue(tl.IsOutput(Direction.West));
		tl.ClearWall(Direction.West);
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeSolid(Direction.North);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeOutput(Direction.East);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsOutput(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeInput(Direction.South);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsOutput(Direction.East));
		AssertTrue(tl.IsInput(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeSolid(Direction.West);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsOutput(Direction.East));
		AssertTrue(tl.IsInput(Direction.South));
		AssertTrue(tl.IsSolid(Direction.West));
	}

	[ArTest.Test] public void TestCanItemEnter()
	{
		var tl = m_Lv.World.InstallEmptyTile(Vector2I.Zero);
		tl.MakeSolid(Direction.North);
		tl.MakeOutput(Direction.East);
		tl.MakeInput(Direction.South);
		tl.ClearWall(Direction.West);
		
		AssertFalse(tl.CanItemEnter(Direction.North));
		AssertFalse(tl.CanItemEnter(Direction.East));
		AssertTrue(tl.CanItemEnter(Direction.South));
		AssertTrue(tl.CanItemEnter(Direction.West));

		// Function should not take held items into consideration.
		m_Lv.World.SpawnItem(Vector2I.Zero, 0);
		AssertFalse(tl.CanItemEnter(Direction.North));
		AssertFalse(tl.CanItemEnter(Direction.East));
		AssertTrue(tl.CanItemEnter(Direction.South));
		AssertTrue(tl.CanItemEnter(Direction.West));
	}

	[ArTest.Test] public void TestCanItemExit()
	{
		var tl = m_Lv.World.InstallEmptyTile(Vector2I.Zero);
		tl.MakeSolid(Direction.North);
		tl.MakeOutput(Direction.East);
		tl.MakeInput(Direction.South);
		tl.ClearWall(Direction.West);

		AssertFalse(tl.CanItemExit(Direction.North));
		AssertTrue(tl.CanItemExit(Direction.East));
		AssertFalse(tl.CanItemExit(Direction.South));
		AssertTrue(tl.CanItemExit(Direction.West));

		// Function should not take held items into consideration.
		m_Lv.World.SpawnItem(Vector2I.Zero, 0);
		AssertFalse(tl.CanItemExit(Direction.North));
		AssertTrue(tl.CanItemExit(Direction.East));
		AssertFalse(tl.CanItemExit(Direction.South));
		AssertTrue(tl.CanItemExit(Direction.West));
	}

	[ArTest.Test] public void Test_WorldPanel_InstallTile_and_DeleteTile()
	{
		var world = m_Lv.World;
		AssertFalse(world.HasTile(Vector2I.One));
		AssertNull(world[1, 1]);

		var tl = world.InstallEmptyTile(Vector2I.One);
		AssertEq(tl.GridLoc, Vector2I.One);
		AssertTrue(world.HasTile(Vector2I.One));
		AssertRefEq(world[1, 1], tl);
		
		world.DeleteTile(Vector2I.One);
		AssertFalse(world.HasTile(Vector2I.One));
		AssertNull(world[1, 1]);
	}

	[ArTest.Test] public void Test_WorldPanel_SpawnItem_and_DestroyItem()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		AssertFalse(world[0, 0].HasItem());
		AssertNotNull(world.SpawnItem(tl.GridLoc, 7));
		AssertEq(tl.Item.Value, 7);
		AssertNull(world.SpawnItem(tl.GridLoc, 8)); // Should just give up in release.
		AssertEq(tl.Item.Value, 7);

		tl.DestroyItem();
		AssertFalse(tl.HasItem());
		AssertNotNull(world.SpawnItem(tl.GridLoc, 8));
		AssertTrue(tl.HasItem());
		AssertEq(tl.Item.Value, 8);

		tl.DestroyItem();
		AssertFalse(tl.HasItem());
		
		tl.DestroyItem(); // Can be called on empty tiles? on debug you have to pass in bMaybeNull.
		AssertFalse(tl.HasItem());
	}

	[ArTest.Test] public void Test_WorldPanel_ExtractItem_and_InstallItem()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var spawnedIt = world.SpawnItem(tl.GridLoc, 7);
		AssertRefEq(spawnedIt.GetParent(), world);

		var extractedIt = world[0, 0].ExtractItem();
		AssertNull(extractedIt.GetParent());
		// TODO: When you add parent tile reference, check for it here!
		AssertFalse(world[0, 0].HasItem());
		AssertRefEq(spawnedIt, extractedIt);
		AssertEq(extractedIt.Value, 7);

		Item installedIt = world.InstallItem(extractedIt);
		AssertRefEq(installedIt.GetParent(), world);
		AssertRefEq(installedIt, extractedIt);
		AssertRefEq(world.GetTile(Vector2I.Zero).Item, installedIt);
		AssertEq(installedIt.Value, 7);
	}

	[ArTest.Test] public void Test_WorldPanel_ExtractTile()
	{
		var world = m_Lv.World;
		var installedTl = world.InstallEmptyTile(new(3, 4));
		var extractedTl = world.ExtractTile(installedTl.GridLoc);
		AssertRefEq(installedTl, extractedTl);
		AssertFalse(world.HasTile(installedTl.GridLoc));

		extractedTl.SetGridLocUnsafe(Vector2I.Zero); // Should be ignored and overriden.
		var anotherTl = world.InstallTile(extractedTl, new(1, 2));
		AssertTrue(world.HasTile(new(1, 2)));
		AssertFalse(world.HasTile(new(3, 4))); // Ignore the tile's grid-loc
		AssertRefEq(anotherTl, extractedTl);

		world.DeleteTile(anotherTl.GridLoc);

		// Must pass bMaybeNull in debug.
		AssertNull(world.ExtractTile(Vector2I.Zero));
		AssertNull(world.ExtractTile(Vector2I.Zero));
	}
}