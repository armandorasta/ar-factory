using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;

public class LevelTests : ArTest.TestSuit
{
	public Editor Lvl => m_Lv!;
	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.EditorScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(m_Lv);
		m_Lv.Clock.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		m_Lv!.World.Reset();
		m_Lv!.World.SetDims(new(15, 10));
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

	[ArTest.Test] public async Task TestDebugMode()
	{
		var simy = Lvl.Clock;
		using (simy.PlaySimulationInDebug())
		{
			await simy.TickOnceAsync();
			AssertEq(simy.GetTicksSinceStart(), 1); 
			await simy.TickNTimesAsync(5);
			AssertEq(simy.GetTicksSinceStart(), 6);
			await simy.TickNTimesAsync(5);
			AssertEq(simy.GetTicksSinceStart(), 11);
		}
	}

	[ArTest.Test] public async Task TestPausingAndResuming()
	{
		var simy = Lvl.Clock;
		AssertTrue(simy.IsHalted());

		simy.PlaySimulation();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertFalse(simy.IsDebugging());

		simy.PauseSimulation();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused()); // Tick has to end first, then simulation actually pauses.
		AssertTrue(simy.IsDebugging());
		
		await simy.EndTickAsync(); // NOW the simulation is paused for real.
		AssertFalse(simy.IsHalted());
		AssertTrue(simy.IsPaused());
		AssertTrue(simy.IsDebugging());

		var nTicksBefore = simy.GetTicksSinceStart();
		simy.TickOnce();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.GetTicksSinceStart(), nTicksBefore + 1);

		nTicksBefore = simy.GetTicksSinceStart();
		simy.ResumeSimulation();
		await simy.WaitForNTicks(3);
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertFalse(simy.IsDebugging());
		AssertEq(simy.GetTicksSinceStart(), nTicksBefore + 3);

		simy.HaltSimulation();
		AssertTrue(simy.IsHalted());

		simy.PlaySimulationInDebug();
		AssertFalse(simy.IsHalted());
		AssertTrue(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.GetTicksSinceStart(), 0);
		
		simy.TickOnce();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.GetTicksSinceStart(), 1);

		await simy.TickOnceAsync();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.GetTicksSinceStart(), 2);

		await simy.TickNTimesAsync(2);
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.GetTicksSinceStart(), 4);

		simy.HaltSimulation();
	}
}