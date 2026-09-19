using Godot;

namespace ArFactory;

public partial class CmdSpawn(Vector2I gloc, int val) : Command(0)
{
	public static CmdSpawn FromTiles(Tile tl, int val) => new(tl.GridLoc, val);

	public Vector2I GridLoc = gloc;
	public int Value = val;

	public override void OnTick(Level lv)
	{
		if (lv.World.GetTile(GridLoc).IsReserved())
		{
			PauseThisTick();
			return;
		}

		lv.World.SpawnItem(GridLoc, Value);
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Spawn[at {GridLoc} with val {Value}]");
}
