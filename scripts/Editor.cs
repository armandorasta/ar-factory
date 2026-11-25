using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ArFactory.Serialization;
using Godot;

namespace ArFactory;

public partial class Editor : Godot.Node2D
{
	// Constants
	public const string AutoSaveFilePath = "res://resources/saves/Level.json";

	// Nodes
	public Cam WorldCam => m_Cam!;
	public WorldPanel World => m_World!;
	public Godot.Button PlayButt => m_PlayButt!;
	public Godot.Button HaltButt => m_HaltButt!;
	public Godot.Button DebugButt => m_DebugButt!;
	public Godot.Button SaveButt => m_SaveButt!;
	public Godot.Button LoadButt => m_LoadButt!;
	public Godot.Button DimButt => m_DimButt!;
	public Godot.HBoxContainer ToolsHBox => m_ToolsHBox!;
	public Godot.HBoxContainer PlayHBox => m_PlayHBox!;
	public Godot.Label TicksLabel => m_TicksLabel!;
	public Godot.HSlider SpeedSlider => m_SpeedSlider!;
	public Godot.Label TickSpeedLabel => m_TickSpeedLabel!;
	public Godot.MarginContainer UnitPlacerMarCon => m_UnitPlacerMarCon!;
	public Godot.GridContainer UnitPlacerGCon => m_UnitPlacerGCon!;
	public Godot.LineEdit XLiEd => m_XLiEd!;
	public Godot.LineEdit YLiEd => m_YLiEd!;

	#region Actual Nodes
	private Cam? m_Cam;
	private WorldPanel? m_World;
	private Godot.Button? m_PlayButt;
	private Godot.Button? m_HaltButt;
	private Godot.Button? m_DebugButt;
	private Godot.Button? m_SaveButt;
	private Godot.Button? m_LoadButt;
	private Godot.Button? m_DimButt;
	private Godot.HBoxContainer? m_ToolsHBox;
	private Godot.HBoxContainer? m_PlayHBox;
	private Godot.Label? m_TicksLabel;
	private Godot.HSlider? m_SpeedSlider;
	private Godot.Label? m_TickSpeedLabel;
	private Godot.MarginContainer? m_UnitPlacerMarCon;
	private Godot.GridContainer? m_UnitPlacerGCon;
	private Godot.LineEdit? m_XLiEd;
	private Godot.LineEdit? m_YLiEd;
	#endregion

	// Publics
	public float DefaultTickMillis = 300.0f;
	public Simulator Simy => m_Simy!;
	public UnitManager UniMan => m_UniMan!;

	// Privates
	private EditorState m_CurrState;
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

	public Editor()
	{
		m_CurrState = new EditorStateIdle(this);
	}

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
		InitDimButts();
		InitSimSpeedSlider();
		InitUnitPicker();
		InitToolbox();
		InitDirPlacementLabel();
	}

	private void GetAllNodes()
	{
		m_Cam = GetNode<Cam>("WorldPanel/Cam");
		m_World = GetNode<WorldPanel>("WorldPanel");
		m_PlayButt = GetNode<Godot.Button>("HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PlayButt");
		m_HaltButt = GetNode<Godot.Button>("HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/PauseButt");
		m_DebugButt = GetNode<Godot.Button>("HUDLayer/MarginContainer/HBoxContainer/ButtPanel/MarginContainer/ButtsHBox/DebugButt");
		m_SaveButt = GetNode<Godot.Button>("HUDLayer/TopPanel/MarCon/HBox/SaveButt");
		m_LoadButt = GetNode<Godot.Button>("HUDLayer/TopPanel/MarCon/HBox/LoadButt");
		m_DimButt = GetNode<Godot.Button>("HUDLayer/TopPanel/MarCon/HBox/DimButt");
		m_ToolsHBox = GetNode<Godot.HBoxContainer>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/ToolsHBox");
		m_PlayHBox = GetNode<Godot.HBoxContainer>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox");
		m_TicksLabel = GetNode<Godot.Label>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TicksLabel");
		m_SpeedSlider = GetNode<Godot.HSlider>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/SpeedHSlider");
		m_TickSpeedLabel = GetNode<Godot.Label>("HUDLayer/MarginContainer/HBoxContainer/FactoryPan/MarginContainer/PlayHBox/TickSpeedLabel");
		m_UnitPlacerMarCon = GetNode<Godot.MarginContainer>("HUDLayer/UnitPlacerMarCon");
		m_UnitPlacerGCon = GetNode<Godot.GridContainer>("HUDLayer/UnitPlacerMarCon/Pan/MarginContainer/GCon");
		m_XLiEd = GetNode<Godot.LineEdit>("HUDLayer/TopPanel/MarCon/HBox/XLiEd");
		m_YLiEd = GetNode<Godot.LineEdit>("HUDLayer/TopPanel/MarCon/HBox/YLiEd");
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
		SaveButt.Pressed += OnSaveButt_Pressed;
		LoadButt.Pressed += OnLoadButt_Pressed;
	}

	private void InitDimButts()
	{
		DimButt.Pressed += OnDimButt_Pressed;
		World.DimensionsChanged += OnWorld_DimensionsChanged;
		UpdateDimInterface(World.Dimensions);
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
		AddUnitToPicker("Supplier", Vector2I.One, true, 
			"res://resources/supplier.png", 
			(l, d) => UniMan.PlaceSupplier(l, d, 1, [1, 2, 3, 4, 5, 6]));
		AddUnitToPicker("Slider", Vector2I.One, false, 
			"res://resources/belt.png", 
			(l, d) => UniMan.PlaceSlider(l, d, 1));
		AddUnitToPicker("Bin", Vector2I.One, true, 
			"res://resources/bin.png", 
			(l, d) => UniMan.PlaceBin(l));
		AddUnitToPicker("Branch", new Vector2I(1, 2), true, 
			"res://resources/branch.png", 
			(l, d) => UniMan.PlaceBranch(l, d, 2, CmdIf.CheckType.Greater, 0));
		AddUnitToPicker("Updater", Vector2I.One, true, 
			"res://resources/updater.png", 
			(l, d) => UniMan.PlaceUpdater(l, d, 2, UNUpdater.UpdateType.Double));
		AddUnitToPicker("Demander", Vector2I.One, true, 
			"res://resources/demander.png", 
			(l, d) => UniMan.PlaceDemander(l, d, [2, 4, 6, 8, 10, 12]));
		AddUnitToPicker("Combiner", new Vector2I(1, 2), true, 
			"res://resources/combiner.png", 
			(l, d) => UniMan.PlaceCombiner(l, d, 2, BinaryOperation.Add));
		AddUnitToPicker("Cloner", new Vector2I(1, 2), true, 
			"res://resources/cloner.png", 
			(l, d) => UniMan.PlaceCloner(l, d, 2));
		
		AddUnitToPicker("Teleporter", Vector2I.One, false, 
			"res://resources/belt.png", (l, d) =>
			{
				m_ConnecterUnit = UniMan.PlaceTeleInput(l, d, 1);
				CursorLabel.Text = $"Out: {m_CurrUnitPlacement.Dir}";
				SetState(new EditorStatePlacingMatchingTeleOutput(this));
				return m_ConnecterUnit;
			});
	}

	private void AddUnitToPicker(
		string text, Vector2I dims, bool bEastWest, string iconPath, Func<Vector2I, Direction, Unit> placementFunc)
	{
		var butt = new Godot.Button
		{
			Text = text,
			CustomMinimumSize = new Vector2I(0, 90),
			ButtonGroup = m_CurrUnitPlacement.ButtGroup,
			ToggleMode = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Icon = GD.Load<Texture2D>(iconPath),
			IconAlignment = HorizontalAlignment.Center,
			VerticalIconAlignment = VerticalAlignment.Top,
			ExpandIcon = true,
		};
		butt.AddThemeFontSizeOverride("font_size", 10);
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
		AddToolToToolbox("MOVE", () => new EditorStateMoveTool(this));
		AddToolToToolbox("ROTATE", () => new EditorStateRotationTool(this));
	}

	private void AddToolToToolbox(string text, Func<EditorState> onClickFunc)
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
			var newState = onClickFunc.Invoke();
			CursorLabel.Text = newState.GetType().Name;
			SetState(newState);
		};
	}

	#endregion // Init
	#region .Serialization

	private struct EditorSerializationData
	{
		[JsonInclude] public JsonElement WorldElem;
		[JsonInclude] public JsonElement UniManElem;
	}

	public bool Serialize(string filePath)
	{
		var data = new EditorSerializationData
		{
			WorldElem = Serializer.SerializeWorldPanel(World),
			UniManElem = Serializer.SerializeUnitManager(UniMan),
		};

		using var file = Godot.FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
		if (file is null)
		{
			return false;
		}

		file.StoreString(Serializer.Serialize(data));
		return true;
	}
		
	public bool Deserialize(string filePath)
	{
		World.Clear();
		GD.Print("Cleared the world");

		if (!Godot.FileAccess.FileExists(filePath))
		{
			GD.Print($"Couldn't find save file: {filePath}");
			return false;
		}

		var fileContents = Godot.FileAccess.GetFileAsString(filePath);
		if (string.IsNullOrEmpty(fileContents))
		{
			GD.Print($"Something happaned while loading file: {filePath}");
		}

		var data = Serializer.Deserialize<EditorSerializationData>(fileContents);
		Debug.AssertNotNull(data);
		Serializer.DeserializeWorldPanel(data.WorldElem, World);
		Serializer.DeserializeUnitManager(data.UniManElem, UniMan);
		return true;
	}

	#endregion // .Serialization
	#region .Godot Overrides

	public override void _UnhandledInput(InputEvent ev)
	{
		m_CurrState.OnUnhandledInput(ev);
	}

	public override void _Process(double dt)
	{
		m_CurrState.OnProcess(dt);
	}

	#endregion // .Godot Overrides
	#region .Signal Handlers

	private void OnSaveButt_Pressed()
	{
		if (Serialize(AutoSaveFilePath))
		{
			GD.Print("Saved");
		}
		else
		{
			GD.Print($"Save failed: couldn't open save file {AutoSaveFilePath} for reading.");
		}
	}

	private void OnLoadButt_Pressed()
	{
		if (Deserialize(AutoSaveFilePath))
		{
			GD.Print("Loaded from disk");
		}
	}

	private void OnDimButt_Pressed()
	{
		if (int.TryParse(XLiEd.Text, out var w) && 
			int.TryParse(YLiEd.Text, out var h))
		{
			World.SetDimensions(new Vector2I(w, h));
		}
	}

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(m_CurrState is ISimulationState);
		var simState = (m_CurrState as ISimulationState)!;
		simState.OnPlayButtPressed();
	}

	private void OnHaltButt_Pressed()
	{
		Debug.Assert(m_CurrState is ISimulationState);
		var simState = (m_CurrState as ISimulationState)!;
		simState.OnHaltButtPressed();
	}

	private void OnDebugButt_Pressed()
	{
		Debug.Assert(m_CurrState is ISimulationState);
		var simState = (m_CurrState as ISimulationState)!;
		simState.OnDebugButtPressed();
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
		// SetStateOld(State.Placement);
		SetState(new EditorStateUnitPlacement(this));		
		m_CurrUnitPlacement.Butt = butt;
		m_CurrUnitPlacement.Dir = Direction.East;

		CursorLabel.Text = m_CurrUnitPlacement.Dir.ToString();
	}

	private void OnWorld_DimensionsChanged(Vector2I newDims)
	{
		UpdateDimInterface(newDims);
	}

	#endregion .Signal Handlers


	private void Reset()
	{
		World.Clear();
		Simy.Reset();
	}

	private void SyncButtStates()
	{
		m_CurrState.SyncButtStates();
	}

	private void OnSimulationStart()
	{
		Debug.Assert(m_CurrState is EditorStateIdle);
		SetState(Simy.IsDebugging() ? new EditorStateDebug(this) : new EditorStatePlay(this));
		UpdateTickCountLabel();
	}

	private void OnSimulationEnd()
	{
		Debug.Assert(m_CurrState is ISimulationState);
		SetState(new EditorStateIdle(this));
	}

	private void SetState(EditorState newState, bool bForce = false)
	{
		if (!bForce && newState.GetType() == m_CurrState.GetType())
		{
			return;
		}

		m_CurrState = newState;
		m_CurrState.SyncButtStates();
	}

	private void UpdateTickCountLabel()
	{
		TicksLabel.Text = $"tick: {Simy.Clock.GetTicksSinceStart()}";
	}

	private void UpdateDimInterface(Vector2I newDims)
	{
		XLiEd.Text = newDims.X.ToString();
		YLiEd.Text = newDims.Y.ToString();
	}

	#region .Inner Classes

	private abstract class EditorState(Editor parent)
	{
		protected Editor m_Parent = parent;

		public abstract void SyncButtStates();
		public abstract void OnUnhandledInput(InputEvent ev);
		
		public virtual void OnProcess(double dt) { }

		protected void ResetEverythingForButtSync()
		{
			// Hide everything first.
			// I could the extra marshalling by using in-between booleans but who cares bruh, this only happens once in 
			// a while.
			m_Parent.PlayButt.Disabled = true;
			m_Parent.HaltButt.Disabled = true;
			m_Parent.DebugButt.Disabled = true;
			m_Parent.PlayHBox.Hide();
			m_Parent.ToolsHBox.Hide();
			m_Parent.CursorLabel.Hide();
			m_Parent.UnitPlacerMarCon.Hide();
		}
	}

	/// <summary>
	/// For states valid during simulation.
	/// </summary>
	private interface ISimulationState
	{
		void OnPlayButtPressed();
		void OnDebugButtPressed();
		void OnHaltButtPressed();
	}

	private class EditorStateIdle(Editor parent) : EditorState(parent), ISimulationState
	{
		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				var mev = (ev as InputEventMouse)!;
				var mgloc = m_Parent.World.PosToGrid(m_Parent.World.GetLocalMousePosition());
				switch (m_Parent.UniMan.GetUnit(mgloc)) {
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
					MakePopup(mev, [
						(combi.Operation is BinaryOperation.Add ? null : "x + x + ...", 0),
						(combi.Operation is BinaryOperation.Mul ? null : "x * x * x...", 1)],
						id =>
						{
							combi.SetOperation(id switch
							{
								0 => BinaryOperation.Add,
								1 => BinaryOperation.Mul,
								_ => throw new NotImplementedException(),
							});
						}
					);
					break;

				case UNSlider slid:
					MakePopup(mev, [
						(slid.IsLocked() ? "Unlock" : "Lock", 0),
						(m_Parent.UniMan.GetExtension(slid) is not UnitManager.UEConnector ? "Connect" : null, 1),
						(m_Parent.UniMan.GetExtension(slid) is not UnitManager.UEDirectionToggle ? "Make toggle" : null, 2),
						(m_Parent.UniMan.GetExtension(slid) is not UnitManager.UERuntimeLock ? "Make lock" : null, 3),
						(m_Parent.UniMan.HasExtension(slid) ? "Detach Extension" : null, 4),
						],
						id =>
						{
							switch (id)
							{
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

								case 1: // Connector
									GD.Print("Connecting...");
									m_Parent.m_ConnecterUnit = slid;
									m_Parent.CursorLabel.Text = "Connecting...";
									m_Parent.SetState(new EditorStatePlacingUEConnector(m_Parent));
									break;

								case 2: // Toggle
									m_Parent.UniMan.DetachExtensionFrom(slid);
									m_Parent.UniMan.AttachExtension(
										new UnitManager.UEDirectionToggle(slid, UnitManager.UEDirectionToggle.Kind.CounterClockwise));
									break;

								case 3: // RuntimeLock
									m_Parent.UniMan.DetachExtensionFrom(slid);
									m_Parent.UniMan.AttachExtension(new UnitManager.UERuntimeLock(slid));
									break;

								case 4: // Detaching
									m_Parent.UniMan.DetachExtensionFrom(slid);
									break;

								default:
									throw new NotImplementedException();
							}
						}
					);
					break;

				case UNBranch branch:
					MakePopup(mev, [
						(branch.Check is CmdIf.CheckType.Equal   ? null : "==", 0),
						(branch.Check is CmdIf.CheckType.Greater ? null :  ">", 1),
						(branch.Check is CmdIf.CheckType.Less    ? null :  "<", 2),
						],
						id =>
						{
							branch.SetCheck(id switch
							{
								0 => CmdIf.CheckType.Equal,
								1 => CmdIf.CheckType.Greater,
								2 => CmdIf.CheckType.Less,
								_ => throw new NotImplementedException(),
							});
						}
					);
					break;

				default:
					break;
				}
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.PlayButt.Disabled = false;
			m_Parent.DebugButt.Disabled = false;
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
		}

		public void OnDebugButtPressed()
		{
			m_Parent.Simy.StartAndPause();
			m_Parent.SetState(new EditorStateDebug(m_Parent));
		}

		public void OnHaltButtPressed()
		{
			throw new InvalidProgramException();
		}

		public void OnPlayButtPressed()
		{
			m_Parent.Simy.Start();
			m_Parent.SetState(new EditorStatePlay(m_Parent));
		}

		private void MakePopup(Godot.InputEventMouse mev, IEnumerable<(string? Name, int id)> items, Action<int> pressFunc)
		{ // TODO: Reuse the same instance!
			var men = new Godot.PopupMenu();
			foreach (var (name, id) in items.Where(it => !string.IsNullOrEmpty(it.Name)))
			{
				men.AddItem(name, id);
			}
			men.IdPressed += id => pressFunc.Invoke((int)id);
			men.PopupHide += () =>
			{
				m_Parent.RemoveChild(men);
				men.QueueFree();
			};
			m_Parent.AddChild(men);
			men.Position = (Vector2I)mev.GlobalPosition;
			men.Popup();
		}
	}

	private class EditorStateUnitPlacement(Editor parent) : EditorState(parent)
	{
		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				m_Parent.SetState(new EditorStateIdle(m_Parent));
				m_Parent.m_CurrUnitPlacement.Butt!.ButtonPressed = false;
			}
			else if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				var mousePos = m_Parent.GetLocalMousePosition();
				if (m_Parent.World.GetWorldRect().HasPoint(mousePos))
				{
					var info = m_Parent.m_UnitPickerButtInfos[m_Parent.m_CurrUnitPlacement.Butt!.Name];
					Vector2I mgloc = m_Parent.World.GetMouseGridLocation();
					if (Unit.CanFitIn(m_Parent.World, mgloc, info.Dims, m_Parent.m_CurrUnitPlacement.Dir))
					{
						info.PlacementFunc.Invoke(mgloc, m_Parent.m_CurrUnitPlacement.Dir);
					}
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.EdRotate, true))
			{
				var info = m_Parent.m_UnitPickerButtInfos[m_Parent.m_CurrUnitPlacement.Butt!.Name];
				if (info.bEastWest)
				{
					m_Parent.m_CurrUnitPlacement.Dir = m_Parent.m_CurrUnitPlacement.Dir switch
					{
						Direction.East => Direction.West,
						Direction.West or _ => Direction.East,
					};
				}
				else
				{
					m_Parent.m_CurrUnitPlacement.Dir = m_Parent.m_CurrUnitPlacement.Dir.Rotate90();
				}
				m_Parent.CursorLabel.Text = m_Parent.m_CurrUnitPlacement.Dir.ToString();
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}

		public override void OnProcess(double dt)
		{
			m_Parent.CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}
	}

	private class EditorStatePlacingUEConnector(Editor parent) : EditorState(parent)
	{
		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is UNSlider tgtSlid)
				{
					if (tgtSlid == m_Parent.m_ConnecterUnit)
					{
						GD.Print("Hit the connecting slider bruh...");
					}
					else
					{
						var connSlid = (m_Parent.m_ConnecterUnit as UNSlider)!;
						m_Parent.UniMan.DetachExtensionFrom(connSlid);
						m_Parent.UniMan.AttachExtension(new UnitManager.UEConnector(connSlid, tgtSlid));
						GD.Print($"Connected slider on {connSlid!.GLocation} to the one on {tgtSlid.GLocation}");
						m_Parent.SetState(new EditorStateIdle(m_Parent));
						m_Parent.m_ConnecterUnit = null;
					}
				}
				else
				{
					GD.Print("Hit nothing and cancelled anyway");
					m_Parent.SetState(new EditorStateIdle(m_Parent));
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				GD.Print("Lock cancelled");
				m_Parent.SetState(new EditorStateIdle(m_Parent));
				m_Parent.m_ConnecterUnit = null;
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}

		public override void OnProcess(double dt)
		{
			m_Parent.CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}
	}

	private class EditorStatePlacingMatchingTeleOutput(Editor parent) : EditorState(parent)
	{
		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				var mgloc = m_Parent.World.GetMouseGridLocation();
				if (Unit.CanFitIn(m_Parent.World, mgloc, Vector2I.One, Direction.East))
				{
					var teleIn = (m_Parent.m_ConnecterUnit as UNTeleInput)!;
					var teleOut = m_Parent.UniMan.PlaceTeleOutput(mgloc, m_Parent.m_CurrUnitPlacement.Dir, 1);
					teleIn.AssignOutput(teleOut);
					m_Parent.m_ConnecterUnit = null;
					m_Parent.SetState(new EditorStateUnitPlacement(m_Parent));
				}
				else
				{
					GD.Print("Nope, doesn't fit");
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				m_Parent.UniMan.DeleteUnit(m_Parent.m_ConnecterUnit!);
				GD.Print("Cancelled");
				m_Parent.m_ConnecterUnit = null;
				m_Parent.SetState(new EditorStateUnitPlacement(m_Parent));
			}
			if (Input.IsActionJustPressed(InputManager.Names.EdRotate, true))
			{
				m_Parent.m_CurrUnitPlacement.Dir = m_Parent.m_CurrUnitPlacement.Dir.Rotate90();
				m_Parent.CursorLabel.Text = $"Out: {m_Parent.m_CurrUnitPlacement.Dir}";
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}

		public override void OnProcess(double dt)
		{
			m_Parent.CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}
	}

	private class EditorStateMoveTool(Editor parent) : EditorState(parent)
	{
		public override void OnProcess(double dt)
		{
			m_Parent.CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is { } u)
				{
					m_Parent.m_ConnecterUnit = u;
					u.Deconstruct();
					GD.Print($"Picked up a {u.GetType().Name}");
					m_Parent.m_CurrUnitPlacement.Dir = u.Dir;
					m_Parent.CursorLabel.Text = $"Moving a {u.GetType().Name} ({m_Parent.m_CurrUnitPlacement.Dir})";
					m_Parent.SetState(new EditorStatePlacingMovedUnit(m_Parent));
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				GD.Print("Cancelled move tool");
				m_Parent.SetState(new EditorStateIdle(m_Parent));
				m_Parent.m_CurrUnitPlacement.ButtGroup.GetPressedButton().ButtonPressed = false;
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}
	}

	private class EditorStatePlacingMovedUnit(Editor parent) : EditorState(parent)
	{
		public override void OnProcess(double dt)
		{
			m_Parent.CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				GD.Print("Cancelled moving.");
				m_Parent.m_ConnecterUnit!.Construct(); // Where it originally was.
				m_Parent.CursorLabel.Text = nameof(EditorStateMoveTool);
				m_Parent.SetState(new EditorStateMoveTool(m_Parent));
			}
			else if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				var mousePos = m_Parent.GetLocalMousePosition();
				if (m_Parent.World.GetWorldRect().HasPoint(mousePos))
				{
					var mgloc = m_Parent.World.GetMouseGridLocation();
					if (Unit.CanFitIn(m_Parent.World, mgloc, m_Parent.m_ConnecterUnit!))
					{
						GD.Print($"Moved a {m_Parent.m_ConnecterUnit!.GetType().Name} from {m_Parent.m_ConnecterUnit.GLocation} to {mgloc}");
						m_Parent.m_ConnecterUnit!.SetGLocation(mgloc);
						m_Parent.m_ConnecterUnit.SetDirection(m_Parent.m_CurrUnitPlacement.Dir);
						m_Parent.m_ConnecterUnit.Construct();
						m_Parent.CursorLabel.Text = nameof(EditorStateMoveTool);
						m_Parent.SetState(new EditorStateMoveTool(m_Parent));
					}
					else
					{
						GD.Print("Nope, doesn't fit.");
					}
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.EdRotate, true))
			{
				if (m_Parent.m_ConnecterUnit!.CanFaceDir(Direction.North) is false)
				{
					m_Parent.m_CurrUnitPlacement.Dir = m_Parent.m_CurrUnitPlacement.Dir switch
					{
						Direction.East => Direction.West,
						Direction.West or _ => Direction.East,
					};
				}
				else
				{
					m_Parent.m_CurrUnitPlacement.Dir = m_Parent.m_CurrUnitPlacement.Dir.Rotate90();
				}
				m_Parent.CursorLabel.Text = $"Moving a {m_Parent.m_ConnecterUnit!.GetType().Name} ({m_Parent.m_CurrUnitPlacement.Dir})";
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}
	}

	private class EditorStateRotationTool(Editor parent) : EditorState(parent)
	{
		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.EdRotationToolNorm, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is { } u)
				{
					if (u.CanFaceDir(Direction.North) is false)
					{
						GD.Print($"Flipped a {u.GetType().Name}");
						u.Rotate180();
					}
					else
					{
						GD.Print($"Rotated a {u.GetType().Name} by 90 degrees");
						u.Rotate90();
					}

				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.EdRotationToolAlt, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is { } u)
				{
					if (u.CanFaceDir(Direction.North) is false)
					{
						GD.Print($"Flipped a {u.GetType().Name}");
						u.Rotate180();
					}
					else
					{
						GD.Print($"Rotated a {u.GetType().Name} by -90 degrees");
						u.Rotate270();
					}
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick))
			{
				GD.Print("Cancelled rotation tool");
				m_Parent.SetState(new EditorStateIdle(m_Parent));
				m_Parent.m_CurrUnitPlacement.ButtGroup.GetPressedButton().ButtonPressed = false;
			}
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
		}
	}

	private class EditorStatePlay(Editor parent) : EditorState(parent), ISimulationState
	{
		public override void OnUnhandledInput(InputEvent ev) { }
		public override void OnProcess(double dt)
		{
			m_Parent.Simy.DoPerFrame(dt);
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.HaltButt.Disabled = false;
			m_Parent.DebugButt.Disabled = false;
			m_Parent.PlayHBox.Show();
		}

		public void OnDebugButtPressed()
		{
			m_Parent.Simy.Pause();
			m_Parent.SetState(new EditorStateDebug(m_Parent));
		}

		public void OnHaltButtPressed()
		{
			m_Parent.Simy.Halt();
			m_Parent.SetState(new EditorStateIdle(m_Parent));
		}

		public void OnPlayButtPressed()
		{
			throw new NotImplementedException();
		}
	}

	private class EditorStateDebug(Editor parent) : EditorState(parent), ISimulationState
	{
		public override void OnUnhandledInput(InputEvent ev) { }
		public override void OnProcess(double dt)
		{
			m_Parent.Simy.DoPerFrame(dt);
		}

		public override void SyncButtStates()
		{
			ResetEverythingForButtSync();
			m_Parent.PlayButt.Disabled = false;
			m_Parent.HaltButt.Disabled = false;
			m_Parent.DebugButt.Disabled = false;
			m_Parent.PlayHBox.Show();
		}

		public void OnDebugButtPressed()
		{
			if (!m_Parent.Simy.IsPaused()) // Spamming the debug key while the tick is in progress.
			{
				return;
			}

			m_Parent.Simy.TickOnce();
		}

		public void OnHaltButtPressed()
		{
			m_Parent.Simy.Halt();
			m_Parent.SetState(new EditorStateIdle(m_Parent));
		}

		public void OnPlayButtPressed()
		{
			m_Parent.Simy.Resume();
			m_Parent.SetState(new EditorStatePlay(m_Parent));
		}
	}









	#endregion // .Inner Classes
}
