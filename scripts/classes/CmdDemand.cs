using Godot;

namespace ArFactory;

public partial class CmdDemand(Vector2I gloc, int reqVal) : Command(1)
{
	public static CmdDemand FromTiles(TlHolder tl, int reqVal) => new(tl.GetGridLoc(), reqVal);

	public Vector2I GridLoc = gloc;
	public int Value = reqVal;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(GridLoc) is TlHolder);
		var targetTile = lv.World.GetTile(GridLoc) as TlHolder;
		if (!targetTile.HasItem() || targetTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		var itVal = targetTile.GetItem().GetValue();
		if (itVal == Value)
		{
			GD.Print($"Expected {Value} and got it!");
			targetTile.DestroyItem();
		}
		else
		{
			GD.Print($"Expected {Value}, but got {itVal} instead...");
			targetTile.DestroyItem();
			targetTile.SetReserved(true); // Forever until I implement halt.
		}
	}
}
