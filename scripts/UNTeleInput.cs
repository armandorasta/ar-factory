using System;
using Godot;

namespace ArFactory;

public partial class UNTeleInput : Unit
{
	public Tile InTile => GetTile(Vector2I.Zero);

	private WeakReference<UNTeleOutput>? m_MatchingOut;

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir)
	{
		BaseInit(world, TickType.OnDemand, workRate, gloc, Vector2I.One, dir, true);
	}

	public override void _ExitTree()
	{
		UnassignOutput();
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddInput(Vector2I.Zero, Direction.East, true);
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		if (m_MatchingOut is not null && m_MatchingOut.TryGetTarget(out var tgt))
		{
			pender.Pend([
				[ new CmdVacate([tgt.OutTile]) ],
				[ new CmdTeleport(InTile, tgt.OutTile) ]
			]);
		}
	}

	public override bool CanFaceDir(Direction dir) => true;
	public bool IsConnectedToSomething() => m_MatchingOut is not null;
	public bool IsConnectedTo(UNTeleOutput what)
		=> m_MatchingOut is not null && m_MatchingOut.TryGetTarget(out var tgt) && tgt == what;

	public override string ToString()
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "TeleIn");

	public override void _Draw()
	{
		base._Draw();
		if (m_MatchingOut is not null)
		{
			Debug.Assert(m_MatchingOut.TryGetTarget(out var tgt));
			DrawLine(0.5f*World.GetCellSize(), tgt.Position - Position + 0.5f*World.GetCellSize(), Godot.Colors.Cyan);
		}
	}

	public UNTeleOutput? GetConnectedOutput()
	{
		if (m_MatchingOut is not null && m_MatchingOut.TryGetTarget(out var tgt))
		{
			return tgt;
		}
		else return null;
	}
	
	/// <summary>
	/// This automatically calls AssignOutput on ass
	/// </summary>
	public void AssignOutput(UNTeleOutput ass)
	{
		UnassignOutput();
		m_MatchingOut = new(ass);
		ass.AssignInput(this);
		World.QueueRedraw();
	}

	public void UnassignOutput()
	{
		if (m_MatchingOut is not null && m_MatchingOut.TryGetTarget(out var tgt))
		{
			m_MatchingOut = null;
			tgt.UnassignInput();
			World.QueueRedraw();
		}
	}
}
