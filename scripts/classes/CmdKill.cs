using Godot;

namespace ArFactory;

/// <summary>
/// Deletes an item in the specified location.
/// </summary>
public class CmdKill : Command
{
	public Vector2I GridLoc { get; private set; }

	public CmdKill(Vector2I gloc) : base(0)
	{
		GridLoc = gloc;
		AddSubParallelCmds([new CmdAwait([gloc])]);
	}

	public CmdKill (Tile tl) : this(tl.GridLoc) { }


	protected override void OnTick(Level lv)
	{
		lv.World.GetTile(GridLoc).DestroyItem();
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Kill[at {GridLoc}]");
}
