using Godot;
using System.Threading.Tasks;

namespace ArFactory.Tests;
using static Asserts;

[TestSuite]
public class CommandTests
{
	[Test] public void BeforeAll(Level lv)
	{
		lv.SetTickRate(50.0f);
	}

	[Test] public async Task TestWaitForTicks(Level lv)
	{
		lv.StartSimulation();
		{
			await lv.WaitForTicks(5);
			AssertEq(lv.GetTicksSinceStart(), 6); // First one executes instantly, that's why.
			await lv.WaitForTicks(5);
			AssertEq(lv.GetTicksSinceStart(), 11);
		}
		lv.EndSimulation();
	}

	[Test] public async Task TestCmdSpawn(Level lv)
	{
		var world = lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		world.PlaceInjector([
			new CmdSleep(1),
			new CmdSpawn(Vector2I.Zero, 5),
		]);
		
		lv.StartSimulation();
		{
			AssertFalse(tl.HasItem());
			
			await lv.WaitForTicks(1); // Spawns!
			AssertTrue(tl.HasItem());

			var it = tl.Item;
			AssertEq(it.GetValue(), 5);
			AssertTrue(it.IsAllowedToMove());
			AssertFalse(it.IsMidAnimation());
		}
		lv.EndSimulation();
	}

	[Test] public async Task Test_CmdSleep_and_CmdSpawn_and_CmdKill(Level lv)
	{
		// Behaviour for clear:
		// When it's not the first current pending command, execute all commands before it only,
		// then start counting ticks for the sleep, and only when it dies do the other commands see
		// the light.

		var world = lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var inj = world.PlaceInjector([
			new CmdSleep(3),
			new CmdSpawn(Vector2I.Zero, 3),
			new CmdSleep(1), // Just pushes the kill below to the next tick.
			new CmdKill(Vector2I.Zero),
		]);

		lv.StartSimulation();
		{
			// First cycle executes instantly, so we start waiting from the second and up.

			await lv.WaitForTicks(1);
			AssertEq(lv.GetTicksSinceStart(), 2);
			AssertFalse(tl.HasItem());
			
			await lv.WaitForTicks(1);
			AssertEq(lv.GetTicksSinceStart(), 3);
			AssertFalse(tl.HasItem());		
			// And now sleep is done.

			await lv.WaitForTicks(1);
			AssertEq(lv.GetTicksSinceStart(), 4);
			AssertTrue(tl.HasItem());
			// The second sleep should be done immediately this tick as well.

			await lv.WaitForTicks(1);
			AssertEq(lv.GetTicksSinceStart(), 5);
			AssertFalse(tl.HasItem()); // Item should be dead now
			AssertFalse(inj.HasPendingCmds());
		}
		lv.EndSimulation();
	}

	[Test] public async Task TestCmdSlide(Level lv)
	{
		var world = lv.World;
		world.InstallEmptyTile(new(0, 0));
		world.InstallEmptyTile(new(1, 0));
		
		world.InstallEmptyTile(new(2, 0));
		world.InstallEmptyTile(new(2, 1));
		
		world.InstallEmptyTile(new(3, 0));
		world.InstallEmptyTile(new(3, 1));
		
		world.InstallEmptyTile(new(4, 0));
		world.InstallEmptyTile(new(5, 0));
		
		var injy = world.PlaceInjector([
			new CmdSleep(1),
			new CmdSpawn(new(0, 0), 1),
			new CmdSlide(new(0, 0), Direction.East),
			new CmdSpawn(new(2, 0), 2),
			new CmdSlide(new(2, 0), Direction.South),
			new CmdSpawn(new(3, 1), 3),
			new CmdSlide(new(3, 1), Direction.North),
			new CmdSpawn(new(5, 0), 4),
			new CmdSlide(new(5, 0), Direction.West),
		]);

		lv.StartSimulation();
		{
			await lv.WaitForTicks(1);
			AssertTrue(injy.HasPendingCmds());
			AssertFalse(world.GetTile(new(0, 0)).HasItem());
			AssertTrue(world.GetTile(new(1, 0)).HasItem());
			AssertFalse(world.GetTile(new(2, 0)).HasItem());
			AssertTrue(world.GetTile(new(2, 1)).HasItem());

			var itEast = world.GetTile(new(1, 0)).Item;
			AssertEq(itEast.GetValue(), 1);
			AssertFalse(itEast.IsAllowedToMove());
			AssertTrue(itEast.IsMidAnimation());

			var itSouth = world.GetTile(new(2, 1)).Item;
			AssertEq(itSouth.GetValue(), 2);
			AssertFalse(itSouth.IsAllowedToMove());
			AssertTrue(itSouth.IsMidAnimation());

			var itNorth = world.GetTile(new(3, 0)).Item;
			AssertEq(itNorth.GetValue(), 3);
			AssertFalse(itNorth.IsAllowedToMove());
			AssertTrue(itNorth.IsMidAnimation());

			var itWest = world.GetTile(new(4, 0)).Item;
			AssertEq(itWest.GetValue(), 4);
			AssertFalse(itWest.IsAllowedToMove());
			AssertTrue(itWest.IsMidAnimation());

			await lv.WaitForTicks(1);
			AssertFalse(injy.HasPendingCmds());
			AssertFalse(world.GetTile(new(0, 0)).HasItem());
			AssertTrue(world.GetTile(new(1, 0)).HasItem());
			AssertFalse(world.GetTile(new(2, 0)).HasItem());
			AssertTrue(world.GetTile(new(2, 1)).HasItem());

			itEast = world.GetTile(new(1, 0)).Item;
			AssertEq(itEast.GetValue(), 1);
			AssertTrue(itEast.IsAllowedToMove());
			AssertFalse(itEast.IsMidAnimation());

			itSouth = world.GetTile(new(2, 1)).Item;
			AssertEq(itSouth.GetValue(), 2);
			AssertTrue(itSouth.IsAllowedToMove());
			AssertFalse(itSouth.IsMidAnimation());
			
			itNorth = world.GetTile(new(3, 0)).Item;
			AssertEq(itNorth.GetValue(), 3);
			AssertTrue(itNorth.IsAllowedToMove());
			AssertFalse(itNorth.IsMidAnimation());
			
			itWest = world.GetTile(new(4, 0)).Item;
			AssertEq(itWest.GetValue(), 4);
			AssertTrue(itWest.IsAllowedToMove());
			AssertFalse(itWest.IsMidAnimation());
		}
		lv.EndSimulation();
	}

	[Test] public async Task TestInstantCmds(Level lv)
	{
		var world = lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector([
			new CmdSleep(1),
			CmdSpawn.FromTiles(tl0, 1),
			CmdSpawn.FromTiles(tl1, 1),
			new CmdSleep(1), // Expected to stretch those instant commands to fill the tick.
			CmdKill.FromTiles(tl0),
			CmdKill.FromTiles(tl1),
			new CmdSleep(1),
			CmdSpawn.FromTiles(tl0, 1),
			CmdSpawn.FromTiles(tl1, 1),
			CmdKill.FromTiles(tl0),
			CmdKill.FromTiles(tl1),
		]);

		lv.StartSimulation();
		{
			AssertFalse(tl0.HasItem());
			AssertFalse(tl1.HasItem());

			await lv.WaitForTicks(1);
			AssertTrue(tl0.HasItem());
			AssertTrue(tl1.HasItem());

			await lv.WaitForTicks(1);
			AssertFalse(tl0.HasItem());
			AssertFalse(tl1.HasItem());
			
			await lv.WaitForTicks(1); // Should spawn and kill the items immediately
			AssertFalse(tl0.HasItem());
			AssertFalse(tl1.HasItem());

			AssertFalse(injy.HasPendingCmds());
		}
		lv.EndSimulation();
	}

	[Ignore]
	[Test] public async Task TestKillBeforeSlide(Level lv)
	{
		var world = lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector([
			new CmdSleep(1),
			CmdKill.FromTiles(tl1),
			CmdSlide.FromTiles(tl0, Direction.East),
		]);

		lv.StartSimulation();
		{
			await lv.WaitForTicks(1); // Kill should do its job here.
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
		}
		lv.EndSimulation();
	}
}