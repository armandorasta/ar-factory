using Godot;

namespace ArFactory;

/// <summary>
/// Teleports an item from a location to another.
/// </summary>
public class CmdTeleport : Command
{
	public Vector2I SrcGridLoc { get; private set; }
	public Vector2I DestGridLoc { get; private set; }

	public CmdTeleport(Vector2I gfrom, Vector2I gto) : base(0)
	{
		SrcGridLoc = gfrom;
		DestGridLoc = gto;
		AddSubParallelCmds([ new CmdAwaitVacate([gfrom], [gto]) ]);
	}

	public CmdTeleport(Tile tlFrom, Tile tlTo) : this(tlFrom.GridLoc, tlTo.GridLoc) { }

	protected override void OnTick(Level lv)
	{
		lv.World.TeleportItem(SrcGridLoc, DestGridLoc);
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Teleport[from {SrcGridLoc} to {DestGridLoc}]");
}
