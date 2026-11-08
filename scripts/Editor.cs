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
		
		MoveTool,
		PlacingMovedUnit,

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
	private Godot.MarginContainer UnitPlacerMarCon => m_UnitPlacerMarCon!;
	private Godot.GridContainer UnitPlacerGCon => m_UnitPlacerGCon!;

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
	private Godot.MarginContainer? m_UnitPlacerMarCon;
	private Godot.GridContainer? m_UnitPlacerGCon;
	#endregion

	// Publics
	public float DefaultTickMillis = 300.0f;
	public Simulator Simy => m_Simy!;
	public UnitManager UniMan => m_UniMan!;

	// Privates
	private State m_CurrState = State.Off;
	private float m_TickMillis;
	private Simulator? m_Simy; // Not actually nullable.
	private UnitManager? m_UniMan;

	// Editor stuff
	private record struct UnitPlacement(Godot.ButtonGroup ButtGroup, Godot.Button? Butt, Direction Dir);
	private record UnitPickerButtInfo(Vector2I Dims, bool bEastWest, Func<Vector2I, Direction, Unit> PlacementFunc);

	public Godot.Label CursorLabel => m_CursorLabel!;
	private Godot.Label? m_CursorLabel;
	private UnitPlacement m_CurrUnitPlacement = new();
	private readonly Dictionary<string, UnitPickerButtInfo> m_UnitPickerButtInfos = [];
	private Unit? m_ConnecterUnit;
	
	// private Godot.ButtonGroup m_ToolboxButtGroup = new() { AllowUnpress = true };


	#region .Init

	public override void _EnterTree()
	{
		Debug.TreePtr = GetTree();
	}

	public override void _Ready()
	{
		GetAllNodes();
		InitUniMan();
		InitSimy();
		InitWorldCam();
		InitSimButts();
		InitSimSpeedSlider();
		InitUnitPicker();
		InitToolbox();
		InitDirPlacementLabel();
		SetState(State.Off, true);
	}

	private void GetAllNodes()
	{
		m_Cam = GetNode<Cam>("WorldPanel/Cam");
		m_World = GetNode<WorldPanel>("WorldPanel");
		m_PlayButt = GetNode<Godot.Button>("HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt");
		m_HaltButt = GetNode<Godot.Button>("HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt");
		m_DebugButt = GetNode<Godot.Button>("HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt");
		m_ToolsHBox = GetNode<Godot.HBoxContainer>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox");
		m_PlayHBox = GetNode<Godot.HBoxContainer>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox");
		m_TicksLabel = GetNode<Godot.Label>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel");
		m_SpeedSlider = GetNode<Godot.HSlider>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider");
		m_TickSpeedLabel = GetNode<Godot.Label>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel");
		m_UnitPlacerMarCon = GetNode<Godot.MarginContainer>("HUDLayer/UnitPlacerMarCon");
		m_UnitPlacerGCon = GetNode<Godot.GridContainer>("HUDLayer/UnitPlacerMarCon/Pan/MarginContainer/GCon");
	}

	private void InitSimy()
	{
		Debug.AssertNotNull(m_UniMan is not null);
		m_Simy = new(World, UniMan);
		m_Simy.SimulationStarted += OnSimulationStart;
		m_Simy.SimulationEnded += OnSimulationEnd;
	}

	private void InitWorldCam()
	{
		WorldCam.Position = (Vector2)GetWindow().Size * 0.5f;
		WorldCam.MaxZoom = 1.0f;
		WorldCam.MinZoom = 0.3f;
	}

	private void InitUniMan()
	{
		Debug.Assert(m_World is not null);
		m_UniMan = new(World);
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
		SpeedSlider.Value = Simy.Clock.GetTickRate();
	}

	private void InitUnitPicker()
	{
		m_CurrUnitPlacement.ButtGroup = new() { AllowUnpress = true };
		m_CurrUnitPlacement.ButtGroup.Pressed += OnUnitPicker_Pressed;
		AddUnitToPicker("||>", Vector2I.One, true, (l, d) => UniMan.PlaceSupplier(l, d, 1, [1, 2, 3, 4, 5, 6]));
		AddUnitToPicker(">>>", Vector2I.One, false, (l, d) => UniMan.PlaceSlider(l, d, 1));
		AddUnitToPicker("xX", Vector2I.One, true, (l, d) => UniMan.PlaceBin(l));
		AddUnitToPicker("?:", new Vector2I(1, 2), true, (l, d) => UniMan.PlaceBranch(l, d, 2, CmdIf.CheckType.Greater, 0));
		AddUnitToPicker("1>2", Vector2I.One, true, (l, d) => UniMan.PlaceUpdater(l, d, 2, UNUpdater.UpdateType.Double));
		AddUnitToPicker(">||", Vector2I.One, true, (l, d) => UniMan.PlaceDemander(l, d, [2, 4, 6, 8, 10, 12]));
		AddUnitToPicker(">^<", new Vector2I(1, 2), true, (l, d) => UniMan.PlaceCombiner(l, d, 2, BinaryOperation.Add));
		AddUnitToPicker("1>11", new Vector2I(1, 2), true, (l, d) => UniMan.PlaceCloner(l, d, 2));
		
		AddUnitToPicker("1>>", Vector2I.One, false, (l, d) =>
		{
			m_ConnecterUnit = UniMan.PlaceTeleInput(l, d, 1);
			CursorLabel.Text = $"Out: {m_CurrUnitPlacement.Dir}";
			SetState(State.PlacingMatchingTeleOutput);
			return m_ConnecterUnit;
		});
	}

	private void AddUnitToPicker(
		string text, Vector2I dims, bool bEastWest, Func<Vector2I, Direction, Unit> placementFunc)
	{
		var butt = new Godot.Button
		{

			Text = text,
			CustomMinimumSize = new Vector2I(0, 90),
			ButtonGroup = m_CurrUnitPlacement.ButtGroup,
			ToggleMode = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		UnitPlacerGCon.AddChild(butt);
		m_UnitPickerButtInfos.Add(butt.Name, new(dims, bEastWest, placementFunc));
	}

	private void InitDirPlacementLabel()
	{
		m_CursorLabel = new Godot.Label();
		AddChild(CursorLabel);
	}

	private void InitToolbox()
	{
		AddToolToToolbox("MOVE", State.MoveTool);
	}

	private void AddToolToToolbox(string text, State onClickState)
	{
		var butt = new Godot.Button
		{
			Text = text,
			CustomMinimumSize = new Vector2I(60, 0),
			ToggleMode = true,
			ButtonGroup = m_CurrUnitPlacement.ButtGroup,
		};
		ToolsHBox.AddChild(butt);
		butt.Pressed += () => 
		{
			CursorLabel.Text = onClickState.ToString();
			SetState(onClickState);
		};
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
					switch (UniMan.GetUnit(mgloc)) {
					case UNUpdater updater:
						MakePopup(mev, [("2x", 0), ("x+1", 1), ("x-1", 2), ("-x", 3)], id =>
						{
							updater.SetUpdateType(id switch
							{
								0 => UNUpdater.UpdateType.Double,
								1 => UNUpdater.UpdateType.Increment,
								2 => UNUpdater.UpdateType.Decrement,
								3 => UNUpdater.UpdateType.Negate,
								_ => throw new NotImplementedException(),
							});
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
								CursorLabel.Text = "Connecting...";
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
				if (UniMan.GetUnit(mouseGL) is { } u)
				{
					switch (u) {
					case UNTeleInput input:
					{
						if (input.GetConnectedOutput() is { } output)
						{
							UniMan.DeleteUnit(output);
						}
						UniMan.DeleteUnit(u);
						break;
					}
					case UNTeleOutput output:
					{
						if (output.GetConnectedInput() is { } input)
						{
							UniMan.DeleteUnit(input);
						}
						UniMan.DeleteUnit(u);
						break;
					}
					default:
						UniMan.DeleteUnit(u);
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
							var info = m_UnitPickerButtInfos[m_CurrUnitPlacement.Butt!.Name];
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
					var info = m_UnitPickerButtInfos[m_CurrUnitPlacement.Butt!.Name];
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
					CursorLabel.Text = m_CurrUnitPlacement.Dir.ToString();
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
					if (UniMan.GetUnit(World.GetMouseGridLocation()) is UNSlider tgtSlid)
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
						var teleOut = UniMan.PlaceTeleOutput(mgloc, m_CurrUnitPlacement.Dir, 1);
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
					UniMan.DeleteUnit(m_ConnecterUnit!);
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
					CursorLabel.Text = $"Out: {m_CurrUnitPlacement.Dir}";
				}
			}
			break;
		}
		case State.MoveTool:
		{
			if (ev is InputEventMouse mev && mev.IsPressed() && !mev.CtrlPressed && !mev.ShiftPressed)
			{
				if (mev.ButtonMask == MouseButtonMask.Left)
				{
					if (UniMan.GetUnit(World.GetMouseGridLocation()) is { } u)
					{
						m_ConnecterUnit = u;
						u.Deconstruct();
						GD.Print($"Picked up a {u.GetType().Name}");
						m_CurrUnitPlacement.Dir = u.Dir;
						CursorLabel.Text = $"Moving a {u.GetType().Name} ({m_CurrUnitPlacement.Dir})";
						SetState(State.PlacingMovedUnit);
					}
				}
				else if (mev.ButtonMask == MouseButtonMask.Right)
				{
					GD.Print("Cancelled move tool");
					SetState(State.Off);
					m_CurrUnitPlacement.ButtGroup.GetPressedButton().ButtonPressed = false;
				}
			}
			break;
		}
		case State.PlacingMovedUnit:
		{
			if (ev is InputEventMouseButton mev)
			{
				if (mev.ButtonIndex == MouseButton.Right)
				{
					if (mev.IsPressed())
					{
						GD.Print("Cancelled moving.");
						m_ConnecterUnit!.Construct(); // Where it originally was.
						CursorLabel.Text = State.MoveTool.ToString();
						SetState(State.MoveTool);
					}
				}
				else if (mev.ButtonIndex == MouseButton.Left)
				{
					if (mev.IsPressed() && !mev.CtrlPressed && !mev.ShiftPressed)
					{
						var mousePos = GetLocalMousePosition();
						if (World.GetWorldRect().HasPoint(mousePos))
						{
							Vector2I mgloc = World.GetMouseGridLocation();
							if (Unit.CanFitIn(World, mgloc, m_ConnecterUnit!))
							{
								GD.Print($"Moved a {m_ConnecterUnit!.GetType().Name} from {m_ConnecterUnit.GLocation} to {mgloc}");
								m_ConnecterUnit!.SetGLocation(mgloc);
								m_ConnecterUnit.SetDirection(m_CurrUnitPlacement.Dir);
								m_ConnecterUnit.Construct();
								CursorLabel.Text = State.MoveTool.ToString();
								SetState(State.MoveTool);
							}
							else
							{
								GD.Print("Nope, doesn't fit.");
							}
						}
					}
				}
			}
			else if (ev is InputEventKey kev)
			{
				if (kev.Keycode == Godot.Key.R && kev.IsPressed() && !kev.CtrlPressed && !kev.ShiftPressed)
				{
					if (m_ConnecterUnit!.CanFaceDir(Direction.North) is false)
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
					CursorLabel.Text = $"Moving a {m_ConnecterUnit!.GetType().Name} ({m_CurrUnitPlacement.Dir})";
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
		case State.MoveTool:
		case State.PlacingMovedUnit:
			CursorLabel.Position = GetLocalMousePosition() + Vector2.One * 50;
			break;

		case State.Debug:
		case State.Play:
			Simy.DoPerFrame(dt);
			break;
		}
	}

	#region .Signal Handlers

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(m_CurrState is State.Off or State.Debug);
		switch (m_CurrState) {
		case State.Off:
			Simy.Start();
			SetState(State.Play);
			break;


		case State.Debug:
			Simy.Resume();
			SetState(State.Play);
			break;

		case State.Play:
		default:
			throw new InvalidProgramException("Somehow pressed the play button in PlayMode.Play");
		}
	}

	private void OnHaltButt_Pressed()
	{
		Simy.Halt();
		SetState(State.Off);
	}

	private void OnDebugButt_Pressed()
	{
		switch (m_CurrState) {
		case State.Off:
			Simy.StartAndPause();
			SetState(State.Debug);
			break;

		case State.Play:
			Simy.Start();
			SetState(State.Debug);
			break;

		case State.Debug:
			if (!Simy.IsPaused()) // Spamming the debug key while the tick is in progress.
			{
				return;
			}

			Simy.TickOnce();
			break;

		default:
			throw new NotImplementedException("New PlayMode?");
		}
	}

	private void OnSpeedSlider_ValueChanged(double newVal)
	{
		Simy.Clock.SetTickRate((float)newVal);
		TickSpeedLabel.Text = $"{Simy.Clock.GetTickRate()} tick/s";
	}

	private void OnUnitPicker_Pressed(Godot.BaseButton baseButt)
	{
		Debug.AssertIs(baseButt, typeof(Godot.Button));
		var butt = (baseButt as Godot.Button)!;
		SetState(State.Placement);
		m_CurrUnitPlacement.Butt = butt;
		m_CurrUnitPlacement.Dir = Direction.East;

		CursorLabel.Text = m_CurrUnitPlacement.Dir.ToString();
	}

	#endregion .Signal Handlers


	private void Reset()
	{
		World.Clear();
		Simy.Reset();
	}

	private void SyncButtStates()
	{
		// Hide everything first.
		// I could the extra marshalling by using in-between booleans but who cares bruh, this only happens once in 
		// a while.
		PlayButt.Disabled = true;
		HaltButt.Disabled = true;
		DebugButt.Disabled = true;
		PlayHBox.Hide();
		ToolsHBox.Hide();
		CursorLabel.Hide();
		UnitPlacerMarCon.Hide();

		switch (m_CurrState) {
		case State.Off:
			PlayButt.Disabled = false;
			DebugButt.Disabled = false;
			ToolsHBox.Show();
			UnitPlacerMarCon.Show();
			break;

		case State.Placement:
		case State.ConnectingSliders:
		case State.PlacingMatchingTeleOutput:
		case State.MoveTool:
		case State.PlacingMovedUnit:
			ToolsHBox.Show();
			UnitPlacerMarCon.Show();
			CursorLabel.Show();
			break;

		case State.Play:
			HaltButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			break;

		case State.Debug:
			PlayButt.Disabled = false;
			HaltButt.Disabled = false;
			DebugButt.Disabled = false;
			PlayHBox.Show();
			break;

		default:
			throw new NotImplementedException("New PlayMode?");
		}
	}

	private void OnSimulationStart()
	{
		Debug.Assert(m_CurrState is State.Off);
		SetState(Simy.IsDebugging() ? State.Debug : State.Play);
		UpdateTickCountLabel();
	}

	private void OnSimulationEnd()
	{
		Debug.Assert(m_CurrState is State.Play or State.Debug);
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
		TicksLabel.Text = $"tick: {Simy.Clock.GetTicksSinceStart()}";
	}
}
