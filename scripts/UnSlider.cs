using Godot;

namespace ArFactory;

public partial class UnSlider : Unit
{
	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir);
	}

	protected override void BuildTiles()
	{
		AddOutput(Vector2I.Zero, Direction.East);
		AddInput(Vector2I.Zero, Direction.North);
		AddInput(Vector2I.Zero, Direction.West);
		AddInput(Vector2I.Zero, Direction.South);
	}

	public override void PendNewCommands()
	{
		if (!IsWorkTick())
		{
			return;
		}
		if (HasPendingCmds() && !m_Runner.IsJustAwaitingOutSlideAnim())
		{
			PauseThisTick();
			return;
		}

		PendCmd(new CmdSlide(GetTile(Vector2I.Zero), Dir));
	}

	public override bool CanFaceDir(Direction dir) => true;

	public override string ToString() 
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "Slider");
}
