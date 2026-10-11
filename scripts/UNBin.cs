using Godot;

namespace ArFactory;

public partial class UNBin : Unit
{
	public void Setup(WorldPanel world, Vector2I gloc)
	{
		BaseInit(world, TickType.OnDemand, 1, gloc, Vector2I.One, Direction.East);
	}

	public override void PendNewCommands(WorldPanel.PendProxy pender)
	{
		Tile tl = GetTile(Vector2I.Zero);
		pender.Pend([
			[ new CmdAwait(tl) ], 
			[ new CmdKill(tl) ],
		]);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		// Blackhole that swallows from all directions, might just call it that in the final game.
		AddInput(Vector2I.Zero, Direction.North, true);
		AddInput(Vector2I.Zero, Direction.East);
		AddInput(Vector2I.Zero, Direction.South);
		AddInput(Vector2I.Zero, Direction.West);
	}

	public override string ToString() => Utilz.AppendToBaseToString(base.ToString(), "Bin[]");
}
