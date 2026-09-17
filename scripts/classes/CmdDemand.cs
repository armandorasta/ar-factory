using Godot;

namespace ArFactory;

public partial class CmdDemand(Vector2I gloc, int reqVal) : Command(1)
{
	public static CmdDemand FromTiles(Tile tl, int reqVal) => new(tl.GridLoc, reqVal);

	public Vector2I GridLoc = gloc;
	public int Value = reqVal;

	public override void OnTick(Level lv)
	{
		Debug.AssertIs(lv.World.GetTile(GridLoc), typeof(Tile));
		var targetTile = lv.World.GetTile(GridLoc);
		if (!targetTile.HasItem() || targetTile.Item.IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		var itVal = targetTile.Item.GetValue();
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
