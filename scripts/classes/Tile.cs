using System;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public class Tile(Vector2I gloc)
{
	[Flags]
	private enum WallType
	{
		Nothing = 0x00,
		Solid   = 0x01,
		Input   = 0x02,
		Output  = 0x03,
	}

	private const int WallFieldWidth = sizeof(Int32) * 8 / 4;
	private const int WallFieldMask = (1 << WallFieldWidth) - 1;


	// Publics
	public Vector2I GridLoc => m_GridLoc;
	/// <summary>
	/// This accessor crashes the program if the item is null, use <see cref="GetItemMaybeNull"/>
	/// if that behaviour is not desired.
	/// </summary>
	public Item Item
	{
		get
		{
			Debug.Assert(m_Item != null);
			return m_Item;
		}	
	}


	// Privates
	private Vector2I m_GridLoc = gloc;
	// High to low: west -> south -> east -> north.
	private Int32 m_WallFlags;
	private Item m_Item;
	private bool m_bReserved = false;


	// Factory Methods
	public static Tile CreateSolid(Vector2I gloc)
	{
		var res = new Tile(gloc);
		res.SetWall(Direction.North, WallType.Solid);
		res.SetWall(Direction.East, WallType.Solid);
		res.SetWall(Direction.South, WallType.Solid);
		res.SetWall(Direction.West, WallType.Solid);
		return res;
	}


	/// <summary>
	/// Use this if the tile is floating, otherwise use <see cref="WorldPanel.MoveTile"/> instead.
	/// </summary>
	public void SetGridLocUnsafe(Vector2I gloc) { m_GridLoc = gloc; }


	#region .Wall Methods

	public bool IsClear(Direction dir) => GetWall(dir) == WallType.Nothing;
	public bool IsSolid(Direction dir) => GetWall(dir) == WallType.Solid;
	public bool IsInput(Direction dir) => GetWall(dir) == WallType.Input;
	public bool IsOutput(Direction dir) => GetWall(dir) == WallType.Output;

	public Tile ClearWall(Direction dir)
	{
		return SetWall(dir, WallType.Nothing);
	}

	public Tile MakeSolid(Direction dir, bool bOverride = false)
	{
		Debug.Assert(bOverride || GetWall(dir) == WallType.Nothing);
		return SetWall(dir, WallType.Solid);
	}

	public Tile AddInput(Direction dir, bool bOverride = false)
	{
		Debug.Assert(bOverride || GetWall(dir) == WallType.Nothing);
		return SetWall(dir, WallType.Input);
	}

	public Tile AddOutput(Direction dir, bool bOverride = false)
	{
		Debug.Assert(bOverride || GetWall(dir) == WallType.Nothing);
		return SetWall(dir, WallType.Output);
	}


	private static int WallDirToShift(Direction dir)
	{
		Debug.Assert(Enum.IsDefined(dir));
		return (int)dir * WallFieldWidth;
	}

	private Tile SetWall(Direction dir, WallType wtype)
	{
		Debug.Assert(Enum.IsDefined(dir));
		m_WallFlags |= (int)wtype << WallDirToShift(dir);
		return this;
	}

	private WallType GetWall(Direction dir) 
	{
		Debug.Assert(Enum.IsDefined(dir));
		return (WallType)((m_WallFlags >> WallDirToShift(dir)) & WallFieldMask);
	}
	
	
	#endregion // .Wall Methods


	/// <summary>
	/// Returns true if the item can slide into through the edge facing <paramref name="wallDir"/>.
	/// This function does not check for items in the tile.
	/// </summary>
	/// <param name="wallDir">Direction of the tile edge</param>
	public bool CanItemEnterInDir(Direction wallDir)
	{
		return GetWall(wallDir.Invert()) switch
		{
			WallType.Nothing => true,
			WallType.Input => true,
			_ => false,
		};
	}

	public bool CanItemExitFromDir(Direction wallDir)
	{
		return GetWall(wallDir.Invert()) switch
		{
			WallType.Nothing => true,
			WallType.Output => true,
			_ => false,
		};
	}

	/// <summary>
	/// Items are not allowed to slide into reserved tiles.<br/>
	/// </summary>
	public bool IsReserved() => m_bReserved;

	/// <summary>
	/// Items are not allowed to slide into reserved tiles. <br/>
	/// </summary>
	public virtual void Reserve() { m_bReserved = true; }

	/// <summary>
	/// Rotates the unit 90 degrees clockwise.
	/// </summary>
	public void Rotate90()
	{
		// Before: west -> [south -> east -> north].
		// After: [south -> east -> north] -> west.
		// North is what was west, west is what was south...
		m_WallFlags = int.RotateLeft(m_WallFlags, WallFieldWidth);
	}

	/// <summary>
	/// Rotates the unit 270 degrees clockwise or 90 degrees counter-clockwise.
	/// </summary>
	public void Rotate270() 
	{ 
		// Before: [west -> south -> east]  -> north.
		// After : north -> [west  -> south -> east].
		// North is what was east, west is what was north...
		m_WallFlags = int.RotateRight(m_WallFlags, WallFieldWidth);
	}

	/// <summary>
	/// Rotates the unit 180 degrees.
	/// </summary>
	public void Rotate180()
	{
		// Before: [west -> south] -> [east  -> north].
		// After : [east  -> north] -> [west -> south].
		// North is what was south, west is what was east...
		m_WallFlags = int.RotateRight(m_WallFlags, 2*WallFieldWidth);
	}


	#region .Item Methods

	public bool HasItem() => m_Item is not null;
	public Item GetItemMaybeNull() => m_Item;

	/// <summary>
	/// This doesn't sync the world position as the item doesn't have access to the world, hence
	/// use <see cref="WorldPanel.SpawnItem"/> instead.
	/// </summary>
	public void SetItemUnchecked(Item newItem, bool bOverride = false)
	{
		Debug.AssertIs(newItem.GetParent(), typeof(WorldPanel));
		Debug.Assert(bOverride || !HasItem());
		DestroyItem(true);
		m_Item = newItem;
		m_Item.SetGridLocUnsafe(GridLoc);
		m_bReserved = true;
	}

	/// <summary>
	/// Sends the item into oblivion.
	/// </summary>
	public void DestroyItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		if (m_Item != null)
		{
			m_Item.QueueFree();
			m_Item.GetParent().RemoveChild(m_Item);
			m_Item = null;
		}
		m_bReserved = false;
	}

	/// <summary>
	/// Removes the item from the block and returns it, the caller has full ownership of the item.
	/// </summary>
	public Item ExtractItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		var myItem = m_Item;
		m_Item = null;
		m_bReserved = false;
		return myItem;
	}


	#endregion // .Item Methods


	public virtual void DebugDraw(WorldPanel world)
	{
		var myPos = world.GridToPos(GridLoc);
		var col = Colors.DarkGray;

		// At least one input?
		if (GetWall(Direction.North) == WallType.Input ||
			GetWall(Direction.East)  == WallType.Input ||
			GetWall(Direction.South) == WallType.Input ||
			GetWall(Direction.West)  == WallType.Input)
		{
			col = Colors.IndianRed;
		}

		// At least one output?
		if (GetWall(Direction.North) == WallType.Output ||
			GetWall(Direction.East)  == WallType.Output ||
			GetWall(Direction.South) == WallType.Output ||
			GetWall(Direction.West)  == WallType.Output)
		{
			// Has input as well? yellow, otherwise green.
			col = col == Colors.IndianRed ? Colors.LightYellow : Colors.LightGreen;
		}

		// Is completely solid?
		if (GetWall(Direction.North) == WallType.Solid &&
			GetWall(Direction.East)  == WallType.Solid &&
			GetWall(Direction.South) == WallType.Solid &&
			GetWall(Direction.West)  == WallType.Solid)
		{
			col = Colors.DarkBlue;
		}

		world.DrawRect(new Rect2(myPos, world.CellWidth * Vector2.One), col);
		for (var i = 0; i < 4; ++i)
		{
			var d = (Direction)i;
			var gdir = d.ToGrid();
			if (!IsInput(d) && !IsOutput(d))
			{
				continue;
			}

			var edgePos = myPos + world.CellWidth * (0.5f * Vector2.One + 0.4f * (Vector2)gdir);
			var myCol = IsOutput(d) ? Colors.DarkRed : Colors.DarkGreen;

			var myScale = 0.15f;
			if (IsOutput(d) && 
				!(world.HasTile(GridLoc + gdir) && world.GetTile(GridLoc + gdir).IsInput(d.Invert())))
			{
				myScale *= 0.5f;
				myCol.A = 0.3f;
			}

			var w = world.CellWidth * myScale;
			// world.DrawRect(new Rect2(edgePos - (0.5f*w * Vector2.One), w*Vector2.One), myCol, false);
			world.DrawCircle(edgePos, 0.5f * world.CellWidth * myScale, myCol, false);
		}
	}

	public override string ToString() => $"Tile[{GridLoc}]";
}

