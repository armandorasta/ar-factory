using Godot;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ArFactory.Tests;
using static ArTest.Asserts;


public class CommandTests : ArTest.TestSuit
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

	[ArTest.Test] public async Task TestWaitForTicks()
	{
		using (m_Lv.StartSimulation())
		{
			await m_Lv.ProcessNextTicks(5);
			AssertEq(m_Lv.GetTicksSinceStart(), 6); // First one executes instantly, that's why.
			await m_Lv.ProcessNextTicks(5);
			AssertEq(m_Lv.GetTicksSinceStart(), 11);
		}
	}

	[ArTest.Test] public async Task Test_CmdSpawn_and_CmdKill()
	{
		// This test has to be this simple because it can only use spawn (not even kill).
		// See TestKill for a more complete test.

		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var injy0 = world.PlaceInjector([
			[new CmdSleep(1)],
			[ // 1. Vanilla
				new CmdSpawn(tl, 5),
				new CmdSleep(1),
			],
			[ // 2. Another item is in the way.
				new CmdKill(tl),
				new CmdSpawn(tl, 10), // injy1 is suppossed to kill this one.
				new CmdSpawn(tl, 11), // Should be delayed a tick.
				new CmdSleep(3), // Leak protection

				// Tick 3: injy0 cleans up and spawns a 10.
				// Tick 4: injy1 kills the 10, and injy0 only realizes that next tick.
				// Tick 3: injy0 finally spawns the 11.
				// The sleep is just a padding.
			],
			[ // 3. Waiting for the kill
				// Tick 6
				new CmdKill(tl), // Clean up from last test
				new CmdKill(tl), // Should be tick 8
				// injy 1 will give us a prey next tick, which will be killed the tick after.
				new CmdSleep(3) // Leak protection

				// Here's the full shit:
				// Tick 6: injy0 cleans up from last tick and pends a blocking kill
				// Tick 7: injy1 spawns a prey for the blocking kill from last tick, but because
				//         it's placed after injy0, injy0 will only see the item next tick.
				// Tick 8: finally injy0 will kill the item.
			],

			// 4. Non-blocking stuff 
			[ // Tick 9, no blocking from here on
				new CmdSpawn(tl, 41).MakeNonBlocking(), // Works
				new CmdSleep(1),
			],
			[
				new CmdKill(tl).MakeNonBlocking(), // Also works
				new CmdSleep(1),				
			],
			[
				new CmdKill(tl).MakeNonBlocking(), // Fails (no item to kill)
				new CmdSleep(1),		
			],
			[
				new CmdSpawn(tl, 0), // Works as there's no blocking item.
				new CmdSpawn(tl, -90).MakeNonBlocking(), // Fails
			],
		]);

		var injy1 = world.PlaceInjector([
			[new CmdSleep(3)],
			[
				// The first spawn of injy0's group 3 should have executed last tick.
				new CmdKill(tl), // Tick 4
				new CmdSleep(3)
			],
			[ // Tick 7
				new CmdSpawn(tl, 99),
			]
		]);

		using (m_Lv.StartSimulation())
		{
			AssertFalse(tl.HasItem());
		
			// 1
			await m_Lv.ProcessNextTicks(1); // Spawns!
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 5);
			AssertTrue(tl.Item.IsAllowedToMove());
			AssertFalse(tl.Item.IsMidAnimation());

			// 2
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 10);
			AssertTrue(injy1.HasPendingCmds());

			await m_Lv.ProcessNextTicks(1); // injy0 goes for the kill.
			AssertFalse(tl.HasItem());

			// We need to wait for next tick cuz injy0 before the kill.
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 11);

			// 3
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl.HasItem());
			AssertTrue(injy0.HasPendingCmds());
			AssertTrue(injy1.HasPendingCmds());

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 99);
			AssertFalse(injy1.HasPendingCmds()); // Die after the spawn.

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl.HasItem());

			// 4
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 41);
			
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl.HasItem());
			
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl.HasItem());
			
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 0);

			AssertFalse(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdSleep()
	{
		var world = m_Lv.World;
		var tl = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[
				new CmdSleep(2)
			],
			[
				new CmdSpawn(tl, 3),
				new CmdSleep(1),
			],
			[
				new CmdKill(tl)
			],
		]);

		using (m_Lv.StartSimulation())
		{
			// First cycle executes instantly, so we start waiting from the second and up.

			await m_Lv.ProcessNextTicks(1);
			AssertEq(m_Lv.GetTicksSinceStart(), 2);
			AssertFalse(tl.HasItem());
		
			await m_Lv.ProcessNextTicks(1);
			AssertEq(m_Lv.GetTicksSinceStart(), 3);
			AssertFalse(tl.HasItem());
			// And now sleep is done.

			await m_Lv.ProcessNextTicks(1);
			AssertEq(m_Lv.GetTicksSinceStart(), 4);
			AssertTrue(tl.HasItem());
			// The second sleep should be done immediately this tick as well.

			await m_Lv.ProcessNextTicks(1);
			AssertEq(m_Lv.GetTicksSinceStart(), 5);
			AssertFalse(tl.HasItem()); // Item should be dead now
			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCmdSlide()
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

		var nonBlockingSlides = new CmdSlide[]
		{
			new(tlEast0, Direction.East),
			new(tlEast0, Direction.East),
			new(tlEast0, Direction.East),
		};
	
		var injy0 = world.PlaceInjector([
			[new CmdSleep(1)],

			// 1. Vanilla
			[
				new CmdSpawn(tlEast0, 1),
				new CmdSpawn(tlSouth0, 2),
				new CmdSpawn(tlNorth0, 3),
				new CmdSpawn(tlWest0, 4),
				new CmdSleep(1),
			],
			[
				new CmdSlide(tlEast0, Direction.East),
				new CmdSlide(tlSouth0, Direction.South),
				new CmdSlide(tlNorth0, Direction.North),
				new CmdSlide(tlWest0, Direction.West),
				new CmdSleep(2),
			],

			// For now on I will only test east and assume the rest will work similarly because
			// otherwise the code size will explode for a very stupid reason...
			
			// Also injy1 joins the game... next tick

			// 2. Destination has an item in the way
			[ // The kill happens external to the unit
				new CmdKill(tlEast1),
				new CmdKill(tlSouth1),
				new CmdKill(tlNorth1),
				new CmdKill(tlWest1),
				
				new CmdSpawn(tlEast0, 1),
				new CmdSpawn(tlEast1, 2),
				new CmdSlide(tlEast0, Direction.East),
				new CmdSleep(3), // Leak protection
			],
			[ // The kill is internal to the unit, shouldnt matter since units are just a hoax xd
				// Tick 8
				new CmdKill(tlEast1),
				new CmdSpawn(tlEast0, 1),
				new CmdSpawn(tlEast1, 2),
				new CmdSlide(tlEast0, Direction.East),
				new CmdKill(tlEast1), // Here the kill happens, then the above slide overlaps.
				new CmdSleep(2), // Leak protection
			],

			// 3. Source has yet to have an item
			[
				new CmdKill(tlEast1),
				new CmdSlide(tlEast0, Direction.East),
				// The overlap algo doesn't cover this case... when an item spawns after the slide
				// command checks for it, because almost always the spawn command is placed before.
				new CmdSpawn(tlEast0, 67),
				new CmdSleep(3),
			],

			// 4. Non-blocking stuff
			[
				new CmdKill(tlEast1),
				nonBlockingSlides[0].MakeNonBlocking(), // Failure (nothing to slide)
				new CmdSleep(1),
			],
			[
				new CmdSpawn(tlEast0, -13),
				new CmdSpawn(tlEast1, -107),
				nonBlockingSlides[1].MakeNonBlocking(), // Failure (destination blocked)
				new CmdSleep(1),
			],
			[
				new CmdKill(tlEast1),
				nonBlockingSlides[2].MakeNonBlocking(), // Works
				// new CmdSleep(2),
			],
		]);

		var injy1 = world.PlaceInjector([
			[new CmdSleep(5)],
			[ // 2
				new CmdKill(tlEast1), // Tick 5
			],
		]);

		using (m_Lv.StartSimulation())
		{
			// 1
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast0.HasItem());
			AssertTrue(tlSouth0.HasItem());
			AssertTrue(tlNorth0.HasItem());
			AssertTrue(tlWest0.HasItem());
			var itEast = tlEast0.Item;
			var itSouth = tlSouth0.Item;
			var itNorth = tlNorth0.Item;
			var itWest = tlWest0.Item;

			await m_Lv.ProcessNextTicks(1);
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

			await m_Lv.ProcessNextTicks(1);
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

			// 2
			// External
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertEq(tlEast0.Item.Value, 1);
			AssertEq(tlEast1.Item.Value, 2);

			// injy1 goes for the kill, but the overlap algo kicks in.
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertTrue(tlEast1.Item.IsMidAnimation());
			AssertEq(tlEast1.Item.Value, 1);
			
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertFalse(tlEast1.Item.IsMidAnimation());
			
			AssertFalse(injy1.HasPendingCmds());

			// Internal
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertTrue(tlEast1.Item.IsMidAnimation());
			AssertEq(tlEast1.Item.Value, 1);

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertFalse(tlEast1.Item.IsMidAnimation());

			// 3
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast0.HasItem());
			AssertFalse(tlEast0.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertTrue(tlEast1.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertFalse(tlEast1.Item.IsMidAnimation());

			// 4
			await m_Lv.ProcessNextTicks(1); // Move nothing and being unblocking == death
			AssertTrue(nonBlockingSlides[0].IsDone());

			await m_Lv.ProcessNextTicks(1); // Move something but get blocked    == death
			AssertTrue(nonBlockingSlides[1].IsDone());
			
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(nonBlockingSlides[2].IsDone());
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertTrue(tlEast1.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(nonBlockingSlides[2].IsDone());
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertFalse(tlEast1.Item.IsMidAnimation());

			AssertFalse(injy0.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestUpdate()
	{
		var world = m_Lv.World;
		var tlEast = world.InstallEmptyTile(Vector2I.Zero);
		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[
				new CmdSpawn(tlEast, 89),
				new CmdSleep(1),
			],
			[
				new CmdUpdate(tlEast, x => 0),
				new CmdSleep(1),
			],
			[
				new CmdUpdate(tlEast, x => 100),
				new CmdSleep(1),
			],
			[
				new CmdKill(tlEast),
				new CmdSpawn(tlEast, 5),
				new CmdUpdate(tlEast, x => x * 2),
			],
			// TODO: Add cases where the item still is not there, and unblocking.
			[
				
			]
		]);

		using (m_Lv.StartSimulation())
		{
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertEq(tlEast.Item.Value, 89);
			var it0 = tlEast.Item;

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertEq(tlEast.Item.Value, 0);
			AssertRefEq(tlEast.Item, it0);

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertEq(tlEast.Item.Value, 100);
			AssertRefEq(tlEast.Item, it0);

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertEq(tlEast.Item.Value, 10);
		}
	}

	[ArTest.Test] public async Task TestAwait()
	{
		var world = m_Lv.World;
		var tlEast = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));

		var waits = new CmdAwait[]
		{
			new(tlEast),
			new(tl1),
			new(tl1),
		};

		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[
				new CmdSpawn(tlEast, 7),
				waits[0], // Here it does nothing.
				new CmdSleep(1),
			],
			[
				new CmdSlide(tlEast, Direction.East),
				waits[1], // Also does nothing here...
			],
			[
				new CmdKill(tl1),
				new CmdSleep(3),
			],
			[
				new CmdSpawn(tl1, 9), // Second injector will wait for this.
			]
		]);

		var altInjy = world.PlaceInjector([
			[new CmdSleep(3)],
			[waits[2]], // Waits for the next spawn for 3 ticks.
		]);

		using (m_Lv.StartSimulation())
		{
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(waits[0].IsDone());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertFalse(waits[1].IsDone());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl1.HasItem());
			AssertTrue(waits[1].IsDone());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl1.HasItem());
			AssertFalse(waits[2].IsDone());
			AssertTrue(injy.HasPendingCmds());
			AssertTrue(altInjy.HasPendingCmds());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl1.HasItem());
			AssertFalse(waits[2].IsDone());
			AssertTrue(injy.HasPendingCmds());
			AssertTrue(altInjy.HasPendingCmds());

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tl1.HasItem());
			AssertTrue(waits[2].IsDone());
			AssertFalse(injy.HasPendingCmds());
			AssertFalse(altInjy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestVacate()
	{
		var world = m_Lv.World;
		var tlEast0 = world.InstallEmptyTile(Vector2I.Zero);
		var tl10 = world.InstallEmptyTile(new(1, 0));
		var tlEast1 = world.InstallEmptyTile(new(0, 1));
		var tl11 = world.InstallEmptyTile(new(1, 1));

		var vacs = new CmdVacate[] {
			new([tlEast0]),
			new([tlEast0]),
			new([tl10, tl11]),
		};

		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[
				vacs[0],
				new CmdSleep(1), // So it doesn't leak to the next group
			],
			[
				new CmdSpawn(tlEast0, 7),
				new CmdSlide(tlEast0, Direction.East),
				vacs[1], // Should be done immediate because they don't wait items to finish sliding.
				new CmdSleep(2), // So the slide doesn't leak to the next group
			],
			[
				new CmdSpawn(tl11, 8),
				new CmdSlide(tl10, Direction.West), // tl1 => tlEast
				new CmdSlide(tl11, Direction.West), // tl3 => tl2,
				vacs[2],
			]
		]);

		using (m_Lv.StartSimulation())
		{
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(vacs[0].IsDone());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tl10.HasItem());
			AssertTrue(tl10.Item.IsMidAnimation());
			AssertTrue(vacs[1].IsDone());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast0.HasItem());
			AssertTrue(tl10.HasItem());
			AssertFalse(tl10.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl10.HasItem());
			AssertFalse(tl11.HasItem());
			AssertTrue(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertTrue(tlEast0.Item.IsMidAnimation());
			AssertTrue(tlEast1.Item.IsMidAnimation());
			AssertTrue(vacs[2].IsDone());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tl10.HasItem());
			AssertFalse(tl11.HasItem());
			AssertTrue(tlEast0.HasItem());
			AssertTrue(tlEast1.HasItem());
			AssertFalse(tlEast0.Item.IsMidAnimation());
			AssertFalse(tlEast1.Item.IsMidAnimation());

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestClone()
	{
		var world = m_Lv.World;
		var tlEast = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var tl2 = world.InstallEmptyTile(new(2, 0));
		var tl3 = world.InstallEmptyTile(new(2, 1));
		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[ // 1. Vanilla
				new CmdSpawn(tlEast, 5),
				new CmdClone(tlEast, [tl1, tl2]),
				new CmdSleep(1),
			],
			[ // 2. One destination tile blocked.
				new CmdUpdate(tlEast, _ => 10),
				new CmdKill(tl1), // tlEast = 10, tl1 => nothing, tl2 => 5
				new CmdClone(tlEast, [tl1, tl2]), // Should wait for the item on tl2 to slide away first.
				new CmdSlide(tl2, Direction.South),
				new CmdSleep(2), // So the slide doesn't leak to the next group...
			],
			[ // 3. Source tile blocked.
				new CmdKill(tl1),
				new CmdKill(tl2),
				new CmdKill(tl3),
				new CmdSlide(tlEast, Direction.East),
				new CmdClone(tl1, [tl2]), // Should wait for the item to slide in first.
			]
		]);

		using (m_Lv.StartSimulation())
		{
			// 1
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl1.Item.Value, tlEast.Item.Value);
			AssertEq(tl2.Item.Value, tlEast.Item.Value);

			// 2
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertFalse(tl2.HasItem());
			AssertTrue(tl3.HasItem());
			AssertEq(tlEast.Item.Value, 10);
			AssertEq(tl3.Item.Value, 5);
			AssertTrue(tl3.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertTrue(tl3.HasItem());
			AssertEq(tl1.Item.Value, tlEast.Item.Value);
			AssertEq(tl2.Item.Value, tlEast.Item.Value);
			AssertEq(tl3.Item.Value, 5);

			// 3
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertFalse(tl2.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl1.Item.Value, tl2.Item.Value);

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestCombine()
	{
		var world = m_Lv.World;
		var tlEast = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var tl2 = world.InstallEmptyTile(new(2, 0));
		var tl3 = world.InstallEmptyTile(new(2, 1));
		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[ // 1. Vanilla
				new CmdSpawn(tlEast, 29),	
				new CmdSpawn(tl1, -29),
				new CmdCombine([tlEast, tl1], tl2, vals => vals.Sum()),
				new CmdSleep(1),
			],
			[ // 2. Item sliding away from destination tile.
				new CmdSpawn(tlEast, 5),	
				new CmdSpawn(tl1, 7),
				new CmdSlide(tl2, Direction.South), // tl2 is 0
				// Should just spawn the new item immediately since the command doesn't wait for
				// items to slide away.
				new CmdCombine([tlEast, tl1], tl2, vals => vals.Aggregate(1, (l, r) => l * r)),
				new CmdSleep(2), // So the slide doesn't leak to the next group			
			],
			[ // 3. Destination tile gets blocked, then unblocked.
				new CmdSpawn(tlEast, 100),
				new CmdSpawn(tl1, -50),
				new CmdKill(tl2),
				new CmdSlide(tl3, Direction.North), // tl3 is 0
				// Placing this below the kill will speed up the code by 1 tick since combine depends 
				// on the kill after it.
				new CmdCombine([tlEast, tl1], tl2, vals => vals.Sum()),
				new CmdKill(tl2),
				new CmdSleep(3), // So the combine doesn't leak to the next group
			],
			[ // 4. Source is empty, then gets filled.
				new CmdSpawn(tlEast, 10),
				new CmdSlide(tl2, Direction.West), // tl2 is 50
				new CmdCombine([tlEast, tl1], tl2, vals => vals.Sum()),
				new CmdSleep(2), // So the slide doesn't leak to the next group			
			],
			[ // 5. Destination overlaps with source.
				new CmdSpawn(tlEast, 69),
				new CmdSpawn(tl1, 37),
				new CmdKill(tl2),
				new CmdCombine([tlEast, tl1], tl1, vals => vals.Sum()),
			],
		]);

		using (m_Lv.StartSimulation())
		{
			// 1
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl2.Item.Value, 29 - 29);

			// 2
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl2.Item.Value, 5 * 7);

			await m_Lv.ProcessNextTicks(1); // Wait for the slide to finish!

			// 3
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertTrue(tl2.Item.IsMidAnimation());
			AssertEq(tl2.Item.Value, 0);
			
			// The combine is before the kill, so it doesn't see the empty tile until next tick.
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertFalse(tl2.HasItem());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl2.Item.Value, 100 - 50);

			// 4
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertTrue(tl2.HasItem());
			AssertEq(tl2.Item.Value, 10 + 50);

			// 5
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 69 + 37);

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Test] public async Task TestTeleport()
	{
		var world = m_Lv.World;
		var tlEast = world.InstallEmptyTile(Vector2I.Zero);
		var tl1 = world.InstallEmptyTile(new(1, 0));
		var tl2 = world.InstallEmptyTile(new(2, 0));
		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[ // 1. Vanilla,
				new CmdSpawn(tlEast, 67),
				new CmdTeleport(tlEast, tl1),
				new CmdSleep(1), // Leak protection
			],
			[ // 2. Destination tile has item sliding away
				new CmdKill(tl1),

				new CmdSpawn(tlEast, -89),
				new CmdSpawn(tl1, 10),
				new CmdSlide(tl1, Direction.East),
				// Should ignore the sliding animtion spawn immediately.
				new CmdTeleport(tlEast, tl1),
				new CmdSleep(2), // Leak protection
			],
			[ // 3. Destination tile gets blocked, then unblocked.
				new CmdKill(tl1),
				new CmdKill(tl2),
				
				new CmdSpawn(tlEast, 55),
				new CmdSpawn(tl2, -33),
				new CmdSlide(tl2, Direction.West),
				new CmdKill(tl1), // Should delete the sliding item, not the teleported one.
				new CmdTeleport(tlEast, tl1),
				new CmdSleep(2), // Leak protection
			],
			[ // 4. Source is empty, then gets filled.		
				new CmdKill(tl1),

				new CmdSpawn(tl1, -23),
				new CmdTeleport(tlEast, tl1),
				new CmdSlide(tl1, Direction.West),
			],
		]);

		using (m_Lv.StartSimulation())
		{
			// 1
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, 67);

			// 2
			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertEq(tl1.Item.Value, -89);

			await m_Lv.ProcessNextTicks(1); // Wait for the slide to finish.

			// 3
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, -33);
			var tlEastIt = tlEast.Item;

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertRefEq(tl1.Item, tlEastIt);
			AssertEq(tl1.Item.Value, 55);

			// 4
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertTrue(tlEast.Item.IsMidAnimation());
			AssertEq(tlEast.Item.Value, -23);

			// Taking extra tick because the teleport executes before the slide, so it doesn't know
			// any changes it makes until next tick.
			await m_Lv.ProcessNextTicks(1);
			AssertTrue(tlEast.HasItem());
			AssertFalse(tl1.HasItem());
			AssertFalse(tlEast.Item.IsMidAnimation());
			AssertEq(tlEast.Item.Value, -23);
			tlEastIt = tlEast.Item;

			await m_Lv.ProcessNextTicks(1);
			AssertFalse(tlEast.HasItem());
			AssertTrue(tl1.HasItem());
			AssertRefEq(tl1.Item, tlEastIt);
			AssertEq(tl1.Item.Value, -23);

			AssertFalse(injy.HasPendingCmds());
		}
	}

	[ArTest.Ignore]
	[ArTest.Test] public async Task TestCondSlide()
	{
		var world = m_Lv.World;
		for (var y = 0; y < 3; ++y)
		{
			for (var x = 0; x < 3; ++x)
			{
				world.InstallEmptyTile(new(x, y));
			}
		}

		var injy = world.PlaceInjector([
			[new CmdSleep(1)],
			[ // 1. Vanilla
				new CmdSpawn(world[0, 0], -16),
				new CmdCondSlide(world[0, 0], 
					world[1, 0], Direction.East, 
					world[1, 1], Direction.South, tl => tl.Item.Value < 0),
			]
		]);

		using (m_Lv.StartSimulation())
		{
			await m_Lv.ProcessNextTicks(1);

			AssertFalse(injy.HasPendingCmds());
		}
	}
}