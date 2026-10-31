using Godot;

namespace ArFactory;

/// <summary>
/// Combines items in multiple locations into one.
/// </summary>
/// <remarks>
/// <paramref name="glDest"/> can overlap with one of the sources as the old items will be destroyed before the new 
/// item is spawned.
/// </remarks>
public class CmdCombine(Vector2I glLeft, Vector2I glRight, Vector2I glDest, BinaryOperation op) : Command(1)
{
	public readonly Vector2I LeftGL = glLeft;
	public readonly Vector2I RightGL = glRight;
	public readonly Vector2I ResultGL = glDest;
	public readonly BinaryOperation Op = op;

	public CmdCombine(Tile tlLeft, Tile tlRight, Tile tlDest, BinaryOperation op) 
		: this(tlLeft.GLocation, tlRight.GLocation, tlDest.GLocation, op)
	{ }


	protected override void OnTick(Simulator.OnTickMediator ticker)
	{
		Debug.Assert(HasTileWithItemNotMidAnim(ticker.World, LeftGL));
		Debug.Assert(HasTileWithItemNotMidAnim(ticker.World, RightGL));
		Debug.Assert(ResultGL == LeftGL || ResultGL == RightGL || HasTileUnreserved(ticker.World, ResultGL));

		Tile tlLeft = ticker.World.GetTileNonNull(LeftGL);
		Tile tlRight = ticker.World.GetTileNonNull(RightGL);
		var finalVal = Op.Apply(tlLeft.Item.Value, tlRight.Item.Value);
		tlLeft.DestroyItem();
		tlRight.DestroyItem();
		ticker.World.SpawnItem(ResultGL, finalVal);
	}

	public override string ToString()
	{
		return Utilz.AppendToBaseToString(base.ToString(), 
			$"Combine[{LeftGL} {Op.ToString()} {RightGL} -> {ResultGL}]");	
	} 
}