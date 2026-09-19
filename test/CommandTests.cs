using Godot;
using System.Threading.Tasks;

namespace ArFactory.Tests;
using static Asserts;

public static class CommandTests
{
	public static float s_TickRate = 50.0f;

	public static async Task TestWaitForTicks(Level lv)
	{
		lv.SetTickRate(s_TickRate);
		lv.StartSimulation();
		{
			await lv.WaitForTicks(5);
			AssertEq(lv.GetTicksSinceStart(), 6); // First one executes instantly, that's why.
			await lv.WaitForTicks(5);
			AssertEq(lv.GetTicksSinceStart(), 11);
		}
		lv.EndSimulation();
	}

	public static async Task TestCmdSpawn(Level lv)
	{
		var world = lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		world.PlaceInjector([
			new CmdSleep(1),
			new CmdSpawn(Vector2I.Zero, 5),
		]);
		
		lv.SetTickRate(s_TickRate);
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

	public static async Task TestCmdSleep(Level lv)
	{
		// Behaviour:
		// When it's not the first current pending command, execute all commands before it only,
		// then start counting ticks for the sleep, and only when it dies do the other commands see
		// the light.

		var world = lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var inj = world.PlaceInjector([
			new CmdSleep(3),
			new CmdSpawn(Vector2I.Zero, 3),
			new CmdSleep(1),
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

			await lv.WaitForTicks(1);
			AssertEq(lv.GetTicksSinceStart(), 5);
			AssertTrue(tl.HasItem());
			// The second sleep should be done.

			await lv.WaitForTicks(1);
			AssertEq(lv.GetTicksSinceStart(), 6);
			AssertFalse(tl.HasItem()); // Item should be dead now
			AssertFalse(inj.HasPendingCmds());
		}
		lv.EndSimulation();
	}

	public static async Task TestCmdSlide(Level lv)
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

		lv.SetTickRate(s_TickRate);
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
}