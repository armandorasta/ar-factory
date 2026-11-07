using System;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class WorldPanelTests : ArTest.TestSuit
{
	public Editor Lvl => m_Lv!;
	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.EditorScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(m_Lv);
		Lvl.Simy.Clock.SetTickRate(300);
	}

	public override void BeforeEach()
	{
		Lvl.Simy.Reset();
		Lvl.World.Clear();
	}

	public override void AfterEach()
	{
		AssertTrue(Lvl.Simy.IsHalted());
	}

	public override void AfterAll()
	{
		RemoveNode(Lvl);
		Lvl.QueueFree();
	}

	[ArTest.Test] public void TestInstallTile_vanilla()
	{
		var world = Lvl.World;
		AssertFalse(world.HasTileOn(Vector2I.One));
		AssertNull(world[1, 1]);

		var tl = world.AddEmptyTile(Vector2I.Zero);
		tl = world.ExtractTile(tl.GLocation)!;
		var reTl = world.InstallTile(tl, Vector2I.One);
		AssertRefEq(tl, reTl);
		AssertRefEq(world[1, 1], tl);
		AssertEq(tl.GLocation, Vector2I.One);
		AssertTrue(world.HasTileOn(Vector2I.One));
	}

	[ArTest.Test] public void TestInstallTile_INVALID_CASE_out_of_bounds()
	{
		var world = Lvl.World;
		// Installing out of bounds will crash the program.
		AssertThrow(() => world.AddEmptyTile(Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestInstallTile_INVALID_CASE_overriding_without_passing_flag()
	{
		var world = Lvl.World;
		world.AddEmptyTile(Vector2I.One);
		AssertThrow(() => world.AddEmptyTile(Vector2I.One));
	}

	[ArTest.Test] public void TestDeleteTile_vanilla()
	{
		// Items will not be tested here, as tile tests don't depend on item tests.

		var world = Lvl.World;
		world.AddEmptyTile(Vector2I.One);
		world.DeleteTile(Vector2I.One);
		AssertFalse(world.HasTileOn(Vector2I.One));
		AssertNull(world.GetTile(Vector2I.One));
	}
	
	[ArTest.Test] public void TestDeleteTile_deleting_nothing_with_flag()
	{
		var world = Lvl.World;
		world.DeleteTile(Vector2I.One, true); // Should just do nothing.
	}

	[ArTest.Test] public void TestDeleteTile_INVALID_CASE_deleting_nothing_without_flag()
	{
		var world = Lvl.World;
		AssertThrow(() => world.DeleteTile(Vector2I.One));
	}

	[ArTest.Test] public void TestDeleteTile_INVALID_CASE_deleting_out_of_bounds()
	{
		var world = Lvl.World;
		AssertThrow(() => world.DeleteTile(Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestMoveTile_vanilla()
	{
		var world = Lvl.World;
		var tl = world.AddEmptyTile(Vector2I.Zero);
		var movedTl = world.MoveTile(tl, Vector2I.One);
		AssertRefEq(tl, movedTl);
		AssertFalse(world.HasTileOn(Vector2I.Zero));
		AssertTrue(world.HasTileOn(Vector2I.One));
		AssertEq(tl.GLocation, Vector2I.One);
	}

	[ArTest.Test] public void TestMoveTile_INVALID_CASE_from_to_out_of_bounds()
	{
		var world = Lvl.World;
		AssertThrow(() => world.MoveTile(Vector2I.One * 1000, Vector2I.One * 1001));
	}

	[ArTest.Test] public void TestExtractTile_vanilla()
	{
		var world = Lvl.World;
		var installedTl = world.AddEmptyTile(new Vector2I(3, 4));
		var extractedTl = world.ExtractTile(installedTl.GLocation);
		AssertRefEq(installedTl, extractedTl);
		AssertFalse(world.HasTileOn(installedTl.GLocation));
	}

	[ArTest.Test] public void TestExtractTile_floating_tile_gloc()
	{
		var world = Lvl.World;
		var installedTl = world.AddEmptyTile(new Vector2I(3, 4));
		var extractedTl = world.ExtractTile(installedTl.GLocation)!;

		extractedTl!._SetGLUnsafe(Vector2I.Zero); // Should be ignored and overriden.
		var anotherTl = world.InstallTile(extractedTl, new Vector2I(1, 2));
		AssertTrue(world.HasTileOn(new Vector2I(1, 2)));
		AssertFalse(world.HasTileOn(new Vector2I(3, 4))); // Ignore the tile's grid-loc
		AssertRefEq(anotherTl, extractedTl);
		world.DeleteTile(anotherTl);
	}

	[ArTest.Test] public void TestExtractTile_extracting_null_tile_with_flag()
	{
		var world = Lvl.World;
		AssertNull(world.ExtractTile(Vector2I.Zero, true));
		AssertNull(world.ExtractTile(Vector2I.Zero, true));

		// Extracting out of bounds will crash the program.
		AssertThrow(() => world.ExtractTile(Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestExtractTile_INVALID_CASE_extracting_null_tile_without_flag()
	{
		var world = Lvl.World;
		AssertThrow(() => world.ExtractTile(Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestIsInstalled()
	{
		var world = Lvl.World;
		var tl = world.AddEmptyTile(new Vector2I(3, 1));
		AssertTrue(world.IsInstalled(tl));

		var exTl = world.ExtractTile(tl.GLocation);
		AssertFalse(world.IsInstalled(tl));
	}

	[ArTest.Test] public void TestSpawnItem_vanilla()
	{
		var world = Lvl.World;
		var tl = world.AddEmptyTile(Vector2I.Zero);
		AssertFalse(tl.HasItem());

		var it = world.SpawnItem(tl, 7)!;
		AssertNotNull(it);
		AssertRefEq(it, tl.Item);
		AssertTrue(tl.HasItem());
		AssertEq(tl.Item.Value, 7);
	}

	[ArTest.Test] public void TestSpawnItem_Deleting_a_tile_with_an_item_in_it()
	{
		var world = Lvl.World;
		var tl = world.AddEmptyTile(Vector2I.Zero);
		var it = world.SpawnItem(tl, 7)!;
		world.DeleteTile(tl);
		AssertTrue(it.IsQueuedForDeletion());
	}
	
	[ArTest.Test] public void TestSpawnItem_INVALID_CASE_on_top_of_another_item()
	{
		var world = Lvl.World;
		var tl = world.AddEmptyTile(Vector2I.Zero);
		var it = world.SpawnItem(tl, 7)!;
		AssertThrow(() => world.SpawnItem(tl, 8));
	}

	[ArTest.Test] public void TestSpawnItem_INVALID_CASE_out_of_bounds()
	{
		var world = Lvl.World;
		AssertThrow(() => world.SpawnItem(Vector2I.One * 1000, -174));
	}

	[ArTest.Test] public void TestInstallItem_vanilla()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(Vector2I.Zero);
		var tl1 = world.AddEmptyTile(Vector2I.One);
		world.SpawnItem(tl0, 7);
		var extractedIt = tl0.ExtractItem()!;

		var installedIt = world.InstallItem(extractedIt, tl1);
		AssertRefEq(installedIt, extractedIt);
		AssertRefEq(tl1.Item, installedIt);
		AssertEq(installedIt!.Value, 7);
		AssertFalse(tl0.HasItem());
	}

	[ArTest.Test] public void TestInstallItem_INVALID_CASE_non_existant_tile()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(Vector2I.Zero);
		var tl1 = world.AddEmptyTile(Vector2I.One);

		world.SpawnItem(tl1, 901);
		var it1 = AddNode(tl1.ExtractItem()!);
		AssertThrow(() => world.InstallItem(it1, new Vector2I(2, 3)));
	}

	[ArTest.Test] public void TestInstallItem_INVALID_CASE_out_of_bounds()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(Vector2I.Zero);
		var tl1 = world.AddEmptyTile(Vector2I.One);
		
		world.SpawnItem(tl1, 577);
		var it1 = AddNode(tl1.ExtractItem()!);
		AssertThrow(() => world.InstallItem(it1, Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestCloneItem_vanilla()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 5)!;
		var cloneIt = world.CloneItem(tl0, tl1);
		AssertTrue(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertRefEq(it0, tl0.Item);
		AssertRefNotEq(it0, cloneIt);
		AssertRefNotEq(tl1.Item, tl0.Item); // Must not be the same for obvious reasons...
		AssertEq(tl1.Item.Value, tl0.Item.Value);
	}

	[ArTest.Test] public void TestCloneItem_overriding_with_flag()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 5)!;
		var cloneIt = world.CloneItem(tl0, tl1)!;
		tl0.Item.SetValue(6); // Both items had the same value...
		AssertNotEq(tl0.Item.Value, tl1.Item.Value);
		cloneIt = world.CloneItem(tl0, tl1, true);
		AssertTrue(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertRefEq(cloneIt, tl1.Item);
		AssertRefNotEq(tl0.Item, tl1.Item);
		AssertEq(tl1.Item.Value, tl0.Item.Value);
		AssertEq(tl1.Item.Value, 6);
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_cloning_null()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		AssertThrow(() => world.CloneItem(tl0, tl1));
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_cloning_null_into_item()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		world.SpawnItem(tl1, 235);
		AssertThrow(() => world.CloneItem(tl0, tl1));
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_from_non_existant_tile()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 5)!;

		AssertThrow(() => world.CloneItem(Vector2I.Zero, tl1.GLocation));
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_to_non_existant_tile()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		AssertThrow(() => world.CloneItem(tl0.GLocation, Vector2I.Zero));
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_from_out_of_bounds()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		AssertThrow(() => world.CloneItem(Vector2I.One * 1000, tl0.GLocation));
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_to_out_of_bounds()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var it0 = world.SpawnItem(tl0, 5)!;
		AssertThrow(() => world.CloneItem(it0.GLocation, Vector2I.One * 1000));
	}

	[ArTest.Test] public void TestCloneItem_INVALID_CASE_into_self()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var it0 = world.SpawnItem(tl0, 5)!;
		AssertThrow(() => world.CloneItem(tl0, tl0));
	}

	[ArTest.Test] public void TestTelportItem_vanilla()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 5)!;
		var teleIt = world.TeleportItem(tl0, tl1);
		AssertFalse(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertRefEq(it0, tl1.Item);
		AssertRefEq(it0, teleIt);
	}

	[ArTest.Test] public void TestTeleportItem_overriding_with_flag()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0, 5)!;
		world.SpawnItem(tl1, 2);
		var teleIt = world.TeleportItem(tl0, tl1, true);
		AssertFalse(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertRefEq(it0, tl1.Item);
		AssertRefEq(it0, teleIt);
	}

	[ArTest.Test] public void TestTeleportItem_into_self()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var it0 = world.SpawnItem(tl0, 5)!;
		world.TeleportItem(tl0, tl0);
		AssertTrue(tl0.HasItem());
		AssertRefEq(it0, tl0.Item);
	}

	[ArTest.Test] public void TestTeleportItem_INVALID_CASE_teleporting_null()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		AssertThrow(() => world.TeleportItem(tl0, tl1));
	}

	[ArTest.Test] public void TestTeleportItem_INVALID_CASE_null_into_item()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var tl1 = world.AddEmptyTile(new(3, 4));
		world.SpawnItem(tl1, 235);
		AssertThrow(() => world.TeleportItem(tl0, tl1));
	}

	[ArTest.Test] public void TestTeleportItem_INVALID_CASE_from_non_existant_tile()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		AssertThrow(() => world.TeleportItem(Vector2I.Zero, tl0.GLocation));
	}

	[ArTest.Test] public void TestTeleportItem_INVALID_CASE_to_non_existant_tile()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var it0 = world.SpawnItem(tl0, 5)!;
		AssertThrow(() => world.CloneItem(tl0.GLocation, Vector2I.Zero));
	}

	[ArTest.Test] public void TestTeleportItem_INVALID_CASE_from_out_of_bounds()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		AssertThrow(() => world.CloneItem(Vector2I.One * 1000, tl0.GLocation));
	}

	[ArTest.Test] public void TestTeleportItem_INVALID_CASE_to_out_of_bounds()
	{
		var world = Lvl.World;
		var tl0 = world.AddEmptyTile(new(1, 2));
		var it0 = world.SpawnItem(tl0, 5)!;
		AssertThrow(() => world.CloneItem(it0.GLocation, Vector2I.One * 1000));
	}

	// [ArTest.RunThisOnly]
	[ArTest.Test] public void TestSetDims()
	{
		var world = Lvl.World;
		var tls = new Tile[]
		{
			world.AddEmptyTile(new(1, 2)),
			world.AddEmptyTile(new(3, 3)),
			world.AddEmptyTile(new(0, 7)),
			world.AddEmptyTile(new(2, 0)),
		};

		// All tiles fit
		world.SetDimensions(new(10, 10));
		AssertEq(tls[0].GLocation, new(1, 2));
		AssertEq(tls[1].GLocation, new(3, 3));
		AssertEq(tls[2].GLocation, new(0, 7));
		AssertEq(tls[3].GLocation, new(2, 0));
		AssertRefEq(tls[0], world[1, 2]);
		AssertRefEq(tls[1], world[3, 3]);
		AssertRefEq(tls[2], world[0, 7]);
		AssertRefEq(tls[3], world[2, 0]);

		// Third tile gets its ass handed over to it
		world.SetDimensions(new(5, 5));
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
