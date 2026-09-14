using Godot;

namespace ArFactory;

public partial class CmdSpawn(Vector2I gloc, int val) : Command(0)
{
	public static CmdSpawn FromTiles(TlHolder tl, int val) => new(tl.GetGridLoc(), val);

	public Vector2I GridLoc = gloc;
	public int Value = val;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(GridLoc) is TlHolder);
		var targetTile = lv.World.GetTile(GridLoc) as TlHolder;
		if (targetTile.IsReserved())
		{
			PauseThisTick();
			return;
		}

		lv.World.SpawnItem(GridLoc, Value);
	}
}
