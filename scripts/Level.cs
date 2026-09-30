using System;
using System.Threading.Tasks;

namespace ArFactory;

public partial class Level : Godot.Node2D
{
	public class SimulationAutoEnder(Level lv) : IDisposable
	{
		private readonly Level m_Level = lv;

		void IDisposable.Dispose()
		{
			Debug.Assert(m_Level.IsSimRunning());
			m_Level.HaltSimulation();
		}
	}

	public const float MinTickRate = 0.1f;
	public const float MaxTickRate = 1000.0f;


	enum PlayMode 
	{ 
		Off,
		Play,
		Debug,
	}

	/// <summary>
	/// Emitted after a tick is fully processed. <paramref name="tickCount"/> is the number of ticks
	/// processed up until now.
	/// </summary>
	[Godot.Signal]
	public delegate void TickProcessedEventHandler(int tickCount);

	// Nodes
	public Godot.Camera2D Cam;
	public WorldPanel World;
	public Godot.Button PlayButt;
	public Godot.Button HaltButt;
	public Godot.Button DebugButt;
	public Godot.HBoxContainer ToolsHBox;
	public Godot.HBoxContainer PlayHBox;
	public Godot.Label TicksLabel;
	public Godot.HSlider SpeedSlider;
	public Godot.Label TickSpeedLabel;


	// Publics
	public float DefaultTickMillis = 300.0f;


	// Privates
	private PlayMode m_CurrPlayMode = PlayMode.Off;
	private Godot.Timer m_TickTimer = new();
	private int m_TickCount = 0; // Number of ticks since the start.
	private float m_TickMillis;


	public override void _Ready()
	{
		GetAllNodes();
		
		Cam.Position = World.Size * 0.5f;
		
		InitTimer();
		InitSimButts();
		InitSimSpeedSlider();
		AddTools();
	}

	private void GetAllNodes()
	{
		Cam = GetNode<Godot.Camera2D>("WorldPanel/Cam");
		World = GetNode<WorldPanel>("WorldPanel");
		PlayButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt");
		HaltButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt");
		DebugButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt");
		ToolsHBox = GetNode<Godot.HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox");
		PlayHBox = GetNode<Godot.HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox");
		TicksLabel = GetNode<Godot.Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel");
		SpeedSlider = GetNode<Godot.HSlider>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider");
		TickSpeedLabel = GetNode<Godot.Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel");
	}

	private void InitTimer()
	{
		m_TickTimer.OneShot = true;
		AddChild(m_TickTimer);
		m_TickTimer.Timeout += OnTickTimer_TimeOut;
	}

	private void InitSimButts()
	{
		PlayButt.Pressed += OnPlayButt_Pressed;
		DebugButt.Pressed += OnDebugButt_Pressed;
		HaltButt.Pressed += OnHaltButt_Pressed;
		SyncButtStates();
	}
	
	private void InitSimSpeedSlider()
	{
		SpeedSlider.ValueChanged += OnSpeedSlider_ValueChanged;
		ResetTickRate();
		SpeedSlider.Value = GetTickRate();
	}

	public override void _EnterTree()
	{
		Debug.TreePtr = GetTree();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double dt)
	{
		switch (m_CurrPlayMode) {
		case PlayMode.Off: break;
		case PlayMode.Debug:
		case PlayMode.Play: 
			World.DoPerFrame((float)dt, this);
			break;
		}
	}

	public float GetTickMillis() => m_TickMillis;
	public float GetTickElapsedMillis() => m_TickMillis - 1000.0f * (float)m_TickTimer.TimeLeft;
	public float GetTickRate() => 1000.0f / m_TickMillis;
	public int GetTicksSinceStart() => m_TickCount;

	public void ResetTickRate() { m_TickMillis = DefaultTickMillis; }
	public void SetTickRate(float ratePerSec) { m_TickMillis = 1000.0f / ratePerSec; }

	public bool IsSimRunning() => m_CurrPlayMode != PlayMode.Off;
	public bool IsDebugging() => m_CurrPlayMode == PlayMode.Debug;
	public bool IsPaused() => m_TickTimer.TimeLeft == 0.0;

	/// <summary>
	/// Checks if we are in debug mode waiting for a button press to proceed.
	/// </summary>
	private bool IsPausedRightBeforeOnTick() => m_TickTimer.TimeLeft == 0;


	private void OnTick()
	{
		World.OnTick(this);
		m_TickCount += 1;
		UpdateTickCountLabel();
		EmitSignal(SignalName.TickProcessed, m_TickCount);
	}

	/// <summary>
	/// Same as pressing the play button on the level.
	/// </summary>
	/// <returns>
	/// A proxy that can be used in a using statement which will automatically end the simulation.
	/// </returns>
	public SimulationAutoEnder Play() => StartSimulation(bPauseOnEntry: false);
	/// <summary>
	/// Same as pressing the debug button to start the level. This pauses the simulation before the
	/// first tick is processed then <see cref="TickOnce"/> or <see cref="ResumeSimulation"/> can
	/// be used to proceed.
	/// </summary>
	/// <returns>
	/// A proxy that can be used in a using statement which will automatically end the simulation.
	/// </returns>
	public SimulationAutoEnder PlayButPauseOnEntry() => StartSimulation(bPauseOnEntry: true);

	/// <summary>
	/// Returns a proxy that can be used in a using statement which will automatically end the simulation.
	/// </summary>
	private SimulationAutoEnder StartSimulation(bool bPauseOnEntry = false) 
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Play);
		switch (m_CurrPlayMode) {
		case PlayMode.Off:
			// When it's off, both values for bPauseOnEntry work because of the tests...
			SetPlayMode(bPauseOnEntry ? PlayMode.Debug : PlayMode.Play);
			break;
		
		case PlayMode.Play:
			throw new InvalidProgramException();

		case PlayMode.Debug:
			// In debug mode, bPauseOnEntry must be true!
			Debug.Assert(bPauseOnEntry);
			break;
		}
		
		OnSimulationStart();
		m_TickTimer.Paused = false;
		OnTickTimer_TimeOut();

		return new(this);
	}

	/// <summary>
	/// Same as hitting the play button mid-simlation to exit debug mode.
	/// </summary>
	public void ResumeSimulation()
	{
		Debug.Assert(m_CurrPlayMode == PlayMode.Debug);
		SetPlayMode(PlayMode.Play);
		
		if (IsPausedRightBeforeOnTick())
		{
			TickOnce();
		}
	}
	
	/// <summary>
	/// Same as hitting the debug button mid-simlation the first time to enter debug mode.
	/// </summary>
	public void PauseSimulation()
	{
		Debug.Assert(m_CurrPlayMode == PlayMode.Play);
		SetPlayMode(PlayMode.Debug);
	}
	
	/// <summary>
	/// Halts everything, bombs all items, resets all the units, etc...
	/// </summary>
	public void HaltSimulation()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Off);
		
		// This makes it possible to call this function multiple times.
		if (m_TickCount > 0)
		{
			// This has to be called before switching the PlayMode for fast tick rates.
			OnSimulationEnd();
		}		

		m_CurrPlayMode = PlayMode.Off;
		SyncButtStates();

	}

	/// <summary>
	/// Calls <see cref="OnTick"/> first, then starts the timer, so ticks process at the begining
	/// not the end of the tick. It has to be in this order, otherwise smooth commands get confused.<br/>
	/// <b>Calling this function again before the timer finishes is undefined behaviour</b>
	/// </summary>
	public void TickOnce()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Off);
		Debug.Assert(!m_TickTimer.Paused);
		Debug.Assert(IsPausedRightBeforeOnTick());

		m_TickTimer.Start(m_TickMillis * 0.001f);
		OnTick();
	}

	/// <summary>
	/// <see cref="TickOnce"/> usually finishes instantly then fires a timer that controls tick speed.
	/// In debug calling <see cref="TickOnce"/> before the timer is finished crashes the program,
	/// in release, it will instantly skip to next tick (undefined behaviour).
	/// This function however will wait for the timer if it's not finished yet and return right after
	/// <see cref="TickOnce"/> is called.
	/// </summary>
	public async Task TickOnceAsync()
	{
		if (m_TickTimer.TimeLeft > 0.0)
		{
			await ToSignal(m_TickTimer, Godot.Timer.SignalName.Timeout);
		}
		TickOnce();
	}
	
	/// <summary>
	/// Same as calling <see cref="TickOnceAsync"/> n times. Returns after the nth tick is fully
	/// processed.
	/// </summary>
	public async Task TickNTimesAsync(int tickCount)
	{
		for (var i = 0; i < tickCount; ++i)
		{
			await TickOnceAsync();
		}
	}

	/// <summary>
	/// Waits a certain number of ticks, and returns after processing the last tick.
	/// This function does not start or resume the simulation, it simply waits for 
	/// <see cref="TickProcessed"/> signal n times.
	/// </summary>
	public async Task WaitForNTicks(int tickCount)
	{
		for (var i = 0; i < tickCount; ++i)
		{
			await ToSignal(this, SignalName.TickProcessed);
		}
	}


	#region .Signal Handlers

	private void OnTickTimer_TimeOut()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Off);		
		switch (m_CurrPlayMode) {
		case PlayMode.Off:
			throw new InvalidProgramException();
		
		case PlayMode.Play:
			TickOnce();
			break;
		
		case PlayMode.Debug:
			break;
		}
	}

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Play);
		switch (m_CurrPlayMode) {
		case PlayMode.Off:
			StartSimulation(bPauseOnEntry: false);
			break;
		
		case PlayMode.Play:
			throw new InvalidProgramException();

		case PlayMode.Debug:
			ResumeSimulation();
			break;
		}
	}

	private void OnHaltButt_Pressed()
	{
		HaltSimulation();
	}

	private void OnDebugButt_Pressed()
	{
		switch (m_CurrPlayMode) {
		case PlayMode.Off:
			SetPlayMode(PlayMode.Debug);
			StartSimulation(bPauseOnEntry: true);
			break;
		
		case PlayMode.Play:
			PauseSimulation();
			break;
		
		case PlayMode.Debug:
			if (!IsPausedRightBeforeOnTick()) // Spamming the debug key while the tick is in progress.
			{
				return;	
			}

			TickOnce();
			break;
		}
	}

	private void OnSpeedSlider_ValueChanged(double newVal)
	{
		SetTickRate((float)newVal);
		TickSpeedLabel.Text = $"{GetTickRate()} tick/s";
	}

	
	#endregion .Signal Handlers


	private void SyncButtStates()
	{
		switch (m_CurrPlayMode) {
		case PlayMode.Off:
			PlayButt.Disabled = false;
			HaltButt.Disabled = true;
			DebugButt.Disabled = false;
			PlayHBox.Hide();
			ToolsHBox.Show();
			break;
		
		case PlayMode.Play:
			PlayButt.Disabled = true;
			HaltButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;
		
		case PlayMode.Debug:
			PlayButt.Disabled = false;
			HaltButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;
		}
	}

	private void OnSimulationStart()
	{
		UpdateTickCountLabel();
	}

	private void OnSimulationEnd()
	{
		m_TickTimer.Paused = true;
		m_TickCount = 0;
		World.CleanUpAfterSim();
	}

	private void SetPlayMode(PlayMode newPlayMode)
	{
		if (newPlayMode == m_CurrPlayMode)
		{
			return;
		}

		m_CurrPlayMode = newPlayMode;
		SyncButtStates();
	}

	private void UpdateTickCountLabel()
	{
		TicksLabel.Text = $"tick: {m_TickCount}";
	}

	private void AddTools()
	{
	}
}
