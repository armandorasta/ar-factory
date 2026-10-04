using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class UnitTests : ArTest.TestSuit
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

	[ArTest.Test] public void TestCanFitIn()
	{
		var world = m_Lv.World;
		
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

	[ArTest.Test] public async Task TestSupplier()
	{
		var world = m_Lv.World;

		var supp = world.PlaceSupplier(Vector2I.Zero, Direction.East, 3, [237, 68, 236]);
		var tl1 = world.InstallEmptyTile(new Vector2I(1, 0));
		AssertTrue(world.HasTile(supp.GridLoc));
		using (m_Lv.PlaySimulationInDebug())
		{
			await m_Lv.TickOnceAsync(); // Tick 0 processed
			AssertFalse(tl1.HasItem());
			await m_Lv.TickOnceAsync(); // Tick 1 processed
			AssertFalse(tl1.HasItem());
			await m_Lv.TickOnceAsync(); // Tick 2 processed, first item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 237);

			await m_Lv.TickOnceAsync(); // Tick 3 processed
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await m_Lv.TickOnceAsync(); // Tick 4 processed
			AssertFalse(tl1.HasItem());
		
			await m_Lv.TickOnceAsync(); // Tick 5 processed, second item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 68);

			await m_Lv.TickOnceAsync(); // Tick 6 processed
			AssertTrue(tl1.HasItem());
			AssertFalse(tl1.Item.IsMidAnimation());
			tl1.DestroyItem();
		
			await m_Lv.TickOnceAsync(); // Tick 7 processed
			AssertFalse(tl1.HasItem());

			await m_Lv.TickOnceAsync(); // Tick 8 processed, third item
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 236);
			AssertTrue(supp.IsDoneWithInputSeq());
		}

		// Trying a different direction.
		world.Reset();
		supp = world.PlaceSupplier(Vector2I.One, Direction.South, 1, [261, 296]);
		tl1 = world.InstallEmptyTile(supp.GridLoc + new Vector2I(0, 1));
		AssertTrue(world.HasTile(supp.GridLoc));
		using (m_Lv.PlaySimulationInDebug())
		{
			await m_Lv.TickOnceAsync();
			AssertTrue(tl1.HasItem());
			AssertTrue(tl1.Item.IsMidAnimation());
			AssertEq(tl1.Item.Value, 261);
		}
	}

	// [ArTest.RunThisOnly]
	[ArTest.Test] public async Task TestSlider()
	{
		var world = m_Lv.World;
		var slid = world.PlaceSlider(new Vector2I(3, 4), Direction.North);
	}
}
