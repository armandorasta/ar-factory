using Godot;

namespace ArFactory;

public partial class CmdKill(Vector2I gloc) : Command(0)
{
	public static CmdKill FromTiles(TlHolder tl) => new(tl.GetGridLoc());

	public Vector2I GridLoc = gloc;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(GridLoc) is TlHolder);
		var targetTile = lv.World.GetTile(GridLoc) as TlHolder;
		if (!targetTile.HasItem() || targetTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		targetTile.DestroyItem();
	}
}
