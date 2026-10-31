using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace ArFactory;

public partial class Editor : Godot.Node2D
{
	private enum State
	{
		Off,
		Placement,
		ConnectingSliders,
		PlacingMatchingTeleOutput,

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

	#region Actual Nodes
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
	public Simulator Simy => m_Simy;

	// Privates
	private State m_CurrState = State.Off;
	private float m_TickMillis;
	private readonly Simulator m_Simy = new();

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

	private void InitSimButts()
	{
		PlayButt.Pressed += OnPlayButt_Pressed;
		DebugButt.Pressed += OnDebugButt_Pressed;
		HaltButt.Pressed += OnHaltButt_Pressed;
	}

	private void InitSimSpeedSlider()
	{
		SpeedSlider.ValueChanged += OnSpeedSlider_ValueChanged;
		SpeedSlider.Value = m_Simy.GetTickRate();
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
			SetState(State.PlacingMatchingTeleOutput);
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
						MakePopup(mev, [("2x", 0), ("x+1", 1), ("x-1", 2), ("-x", 3)], id =>
						{
							updater.UpType = id switch
							{
								0 => UNUpdater.UpdateType.Double,
								1 => UNUpdater.UpdateType.Increment,
								2 => UNUpdater.UpdateType.Decrement,
								3 => UNUpdater.UpdateType.Negate,
								_ => throw new NotImplementedException(),
							};
						});
						break;
					
					case UNCombiner combi:
						MakePopup(mev, [("x + x + ...", 0), ("x * x * x...", 1)], id =>
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
							(slid.IsLocked() ? "Unlock" : "Lock", 0), 
							("Connect", 1),
							(!slid.IsToggle() ? "Make toggle" : null, 2),
							(!slid.IsLock() ? "Make lock" : null, 3),
							(!slid.IsNormal() ? slid.IsKey() ? "Disconnect" : "Make normal" : null, 4),
						], id =>
						{
							switch (id) {
							case 0: // Lock
								if (slid.IsLocked())
								{
									slid.Unlock();
								}
								else
								{
									slid.Lock();
								}
								break;
							
							case 1: // Connect to lock
								GD.Print("Connecting...");
								m_ConnecterUnit = slid;
								PlacementDirLabel.Text = "Connecting...";
								SetState(State.ConnectingSliders);
								break;
							
							case 2: // Toggle
								slid.RemoveExtension();
								slid.InstallDirectionToggle(UNSlider.SliderExtensionDirectionToggle.Kind._90);
								break;
							
							case 3: // Lock
								slid.RemoveExtension();
								slid.InstallRuntimeLock();
								break;

							case 4: // Normal
								slid.RemoveExtension();
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
				if (World.GetUnit(mouseGL) is { } u)
				{
					switch (u) {
					case UNTeleInput input:
					{
						if (input.GetConnectedOutput() is { } output)
						{
							World.DeleteUnit(output);
						}
						World.DeleteUnit(u);
						break;
					}
					case UNTeleOutput output:
					{
						if (output.GetConnectedInput() is { } input)
						{
							World.DeleteUnit(input);
						}
						World.DeleteUnit(u);
						break;
					}
					default:
						World.DeleteUnit(u);
						break;
					}
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
		case State.ConnectingSliders:
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
							connSlid.RemoveExtension();
							connSlid.MakeKey(tgtSlid);
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
		case State.PlacingMatchingTeleOutput:
		{
			if (ev is InputEventMouse mev && mev.IsPressed() && !mev.CtrlPressed && !mev.ShiftPressed)
			{
				if (mev.ButtonMask == MouseButtonMask.Left)
				{
					var mgloc = World.GetMouseGridLocation();
					if (Unit.CanFitIn(World, mgloc, Vector2I.One, Direction.East))
					{
						var teleIn = (m_ConnecterUnit as UNTeleInput)!;
						var teleOut = World.PlaceTeleOutput(mgloc, m_CurrUnitPlacement.Dir, 1);
						teleIn.AssignOutput(teleOut);
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

	private void MakePopup(Godot.InputEventMouse mev, IEnumerable<(string? Name, int id)> items, Action<int> pressFunc)
	{
		var men = new Godot.PopupMenu();
		foreach (var (name, id) in items.Where(it => !string.IsNullOrEmpty(it.Name)))
		{
			men.AddItem(name, id);
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
		case State.ConnectingSliders:
		case State.PlacingMatchingTeleOutput:
			PlacementDirLabel.Position = GetLocalMousePosition() + Vector2.One * 50;
			break;

		case State.Debug:
		case State.Play:
			DoPerFrame(dt);
			break;
		}
	}

	#region .Signal Handlers

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(m_CurrState is State.Off or State.Debug);
		switch (m_CurrState) {
		case State.Off:
			m_Simy.PlaySimulation();
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
		case State.ConnectingSliders:
		case State.PlacingMatchingTeleOutput:
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
