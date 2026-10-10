using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

[ArTest.RunThisOnly]
public class UnitTests : ArTest.TestSuit
{
	private Level Lvl => m_Lv!;
	private Level? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		m_Lv = levelScene.Instantiate<Level>();
		AddNode(Lvl);
		Lvl.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(6, 6));
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
		AssertFalse(Unit.CanFitIn(world, Vector2I.One, world.Dims, Direction.East)); // Barely clipping

		// 4. Perfect fit
		// As big as the whole world exactly.
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, world.Dims, Direction.East));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, world.Dims, Direction.West));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(world.Dims.Y, world.Dims.X), Direction.North));
		AssertTrue(Unit.CanFitIn(world, Vector2I.Zero, new(world.Dims.Y, world.Dims.X), Direction.South));
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
		using (Lvl.PlaySimulationInDebug())
		{
			await Lvl.TickOnceAsync(); // Tick 0 processed
			AssertFalse(tl1.HasItem());
			await Lvl.TickOnceAsync(); // Tick 1 processed
			AssertFalse(tl1.HasItem());
			await Lvl.TickOnceAsync(); // Tick 2 processed, first item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 237);

			await Lvl.EndTickAsync();
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await Lvl.TickOnceAsync(); // Tick 3 processed
			AssertFalse(tl1.HasItem());
			await Lvl.TickOnceAsync(); // Tick 4 processed
			AssertFalse(tl1.HasItem());
			await Lvl.TickOnceAsync(); // Tick 5 processed, second item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 68);

			await Lvl.EndTickAsync();
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await Lvl.TickOnceAsync(); // Tick 6 processed
			AssertFalse(tl1.HasItem());
			await Lvl.TickOnceAsync(); // Tick 7 processed
			AssertFalse(tl1.HasItem());

			await Lvl.TickOnceAsync(); // Tick 8 processed, third item
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
		var supp = world.PlaceSupplier(Vector2I.One, Direction.South, 1, [261, 296]);
		var tl1 = world.InstallEmptyTile(supp.GLocation + new Vector2I(0, 1));
		AssertTrue(world.HasTile(supp.GLocation));
		using (Lvl.PlaySimulationInDebug())
		{
			await Lvl.TickOnceAsync();
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 261);
		}
	}

	[ArTest.Test] public async Task TestSupplier_clogged()
	{
		var world = Lvl.World;
		world.Reset();
		var supp = world.PlaceSupplier(Vector2I.One, Direction.South, 2, [261, 296]);
		var injy = world.PlaceInjector();
		var tlSupp = world.GetTileNonNull(supp.GLocation);
		var tlNearby = world.InstallEmptyTile(supp.GLocation + new Vector2I(0, 1));
		var tlFar = world.InstallEmptyTile(tlNearby.GridLoc + new Vector2I(0, 1));

		using (Lvl.PlaySimulationInDebug())
		{
			world.SpawnItem(tlNearby, -217); // In the way

			await Lvl.TickOnceAsync(); // Nothing happens
			AssertFalse(tlSupp.HasItem());
			
			await Lvl.TickOnceAsync(); // First item spawns
			AssertTrue(tlSupp.HasItem());
			AssertFalse(tlSupp.Item.IsMidAnimation());
			AssertEq(tlSupp.Item.Value, 261);

			await Lvl.TickOnceAsync(); // Nothing happens

			await Lvl.TickOnceAsync(); // Blocked, and differred to next tick not the next time around.
			AssertTrue(tlSupp.HasItem());
			AssertFalse(tlSupp.Item.IsMidAnimation());
			AssertEq(tlSupp.Item.Value, 261);

			tlNearby.DestroyItem(); // Remove the obsticle
			await Lvl.TickOnceAsync();
			
			// AssertFalse(tlSupp.HasItem());

			AssertTrue(tlNearby.HasItem());
			AssertTrue(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 261);

			await Lvl.EndTickAsync();
			AssertTrue(tlNearby.HasItem());
			AssertFalse(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 261);

			injy.PendParallel([new CmdSlide(tlNearby, Direction.South)]);

			// First item finished sliding out, second item spawns and slides out right after it.
			await Lvl.TickOnceAsync();
			AssertFalse(tlSupp.HasItem());
			AssertTrue(tlNearby.HasItem());
			AssertTrue(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 296);

			AssertTrue(tlFar.HasItem());
			AssertTrue(tlFar.Item.IsMidAnimation());			
			AssertEq(tlFar.Item.Value, 261);

			AssertTrue(supp.IsDoneWithInputSeq());
		}
	}

	[ArTest.Test] public async Task TestSlider_all_directions()
	{
		var world = Lvl.World;
		var slid = world.PlaceSlider(new Vector2I(3, 4), Direction.North);
		var injy = world.PlaceInjector();

		var tlSlid = slid.GetTileNonNull(Vector2I.Zero);
		var tlNorth = world.InstallEmptyTile(tlSlid.GridLoc + Direction.North.ToGrid());
		var tlEast = world.InstallEmptyTile(tlSlid.GridLoc + Direction.East.ToGrid());
		var tlSouth = world.InstallEmptyTile(tlSlid.GridLoc + Direction.South.ToGrid());
		var tlWest = world.InstallEmptyTile(tlSlid.GridLoc + Direction.West.ToGrid());

		using (Lvl.PlaySimulationInDebug())
		{
			// From south
			world.SpawnItem(tlSouth, -511);
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, -511);

			await Lvl.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, -511);

			await Lvl.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From east
			world.SpawnItem(tlEast, 635);
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 635);

			await Lvl.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 635);

			await Lvl.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From west
			world.SpawnItem(tlWest, 999);
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 999);

			await Lvl.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 999);

			await Lvl.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From north (will be stuck forever)
			world.SpawnItem(tlNorth, 638);
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.TickOnceAsync();
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
		var injy = world.PlaceInjector();

		var tlSlid = slid.GetTileNonNull(Vector2I.Zero);
		var tlDest = world.InstallEmptyTile(tlSlid.GridLoc + Direction.West.ToGrid());

		using (Lvl.PlaySimulationInDebug())
		{
			world.SpawnItem(tlSlid, 13);
			await Lvl.TickOnceAsync(); // Should move 2 ticks from now.
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
			await Lvl.TickOnceAsync(); // Next tick
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());

			await Lvl.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 13);

			await Lvl.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.TickOnceAsync();
		
			world.SpawnItem(tlSlid, -804);
			await Lvl.TickOnceAsync(); // Should still move the tick after this one.
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
		
			await Lvl.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, -804);

			await Lvl.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.TickOnceAsync();
			await Lvl.TickOnceAsync();
		
			world.SpawnItem(tlSlid, 982); // Should move next tick right away.
			await Lvl.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 982);
		}
	}

	[ArTest.Test] public async Task TestSlider_no_dest_tile()
	{
		AssertTrue(1 + 1 + 1 == 1);
	}

	[ArTest.Test] public async Task TestUpdater()
	{
		// I am not gonna test all the update types and just provide a gaurantee of translating to UnaryOperation.

		var world = Lvl.World;
		var slid = world.PlaceUpdater(new Vector2I(3, 4), Direction.South, 2, UNUpdater.UpdateType.Increment);
		var injy = world.PlaceInjector();

		var tlUp = slid.GetTileNonNull(Vector2I.Zero);
		var tlIn = world.InstallEmptyTile(tlUp.GridLoc + Direction.North.ToGrid());
		var tlOut = world.InstallEmptyTile(tlUp.GridLoc + Direction.South.ToGrid());

		using (Lvl.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 402);
			injy.PendFullSequence([[ new CmdSlide(tlIn, Direction.South) ]]);

			await Lvl.TickOnceAsync();
			AssertFalse(tlIn.HasItem());
			AssertTrue(tlUp.HasItem());
			AssertEq(tlUp.Item.Value, 402);

			await Lvl.TickOnceAsync(); // Rate is 2 ticks per item, this tick does nothing.
			AssertTrue(tlUp.HasItem());
			AssertFalse(tlUp.Item.IsMidAnimation());
			// I am gonna leave it to the implemetation whether to update the value early or not.

			await Lvl.TickOnceAsync();
			AssertFalse(tlUp.HasItem());
			AssertTrue(tlOut.HasItem());
			AssertEq(tlOut.Item.Value, 402 + 1);
		}
	}
}
