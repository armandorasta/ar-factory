using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class LevelTests : ArTest.TestSuit
{
	public Level Lvl => m_Lv!;
	private Level? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<PackedScene>("res://scenes/level.tscn");
		m_Lv = levelScene.Instantiate<Level>();
		AddNode(m_Lv);
		m_Lv.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(15, 10));
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

	[ArTest.Test] public async Task TestDebugMode()
	{
		using (Lvl.PlaySimulationInDebug())
		{
			await Lvl.TickOnceAsync();
			AssertEq(Lvl.GetTicksSinceStart(), 1); 
			await Lvl.TickNTimesAsync(5);
			AssertEq(Lvl.GetTicksSinceStart(), 6);
			await Lvl.TickNTimesAsync(5);
			AssertEq(Lvl.GetTicksSinceStart(), 11);
		}
	}

	[ArTest.Test] public async Task TestPausingAndResuming()
	{
		AssertFalse(Lvl.IsSimRunning());

		Lvl.PlaySimulation();
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused());
		AssertFalse(Lvl.IsDebugging());

		Lvl.PauseSimulation();
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused()); // Tick has to end first, then simulation actually pauses.
		AssertTrue(Lvl.IsDebugging());
		
		await Lvl.EndTickAsync(); // NOW the simulation is paused for real.
		AssertTrue(Lvl.IsSimRunning());
		AssertTrue(Lvl.IsPaused());
		AssertTrue(Lvl.IsDebugging());

		var nTicksBefore = Lvl.GetTicksSinceStart();
		Lvl.TickOnce();
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused());
		AssertTrue(Lvl.IsDebugging());
		AssertEq(Lvl.GetTicksSinceStart(), nTicksBefore + 1);

		nTicksBefore = Lvl.GetTicksSinceStart();
		Lvl.ResumeSimulation();
		await Lvl.WaitForNTicks(3);
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused());
		AssertFalse(Lvl.IsDebugging());
		AssertEq(Lvl.GetTicksSinceStart(), nTicksBefore + 3);

		Lvl.HaltSimulation();
		AssertFalse(Lvl.IsSimRunning());

		Lvl.PlaySimulationInDebug();
		AssertTrue(Lvl.IsSimRunning());
		AssertTrue(Lvl.IsPaused());
		AssertTrue(Lvl.IsDebugging());
		AssertEq(Lvl.GetTicksSinceStart(), 0);
		
		Lvl.TickOnce();
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused());
		AssertTrue(Lvl.IsDebugging());
		AssertEq(Lvl.GetTicksSinceStart(), 1);

		await Lvl.TickOnceAsync();
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused());
		AssertTrue(Lvl.IsDebugging());
		AssertEq(Lvl.GetTicksSinceStart(), 2);

		await Lvl.TickNTimesAsync(2);
		AssertTrue(Lvl.IsSimRunning());
		AssertFalse(Lvl.IsPaused());
		AssertTrue(Lvl.IsDebugging());
		AssertEq(Lvl.GetTicksSinceStart(), 4);

		Lvl.HaltSimulation();
	}
}