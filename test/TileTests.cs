using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
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

	[ArTest.Test] public void TestDefaultConstructor()
	{
		var world = m_Lv.World;
		var tl = new Tile();
		AssertTrue(tl.IsClear(Direction.North));
		AssertTrue(tl.IsClear(Direction.East));
		AssertTrue(tl.IsClear(Direction.South));
		AssertTrue(tl.IsClear(Direction.West));
		AssertFalse(tl.IsReserved());
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

	[ArTest.Test] public void TestRotation()
	{
		var world = m_Lv.World;
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
		var tl = m_Lv.World.InstallEmptyTile(Vector2I.Zero);
		tl.MakeSolid(Direction.North);
		AssertFalse(tl.CanItemEnter(Direction.North));
		
		tl.MakeOutput(Direction.East);
		AssertFalse(tl.CanItemEnter(Direction.East));
		
		tl.MakeInput(Direction.South);
		AssertTrue(tl.CanItemEnter(Direction.South));
		
		tl.ClearWall(Direction.West);
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

	[ArTest.Test] public void TestDestroyItem()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);		
		var it = world.SpawnItem(tl, 8);
		AssertNotNull(it);
		AssertTrue(tl.HasItem());
		AssertEq(it.Value, 8);

		tl.DestroyItem();
		AssertFalse(tl.HasItem());
		AssertTrue(it.IsQueuedForDeletion());
		
		tl.DestroyItem(); // Should just do nothing and not cause any trouble in release.
		AssertFalse(tl.HasItem());
		AssertTrue(it.IsQueuedForDeletion());
	}

	[ArTest.Test] public void TestExtractItem()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var spawnedIt = world.SpawnItem(tl, 7);

		var extractedIt = AddNode(tl.ExtractItem());
		AssertFalse(tl.HasItem());
		AssertRefEq(spawnedIt, extractedIt);
		AssertEq(extractedIt.Value, 7);
	}
}