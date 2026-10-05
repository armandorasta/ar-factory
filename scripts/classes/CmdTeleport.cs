using Godot;

namespace ArFactory;

/// <summary>
/// Teleports an item from a location to another.
/// </summary>
public class CmdTeleport(Vector2I gfrom, Vector2I gto) : Command(1)
{
	public Vector2I SourceGL { get; private set; } = gfrom;
	public Vector2I DestinationGL { get; private set; } = gto;

	public CmdTeleport(Tile tlFrom, Tile tlTo) : this(tlFrom.GridLoc, tlTo.GridLoc) { }

	protected override void OnTick(Level lv)
	{
		Debug.Assert(HasTileWithItem(lv.World, SourceGL));
		Debug.Assert(HasTileUnreserved(lv.World, DestinationGL));
		lv.World.TeleportItemUnchecked(SourceGL, DestinationGL);
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Teleport[from {SourceGL} to {DestinationGL}]");
}
