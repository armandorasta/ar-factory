using Godot;
using System;
using System.Collections.Generic;

namespace ArFactory;

public partial class WorldPanel : Panel
{
	// Nodes
	public static readonly PackedScene ItemScene = GD.Load<PackedScene>("res://scenes/item.tscn");
	public static readonly PackedScene UnSupplierScene = GD.Load<PackedScene>("res://scenes/un_supplier.tscn");
	public static readonly PackedScene UnSliderScene = GD.Load<PackedScene>("res://scenes/un_slider.tscn");
	public static readonly PackedScene UnUpdaterScene = GD.Load<PackedScene>("res://scenes/un_updater.tscn");
	public static readonly PackedScene UnBinScene = GD.Load<PackedScene>("res://scenes/un_bin.tscn");
	public static readonly PackedScene UnDemanderScene = GD.Load<PackedScene>("res://scenes/un_demander.tscn");
	public static readonly PackedScene UnCombinerScene = GD.Load<PackedScene>("res://scenes/un_combiner.tscn");
	public static readonly PackedScene UnClonerScene = GD.Load<PackedScene>("res://scenes/un_cloner.tscn");
	public static readonly PackedScene UnBranchScene = GD.Load<PackedScene>("res://scenes/un_branch.tscn");


	// Other statics
	public static readonly Font DebugFont = GD.Load<Font>("res://resources/fonts/AnonymousPro-Regular.ttf");


	// Public interface
	public readonly Vector2I Dims = new(15, 10);
	public readonly float CellWidth = 100.0f;
	// TODO: Make this private!
	public List<CmdSlide> BlockedSlideCmds = [];


	// Privates
	private List<Unit> m_Units = [];
	private List<Tile> m_Tiles = [];


	#region .Essential Functions

	/// <summary>
	/// Checks if <paramref name="gloc"/> is within the world in grid-space.
	/// </summary>
	public bool IsWithin(Vector2I gloc) => GetRectGrid().HasPoint(gloc);

	/// <summary>
	/// Returns enclosing Rect2I in grid-space.
	/// </summary>
	public Rect2I GetRectGrid() => new(Vector2I.Zero, Dims);

	/// <summary>
	/// Returns enclosing Rect2 in world-space.
	/// </summary>
	public Rect2 GetRect2() => new(Vector2I.Zero, CellWidth * (Vector2)Dims);
	
	/// <summary>
	/// Converts from grid-space to world-space. 
	/// This function does not bounds checking whatsoever.
	/// </summary>
	public Vector2 GridToPos(Vector2I gloc) => CellWidth * (Vector2)gloc; 
	
	/// <summary>
	/// Converts from world-space to grid-space.
	/// This function does not bounds checking whatsoever.
	/// </summary>
	public Vector2I PosToGrid(Vector2 pos) => new((int)(pos.X / CellWidth), (int)(pos.Y / CellWidth));
	
	/// <summary>
	/// Converts a vector in grid-space to an index into an array.
	/// This function does not bounds checking whatsoever.
	/// </summary>
	public int GridToIndex(Vector2I gloc) => gloc.Y * Dims.X + gloc.X;

	/// <summary>
	/// Converts an index to a vector in grid-space.
	/// This function does not bounds checking whatsoever.
	/// </summary>
	public Vector2I IndexToGrid(int index) => new(index % Dims.X, index / Dims.X);

	/// <summary>
	/// Same as <seealso cref="IsWithin"/>, but also checks if a tile is allocated in that spot.
	/// </summary>
	public bool HasTile(Vector2I gloc) => IsWithin(gloc) && m_Tiles[GridToIndex(gloc)] != null;
	public Tile GetTile(Vector2I gloc) => IsWithin(gloc) ? m_Tiles[GridToIndex(gloc)] : null;

	/// <summary>
	/// Returns the whichever unit that <paramref name="gloc"/> lands on.
	/// </summary>
	public Unit GetUnit(Vector2I gloc)
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


	#region .Tile Methods

	// Adds the passed tile to the world, the tile has to be within boundaries!
	// `bOverride` can be used to override an existing tile in the same location, the old tile will be
	// sent to oblivion and not returned.
	public Tile InstallTile(Tile tl, bool bOverride = false)
	{
		var gloc = tl.GetGridLoc();
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bOverride || !HasTile(gloc));

		RemoveTile(gloc, !bOverride);
		m_Tiles[GridToIndex(gloc)] = tl;
		tl.Connect(Tile.SignalName.NeedsRedraw, Callable.From(OnTile_NeedsRedraw));
		return tl;
	}

	// Removes the tile from the world, destroys items within it, and sends it into oblivion.
	public void RemoveTile(Vector2I gloc, bool bMaybeNull = false)
	{
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bMaybeNull || HasTile(gloc));
		if (!HasTile(gloc))
		{
			return;
		}
		var i = GridToIndex(gloc);
		if (m_Tiles[i] is TlHolder holder)
		{
			holder.DestroyItem(true);
		}
		m_Tiles[i] = null;
	}

	// Returns the moved tile.
	// Moves the tile from one location to another, along with the items it contains.
	// Does nothing when `gFrom` and 'gTo' are the same.
	// Same as `ExtractTile` followed by `InstallTile`.
	public Tile MoveTile(Vector2I gFrom, Vector2I gTo, bool bMaybeNull = false, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gFrom));
		Debug.Assert(IsWithin(gTo));
		Debug.Assert(bMaybeNull || HasTile(gFrom));
		Debug.Assert(bOverride || gFrom == gTo || !HasTile(gTo));
		if (gFrom == gTo)
		{
			return GetTile(gFrom);
		}
		var myTile = ExtractTile(gFrom, bMaybeNull);
		myTile.SetGridLocUnchecked(gTo);
		InstallTile(myTile, bOverride);
		return myTile;
	}

	// Removes the tile from the world, and returns it.
	public Tile ExtractTile(Vector2I gloc, bool bMaybeNull = false)
	{
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bMaybeNull || HasTile(gloc));
		var i = GridToIndex(gloc);
		var myTile = m_Tiles[i];
		m_Tiles[i] = null;
		myTile.Disconnect(Tile.SignalName.NeedsRedraw, Callable.From(OnTile_NeedsRedraw));
		return myTile;
	}

	// Swaps two tiles, or does nothing when `gloc1` and `gloc2` are the same.
	public void SwapTiles(Vector2I gloc1, Vector2I gloc2, bool bMaybeNull = false)
	{
		Debug.Assert(IsWithin(gloc1));
		Debug.Assert(IsWithin(gloc2));
		Debug.Assert(bMaybeNull || HasTile(gloc1) && HasTile(gloc2));
		if (gloc1 == gloc2)
		{
			return;
		}

		// Extract both out of the way
		var tl1 = ExtractTile(gloc1, true);
		var tl2 = ExtractTile(gloc2, true);
		
		if (tl1 != null)
		{
			tl1.SetGridLocUnchecked(gloc2);
			InstallTile(tl1);
		}
		
		if (tl2 != null)
		{
			tl2.SetGridLocUnchecked(gloc1);
			InstallTile(tl2);
		}
	}


	#endregion // Tile Methods
	#region .Item Methods

	/// <summary>
	/// Returns the item just added.
	/// </summary>
	public Item SpawnItem(Vector2I gloc, int value)
	{
		Debug.Assert(GetTile(gloc) is TlHolder);
		var myTile = GetTile(gloc) as TlHolder;
		Debug.Assert(!myTile.IsReserved()); // Might have to change this to `HasItem`, but who knows.
		
		var newItem = ItemScene.Instantiate<Item>();
		AddChild(newItem);
		newItem.Setup(this, gloc, value);
		
		myTile.SetItemUnchecked(newItem);
		return newItem;
	}

	/// <summary>
	/// Returns the clone, not the original.
	/// Clones the item into another tile, or does nothing if <paramref name="gSrc"/> and <paramref name="gDest"/> 
	/// are the same.
	/// </summary>
	public Item CloneItem(Vector2I gSrc, Vector2I gDest, bool bMaybeNull = false, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gSrc));
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(HasTile(gSrc) && GetTile(gSrc) is TlHolder);
		Debug.Assert(HasTile(gDest)   && GetTile(gDest)   is TlHolder);
		Debug.Assert(bMaybeNull || (GetTile(gSrc) as TlHolder).HasItem());
		Debug.Assert(bOverride || gSrc == gDest || !(GetTile(gDest) as TlHolder).HasItem());
		return SpawnItem(gDest, GetTile(gSrc).GetItem(false).GetValue());
	}

	/// <summary>
 	/// Returns the teleported item.
	/// Teleports the item to another tile, or does nothing if <paramref name="gSrc"/> and <paramref name="gDest"/> 
	/// are the same.
	/// </summary>
	public Item TeleportItem(Vector2I gSrc, Vector2I gDest, bool bMaybeNull = false, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gSrc));
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(GetTile(gSrc) is TlHolder);
		Debug.Assert(GetTile(gDest) is TlHolder);
		if (gSrc == gDest)
		{
			return GetTile(gSrc).GetItem();
		}

		var srcTile = GetTile(gSrc) as TlHolder;
		var destTile = GetTile(gDest) as TlHolder;
		Debug.Assert(bMaybeNull || srcTile.HasItem());
		Debug.Assert(bOverride || gSrc == gDest || !destTile.HasItem());
		destTile.SetItemUnchecked(srcTile.ExtractItem());
		destTile.GetItem().SyncPosWithGrid(this);
		return destTile.GetItem();
	}


	#endregion // Item Methods
	#endregion // Essential Functions
	#region .Simulation Related.

	public void DoPerFrame(float dt, Level lv)
	{
		foreach (var u in m_Units)
		{
			u.DoPerFrame(dt, lv);
		}
	}

	public void OnTick(Level lv)
	{
		Debug.Assert(lv.World == this);
		foreach (var tl in m_Tiles)
		{
			tl?.GetItem(true)?.ResetMovementFlag();
		}

		// THIS LOOP HAS TO HAPPEN BEFORE PENDING NEW COMMANDS!
		foreach (var u in m_Units)
		{
			u.PreProcessTick();
		}

		// This must happen before HandleCmdTick, otherwise the first tick will handle nothing.
		foreach (var u in m_Units)
		{
			u.PendNewCommands();
		}

		foreach (var u in m_Units)
		{
			u.HandleCmdTick(lv);
		}

		// Must happen after HandleCmdTick, otherwise it will do nothing since BlockSlideCmds is
		// populated by HandleCmdTick.
		HandleSlideCmdOverlapping();
	}

	public void CleanUp()
	{
		foreach (var u in m_Units)
		{
			u.Reset();
		}
		
		foreach (var tl in m_Tiles)
		{
			if (tl is TlHolder holder)
			{
				holder.DestroyItem(true);
			}
		}

		BlockedSlideCmds.Clear();
	}

	#endregion
	#region .Godot Overrides

	public override void _Ready()
	{
		Size = CellWidth * (Vector2)Dims;
		for (var i = 0; i < Dims.X * Dims.Y; ++i)
		{
			m_Tiles.Add(null);
		}

		PlaceSomeUnits();
	}

	public override void _Draw()
	{
		DrawRect(new(Vector2.Zero, Size), new(0.0f, 0.1f, 0.0f));
		for (var i = 1; i < Dims.X; ++i)
		{
			DrawLine(new(i*CellWidth, 0.0f), new(i*CellWidth, Size.Y), new(0.0f, 0.18f, 0.0f));
		}
		for (var i = 1; i < Dims.Y; ++i)
		{
			DrawLine(new(0.0f, i*CellWidth), new(Size.X, i*CellWidth), new(0.0f, 0.18f, 0.0f));
		}
		foreach (var tl in m_Tiles)
		{
			tl?.DebugDraw(this);
		}
		foreach (var u in m_Units)
		{
			DrawRect(new(GridToPos(u.GridLoc), CellWidth*(Vector2)u.Dims), Colors.Black, false, -2.0f);
			DrawString(DebugFont, u.Position + new Vector2(0.0f, 15.0f), u.GetType().Name,
				HorizontalAlignment.Left, CellWidth, 16, Colors.Black);
				// u.GetScript().As<CSharpScript>().GetGlobalName(),
		}
	}

	#endregion // Godot Overrides
	#region .Placement Functions

	[System.Diagnostics.Conditional("DEBUG")]
	private void PlaceSomeUnits()
	{
		// PlaceInjector([
		// 	new CmdSpawn(new(2, 7), 1),
		// 	new CmdSpawn(new(2, 7), 2),
		// 	new CmdSpawn(new(2, 7), 3),
		// 	new CmdSpawn(new(2, 7), 3),
		// ]);
		// PlaceSlider(new(2, 7), Direction.East);
		// PlaceSlider(new(3, 7), Direction.North);
		// PlaceSlider(new(3, 6), Direction.East);
		// PlaceUpdater(new(4, 6), Direction.East, 2, UpdateType.Double);
		// return;

		PlaceSupplier(new(0, 6), Direction.East, 1, [1, 2, 3, 4, 5, 6]);
		PlaceSlider(new(2, 7), Direction.East);
		PlaceSlider(new(3, 7), Direction.North);
		PlaceSlider(new(3, 6), Direction.East);
		PlaceUpdater(new(4, 6), Direction.East, 1, UpdateType.Double);
		PlaceSlider(new(5, 6), Direction.East);
		PlaceSlider(new(6, 6), Direction.South);
		PlaceSlider(new(6, 7), Direction.South);
		PlaceSlider(new(6, 8), Direction.West);
		PlaceUpdater(new(5, 8), Direction.West, 1, UpdateType.Double);
		PlaceSlider(new(4, 8), Direction.West);
		PlaceSlider(new(3, 8), Direction.North);
	}

	public void PlaceInjector(IEnumerable<Command> cmdsToInject)
	{
		var myUnit = new UnCmdInjector();
		AddUnit(myUnit);
		myUnit.Setup(this, cmdsToInject);
	}

	public void PlaceSupplier(Vector2I gloc, Direction dir, int rate, IEnumerable<int> seq)
	{
		var myUnit = UnSupplierScene.Instantiate() as UnSupplier;
		AddUnit(myUnit);
		myUnit.Setup(this, gloc, rate, dir, seq);
	}

	public void PlaceSlider(Vector2I gloc, Direction dir)
	{
		var myUnit = UnSliderScene.Instantiate() as UnSlider;
		AddUnit(myUnit);
		myUnit.Setup(this, gloc, 1, dir);
	}

	public void PlaceUpdater(Vector2I gloc, Direction dir, int rate, UpdateType UpT)
	{
		var doub = UnUpdaterScene.Instantiate<UnUpdater>();
		AddUnit(doub);
		doub.Setup(this, gloc, rate, dir, UpT);
	}

	// public void PlaceBin(Vector2I gloc)
	// {
	// 	var bin = UNBinScene.Instantiate<UnBin>();
	// 	AddUnit(bin);
	// 	bin.("setup", this, gloc);		
	// }

	// public void PlaceDemander(Vector2I gloc, Direction dir, int[] seq)
	// {
	// 	var dem = UNDemanderScene.Instantiate<UnDemander>();
	// 	AddUnit(dem);
	// 	dem.("setup", this, gloc, dir, seq);		
	// }

	// // TODO: int to UNCombiner.Operation
	// public void PlaceCombiner(Vector2I gloc, Direction dir, int rate, int op)
	// {
	// 	var bus = UNCombinerScene.Instantiate<UnCombiner>();
	// 	AddUnit(bus);
	// 	bus.("setup", this, gloc, rate, dir, op);		
	// }

	// public void PlaceCloner(Vector2I gloc, Direction dir, int rate)
	// {
	// 	var cloner = UNClonerScene.Instantiate<UnCloner>();
	// 	AddUnit(cloner);
	// 	cloner.("setup", this, gloc, rate, dir);	
	// }

	// public void PlaceBranch(Vector2I gloc, Direction dir, int rate, int minVal)
	// {
	// 	var branch = UNBranchScene.Instantiate<UnBranch>();
	// 	AddUnit(branch);
	// 	branch.("setup", this, gloc, rate, dir, minVal);
	// }

	#endregion // Placement Functions
	#region .Other Methods

	/// <summary>
	/// Called after handling all ticks of pending commands
	/// </summary>
	private void HandleSlideCmdOverlapping()
	{
		// If any update happens in a pass, end that pass and start over,
		// repeat until a full pass happens with no updates.
		while (1 + 1 == 2)
		{
			var dirtyIndex = -1;
			for (var i = 0; i < BlockedSlideCmds.Count; ++i)
			{
				if (BlockedSlideCmds[i].TryMovingByOverlap(this))
				{
					dirtyIndex = i;
					break;
				}
			}
			if (dirtyIndex == -1) // None has updated? it's over.
			{
				break;
			}

			// Something moved? can only move once per tick so bye-bye.
			BlockedSlideCmds.RemoveAt(dirtyIndex);
		}
		BlockedSlideCmds.Clear();
	}

	/// <summary>
	/// Silly little function... Needed because disconnection requires a reference to the callable, 
	/// otherwise I would have to disconnect everything, which I feel might bite me in the butt later.
	/// </summary>
	private void OnTile_NeedsRedraw()
	{
		QueueRedraw();
	}

	/// <summary>
	/// This adds the unit to the tree, so only call the setup function after this one.
	/// </summary>
	private void AddUnit(Unit newUnit)
	{
		m_Units.Add(newUnit);
		AddChild(newUnit);
	}

	#endregion
}