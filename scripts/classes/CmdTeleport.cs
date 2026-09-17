using Godot;

namespace ArFactory;

public partial class CmdTeleport(Vector2I gfrom, Vector2I gto) : Command(0)
{
	public static CmdTeleport FromTiles(TlHolder tlFrom, TlHolder tlTo) 
		=> new(tlFrom.GetGridLoc(), tlTo.GetGridLoc());

	public Vector2I SrcGridLoc = gfrom;
	public Vector2I DestGridLoc = gto;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(SrcGridLoc) is TlHolder);
		Debug.Assert(lv.World.GetTile(DestGridLoc) is TlHolder);
		var srcTile = lv.World.GetTile<TlHolder>(SrcGridLoc);
		if (!srcTile.HasItem() || srcTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		lv.World.TeleportItem(SrcGridLoc, DestGridLoc);
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Teleport[from {SrcGridLoc} to {DestGridLoc}]");
}
