using Godot;

namespace ArFactory;

/// <summary>
/// Same as await, but expects a specific value, if it doesn't get it, will fail the program.
/// </summary>
public class CmdDemand : Command
{
	public Vector2I GLocation { get; private set; }
	public int Value { get; private set; }

	public CmdDemand(Vector2I gloc, int reqVal) : base(1)
	{
		Debug.Assert(Item.IsValueWithinRange(reqVal));
		GLocation = gloc;
		Value = reqVal;
	}

	public CmdDemand(Tile tl, int reqVal) : this(tl.GLocation, reqVal) { }


	protected override void OnTick(Simulator.OnTickMediator ticker)
	{
		Debug.Assert(HasTileWithItemNotMidAnim(ticker.World, GLocation));

		var targetTile = ticker.World.GetTileNonNull(GLocation);
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
			ticker.CrashSimulation(Value, itVal, GLocation);
		}
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Demand[at {GLocation} demand val {Value}]");
}
