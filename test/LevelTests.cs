using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class LevelTests : ArTest.TestSuit
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

	[ArTest.Test] public async Task TestDebugMode()
	{
		using (m_Lv.PlaySimulationInDebug())
		{
			await m_Lv.TickOnceAsync();
			AssertEq(m_Lv.GetTicksSinceStart(), 1); 
			await m_Lv.TickNTimesAsync(5);
			AssertEq(m_Lv.GetTicksSinceStart(), 6);
			await m_Lv.TickNTimesAsync(5);
			AssertEq(m_Lv.GetTicksSinceStart(), 11);
		}
	}

	[ArTest.Test] public async Task TestPausingAndResuming()
	{
		AssertFalse(m_Lv.IsSimRunning());

		m_Lv.PlaySimulation();
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused());
		AssertFalse(m_Lv.IsDebugging());

		m_Lv.PauseSimulation();
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused()); // Tick has to end first, then simulation actually pauses.
		AssertTrue(m_Lv.IsDebugging());
		
		await m_Lv.EndTickAsync(); // NOW the simulation is paused for real.
		AssertTrue(m_Lv.IsSimRunning());
		AssertTrue(m_Lv.IsPaused());
		AssertTrue(m_Lv.IsDebugging());

		var nTicksBefore = m_Lv.GetTicksSinceStart();
		m_Lv.TickOnce();
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused());
		AssertTrue(m_Lv.IsDebugging());
		AssertEq(m_Lv.GetTicksSinceStart(), nTicksBefore + 1);

		nTicksBefore = m_Lv.GetTicksSinceStart();
		m_Lv.ResumeSimulation();
		await m_Lv.WaitForNTicks(3);
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused());
		AssertFalse(m_Lv.IsDebugging());
		AssertEq(m_Lv.GetTicksSinceStart(), nTicksBefore + 3);

		m_Lv.HaltSimulation();
		AssertFalse(m_Lv.IsSimRunning());

		m_Lv.PlaySimulationInDebug();
		AssertTrue(m_Lv.IsSimRunning());
		AssertTrue(m_Lv.IsPaused());
		AssertTrue(m_Lv.IsDebugging());
		AssertEq(m_Lv.GetTicksSinceStart(), 0);
		
		m_Lv.TickOnce();
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused());
		AssertTrue(m_Lv.IsDebugging());
		AssertEq(m_Lv.GetTicksSinceStart(), 1);

		await m_Lv.TickOnceAsync();
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused());
		AssertTrue(m_Lv.IsDebugging());
		AssertEq(m_Lv.GetTicksSinceStart(), 2);

		await m_Lv.TickNTimesAsync(2);
		AssertTrue(m_Lv.IsSimRunning());
		AssertFalse(m_Lv.IsPaused());
		AssertTrue(m_Lv.IsDebugging());
		AssertEq(m_Lv.GetTicksSinceStart(), 4);

		m_Lv.HaltSimulation();
	}
}