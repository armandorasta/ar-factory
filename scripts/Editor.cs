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
		public ExecState m_TState;

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

	private enum State
	{
		Off,
		Placement,
		ConnectingLock,
		ConnectingTeleporter,

		Play,
		Debug,
	}

	// Nodes
	public Cam WorldCam => m_Cam!;
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
	private Cam? m_Cam;
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
	private record ToolBoxButtInfo(Vector2I Dims, bool bEastWest, Func<Vector2I, Direction, Unit> PlacementFunc);

	public Godot.Label PlacementDirLabel => m_PlacementDirLabel!;
	private Godot.Label? m_PlacementDirLabel;

	private UnitPlacement m_CurrUnitPlacement = new();
	private readonly Dictionary<string, ToolBoxButtInfo> m_ToolboxButtInfos = [];

	private Unit? m_ConnecterUnit;


	#region .Init

	public override void _EnterTree()
	{
		Debug.TreePtr = GetTree();
	}

	public override void _Ready()
	{
		GetAllNodes();
		WorldCam.Position = World!.Size * 0.5f;
		WorldCam.MaxZoom = 1.0f;
		WorldCam.MinZoom = 0.3f;
		InitSimy();
		InitSimButts();
		InitSimSpeedSlider();
		InitTools();
		InitDirPlacementLabel();
		SetState(State.Off, true);
	}

	private void GetAllNodes()
	{
		m_Cam = GetNode<Cam>("WorldPanel/Cam");
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
		AddTool("||>", Vector2I.One, true, (l, d) => World.PlaceSupplier(l, d, 1, [1, 2, 3, 4, 5, 6]));
		AddTool(">>>", Vector2I.One, false, (l, d) => World.PlaceSlider(l, d, 1));
		AddTool("xX", Vector2I.One, true, (l, d) => World.PlaceBin(l));
		AddTool("?:", new Vector2I(1, 2), true, (l, d) => World.PlaceBranch(l, d, 2, CmdIf.CheckType.Greater, 0));
		AddTool("1>2", Vector2I.One, true, (l, d) => World.PlaceUpdater(l, d, 2, UNUpdater.UpdateType.Double));
		AddTool(">||", Vector2I.One, true, (l, d) => World.PlaceDemander(l, d, [2, 4, 6, 8, 10, 12]));
		AddTool(">^<", new Vector2I(1, 2), true, (l, d) => World.PlaceCombiner(l, d, 2, BinaryOperation.Add));
		AddTool("1>11", new Vector2I(1, 2), true, (l, d) => World.PlaceCloner(l, d, 2));
		
		AddTool("1>>", Vector2I.One, false, (l, d) => 
		{
			m_ConnecterUnit = World.PlaceTeleInput(l, d, 1);
			PlacementDirLabel.Text = $"Out: {m_CurrUnitPlacement.Dir}";
			SetState(State.ConnectingTeleporter);
			return m_ConnecterUnit;
		});
	}

	private void InitDirPlacementLabel()
	{
		m_PlacementDirLabel = new Godot.Label();
		AddChild(PlacementDirLabel);
	}

	private void AddTool(string text, Vector2I dims, bool bEastWest, Func<Vector2I, Direction, Unit> placementFunc)
	{
		var butt = new Godot.Button
		{
			Text = text,
			CustomMinimumSize = new Vector2I(60, 0),
			ButtonGroup = m_CurrUnitPlacement.ButtGroup,
			ToggleMode = true,
		};
		ToolsHBox.AddChild(butt);
		m_ToolboxButtInfos.Add(butt.Name, new(dims, bEastWest, placementFunc));
	}

	#endregion // Init


	public override void _UnhandledInput(InputEvent ev)
	{
		switch (m_CurrState) {
		case State.Off:
		{
			if (ev is InputEventMouseButton mev)
			{
				if (mev.IsPressed() && mev.ButtonMask == MouseButtonMask.Right && !mev.CtrlPressed && !mev.ShiftPressed)
				{
					var mgloc = World.PosToGrid(World.GetLocalMousePosition());
					switch (World.GetUnit(mgloc)) {
					case UNUpdater updater:
						MakePopup(mev, ["2x", "x+1", "-x"], id =>
						{
							updater.UpType = id switch
							{
								0 => UNUpdater.UpdateType.Double,
								1 => UNUpdater.UpdateType.Increment,
								2 => UNUpdater.UpdateType.Negate,
								_ => throw new NotImplementedException(),
							};
						});
						break;
					
					case UNCombiner combi:
						MakePopup(mev, ["x + x + ...", "x * x * x..."], id =>
						{
							combi.Operation = id switch
							{
								0 => BinaryOperation.Add,
								1 => BinaryOperation.Mul,
								_ => throw new NotImplementedException(),
							};
						});
						break;
					
					case UNSlider slid:
						MakePopup(mev, [
							slid.IsLocked()    ? "Unlock" : "Lock", 
							"Connect",
							slid.HasSyncPirimInstalled() ? "Disconnect" : null
						], id =>
						{
							switch (id) {
							case 0:
								if (slid.IsLocked())
								{
									slid.Unlock();
								}
								else
								{
									slid.Lock();
								}
								break;
							
							case 1:
								GD.Print("Connecting lock...");
								m_ConnecterUnit = slid;
								PlacementDirLabel.Text = "Connecting lock...";
								SetState(State.ConnectingLock);
								break;
							
							case 2:
								slid.DisconnectSyncPirim(bBothWays: true);
								break;
							
							default:
								throw new NotImplementedException();
							}
						});
						break;
					
					case UNTeleInput teleIn:
						MakePopup(mev, [
							"Connect to output", 
							teleIn.HasMatchingOutput() ? "Disconnect" : null
						], id =>
						{
							switch (id) {
							case 0:
								GD.Print("Connecting to output...");
								m_ConnecterUnit = teleIn;
								PlacementDirLabel.Text = "Connecting to output...";
								SetState(State.ConnectingTeleporter);
								break;

							case 1:
								teleIn.DisconnectOutput(bBothWays: true);
								break;

							default:
								throw new NotImplementedException();
							}
						});
						break;
					}
				}
			}
			
			if (Input.GetMouseButtonMask() == MouseButtonMask.Right && Input.IsKeyPressed(Godot.Key.Ctrl))
			{
				var mouseGL = World.PosToGrid(GetLocalMousePosition());
				if (World.GetUnit(mouseGL) is Unit u)
				{
					World.DeleteUnit(u);
				}
			}
			break;
		}
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
					}
				}
				else if (mev.ButtonIndex == MouseButton.Left)
				{
					if (mev.IsPressed() && !mev.CtrlPressed && !mev.ShiftPressed)
					{
						var mousePos = GetLocalMousePosition();
						if (World.GetWorldRect().HasPoint(mousePos))
						{
							var info = m_ToolboxButtInfos[m_CurrUnitPlacement.Butt!.Name];
							Vector2I mgloc = World.GetMouseGridLocation();
							if (Unit.CanFitIn(World, mgloc, info.Dims, m_CurrUnitPlacement.Dir))
							{
								info.PlacementFunc.Invoke(mgloc, m_CurrUnitPlacement.Dir);
							}
						}
					}
				}
			}
			else if (ev is InputEventKey kev)
			{
				if (kev.Keycode == Godot.Key.R && kev.IsPressed() && !kev.CtrlPressed && !kev.ShiftPressed)
				{
					var info = m_ToolboxButtInfos[m_CurrUnitPlacement.Butt!.Name];
					if (info.bEastWest)
					{
						m_CurrUnitPlacement.Dir = m_CurrUnitPlacement.Dir switch
						{
							Direction.East => Direction.West,
							Direction.West or _ => Direction.East,
						};
					}
					else
					{
						m_CurrUnitPlacement.Dir = m_CurrUnitPlacement.Dir.Rotate90();
					}
					PlacementDirLabel.Text = m_CurrUnitPlacement.Dir.ToString();
				}
			}
			break;
		}
		case State.ConnectingLock:
		{
			if (ev is InputEventMouse mev && mev.IsPressed() && !mev.CtrlPressed && !mev.ShiftPressed)
			{
				if (mev.ButtonMask == MouseButtonMask.Left)
				{
					if (World.GetUnit(World.GetMouseGridLocation()) is UNSlider tgtSlid)
					{
						if (tgtSlid == m_ConnecterUnit)
						{
							GD.Print("Hit the connecting slider bruh...");
						}
						else
						{
							var connSlid = (m_ConnecterUnit as UNSlider)!;
							if (connSlid.HasSyncPirimInstalled())
							{
								connSlid.DisconnectSyncPirim(bBothWays: true);
							}
							tgtSlid.InstallLock(connSlid!);
							connSlid.InstallKey(tgtSlid);
							GD.Print($"Connected slider on {connSlid!.GLocation} to the one on {tgtSlid.GLocation}");
							SetState(State.Off);
							m_ConnecterUnit = null;
						}
					}
					else
					{
						GD.Print("Hit nothing and cancelled anyway");
						SetState(State.Off);
					}
				}
				else if (mev.ButtonMask == MouseButtonMask.Right)
				{
					GD.Print("Lock cancelled");
					SetState(State.Off);
					m_ConnecterUnit = null;
				}
			}
			break;
		}
		case State.ConnectingTeleporter:
		{
			if (ev is InputEventMouse mev && mev.IsPressed() && !mev.CtrlPressed && !mev.ShiftPressed)
			{
				if (mev.ButtonMask == MouseButtonMask.Left)
				{
					var mgloc = World.GetMouseGridLocation();
					if (Unit.CanFitIn(World, mgloc, Vector2I.One, Direction.East))
					{
						var teleIn = (m_ConnecterUnit as UNTeleInput)!;
						var teleOut = World.PlaceTeleOutput(mgloc, m_CurrUnitPlacement.Dir, 0);
						teleIn.DisconnectOutput(bBothWays: true);
						teleIn.AssignOutput(teleOut);
						teleOut.AssignInput(teleIn);
						m_ConnecterUnit = null;
						SetState(State.Placement);
					}
					else
					{
						GD.Print("Nope, doesn't fit");
					}
				}
				else if (mev.ButtonMask == MouseButtonMask.Right)
				{
					World.DeleteUnit(m_ConnecterUnit!);
					GD.Print("Cancelled");
					m_ConnecterUnit = null;
					SetState(State.Placement);
				}
			}
			else if (ev is InputEventKey kev)
			{
				if (kev.Keycode == Godot.Key.R && kev.IsPressed() && !kev.CtrlPressed && !kev.ShiftPressed)
				{
					m_CurrUnitPlacement.Dir = m_CurrUnitPlacement.Dir.Rotate90();
					PlacementDirLabel.Text = $"Out: {m_CurrUnitPlacement.Dir}";
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

	private void MakePopup(Godot.InputEventMouse mev, IEnumerable<string?> items, Action<int> pressFunc)
	{
		var men = new Godot.PopupMenu();
		foreach (var item in items.Where(it => it is not null))
		{
			men.AddItem(item);
		}
		men.IdPressed += id => pressFunc.Invoke((int)id);
		men.PopupHide += () =>
		{
			RemoveChild(men);
			men.QueueFree();
		};
		AddChild(men);
		men.Position = (Vector2I)mev.GlobalPosition;
		men.Popup();
	}

	public override void _Process(double dt)
	{
		switch (m_CurrState) {
		case State.Off:
			break;

		case State.Placement:
		case State.ConnectingLock:
		case State.ConnectingTeleporter:
			PlacementDirLabel.Position = GetLocalMousePosition() + Vector2.One * 50;
			break;

		case State.Debug:
		case State.Play:
			DoPerFrame(dt);
			break;
		}
	}

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
			m_Clock.HaltSimulation();
			GD.PrintErr($"Expected {ticker.m_TState.ExpectedValue} but found {ticker.m_TState.RecievedValue} on {ticker.m_TState.GLocation}");
			break;

		default:
			throw new NotImplementedException("New ExecState.Status?");
		}

		switch (World.CheckForErrorsThisTick()) {
		case WorldPanel.TickState.NothingSus:
			break;

		case WorldPanel.TickState.RanOutOfItems:
			m_Clock.HaltSimulation();
			GD.PrintErr("RAN OUT OF ITEMS!!");
			break;
		
		case WorldPanel.TickState.SuccessfulRun:
			m_Clock.HaltSimulation();
			GD.PrintErr("WIN!");
			break;

		default:
			throw new NotImplementedException("New WorldPanel.TickState?");
		}

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
			PlacementDirLabel.Hide();
			break;

		case State.Placement:
		case State.ConnectingLock:
		case State.ConnectingTeleporter:
			PlayButt.Disabled = true;
			HaltButt.Disabled = true;
			DebugButt.Disabled = true;
			PlayHBox.Hide();
			ToolsHBox.Show();
			PlacementDirLabel.Show();
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

	private void SetState(State newPlayMode, bool bForce = false)
	{
		if (!bForce && newPlayMode == m_CurrState) // Avoid unecessary marshaling
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
