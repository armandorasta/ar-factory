using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Teleports an item from a location to another.
/// </summary>
public class CmdTeleport(Vector2I gfrom, Vector2I gto) : Command(1)
{
	public Vector2I SourceGL { get; private set; } = gfrom;
	public Vector2I DestinationGL { get; private set; } = gto;

	public CmdTeleport(Tile tlFrom, Tile tlTo) : this(tlFrom.GLocation, tlTo.GLocation) { }

	protected override void OnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		Debug.Assert(HasTileWithItemNotMidAnim(lv.World, SourceGL));
		Debug.Assert(SourceGL == DestinationGL || HasTileUnreserved(lv.World, DestinationGL));
		if (lv.World.TeleportItem(SourceGL, DestinationGL) is null)
		{
			throw new InvalidProgramException("CmdTeleport failed");
		}
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Teleport[from {SourceGL} to {DestinationGL}]");
}
