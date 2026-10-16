using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

[ArTest.RunThisOnly]
public class UnitTests : ArTest.TestSuit
{
	private Editor Lvl => m_Lv!;
	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.LevelScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(Lvl);
		Lvl.Simy.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(6, 6));
	}

	public override void AfterEach()
	{
		AssertTrue(Lvl.Simy.IsHalted());
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
		using (Lvl.Simy.PlaySimulationInDebug())
		{
			await Lvl.Simy.TickOnceAsync(); // Tick 0 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Tick 1 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Tick 2 processed, first item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 237);

			await Lvl.Simy.EndTickAsync();
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await Lvl.Simy.TickOnceAsync(); // Tick 3 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Tick 4 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Tick 5 processed, second item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 68);

			await Lvl.Simy.EndTickAsync();
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await Lvl.Simy.TickOnceAsync(); // Tick 6 processed
			AssertFalse(tl1.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Tick 7 processed
			AssertFalse(tl1.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Tick 8 processed, third item
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
		using (Lvl.Simy.PlaySimulationInDebug())
		{
			await Lvl.Simy.TickOnceAsync();
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

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			world.SpawnItem(tlNearby, -217); // In the way

			await Lvl.Simy.TickOnceAsync(); // Nothing should happen, but we have no way of checking
			await Lvl.Simy.TickOnceAsync(); // First might have spawned, but not checkable
			await Lvl.Simy.TickOnceAsync(); // Nothing should happen
			await Lvl.Simy.TickOnceAsync(); // Blocked.

			tlNearby.DestroyItem(); // Remove the obsticle
			await Lvl.Simy.TickOnceAsync(); // First item slides out first.
			AssertTrue(tlNearby.HasItem());
			AssertTrue(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 261);

			await Lvl.Simy.EndTickAsync();
			AssertTrue(tlNearby.HasItem());
			AssertFalse(tlNearby.Item.IsMidAnimation());
			AssertEq(tlNearby.Item.Value, 261);

			injy.PendParallel([new CmdSlide(tlNearby, supp.Dir)]);

			// First item finished sliding out, second item spawns and slides out right after it.
			await Lvl.Simy.TickOnceAsync();
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

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Item would slide out if the supplier was connected.
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged

			var tl = world.InstallEmptyTile(tlSupp.GLocation + supp.Dir.ToGrid());
			await Lvl.Simy.TickOnceAsync(); // First item slides out
			AssertTrue(tl.HasItem());
			AssertEq(tl.Item.Value, 854);
			await Lvl.Simy.EndTickAsync();
			world.DeleteTile(tl.GLocation);

			await Lvl.Simy.TickOnceAsync(); // Next item would slide out if I didn't delete the tile.
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged
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

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			// From south
			world.SpawnItem(tlSouth, -511);
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, -511);

			await Lvl.Simy.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, -511);

			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From east
			world.SpawnItem(tlEast, 635);
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 635);

			await Lvl.Simy.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 635);

			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From west
			world.SpawnItem(tlWest, 999);
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertTrue(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 999);

			await Lvl.Simy.TickOnceAsync();
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 999);

			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From north (will be stuck forever)
			world.SpawnItem(tlNorth, 638);
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.Simy.TickOnceAsync();
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

		var tlSlid = slid.GetTile(Vector2I.Zero);
		var tlDest = world.InstallEmptyTile(tlSlid.GLocation + Direction.West.ToGrid());

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			world.SpawnItem(tlSlid, 13);
			await Lvl.Simy.TickOnceAsync(); // Should move 2 ticks from now.
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
			await Lvl.Simy.TickOnceAsync(); // Next tick
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());

			await Lvl.Simy.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 13);

			await Lvl.Simy.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.Simy.TickOnceAsync();
		
			world.SpawnItem(tlSlid, -804);
			await Lvl.Simy.TickOnceAsync(); // Should still move the tick after this one.
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
		
			await Lvl.Simy.TickOnceAsync(); // Moves
			AssertFalse(tlSlid.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, -804);

			await Lvl.Simy.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.Simy.TickOnceAsync();
			await Lvl.Simy.TickOnceAsync();
		
			world.SpawnItem(tlSlid, 982); // Should move next tick right away.
			await Lvl.Simy.TickOnceAsync(); // Moves
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
		var injy = world.PlaceInjector();

		var tlSlid = slid.GetTile(Vector2I.Zero);
		var tlDest = world.InstallEmptyTile(tlSlid.GLocation + Direction.West.ToGrid());

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			world.SpawnItem(tlSlid, 261);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(tlSlid.HasItem());
			AssertFalse(tlSlid.Item.IsMidAnimation());
			AssertEq(tlSlid.Item.Value, 261);
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

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 402);
			injy.PendFullSequence([[ new CmdSlide(tlIn, upd.Dir) ]]);

			await Lvl.Simy.TickOnceAsync(); // Slides in and updater starts counting ticks.
			AssertFalse(tlOut.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Rate is 2 ticks per item, this tick does nothing.
			AssertFalse(tlOut.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Updated item pops out.
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

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			world.SpawnItem(tlIn, 142);
			injy.PendFullSequence([[ new CmdSlide(tlIn, upd.Dir) ]]);
			
			await Lvl.Simy.TickOnceAsync(); // Slides in and updater starts counting ticks.
			await Lvl.Simy.TickOnceAsync(); // 1
			await Lvl.Simy.TickOnceAsync(); // 2, would slide out if there was a way.
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged

			var tlOut = world.InstallEmptyTile(upd.Output.GLocation + upd.Dir.ToGrid());
			await Lvl.Simy.TickOnceAsync(); // Item slides out updated
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

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			// From north
			var it = world.SpawnItem(tlNorth, 575)!;
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());
			
			// From east
			it = world.SpawnItem(tlEast, -241)!;
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());

			// From south
			it = world.SpawnItem(tlSouth, -511)!;
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());

			// From west
			it = world.SpawnItem(tlWest, 628)!;
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(tlBin.IsReserved());
		}
	}

	[ArTest.Test] public async Task TestBranch_vanilla()
	{
		var world = Lvl.World;
		// For now I am only planning on allowing the value zero.
		var bra = world.PlaceBranch(new Vector2I(1, 2), Direction.East, CmdIf.CheckType.Greater, 0);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(bra.GLocation + bra.Dir.Invert().ToGrid());
		var tlTrue = world.InstallEmptyTile(bra.GLocation + bra.Dir.ToGrid());
		var tlFalse = world.InstallEmptyTile(tlTrue.GLocation + new Vector2I(0, 1));

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			// When the check returns true
			var positiveIt = world.SpawnItem(tlIn, 535)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in.
			AssertFalse(tlIn.HasItem());
			AssertTrue(positiveIt.IsMidAnimation());
			await Lvl.Simy.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			
			await Lvl.Simy.TickOnceAsync(); // Item pops out of the true tile
			AssertFalse(tlFalse.HasItem());
			AssertTrue(tlTrue.HasItem());
			AssertTrue(tlTrue.Item.IsMidAnimation());
			AssertRefEq(tlTrue.Item, positiveIt);
			await Lvl.Simy.EndTickAsync();
			AssertTrue(tlTrue.HasItem());
			AssertFalse(tlTrue.Item.IsMidAnimation());
			tlTrue.DestroyItem();

			// When the check returns false
			var negativeIt = world.SpawnItem(tlIn, -312)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in.
			AssertFalse(tlIn.HasItem());
			AssertTrue(negativeIt.IsMidAnimation());
			await Lvl.Simy.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			
			await Lvl.Simy.TickOnceAsync(); // Item pops out of the false tile
			AssertFalse(tlTrue.HasItem());
			AssertTrue(tlFalse.HasItem());
			AssertTrue(tlFalse.Item.IsMidAnimation());
			AssertRefEq(tlFalse.Item, negativeIt);
			await Lvl.Simy.EndTickAsync();
			AssertTrue(tlFalse.HasItem());
			AssertFalse(tlFalse.Item.IsMidAnimation());
			tlFalse.DestroyItem();
		}
	}

	[ArTest.Test] public async Task TestBranch_clogged()
	{
		var world = Lvl.World;
		// For now I am only planning on allowing the value zero.
		var bra = world.PlaceBranch(new Vector2I(1, 2), Direction.East, CmdIf.CheckType.Greater, 0);
		var injy = world.PlaceInjector();

		var tlIn = world.InstallEmptyTile(bra.GLocation + bra.Dir.Invert().ToGrid());
		var tlTrue = world.InstallEmptyTile(bra.GLocation + bra.Dir.ToGrid());
		var tlFalse = world.InstallEmptyTile(tlTrue.GLocation + new Vector2I(0, 1));

		using (Lvl.Simy.PlaySimulationInDebug())
		{
			// Blocking both output tiles.
			world.SpawnItem(tlTrue, 27);
			world.SpawnItem(tlFalse, 761);

			// When the check returns true
			var positiveIt = world.SpawnItem(tlIn, 535)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in.
			await Lvl.Simy.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			tlTrue.DestroyItem();
			
			await Lvl.Simy.TickOnceAsync(); // Item pops out of the true tile
			AssertTrue(tlTrue.HasItem());
			AssertTrue(tlTrue.Item.IsMidAnimation());
			AssertRefEq(tlTrue.Item, positiveIt);
			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlTrue.Item.IsMidAnimation());

			// When the check returns false
			var negativeIt = world.SpawnItem(tlIn, -312)!;
			injy.PendFullSequence([[ new CmdSlide(tlIn, bra.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in.
			await Lvl.Simy.EndTickAsync();
			AssertFalse(injy.HasPendingCmds());
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			tlFalse.DestroyItem();
			
			await Lvl.Simy.TickOnceAsync(); // Item pops out of the false tile
			AssertTrue(tlTrue.HasItem()); // From last time, need to make sure it doesn't get deleted somehow.
			AssertTrue(tlFalse.HasItem());
			AssertRefEq(tlFalse.Item, negativeIt);
		}
	}
}
