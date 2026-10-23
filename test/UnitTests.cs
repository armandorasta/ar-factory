using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class UnitTests : ArTest.TestSuit
{
	private Editor Lvl => m_Lv!;
	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.EditorScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(Lvl);
		Lvl.Clock.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(6, 6));
	}

	public override void AfterEach()
	{
		AssertTrue(Lvl.Clock.IsHalted());
	}

	public override void AfterAll()
	{
		Lvl.QueueFree();
		RemoveNode(Lvl);
	}

	[ArTest.Test] public void TestCanFitIn()
	{
		var world = Lvl.World;
		
		// 1. Empty world.
		// One block.
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.East));	
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.South));	
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.West));
		// 2x1
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(2, 1), Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(2, 1), Direction.East));	
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(2, 1), Direction.South));	
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(2, 1), Direction.West));
		// 4x4
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(4, 4), Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(4, 4), Direction.East));	
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(4, 4), Direction.South));	
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(4, 4), Direction.West));

		// 2. Something in the way
		world.InstallEmptyTile(Vector2I.One);
		// One block.
		AssertFalse(Unit.CanFitIn(world, Vector2I.One, Vector2I.One, Direction.North));
		AssertFalse(Unit.CanFitIn(world, Vector2I.One, Vector2I.One, Direction.East));	
		AssertFalse(Unit.CanFitIn(world, Vector2I.One, Vector2I.One, Direction.South));	
		AssertFalse(Unit.CanFitIn(world, Vector2I.One, Vector2I.One, Direction.West));
		// 2x1
		AssertFalse(Unit.CanFitIn(world, new(1, 0), new(2, 1), Direction.North));
		AssertFalse(Unit.CanFitIn(world, new(0, 1), new(2, 1), Direction.East));	
		AssertFalse(Unit.CanFitIn(world, new(1, 0), new(2, 1), Direction.South));	
		AssertFalse(Unit.CanFitIn(world, new(0, 1), new(2, 1), Direction.West));
		// 4x4
		AssertFalse(Unit.CanFitIn(world, new(0, 0), new(4, 4), Direction.North));
		AssertFalse(Unit.CanFitIn(world, new(1, 0), new(4, 4), Direction.East));	
		AssertFalse(Unit.CanFitIn(world, new(0, 1), new(4, 4), Direction.South));	
		AssertFalse(Unit.CanFitIn(world, new(1, 1), new(4, 4), Direction.West));
		world.DeleteTile(Vector2I.One);

		// 3. Out of bounds
		// Placing out of bounds
		AssertFalse(Unit.CanFitIn(world, 1000 * Vector2I.One, new(3, 1), Direction.North));
		AssertFalse(Unit.CanFitIn(world, 1000 * Vector2I.One, new(3, 1), Direction.East));
		AssertFalse(Unit.CanFitIn(world, 1000 * Vector2I.One, new(3, 1), Direction.South));
		AssertFalse(Unit.CanFitIn(world, 1000 * Vector2I.One, new(3, 1), Direction.West));
		// Negative
		AssertFalse(Unit.CanFitIn(world, -1   * Vector2I.One, new(3, 1), Direction.North));
		AssertFalse(Unit.CanFitIn(world, -1   * Vector2I.One, new(3, 1), Direction.East));
		AssertFalse(Unit.CanFitIn(world, -1   * Vector2I.One, new(3, 1), Direction.South));
		AssertFalse(Unit.CanFitIn(world, -1   * Vector2I.One, new(3, 1), Direction.West));
		// Clipping out of bounds
		AssertFalse(Unit.CanFitIn(world, Vector2I.Zero, new(1000, 1), Direction.East));
		AssertFalse(Unit.CanFitIn(world, Vector2I.Zero, new(1, 1000), Direction.North));
		AssertFalse(Unit.CanFitIn(world, Vector2I.Zero, new(1000, 1000), Direction.South));
		AssertFalse(Unit.CanFitIn(world, Vector2I.One, world.Dimensions, Direction.East)); // Barely clipping

		// 4. Perfect fit
		// As big as the whole world exactly.
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, world.Dimensions, Direction.East));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, world.Dimensions, Direction.West));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(world.Dimensions.Y, world.Dimensions.X), Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(world.Dimensions.Y, world.Dimensions.X), Direction.South));
		// 1x1
		world.InstallEmptyTile(new(1, 0));
		world.InstallEmptyTile(new(0, 1));
		world.InstallEmptyTile(new(1, 1));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.East));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.South));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, Vector2I.One, Direction.West));
		// 2x1 or 1x2.
		world.DeleteTile(new Vector2I(1, 0));
		world.InstallEmptyTile(new(2, 0));
		world.InstallEmptyTile(new(2, 1));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(2, 1), Direction.East));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(2, 1), Direction.West));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(1, 2), Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(1, 2), Direction.South));
	}

	[ArTest.Test] public async Task TestSupplier_tick_rate()
	{
		var world = Lvl.World;
		var supp = world.PlaceSupplier(Vector2I.Zero, Direction.East, 3, [237, 68, 236]);
		var tl1 = world.InstallEmptyTile(new Vector2I(1, 0));
		AssertTrue(world.HasTile(supp.GLocation));
		using (Lvl.Clock.PlaySimulationInDebug())
		{
			await Lvl.Clock.TickOnceAsync(); // Tick 0 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Clock.TickOnceAsync(); // Tick 1 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Clock.TickOnceAsync(); // Tick 2 processed, first item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 237);

			await Lvl.Clock.EndTickAsync();
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await Lvl.Clock.TickOnceAsync(); // Tick 3 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Clock.TickOnceAsync(); // Tick 4 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Clock.TickOnceAsync(); // Tick 5 processed, second item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 68);

			await Lvl.Clock.EndTickAsync();
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await Lvl.Clock.TickOnceAsync(); // Tick 6 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Clock.TickOnceAsync(); // Tick 7 processed
			AssertFalse(tl1.HasItem());

			await Lvl.Clock.TickOnceAsync(); // Tick 8 processed, third item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 236);
			AssertTrue(supp.IsDoneWithInputSeq());
		}
	}

	[ArTest.Test] public async Task TestSupplier_different_direction()
	{
		var world = Lvl.World;
		world.Reset();
		var supp = world.PlaceSupplier(Vector2I.One, Direction.East, 1, [261, 296]);
		var tl1 = world.InstallEmptyTile(supp.GLocation + supp.Dir.ToGrid());
		AssertTrue(world.HasTile(supp.GLocation));
		using (Lvl.Clock.PlaySimulationInDebug())
		{
			await Lvl.Clock.TickOnceAsync();
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 261);
		}
	}

	[ArTest.Test] public async Task TestSupplier_clogged()
	{
		var world = Lvl.World;
		var supp = world.PlaceSupplier(Vector2I.One, Direction.East, 2, [261, 296]);
		var injy = world.PlaceInjector();
		var tlNearby = world.InstallEmptyTile(supp.GLocation + supp.Dir.ToGrid());
		var tlFar = world.InstallEmptyTile(supp.GLocation + 2 * supp.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlNearby, -217); // In the way

			await Lvl.Clock.TickOnceAsync(); // Nothing should happen, but we have no way of checking
			await Lvl.Clock.TickOnceAsync(); // First might have spawned, but not checkable
			await Lvl.Clock.TickOnceAsync(); // Nothing should happen
			await Lvl.Clock.TickOnceAsync(); // Blocked.

			tlNearby.DestroyItem(); // Remove the obsticle
			await Lvl.Clock.TickOnceAsync(); // First item slides out first.
			AssertTrue(tlNearby.HasItem());
			AssertTrue(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 261);

			await Lvl.Clock.EndTickAsync();
			AssertTrue(tlNearby.HasItem());
			AssertFalse(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 261);

			injy.PendParallel([new CmdSlide(tlNearby, supp.Dir)]);

			// First item finished sliding out, second item spawns and slides out right after it.
			await Lvl.Clock.TickOnceAsync();
			AssertTrue(tlNearby.HasItem());
			AssertTrue(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 296);

			AssertTrue(tlFar.HasItem());
			AssertTrue(tlFar.Item.IsMidAnimation());			
			AssertEq(tlFar.Item.Value, 261);

			AssertTrue(supp.IsDoneWithInputSeq());
		}
	}

	[ArTest.Test] public async Task TestSupplier_missing_connections()
	{
		var world = Lvl.World;
		var supp = world.PlaceSupplier(Vector2I.One, Direction.East, 2, [854, 743, -374]);
		var tlSupp = supp.GetTile(Vector2I.Zero);

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // Item would slide out if the supplier was connected.
			await Lvl.Clock.TickOnceAsync(); // clogged
			await Lvl.Clock.TickOnceAsync(); // clogged
			await Lvl.Clock.TickOnceAsync(); // clogged

			var tl = world.InstallEmptyTile(tlSupp.GLocation + supp.Dir.ToGrid());
			await Lvl.Clock.TickOnceAsync(); // First item slides out
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 854);
			await Lvl.Clock.EndTickAsync();
			world.DeleteTile(tl.GLocation);

			await Lvl.Clock.TickOnceAsync(); // Next item would slide out if I didn't delete the tile.
			await Lvl.Clock.TickOnceAsync(); // clogged
			await Lvl.Clock.TickOnceAsync(); // clogged
			await Lvl.Clock.TickOnceAsync(); // clogged
		}
	}

	[ArTest.Test] public async Task TestSupplier_restart()
	{
		var world = Lvl.World;
		var supp = world.PlaceSupplier(Vector2I.One, Direction.East, 2, [854, 743, -374]);
		var tlNearby = world.InstallEmptyTile(supp.GLocation + supp.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // First item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 854);
		}

		using (Lvl.Clock.PlaySimulationInDebug()) // Restart
		{
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // First item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 854);
			await Lvl.Clock.EndTickAsync();
			tlNearby.DestroyItem();
			
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // Second item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 743);
		}

		using (Lvl.Clock.PlaySimulationInDebug()) // Restart
		{
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // First item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 854);
			await Lvl.Clock.EndTickAsync();
			tlNearby.DestroyItem();

			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // Second item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 743);
			await Lvl.Clock.EndTickAsync();
			tlNearby.DestroyItem();

			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			await Lvl.Clock.TickOnceAsync(); // Second item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, -374);
		}
	}

	[ArTest.Test] public async Task TestSlider_all_directions()
	{
		var world = Lvl.World;
		var slid = world.PlaceSlider(new Vector2I(3, 4), Direction.North);
		var injy = world.PlaceInjector();

		var tlSlid = slid.GetTile(Vector2I.Zero);
		var tlNorth = world.InstallEmptyTile(tlSlid.GLocation + Direction.North.ToGrid());
		var tlEast = world.InstallEmptyTile(tlSlid.GLocation + Direction.East.ToGrid());
		var tlSouth = world.InstallEmptyTile(tlSlid.GLocation + Direction.South.ToGrid());
		var tlWest = world.InstallEmptyTile(tlSlid.GLocation + Direction.West.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			// From south
			world.SpawnItem(tlSouth, -511);
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.Clock.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, -511);

			await Lvl.Clock.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, -511);

			await Lvl.Clock.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From east
			world.SpawnItem(tlEast, 635);
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.Clock.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 635);

			await Lvl.Clock.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 635);

			await Lvl.Clock.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From west
			world.SpawnItem(tlWest, 999);
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.Clock.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 999);

			await Lvl.Clock.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 999);

			await Lvl.Clock.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From north (will be stuck forever)
			world.SpawnItem(tlNorth, 638);
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.Clock.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 638);
			AssertTrue(injy.HasPendingCmds()); // Stuck forever...
		}
	}

	[ArTest.Test] public async Task TestSlider_tick_sync()
	{
		var world = Lvl.World;
		var slid = world.PlaceSlider(new Vector2I(3, 4), Direction.West, 3);

		var tlSlid = slid.GetTile(Vector2I.Zero);
		var tlDest = world.InstallEmptyTile(tlSlid.GLocation + Direction.West.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlSlid, 13);
			await Lvl.Clock.TickOnceAsync(); // Should move 2 ticks from now.
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
			await Lvl.Clock.TickOnceAsync(); // Next tick
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());

			await Lvl.Clock.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 13);

			await Lvl.Clock.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.Clock.TickOnceAsync();
		
			world.SpawnItem(tlSlid, -804);
			await Lvl.Clock.TickOnceAsync(); // Should still move the tick after this one.
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
		
			await Lvl.Clock.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, -804);

			await Lvl.Clock.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.Clock.TickOnceAsync();
			await Lvl.Clock.TickOnceAsync();
		
			world.SpawnItem(tlSlid, 982); // Should move next tick right away.
			await Lvl.Clock.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 982);
		}
	}

	[ArTest.Test] public async Task TestSlider_no_dest_tile()
	{
		var world = Lvl.World;
		var slid = world.PlaceSlider(new Vector2I(3, 4), Direction.West, 3);
		var tlSlid = slid.GetTile(Vector2I.Zero);

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlSlid, 261);
			await Lvl.Clock.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 261);
		}
	}

	[ArTest.Test] public async Task TestLocking_using_slider()
	{
		// Semantics for the time being:
		// - Locked units still allow items in.
		// - Locking a unit will reset the counter, so it acts like a reset kind of. I made it this way because of the
		//   on-demand units, and all their complications, solved by just adding a `m_Count = 0`.

		var world = Lvl.World;
		var slid = world.PlaceSlider(new Vector2I(2, 1), Direction.East, 2);
		var injy = world.PlaceInjector();
		
		var tlIn = world.InstallEmptyTile(slid.GLocation + slid.Dir.Invert().ToGrid());
		var tlSlid = slid.GetTile(Vector2I.Zero);
		var tlOut = world.InstallEmptyTile(slid.GLocation + slid.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, -103);
			injy.PendFullSequence([[ new CmdSlide(tlIn, slid.Dir) ]]);
			slid.Lock(); // Should not crash
			AssertTrue(slid.IsLocked());
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // 1
			await Lvl.TickOnceForTestingAsync(); // 2, here with locking the item would slide out, but it shouldn't
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlOut.HasItem());
			await Lvl.TickOnceForTestingAsync(); // Locked
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlOut.HasItem());
			await Lvl.TickOnceForTestingAsync(); // Locked
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlOut.HasItem());

			slid.Unlock();
			AssertFalse(slid.IsLocked());
			await Lvl.TickOnceForTestingAsync(); // 1 (counter resets when locking)
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlOut.HasItem());
			await Lvl.TickOnceForTestingAsync(); // 2, item slides out yay!
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlOut.HasItem());
		}
	}

	[ArTest.Test] public async Task TestUpdater()
	{
		// I am not gonna test all the update types and just provide a gaurantee of translating to UnaryOperation.

		var world = Lvl.World;
		var upd = world.PlaceUpdater(new Vector2I(3, 4), Direction.East, 2, UNUpdater.UpdateType.Increment);
		var injy = world.PlaceInjector();

		var tlUpd = upd.GetTile(Vector2I.Zero);
		var tlIn = world.InstallEmptyTile(tlUpd.GLocation + upd.Dir.Invert().ToGrid());
		var tlOut = world.InstallEmptyTile(tlUpd.GLocation + upd.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 402);
			injy.PendFullSequence([[ new CmdSlide(tlIn, upd.Dir) ]]);

			await Lvl.Clock.TickOnceAsync(); // Slides in and updater starts counting ticks.
			AssertFalse(tlOut.HasItem());
			await Lvl.Clock.TickOnceAsync(); // Rate is 2 ticks per item, this tick does nothing.
			AssertFalse(tlOut.HasItem());

			await Lvl.Clock.TickOnceAsync(); // Updated item pops out.
			AssertTrue(tlOut.HasItem());
			AssertEq(tlOut.Item.Value, 402 + 1);
		}
	}

	[ArTest.Test] public async Task TestUpdater_clogged()
	{
		var world = Lvl.World;
		var upd = world.PlaceUpdater(new Vector2I(3, 4), Direction.East, 2, UNUpdater.UpdateType.Increment);
		var injy = world.PlaceInjector();
		var tlIn = world.InstallEmptyTile(upd.Output.GLocation + upd.Dir.Invert().ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 142);
			injy.PendFullSequence([[ new CmdSlide(tlIn, upd.Dir) ]]);
			
			await Lvl.Clock.TickOnceAsync(); // Slides in and updater starts counting ticks.
			await Lvl.Clock.TickOnceAsync(); // 1
			await Lvl.Clock.TickOnceAsync(); // 2, would slide out if there was a way.
			await Lvl.Clock.TickOnceAsync(); // Clogged
			await Lvl.Clock.TickOnceAsync(); // Clogged
			await Lvl.Clock.TickOnceAsync(); // Clogged

			var tlOut = world.InstallEmptyTile(upd.Output.GLocation + upd.Dir.ToGrid());
			await Lvl.Clock.TickOnceAsync(); // Item slides out updated
			AssertTrue(tlOut.HasItem());
			AssertTrue(tlOut.Item.IsMidAnimation());
			AssertEq(tlOut.Item.Value, 142 + 1);
		}
	}

	[ArTest.Test] public async Task TestBin()
	{
		var world = Lvl.World;
		var bin = world.PlaceBin(new Vector2I(3, 4));
		var injy = world.PlaceInjector();

		var tlBin = bin.GetTile(Vector2I.Zero);
		var tlNorth = world.InstallEmptyTile(tlBin.GLocation + Direction.North.ToGrid());
		var tlEast = world.InstallEmptyTile(tlBin.GLocation + Direction.East.ToGrid());
		var tlSouth = world.InstallEmptyTile(tlBin.GLocation + Direction.South.ToGrid());
		var tlWest = world.InstallEmptyTile(tlBin.GLocation + Direction.West.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			// From north
			var it = world.SpawnItem(tlNorth, 575)!;
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.Clock.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());
			
			// From east
			it = world.SpawnItem(tlEast, -241)!;
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.Clock.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());

			// From south
			it = world.SpawnItem(tlSouth, -511)!;
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.Clock.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());

			// From west
			it = world.SpawnItem(tlWest, 628)!;
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.Clock.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Clock.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());
		}
	}

	[ArTest.Test] public async Task TestBranch_vanilla()
	{
		var world = Lvl.World;
		// For now I am only planning on allowing the value zero.
		var bra = world.PlaceBranch(new Vector2I(1, 2), Direction.East, 1, CmdIf.CheckType.Greater, 0);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(bra.GLocation + bra.Dir.Invert().ToGrid());
		var tlTrue = world.InstallEmptyTile(bra.GLocation + bra.Dir.ToGrid());
		var tlFalse = world.InstallEmptyTile(tlTrue.GLocation + new Vector2I(0, 1));

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			// When the check returns true
			var positiveIt = world.SpawnItem(tlIn, 535)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Clock.TickOnceAsync(); // Item slides in.
			AssertFalse(tlIn.HasItem());
			AssertTrue(positiveIt.IsMidAnimation());
			await Lvl.Clock.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			
			await Lvl.Clock.TickOnceAsync(); // Item pops out of the true tile
			AssertFalse(tlFalse.HasItem());
			AssertTrue(tlTrue.HasItem());
			AssertTrue(tlTrue.Item.IsMidAnimation());
			AssertRefEq(tlTrue.Item, positiveIt);
			await Lvl.Clock.EndTickAsync();
			AssertTrue(tlTrue.HasItem());
			AssertFalse(tlTrue.Item.IsMidAnimation());
			tlTrue.DestroyItem();

			// When the check returns false
			var negativeIt = world.SpawnItem(tlIn, -312)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Clock.TickOnceAsync(); // Item slides in.
			AssertFalse(tlIn.HasItem());
			AssertTrue(negativeIt.IsMidAnimation());
			await Lvl.Clock.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			
			await Lvl.Clock.TickOnceAsync(); // Item pops out of the false tile
			AssertFalse(tlTrue.HasItem());
			AssertTrue(tlFalse.HasItem());
			AssertTrue(tlFalse.Item.IsMidAnimation());
			AssertRefEq(tlFalse.Item, negativeIt);
			await Lvl.Clock.EndTickAsync();
			AssertTrue(tlFalse.HasItem());
			AssertFalse(tlFalse.Item.IsMidAnimation());
			tlFalse.DestroyItem();
		}
	}

	[ArTest.Test] public async Task TestBranch_clogged()
	{
		var world = Lvl.World;
		// For now I am only planning on allowing the value zero.
		var bra = world.PlaceBranch(new Vector2I(1, 2), Direction.East, 1, CmdIf.CheckType.Greater, 0);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(bra.GLocation + bra.Dir.Invert().ToGrid());
		var tlTrue = world.InstallEmptyTile(bra.GLocation + bra.Dir.ToGrid());
		var tlFalse = world.InstallEmptyTile(tlTrue.GLocation + new Vector2I(0, 1));

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			// Blocking both output tiles.
			world.SpawnItem(tlTrue, 27);
			world.SpawnItem(tlFalse, 761);

			// When the check returns true
			var positiveIt = world.SpawnItem(tlIn, 535)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Clock.TickOnceAsync(); // Item slides in.
			await Lvl.Clock.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			tlTrue.DestroyItem();
			
			await Lvl.Clock.TickOnceAsync(); // Item pops out of the true tile
			AssertTrue(tlTrue.HasItem());
			AssertTrue(tlTrue.Item.IsMidAnimation());
			AssertRefEq(tlTrue.Item, positiveIt);
			await Lvl.Clock.EndTickAsync();
			AssertFalse(tlTrue.Item.IsMidAnimation());

			// When the check returns false
			var negativeIt = world.SpawnItem(tlIn, -312)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Clock.TickOnceAsync(); // Item slides in.
			await Lvl.Clock.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			await Lvl.Clock.TickOnceAsync(); // Nothing happens
			tlFalse.DestroyItem();
			
			await Lvl.Clock.TickOnceAsync(); // Item pops out of the false tile
			AssertTrue(tlTrue.HasItem()); // From last time, need to make sure it doesn't get deleted somehow.
			AssertTrue(tlFalse.HasItem());
			AssertRefEq(tlFalse.Item, negativeIt);
		}
	}

	[ArTest.Test] public async Task TestDemander_all_values_correct()
	{
		var world = Lvl.World;
		var dem = world.PlaceDemander(new Vector2I(3, 4), Direction.West, [655, 282, -279]);
		var injy = world.PlaceInjector();
		var tlIn = world.InstallEmptyTile(dem.GLocation + dem.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 655);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Item gets consumed
			AssertFalse(Lvl.Clock.IsHalted());
			AssertFalse(dem.Input.HasItem());

			world.SpawnItem(tlIn, 282);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Item gets consumed
			AssertFalse(Lvl.Clock.IsHalted());
			AssertFalse(dem.Input.HasItem());

			world.SpawnItem(tlIn, -279);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Item gets consumed
			AssertTrue(Lvl.Clock.IsHalted()); // Last item, so simulation should succeed.
		}
	}

	[ArTest.Test] public async Task TestDemander_incorrect_value_and_restart()
	{
		var world = Lvl.World;
		var dem = world.PlaceDemander(new Vector2I(3, 4), Direction.West, [-76, -797]);
		var injy = world.PlaceInjector();
		var tlIn = world.InstallEmptyTile(dem.GLocation + dem.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, -76);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Item gets consumed
			AssertFalse(Lvl.Clock.IsHalted());
			AssertFalse(dem.Input.HasItem());

			world.SpawnItem(tlIn, -1);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in (incorrect)
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Simulation fails miserablely
			AssertTrue(Lvl.Clock.IsHalted());
		}

		using (Lvl.Clock.PlaySimulationInDebug()) // Restart
		{
			world.SpawnItem(tlIn, -76);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Item gets consumed
			AssertFalse(Lvl.Clock.IsHalted());
			AssertFalse(dem.Input.HasItem());

			world.SpawnItem(tlIn, -797);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.TickOnceForTestingAsync(); // Item slides in
			AssertFalse(Lvl.Clock.IsHalted());
			AssertTrue(dem.Input.HasItem());

			await Lvl.TickOnceForTestingAsync(); // Item gets consumed
			AssertTrue(Lvl.Clock.IsHalted()); // Last item, so simulation should succeed.
		}
	}

	[ArTest.Test] public async Task TestCombiner_vanilla()
	{
		var world = Lvl.World;
		var combi = world.PlaceCombiner(new Vector2I(2, 4), Direction.East, 2, BinaryOperation.Add);
		var injy = world.PlaceInjector();
		var tl0 = world.InstallEmptyTile(combi.InLeftTile.GLocation + combi.Dir.Invert().ToGrid());
		var tl1 = world.InstallEmptyTile(combi.InRightTile.GLocation + combi.Dir.Invert().ToGrid());
		var tlOut = world.InstallEmptyTile(combi.OutTile.GLocation + combi.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 774);
			world.SpawnItem(tl1, -539);
			injy.PendFullSequence([[
				new CmdSlide(tl0, combi.Dir),
				new CmdSlide(tl1, combi.Dir),
			]]);

			await Lvl.TickOnceForTestingAsync(); // Items slide in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Result slides out
			AssertTrue(tlOut.HasItem());
			AssertEq(tlOut.Item.Value, 774 + -539);
		}
	}

	[ArTest.Test] public async Task TestCombiner_clogged()
	{
		var world = Lvl.World;
		var combi = world.PlaceCombiner(new Vector2I(2, 4), Direction.East, 2, BinaryOperation.Add);
		var injy = world.PlaceInjector();
		var tl0 = world.InstallEmptyTile(combi.InLeftTile.GLocation + combi.Dir.Invert().ToGrid());
		var tl1 = world.InstallEmptyTile(combi.InRightTile.GLocation + combi.Dir.Invert().ToGrid());
		var tlOut = world.InstallEmptyTile(combi.OutTile.GLocation + combi.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 774);
			world.SpawnItem(tl1, -539);
			world.SpawnItem(tlOut, -195); // In the way
			injy.PendFullSequence([[
				new CmdSlide(tl0, combi.Dir),
				new CmdSlide(tl1, combi.Dir),
			]]);

			await Lvl.TickOnceForTestingAsync(); // Items slide in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			tlOut.DestroyItem();
			await Lvl.TickOnceForTestingAsync(); // Result slides out immediately
			AssertTrue(tlOut.HasItem());
			AssertEq(tlOut.Item.Value, 774 + -539);
		}
	}

	[ArTest.Test] public async Task TestCombiner_missing_output_connection()
	{
		var world = Lvl.World;
		var combi = world.PlaceCombiner(new Vector2I(2, 4), Direction.East, 2, BinaryOperation.Add);
		var injy = world.PlaceInjector();
		var tl0 = world.InstallEmptyTile(combi.InLeftTile.GLocation + combi.Dir.Invert().ToGrid());
		var tl1 = world.InstallEmptyTile(combi.InRightTile.GLocation + combi.Dir.Invert().ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tl0, 774);
			world.SpawnItem(tl1, -539);
			injy.PendFullSequence([[
				new CmdSlide(tl0, combi.Dir),
				new CmdSlide(tl1, combi.Dir),
			]]);

			await Lvl.TickOnceForTestingAsync(); // Items slide in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
		}
	}

	[ArTest.Test] public async Task TestCloner_vanilla()
	{
		var world = Lvl.World;
		var clo = world.PlaceCloner(new Vector2I(1, 4), Direction.East, 2);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(clo.InTile.GLocation + clo.Dir.Invert().ToGrid());
		var tlOut0 = world.InstallEmptyTile(clo.OutTile0.GLocation + clo.Dir.ToGrid());
		var tlOut1 = world.InstallEmptyTile(clo.OutTile1.GLocation + clo.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Two items slide out \_(0_0)_/
			AssertTrue(tlOut0.HasItem());
			AssertEq(tlOut0.Item.Value, 508);
			AssertTrue(tlOut1.HasItem());
			AssertEq(tlOut0.Item.Value, 508);
		}
	}

	[ArTest.Test] public async Task TestCloner_clogged()
	{
		var world = Lvl.World;
		var clo = world.PlaceCloner(new Vector2I(1, 4), Direction.East, 2);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(clo.InTile.GLocation + clo.Dir.Invert().ToGrid());
		var tlOut0 = world.InstallEmptyTile(clo.OutTile0.GLocation + clo.Dir.ToGrid());
		var tlOut1 = world.InstallEmptyTile(clo.OutTile1.GLocation + clo.Dir.ToGrid());

		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 508);
			world.SpawnItem(tlOut0, 964); // In the way
			world.SpawnItem(tlOut1, 212); // In the way
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			
			tlOut0.DestroyItem();
			await Lvl.TickOnceForTestingAsync(); // Item slides out of out0
			AssertTrue(tlOut0.HasItem());
			AssertEq(tlOut0.Item.Value, 508);

			tlOut1.DestroyItem();
			await Lvl.TickOnceForTestingAsync(); // Item slides out of out1
			AssertTrue(tlOut1.HasItem());
			AssertEq(tlOut0.Item.Value, 508);
			
			await Lvl.Clock.EndTickAsync();
			tlOut0.DestroyItem();
			tlOut1.DestroyItem();
			world.SpawnItem(tlIn, -900);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // 2 pop out baby
			AssertTrue(tlOut0.HasItem());
			AssertEq(tlOut0.Item.Value, -900);
			AssertTrue(tlOut1.HasItem());
			AssertEq(tlOut0.Item.Value, -900);
		}
	}

	[ArTest.Test] public async Task TestCloner_missing_output_connections()
	{
		var world = Lvl.World;
		var clo = world.PlaceCloner(new Vector2I(1, 4), Direction.East, 2);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(clo.InTile.GLocation + clo.Dir.Invert().ToGrid());
		using (Lvl.Clock.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Clogged, or straight up disabled.
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
			await Lvl.TickOnceForTestingAsync(); // Clogged
		}

		var tlOut1 = world.InstallEmptyTile(clo.OutTile1.GLocation + clo.Dir.ToGrid());
		using (Lvl.Clock.PlaySimulationInDebug()) // One output connected, other one missing
		{
			world.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing should slide out as normal!
			AssertFalse(tlOut1.HasItem());
			await Lvl.TickOnceForTestingAsync(); // Clogged
			AssertFalse(tlOut1.HasItem());
			await Lvl.TickOnceForTestingAsync(); // Clogged
			AssertFalse(tlOut1.HasItem());
		}

		var tlOut0 = world.InstallEmptyTile(clo.OutTile0.GLocation + clo.Dir.ToGrid());
		world.DeleteTile(tlOut1.GLocation);
		using (Lvl.Clock.PlaySimulationInDebug()) // The other output this time.
		{
			world.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.TickOnceForTestingAsync(); // Item slides in
			await Lvl.TickOnceForTestingAsync(); // Nothing happens
			await Lvl.TickOnceForTestingAsync(); // Nothing should slide out as normal!
			AssertFalse(tlOut0.HasItem());
			await Lvl.TickOnceForTestingAsync(); // Clogged
			AssertFalse(tlOut0.HasItem());
			await Lvl.TickOnceForTestingAsync(); // Clogged
			AssertFalse(tlOut0.HasItem());
		}
	}
}
