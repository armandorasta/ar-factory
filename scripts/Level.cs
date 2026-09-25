using ArFactory.Tests;
using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArFactory;

public partial class Level : Node2D
{
	public class SimulationAutoEnder(Level lv) : IDisposable
	{
		private readonly Level m_Level = lv;

		void IDisposable.Dispose()
		{
			Debug.Assert(m_Level.IsSimRunning());
			m_Level.EndSimulation();
		}
	}


	enum PlayMode { Off, Play, Debug }

	/// <summary>
	/// Emitted after a tick is fully processed. <paramref name="tickCount"/> is the number of ticks
	/// processed up until now.
	/// </summary>
	[Signal]
	public delegate void TickProcessedEventHandler(int tickCount);


	// Nodes
	public Godot.Camera2D Cam;
	public WorldPanel World;
	public Godot.Button PlayButt;
	public Godot.Button PauseButt;
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
		Cam = GetNode<Godot.Camera2D>("WorldPanel/Cam");
		World = GetNode<WorldPanel>("WorldPanel");
		PlayButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt");
		PauseButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt");
		DebugButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt");
		ToolsHBox = GetNode<Godot.HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox");
		PlayHBox = GetNode<Godot.HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox");
		TicksLabel = GetNode<Godot.Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel");
		SpeedSlider = GetNode<Godot.HSlider>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider");
		TickSpeedLabel = GetNode<Godot.Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel");

		// World.SetDims(new(5, 5));
		Cam.Position = World.Size * 0.5f;
		
		m_TickTimer.OneShot = true;
		AddChild(m_TickTimer);
		m_TickTimer.Timeout += OnTickTimer_TimeOut;

		PlayButt.Pressed += OnPlayButt_Pressed;
		DebugButt.Pressed += OnDebugButt_Pressed;
		PauseButt.Pressed += OnPauseButt_Pressed;
		SyncButtStates();

		SpeedSlider.ValueChanged += OnSpeedSlider_ValueChanged;
		ResetTickRate();
		SpeedSlider.Value = GetTickRate();

		AddTools();
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
		case PlayMode.Debug: break;
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


	private void OnTick()
	{
		World.OnTick(this);
		TicksLabel.Text = $"ticks: {m_TickCount}";
		m_TickCount += 1;
		EmitSignal(SignalName.TickProcessed, m_TickCount);
	}

	/// <summary>
	/// Returns a proxy that can be used in a using statement which will automatically end the simulation.
	/// </summary>
	public SimulationAutoEnder StartSimulation(bool bWasteFirstTick = false)
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Play);
		m_CurrPlayMode = PlayMode.Play;
		SyncButtStates();
		OnSimulationStart();

		m_TickTimer.Paused = false;

		if (bWasteFirstTick)
		{
			m_TickTimer.Start(m_TickMillis * 0.001f);
		}
		else
		{
			OnTickTimer_TimeOut();
		}

		return new(this);
	}

	public void EndSimulation()
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
	/// Waits a certain number of ticks, and returns after processing the last tick.
	/// </summary>
	public async Task ProcessNextTicks(int tickCount)
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
		OnTick();
		m_TickTimer.Start(m_TickMillis * 0.001f);
	}

	private void OnPlayButt_Pressed()
	{
		StartSimulation();
	}

	private void OnPauseButt_Pressed()
	{
		EndSimulation();
	}

	private void OnDebugButt_Pressed()
	{
		m_CurrPlayMode = PlayMode.Debug;
		SyncButtStates();
		GD.Print("Debug thick meaty butt");
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
			PauseButt.Disabled = true;
			DebugButt.Disabled = false;
			PlayHBox.Hide();
			ToolsHBox.Show();
			break;
		
		case PlayMode.Play:
			PlayButt.Disabled = true;
			PauseButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;
		
		case PlayMode.Debug:
			PlayButt.Disabled = false;
			PauseButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;
		}
	}

	private void OnSimulationStart()
	{
	}

	private void OnSimulationEnd()
	{
		m_TickTimer.Paused = true;
		m_TickCount = 0;
		World.CleanUpAfterSim();
	}

	private void AddTools()
	{
	}
}
