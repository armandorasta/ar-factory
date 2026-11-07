using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public class UnitManager : IDestroyable
{
	#region .Scenes
	public static readonly Godot.PackedScene UNSupplierScene = GD.Load<Godot.PackedScene>("res://scenes/UNSupplier.tscn");
	public static readonly Godot.PackedScene UNSliderScene = GD.Load<Godot.PackedScene>("res://scenes/UNSlider.tscn");
	public static readonly Godot.PackedScene UNUpdaterScene = GD.Load<Godot.PackedScene>("res://scenes/UNUpdater.tscn");
	public static readonly Godot.PackedScene UNBinScene = GD.Load<Godot.PackedScene>("res://scenes/UNBin.tscn");
	public static readonly Godot.PackedScene UNDemanderScene = GD.Load<Godot.PackedScene>("res://scenes/UNDemander.tscn");
	public static readonly Godot.PackedScene UNCombinerScene = GD.Load<Godot.PackedScene>("res://scenes/UNCombiner.tscn");
	public static readonly Godot.PackedScene UNClonerScene = GD.Load<Godot.PackedScene>("res://scenes/UNCloner.tscn");
	public static readonly Godot.PackedScene UNBranchScene = GD.Load<Godot.PackedScene>("res://scenes/UNBranch.tscn");
	public static readonly Godot.PackedScene UNTeleInputScene = GD.Load<Godot.PackedScene>("res://scenes/UNTeleInput.tscn");
	public static readonly Godot.PackedScene UNTeleOutputScene = GD.Load<Godot.PackedScene>("res://scenes/UNTeleOutput.tscn");
	public static readonly Godot.PackedScene UNPlatformScene = GD.Load<Godot.PackedScene>("res://scenes/UNPlatform.tscn");
	#endregion // .Scenes


	public WorldPanel World { get; private init; }

	private readonly List<Unit> m_Units = [];

	public UnitManager(WorldPanel world)
	{
		this.World = world;
		World.Redrawing += DebugDraw;
		World.Cleared += Clear;
		World.DimensionsChanged += OnWorldDimensionsChanged;
	}


	public IEnumerable<Unit> GetUnitsIter() => m_Units;

	/// <summary>
	/// Returns the whichever unit that <paramref name="gloc"/> lands on. Disgards floating status.
	/// </summary>
	public Unit? GetUnit(Vector2I gloc)
	{
		foreach (var u in m_Units)
		{
			if (u.IsWithin(gloc))
			{
				return u;
			}
		}
		return null;
	}

	/// <summary>
	/// This adds the unit to the tree, so only call the setup function after this one.
	/// </summary>
	private void AddUnit(Unit newUnit)
	{
		m_Units.Add(newUnit);
		World.AddChild(newUnit);
	}

	public void DeleteUnit(Unit u)
	{
		World.RemoveChild(u);
		u.Destroy();
		m_Units.Remove(u);
	}

	public void DebugDraw()
	{
		foreach (var u in m_Units)
		{
			u.QueueRedraw();
		}
	}

	public void OnWorldDimensionsChanged()
	{
		for (var i = m_Units.Count - 1; i >= 0; --i)
		{
			var u = m_Units[i];
			if (!Unit.CanFitIn(World, u.GLocation, u.Dimensions, u.Dir))
			{
				DeleteUnit(u);
			}
		}
	}

	public void Reset()
	{
		foreach (var u in m_Units)
		{
			u.Reset();
		}
	}

	public void Clear()
	{
		for (var i = m_Units.Count - 1; i >= 0; --i)
		{
			DeleteUnit(m_Units[i]);
		}
	}

	public void Destroy()
	{
		Clear();
	}

	#region .Placement Functions

	[Conditional("DEBUG")]
	private void PlaceSomeUnits()
	{
		PlaceSupplier(new Vector2I(1, 7), Direction.East, 3, [1, 2, 3, 4, 5, 6]);
		PlaceSlider(new Vector2I(2, 7), Direction.East);
		PlaceSlider(new Vector2I(3, 7), Direction.North);
		PlaceSlider(new Vector2I(3, 6), Direction.East);
		PlaceSlider(new Vector2I(4, 6), Direction.East);
		PlaceSlider(new Vector2I(5, 6), Direction.East);
		PlaceBranch(new Vector2I(6, 6), Direction.East, 2, CmdIf.CheckType.Greater, 100);
		PlaceSlider(new Vector2I(7, 6), Direction.East);
		PlaceBin(new Vector2I(8, 6));
		PlaceSlider(new Vector2I(7, 7), Direction.South);
		PlaceSlider(new Vector2I(7, 8), Direction.South);
		PlaceSlider(new Vector2I(7, 9), Direction.West);
		PlaceUpdater(new Vector2I(6, 9), Direction.West, 2, UNUpdater.UpdateType.Double);
		PlaceSlider(new Vector2I(5, 9), Direction.North);
		PlaceSlider(new Vector2I(5, 8), Direction.North);
		PlaceSlider(new Vector2I(5, 7), Direction.North);
	}

	public UNInjector PlaceInjector()
	{
		var injy = new UNInjector();
		AddUnit(injy);
		injy.Setup(World);
		return injy;
	}

	public UNSupplier PlaceSupplier(Vector2I gloc, Direction dir, int rate, IEnumerable<int> seq)
	{
		var suppy = UNSupplierScene.Instantiate<UNSupplier>();
		AddUnit(suppy);
		suppy.Setup(World, gloc, rate, dir, seq);
		return suppy;
	}

	public UNSlider PlaceSlider(Vector2I gloc, Direction dir, int rate = 1)
	{
		var slidy = UNSliderScene.Instantiate<UNSlider>();
		AddUnit(slidy);
		slidy.Setup(World, gloc, rate, dir);
		return slidy;
	}

	public UNUpdater PlaceUpdater(Vector2I gloc, Direction dir, int rate, UNUpdater.UpdateType UpT)
	{
		var updater = UNUpdaterScene.Instantiate<UNUpdater>();
		AddUnit(updater);
		updater.Setup(World, gloc, rate, dir, UpT);
		return updater;
	}

	public UNBin PlaceBin(Vector2I gloc)
	{
		var bin = UNBinScene.Instantiate<UNBin>();
		AddUnit(bin);
		bin.Setup(World, gloc);
		return bin;
	}

	public UNBranch PlaceBranch(Vector2I gloc, Direction dir, int rate, CmdIf.CheckType check, int value)
	{
		var bra = UNBranchScene.Instantiate<UNBranch>();
		AddUnit(bra);
		bra.Setup(World, gloc, dir, rate, check, value);
		return bra;
	}

	public UNDemander PlaceDemander(Vector2I gloc, Direction dir, IEnumerable<int> seq)
	{
		var dem = UNDemanderScene.Instantiate<UNDemander>();
		AddUnit(dem);
		dem.Setup(World, gloc, dir, seq);
		return dem;
	}

	public UNCombiner PlaceCombiner(Vector2I gloc, Direction dir, int rate, BinaryOperation op)
	{
		var com = UNCombinerScene.Instantiate<UNCombiner>();
		AddUnit(com);
		com.Setup(World, gloc, dir, rate, op);
		return com;
	}

	public UNCloner PlaceCloner(Vector2I gloc, Direction dir, int rate)
	{
		var cloner = UNClonerScene.Instantiate<UNCloner>();
		AddUnit(cloner);
		cloner.Setup(World, gloc, dir, rate);
		return cloner;
	}

	public UNTeleInput PlaceTeleInput(Vector2I gloc, Direction dir, int rate)
	{
		var teleIn = UNTeleInputScene.Instantiate<UNTeleInput>();
		AddUnit(teleIn);
		teleIn.Setup(World, gloc, rate, dir);
		return teleIn;
	}

	public UNTeleOutput PlaceTeleOutput(Vector2I gloc, Direction dir, int delay)
	{
		var teleIn = UNTeleOutputScene.Instantiate<UNTeleOutput>();
		AddUnit(teleIn);
		teleIn.Setup(World, gloc, dir, delay);
		return teleIn;
	}

	public UNPlatform PlacePlatform(Vector2I gloc)
	{
		var plat = UNPlatformScene.Instantiate<UNPlatform>();
		AddUnit(plat);
		plat.Setup(World, gloc);
		return plat;
	}

	#endregion // Placement Functions
}
