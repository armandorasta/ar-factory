using Godot;
using System.Threading.Tasks;

namespace ArFactory.Tests;
using static ArTest.Asserts;

[ArTest.RunThisOnly]
public class CommandTests : ArTest.TestSuit
{
	private Level m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		m_Lv = levelScene.Instantiate<Level>();
		AddNode(m_Lv);
		m_Lv.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		m_Lv.World.Reset();
		m_Lv.World.SetDims(new(6, 6));
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

	[ArTest.Test] public async Task TestCmdSpawn_Vanilla()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy0 = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			AssertFalse(tl0.HasItem());
			injy0.PendParallel([ new CmdSpawn(tl0, 41) ]);
			await m_Lv.TickOnceAsync();
			AssertTrue(tl0.HasItem());
			AssertEq(tl0.Item.Value, 41);
			AssertFalse(injy0.HasPendingCmds());
			
			await m_Lv.EndTickAsync();
			AssertTrue(tl0.HasItem());
			AssertEq(tl0.Item.Value, 41);
			AssertFalse(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdSpawn_INVALID_CASES()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy0 = world.PlaceInjector();

		// Spawn on non-existing tile
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdSpawn(new Vector2I(2, 3), -552)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Double spawn.
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -380);
			injy0.PendParallel([new CmdSpawn(tl0, 150)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Out of bounds spawn
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdSpawn(new Vector2I(-1, -1), -428)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdKill_Vanilla()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy0 = world.PlaceInjector();
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdKill(tl0)]);
			world.SpawnItem(tl0, 497);
			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertFalse(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdKill_INVALID_CASES()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy0 = world.PlaceInjector();
	
		// Kill on non-existing tile
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdKill(new Vector2I(2, 3))]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Killing nothing (existing tile tho).
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdKill(new Vector2I(0, 0))]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Out of bounds kill
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdKill(new Vector2I(-1, -1))]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Killing mid-animation
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 112);
			injy0.PendParallel([
				new CmdSlide(tl0, Direction.East),
				new CmdKill(new Vector2I(-1, -1))
			]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdSleep()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([new CmdSleep(3)], 0);
			injy.PendParallel([new CmdSpawn(tl, 3), new CmdSleep(1)], 1);
			injy.PendParallel([new CmdKill(tl)], 2);

			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 1);
			AssertFalse(tl.HasItem());

			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 2);
			AssertFalse(tl.HasItem());

			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 3);
			AssertFalse(tl.HasItem());
			// And now sleep is done.

			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 4);
			AssertTrue(tl.HasItem());
			// The second sleep should be done immediately this tick as well.

			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 5);
			AssertFalse(tl.HasItem()); // Item should be dead now
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdSlide_Vanilla()
	{
		var world = m_Lv.World;
		var tlEast0 = world.InstallEmptyTile(new(0, 0));
		var tlEast1 = world.InstallEmptyTile(new(1, 0));

		var tlSouth0 = world.InstallEmptyTile(new(2, 0));
		var tlSouth1 = world.InstallEmptyTile(new(2, 1));

		var tlNorth0 = world.InstallEmptyTile(new(3, 1));
		var tlNorth1 = world.InstallEmptyTile(new(3, 0));

		var tlWest0 = world.InstallEmptyTile(new(5, 0));
		var tlWest1 = world.InstallEmptyTile(new(4, 0));

		var injy0 = world.PlaceInjector();

		using var _ = m_Lv.PlaySimulationInDebug();
		injy0.PendParallel([
			new CmdSpawn(tlEast0, 1),
			new CmdSpawn(tlSouth0, 2),
			new CmdSpawn(tlNorth0, 3),
			new CmdSpawn(tlWest0, 4),
		]);
	
		await m_Lv.TickOnceAsync();
		AssertTrue(tlEast0.HasItem());
		AssertTrue(tlSouth0.HasItem());
		AssertTrue(tlNorth0.HasItem());
		AssertTrue(tlWest0.HasItem());
		AssertFalse(injy0.HasPendingCmds());

		await m_Lv.EndTickAsync();
		injy0.PendParallel([
			new CmdSlide(tlEast0, Direction.East),
			new CmdSlide(tlSouth0, Direction.South),
			new CmdSlide(tlNorth0, Direction.North),
			new CmdSlide(tlWest0, Direction.West),
		]);

		var itEast = tlEast0.Item;
		var itSouth = tlSouth0.Item;
		var itNorth = tlNorth0.Item;
		var itWest = tlWest0.Item;

		await m_Lv.TickOnceAsync();
		AssertFalse(tlEast0.HasItem());
		AssertTrue(tlEast1.HasItem());
		AssertTrue(tlEast1.Item.IsMidAnimation());

		AssertFalse(tlSouth0.HasItem());
		AssertTrue(tlSouth1.HasItem());
		AssertTrue(tlSouth1.Item.IsMidAnimation());

		AssertFalse(tlNorth0.HasItem());
		AssertTrue(tlNorth1.HasItem());
		AssertTrue(tlNorth1.Item.IsMidAnimation());

		AssertFalse(tlWest0.HasItem());
		AssertTrue(tlWest1.HasItem());
		AssertTrue(tlWest1.Item.IsMidAnimation());

		AssertRefEq(itEast, tlEast1.Item);
		AssertRefEq(itSouth, tlSouth1.Item);
		AssertRefEq(itNorth, tlNorth1.Item);
		AssertRefEq(itWest, tlWest1.Item);

		await m_Lv.EndTickAsync();
		AssertFalse(tlEast0.HasItem());
		AssertTrue(tlEast1.HasItem());
		AssertFalse(tlEast1.Item.IsMidAnimation());

		AssertFalse(tlSouth0.HasItem());
		AssertTrue(tlSouth1.HasItem());
		AssertFalse(tlSouth1.Item.IsMidAnimation());

		AssertFalse(tlNorth0.HasItem());
		AssertTrue(tlNorth1.HasItem());
		AssertFalse(tlNorth1.Item.IsMidAnimation());

		AssertFalse(tlWest0.HasItem());
		AssertTrue(tlWest1.HasItem());
		AssertFalse(tlWest1.Item.IsMidAnimation());
		AssertFalse(injy0.HasPendingCmds());
	}

	[ArTest.Test] public async Task TestCmdSlide_Destination_item_is_still_coming()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(new(0, 0));
		var tl1 = world.InstallEmptyTile(new(1, 0));

		var injy0 = world.PlaceInjector();
		var injy1 = world.PlaceInjector();

		// For now on I will only test east and assume the rest will work similarly because
		// otherwise the code size will explode for a very stupid reason...
		
		using var _ = m_Lv.PlaySimulationInDebug();

		// Across units
		injy0.PendParallel([ // The kill happens external to the unit
			new CmdSpawn(tl0, 1),
			new CmdSpawn(tl1, 2),
			new CmdSlide(tl0, Direction.East),
		]);

		await m_Lv.TickOnceAsync();
		AssertTrue(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertEq(tl0.Item.Value, 1);
		AssertEq(tl1.Item.Value, 2);

		await m_Lv.EndTickAsync();
		injy1.PendParallel([new CmdKill(tl1)]);

		// injy1 goes for the kill, but the overlap algo kicks in.
		await m_Lv.TickOnceAsync();
		AssertFalse(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.Item.IsMidAnimation());
		AssertEq(tl1.Item.Value, 1);

		await m_Lv.EndTickAsync();
		AssertFalse(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertFalse(tl1.Item.IsMidAnimation());

		AssertFalse(injy0.HasPendingCmds());
		AssertFalse(injy1.HasPendingCmds());

		tl1.DestroyItem();

		// The kill is internal to the unit, shouldnt matter since units are just a hoax xd
		injy0.PendParallel([
			new CmdSpawn(tl0, 1),
			new CmdSpawn(tl1, 2),
			new CmdSlide(tl0, Direction.East),
			new CmdKill(tl1), // Here the kill happens, then the above slide overlaps.
		]);

		// Internal
		await m_Lv.TickOnceAsync();
		AssertFalse(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertTrue(tl1.Item.IsMidAnimation());
		AssertEq(tl1.Item.Value, 1);

		await m_Lv.EndTickAsync();
		AssertFalse(tl0.HasItem());
		AssertTrue(tl1.HasItem());
		AssertFalse(tl1.Item.IsMidAnimation());
		AssertFalse(injy0.HasPendingCmds());

		tl1.DestroyItem();
	}

	[ArTest.Test] public async Task TestCmdSlide_INVALID_CASES()
	{
		var world = m_Lv.World;
		var injy0 = world.PlaceInjector();
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));

		// Sliding from non-existing tile
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdSlide(new Vector2I(0, 1), Direction.North)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Sliding from to a non-existant tile
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -294);
			injy0.PendParallel([new CmdSlide(tl0, Direction.South)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Sliding against the border of the world
		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([new CmdSlide(tl0, Direction.North)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Sliding nothing on existing tiles
		using (m_Lv.PlaySimulationInDebug())
		{
			tl0.DestroyItem();
			injy0.PendParallel([new CmdSlide(tl0, Direction.East)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Sliding from out of bounds
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl1, 949);
			injy0.PendParallel([new CmdSlide(new Vector2I(0, -1), Direction.South)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}

		// Sliding to out of bounds is just the same as the against world boundary case.

		// Sliding from and to out of bounds
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl1, 949);
			injy0.PendParallel([new CmdSlide(new Vector2I(0, -1), Direction.North)]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdAwait_After_spawn()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy0 = world.PlaceInjector();
		
		using (m_Lv.PlaySimulationInDebug())
		{
			var wait0 = new CmdAwait([tl0]);
			injy0.PendParallel([
				new CmdSpawn(tl0, 7),
				wait0, // Here it does nothing.
			]);

			await m_Lv.TickOnceAsync();
			AssertTrue(tl0.HasItem());
			AssertTrue(wait0.IsDone());
		}
	}
	
	[ArTest.Test] public async Task TestCmdAwait_after_slide_on_dest()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy0 = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{ 
			world.SpawnItem(tl0, 447);
			var wait1 = new CmdAwait(tl1);
			injy0.PendParallel([
				new CmdSlide(tl0, Direction.East),
				wait1, // Also does nothing here...
			]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertFalse(wait1.IsDone());
			await m_Lv.EndTickAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation()); // Animation ends.
			AssertFalse(wait1.IsDone()); // but wait only ends next tick.
		}
	}

	[ArTest.Test] public async Task TestCmdAwait_cross_unit()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(new(1, 0));
		var injy0 = world.PlaceInjector();
		var injy1 = world.PlaceInjector();
		
		using (m_Lv.PlaySimulationInDebug())
		{
			var wait = new CmdAwait(tl);
			injy0.PendParallel([wait]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl.HasItem());
			AssertFalse(wait.IsDone());
			AssertTrue(injy0.HasPendingCmds());
			await m_Lv.EndTickAsync();
			AssertFalse(tl.HasItem());
			AssertFalse(wait.IsDone());
			AssertTrue(injy0.HasPendingCmds());
			
			injy1.PendParallel([
				new CmdSpawn(tl, 9), // Second injector is waiting for this.
			]);

			await m_Lv.TickOnceAsync();
			AssertTrue(tl.HasItem());
			AssertFalse(wait.IsDone()); // Spawn happens after await checks
			AssertTrue(injy0.HasPendingCmds());
			AssertFalse(injy1.HasPendingCmds());

			await m_Lv.TickOnceAsync();
			AssertTrue(tl.HasItem());
			AssertTrue(wait.IsDone());
			AssertFalse(injy0.HasPendingCmds());
			AssertFalse(injy1.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdVacate_Lone_vacate()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var vac = new CmdVacate([tl]);
		var injy0 = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([ vac ]);
			await m_Lv.TickOnceAsync();
			AssertFalse(tl.HasItem());
			AssertTrue(vac.IsDone());
			AssertFalse(injy0.HasPendingCmds());
			await m_Lv.EndTickAsync();
			AssertFalse(tl.HasItem());
			AssertTrue(vac.IsDone());
			AssertFalse(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdVacate_On_slide_dest()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var vac = new CmdVacate([tl0]);
		var injy0 = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy0.PendParallel([
			new CmdSpawn(tl0, 7),
				new CmdSlide(tl0, Direction.East),
				vac, // Should be done immediately because they don't wait for items to finish sliding.
			]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertTrue(vac.IsDone());
			await m_Lv.EndTickAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			AssertTrue(vac.IsDone());
			AssertFalse(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdVacate_Multiple_tiles()
	{
		var world = m_Lv.World;
		var tl00 = world.InstallEmptyTile(Vector2I.Zero);
		var tl10 = world.InstallEmptyTile(new(1, 0));
		var tl01 = world.InstallEmptyTile(new(0, 1));
		var tl11 = world.InstallEmptyTile(new(1, 1));
		
		var vac = new CmdVacate([tl10, tl11]);
		world.SpawnItem(tl10, 731);
		world.SpawnItem(tl11, 125);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdSlide(tl10, Direction.West), // (1, 0) => (0, 0)
				new CmdSlide(tl11, Direction.West), // (1, 1) => (0, 1)
				vac,
			]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl10.HasItem());
			AssertFalse(tl11.HasItem());
			AssertTrue(tl00.HasItem());
			AssertTrue(tl01.HasItem());
			AssertTrue(tl00.Item.IsMidAnimation());
			AssertTrue(tl01.Item.IsMidAnimation());
			AssertTrue(vac.IsDone());
			await m_Lv.EndTickAsync();
			AssertFalse(tl10.HasItem());
			AssertFalse(tl11.HasItem());
			AssertTrue(tl00.HasItem());
			AssertTrue(tl01.HasItem());
			AssertFalse(tl00.Item.IsMidAnimation());
			AssertFalse(tl01.Item.IsMidAnimation());

			await m_Lv.TickOnceAsync();
			AssertTrue(vac.IsDone());
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public void Test_CmdUpdate_Apply()
	{
		AssertEq(UnaryOperation.Add.Apply(-29, -715), -29 + -715);
		// This should work because apply doesn't concern itself with item value limits.
		AssertEq(UnaryOperation.Mul.Apply(-355, -617), -355 * -617);
		AssertEq(UnaryOperation.Override.Apply(348, -976), -976);
	}

	[ArTest.Test] public async Task TestCmdUpdate_Vanilla()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl, 274);
			injy.PendParallel([ new CmdUpdate(tl, UnaryOperation.Mul, 2), new CmdSleep(1) ]);
			injy.PendParallel([ new CmdUpdate(tl, UnaryOperation.Add, 5) ], 1);

			await m_Lv.TickOnceAsync();
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 2*274);

			await m_Lv.TickOnceAsync();
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 2*274 + 5);
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdUpdate_INVALID_CASES()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector();

		// Updating nothing (existing tile)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([(new CmdUpdate(tl0, UnaryOperation.Override, 559))]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Updating nothing (no tile)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdUpdate(new Vector2I(2, 3), UnaryOperation.Override, 12) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Updating nothing (out of bounds)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdUpdate(new Vector2I(-1, 2), UnaryOperation.Override, -320) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Updating something mid-animation
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 376);
			injy.PendParallel([
				new CmdSlide(tl0, Direction.East),
				new CmdUpdate(tl1, UnaryOperation.Override, -552)
			]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdClone_Vanilla()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var tl2 = world.InstallEmptyTile(new(1, 1));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 940);
			injy.PendParallel([ new CmdClone(tl0, [tl1, tl2]) ]);
			await m_Lv.TickOnceAsync();
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, tl0.Item.Value);
			AssertTrue(tl2.HasItem());
			AssertEq(tl2.Item.Value, tl0.Item.Value);
			AssertRefNotEq(tl1.Item, tl0.Item);
			AssertRefNotEq(tl1.Item, tl2.Item);
			AssertRefNotEq(tl2.Item, tl0.Item);
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdClone_INVALID_CASES()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var tl2 = world.InstallEmptyTile(new(1, 1));
		var injy = world.PlaceInjector();

		// No item to clone (existing tile)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdClone(tl0, [tl1, tl2]) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// No item to clone (non-existing tile)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdClone(new Vector2I(2, 3), [tl1.GridLoc, tl2.GridLoc]) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Cloning to non-existing tile
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -73);
			injy.PendParallel([ new CmdClone(tl0.GridLoc, [new Vector2I(2, 3), tl2.GridLoc]) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Cloning to already occupied tile
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -73);
			world.SpawnItem(tl1, 222);
			injy.PendParallel([ new CmdClone(tl0, [tl1, tl2]) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Cloning an item mid-animation (should I allow this?)
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -73);
			injy.PendParallel([
				new CmdSlide(tl0, Direction.East),
				new CmdClone(tl1, [tl0, tl2])
			]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public void Test_BinaryOperation()
	{
		AssertEq(BinaryOperation.Add.Apply(-323, -413), -323 + -413);
		AssertEq(BinaryOperation.Sub.Apply(-982, 316), -982 - 316);
		AssertEq(BinaryOperation.Mul.Apply(-323, -413), -323 * -413);
		AssertEq(BinaryOperation.Div.Apply(-323, -413), -323 / -413);
		
		AssertEq(BinaryOperation.And.Apply(-323, -413), 1);
		AssertEq(BinaryOperation.And.Apply(-323, 0), 0);
		AssertEq(BinaryOperation.And.Apply(0, -413), 0);
		AssertEq(BinaryOperation.And.Apply(0, 0), 0);

		AssertEq(BinaryOperation.Or.Apply(-323, -413), 1);
		AssertEq(BinaryOperation.Or.Apply(-323, 0), 1);
		AssertEq(BinaryOperation.Or.Apply(0, -413), 1);
		AssertEq(BinaryOperation.Or.Apply(0, 0), 0);

		AssertEq(BinaryOperation.Xor.Apply(-323, -413), 0);
		AssertEq(BinaryOperation.Xor.Apply(-323, 0), 1);
		AssertEq(BinaryOperation.Xor.Apply(0, -413), 1);
		AssertEq(BinaryOperation.Xor.Apply(0, 0), 0);
		
		AssertEq(BinaryOperation.BitAnd.Apply(-323, -413), -323 & -413);
		AssertEq(BinaryOperation.BitOr.Apply(-323, -413), -323 | -413);
		AssertEq(BinaryOperation.BitXor.Apply(-323, -413), -323 ^ -413);

		// Left > right
		AssertEq(BinaryOperation.Eq.Apply(709, 19), 0);
		AssertEq(BinaryOperation.NotEq.Apply(709, 19), 1);
		AssertEq(BinaryOperation.Greater.Apply(709, 19), 1);
		AssertEq(BinaryOperation.GreaterEq.Apply(709, 19), 1);
		AssertEq(BinaryOperation.Less.Apply(709, 19), 0);
		AssertEq(BinaryOperation.LessEq.Apply(709, 19), 0);

		// Left < right
		AssertEq(BinaryOperation.Eq.Apply(19, 709), 0);
		AssertEq(BinaryOperation.NotEq.Apply(19, 709), 1);
		AssertEq(BinaryOperation.Greater.Apply(19, 709), 0);
		AssertEq(BinaryOperation.GreaterEq.Apply(19, 709), 0);
		AssertEq(BinaryOperation.Less.Apply(19, 709), 1);
		AssertEq(BinaryOperation.LessEq.Apply(19, 709), 1);

		// Left == right
		AssertEq(BinaryOperation.Eq.Apply(273, 273), 1);
		AssertEq(BinaryOperation.NotEq.Apply(273, 273), 0);
		AssertEq(BinaryOperation.Greater.Apply(273, 273), 0);
		AssertEq(BinaryOperation.GreaterEq.Apply(273, 273), 1);
		AssertEq(BinaryOperation.Less.Apply(273, 273), 0);
		AssertEq(BinaryOperation.LessEq.Apply(273, 273), 1);
	}

	[ArTest.Test] public async Task TestCmdCombine_Vanilla()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(0, 1));
		var tl2 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -401);
			world.SpawnItem(tl1, 665);
			injy.PendParallel([ new CmdCombine(tl0, tl1, tl2, BinaryOperation.Add) ]);
			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertFalse(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl2.Item.Value, -401 + 665);
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdCombine_Overlap()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(0, 1));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 493);
			world.SpawnItem(tl1, -40);
			injy.PendParallel([ new CmdCombine(tl0, tl1, tl1, BinaryOperation.Add) ]);
			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 493 + -40);
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdCombine_INVALID_CASES()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(0, 1));
		var tl2 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector();

		// Missing item (existing tile)
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 43);
			injy.PendParallel([ new CmdCombine(tl0, tl1, tl2, BinaryOperation.BitOr) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Missing tile (source)
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 587);
			injy.PendParallel([ new CmdCombine(tl0.GridLoc, new Vector2I(2, 3), tl1.GridLoc, BinaryOperation.BitOr) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Missing tile (destination)
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -792);
			injy.PendParallel([ new CmdCombine(tl0.GridLoc, tl1.GridLoc, new Vector2I(2, 3), BinaryOperation.BitOr) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Out of bounds (source)
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 587);
			injy.PendParallel([ new CmdCombine(tl0.GridLoc, new Vector2I(2, -3), tl1.GridLoc, BinaryOperation.BitOr) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Out of bounds (destination)
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -792);
			injy.PendParallel([ new CmdCombine(tl0.GridLoc, tl1.GridLoc, new Vector2I(-2, 3), BinaryOperation.BitOr) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Destination tile already occupied
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, -68);
			world.SpawnItem(tl1, -270);
			world.SpawnItem(tl2, -483);
			injy.PendParallel([ new CmdCombine(tl0, tl1, tl2, BinaryOperation.BitOr) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Source item mid-animation
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl1, 404);
			world.SpawnItem(tl2, -502);
			injy.PendParallel([
				new CmdSlide(tl2, Direction.West),
				new CmdCombine(tl0, tl1, tl2, BinaryOperation.BitOr)
			]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdTeleport_Vanilla()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(0, 1));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			var myItem = world.SpawnItem(tl0, -744);
			injy.PendParallel([ new CmdTeleport(tl0, tl1) ]);
			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, -744);
			AssertRefEq(tl1.Item, myItem);
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdTeleport_To_same_tile()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			var myItem = world.SpawnItem(tl0, -744);
			injy.PendParallel([ new CmdTeleport(tl0, tl0) ]);
			await m_Lv.TickOnceAsync();
			AssertTrue(tl0.HasItem());
			AssertEq(tl0.Item.Value, -744);
			AssertRefEq(tl0.Item, myItem);
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdTeleport_INVALID_CASES()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(0, 1));
		var injy = world.PlaceInjector();

		// Missing item (existing tile)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdTeleport(tl0, tl1) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Missing source tile
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdTeleport(new Vector2I(2, 3), tl1.GridLoc) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Missing destination tile
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdTeleport(tl0.GridLoc, new Vector2I(2, 3)) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Out of bounds (source)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdTeleport(new Vector2I(-2, 3), tl1.GridLoc) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Out of bounds (destination)
		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([ new CmdTeleport(tl0.GridLoc, new Vector2I(2, -3)) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Destination tile already occupied
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl1, 6);
			injy.PendParallel([ new CmdTeleport(tl0, tl1) ]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}

		// Teleporting mid-animation
		using (m_Lv.PlaySimulationInDebug())
		{
			world.SpawnItem(tl1, -254);
			injy.PendParallel([
				new CmdSlide(tl1, Direction.West),
				new CmdTeleport(tl0, tl1)
			]);
			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public void TestCmdIf_CheckType_Apply()
	{
		// Apply is a function that must be used by the implementation of CmdIf to check the
		// condition.

		var world = m_Lv.World;

		// Null tiles are not allowed here.
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.NoItem, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.HasItem, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.Equal, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.NotEq, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.Greater, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.GreaterEq, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.Less, null, -1));
		AssertThrow(() => CmdIf.Apply(CmdIf.CheckType.LessEq, null, -1));

		/// Empty Tile, should return false for everything except EmptyTile.
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.NoItem, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.HasItem, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Equal, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.NotEq, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Greater, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.GreaterEq, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Less, tl0, -1));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.LessEq, tl0, -1));

		world.SpawnItem(tl0, 10); // Comparing to 10: ==, >= and <= should be true.
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.NoItem, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.HasItem, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.Equal, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.NotEq, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Greater, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.GreaterEq, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Less, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.LessEq, tl0, 10));

		tl0.Item.SetValue(20); // Comparing to 10: >, >=, !=
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.NoItem, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.HasItem, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Equal, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.NotEq, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.Greater, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.GreaterEq, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Less, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.LessEq, tl0, 10));

		tl0.Item.SetValue(8); // Comparing to 10: <, <=, !=
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.NoItem, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.HasItem, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Equal, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.NotEq, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.Greater, tl0, 10));
		AssertFalse(CmdIf.Apply(CmdIf.CheckType.GreaterEq, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.Less, tl0, 10));
		AssertTrue(CmdIf.Apply(CmdIf.CheckType.LessEq, tl0, 10));
	}

	[ArTest.Test] public async Task TestCmdIf_Vanilla()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(tl0, CmdIf.CheckType.NoItem, -1, 
				[
					new CmdSpawn(tl0, 6),
					new CmdSlide(tl0, Direction.East),
				],
				[
					new CmdSpawn(tl1, 948),
					new CmdSlide(tl1, Direction.West),
				]),
			]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 6);
			AssertTrue(tl1.Item.IsMidAnimation());

			await m_Lv.EndTickAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 6);

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdIf_No_else_branch()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(tl0, CmdIf.CheckType.NoItem, -1, 
				[
					new CmdSpawn(tl0, 6),
					new CmdSlide(tl0, Direction.East),
				]),
			]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 6);
			AssertTrue(tl1.Item.IsMidAnimation());

			await m_Lv.EndTickAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 6);

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdIf_Empty_branches()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(tl0, CmdIf.CheckType.NoItem, -1, [ ], [ ]),
			]);

			await m_Lv.TickOnceAsync();
			await m_Lv.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdIf_Else_branch()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(tl0, CmdIf.CheckType.Greater, Item.MaxValue, 
				[
					new CmdSpawn(tl1, 948),
					new CmdSlide(tl1, Direction.West),
				],
				[
					new CmdSpawn(tl0, 6),
					new CmdSlide(tl0, Direction.East),
				]),
			]);

			await m_Lv.TickOnceAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 6);
			AssertTrue(tl1.Item.IsMidAnimation());

			await m_Lv.EndTickAsync();
			AssertFalse(tl0.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 6);

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdIf_INVALID_CASE_on_non_existant_tile()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(new Vector2I(2, 3), CmdIf.CheckType.NoItem, -1, 
				[
					new CmdSpawn(tl0, 6),
					new CmdSlide(tl0, Direction.East),
				]),
			]);

			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdIf_INVALID_CASE_on_out_of_bounds_tile()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(new Vector2I(-2, 3), CmdIf.CheckType.NoItem, -1, 
				[
					new CmdSpawn(tl0, 6),
					new CmdSlide(tl0, Direction.East),
				]),
			]);

			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdIf_INVALID_CASE_sub_command_fail()
	{
		var world = m_Lv.World;
		var tl0 = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector();

		using (m_Lv.PlaySimulationInDebug())
		{
			injy.PendParallel([
				new CmdIf(tl0, CmdIf.CheckType.NoItem, -1, 
				[
					new CmdKill(tl0),
				]),
			]);

			await AssertThrow(m_Lv.TickOnceAsync);
			AssertTrue(injy.HasPendingCmds());
		}
	}
}