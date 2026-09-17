using Godot;

namespace ArFactory;

public partial class CmdKill(Vector2I gloc) : Command(0)
{
	public static CmdKill FromTiles(Tile tl) => new(tl.GridLoc);

	public Vector2I GridLoc = gloc;

	public override void OnTick(Level lv)
	{
		Debug.AssertIs(lv.World.GetTile(GridLoc), typeof(Tile));
		var targetTile = lv.World.GetTile(GridLoc);
		if (!targetTile.HasItem() || targetTile.Item.IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		targetTile.DestroyItem();
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Kill[at {GridLoc}]");
}
