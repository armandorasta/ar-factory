using System;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public class Tile(Vector2I gloc)
{
	[Flags]
	private enum WallType : Int32
	{
		Nothing = 0x00,
		Solid   = 0x01,
		Input   = 0x02,
		Output  = 0x04,
	}

	private const int WallFieldWidth = sizeof(WallType) * 8 / 4;
	private const int WallFieldMask = (1 << WallFieldWidth) - 1;


	// Publics
	public Vector2I GridLoc => m_GridLoc;
	/// <summary>
	/// This accessor crashes the program in debug mode if the item is null, use 
	/// <see cref="GetItemMaybeNull"/> if that behaviour is not desired.
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
	// This flag is only set from outside, function IsReserved checks this and the item directly.
	private bool m_bReserved = false;


	#region .Factory Methods

	public static Tile CreateSolid(Vector2I gloc)
	{
		var res = new Tile(gloc);
		res.SetWall(Direction.North, WallType.Solid);
		res.SetWall(Direction.East, WallType.Solid);
		res.SetWall(Direction.South, WallType.Solid);
		res.SetWall(Direction.West, WallType.Solid);
		return res;
	}

	#endregion // .Factory Methods
	#region .Public Methods

	/// <summary>
	/// Use this if the tile is floating, otherwise use <see cref="WorldPanel.MoveTile"/> instead.
	/// </summary>
	public void SetGridLocUnsafe(Vector2I gloc) { m_GridLoc = gloc; }

	/// <summary>
	/// Returns true if the item can slide into through the edge facing <paramref name="wallDir"/>.
	/// This function does not check for items in the tile.
	/// </summary>
	/// <param name="wallDir">Direction of the tile edge</param>
	public bool CanItemEnter(Direction wallDir)
	{
		return GetWall(wallDir) switch
		{
			WallType.Nothing => true,
			WallType.Input => true,
			_ => false,
		};
	}

	public bool CanItemExit(Direction wallDir)
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
	public bool IsReserved() => m_bReserved || m_Item is not null;

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


	#endregion // .Public Methods
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

	public Tile MakeOutput(Direction dir, bool bOverride = false)
	{
		Debug.Assert(bOverride || GetWall(dir) == WallType.Nothing);
		return SetWall(dir, WallType.Output);
	}

	public Tile MakeInput(Direction dir, bool bOverride = false)
	{
		Debug.Assert(bOverride || GetWall(dir) == WallType.Nothing);
		return SetWall(dir, WallType.Input);
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
	#region .Item Methods

	public bool HasItem() => m_Item is not null;

	/// <summary>
	/// Does the same thing as the property in release, mandatory in debug if the item might be null.
	/// </summary>
	public Item GetItemMaybeNull() => m_Item;

	/// <summary>
	/// Only works with extracted tiles in debug, use <see cref="WorldPanel.SpawnItem"/> or
	/// <see cref="WorldPanel.InstallItem"/>.<br/>
	/// </summary>
	public void SetItemUnsafe(Item newItem, bool bOverride = false)
	{
		Debug.AssertEq(newItem.GetParent(), null, "Use 'WorldPanel.SpawnItem' or 'WorldPanel.InstallItem'");
		Debug.Assert(bOverride || !HasItem());
		DestroyItem(true);
		m_Item = newItem;
	}

	/// <summary>
	/// Sends the item into oblivion.
	/// </summary>
	public void DestroyItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		Debug.Assert(bMaybeNull || m_bReserved);
		if (m_Item is not null)
		{
			m_Item.GetParent().RemoveChild(m_Item);
			m_Item.QueueFree();
			m_Item = null;
		}
	}

	/// <summary>
	/// Removes the item from the block and returns it, the caller has full ownership of the item.
	/// </summary>
	public Item ExtractItem(bool bMaybeNull = false)
	{
		Debug.Assert(bMaybeNull || HasItem());
		var extractedIt = m_Item;
		m_Item = null;
		m_bReserved = false;
		// Parents might be null if the user carelessly uses SetItemUnsafe directly.
		extractedIt.GetParent().RemoveChild(extractedIt);
		return extractedIt;
	}


	#endregion // .Item Methods
	#region .Private Methods

	public virtual void DebugDraw(WorldPanel world)
	{
		var myPos = world.GridToPos(GridLoc);
		var col = Colors.DarkGray;

		// I know I can make the following checks faster with some bit arithmatic, but I just don't
		// feel like it.

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
			col = (col == Colors.IndianRed) ? Colors.LightYellow : Colors.LightGreen;
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

			// var w = world.CellWidth * myScale;
			// world.DrawRect(new Rect2(edgePos - (0.5f*w * Vector2.One), w*Vector2.One), myCol, false);
			world.DrawCircle(edgePos, 0.5f * world.CellWidth * myScale, myCol, false);
		}
	}

	public override string ToString() => $"Tile[{GridLoc}]";
	#endregion // .Private Methods
}

