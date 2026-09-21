using Godot;

namespace ArFactory.Tests;
using static Asserts;

[TestSuite] public class WorldPanelTests
{
	[Test] public void TestSetDims(Level lv)
	{
		var world = lv.World;
		world.SetDims(new(10, 10));
		AssertEq(world.Dims, new(10, 10));
		AssertTrue(world.IsWithin(new(5, 2)));
		AssertFalse(world.IsWithin(new(20, 55)));
		AssertFalse(world.HasTile(new(1, 6)));
		AssertFalse(world.HasTile(new(23, 6)));
		// TODO: place some tiles and see if there are indeed in the same location!
		// TODO: place some units and determine what happens to cut-off units.
	}

	[Test] public void TestCloneItem(Level lv)
	{
		var world = lv.World;
		var tl0 = world.InstallEmptyTile(new(1, 2));
		var tl1 = world.InstallEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0.GridLoc, 5);
		var cloneIt = world.CloneItem(tl0.GridLoc, tl1.GridLoc);
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
		cloneIt = world.CloneItem(tl0.GridLoc, tl1.GridLoc);
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
		cloneIt = world.CloneItem(tl0.GridLoc, tl1.GridLoc);
		AssertNull(cloneIt);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());
		AssertEq(tl1.Item.Value, 6);

		// Cloning null into null, should just do nothing.
		tl1.DestroyItem();
		cloneIt = world.CloneItem(tl0.GridLoc, tl1.GridLoc);
		AssertNull(cloneIt);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertFalse(tl1.HasItem());
		AssertFalse(tl1.IsReserved());
	}

	[Test] public void TestTeleportItem(Level lv)
	{
		var world = lv.World;
		var tl0 = world.InstallEmptyTile(new(1, 2));
		var tl1 = world.InstallEmptyTile(new(3, 4));
		var it0 = world.SpawnItem(tl0.GridLoc, 50);
		var teleIt = world.TeleportItem(tl0.GridLoc, tl1.GridLoc);
		AssertRefEq(it0, teleIt);
		AssertRefEq(it0, tl1.Item);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());
		AssertEq(tl1.Item.Value, 50);

		// Both have items, should just override the destination (requires bOverride in debug).
		it0 = world.SpawnItem(tl0.GridLoc, 85);
		teleIt = world.TeleportItem(tl0.GridLoc, tl1.GridLoc);
		AssertRefEq(it0, teleIt);
		AssertRefEq(it0, tl1.Item);
		AssertEq(tl1.Item.Value, 85);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());

		// Teleport null into a tile with an item, should just do nothing.
		teleIt = world.TeleportItem(tl0.GridLoc, tl1.GridLoc);
		AssertNull(teleIt);
		AssertEq(tl1.Item.Value, 85);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.IsReserved());

		// Cloning null into null, should just do nothing.
		tl1.DestroyItem();
		teleIt = world.TeleportItem(tl0.GridLoc, tl1.GridLoc);
		AssertNull(teleIt);
		AssertFalse(tl0.HasItem());
		AssertFalse(tl0.IsReserved());
		AssertFalse(tl0.HasItem());
		AssertFalse(tl1.IsReserved());
	}
}
