using Godot;

namespace ArFactory;

/// <summary>
/// Spawns an item into the specified location.
/// </summary>
public class CmdSpawn : Command
{
	public Vector2I GridLoc { get; private set; }
	public int Value { get; private set; }

	public CmdSpawn(Vector2I gloc, int val) : base(0)
	{
		GridLoc = gloc;
		Value = val;
		AddSubParallelCmds([ new CmdVacate([gloc]) ]);
	}

	public CmdSpawn(Tile tl, int val) : this(tl.GridLoc, val) { }


	protected override void OnTick(Level lv)
	{
		lv.World.SpawnItem(GridLoc, Value);
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Spawn[at {GridLoc} with val {Value}]");
}
