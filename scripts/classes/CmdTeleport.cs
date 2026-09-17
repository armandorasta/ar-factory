using Godot;

namespace ArFactory;

public partial class CmdTeleport(Vector2I gfrom, Vector2I gto) : Command(0)
{
	public static CmdTeleport FromTiles(Tile tlFrom, Tile tlTo) 
		=> new(tlFrom.GridLoc, tlTo.GridLoc);

	public Vector2I SrcGridLoc = gfrom;
	public Vector2I DestGridLoc = gto;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(SrcGridLoc) is Tile);
		Debug.Assert(lv.World.GetTile(DestGridLoc) is Tile);
		var srcTile = lv.World.GetTile(SrcGridLoc);
		if (!srcTile.HasItem() || srcTile.Item.IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		lv.World.TeleportItem(SrcGridLoc, DestGridLoc);
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Teleport[from {SrcGridLoc} to {DestGridLoc}]");
}
