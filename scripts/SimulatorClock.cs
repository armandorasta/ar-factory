using System;
using System.Threading.Tasks;

namespace ArFactory;

public partial class SimulatorClock : Godot.Node
{
	private enum State
	{
		Off,
		Play,
		Debug,
	}

	public const float MinTickRate = 0.1f;
	public const float MaxTickRate = 1000.0f;


	public float DefaultTickMillis = 300.0f;

	private State m_CurrState;
	private Simulator m_ParentSimy;
	private Godot.Timer m_TickTimer = new();
	private int m_TickCount = 0; // Number of ticks since the start.
	private float m_TickMillis;

	public SimulatorClock(Simulator parentSimy)
	{
		this.m_TickMillis = DefaultTickMillis;
		this.m_ParentSimy = parentSimy;
		InitTimer();
	}

	private void InitTimer()
	{
		AddChild(m_TickTimer);
		m_TickTimer.OneShot = true;
		m_TickTimer.Paused = true;
		m_TickTimer.Timeout += OnTickTimer_TimeOut;
	}

	public float GetTickElapsedMillis() => m_TickMillis - 1000.0f * (float)m_TickTimer.TimeLeft;
	public float GetTickRate() => 1000.0f / m_TickMillis;
	public float GetMilliSecondsPerTick() => m_TickMillis;
	public int GetTicksSinceStart() => m_TickCount;
	
	public void SetTickRate(float ratePerSec)
		=> m_TickMillis = 1000.0f / float.Clamp(ratePerSec, MinTickRate, MaxTickRate);
	public void SetTickMillis(float millis) => SetTickRate(1000.0f / millis);

	public bool IsStopped() => m_CurrState is State.Off;
	/// <summary>
	/// Checks if the tick timer has timed out, this almost certainly means we are in debug mode waiting for the user
	/// to advance, but there's an extremely small chance this function returns /// <see langword="true"/> in normal
	/// mode...
	/// </summary>
	public bool IsPausedRightAtTickStart() => m_TickTimer.TimeLeft == 0.0;

	public void Start(bool bPauseOnEntry = false)
	{
		SetState(bPauseOnEntry ? State.Debug : State.Play);
		OnStart();
		m_TickTimer.Paused = false;
		BeginTick();
	}

	/// <summary>
	/// Same as hitting the play button mid-simlation to exit debug mode.
	/// </summary>
	public void Resume()
	{
		Debug.Assert(m_CurrState is State.Debug);
		SetState(State.Play);

		if (IsPausedRightAtTickStart())
		{
			TickOnce();
		}
	}

	/// <summary>
	/// Same as hitting the debug button mid-simlation in normal mode to enter debug mode.
	/// </summary>
	public void Pause()
	{
		Debug.Assert(m_CurrState == State.Play);
		SetState(State.Debug);
	}

	/// <summary>
	/// Halts everything, bombs all items, resets all the units, etc...
	/// </summary>
	public void Stop()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		// This has to be called before switching the PlayMode for fast tick rates.
		OnStop();
		SetState(State.Off);
	}

	/// <summary>
	/// Advances the simulation by 1 tick. This function only works when the last tick has fully finished.
	/// <b>Calling this function again before the timer finishes is undefined behaviour</b> use
	/// <see cref="TickOnceAsync"/> to avoid that situation.
	/// </summary>
	public void TickOnce()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		Debug.Assert(!m_TickTimer.Paused);
		Debug.Assert(IsPausedRightAtTickStart());

		m_TickTimer.Start(m_TickMillis * 0.001f);
		m_TickCount += 1;
		m_ParentSimy.OnTick();
	}

	/// <summary>
	/// Ticks process at the start then wait for a timer, this returns after timer times out and
	/// /// <see cref="Command.OnEdge"/> is called.
	/// </summary>
	public async Task EndTickAsync()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		if (m_TickTimer.TimeLeft > 0.0)
		{
			await ToSignal(m_TickTimer, Godot.Timer.SignalName.Timeout);
		}
	}

	/// <summary>
	/// <see cref="TickOnce"/> usually finishes instantly then fires a timer that controls tick speed.<br/>
	/// <b><see cref="TickOnce"/> may not be called again before the timer is finished (undefined behaviour).</b><br/>
	/// This function however will wait for the timer if it's not finished yet and return right after
	/// <see cref="TickOnce"/> is called.<br/>
	/// Same as calling <see cref="EndTickAsync"/> followed by <see cref="TickOnce"/>.
	/// </summary>
	public async Task TickOnceAsync()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		await EndTickAsync();
		TickOnce();
	}

	/// <summary>
	/// Same as calling <see cref="TickOnceAsync"/> n times. Returns after the nth tick is fully processed.
	/// </summary>
	public async Task TickNTimesAsync(int tickCount)
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		for (var i = 0; i < tickCount; ++i)
		{
			await TickOnceAsync();
		}
	}

	/// <summary>
	/// Waits a certain number of ticks, and returns after processing the last tick.
	/// This function does not start or resume the simulation, it simply waits for <see cref="TickStarted"/> signal
	/// n times, resumes after the nth tick has started excuting.
	/// </summary>
	public async Task WaitForNTicks(int tickCount)
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		for (var i = 0; i < tickCount; ++i)
		{
			await EndTickAsync();
		}
	}

	public void Reset()
	{
		m_TickTimer.Paused = true;
		m_TickTimer.Stop();
		m_TickCount = 0;
		SetState(State.Off);
	}


	#region .Signal Handlers

	private void OnTickTimer_TimeOut()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		m_ParentSimy.OnEdge();
		BeginTick();
	}

	#endregion


	/// <summary>
	/// Called at the very start of the tick JUST before <see cref="TickOnce"/>.
	/// </summary>
	private void BeginTick()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		switch (m_CurrState) {
		case State.Play:
			TickOnce();
			break;

		case State.Debug:
			break;

		default:
			throw new InvalidProgramException("New PlayMode?");
		}
	}

	private void SetState(State newPlayMode)
	{
		m_CurrState = newPlayMode;
	}

	private void OnStart()
	{
	}

	private void OnStop()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		m_TickTimer.Paused = true;
		m_TickTimer.Stop();
		m_TickCount = 0;
	}
}
