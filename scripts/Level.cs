using Godot;
using System;

namespace ArFactory;

public partial class Level : Node2D
{
	enum PlayMode { Off, Play, Debug }

	public Camera2D Cam;
	public WorldPanel World;
	public Button PlayButt;
	public Button PauseButt;
	public Button DebugButt;
	public HBoxContainer ToolsHBox;
	public HBoxContainer PlayHBox;
	public Label TicksLabel;
	public HSlider SpeedSlider;
	public Label TickSpeedLabel;

	public float DefaultTickMillis = 300.0f;

	private PlayMode m_CurrPlayMode = PlayMode.Off;
	private Timer m_TickTimer = new();
	private int m_TickCount = 0; // Number of ticks since the start.
	private float m_TickMillis;

	public override void _Ready()
	{
		Cam = GetNode<Camera2D>("WorldPanel/Cam");
		World = GetNode<WorldPanel>("WorldPanel");
		PlayButt = GetNode<Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt");
		PauseButt = GetNode<Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt");
		DebugButt = GetNode<Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt");
		ToolsHBox = GetNode<HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox");
		PlayHBox = GetNode<HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox");
		TicksLabel = GetNode<Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel");
		SpeedSlider = GetNode<HSlider>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider");
		TickSpeedLabel = GetNode<Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel");

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


	private void OnTick()
	{
		World.OnTick(this);
		TicksLabel.Text = $"ticks: {m_TickCount}";
		m_TickCount += 1;
	}

	private void OnTickTimer_TimeOut()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Off);
		OnTick();
		m_TickTimer.Start(m_TickMillis * 0.001f);
	}

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Play);
		m_CurrPlayMode = PlayMode.Play;
		SyncButtStates();
		OnSimulationStart();

		m_TickTimer.Paused = false;
		OnTickTimer_TimeOut();
	}
	
	private void OnPauseButt_Pressed()
	{
		Debug.Assert(m_CurrPlayMode != PlayMode.Off);
		m_CurrPlayMode = PlayMode.Off;
		SyncButtStates();
		OnSimulationEnd();
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
			PlayButt.Disabled = false;
			PauseButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;
		
		case PlayMode.Debug:
			PlayButt.Disabled = true;
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
		World.CleanUp();
		m_TickTimer.Paused = true;
		m_TickCount = 0;
	}

	private void AddTools()
	{
	}
}
