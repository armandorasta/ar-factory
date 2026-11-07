using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class UnitTests : ArTest.TestSuit
{
	public Editor Lvl => m_Lv!;
	public WorldPanel World => Lvl.World;

	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.EditorScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(Lvl);
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

	[ArTest.Test] public void TestCanFitIn()
	{		
		// 1. Empty World.
		// One block.
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.North));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.East));	
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.South));	
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.West));
		// 2x1
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(2, 1), Direction.North));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(2, 1), Direction.East));	
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(2, 1), Direction.South));	
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(2, 1), Direction.West));
		// 4x4
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(4, 4), Direction.North));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(4, 4), Direction.East));	
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(4, 4), Direction.South));	
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(4, 4), Direction.West));

		// 2. Something in the way
		World.AddEmptyTile(Vector2I.One);
		// One block.
		AssertFalse(Unit.CanFitIn(World, Vector2I.One, Vector2I.One, Direction.North));
		AssertFalse(Unit.CanFitIn(World, Vector2I.One, Vector2I.One, Direction.East));	
		AssertFalse(Unit.CanFitIn(World, Vector2I.One, Vector2I.One, Direction.South));	
		AssertFalse(Unit.CanFitIn(World, Vector2I.One, Vector2I.One, Direction.West));
		// 2x1
		AssertFalse(Unit.CanFitIn(World, new(1, 0), new(2, 1), Direction.North));
		AssertFalse(Unit.CanFitIn(World, new(0, 1), new(2, 1), Direction.East));	
		AssertFalse(Unit.CanFitIn(World, new(1, 0), new(2, 1), Direction.South));	
		AssertFalse(Unit.CanFitIn(World, new(0, 1), new(2, 1), Direction.West));
		// 4x4
		AssertFalse(Unit.CanFitIn(World, new(0, 0), new(4, 4), Direction.North));
		AssertFalse(Unit.CanFitIn(World, new(1, 0), new(4, 4), Direction.East));	
		AssertFalse(Unit.CanFitIn(World, new(0, 1), new(4, 4), Direction.South));	
		AssertFalse(Unit.CanFitIn(World, new(1, 1), new(4, 4), Direction.West));
		World.DeleteTile(Vector2I.One);

		// 3. Out of bounds
		// Placing out of bounds
		AssertFalse(Unit.CanFitIn(World, 1000 * Vector2I.One, new(3, 1), Direction.North));
		AssertFalse(Unit.CanFitIn(World, 1000 * Vector2I.One, new(3, 1), Direction.East));
		AssertFalse(Unit.CanFitIn(World, 1000 * Vector2I.One, new(3, 1), Direction.South));
		AssertFalse(Unit.CanFitIn(World, 1000 * Vector2I.One, new(3, 1), Direction.West));
		// Negative
		AssertFalse(Unit.CanFitIn(World, -1   * Vector2I.One, new(3, 1), Direction.North));
		AssertFalse(Unit.CanFitIn(World, -1   * Vector2I.One, new(3, 1), Direction.East));
		AssertFalse(Unit.CanFitIn(World, -1   * Vector2I.One, new(3, 1), Direction.South));
		AssertFalse(Unit.CanFitIn(World, -1   * Vector2I.One, new(3, 1), Direction.West));
		// Clipping out of bounds
		AssertFalse(Unit.CanFitIn(World, Vector2I.Zero, new(1000, 1), Direction.East));
		AssertFalse(Unit.CanFitIn(World, Vector2I.Zero, new(1, 1000), Direction.North));
		AssertFalse(Unit.CanFitIn(World, Vector2I.Zero, new(1000, 1000), Direction.South));
		AssertFalse(Unit.CanFitIn(World, Vector2I.One, World.Dimensions, Direction.East)); // Barely clipping

		// 4. Perfect fit
		// As big as the whole world exactly.
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, World.Dimensions, Direction.East));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, World.Dimensions, Direction.West));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(World.Dimensions.Y, World.Dimensions.X), Direction.North));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(World.Dimensions.Y, World.Dimensions.X), Direction.South));
		// 1x1
		World.AddEmptyTile(new Vector2I(1, 0));
		World.AddEmptyTile(new Vector2I(0, 1));
		World.AddEmptyTile(new Vector2I(1, 1));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.North));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.East));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.South));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, Vector2I.One, Direction.West));
		// 2x1 or 1x2.
		World.DeleteTile(new Vector2I(1, 0));
		World.AddEmptyTile(new Vector2I(2, 0));
		World.AddEmptyTile(new Vector2I(2, 1));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(2, 1), Direction.East));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(2, 1), Direction.West));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(1, 2), Direction.North));
		AssertTrue(Unit.CanFitIn(World, Vector2I.Zero, new(1, 2), Direction.South));
	}

	[ArTest.Test] public async Task TestPlatform_vanilla()
	{
		var platLeft = World.AddEmptyTile(new Vector2I(2, 4));
		var platRight = World.AddEmptyTile(new Vector2I(3, 4));
		var injy = Lvl.UniMan.PlaceInjector();

		using (Lvl.Simy.StartAndPause())
		{
			injy.PendFullSequence([[ new CmdSpawn(platLeft, 9) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(platLeft.HasItem());
			AssertEq(platLeft.Item.Value, 9);
			
			injy.PendFullSequence([[ new CmdSlide(platLeft, Direction.East) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertFalse(platLeft.HasItem());
			AssertTrue(platRight.HasItem());
			AssertEq(platRight.Item.Value, 9);

			injy.PendFullSequence([[ new CmdSlide(platRight, Direction.West) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(platLeft.HasItem());
			AssertFalse(platRight.HasItem());
			AssertEq(platLeft.Item.Value, 9);
		}
	}

	[ArTest.Test] public async Task TestSupplier_tick_rate()
	{
		var supp = Lvl.UniMan.PlaceSupplier(Vector2I.Zero, Direction.East, 3, [237, 68, 236]);
		var tl1 = World.AddEmptyTile(new Vector2I(1, 0));
		AssertTrue(World.HasTileOn(supp.GLocation));
		using (Lvl.Simy.StartAndPause())
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
		World.Clear();
		var supp = Lvl.UniMan.PlaceSupplier(Vector2I.One, Direction.East, 1, [261, 296]);
		var tl1 = World.AddEmptyTile(supp.GLocation + supp.Dir.ToGrid());
		AssertTrue(World.HasTileOn(supp.GLocation));
		
		using (Lvl.Simy.StartAndPause())
		{
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 261);
		}
	}

	[ArTest.Test] public async Task TestSupplier_clogged()
	{
		var supp = Lvl.UniMan.PlaceSupplier(Vector2I.One, Direction.East, 2, [261, 296]);
		var injy = Lvl.UniMan.PlaceInjector();
		var tlNearby = World.AddEmptyTile(supp.GLocation + supp.Dir.ToGrid());
		var tlFar = World.AddEmptyTile(supp.GLocation + 2 * supp.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlNearby, -217); // In the way

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
		var supp = Lvl.UniMan.PlaceSupplier(Vector2I.One, Direction.East, 2, [854, 743, -374]);
		var tlSupp = supp.GetTile(Vector2I.Zero);

		using (Lvl.Simy.StartAndPause())
		{
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Item would slide out if the supplier was connected.
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged

			/* PLACING NEW TILES MID-SIMULATION IS NOT SUPPORTED FOR NOW

			var plat = Lvl.UniMan.PlacePlatform(tlSupp.GLocation + supp.Dir.ToGrid());
			await Lvl.Simy.TickOnceAsync(); // First item slides out
			AssertTrue(plat.FreeTile.HasItem());
			AssertEq(plat.FreeTile.Item.Value, 854);
			await Lvl.Simy.EndTickAsync();
			World.DeleteUnit(plat);

			await Lvl.Simy.TickOnceAsync(); // Next item would slide out if I didn't delete the tile.
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged
			await Lvl.Simy.TickOnceAsync(); // clogged
			
			*/
		}
	}

	[ArTest.Test] public async Task TestSupplier_restart()
	{
		var supp = Lvl.UniMan.PlaceSupplier(Vector2I.One, Direction.East, 2, [854, 743, -374]);
		var tlNearby = World.AddEmptyTile(supp.GLocation + supp.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // First item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 854);
		}

		using (Lvl.Simy.StartAndPause()) // Restart
		{
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // First item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 854);
			await Lvl.Simy.EndTickAsync();
			tlNearby.DestroyItem();
			
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Second item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 743);
		}

		using (Lvl.Simy.StartAndPause()) // Restart
		{
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // First item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 854);
			await Lvl.Simy.EndTickAsync();
			tlNearby.DestroyItem();

			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Second item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, 743);
			await Lvl.Simy.EndTickAsync();
			tlNearby.DestroyItem();

			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Second item slides out
			AssertTrue(tlNearby.HasItem());
			AssertEq(tlNearby.Item.Value, -374);
		}
	}

	[ArTest.Test] public async Task TestSlider_all_directions()
	{
		var slid = Lvl.UniMan.PlaceSlider(new Vector2I(3, 4), Direction.North);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlNorth = World.AddEmptyTile(slid.OnlyTile.GLocation + Direction.North.ToGrid());
		var tlEast = World.AddEmptyTile(slid.OnlyTile.GLocation + Direction.East.ToGrid());
		var tlSouth = World.AddEmptyTile(slid.OnlyTile.GLocation + Direction.South.ToGrid());
		var tlWest = World.AddEmptyTile(slid.OnlyTile.GLocation + Direction.West.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			// From south
			World.SpawnItem(tlSouth, -511);
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(slid.OnlyTile.HasItem());
			AssertTrue(slid.OnlyTile.Item.IsMidAnimation());
			AssertEq(slid.OnlyTile.Item.Value, -511);

			await Lvl.Simy.TickOnceAsync();
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, -511);

			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From east
			World.SpawnItem(tlEast, 635);
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(slid.OnlyTile.HasItem());
			AssertTrue(slid.OnlyTile.Item.IsMidAnimation());
			AssertEq(slid.OnlyTile.Item.Value, 635);

			await Lvl.Simy.TickOnceAsync();
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 635);

			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From west
			World.SpawnItem(tlWest, 999);
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(slid.OnlyTile.HasItem());
			AssertTrue(slid.OnlyTile.Item.IsMidAnimation());
			AssertEq(slid.OnlyTile.Item.Value, 999);

			await Lvl.Simy.TickOnceAsync();
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertTrue(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 999);

			await Lvl.Simy.EndTickAsync();
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertFalse(injy.HasPendingCmds());
			tlNorth.DestroyItem();

			// From north (will be stuck forever)
			World.SpawnItem(tlNorth, 638);
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.Simy.TickOnceAsync();
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlNorth.HasItem());
			AssertFalse(tlNorth.Item.IsMidAnimation());
			AssertEq(tlNorth.Item.Value, 638);
			AssertTrue(injy.HasPendingCmds()); // Stuck forever...
		}
	}

	[ArTest.Test] public async Task TestSlider_tick_sync()
	{
		var slid = Lvl.UniMan.PlaceSlider(new Vector2I(3, 4), Direction.West, 3);

		var tlDest = World.AddEmptyTile(slid.OnlyTile.GLocation + Direction.West.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(slid.OnlyTile, 13);
			await Lvl.Simy.TickOnceAsync(); // Should move 2 ticks from now.
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(slid.OnlyTile.Item.IsMidAnimation());
			await Lvl.Simy.TickOnceAsync(); // Next tick
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(slid.OnlyTile.Item.IsMidAnimation());

			await Lvl.Simy.TickOnceAsync(); // Moves
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 13);

			await Lvl.Simy.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.Simy.TickOnceAsync();
		
			World.SpawnItem(slid.OnlyTile, -804);
			await Lvl.Simy.TickOnceAsync(); // Should still move the tick after this one.
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(slid.OnlyTile.Item.IsMidAnimation());
		
			await Lvl.Simy.TickOnceAsync(); // Moves
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, -804);

			await Lvl.Simy.EndTickAsync();
			tlDest.DestroyItem();

			await Lvl.Simy.TickOnceAsync();
			await Lvl.Simy.TickOnceAsync();
		
			World.SpawnItem(slid.OnlyTile, 982); // Should move next tick right away.
			await Lvl.Simy.TickOnceAsync(); // Moves
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlDest.HasItem());
			AssertTrue(tlDest.Item.IsMidAnimation());
			AssertEq(tlDest.Item.Value, 982);
		}
	}

	[ArTest.Test] public async Task TestSlider_no_dest_tile()
	{
		var slid = Lvl.UniMan.PlaceSlider(new Vector2I(3, 4), Direction.West, 3);

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(slid.OnlyTile, 261);
			await Lvl.Simy.TickOnceAsync();
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(slid.OnlyTile.Item.IsMidAnimation());
			AssertEq(slid.OnlyTile.Item.Value, 261);
		}
	}

	[ArTest.Test] public async Task TestLocking_using_slider()
	{
		// Semantics for the time being:
		// - Locked units still allow items in.
		// - Locking a unit will reset the counter, so it acts like a reset kind of. I made it this way because of the
		//   on-demand units, and all their complications, solved by just adding a `m_Count = 0`.

		var slid = Lvl.UniMan.PlaceSlider(new Vector2I(2, 1), Direction.East, 2);
		var injy = Lvl.UniMan.PlaceInjector();
		
		var tlIn = World.AddEmptyTile(slid.GLocation + slid.Dir.Invert().ToGrid());
		var tlOut = World.AddEmptyTile(slid.GLocation + slid.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, -103);
			injy.PendFullSequence([[ new CmdSlide(tlIn, slid.Dir) ]]);
			slid.Lock(); // Should not crash
			AssertTrue(slid.IsLocked());
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // 1
			await Lvl.Simy.TickOnceAsync(); // 2, here with locking the item would slide out, but it shouldn't
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(tlOut.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Locked
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(tlOut.HasItem());
			await Lvl.Simy.TickOnceAsync(); // Locked
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(tlOut.HasItem());

			slid.Unlock();
			AssertFalse(slid.IsLocked());
			await Lvl.Simy.TickOnceAsync(); // 1 (counter resets when locking)
			AssertTrue(slid.OnlyTile.HasItem());
			AssertFalse(tlOut.HasItem());
			await Lvl.Simy.TickOnceAsync(); // 2, item slides out yay!
			AssertFalse(slid.OnlyTile.HasItem());
			AssertTrue(tlOut.HasItem());
		}
	}

	[ArTest.Test] public async Task TestUpdater()
	{
		// I am not gonna test all the update types and just provide a gaurantee of translating to UnaryOperation.

		var upd = Lvl.UniMan.PlaceUpdater(new Vector2I(3, 4), Direction.East, 2, UNUpdater.UpdateType.Increment);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(upd.Input.GLocation + upd.Dir.Invert().ToGrid());
		var tlOut = World.AddEmptyTile(upd.Output.GLocation + upd.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, 402);
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
		var upd = Lvl.UniMan.PlaceUpdater(new Vector2I(3, 4), Direction.East, 2, UNUpdater.UpdateType.Increment);
		var injy = Lvl.UniMan.PlaceInjector();
		var tlIn = World.AddEmptyTile(upd.Output.GLocation + upd.Dir.Invert().ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, 142);
			injy.PendFullSequence([[ new CmdSlide(tlIn, upd.Dir) ]]);
			
			await Lvl.Simy.TickOnceAsync(); // Slides in and updater starts counting ticks.
			await Lvl.Simy.TickOnceAsync(); // 1
			await Lvl.Simy.TickOnceAsync(); // 2, would slide out if there was a way.
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged

			var tlOut = World.AddEmptyTile(upd.Output.GLocation + upd.Dir.ToGrid());
			await Lvl.Simy.TickOnceAsync(); // Item slides out updated
			AssertTrue(tlOut.HasItem());
			AssertTrue(tlOut.Item.IsMidAnimation());
			AssertEq(tlOut.Item.Value, 142 + 1);
		}
	}

	[ArTest.Test] public async Task TestBin()
	{
		var bin = Lvl.UniMan.PlaceBin(new Vector2I(3, 4));
		var injy = Lvl.UniMan.PlaceInjector();

		var tlNorth = World.AddEmptyTile(bin.OnlyTile.GLocation + Direction.North.ToGrid());
		var tlEast = World.AddEmptyTile(bin.OnlyTile.GLocation + Direction.East.ToGrid());
		var tlSouth = World.AddEmptyTile(bin.OnlyTile.GLocation + Direction.South.ToGrid());
		var tlWest = World.AddEmptyTile(bin.OnlyTile.GLocation + Direction.West.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			// From north
			var it = World.SpawnItem(tlNorth, 575)!;
			injy.PendFullSequence([[ new CmdSlide(tlNorth, Direction.South) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(bin.OnlyTile.IsReserved());
			
			// From east
			it = World.SpawnItem(tlEast, -241)!;
			injy.PendFullSequence([[ new CmdSlide(tlEast, Direction.West) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(bin.OnlyTile.IsReserved());

			// From south
			it = World.SpawnItem(tlSouth, -511)!;
			injy.PendFullSequence([[ new CmdSlide(tlSouth, Direction.North) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(bin.OnlyTile.IsReserved());

			// From west
			it = World.SpawnItem(tlWest, 628)!;
			injy.PendFullSequence([[ new CmdSlide(tlWest, Direction.East) ]]);
			await Lvl.Simy.TickOnceAsync(); // Slides in
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.EndTickAsync();
			AssertTrue(GodotObject.IsInstanceValid(it));
			await Lvl.Simy.TickOnceAsync(); // Gets bombed
			AssertTrue(it.IsQueuedForDeletion());
			AssertFalse(bin.OnlyTile.IsReserved());
		}
	}

	[ArTest.Test] public async Task TestBranch_vanilla()
	{
		// For now I am only planning on allowing the value zero.
		var bra = Lvl.UniMan.PlaceBranch(new Vector2I(1, 2), Direction.East, 1, CmdIf.CheckType.Greater, 0);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(bra.InTile.GLocation + bra.Dir.Invert().ToGrid());
		var tlTrue = World.AddEmptyTile(bra.OutTrueTile.GLocation + bra.Dir.ToGrid());
		var tlFalse = World.AddEmptyTile(bra.OutFalseTile.GLocation + bra.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			// When the check returns true
			var positiveIt = World.SpawnItem(tlIn, 535)!;
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
			var negativeIt = World.SpawnItem(tlIn, -312)!;
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
		// For now I am only planning on allowing the value zero.
		var bra = Lvl.UniMan.PlaceBranch(new Vector2I(1, 2), Direction.East, 1, CmdIf.CheckType.Greater, 0);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(bra.InTile.GLocation + bra.Dir.Invert().ToGrid());
		var tlTrue = World.AddEmptyTile(bra.OutTrueTile.GLocation + bra.Dir.ToGrid());
		var tlFalse = World.AddEmptyTile(bra.OutFalseTile.GLocation + bra.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			// Blocking both output tiles.
			World.SpawnItem(tlTrue, 27);
			World.SpawnItem(tlFalse, 761);

			// When the check returns true
			var positiveIt = World.SpawnItem(tlIn, 535)!;
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
			var negativeIt = World.SpawnItem(tlIn, -312)!;
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

	[ArTest.Test] public async Task TestDemander_all_values_correct()
	{
		var dem = Lvl.UniMan.PlaceDemander(new Vector2I(3, 4), Direction.West, [655, 282, -279]);
		var injy = Lvl.UniMan.PlaceInjector();
		var tlIn = World.AddEmptyTile(dem.InTile.GLocation + dem.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, 655);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Item gets consumed
			AssertFalse(Lvl.Simy.IsHalted());
			AssertFalse(dem.InTile.HasItem());

			World.SpawnItem(tlIn, 282);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Item gets consumed
			AssertFalse(Lvl.Simy.IsHalted());
			AssertFalse(dem.InTile.HasItem());

			World.SpawnItem(tlIn, -279);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Item gets consumed
			AssertTrue(Lvl.Simy.IsHalted()); // Last item, so simulation should succeed.
		}
	}

	[ArTest.Test] public async Task TestDemander_incorrect_value_and_restart()
	{
		var dem = Lvl.UniMan.PlaceDemander(new Vector2I(3, 4), Direction.West, [-76, -797]);
		var injy = Lvl.UniMan.PlaceInjector();
		var tlIn = World.AddEmptyTile(dem.InTile.GLocation + dem.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, -76);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Item gets consumed
			AssertFalse(Lvl.Simy.IsHalted());
			AssertFalse(dem.InTile.HasItem());

			World.SpawnItem(tlIn, -1);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in (incorrect)
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Simulation fails miserablely
			AssertTrue(Lvl.Simy.IsHalted());
		}

		using (Lvl.Simy.StartAndPause()) // Restart
		{
			World.SpawnItem(tlIn, -76);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Item gets consumed
			AssertFalse(Lvl.Simy.IsHalted());
			AssertFalse(dem.InTile.HasItem());

			World.SpawnItem(tlIn, -797);
			injy.PendFullSequence([[ new CmdSlide(tlIn, dem.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			AssertFalse(Lvl.Simy.IsHalted());
			AssertTrue(dem.InTile.HasItem());

			await Lvl.Simy.TickOnceAsync(); // Item gets consumed
			AssertTrue(Lvl.Simy.IsHalted()); // Last item, so simulation should succeed.
		}
	}

	[ArTest.Test] public async Task TestCombiner_vanilla()
	{
		var combi = Lvl.UniMan.PlaceCombiner(new Vector2I(2, 4), Direction.East, 2, BinaryOperation.Add);
		var injy = Lvl.UniMan.PlaceInjector();
		var tl0 = World.AddEmptyTile(combi.InLeftTile.GLocation + combi.Dir.Invert().ToGrid());
		var tl1 = World.AddEmptyTile(combi.InRightTile.GLocation + combi.Dir.Invert().ToGrid());
		var tlOut = World.AddEmptyTile(combi.OutTile.GLocation + combi.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tl0, 774);
			World.SpawnItem(tl1, -539);
			injy.PendFullSequence([[
				new CmdSlide(tl0, combi.Dir),
				new CmdSlide(tl1, combi.Dir),
			]]);

			await Lvl.Simy.TickOnceAsync(); // Items slide in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Result slides out
			AssertTrue(tlOut.HasItem());
			AssertEq(tlOut.Item.Value, 774 + -539);
		}
	}

	[ArTest.Test] public async Task TestCombiner_clogged()
	{
		var combi = Lvl.UniMan.PlaceCombiner(new Vector2I(2, 4), Direction.East, 2, BinaryOperation.Add);
		var injy = Lvl.UniMan.PlaceInjector();
		var tl0 = World.AddEmptyTile(combi.InLeftTile.GLocation + combi.Dir.Invert().ToGrid());
		var tl1 = World.AddEmptyTile(combi.InRightTile.GLocation + combi.Dir.Invert().ToGrid());
		var tlOut = World.AddEmptyTile(combi.OutTile.GLocation + combi.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tl0, 774);
			World.SpawnItem(tl1, -539);
			World.SpawnItem(tlOut, -195); // In the way
			injy.PendFullSequence([[
				new CmdSlide(tl0, combi.Dir),
				new CmdSlide(tl1, combi.Dir),
			]]);

			await Lvl.Simy.TickOnceAsync(); // Items slide in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			tlOut.DestroyItem();
			await Lvl.Simy.TickOnceAsync(); // Result slides out immediately
			AssertTrue(tlOut.HasItem());
			AssertEq(tlOut.Item.Value, 774 + -539);
		}
	}

	[ArTest.Test] public async Task TestCombiner_missing_output_connection()
	{
		var combi = Lvl.UniMan.PlaceCombiner(new Vector2I(2, 4), Direction.East, 2, BinaryOperation.Add);
		var injy = Lvl.UniMan.PlaceInjector();
		var tl0 = World.AddEmptyTile(combi.InLeftTile.GLocation + combi.Dir.Invert().ToGrid());
		var tl1 = World.AddEmptyTile(combi.InRightTile.GLocation + combi.Dir.Invert().ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tl0, 774);
			World.SpawnItem(tl1, -539);
			injy.PendFullSequence([[
				new CmdSlide(tl0, combi.Dir),
				new CmdSlide(tl1, combi.Dir),
			]]);

			await Lvl.Simy.TickOnceAsync(); // Items slide in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
		}
	}

	[ArTest.Test] public async Task TestCloner_vanilla()
	{
		var clo = Lvl.UniMan.PlaceCloner(new Vector2I(1, 4), Direction.East, 2);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(clo.InTile.GLocation + clo.Dir.Invert().ToGrid());
		var tlOut0 = World.AddEmptyTile(clo.OutTile0.GLocation + clo.Dir.ToGrid());
		var tlOut1 = World.AddEmptyTile(clo.OutTile1.GLocation + clo.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Two items slide out \_(0_0)_/
			AssertTrue(tlOut0.HasItem());
			AssertEq(tlOut0.Item.Value, 508);
			AssertTrue(tlOut1.HasItem());
			AssertEq(tlOut0.Item.Value, 508);
		}
	}

	[ArTest.Test] public async Task TestCloner_clogged()
	{
		var clo = Lvl.UniMan.PlaceCloner(new Vector2I(1, 4), Direction.East, 2);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(clo.InTile.GLocation + clo.Dir.Invert().ToGrid());
		var tlOut0 = World.AddEmptyTile(clo.OutTile0.GLocation + clo.Dir.ToGrid());
		var tlOut1 = World.AddEmptyTile(clo.OutTile1.GLocation + clo.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, 508);
			World.SpawnItem(tlOut0, 964); // In the way
			World.SpawnItem(tlOut1, 212); // In the way
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			
			tlOut0.DestroyItem();
			await Lvl.Simy.TickOnceAsync(); // Item slides out of out0
			AssertTrue(tlOut0.HasItem());
			AssertEq(tlOut0.Item.Value, 508);

			tlOut1.DestroyItem();
			await Lvl.Simy.TickOnceAsync(); // Item slides out of out1
			AssertTrue(tlOut1.HasItem());
			AssertEq(tlOut0.Item.Value, 508);
			
			await Lvl.Simy.EndTickAsync();
			tlOut0.DestroyItem();
			tlOut1.DestroyItem();
			World.SpawnItem(tlIn, -900);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // 2 pop out baby
			AssertTrue(tlOut0.HasItem());
			AssertEq(tlOut0.Item.Value, -900);
			AssertTrue(tlOut1.HasItem());
			AssertEq(tlOut0.Item.Value, -900);
		}
	}

	[ArTest.Test] public async Task TestCloner_missing_output_connections()
	{
		var clo = Lvl.UniMan.PlaceCloner(new Vector2I(1, 4), Direction.East, 2);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(clo.InTile.GLocation + clo.Dir.Invert().ToGrid());
		using (Lvl.Simy.StartAndPause())
		{
			World.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Clogged, or straight up disabled.
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
		}

		var tlOut1 = World.AddEmptyTile(clo.OutTile1.GLocation + clo.Dir.ToGrid());
		using (Lvl.Simy.StartAndPause()) // One output connected, other one missing
		{
			World.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Should slide out anyway
			AssertTrue(tlOut1.HasItem());
			AssertTrue(tlOut1.Item.IsMidAnimation());
		}

		World.DeleteTile(tlOut1.GLocation);
		var tlOut0 = World.AddEmptyTile(clo.OutTile0.GLocation + clo.Dir.ToGrid());
		using (Lvl.Simy.StartAndPause()) // The other output this time.
		{
			World.SpawnItem(tlIn, 508);
			injy.PendFullSequence([[ new CmdSlide(tlIn, clo.Dir) ]]);
			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // Nothing happens
			await Lvl.Simy.TickOnceAsync(); // Should slide out anyway
			AssertTrue(tlOut0.HasItem());
			AssertTrue(tlOut0.Item.IsMidAnimation());
		}
	}

	[ArTest.Test] public async Task TestTeleporter_vanilla()
	{
		var teleIn = Lvl.UniMan.PlaceTeleInput(new Vector2I(2, 1), Direction.West, 1);
		var teleOut = Lvl.UniMan.PlaceTeleOutput(new Vector2I(5, 1), Direction.East, 2);
		teleIn.AssignOutput(teleOut);
		var injy = Lvl.UniMan.PlaceInjector();

		// Place some tiles in the way
		Lvl.UniMan.PlacePlatform(new Vector2I(3, 1));
		Lvl.UniMan.PlacePlatform(new Vector2I(4, 1));

		var tlIn = World.AddEmptyTile(teleIn.InTile.GLocation + teleIn.Dir.ToGrid());
		var tlOut = World.AddEmptyTile(teleOut.OutTile.GLocation + teleOut.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			var it = World.SpawnItem(tlIn, -410);
			injy.PendFullSequence([[ new CmdSlide(tlIn, teleIn.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // 1...
			await Lvl.Simy.TickOnceAsync(); // 2!
			await Lvl.Simy.TickOnceAsync(); // Item slides out from the other side
			AssertTrue(tlOut.HasItem());
			AssertRefEq(tlOut.Item, it);
			AssertTrue(tlOut.Item.IsMidAnimation());
		}
	}

	[ArTest.Test] public async Task TestTeleporter_lone_input()
	{
		var teleIn = Lvl.UniMan.PlaceTeleInput(new Vector2I(2, 1), Direction.West, 1);
		var injy = Lvl.UniMan.PlaceInjector();

		var tlIn = World.AddEmptyTile(teleIn.InTile.GLocation + teleIn.Dir.ToGrid());

		using (Lvl.Simy.StartAndPause())
		{
			var it = World.SpawnItem(tlIn, 189);
			injy.PendFullSequence([[ new CmdSlide(tlIn, teleIn.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // And never leaves...
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
		}

		var teleOut = Lvl.UniMan.PlaceTeleOutput(new Vector2I(5, 1), Direction.East, 2);
		teleIn.AssignOutput(teleOut);
		AssertTrue(teleIn.IsConnectedTo(teleOut));
		// AssertTrue(teleOut.IsConnectedTo(teleIn)); // Not sure if this will be provided.
		teleIn.UnassignOutput(); // Making sure this works...
		AssertFalse(teleIn.IsConnectedTo(teleOut));

		using (Lvl.Simy.StartAndPause()) // Double checking...
		{
			var it = World.SpawnItem(tlIn, -457);
			injy.PendFullSequence([[ new CmdSlide(tlIn, teleIn.Dir.Invert()) ]]);

			await Lvl.Simy.TickOnceAsync(); // Item slides in
			await Lvl.Simy.TickOnceAsync(); // And never leaves...
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
			await Lvl.Simy.TickOnceAsync(); // Clogged
		}
	}
}
