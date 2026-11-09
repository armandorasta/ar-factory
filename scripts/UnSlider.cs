using System;
using Godot;

namespace ArFactory;

public partial class UNSlider : Unit
{
	// public event Action? ItemArrived;

	public Tile OnlyTile => GetTileNonNull(Vector2I.Zero);

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir, true);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddOutput(Vector2I.Zero, Direction.East);
		AddInput(Vector2I.Zero, Direction.North);
		AddInput(Vector2I.Zero, Direction.West);
		AddInput(Vector2I.Zero, Direction.South);
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
		if (HasPendingCmds() && !AllCmdsAreEndingSlidingCmds())
		{
			PauseThisTick();
			return;
		}

		var otl = OnlyTile;
		// We have to do it manually like this, this way the slider is synced with the clock at all times.
		// I could also just add a third type, but I learned the hard way that simple is always the way to go.
		if (otl.HasItem())
		{
			pender.Pend([ [new CmdSlide(otl, Dir)] ]);
			InvokeEventTriggerHappened();
		}
	}

	public override bool CanFaceDir(Direction dir) => true;

	public override string ToString() => Utilz.ReplaceBaseNameInToString(base.ToString(), "Slider");
}