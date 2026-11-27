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
	private class UnitPickerButtInfo
	{
		public Vector2I Dims; 
		public bool bEastWest;
		public Func<Vector2I, Direction, Unit> PlacementFunc;
	}

	public Godot.ButtonGroup m_UnitButtGroup = new();
	public Godot.Label CursorLabel => m_CursorLabel!;
	private Godot.Label? m_CursorLabel;
	private readonly Dictionary<string, UnitPickerButtInfo> m_UnitPickerButtInfos = [];
	
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
		// TODO: Remove this after refactoring
		m_UnitButtGroup = new() { AllowUnpress = true };
		m_UnitButtGroup.Pressed += OnUnitPicker_Pressed;
		
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
				var teleIn = UniMan.PlaceTeleInput(l, d, 1);
				SetState(new EditorStatePlacingMatchingTeleOutput(this, teleIn));
				return teleIn;
			});
	}

	private void AddUnitToPicker(
		string text, Vector2I dims, bool bEastWest, string iconPath, Func<Vector2I, Direction, Unit> placementFunc)
	{
		var butt = new Godot.Button
		{
			Text = text,
			CustomMinimumSize = new Vector2I(0, 90),
			ButtonGroup = m_UnitButtGroup,
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
			ButtonGroup = m_UnitButtGroup,
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
		SetState(new EditorStateUnitPlacement(this));		
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

	private struct UniPlacement
	{
		public readonly Godot.ButtonGroup ButtGroup => m_Parent.m_UnitButtGroup;
		public readonly Godot.Label CursorLabel => m_Parent.CursorLabel;
		public readonly Godot.Button Butt => (ButtGroup.GetPressedButton() as Button)!;
		public Direction Dir { get; private set; }

		public string Prefix { readonly get => m_Prefix; set { m_Prefix = value; UpdateCursorLabel(); } }
		public string Postfix { readonly get => m_Postfix; set { m_Postfix = value; UpdateCursorLabel(); } }

		private readonly Editor m_Parent;
		private string m_Prefix;
		private string m_Postfix;

		public UniPlacement(Editor parent, Direction initDir = Direction.East)
		{
			m_Prefix = string.Empty;
			m_Postfix = string.Empty;
			m_Parent = parent;
			Dir = initDir;

			UpdateCursorLabel();
		}

		public void Rotate() => RotateImpl(m_Parent.m_UnitPickerButtInfos[Butt.Name].bEastWest);
		public void Rotate(Unit tgtUnit) => RotateImpl(!tgtUnit.CanFaceDir(Dir.Rotate90()));

		public void RotateImpl(bool bEastWest)
		{
			Dir = !bEastWest ? Dir.Rotate90() : Dir is Direction.East ? Direction.West : Direction.East;
			UpdateCursorLabel();
		}

		private readonly void UpdateCursorLabel()
		{
			CursorLabel.Text = $"{Prefix}{Dir}{Postfix}";
		}
	}

	private abstract class EditorState(Editor parent)
	{
		protected readonly Editor m_Parent = parent;

		public abstract void SyncButtStates();
		public abstract void OnUnhandledInput(InputEvent ev);
		
		public virtual void OnProcess(double dt) { }

		protected void SetState(EditorState newState)
		{
			m_Parent.SetState(newState);
		}

		protected void HideEverythingForButtSync()
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
									m_Parent.CursorLabel.Text = "Connecting...";
									SetState(new EditorStateConnectorUEConnector(m_Parent, slid));
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
			else if (Input.IsActionPressed(InputManager.Names.EdDelete, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is { } u)
				{
					m_Parent.UniMan.DeleteUnit(u);
				}
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
			m_Parent.PlayButt.Disabled = false;
			m_Parent.DebugButt.Disabled = false;
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
		}

		public void OnDebugButtPressed()
		{
			m_Parent.Simy.StartAndPause();
			SetState(new EditorStateDebug(m_Parent));
		}

		public void OnHaltButtPressed()
		{
			throw new InvalidProgramException();
		}

		public void OnPlayButtPressed()
		{
			m_Parent.Simy.Start();
			SetState(new EditorStatePlay(m_Parent));
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
		public UniPlacement Placement = new(parent);
		public Godot.Label CursorLabel => m_Parent.CursorLabel;

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				SetState(new EditorStateIdle(m_Parent));
				Placement.Butt.ButtonPressed = false;
			}
			else if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				var mousePos = m_Parent.GetLocalMousePosition();
				if (m_Parent.World.GetWorldRect().HasPoint(mousePos))
				{
					var info = m_Parent.m_UnitPickerButtInfos[Placement.Butt.Name];
					Vector2I mgloc = m_Parent.World.GetMouseGridLocation();
					if (Unit.CanFitIn(m_Parent.World, mgloc, info.Dims, Placement.Dir))
					{
						info.PlacementFunc.Invoke(mgloc, Placement.Dir);
					}
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.EdRotate, true))
			{
				Placement.Rotate();
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			CursorLabel.Show();
		}

		public override void OnProcess(double dt)
		{
			CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}
	}

	private class EditorStateConnectorUEConnector(Editor parent, Unit connectorUnit) : EditorState(parent)
	{
		public readonly Unit ConnectorUnit = connectorUnit;
		public Godot.Label CursorLabel => m_Parent.CursorLabel;

		public override void OnProcess(double dt)
		{
			CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is UNSlider tgtSlid)
				{
					if (tgtSlid == ConnectorUnit)
					{
						GD.Print("Hit the connecting slider bruh...");
					}
					else
					{
						var connSlid = (ConnectorUnit as UNSlider)!;
						m_Parent.UniMan.DetachExtensionFrom(connSlid);
						m_Parent.UniMan.AttachExtension(new UnitManager.UEConnector(connSlid, tgtSlid));
						GD.Print($"Connected slider on {connSlid!.GLocation} to the one on {tgtSlid.GLocation}");
						SetState(new EditorStateIdle(m_Parent));
					}
				}
				else
				{
					GD.Print("Hit nothing and cancelled anyway");
					SetState(new EditorStateIdle(m_Parent));
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				GD.Print("Lock cancelled");
				SetState(new EditorStateIdle(m_Parent));
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}
	}

	private class EditorStatePlacingMatchingTeleOutput : EditorState
	{
		public readonly UNTeleInput MatchingInput;
		public Godot.Label CursorLabel => m_Parent.CursorLabel;
		public UniPlacement Placement;

		public EditorStatePlacingMatchingTeleOutput(Editor parent, UNTeleInput teleIn) : base(parent)
		{
			MatchingInput = teleIn;
			Placement = new(parent) { Prefix = "Out: " };
		}

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				var mgloc = m_Parent.World.GetMouseGridLocation();
				if (Unit.CanFitIn(m_Parent.World, mgloc, Vector2I.One, Direction.East))
				{
					var teleOut = m_Parent.UniMan.PlaceTeleOutput(mgloc, Placement.Dir, 1);
					MatchingInput.AssignOutput(teleOut);
					SetState(new EditorStateUnitPlacement(m_Parent));
				}
				else
				{
					GD.Print("Nope, doesn't fit");
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				m_Parent.UniMan.DeleteUnit(MatchingInput);
				GD.Print("Cancelled");
				SetState(new EditorStateUnitPlacement(m_Parent));
			}
			if (Input.IsActionJustPressed(InputManager.Names.EdRotate, true))
			{
				Placement.Rotate();
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			CursorLabel.Show();
		}

		public override void OnProcess(double dt)
		{
			CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}
	}

	private class EditorStateMoveTool : EditorState
	{
		public Godot.Label CursorLabel => m_Parent.CursorLabel;

		public EditorStateMoveTool(Editor parent) : base(parent)
		{
			CursorLabel.Text = nameof(EditorStateMoveTool);
		}

		public override void OnProcess(double dt)
		{
			CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				if (m_Parent.UniMan.GetUnit(m_Parent.World.GetMouseGridLocation()) is { } u)
				{
					var movedUnit = u;
					movedUnit.Deconstruct();
					GD.Print($"Picked up a {u.GetType().Name}");
					SetState(new EditorStatePlacingMovedUnit(m_Parent, movedUnit));
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				GD.Print("Cancelled move tool");
				SetState(new EditorStateIdle(m_Parent));
				m_Parent.m_UnitButtGroup.GetPressedButton().ButtonPressed = false;
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
			m_Parent.ToolsHBox.Show();
			m_Parent.UnitPlacerMarCon.Show();
			m_Parent.CursorLabel.Show();
		}
	}

	private class EditorStatePlacingMovedUnit : EditorState
	{
		public Godot.Label CursorLabel => m_Parent.CursorLabel;
		public readonly Unit FloatingUnit;
		public UniPlacement Placement;

		public EditorStatePlacingMovedUnit(Editor parent, Unit movedUnit) : base(parent)
		{
			FloatingUnit = movedUnit;
			Placement = new(parent, initDir: movedUnit.Dir) 
			{ 
				Prefix = $"Moving a {FloatingUnit.GetType().Name} (",
				Postfix = ")",
			};
		}

		public override void OnProcess(double dt)
		{
			CursorLabel.Position = m_Parent.GetLocalMousePosition() + Vector2.One * 50;
		}

		public override void OnUnhandledInput(InputEvent ev)
		{
			if (Input.IsActionJustPressed(InputManager.Names.RightClick, true))
			{
				GD.Print("Cancelled moving.");
				FloatingUnit.Construct(); // Where it originally was.
				CursorLabel.Text = nameof(EditorStateMoveTool);
				SetState(new EditorStateMoveTool(m_Parent));
			}
			else if (Input.IsActionJustPressed(InputManager.Names.LeftClick, true))
			{
				var mousePos = m_Parent.GetLocalMousePosition();
				if (m_Parent.World.GetWorldRect().HasPoint(mousePos))
				{
					var mgloc = m_Parent.World.GetMouseGridLocation();
					if (Unit.CanFitIn(m_Parent.World, mgloc, FloatingUnit))
					{
						GD.Print($"Moved a {FloatingUnit.GetType().Name} from {FloatingUnit.GLocation} to {mgloc}");
						FloatingUnit.SetGLocation(mgloc);
						FloatingUnit.SetDirection(Placement.Dir);
						FloatingUnit.Construct();
						SetState(new EditorStateMoveTool(m_Parent));
					}
					else
					{
						GD.Print("Nope, doesn't fit.");
					}
				}
			}
			else if (Input.IsActionJustPressed(InputManager.Names.EdRotate, true))
			{
				Placement.Rotate(FloatingUnit);
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
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
				SetState(new EditorStateIdle(m_Parent));
				m_Parent.m_UnitButtGroup.GetPressedButton().ButtonPressed = false;
			}
		}

		public override void SyncButtStates()
		{
			HideEverythingForButtSync();
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
			HideEverythingForButtSync();
			m_Parent.HaltButt.Disabled = false;
			m_Parent.DebugButt.Disabled = false;
			m_Parent.PlayHBox.Show();
		}

		public void OnDebugButtPressed()
		{
			m_Parent.Simy.Pause();
			SetState(new EditorStateDebug(m_Parent));
		}

		public void OnHaltButtPressed()
		{
			m_Parent.Simy.Halt();
			SetState(new EditorStateIdle(m_Parent));
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
			HideEverythingForButtSync();
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
			SetState(new EditorStateIdle(m_Parent));
		}

		public void OnPlayButtPressed()
		{
			m_Parent.Simy.Resume();
			SetState(new EditorStatePlay(m_Parent));
		}
	}









	#endregion // .Inner Classes
}
