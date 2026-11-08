using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ArFactory;

public partial class WorldPanel : Godot.Panel, ITileOwner, IDestroyable
{
	// Constants
	public static readonly Vector2I MinDims = new(3, 3);
	public static readonly Vector2I MaxDims = new(100, 100);

	// Events
	public event Action? Redrawing;
	public event Action? Cleared;
	public event Action? DimensionsChanged;


	// Scenes
	public static readonly Godot.PackedScene ItemScene = GD.Load<Godot.PackedScene>("res://scenes/Item.tscn");


	// Nodes
	public Godot.Sprite2D BGSprite => m_BGSprite!;

	private Godot.Sprite2D? m_BGSprite;


	// Other statics
	public static readonly Godot.Font DebugFont = GD.Load<Godot.Font>("res://resources/fonts/AnonymousPro-Regular.ttf");


	// Public interface
	public Vector2I Dimensions => m_Dims;
	public readonly float CellWidth = 100.0f;
	public Tile? this[int x, int y] => GetTileOrNull(new(x, y));
	public bool DebugDrawEnabled { get; set; } = false;


	// Privates
	private Vector2I m_Dims = new(15, 10);
	// This here is a grid, hence the Tile?
	private List<Tile?> m_Tiles = [];
	// This here is just a normal list referencing tiles already in m_Tiles.
	private readonly List<Tile> m_OrphanTiles = [];

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


	#endregion // Essential Functions
	#region .Tile Methods

	/// <summary>
	/// Same as <seealso cref="IsWithin"/>, but also checks if a tile is allocated in that spot.
	/// </summary>
	public bool HasTileOn(Vector2I gloc) => IsWithin(gloc) && m_Tiles[GridToIndex(gloc)] != null;
	/// <summary>
	/// Gets the tile in the location specified, but returns <see langword="null"/> for all invalid cases.
	/// </summary>
	public Tile? GetTileOrNull(Vector2I gloc) => IsWithin(gloc) ? m_Tiles[GridToIndex(gloc)] : null;
	/// <summary>
	/// Gets the tile in the location specified, and does no bounds checking whatsoever, meaning getting tiles out of 
	/// bounds is undefined behaviour.
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

	public bool RecieveTile(Tile tl)
	{
		if (m_OrphanTiles.Contains(tl))
		{
			return false;
		}

		m_OrphanTiles.Add(tl);
		return true;
	}

	public bool TakeAwayTile(Tile tl)
	{
		Debug.Assert(m_OrphanTiles.Contains(tl));
		m_OrphanTiles.Remove(tl);
		return true;
	}

	/// <summary>
	/// Returns true if the tile is installed within this world, returns false if it's floating or
	/// installed in another world somehow.
	/// </summary>
	public bool IsInstalled(Tile tl) => IsWithin(tl.GLocation) && object.ReferenceEquals(tl, GetTile(tl.GLocation));

	public IEnumerable<Tile> GetTilesIter() => m_Tiles.Where(tl => tl is not null)!;

	/// <summary>
	/// Installs a free tile that allows items in from any direction.
	/// </summary>
	/// <param name="bOwned">
	/// If false, the tile will be managed by the world, otherwise deleting the tile must be done by the owner.
	/// </param>
	/// <returns>The installed tile, or null when it fails</returns>
	public Tile AddEmptyTile(Vector2I gloc, ITileOwner? owner = null, bool bOverride = false)
	{
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bOverride || !HasTileOn(gloc));

		var tl = InstallTile(Tile.CreateEmpty(), gloc, bOverride);
		if (owner is not null)
		{
			tl.Owner = owner;
		}
		return tl;
	}

	/// <summary>
	/// Adds the passed tile to the world, the location of the tile is not consider and is overriden by 
	/// <paramref name="gloc"/>.
	/// </summary>
	/// <param name="bOverride">Only matters in debug mode</param>
	/// <returns>The installed tile, or null when it fails</returns>
	public Tile InstallTile(Tile tl, Vector2I gloc, bool bOverride = false)
	{
		Debug.AssertNotNull(tl);
		Debug.Assert(IsWithin(gloc));
		Debug.Assert(bOverride || !HasTileOn(gloc));
		DeleteTile(gloc, true);
		m_Tiles[GridToIndex(gloc)] = tl;
		tl._SetGLUnsafe(gloc);
		if (!tl.IsOwned())
		{
			tl.Owner = this;
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
		Debug.Assert(bMaybeNull || HasTileOn(gloc));
		var i = GridToIndex(gloc);
		m_Tiles[i]?.Destroy();
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
		Debug.Assert(bMaybeNull || HasTileOn(gFrom));
		Debug.Assert(bOverride || gFrom == gTo || !HasTileOn(gTo));
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
		Debug.Assert(bMaybeNull || HasTileOn(gloc));
		var i = GridToIndex(gloc);
		var tl = m_Tiles[i];
		m_Tiles[i] = null;
		if (tl is not null && tl.Owner == this)
		{
			tl.GetDisowned();
		}
		return tl;
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
		Debug.Assert(bMaybeNull || HasTileOn(gloc1) && HasTileOn(gloc2));
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
		Debug.Assert(bTileMaybeNull || HasTileOn(gloc));
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
		Debug.Assert(HasTileOn(gloc));
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
		Debug.Assert(HasTileOn(gSrc));
		Debug.Assert(GetTileNonNull(gSrc).HasItem());
		Debug.Assert(IsWithin(gDest));
		Debug.Assert(HasTileOn(gDest));
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
		Debug.Assert(HasTileOn(gSrc));
		Debug.Assert(HasTileOn(gDest));
		Debug.Assert(GetTileNonNull(gSrc).HasItem());
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
	/// See <see cref="TeleportItemNoSync(Vector2I, Vector2I, bool)"/>
	/// </summary>
	public Item? TeleportItemNoSync(Tile tlSrc, Tile tlDest, bool bOverride = false)
	{
		Debug.Assert(IsInstalled(tlSrc));
		Debug.Assert(IsInstalled(tlDest));
		return TeleportItemNoSync(tlSrc.GLocation, tlDest.GLocation, bOverride);
	}


	#endregion // Item Methods
	#region .Godot Overrides

	public override void _Ready()
	{
		LoadNodes();
		Clear();
		
		var dims = m_Dims;
		m_Dims = Vector2I.Zero;
		SetDimensions(dims);
	}

	private void LoadNodes()
	{
		m_BGSprite = GetNode<Godot.Sprite2D>("BGSprite");
	}

	/// <summary>
	/// Just calls <see cref="Clear"/>.
	/// </summary>
	public void Destroy()
	{
		Clear();
	}

	public override void _Draw()
	{
#if false
		DrawRect(new(Vector2.Zero, Size), new(0.0f, 0.1f, 0.0f));
		for (var i = 1; i < m_Dims.X; ++i)
		{
			DrawLine(new(i*CellWidth, 0.0f), new(i*CellWidth, Size.Y), new(0.0f, 0.18f, 0.0f));
		}
		for (var i = 1; i < m_Dims.Y; ++i)
		{
			DrawLine(new(0.0f, i*CellWidth), new(Size.X, i*CellWidth), new(0.0f, 0.18f, 0.0f));
		}
#endif
		if (DebugDrawEnabled)
		{
			foreach (var tl in m_OrphanTiles)
			{
				tl?.DebugDraw(this);
			}
		}

		Redrawing?.Invoke();
	}

	#endregion // Godot Overrides
	#region .Other Methods

	/// <summary>
	/// This is a very expenesive operation that will shift around all the tiles, and change the dimensions of the 
	/// godot panel and maybe the sprite in the future.
	/// </summary>
	public void SetDimensions(Vector2I toWhat)
	{
		var oldDims = m_Dims;
		var newDims = toWhat.Clamp(MinDims, MaxDims);
		if (oldDims == newDims)
		{
			return;
		}
		
		m_Dims = newDims;
		Size = CellWidth * (Vector2)m_Dims;

		// Let's let the watchers react before we scramble the tiles and stuff.
		DimensionsChanged?.Invoke();

		var oldTiles = m_Tiles;
		m_Tiles = [];
		for (var i = 0; i < newDims.X * newDims.Y; ++i)
		{
			m_Tiles.Add(null);
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

	public void Clear()
	{
		Cleared?.Invoke();

		for (var i = m_OrphanTiles.Count - 1; i >= 0; --i)
		{
			var tl = m_OrphanTiles[i];
			DeleteTile(tl);
		}

		Debug.Assert(m_Tiles.All(tl => tl is null));
		Debug.Assert(m_OrphanTiles.All(tl => tl is null));
	}

	#endregion // .Other Methods
}