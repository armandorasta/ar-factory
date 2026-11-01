using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

namespace ArFactory.Tests;
using static ArTest.Asserts;


public class SimulatorTests : ArTest.TestSuit
{
	public Editor Lvl => m_Lv!;
	private Editor? m_Lv;

	public override void BeforeAll()
	{
		var levelScene = GD.Load<Godot.PackedScene>(SceneManager.EditorScenePath);
		m_Lv = levelScene.Instantiate<Editor>();
		AddNode(m_Lv);
		m_Lv.Simy.Clock.SetTickRate(100);
	}

	public override void BeforeEach()
	{
		Lvl.World.Reset();
		Lvl.World.SetDims(new(15, 10));
	}

	public override void AfterEach()
	{
		AssertTrue(Lvl.Simy.IsHalted());
	}

	public override void AfterAll()
	{
		Lvl.QueueFree();
	}

	[ArTest.Test] public async Task TestDebugMode()
	{
		var simy = Lvl.Simy;
		using (simy.StartAndPause())
		{
			await simy.TickOnceAsync();
			AssertEq(simy.Clock.GetTicksSinceStart(), 1); 
			await simy.TickNTimesAsync(5);
			AssertEq(simy.Clock.GetTicksSinceStart(), 6);
			await simy.TickNTimesAsync(5);
			AssertEq(simy.Clock.GetTicksSinceStart(), 11);
		}
	}

	[ArTest.Test] public async Task TestPausingAndResuming()
	{
		var simy = Lvl.Simy;
		AssertTrue(simy.IsHalted());

		simy.Start();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertFalse(simy.IsDebugging());

		simy.Pause();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused()); // Tick has to end first, then simulation actually pauses.
		AssertTrue(simy.IsDebugging());
		
		await simy.EndTickAsync(); // NOW the simulation is paused for real.
		AssertFalse(simy.IsHalted());
		AssertTrue(simy.IsPaused());
		AssertTrue(simy.IsDebugging());

		var nTicksBefore = simy.Clock.GetTicksSinceStart();
		simy.TickOnce();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.Clock.GetTicksSinceStart(), nTicksBefore + 1);

		nTicksBefore = simy.Clock.GetTicksSinceStart();
		simy.Resume();
		await simy.WaitForNTicks(3);
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertFalse(simy.IsDebugging());
		AssertEq(simy.Clock.GetTicksSinceStart(), nTicksBefore + 3);

		simy.Halt();
		AssertTrue(simy.IsHalted());

		simy.StartAndPause();
		AssertFalse(simy.IsHalted());
		AssertTrue(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.Clock.GetTicksSinceStart(), 0);
		
		simy.TickOnce();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.Clock.GetTicksSinceStart(), 1);

		await simy.TickOnceAsync();
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.Clock.GetTicksSinceStart(), 2);

		await simy.TickNTimesAsync(2);
		AssertFalse(simy.IsHalted());
		AssertFalse(simy.IsPaused());
		AssertTrue(simy.IsDebugging());
		AssertEq(simy.Clock.GetTicksSinceStart(), 4);

		simy.Halt();
	}
}