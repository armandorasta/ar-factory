using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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
	public Vector2I Dims => m_Dims;
	public readonly float CellWidth = 100.0f;
	public Tile this[int x, int y] => GetTile(new(x, y));


	// Privates
	private Vector2I m_Dims = new(15, 10);
	private List<Unit> m_Units = [];
	private List<Tile> m_Tiles = [];
	private List<CmdSlide> m_BlockedSlideCmdsByItems = [];


	#region .Essential Functions

	/// <summary>
	/// Checks if <paramref name="gloc"/> is within the world in grid-space.
	/// </summary>
	public bool IsWithin(Vector2I gloc) => GetGridRect().HasPoint(gloc);

	/// <summary>
	/// Returns enclosing Rect2I in grid-space.
	/// </summary>
	public Rect2I GetGridRect() => new(Vector2I.Zero, m_Dims);

	/// <summary>
	/// Returns enclosing Rect2 in world-space.
	/// </summary>
	public Rect2 GetWorldRect() => new(Vector2I.Zero, CellWidth * (Vector2)m_Dims);
	
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
	public int GridToIndex(Vector2I gloc) => gloc.Y * m_Dims.X + gloc.X;

	/// <summary>
	/// Converts an index to a vector in grid-space.
	/// This function does not bounds checking whatsoever.
	/// </summary>
	public Vector2I IndexToGrid(int index) => new(index % m_Dims.X, index / m_Dims.X);

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

	/// <summary>
	/// Same as <c>InstallTile(new Tile(Vector2I.Zero), gloc, bOverride)</c>
	/// </summary>
	/// <returns>The installed tile, or null when it fails</returns>
	public Tile InstallEmptyTile(Vector2I gloc, bool bOverride = false)
	{
		return InstallTile(new Tile(Vector2I.Zero), gloc, bOverride);
	}

	/// <summary>
	/// Adds the passed tile to the world, the location of the tile is not consider and is overriden
	/// by <paramref name="gloc"/>.
	/// </summary>
	/// <param name="bOverride">Only matters in debug mode</param>
	/// <returns>The installed tile, or null when it fails</returns>
	public Tile InstallTile(Tile tl, Vector2I gloc, bool bOverride = false)
	{
		Debug.AssertNotNull(tl);
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bOverride || !HasTile(gloc));
		if (tl is not null)
		{
			DeleteTile(gloc, true);
			m_Tiles[GridToIndex(gloc)] = tl;
			tl.SetGridLocUnsafe(gloc);
			// TODO: Replace this with something...
			// tl.Connect(Tile.SignalName.NeedsRedraw, Callable.From(OnTile_NeedsRedraw));
		}
		return tl;
	}

	/// <summary>
	/// Removes the tile from the world, destroys items within it, and sends it into oblivion.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	public void DeleteTile(Vector2I gloc, bool bMaybeNull = false)
	{
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bMaybeNull || HasTile(gloc));
		var i = GridToIndex(gloc);
		m_Tiles[i]?.DestroyItem(true);
		m_Tiles[i] = null;
	}

	/// <summary>
	/// Returns the moved tile, or null when it fails for some reason.
	/// Moves the tile from one location to another, along with the items it contains.
	/// Does nothing when <paramref name="gFrom"/> and <paramref name="gTo"/> are the same or when
	/// the moved tile does not exist.
	/// Same as <see cref="ExtractTile"/> followed by <see cref="InstallTile"/>.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	/// <param name="bOverride">Only matters in debug mode</param>
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
		var myTile = ExtractTile(gFrom, true);
		if (myTile is not null)
		{
			InstallTile(myTile, gTo, true);
		}
		return myTile;
	}

	/// <summary>
	/// Removes the tile from the world, and returns it.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	public Tile ExtractTile(Vector2I gloc, bool bMaybeNull = false)
	{
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bMaybeNull || HasTile(gloc));
		var i = GridToIndex(gloc);
		var myTile = m_Tiles[i];
		m_Tiles[i] = null;
		// TODO: replace this.
		// myTile.Disconnect(Tile.SignalName.NeedsRedraw, Callable.From(OnTile_NeedsRedraw));
		return myTile;
	}

	/// <summary>
	/// Swaps two tiles, or does nothing when <paramref name="gloc1"/> and <paramref name="gloc2"/> 
	/// are the same. This function acts like <see cref="MoveTile"/> when one of the tiles does
	/// not exist
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
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
			InstallTile(tl1, gloc2);
		}
		
		if (tl2 != null)
		{
			InstallTile(tl2, gloc1);
		}
	}


	#endregion // Tile Methods
	#region .Item Methods

	/// <summary>
	/// Returns the item just added or null when spawn fails. Null returns are considered errors
	/// in debug and crash the program.
	/// </summary>
	public Item SpawnItem(Vector2I gloc, int value)
	{
		Debug.AssertNotNull(GetTile(gloc));
		Debug.Assert(!GetTile(gloc).IsReserved());
		var tl = GetTile(gloc);
		if (tl is null || tl.IsReserved())
		{
			return null;
		}

		var newItem = ItemScene.Instantiate<Item>();
		tl.SetItemUnsafe(newItem);
		AddChild(newItem);
		newItem.Setup(this, gloc, value);
		return newItem;
	}

	/// <summary>
	/// Returns the passed item itself or null when it fails. Null returns are considered errors
	/// in debug and crash the program.
	/// This very same passed item is gauranteed to be installed, no new items are made.
	/// </summary>
	public Item InstallItem(Item it)
	{
		Debug.Assert(it.GetParent() is null); // No associated world.
		// TODO: When you add the reference to the parent tile, add an assert here for that.
		Debug.AssertNotNull(GetTile(it.GridLoc));
		Debug.Assert(!GetTile(it.GridLoc).IsReserved());
		
		var tl = GetTile(it.GridLoc);
		if (tl is null || tl.IsReserved())
		{
			return null;
		}
		
		it.GetParent()?.RemoveChild(it);
		tl.SetItemUnsafe(it); // Must be set before adding it to the tree.
		it.SyncPosWithGrid(this);
		AddChild(it);
		return it;
	}

	/// <summary>
	/// Returns the clone, not the original, or null if the item does not exits in the first place.
	/// Clones the item into another tile, or does nothing if <paramref name="gSrc"/> and 
	/// <paramref name="gDest"/> are the same, or when the item or its tile does not exist, or when
	/// the destination tile does not exist.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	/// <param name="bOverride">Only matters in debug mode</param>
	public Item CloneItem(Vector2I gSrc, Vector2I gDest, bool bMaybeNull = false, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gSrc));
		Debug.Assert(HasTile(gSrc));
		Debug.Assert(bMaybeNull || GetTile(gSrc).HasItem());
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(HasTile(gDest));
		Debug.Assert(bOverride || gSrc == gDest || !GetTile(gDest).HasItem());
		
		var srcTl = GetTile(gSrc);
		var destTl = GetTile(gDest);
		if (srcTl is null || !srcTl.HasItem() || destTl is null)
		{
			return null;
		}
		
		destTl.DestroyItem(true);
		return SpawnItem(gDest, srcTl.Item.Value);
	}

	/// <summary>
	/// Returns the teleported item, not the original, or null if the item does not exits in the 
	/// first place.
	/// Teleports the item to another tile, or does nothing if <paramref name="gSrc"/> and 
	/// <paramref name="gDest"/> are the same, or when the item or its tile does not exist, or when
	/// the destination tile does not exist.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	/// <param name="bOverride">Only matters in debug mode</param>
	public Item TeleportItem(Vector2I gSrc, Vector2I gDest, bool bMaybeNull = false, bool bOverride = false)
	{
		var it = TeleportItemNoSync(gSrc, gDest, bMaybeNull, bOverride);
		it?.SyncPosWithGrid(this);
		return it;
	}

	/// <summary>
	/// Same as <see cref="TeleportItem"/> but does not change the position of the item in the world.
	/// Calling <see cref="Item.SyncPosWithGrid"/> right after this is equivalent to <see cref="TeleportItem"/>.
	/// This function is much faster than <see cref="TeleportItem"/> because it does no marsheling.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	/// <param name="bOverride">Only matters in debug mode</param>
	public Item TeleportItemNoSync(Vector2I gSrc, Vector2I gDest, bool bMaybeNull = false, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gSrc));
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(HasTile(gSrc));
		Debug.Assert(HasTile(gDest));
		Debug.Assert(bMaybeNull || GetTile(gSrc).HasItem());
		Debug.Assert(bOverride || gSrc == gDest || !GetTile(gDest).HasItem());
		
		if (gSrc == gDest)
		{
			return GetTile(gSrc)?.GetItemMaybeNull();
		}

		var srcTl = GetTile(gSrc);
		var destTl = GetTile(gDest);
		if (srcTl is null || !srcTl.HasItem() || destTl is null)
		{
			return null;
		}

		var extractedTl = srcTl.ExtractItem();
		destTl.SetItemUnsafe(extractedTl); // Must be before adding to tree.
		AddChild(extractedTl);
		return destTl.Item;
	}


	#endregion // Item Methods
	#endregion // Essential Functions
	#region .Simulation Related

	public void DoPerFrame(float dt, Level lv)
	{
		foreach (var u in m_Units)
		{
			u.DoPerFrame(dt, lv);
		}
	}

	public void OnTick(Level lv)
	{
		Debug.AssertRefEq(lv.World, this);
		foreach (var tl in m_Tiles)
		{
			tl?.GetItemMaybeNull()?.ResetMovementFlag();
		}

		// This must happen before HandleCmdTick, otherwise the first tick will handle nothing.
		foreach (var u in m_Units)
		{
			u.PendNewCommands();
		}

		foreach (var u in m_Units)
		{
			u.PreprocessTick();
		}

		foreach (var u in m_Units)
		{
			u.HandleCmdTick(lv);
		}

		// Must happen after HandleCmdTick, otherwise it will do nothing since the queue is populated 
		// by HandleCmdTick.
		HandleSlideCmdOverlapping();
	}

	public void CleanUpAfterSim()
	{
		foreach (var u in m_Units)
		{
			u.Reset();
		}
		
		foreach (var tl in m_Tiles)
		{
			tl?.DestroyItem(true);
		}

		m_BlockedSlideCmdsByItems.Clear();
	}

	#endregion
	#region .Godot Overrides

	public override void _Ready()
	{
		Reset();
		SetDims(m_Dims);
		PlaceSomeUnits();
	}

	public override void _Draw()
	{
		DrawRect(new(Vector2.Zero, Size), new(0.0f, 0.1f, 0.0f));
		for (var i = 1; i < m_Dims.X; ++i)
		{
			DrawLine(new(i*CellWidth, 0.0f), new(i*CellWidth, Size.Y), new(0.0f, 0.18f, 0.0f));
		}
		for (var i = 1; i < m_Dims.Y; ++i)
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
		}
	}

	#endregion // Godot Overrides
	#region .Placement Functions

	[System.Diagnostics.Conditional("DEBUG")]
	private void PlaceSomeUnits()
	{
		// PlaceInjector([
		// 	new CmdSpawn(new(2, 7), 1),
		// 	new CmdSlide(new(2, 7), Direction.West),
		// 	new CmdSpawn(new(2, 7), 2),
		// 	new CmdSpawn(new(2, 7), 3),
		// ]);
		// PlaceSlider(new(1, 7), Direction.East);
		// PlaceSlider(new(2, 7), Direction.East);
		// PlaceSlider(new(3, 7), Direction.North);
		// PlaceSlider(new(3, 6), Direction.East);
		// PlaceUpdater(new(4, 6), Direction.East, 2, UpdateType.Double);
		// return;

		PlaceSupplier(new(0, 6), Direction.East, 3, [1, 2, 3, 4, 5, 6]);
		PlaceSlider(new(2, 7), Direction.East);
		PlaceSlider(new(3, 7), Direction.North);
		PlaceSlider(new(3, 6), Direction.East);
		PlaceUpdater(new(4, 6), Direction.East, 3, UpdateType.Double);
		PlaceSlider(new(5, 6), Direction.East);
		PlaceSlider(new(6, 6), Direction.South);
		PlaceSlider(new(6, 7), Direction.South);
		PlaceSlider(new(6, 8), Direction.West);
		PlaceUpdater(new(5, 8), Direction.West, 3, UpdateType.Double);
		PlaceSlider(new(4, 8), Direction.West);
		PlaceSlider(new(3, 8), Direction.North);
	}

	public UnCmdInjector PlaceInjector(IEnumerable<Command> cmdsToInject)
	{
		var u = new UnCmdInjector();
		AddUnit(u);
		u.Setup(this, cmdsToInject);
		return u;
	}

	public UnSupplier PlaceSupplier(Vector2I gloc, Direction dir, int rate, IEnumerable<int> seq)
	{
		var u = UnSupplierScene.Instantiate() as UnSupplier;
		AddUnit(u);
		u.Setup(this, gloc, rate, dir, seq);
		return u;
	}

	public UnSlider PlaceSlider(Vector2I gloc, Direction dir)
	{
		var u = UnSliderScene.Instantiate() as UnSlider;
		AddUnit(u);
		u.Setup(this, gloc, 1, dir);
		return u;
	}

	public UnUpdater PlaceUpdater(Vector2I gloc, Direction dir, int rate, UpdateType UpT)
	{
		var u = UnUpdaterScene.Instantiate<UnUpdater>();
		AddUnit(u);
		u.Setup(this, gloc, rate, dir, UpT);
		return u;
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

	public void SetDims(Vector2I newDims)
	{
		var oldDims = m_Dims;
		m_Dims = newDims;
		Size = CellWidth * (Vector2)newDims;

		var oldTiles = m_Tiles;
		m_Tiles = [];
		for (var i = 0; i < m_Dims.X * m_Dims.Y; ++i)
		{
			m_Tiles.Add(null);
		}

		// Remove cut-off units
		m_Units = [];
		for (var i = m_Units.Count - 1; i >= 0;)
		{
			var u = m_Units[i];
			if (!Unit.CanFitIn(this, u.GridLoc, u.Dims))
			{
				RemoveUnit(u);
			}
			else
			{
				--i;
			}
		}

		// Copy over the tiles.
		for (var y = 0; y < int.Min(oldDims.Y, newDims.Y); ++y)
		{
			for (var x = 0; x < int.Min(oldDims.X, newDims.X); ++x)
			{
				m_Tiles[y * newDims.X + x] = oldTiles[y * oldDims.X + x];
			}
		}
	}

	public void Reset()
	{
		foreach (var tl in m_Tiles)
		{
			tl?.DestroyItem(true);
		}
		m_Tiles.Clear();
		for (var i = 0; i < m_Dims.X * m_Dims.Y; ++i)
		{
			m_Tiles.Add(null);
		}

		foreach (var u in m_Units)
		{
			u.QueueFree();
			RemoveChild(u);
		}
		m_Units.Clear();
		
		m_BlockedSlideCmdsByItems.Clear();
	}


	/// <summary>
	/// Adds a slide command blocked by another item in the way only, all other types crash the
	/// program.
	/// </summary>
	public void QueueBlockedSlideCmdByAnotherItem(CmdSlide cmd)
	{
		Debug.Assert(HasTile(cmd.GridFrom));
		Debug.Assert(GetTile(cmd.GridFrom).HasItem()); // Is there an actual item?
		Debug.Assert(GetTile(cmd.GetGridTo()).IsReserved()); // And is it blocked?
		// For now dublication is not allowed, although logically it should be xd.
		Debug.Assert(!m_BlockedSlideCmdsByItems.Contains(cmd));
		
		m_BlockedSlideCmdsByItems.Add(cmd);
	}

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
			for (var i = 0; i < m_BlockedSlideCmdsByItems.Count; ++i)
			{
				if (m_BlockedSlideCmdsByItems[i].TryMovingByOverlap(this))
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
			m_BlockedSlideCmdsByItems.RemoveAt(dirtyIndex);
		}
		m_BlockedSlideCmdsByItems.Clear();
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

	private void RemoveUnit(Unit u)
	{
		u.QueueFree();
		RemoveChild(u);
		m_Units.Remove(u);
	}

	#endregion
}