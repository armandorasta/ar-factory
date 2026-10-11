using System;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class WorldPanelTests : ArTest.TestSuit
{
	public Level Lvl => m_Lv!;
	private Level? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		m_Lv = levelScene.Instantiate<Level>();
		AddNode(m_Lv);
		m_Lv.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(15, 10));
	}

	public override void AfterEach()
	{
		AssertFalse(Lvl.IsSimRunning());
	}

	public override void AfterAll()
	{
		Lvl.QueueFree();
		RemoveNode(Lvl);
	}

	[ArTest.Test] public void TestInstallTile()
	{
		var world = Lvl.World;
		AssertFalse(world.HasTile(Vector2I.One));
		AssertNull(world[1, 1]);

		var tl = new Tile();
		var reTl = world.InstallTile(tl, Vector2I.One);
		AssertRefEq(tl, reTl);
		AssertRefEq(world[1, 1], tl);
		AssertEq(tl.GLocation, Vector2I.One);
		AssertTrue(world.HasTile(Vector2I.One));

		var newTl = world.InstallTile(new Tile(), Vector2I.One); // Should override the old one.
		AssertRefNotEq(tl, newTl);
		AssertRefEq(world[1, 1], newTl);

		// Installing out of bounds will crash the program.
		AssertThrow(() => world.InstallTile(new Tile(), Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestDeleteTile()
	{
		// Items will not be tested here, as tile tests don't depend on item tests.

		var world = Lvl.World;
		world.InstallEmptyTile(Vector2I.One);
		world.DeleteTile(Vector2I.One);
		AssertFalse(world.HasTile(Vector2I.One));
		AssertNull(world[1, 1]);

		world.DeleteTile(Vector2I.One); // Should just do nothing

		// Deleting out of bounds will crash the program.
		AssertThrow(() => world.DeleteTile(Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestMoveTile()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var movedTl = world.MoveTile(tl, Vector2I.One);
		AssertRefEq(tl, movedTl);
		AssertFalse(world.HasTile(Vector2I.Zero));
		AssertTrue(world.HasTile(Vector2I.One));
		AssertEq(tl.GLocation, Vector2I.One);

		// Moving from and/or to out of bounds crashes the program
		AssertThrow(() => world.MoveTile(Vector2I.One * 1000, Vector2I.One * 1001));
	}

	[ArTest.Test] public void TestExtractTile()
	{
		var world = Lvl.World;
		var installedTl = world.InstallEmptyTile(new(3, 4));
		var extractedTl = world.ExtractTile(installedTl.GLocation);
		AssertRefEq(installedTl, extractedTl);
		AssertFalse(world.HasTile(installedTl.GLocation));

		extractedTl!.SetGridLocUnsafe(Vector2I.Zero); // Should be ignored and overriden.
		var anotherTl = world.InstallTile(extractedTl, new(1, 2));
		AssertTrue(world.HasTile(new(1, 2)));
		AssertFalse(world.HasTile(new(3, 4))); // Ignore the tile's grid-loc
		AssertRefEq(anotherTl, extractedTl);

		world.DeleteTile(anotherTl);

		// Must pass bMaybeNull in debug.
		AssertNull(world.ExtractTile(Vector2I.Zero));
		AssertNull(world.ExtractTile(Vector2I.Zero));

		// Extracting out of bounds will crash the program.
		AssertThrow(() => world.ExtractTile(Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestIsInstalled()
	{
		var world = Lvl.World;
		var tl = new Tile();
		AssertFalse(world.IsInstalled(tl));
		
		world.InstallTile(tl, Vector2I.One);
		AssertTrue(world.IsInstalled(tl));
	}

	[ArTest.Test] public void TestSpawnItem()
	{
		var world = Lvl.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		AssertFalse(tl.HasItem());

		var it = world.SpawnItem(tl, 7)!;
		AssertNotNull(it);
		AssertRefEq(it, tl.Item);
		AssertTrue(tl.HasItem());
		AssertEq(tl.Item.Value, 7);
		
		AssertNull(world.SpawnItem(tl, 8)); // Should just give up in release.
		AssertRefEq(it, tl.Item);
		AssertEq(tl.Item.Value, 7);

		world.DeleteTile(tl);
		AssertTrue(it.IsQueuedForDeletion());

		// Spawning out of bounds crashes the program
		AssertThrow(() => world.SpawnItem(Vector2I.One * 1000, -1));
	}

	[ArTest.Test] public void TestInstallItem()
	{
		var world = Lvl.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(Vector2I.One);
		world.SpawnItem(tl0, 7);
		var extractedIt = tl0.ExtractItem()!;

		var installedIt = world.InstallItem(extractedIt, tl1);
		AssertRefEq(installedIt, extractedIt);
		AssertRefEq(tl1.Item, installedIt);
		AssertEq(installedIt!.Value, 7);
		AssertFalse(tl0.HasItem());

		// Installing null crashes the program.
		// Though for now installing null in a non-existant tile just does nothing...
		Item? some = null; // For the wrarning to disappear
		AssertThrow(() => world.InstallItem(some!, tl0), typeof(NullReferenceException));

		var it1 = AddNode(tl1.ExtractItem()!);
		
		// Installing into a non-exitant IN-BOUNDS tile => noop
		AssertNull(world.InstallItem(it1, new Vector2I(1, 0)));
		
		// Installing out of bounds crashes the program
		AssertThrow(() => world.InstallItem(it1, Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestCloneItem()
	{
		var world = Lvl.World;
		var tl0 = world.InstallEmptyTile(new(1, 2));
		var tl1 = world.InstallEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 5)!;
		var cloneIt = world.CloneItem(tl0, tl1);
		AssertTrue(tl0.HasItem());
		AssertTrue(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());
		AssertRefEq(it0, tl0.Item);
		AssertRefNotEq(it0, cloneIt);
		AssertRefNotEq(tl1.Item, tl0.Item); // Must not be the same for obvious reasons...
		AssertEq(tl1.Item.Value, tl0.Item.Value);

		// Both have items, should just override the destination (requires bOverride in debug).
		tl0.Item.SetValue(6); // Both items had the same value...
		AssertNotEq(tl0.Item.Value, tl1.Item.Value);
		cloneIt = world.CloneItem(tl0, tl1);
		AssertTrue(tl0.HasItem());
		AssertTrue(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());
		AssertRefEq(cloneIt, tl1.Item);
		AssertRefNotEq(tl0.Item, tl1.Item);
		AssertEq(tl1.Item.Value, tl0.Item.Value);
		AssertEq(tl1.Item.Value, 6);

		// Clone null into a tile with an item, should just do nothing.
		tl0.DestroyItem();
		cloneIt = world.CloneItem(tl0, tl1);
		AssertNull(cloneIt);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());
		AssertEq(tl1.Item.Value, 6);

		// Cloning null into null, should just do nothing.
		tl1.DestroyItem();
		cloneIt = world.CloneItem(tl0, tl1);
		AssertNull(cloneIt);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertFalse(tl1.HasItem());
		AssertFalse(tl1.IsReserved());

		// To out of bounds => crash
		AssertThrow(() => world.CloneItem(it0.GridLoc, Vector2I.One * 1000));

		// From out of bounds => crash
		AssertThrow(() => world.CloneItem(Vector2I.One * 1000, it0.GridLoc));

		// When both are the same a crash is not required for now, that would require extra checks...
	}

	[ArTest.Test] public void TestTeleportItem()
	{
		var world = Lvl.World;
		var tl0 = world.InstallEmptyTile(new(1, 2));
		var tl1 = world.InstallEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 50)!;
		var teleIt = world.TeleportItem(tl0, tl1);
		AssertRefEq(it0, teleIt);
		AssertRefEq(it0, tl1.Item);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());
		AssertEq(tl1.Item.Value, 50);

		// Both have items, should just override the destination (requires bOverride in debug).
		it0 = world.SpawnItem(tl0, 85)!;
		teleIt = world.TeleportItem(tl0, tl1);
		AssertRefEq(it0, teleIt);
		AssertRefEq(it0, tl1.Item);
		AssertEq(tl1.Item.Value, 85);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());

		// Teleport null into a tile with an item, should just do nothing.
		teleIt = world.TeleportItem(tl0, tl1);
		AssertNull(teleIt);
		AssertEq(tl1.Item.Value, 85);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());

		// Cloning null into null, should just do nothing.
		tl1.DestroyItem();
		teleIt = world.TeleportItem(tl0, tl1);
		AssertNull(teleIt);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertFalse(tl0.HasItem());
		AssertFalse(tl1.IsReserved());

		// To out of bounds => crash
		AssertThrow(() => world.TeleportItem(it0.GridLoc, Vector2I.One * 1000));

		// From out of bounds => crash
		AssertThrow(() => world.TeleportItem(Vector2I.One * 1000, it0.GridLoc));

		// When both are the same a crash is not required for now, that would require extra checks...
	}

	// [ArTest.RunThisOnly]
	[ArTest.Test] public void TestSetDims()
	{
		var world = Lvl.World;
		var tls = new Tile[]
		{
			world.InstallEmptyTile(new(1, 2)),
			world.InstallEmptyTile(new(3, 3)),
			world.InstallEmptyTile(new(0, 7)),
			world.InstallEmptyTile(new(2, 0)),
		};

		// All tiles fit
		world.SetDims(new(10, 10));
		AssertEq(tls[0].GLocation, new(1, 2));
		AssertEq(tls[1].GLocation, new(3, 3));
		AssertEq(tls[2].GLocation, new(0, 7));
		AssertEq(tls[3].GLocation, new(2, 0));
		AssertRefEq(tls[0], world[1, 2]);
		AssertRefEq(tls[1], world[3, 3]);
		AssertRefEq(tls[2], world[0, 7]);
		AssertRefEq(tls[3], world[2, 0]);

		// Third tile gets its ass handed over to it
		world.SetDims(new(5, 5));
		AssertEq(tls[0].GLocation, new(1, 2));
		AssertEq(tls[1].GLocation, new(3, 3));
		AssertEq(tls[3].GLocation, new(2, 0));
		AssertFalse(world.IsInstalled(tls[2]));

		AssertRefEq(tls[0], world[1, 2]);
		AssertRefEq(tls[1], world[3, 3]);
		AssertRefEq(tls[3], world[2, 0]);

		// TODO: Now Add some units and decide what happens to cut-off units...
		// Do we return them?
		// Do we bomb them?
		// Do we shift them around when nothing is in the way? (oh god)
	}
}
