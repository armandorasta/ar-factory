using Godot;

namespace ArFactory;

public abstract partial class Tile(Vector2I gloc) : RefCounted
{
	const Direction DefaultDir = Direction.East;

	[Signal] 
	public delegate void NeedsRedrawEventHandler();

	
	protected Vector2I m_GridLoc = gloc;

	/// <summary>
	/// <paramref name="movementDir"/> is the direction of the movement, not the direction of the 
	/// edge of the tile. If an item is sliding in from the west moving east, <paramref name="movementDir"/> 
	/// will be <see cref="Direction.East"/>,  not <see cref="Direction.West"/>.
	/// </summary>
	/// <param name="movementDir">Direction of the movement, not the direction of the tile edge</param>
	public abstract bool CanItemEnterInDir(Item item, Direction movementDir);
	public abstract bool HasInputInDir(Direction dir);
	public abstract bool HasOutputInDir(Direction dir);
	public abstract Color TypeToCol();


	// Shorthands:
	// Normally these would need you to down-cast, so instead they all return false or do nothing
	// saving you a line or two.

	/// <summary>
	/// Items are not allowed to slide into reserved tiles. <br/>
	/// This is a shorthand for <c>tile is TlHolder holder &amp;&amp; holder.IsReserved()</c><br/>
	/// This always returns true for tiles that are not <see cref="TlHolder"/> by default.
	/// </summary>
	public virtual bool IsReserved() => true;

	/// <summary>
	/// Items are not allowed to slide into reserved tiles. <br/>
	/// This does nothing for tiles that are not <see cref="TlHolder"/> by default.
	/// </summary>
	public virtual void SetReserved(bool toWhat) {}

	/// <summary>
	/// For <see cref="TlIO"/> this is an alias for <see cref="TlIO.GetOutputDir"/>.
	/// </summary>
	/// <returns></returns>
	public virtual Direction GetDir() => DefaultDir;
	
	/// <summary>
	/// This forces a redraw no matter what.
	/// For <see cref="TlIO"/> this is an alias for <see cref="TlIO.SetOutputDir"/>.
	/// </summary>
	public virtual void SetDir(Direction newDir) => EmitSignal(SignalName.NeedsRedraw);

	/// <summary>
	/// Rotates the unit 90 degrees clockwise. All other rotation functions are implemnted in terms
	/// of this one by default.
	/// </summary>
	public virtual void Rotate90() => SetDir(GetDir().Rotate90());

	/// <summary>
	/// By defaut implemented in terms of <see cref="Rotate90"/> and <see cref="Rotate180"/>.
	/// </summary>
	public virtual void Rotate270() { Rotate180(); Rotate90(); }

	/// <summary>
	/// By defaut implemented in terms of <see cref="Rotate90"/>.
	/// </summary>
	public virtual void Rotate180() { Rotate90(); Rotate90(); }

	/// <summary>
	/// Shorthand for: <c>tile is CS.TlHolder holder &amp;&amp; holder.HasItem()</c>
	/// </summary>
	public virtual bool HasItem() => false;

	/// <summary>
	/// Always returns null for tiles that are not <see cref="TlHolder"/>.
	/// </summary>
	public virtual Item GetItem(bool bMaybeNull = false) => null;

	// No SetItem of course, adding that would have caused so many disasters, god damn...


	// Normal Stuff
	// These are never or rarely overriden by sub classes

	public Vector2I GetGridLoc() => m_GridLoc;

	/// <summary>
	/// Use this if the tile is floating, otherwise use <see cref="WorldPanel.MoveTile"/> instead.
	/// </summary>
	public void SetGridLocUnchecked(Vector2I gloc) => m_GridLoc = gloc; 

	public virtual void DebugDraw(WorldPanel world)
	{
		var myPos = world.GridToPos(m_GridLoc);
		world.DrawRect(new Rect2(myPos, world.CellWidth * Vector2.One), TypeToCol());
		for (var i = 0; i < 4; ++i)
		{
			var d = (Direction)i;
			var gdir = d.ToGrid();
			if (!HasOutputInDir(d) && !HasInputInDir(d))
			{
				continue;
			}

			var edgePos = myPos + world.CellWidth * (0.5f * Vector2.One + 0.4f * (Vector2)gdir);
			var myCol = HasInputInDir(d) ? Colors.DarkRed : Colors.DarkGreen;

			var myScale = 0.15f;
			if (HasInputInDir(d) && !(world.HasTile(m_GridLoc + gdir) 
				&& world.GetTile(m_GridLoc + gdir).HasOutputInDir(d.Invert())))
			{
				myScale *= 0.5f;
				myCol.A = 0.3f;
			}

			var w = world.CellWidth * myScale;
			// world.DrawRect(new Rect2(edgePos - (0.5f*w * Vector2.One), w*Vector2.One), myCol, false);
			world.DrawCircle(edgePos, 0.5f * world.CellWidth * myScale, myCol, false);
		}
	}

	public override string ToString() => $"Tile[{m_GridLoc}]";
}

