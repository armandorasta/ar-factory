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
		AddSlider(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands()
	{
		if (!IsWorkTick())
		{
			return;
		}
		if (HasPendingCmds() && !IsJustAwaitingOutSlideAnim())
		{
			PauseThisTick();
			return;
		}

		PendCmd(CmdSlide.FromTiles(GetTile(Vector2I.Zero) as TlSlider, Dir));
	}

	public override bool CanFaceDir(Direction dir) => true;

	public override string ToString() 
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "Slider");
}
