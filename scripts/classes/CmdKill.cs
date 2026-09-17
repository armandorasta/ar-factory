using Godot;

namespace ArFactory;

public partial class CmdKill(Vector2I gloc) : Command(0)
{
	public static CmdKill FromTiles(TlHolder tl) => new(tl.GetGridLoc());

	public Vector2I GridLoc = gloc;

	public override void OnTick(Level lv)
	{
		Debug.AssertIs(lv.World.GetTile(GridLoc), typeof(TlHolder));
		var targetTile = lv.World.GetTile<TlHolder>(GridLoc);
		if (!targetTile.HasItem() || targetTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		targetTile.DestroyItem();
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Kill[at {GridLoc}]");
}
