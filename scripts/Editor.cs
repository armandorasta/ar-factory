using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace ArFactory;

public partial class Editor : Godot.Node2D
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
			StillGoing,
			SuccessfulRun,
			ManualHalt,
			RecievedIncorrectValue,
		}
		
		public Status Stat = Status.StillGoing;
		public int ExpectedValue = Item.MaxValue + 1;
		public int RecievedValue = Item.MaxValue + 1;

		public void Succeed() => Stat = Status.SuccessfulRun;	
		public void ManuallyHalt() => Stat = Status.ManualHalt;	
		public void Crash(int expected, int found)
		{
			Debug.Assert(Item.IsValueWithinRange(expected));
			Debug.Assert(Item.IsValueWithinRange(found));
			Stat = Status.RecievedIncorrectValue;
			ExpectedValue = expected;
			RecievedValue = found;
		}
	}

	public class OnTickMediator(WorldPanel world, CommandRunner runner)
	{
		public CommandRunner.OnTickPender Pender = new(runner);
		public WorldPanel World = world;
		private ExecState m_TState;
		
		public void CrashSimulation(int expected, int found) => m_TState.Crash(expected, found);
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

	private enum State
	{
		Off,
		Placement,
		Play,
		Debug,
	}

	// Nodes
	public Godot.Camera2D Cam => m_Cam!;
	public WorldPanel World => m_World!;
	public Godot.Button PlayButt => m_PlayButt!;
	public Godot.Button HaltButt => m_HaltButt!;
	public Godot.Button DebugButt => m_DebugButt!;
	public Godot.HBoxContainer ToolsHBox => m_ToolsHBox!;
	public Godot.HBoxContainer PlayHBox => m_PlayHBox!;
	public Godot.Label TicksLabel => m_TicksLabel!;
	public Godot.HSlider SpeedSlider => m_SpeedSlider!;
	public Godot.Label TickSpeedLabel => m_TickSpeedLabel!;

	#region
	private Godot.Camera2D? m_Cam;
	private WorldPanel? m_World;
	private Godot.Button? m_PlayButt;
	private Godot.Button? m_HaltButt;
	private Godot.Button? m_DebugButt;
	private Godot.HBoxContainer? m_ToolsHBox;
	private Godot.HBoxContainer? m_PlayHBox;
	private Godot.Label? m_TicksLabel;
	private Godot.HSlider? m_SpeedSlider;
	private Godot.Label? m_TickSpeedLabel;
	#endregion

	// Publics
	public float DefaultTickMillis = 300.0f;
	public SimulatorClock Clock => m_Clock;

	// Privates
	private State m_CurrState = State.Off;
	private float m_TickMillis;
	private readonly SimulatorClock m_Clock = new();
	private readonly CommandRunner m_Runner = new();

	// Editor stuff
	private record struct UnitPlacement(Godot.ButtonGroup ButtGroup, Godot.Button? Butt, Direction Dir);
	private record ToolBoxButtInfo(Vector2I Dims, Action<Vector2I, Direction> PlacementFunc);

	public Godot.Label PlacementDirLabel => m_PlacementDirLabel!;
	private Godot.Label? m_PlacementDirLabel;

	private UnitPlacement m_CurrUnitPlacement = new();
	private readonly Dictionary<string, ToolBoxButtInfo> m_ToolboxButtInfos = [];


	#region .Init

	public override void _EnterTree()
	{
		Debug.TreePtr = GetTree();
	}

	public override void _Ready()
	{
		GetAllNodes();
		Cam!.Position = World!.Size * 0.5f;
		InitSimy();
		InitSimButts();
		InitSimSpeedSlider();
		InitTools();
		InitDirPlacementLabel();
	}

	private void GetAllNodes()
	{
		m_Cam = GetNode<Godot.Camera2D>("WorldPanel/Cam");
		m_World = GetNode<WorldPanel>("WorldPanel");
		m_PlayButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt");
		m_HaltButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt");
		m_DebugButt = GetNode<Godot.Button>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt");
		m_ToolsHBox = GetNode<Godot.HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox");
		m_PlayHBox = GetNode<Godot.HBoxContainer>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox");
		m_TicksLabel = GetNode<Godot.Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel");
		m_SpeedSlider = GetNode<Godot.HSlider>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider");
		m_TickSpeedLabel = GetNode<Godot.Label>("WorldPanel/HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel");
	}

	private void InitSimy()
	{
		AddChild(Clock);
		m_Clock.Connect(SimulatorClock.SignalName.SimulationStarted, Callable.From(OnSimulationStart));
		m_Clock.Connect(SimulatorClock.SignalName.SimulationEnded, Callable.From(OnSimulationEnd));
		m_Clock.Connect(SimulatorClock.SignalName.TickStarted, Callable.From(OnTick));
		m_Clock.Connect(SimulatorClock.SignalName.TickEnded, Callable.From(OnEdge));
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
		SpeedSlider.Value = m_Clock.GetTickRate();
	}

	private void InitTools()
	{
		m_CurrUnitPlacement.ButtGroup = new() { AllowUnpress = true };
		m_CurrUnitPlacement.ButtGroup.Pressed += OnToolsHBox_Pressed;
		AddTool("||>", Vector2I.One, (l, d) => World.PlaceSupplier(l, d, 1, [1, 2, 3, 4, 5, 6]));
		AddTool(">>>", Vector2I.One, (l, d) => World.PlaceSlider(l, d, 1));
		AddTool("xX", Vector2I.One, (l, d) => World.PlaceBin(l));
		AddTool("?:", new Vector2I(1, 2), (l, d) => World.PlaceBranch(l, d, CmdIf.CheckType.Greater, 3));
		AddTool("1>2", Vector2I.One, (l, d) => World.PlaceUpdater(l, d, 2, UNUpdater.UpdateType.Double));
	}

	private void InitDirPlacementLabel()
	{
		m_PlacementDirLabel = new Godot.Label();
		AddChild(PlacementDirLabel);
		PlacementDirLabel.Hide();
	}

	private void AddTool(string text, Vector2I dims, Action<Vector2I, Direction> placementFunc)
	{
		var butt = new Godot.Button
		{
			Text = text,
			CustomMinimumSize = new Vector2I(80, 0),
			ButtonGroup = m_CurrUnitPlacement.ButtGroup,
			ToggleMode = true,
		};
		ToolsHBox.AddChild(butt);
		m_ToolboxButtInfos.Add(butt.Name, new(dims, placementFunc));
	}

	#endregion // Init


	public override void _UnhandledInput(InputEvent ev)
	{
		switch (m_CurrState) {
		case State.Placement:
		{
			if (ev is InputEventMouseButton mev)
			{
				if (mev.ButtonIndex == MouseButton.Right)
				{
					if (mev.IsPressed())
					{
						SetState(State.Off);
						m_CurrUnitPlacement.Butt!.ButtonPressed = false;
						PlacementDirLabel.Hide();
					}
				}
				else if (mev.ButtonIndex == MouseButton.Left)
				{
					if (mev.IsPressed())
					{
						var mousePos = GetLocalMousePosition();
						if (World.GetWorldRect().HasPoint(mousePos))
						{
							var info = m_ToolboxButtInfos[m_CurrUnitPlacement.Butt!.Name];
							Vector2I gloc = World.PosToGrid(mousePos);
							if (Unit.CanFitIn(World, gloc, info.Dims, m_CurrUnitPlacement.Dir))
							{
								info.PlacementFunc.Invoke(World.PosToGrid(mousePos), m_CurrUnitPlacement.Dir);
							}
							else
							{
								GD.Print($"Nope, a {info.Dims.X}x{info.Dims.Y} unit can't fit on {gloc} buddy");
							}
						}
					}
				}
			}
			else if (ev is InputEventKey kev)
			{
				if (kev.Keycode == Key.E && kev.IsPressed())
				{
					m_CurrUnitPlacement.Dir = m_CurrUnitPlacement.Dir.Rotate90();
					PlacementDirLabel.Text = m_CurrUnitPlacement.Dir.ToString();
				}
			}
			break;
		}
		case State.Off:
		{
			if (Input.IsMouseButtonPressed(MouseButton.Right) && Input.IsKeyPressed(Key.Ctrl))
			{
				var mouseGL = World.PosToGrid(GetLocalMousePosition());
				if (World.GetUnit(mouseGL) is Unit u)
				{
					World.DeleteUnit(u);
				}
			}
			break;
		}
		case State.Play:
		case State.Debug:
		default:
			break;
		}
	}

	public override void _Process(double dt)
	{
		switch (m_CurrState) {
		case State.Off:
			break;

		case State.Placement:
			PlacementDirLabel.Position = GetLocalMousePosition() + new Vector2(-50, -50);
			break;

		case State.Debug:
		case State.Play:
			DoPerFrame(dt);
			break;
		}
	}

#if TEST
	/// <summary>
	/// Used for testing for exceptions. Otherwise I just use the method inside of <see cref="SimulatorClock"/> directly.
	/// This does the same exact thing as the Simulator method, but kinda bloats this class with simulation code.
	/// </summary>
	public async Task TickOnceForTestingAsync()
	{
		Debug.Assert(m_CurrState is State.Debug);
		if (!Clock.IsPaused())
		{
			await Clock.EndTickAsync();
		}

		Clock.TickOnceNoSignal();
		OnTick();
	}
#endif

	public void OnTick()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);

		World.OnTick(new PendProxy(m_Runner));
		var ticker = new OnTickMediator(World, m_Runner);
		m_Runner.OnTick(ticker);

		// TODO: CHECK FOR HALTS and PAUSES

		UpdateTickCountLabel();
	}

	public void DoPerFrame(double dt)
	{
		var ticker = new PerFrameMediator(World, m_Clock, dt);
		m_Runner.DoPerFrame(ticker);
	}

	public void OnEdge()
	{
		var ticker = new OnEdgeMediator(World);
		m_Runner.OnEdge(ticker);
	}

	#region .Signal Handlers

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(m_CurrState is State.Off or State.Debug);
		switch (m_CurrState) {
		case State.Off:
			m_Clock.PlaySimulation();
			SetState(State.Play);
			break;


		case State.Debug:
			m_Clock.ResumeSimulation();
			SetState(State.Play);
			break;

		case State.Play:
		default:
			throw new InvalidProgramException("Somehow pressed the play button in PlayMode.Play");
		}
	}

	private void OnHaltButt_Pressed()
	{
		m_Clock.HaltSimulation();
		SetState(State.Off);
	}

	private void OnDebugButt_Pressed()
	{
		switch (m_CurrState) {
		case State.Off:
			m_Clock.PlaySimulationInDebug();
			SetState(State.Debug);
			break;

		case State.Play:
			m_Clock.PauseSimulation();
			SetState(State.Debug);
			break;

		case State.Debug:
			if (!m_Clock.IsPaused()) // Spamming the debug key while the tick is in progress.
			{
				return;
			}

			m_Clock.TickOnce();
			break;

		default:
			throw new NotImplementedException("New PlayMode?");
		}
	}

	private void OnSpeedSlider_ValueChanged(double newVal)
	{
		m_Clock.SetTickRate((float)newVal);
		TickSpeedLabel.Text = $"{m_Clock.GetTickRate()} tick/s";
	}

	private void OnToolsHBox_Pressed(Godot.BaseButton baseButt)
	{
		Debug.AssertIs(baseButt, typeof(Godot.Button));
		var butt = (baseButt as Godot.Button)!;
		SetState(State.Placement);
		m_CurrUnitPlacement.Butt = butt;
		m_CurrUnitPlacement.Dir = Direction.East;

		PlacementDirLabel.Show();
		PlacementDirLabel.Text = m_CurrUnitPlacement.Dir.ToString();
	}

	#endregion .Signal Handlers

	private void Reset()
	{
		World.Reset();
		m_Runner.Reset();
	}

	private void SyncButtStates()
	{
		switch (m_CurrState) {
		case State.Off:
			PlayButt.Disabled = false;
			HaltButt.Disabled = true;
			DebugButt.Disabled = false;
			PlayHBox.Hide();
			ToolsHBox.Show();
			break;

		case State.Placement:
			PlayButt.Disabled = true;
			HaltButt.Disabled = true;
			DebugButt.Disabled = true;
			PlayHBox.Hide();
			ToolsHBox.Show();
			break;

		case State.Play:
			PlayButt.Disabled = true;
			HaltButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;

		case State.Debug:
			PlayButt.Disabled = false;
			HaltButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			ToolsHBox.Hide();
			break;

		default:
			throw new NotImplementedException("New PlayMode?");
		}
	}

	/// <summary>
	/// Adds a slide command blocked by another item in the way only, all other types crash the program in debug.
	/// </summary>
	public void QueueBlockedSlideCmdByAnotherItem(CmdSlide cmd)
	{
		m_Runner.QueueBlockedSlideCmdByAnotherItem(World, cmd);
	}

	private void OnSimulationStart()
	{
		Debug.Assert(m_CurrState is State.Off);
		SetState(m_Clock.IsDebugging() ? State.Debug : State.Play);
		UpdateTickCountLabel();
	}

	private void OnSimulationEnd()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
		World.CleanUpAfterSim();
		m_Runner.Reset();
		SetState(State.Off);
	}

	private void SetState(State newPlayMode)
	{
		if (newPlayMode == m_CurrState) // Avoid unecessary marshaling
		{
			return;
		}

		m_CurrState = newPlayMode;
		SyncButtStates();
	}

	private void UpdateTickCountLabel()
	{
		TicksLabel.Text = $"tick: {m_Clock.GetTicksSinceStart()}";
	}
}
