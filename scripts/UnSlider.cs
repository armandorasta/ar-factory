using Godot;

namespace ArFactory;

public partial class UNSlider : Unit
{
	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddOutput(Vector2I.Zero, Direction.East);
		AddInput(Vector2I.Zero, Direction.North);
		AddInput(Vector2I.Zero, Direction.West);
		AddInput(Vector2I.Zero, Direction.South);
	}

	public override void PendNewCommands(WorldPanel.PendProxy pender)
	{
		if (World.HasTile(GLocation + Dir.ToGrid()))
		{
			var tl = GetTileOrNull(Vector2I.Zero);
			// We have to do it manually like this, this way the slider is synced with the clock at all times.
			// I could also just add a third type, but I learned the hard way that simple is always the way to go.
			if (tl.HasItem())
			{
				pender.Pend([ [new CmdSlide(tl, Dir)] ]);
			}
		}
	}

	public override bool CanFaceDir(Direction dir) => true;

	public override string ToString() 
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "Slider");
}
