using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArFactory;

public partial class WorldPanel : Godot.Panel
{
	// Constants
	public static readonly Vector2I MinDims = new(3, 3);
	public static readonly Vector2I MaxDims = new(100, 100);


	// Scenes
	public static readonly Godot.PackedScene ItemScene = GD.Load<Godot.PackedScene>("res://scenes/Item.tscn");
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


	// Other statics
	public static readonly Godot.Font DebugFont = GD.Load<Godot.Font>("res://resources/fonts/AnonymousPro-Regular.ttf");


	// Public interface
	public Vector2I Dimensions => m_Dims;
	public readonly float CellWidth = 100.0f;
	public Tile? this[int x, int y] => GetTileOrNull(new(x, y));


	// Privates
	private Vector2I m_Dims = new(50, 50);
	private readonly List<Unit> m_Units = [];
	private List<Tile?> m_Tiles = [];


	#region .Essential Functions

	/// <summary>
	/// Checks if <paramref name="gloc"/> is within the world in grid-space.
	/// </summary>
	public bool IsWithin(Vector2I gloc) => GetGridRect().HasPoint(gloc);

	/// <summary>
	/// Returns enclosing Rect2I in grid-space.
	/// </summary>
	public Godot.Rect2I GetGridRect() => new(Vector2I.Zero, m_Dims);

	/// <summary>
	/// Returns enclosing Rect2 in world-space.
	/// </summary>
	public Godot.Rect2 GetWorldRect() => new(Vector2I.Zero, CellWidth * (Vector2)m_Dims);

	public Vector2 GetCellSize() => new(CellWidth, CellWidth);
	
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

	public Vector2I GetMouseGridLocation() => PosToGrid(GetLocalMousePosition());

	/// <summary>
	/// Same as <seealso cref="IsWithin"/>, but also checks if a tile is allocated in that spot.
	/// </summary>
	public bool HasTile(Vector2I gloc) => IsWithin(gloc) && m_Tiles[GridToIndex(gloc)] != null;
	/// <summary>
	/// Gets the tile in the location specified, but also returns null for out of bounds locations.
	/// </summary>
	public Tile? GetTileOrNull(Vector2I gloc) => IsWithin(gloc) ? m_Tiles[GridToIndex(gloc)] : null;
	/// <summary>
	/// Gets the tile in the location specified, and does no bounds checking whatsoever, meaning getting
	/// tiles out of bounds is undefined behaviour.
	/// </summary>
	public Tile? GetTile(Vector2I gloc) => m_Tiles[GridToIndex(gloc)];
	/// <summary>
	/// Throws if the tile is null in debug, equivalent to <see cref="GetTile"/> in release.
	/// </summary>
	public Tile GetTileNonNull(Vector2I gloc)
	{
#if DEBUG
		return GetTile(gloc) ?? throw new InvalidProgramException("GetTileNonNull returns a null tile??");
#else
		return GetTile(gloc)!;
#endif
	}
	/// <summary>
	/// Returns true if the tile is installed within this world, returns false if it's floating or
	/// installed in another world somehow.
	/// </summary>
	public bool IsInstalled(Tile tl) => IsWithin(tl.GLocation) && object.ReferenceEquals(tl, GetTileOrNull(tl.GLocation));

	/// <summary>
	/// Returns the whichever unit that <paramref name="gloc"/> lands on.
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

	public void DeleteUnit(Unit u)
	{
		u.Reset();
		
		var limVec = u.Dir is Direction.East or Direction.West
			? u.Dimensions
			: new Vector2I(u.Dimensions.Y, u.Dimensions.X);
		for (int y = 0; y < limVec.Y; ++y)
		{
			for (int x = 0; x < limVec.X; ++x)
			{
				DeleteTile(u.GLocation + new Vector2I(x, y));
			}
		}

		RemoveChild(u);
		u.QueueFree();
		m_Units.Remove(u);
	}


	#region .Tile Methods

	/// <summary>
	/// Same as <c>InstallTile(new Tile(Vector2I.Zero), gloc, bOverride)</c>
	/// </summary>
	/// <returns>The installed tile, or null when it fails</returns>
	public Tile InstallEmptyTile(Vector2I gloc, bool bOverride = false)
	{
		return InstallTile(new Tile(), gloc, bOverride);
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
		DeleteTile(gloc, true);
		m_Tiles[GridToIndex(gloc)] = tl;
		tl._SetGLUnsafe(gloc);
		// TODO: Replace this with something...
		// tl.Connect(Tile.SignalName.NeedsRedraw, Callable.From(OnTile_NeedsRedraw));
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
	/// Removes the tile from the world, destroys items within it, and sends it into oblivion.<br/>
	/// <b>Tile must be in this world, otherwise the behaviour is undefined.</b>
	/// </summary>
	public void DeleteTile(Tile tl)
	{
		Debug.Assert(IsInstalled(tl));
		DeleteTile(tl.GLocation);
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
	public Tile? MoveTile(Vector2I gFrom, Vector2I gTo, bool bMaybeNull = false, bool bOverride = false)
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
	/// <b>Tile must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="MoveTile(Vector2I, Vector2I, bool, bool)"/>
	/// </summary>
	public Tile MoveTile(Tile tl, Vector2I gTo, bool bOverride = false)
	{
		Debug.Assert(IsInstalled(tl));
		return MoveTile(tl.GLocation, gTo, false, bOverride)!;
	}

	/// <summary>
	/// Removes the tile from the world, and returns it.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	public Tile? ExtractTile(Vector2I gloc, bool bMaybeNull = false)
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
			// Should technically throw for out of bounds, but meh...
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

	/// <summary>
	/// <b>Both tiles must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="SwapTiles(Vector2I, Vector2I, bool)"/>
	/// </summary>
	public void SwapTiles(Tile tl0, Tile tl1)
	{
		Debug.Assert(IsInstalled(tl0));
		Debug.Assert(IsInstalled(tl1));
		SwapTiles(tl0.GLocation, tl1.GLocation, false);
	}


	#endregion // Tile Methods
	#region .Item Methods

	/// <summary>
	/// Returns the item just added or null when spawn fails. Null returns are considered errors in debug and crash 
	/// the program.
	/// </summary>
	public Item? SpawnItem(Vector2I gloc, int value)
	{
		Debug.AssertNotNull(GetTileOrNull(gloc));
		Debug.Assert(!GetTile(gloc)!.IsReserved());
		
		var tl = GetTile(gloc);
		if (tl is null || tl.IsReserved())
		{
			return null;
		}

		var newItem = ItemScene.Instantiate<Item>();
		tl._SetItemUnsafe(newItem);
		AddChild(newItem);
		newItem.Setup(this, gloc, value);
		return newItem;
	}

	/// <summary>
	/// <b>Tile must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="SpawnItem(Vector2I, int)"/>
	/// </summary>
	public Item? SpawnItem(Tile tl, int value)
	{
		Debug.Assert(IsInstalled(tl));
		return SpawnItem(tl.GLocation, value);
	}

	public void DestroyItem(Vector2I gloc, bool bTileMaybeNull = false, bool bItemMaybeNull = false, bool bMaybeMidAnim = false)
	{
		Debug.Assert(bTileMaybeNull || HasTile(gloc));
		if (GetTile(gloc) is { } tl)
		{
			tl.DestroyItem(bItemMaybeNull, bMaybeMidAnim);
		}
	}

	public void DestroyItem(Tile tl, bool bMaybeNull = false, bool bMaybeMidAnim = false)
	{
		Debug.Assert(IsInstalled(tl));
		tl.DestroyItem(bMaybeNull, bMaybeMidAnim);
	}

	/// <summary>
	/// Returns the passed item itself or null when it fails. Null returns are considered errors
	/// in debug and crash the program.
	/// This very same passed item is gauranteed to be installed, no new items are made.
	/// </summary>
	public Item? InstallItem(Item it, Vector2I gloc)
	{
		Debug.AssertNotNull(it); // No installing nulls!
		Debug.Assert(it.GetParent() is null); // No associated world.
		Debug.Assert(it.ParentTile is null);
		Debug.Assert(HasTile(gloc));
		Debug.Assert(!GetTileNonNull(gloc).IsReserved());

		var tl = GetTile(gloc);
		if (tl is null || tl.IsReserved())
		{
			return null;
		}
		
		it.GetParent()?.RemoveChild(it);
		tl._SetItemUnsafe(it); // Must be set before adding it to the tree.
		it.SyncPosWithGrid(this);
		AddChild(it);
		return it;
	}

	/// <summary>
	/// <b>Tile must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="InstallItem(Item, Vector2I)"/>
	/// </summary>
	public Item? InstallItem(Item it, Tile tl)
	{
		Debug.Assert(IsInstalled(tl));
		return InstallItem(it, tl.GLocation);
	}

	/// <summary>
	/// Returns the clone, not the original, or null if the item does not exits in the first place.
	/// Clones the item into another tile, or does nothing if <paramref name="gSrc"/> and 
	/// <paramref name="gDest"/> are the same, or when the destination tile does not exist.
	/// </summary>
	/// <param name="bOverride">Only matters in debug mode</param>
	public Item? CloneItem(Vector2I gSrc, Vector2I gDest, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gSrc));
		Debug.Assert(HasTile(gSrc));
		Debug.Assert(GetTileNonNull(gSrc).HasItem());
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(HasTile(gDest));
		Debug.Assert(bOverride || !GetTileNonNull(gDest).HasItem());
		
		var srcTl = GetTile(gSrc);
		var destTl = GetTile(gDest);
		if (srcTl is null || !srcTl.HasItem() || destTl is null)
		{
			return null;
		}
		
		destTl.DestroyItem(true);
		return SpawnItem(gDest, srcTl.Item.Value)!;
	}

	/// <summary>
	/// <b>Tiles must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="CloneItem(Vector2I, Vector2I, bool, bool)"/>
	/// </summary>
	public Item? CloneItem(Tile tlSrc, Tile tlDest, bool bOverride = false)
	{
		Debug.Assert(IsInstalled(tlSrc));
		Debug.Assert(IsInstalled(tlDest));
		return CloneItem(tlSrc.GLocation, tlDest.GLocation, bOverride);
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
	public Item? TeleportItem(Vector2I gSrc, Vector2I gDest, bool bOverride = false)
	{
		var it = TeleportItemNoSync(gSrc, gDest, bOverride);
		if (gSrc != gDest) // Avoid marshalling for in-place teleports.
		{
			it?.SyncPosWithGrid(this);		
		}
		return it;
	}

	/// <summary>
	/// <b>Tiles must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="TeleportItem(Vector2I, Vector2I, bool, bool)"/>
	/// </summary>
	public Item? TeleportItem(Tile tlSrc, Tile tlDest, bool bOverride = false)
	{
		Debug.Assert(IsInstalled(tlSrc));
		Debug.Assert(IsInstalled(tlDest));
		return TeleportItem(tlSrc.GLocation, tlDest.GLocation, bOverride);
	}

	/// <summary>
	/// Same as <see cref="TeleportItem"/> but does not update the live position of the item in the world.
	/// Calling <see cref="Item.SyncPosWithGrid"/> right after this is equivalent to 
	/// <see cref="TeleportItem"/>.
	/// This function is much faster than <see cref="TeleportItem"/> because it does no marshaling.
	/// </summary>
	/// <param name="bMaybeNull">Only matters in debug mode</param>
	/// <param name="bOverride">Only matters in debug mode</param>
	public Item? TeleportItemNoSync(Vector2I gSrc, Vector2I gDest, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gSrc));
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(HasTile(gSrc));
		Debug.Assert(HasTile(gDest));
		Debug.Assert(GetTileNonNull(gSrc).IsReserved());
		Debug.Assert(bOverride || gSrc == gDest || !GetTileNonNull(gDest).IsReserved());
		
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

		var extractedIt = srcTl.ExtractItem();
		if (extractedIt is not null)
		{
			destTl._SetItemUnsafe(extractedIt, true); // Must be before adding to tree.
			AddChild(extractedIt);
		}
		return extractedIt;
	}

	/// <summary>
	/// <b>Tiles must be in this world, otherwise the behaviour is undefined.</b>
	/// See <see cref="TeleportItemNoSync(Vector2I, Vector2I, bool, bool)"/>
	/// </summary>
	public Item? TeleportItemNoSync(Tile tlSrc, Tile tlDest, bool bOverride = false)
	{
		Debug.Assert(IsInstalled(tlSrc));
		Debug.Assert(IsInstalled(tlDest));
		return TeleportItemNoSync(tlSrc.GLocation, tlDest.GLocation, bOverride);
	}


	#endregion // Item Methods
	#endregion // Essential Functions
	#region .Simulation Related

	public void OnTick(Simulator.PendProxy pender)
	{
		foreach (var tl in m_Tiles)
		{
			tl?.GetItemMaybeNull()?.ResetMovementFlag();
		}

		foreach (var u in m_Units)
		{
			u.PreprocessTick();
		}

		foreach (var u in m_Units)
		{
			pender.PenderUnit = u;
			u.HandlePendingNewCommands(pender);
		}
	}

	public enum TickState
	{
		NothingSus,
		RanOutOfItems,
		SuccessfulRun,
	}

	public TickState CheckForErrorsThisTick()
	{
		var suppliers = m_Units.Where(u => u is UNSupplier).Select(u => (u as UNSupplier)!);
		var demanders = m_Units.Where(u => u is UNDemander).Select(u => (u as UNDemander)!);

		if (demanders.Any(_ => true) // No success ever if there are no demanders obviously...
			&& demanders.All(d => d.IsDoneWithOutputSeq() && !d.HasPendingCmds()))
		{
			return TickState.SuccessfulRun;
		}
		
		// We will check if there are any suppliers in the first place, because we don't want to crash the  simulation 
		// for invalid levels, aka the ones constructed in tests...
		if (suppliers.Any(_ => true) 
			&& suppliers.All(s => s.IsDoneWithInputSeq()) 
			&& m_Tiles.All(tl => tl is null || !tl.HasItem()))
		{
			return TickState.RanOutOfItems;
		}

		return TickState.NothingSus;
	}

	public void CleanUpAfterSim()
	{
		foreach (var u in m_Units)
		{
			u.Reset();
		}
		
		foreach (var tl in m_Tiles)
		{
			tl?.DestroyItem(true, true);
		}
	}

	#endregion
	#region .Godot Overrides

	public override void _Ready()
	{
		Reset();
		SetDims(m_Dims, true);
		// PlaceSomeUnits();
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
			u.QueueRedraw();
		}
	}

	#endregion // Godot Overrides
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

	public UNCmdInjector PlaceInjector()
	{
		var injy = new UNCmdInjector();
		AddUnit(injy);
		injy.Setup(this);
		return injy;
	}

	public UNSupplier PlaceSupplier(Vector2I gloc, Direction dir, int rate, IEnumerable<int> seq)
	{
		var suppy = UNSupplierScene.Instantiate<UNSupplier>();
		AddUnit(suppy);
		suppy.Setup(this, gloc, rate, dir, seq);
		return suppy;
	}

	public UNSlider PlaceSlider(Vector2I gloc, Direction dir, int rate = 1)
	{
		var slidy = UNSliderScene.Instantiate<UNSlider>();
		AddUnit(slidy);
		slidy.Setup(this, gloc, rate, dir);
		return slidy;
	}

	public UNUpdater PlaceUpdater(Vector2I gloc, Direction dir, int rate, UNUpdater.UpdateType UpT)
	{
		var updater = UNUpdaterScene.Instantiate<UNUpdater>();
		AddUnit(updater);
		updater.Setup(this, gloc, rate, dir, UpT);
		return updater;
	}

	public UNBin PlaceBin(Vector2I gloc)
	{
		var bin = UNBinScene.Instantiate<UNBin>();
		AddUnit(bin);
		bin.Setup(this, gloc);
		return bin;
	}

	public UNBranch PlaceBranch(Vector2I gloc, Direction dir, int rate, CmdIf.CheckType check, int value)
	{
		var bra = UNBranchScene.Instantiate<UNBranch>();
		AddUnit(bra);
		bra.Setup(this, gloc, dir, rate, check, value);
		return bra;
	}

	public UNDemander PlaceDemander(Vector2I gloc, Direction dir, IEnumerable<int> seq)
	{
		var dem = UNDemanderScene.Instantiate<UNDemander>();
		AddUnit(dem);
		dem.Setup(this, gloc, dir, seq);
		return dem;
	}

	public UNCombiner PlaceCombiner(Vector2I gloc, Direction dir, int rate, BinaryOperation op)
	{
		var com = UNCombinerScene.Instantiate<UNCombiner>();
		AddUnit(com);
		com.Setup(this, gloc, dir, rate, op);
		return com;
	}

	public UNCloner PlaceCloner(Vector2I gloc, Direction dir, int rate)
	{
		var cloner = UNClonerScene.Instantiate<UNCloner>();
		AddUnit(cloner);
		cloner.Setup(this, gloc, dir, rate);
		return cloner;
	}

	public UNTeleInput PlaceTeleInput(Vector2I gloc, Direction dir, int rate)
	{
		var teleIn = UNTeleInputScene.Instantiate<UNTeleInput>();
		AddUnit(teleIn);
		teleIn.Setup(this, gloc, rate, dir);
		return teleIn;
	}

	public UNTeleOutput PlaceTeleOutput(Vector2I gloc, Direction dir, int delay)
	{
		var teleIn = UNTeleOutputScene.Instantiate<UNTeleOutput>();
		AddUnit(teleIn);
		teleIn.Setup(this, gloc, dir, delay);
		return teleIn;
	}

	#endregion // Placement Functions
	#region .Other Methods

	public void SetDims(Vector2I toWhat, bool bForce = false)
	{
		var oldDims = m_Dims;
		var newDims = toWhat.Clamp(MinDims, MaxDims);
		if (!bForce && oldDims == newDims)
		{
			return;
		}
		
		m_Dims = newDims;
		Size = CellWidth * (Vector2)m_Dims;

		var oldTiles = m_Tiles;
		m_Tiles = [];
		for (var i = 0; i < newDims.X * newDims.Y; ++i)
		{
			m_Tiles.Add(null);
		}

		// Remove cut-off units
		for (var i = m_Units.Count - 1; i >= 0; --i)
		{
			var u = m_Units[i];
			if (!Unit.CanFitIn(this, u.GLocation, u.Dimensions, u.Dir))
			{
				DeleteUnit(u);
			}
		}

		// Copy over the tiles.
		for (int y = 0, ylim = int.Min(oldDims.Y, newDims.Y); y < ylim; ++y)
		{
			for (int x = 0, xlim = int.Min(oldDims.X, newDims.X); x < xlim; ++x)
			{
				m_Tiles[y * newDims.X + x] = oldTiles[y * oldDims.X + x];
			}
		}

		QueueRedraw();
	}

	public void Reset()
	{
		for (var i = m_Units.Count - 1; i >= 0; --i)
		{
			DeleteUnit(m_Units[i]);
		}
		
		foreach (var tl in m_Tiles)
		{
			tl?.DestroyItem(true);
		}
		m_Tiles.Clear();
		for (var i = 0; i < m_Dims.X * m_Dims.Y; ++i)
		{
			m_Tiles.Add(null);
		}
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
	#region .Test Specific
#if TEST
#endif // TEST
	#endregion .Test Specific


}