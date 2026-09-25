using Godot;

namespace ArFactory;

/// <summary>
/// Same as await, but expects a specific value, if it doesn't get it, will fail the program.
/// </summary>
public class CmdDemand : Command
{
	public Vector2I GridLoc { get; private set; }
	public int Value { get; private set; }

	public CmdDemand(Vector2I gloc, int reqVal) : base(1)
	{
		GridLoc = gloc;
		Value = reqVal;
		AddSubParallelCmds([ new CmdAwait([gloc]) ]);
	}

	public CmdDemand(Tile tl, int reqVal) : this(tl.GridLoc, reqVal) { }


	protected override void OnTick(Level lv)
	{
		var targetTile = lv.World.GetTile(GridLoc);
		var itVal = targetTile.Item.Value;
		if (itVal == Value)
		{
			GD.Print($"Expected {Value} and got it!");
			targetTile.DestroyItem();
		}
		else
		{
			GD.Print($"Expected {Value}, but got {itVal} instead...");
			targetTile.DestroyItem();
			targetTile.Reserve(); // Forever until I implement halt.
		}
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Demand[at {GridLoc} demand val {Value}]");
}
