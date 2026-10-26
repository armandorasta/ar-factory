using System;
using Godot;

namespace ArFactory;

public partial class UNTeleOutput : Unit
{
	public Tile OutTile => GetTile(Vector2I.Zero);
	public int Delay { get; set; }

	private WeakReference<UNTeleInput>? m_MatchingIn;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, int delay)
	{
		Debug.Assert(delay >= 0);
		BaseInit(world, TickType.OnDemand, 1, gloc, Vector2I.One, dir, true);
		this.Delay = delay;
	}

	public override void _ExitTree()
	{
		DisconnectInput(bBothWays: true);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddOutput(Vector2I.Zero, Direction.East, true);
	}

	public override void PendNewCommands(Editor.PendProxy pender)
	{
		if (HasPendingCmds() && !AllCmdsAreEndingSlidingCmds())
		{
			PauseThisTick();
			return;
		}

		if (m_MatchingIn is not null && m_MatchingIn.TryGetTarget(out var tgt))
		{
			if (World.HasTile(OutTile.GLocation + Dir.ToGrid()))
			{
				pender.Pend([
					Delay > 0 ? [ new CmdSleep(Delay) ] : [],
					[ new CmdSlide(OutTile, Dir) ],
				]);
			}
		}
	}

	public override bool CanFaceDir(Direction dir) => true;
	public bool HasMatchingInput() => m_MatchingIn is not null;

	public override string ToString()
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "TeleIn");

	public override void _Draw()
	{
		base._Draw();
		if (m_MatchingIn is not null)
		{
			DrawRect(new Rect2(World.GetCellSize()*0.4f, 0.2f*World.GetCellSize()), Godot.Colors.Cyan);
		}
	}
	
	public void AssignInput(UNTeleInput ass)
	{
		DisconnectInput();
		m_MatchingIn = new(ass);
		QueueRedraw();
	}

	public void DisconnectInput(bool bBothWays = true)
	{
		if (m_MatchingIn is not null)
		{
			var par = m_MatchingIn;
			m_MatchingIn = null;
			
			if (bBothWays)
			{
				Debug.Assert(par.TryGetTarget(out var tgt));
				tgt.DisconnectOutput(bBothWays: false);
			}
			
			QueueRedraw();
		}
	}
}
