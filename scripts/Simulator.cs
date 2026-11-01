using ArFactory;
using Godot;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace ArFactory;

public class Simulator
{
	public class PendProxy(CommandRunner runner)
	{
		public readonly struct Perm(CommandRunner runner)
		{
			private readonly CommandRunner m_Runner = runner;
			public bool IsFine(PendProxy proxy) => m_Runner == proxy.m_Runner;
		}

		public Unit? PenderUnit { get; private set; }
		private readonly CommandRunner m_Runner = runner;

		public void Pend(IEnumerable<IEnumerable<Command>> groups, int startIndex = 0)
		{
			m_Runner.PendFullSequence(PenderUnit!, groups.Where(g => g.Any(_ => true)), startIndex);
		}

		public void _SetPenderUnit(Perm perm, Unit newUnit)
		{
			Debug.AssertNotNull(newUnit);
			Debug.Assert(perm.IsFine(this));
			PenderUnit = newUnit;
		}
	}

	public struct TickExecState()
	{
		public enum Kind
		{
			NothingSus,
			IncorrectValue,
		}
		
		public Kind Stat = Kind.NothingSus;
		public int ExpectedValue = Item.InvalidValue;
		public int RecievedValue = Item.InvalidValue;
		public Vector2I GLocation = new(-1, -1);
	}

	public class OnTickMediator(WorldPanel world, CommandRunner runner)
	{
		public CommandRunner.OnTickPender Pender = new(runner);
		public WorldPanel World = world;
		private TickExecState m_TickState;

		public TickExecState GetState() => m_TickState;

		public void CrashSimulation(int expected, int found, Vector2I where)
		{
			m_TickState = new()
			{
				Stat = TickExecState.Kind.IncorrectValue,
				ExpectedValue = expected,
				RecievedValue = found,
				GLocation = where,
			};
		}
	}

	public class PerFrameMediator(WorldPanel world, SimulatorClock clock, double dt)
	{
		public WorldPanel World = world;
		public double FrameDelta = dt;
		public double TickMillis => m_Clock.GetMilliSecondsPerTick();
		public double TickElapsedPercent => m_Clock.GetTickElapsedMillis() / TickMillis;

		private readonly SimulatorClock m_Clock = clock;
	}

	public class OnEdgeMediator(WorldPanel world)
	{
		public WorldPanel World = world;
	}

	public sealed class SimulationAutoEnder(Simulator sim) : IDisposable
	{
		private readonly Simulator m_Sim = sim;
		void IDisposable.Dispose()
		{
			if (!m_Sim.IsHalted())
			{
				m_Sim.Halt();
			}
		}
	}

	private enum State
	{
		Off,
		Play,
		Debug,
	}

	public enum WorldTickResult
	{
		NothingSus,
		RanOutOfItems,
		SuccessfulRun,
	}

	public event Action? SimulationStarted;
	public event Action? SimulationEnded;

	public WorldPanel World { get; private set; }
	public SimulatorClock Clock => m_Clock;

	private State m_CurrState = State.Off;
	private readonly SimulatorClock m_Clock;
	private readonly CommandRunner m_Runner = new();

	
	public Simulator(WorldPanel world)
	{
		this.World = world;
		m_Clock = new SimulatorClock(this);
		world.AddChild(m_Clock);
	}


	public void OnTick()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);

		var ticker = new OnTickMediator(World, m_Runner);

		foreach (var tl in World.GetTilesIter())
		{
			tl.GetItemMaybeNull()?.ResetMovementFlag();
		}

		var allUnits = World.GetUnitsIter();
		foreach (var u in allUnits)
		{
			u.PreprocessTick();
		}

		var pender = new PendProxy(m_Runner);
		foreach (var u in allUnits)
		{
			pender._SetPenderUnit(new(m_Runner), u);
			u.HandlePendingNewCommands(pender);
		}

		m_Runner.OnTick(ticker);
		HandleRunnerAfterOnTick(ticker);
	}

	private void HandleRunnerAfterOnTick(OnTickMediator ticker)
	{
		var tstate = ticker.GetState();
		switch (tstate.Stat) {
		case TickExecState.Kind.NothingSus:
			break;

		case TickExecState.Kind.IncorrectValue:
			Halt();
			GD.PrintErr($"Expected {tstate.ExpectedValue} but found {tstate.RecievedValue} on {tstate.GLocation}");
			break;

		default:
			throw new NotImplementedException("New ExecState.Status?");
		}

		switch (CheckWorldForErrorsThisTick()) {
		case WorldTickResult.NothingSus:
			break;

		case WorldTickResult.RanOutOfItems:
			Halt();
			GD.PrintErr("RAN OUT OF ITEMS!!");
			break;

		case WorldTickResult.SuccessfulRun:
			Halt();
			GD.PrintErr("WIN!");
			break;

		default:
			throw new NotImplementedException("New WorldPanel.TickState?");
		}
	}

	public WorldTickResult CheckWorldForErrorsThisTick()
	{
		var allUnits = World.GetUnitsIter();
		var allTiles = World.GetTilesIter();

		var suppliers = allUnits.Where(u => u is UNSupplier).Select(u => (u as UNSupplier)!);
		var demanders = allUnits.Where(u => u is UNDemander).Select(u => (u as UNDemander)!);

		if (demanders.Any(_ => true) // No success ever if there are no demanders obviously...
			&& demanders.All(d => d.IsDoneWithOutputSeq() && !d.HasPendingCmds()))
		{
			return WorldTickResult.SuccessfulRun;
		}
		
		// We will check if there are any suppliers in the first place, because we don't want to crash the  simulation 
		// for invalid levels, aka the ones constructed in tests...
		if (suppliers.Any(_ => true) 
			&& suppliers.All(s => s.IsDoneWithInputSeq()) 
			&& allTiles.All(tl => !tl.HasItem()))
		{
			return WorldTickResult.RanOutOfItems;
		}

		return WorldTickResult.NothingSus;
	}


	public void DoPerFrame(double dt)
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		var ticker = new PerFrameMediator(World, m_Clock, dt);
		m_Runner.DoPerFrame(ticker);
	}

	public void OnEdge()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		var ticker = new OnEdgeMediator(World);
		m_Runner.OnEdge(ticker);
	}

	public bool IsHalted() => m_CurrState is State.Off;
	/// <summary>
	/// Checks if we are in debug mode.
	/// </summary>
	public bool IsDebugging() => m_CurrState == State.Debug;
	/// <summary>
	/// Checks if the simulation is paused just before the next tick is to be processed.
	/// This function only returns <see langword="true"/> in debug mode, only when <see cref="IsDebugging"/>
	/// returns <see langword="true"/>.
	/// </summary>
	public bool IsPaused() => IsDebugging() && m_Clock.IsPausedRightAtTickStart();

	/// <summary>
	/// Same as pressing the play button on the level.
	/// </summary>
	/// <returns>
	/// A proxy that can be used in a using statement which will automatically end the simulation.
	/// </returns>
	public SimulationAutoEnder Start() => Start(bPauseOnEntry: false);
	/// <summary>
	/// Same as pressing the debug button to start the level. This pauses the simulation before the
	/// first tick is processed then <see cref="OnTick"/> or <see cref="ResumeSimulation"/> can
	/// be used to proceed.
	/// </summary>
	/// <returns>
	/// A proxy that can be used in a using statement which will automatically end the simulation.
	/// </returns>
	public SimulationAutoEnder StartAndPause() => Start(bPauseOnEntry: true);

	/// <summary>
	/// Returns a proxy that can be used in a using statement which will automatically end the simulation.
	/// </summary>
	private SimulationAutoEnder Start(bool bPauseOnEntry = false)
	{
		// When it's off, both values for bPauseOnEntry work because of the tests...
		SetState(bPauseOnEntry ? State.Debug : State.Play);
		OnStart();
		m_Clock.Start(bPauseOnEntry);
		SimulationStarted?.Invoke();
		return new(this);
	}

	/// <summary>
	/// Same as hitting the play button mid-simlation to exit debug mode.
	/// </summary>
	public void Resume()
	{
		Debug.Assert(m_CurrState is State.Debug);
		SetState(State.Play);
		m_Clock.Resume();
	}

	/// <summary>
	/// Same as hitting the debug button mid-simlation in normal mode to enter debug mode.
	/// </summary>
	public void Pause()
	{
		Debug.Assert(m_CurrState is State.Play);
		SetState(State.Debug);
		m_Clock.Pause();
	}

	/// <summary>
	/// Halts everything, bombs all items, resets all the units, etc...
	/// </summary>
	public void Halt()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		m_Clock.Stop();
		// This has to be called before switching the PlayMode for fast tick rates.
		OnEnd();
		SetState(State.Off);
		SimulationEnded?.Invoke();
	}

	public void TickOnce()
	{
		Debug.Assert(m_CurrState is State.Debug);
		Clock.TickOnce();
	}

	public async Task TickOnceAsync()
	{
		Debug.Assert(m_CurrState is State.Debug);
		await Clock.TickOnceAsync();
	}

	public async Task TickNTimesAsync(int tickCount)
	{
		Debug.Assert(m_CurrState is State.Debug);
		await Clock.TickNTimesAsync(tickCount);
	}

	public async Task WaitForNTicks(int tickCount)
	{
		Debug.Assert(m_CurrState is State.Debug or State.Play);
		await Clock.WaitForNTicks(tickCount);
	}

	public async Task EndTickAsync()
	{
		Debug.Assert(m_CurrState is State.Debug);
		await Clock.EndTickAsync();
	}

	public void Reset()
	{
		m_Runner.Reset();
		m_Clock.Reset();
		SetState(State.Off);
	}

	/// <summary>
	/// Adds a slide command blocked by another item in the way only, all other types crash the program in debug.
	/// </summary>
	public void QueueBlockedSlideCmdByAnotherItem(CmdSlide cmd)
	{
		m_Runner.QueueBlockedSlideCmdByAnotherItem(World, cmd);
	}

	private void SetState(State newPlayMode)
	{
		m_CurrState = newPlayMode;
	}

	private void OnStart()
	{
	}

	private void OnEnd()
	{
		foreach (var u in World.GetUnitsIter())
		{
			u.Reset();
		}
		
		foreach (var tl in World.GetTilesIter())
		{
			tl?.DestroyItem(true, true);
		}

		m_Runner.Reset();
	}
}
