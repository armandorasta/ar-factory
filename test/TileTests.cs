using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class TileTests : ArTest.TestSuit
{
	public Editor Lvl => m_Lv!;
	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.LevelScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(Lvl);
		Lvl.Simy.SetTickRate(50);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(15, 10));
	}

	public override void AfterEach()
	{
		AssertTrue(Lvl.Simy.IsHalted());
	}

	public override void AfterAll()
	{
		Lvl.QueueFree();
		RemoveNode(Lvl);
	}

	[ArTest.Test] public void TestDefaultConstructor()
	{
		var world = Lvl.World;
		var tl = new Tile();
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));
		AssertFalse(tl.IsReserved());
	}

	[ArTest.Test] public void Test_SetWall_and_GetWall()
	{
		var tl = Lvl.World.InstallEmptyTile(Vector2I.Zero);
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.MakeSolid(Direction.West);
		AssertTrue(tl.IsSolid(Direction.West));
		tl.MakeInput(Direction.West, true);
		AssertTrue(tl.IsInput(Direction.West));
		tl.MakeOutput(Direction.West, true);
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

		tl.MakeSolid(Direction.West, true);
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsOutput(Direction.East));
		AssertTrue(tl.IsInput(Direction.South));
		AssertTrue(tl.IsSolid(Direction.West));
	}

	[ArTest.Test] public void Test_SetWall_and_GetWall_INVALID_CASE_overriding_without_flag()
	{
		var tl = Lvl.World.InstallEmptyTile(Vector2I.Zero);
		tl.MakeOutput(Direction.North);
		tl.ClearWall(Direction.North);
		tl.MakeOutput(Direction.North); // Should work still
		AssertTrue(tl.IsOutput(Direction.North));
		AssertThrow(() => tl.MakeOutput(Direction.North)); // Flag required
	}

	[ArTest.Test] public void TestRotation()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		tl.MakeSolid(Direction.North).MakeInput(Direction.East).MakeOutput(Direction.South);

		tl.Rotate90();
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsSolid(Direction.East));
		AssertTrue(tl.IsInput(Direction.South));
		AssertTrue(tl.IsOutput(Direction.West));
		tl.Rotate90();
		AssertTrue(tl.IsOutput(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsSolid(Direction.South));
		AssertTrue(tl.IsInput(Direction.West));
		tl.Rotate90();
		AssertTrue(tl.IsInput(Direction.North));
		AssertTrue(tl.IsOutput(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsSolid(Direction.West));
		tl.Rotate90();
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsInput(Direction.East));
		AssertTrue(tl.IsOutput(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));

		tl.Rotate180();
		AssertTrue(tl.IsOutput(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsSolid(Direction.South));
		AssertTrue(tl.IsInput(Direction.West));
		tl.Rotate180();
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsInput(Direction.East));
		AssertTrue(tl.IsOutput(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));


		tl.Rotate270();
		AssertTrue(tl.IsInput(Direction.North));
		AssertTrue(tl.IsOutput(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsSolid(Direction.West));
		tl.Rotate270();
		AssertTrue(tl.IsOutput(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsSolid(Direction.South));
		AssertTrue(tl.IsInput(Direction.West));
		tl.Rotate270();
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsSolid(Direction.East));
		AssertTrue(tl.IsInput(Direction.South));
		AssertTrue(tl.IsOutput(Direction.West));
		tl.Rotate270();
		AssertTrue(tl.IsSolid(Direction.North));
		AssertTrue(tl.IsInput(Direction.East));
		AssertTrue(tl.IsOutput(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));
	}

	[ArTest.Test] public void TestCanItemEnter()
	{
		var tl = Lvl.World.InstallEmptyTile(Vector2I.Zero);
		tl.MakeSolid(Direction.North);
		AssertFalse(tl.CanItemEnter(Direction.North));

		tl.MakeOutput(Direction.East);
		AssertFalse(tl.CanItemEnter(Direction.East));

		tl.MakeInput(Direction.South);
		AssertTrue(tl.CanItemEnter(Direction.South));

		tl.ClearWall(Direction.West);
		AssertTrue(tl.CanItemEnter(Direction.West));

		// Function should not take held items into consideration.
		Lvl.World.SpawnItem(Vector2I.Zero, 0);
		AssertFalse(tl.CanItemEnter(Direction.North));
		AssertFalse(tl.CanItemEnter(Direction.East));
		AssertTrue(tl.CanItemEnter(Direction.South));
		AssertTrue(tl.CanItemEnter(Direction.West));
	}

	[ArTest.Test] public void TestCanItemExit()
	{
		var tl = Lvl.World.InstallEmptyTile(Vector2I.Zero);
		tl.MakeSolid(Direction.North);
		tl.MakeOutput(Direction.East);
		tl.MakeInput(Direction.South);
		tl.ClearWall(Direction.West);

		AssertFalse(tl.CanItemExit(Direction.North));
		AssertTrue(tl.CanItemExit(Direction.East));
		AssertFalse(tl.CanItemExit(Direction.South));
		AssertTrue(tl.CanItemExit(Direction.West));

		// Function should not take held items into consideration.
		Lvl.World.SpawnItem(Vector2I.Zero, 0);
		AssertFalse(tl.CanItemExit(Direction.North));
		AssertTrue(tl.CanItemExit(Direction.East));
		AssertFalse(tl.CanItemExit(Direction.South));
		AssertTrue(tl.CanItemExit(Direction.West));
	}

	[ArTest.Test] public void TestDestroyItem_vanilla()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);

		var it = world.SpawnItem(tl, 8)!;
		tl.DestroyItem();
		AssertFalse(tl.HasItem());
		AssertTrue(it.IsQueuedForDeletion());
	}

	[ArTest.Test] public void TestDestroyItem_with_no_item_with_flag()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		tl.DestroyItem(true); // Should just do nothing and not cause any trouble in release.
		AssertFalse(tl.HasItem());
	}

	[ArTest.Test] public void TestDestroyItem_INVALID_CASE_with_no_item_no_flag()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		AssertThrow(() => tl.DestroyItem());
	}

	[ArTest.Test] public void TestExtractItem_vanilla()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var spawnedIt = world.SpawnItem(tl, 7);

		var extractedIt = AddNode(tl.ExtractItem()!);
		AssertFalse(tl.HasItem());
		AssertRefEq(spawnedIt, extractedIt);
		AssertEq(extractedIt.Value, 7);
	}

	[ArTest.Test] public void TestExtractItem_no_item_with_flag()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);

		var extractedIt = tl.ExtractItem(true);
		AssertNull(extractedIt);
		AssertFalse(tl.HasItem());
	}

	[ArTest.Test] public void TestExtractItem_no_item_no_flag()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		AssertThrow(() => tl.ExtractItem());
	}
}