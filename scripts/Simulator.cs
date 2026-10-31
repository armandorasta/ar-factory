using ArFactory;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Simulator
{
	public class PendProxy(CommandRunner runner)
	{
		public Unit? PenderUnit;
		private readonly CommandRunner m_Runner = runner;

		public void Pend(IEnumerable<IEnumerable<Command>> groups, int startIndex = 0)
		{
			m_Runner.PendFullSequence(PenderUnit!, groups.Where(g => g.Any(_ => true)), startIndex);
		}
	}

	public struct ExecState()
	{
		public enum Status
		{
			NothingSus,
			IncorrectValue,
		}
		
		public Status Stat = Status.NothingSus;
		public int ExpectedValue = Item.InvalidValue;
		public int RecievedValue = Item.InvalidValue;
		public Vector2I GLocation = new(-1, -1);
	}

	public class OnTickMediator(WorldPanel world, CommandRunner runner)
	{
		public CommandRunner.OnTickPender Pender = new(runner);
		public WorldPanel World = world;
		public ExecState m_TState; // TODO: make this private.

		public void CrashSimulation(int expected, int found, Vector2I where)
		{
			m_TState = new()
			{
				Stat = ExecState.Status.IncorrectValue,
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

	public WorldPanel World { get; private set; }
	public SimulatorClock Clock => m_Clock;

	private State m_CurrState = State.Off;
	private readonly SimulatorClock m_Clock;
	private readonly CommandRunner m_Runner = new();

	
	public Simulator(WorldPanel world)
	{
		this.World = world;
		m_Clock = new SimulatorClock(this);
	}


	public void OnTick()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);

		var ticker = new OnTickMediator(World, m_Runner);
		World.OnTick(new PendProxy(m_Runner));
		
		m_Runner.OnTick(ticker);
		switch (ticker.m_TState.Stat) {
		case ExecState.Status.NothingSus:
			break;
		
		case ExecState.Status.IncorrectValue:
			Halt();
			GD.PrintErr($"Expected {ticker.m_TState.ExpectedValue} but found {ticker.m_TState.RecievedValue} on {ticker.m_TState.GLocation}");
			break;

		default:
			throw new NotImplementedException("New ExecState.Status?");
		}

		switch (World.CheckForErrorsThisTick()) {
		case WorldPanel.TickState.NothingSus:
			break;

		case WorldPanel.TickState.RanOutOfItems:
			Halt();
			GD.PrintErr("RAN OUT OF ITEMS!!");
			break;
		
		case WorldPanel.TickState.SuccessfulRun:
			Halt();
			GD.PrintErr("WIN!");
			break;

		default:
			throw new NotImplementedException("New WorldPanel.TickState?");
		}
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
	public SimulationAutoEnder PlaySimulation() => Start(bPauseOnEntry: false);
	/// <summary>
	/// Same as pressing the debug button to start the level. This pauses the simulation before the
	/// first tick is processed then <see cref="OnTick"/> or <see cref="ResumeSimulation"/> can
	/// be used to proceed.
	/// </summary>
	/// <returns>
	/// A proxy that can be used in a using statement which will automatically end the simulation.
	/// </returns>
	public SimulationAutoEnder PlaySimulationInDebug() => Start(bPauseOnEntry: true);

	/// <summary>
	/// Returns a proxy that can be used in a using statement which will automatically end the simulation.
	/// </summary>
	private SimulationAutoEnder Start(bool bPauseOnEntry = false)
	{
		// When it's off, both values for bPauseOnEntry work because of the tests...
		SetState(bPauseOnEntry ? State.Debug : State.Play);
		OnStart();
		m_Clock.Start(bPauseOnEntry);
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
	}
}
