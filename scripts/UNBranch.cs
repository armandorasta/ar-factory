using Godot;
using System;

namespace ArFactory;

public partial class UNBranch : Unit
{
	public CmdIf.CheckType Check { get; private set; }
	public int CmpValue { get; private set; }

	private Tile? m_tlInput;
	private Tile? m_tlTrue;
	private Tile? m_tlFalse;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, CmdIf.CheckType check, int value)
	{
		BaseInit(world, TickType.OnDemand, 1, gloc, new Vector2I(1, 2), dir);
		Check = check;
		CmpValue = value;
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_tlInput = AddInput(Vector2I.Zero, Direction.West, true);
		m_tlTrue  = AddOutput(Vector2I.Zero, Direction.East);
		m_tlFalse = AddOutput(new(0, 1), Direction.East);
	}

	public override void PendNewCommands(WorldPanel.PendProxy pender)
	{
		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		var startIndex = HasPendingCmds() ? 1 : 0; // Overlapping with a slide from last time?
		if (CmdIf.Apply(Check, m_tlInput, CmpValue))
		{
			pender.Pend([
				[ new CmdAwait([m_tlInput!]) ],
				[ new CmdSlide(m_tlTrue!, Dir) ],
			], startIndex);
		}
		else
		{
			pender.Pend([
				[ new CmdAwait([m_tlInput!]) ],
				[ new CmdVacate([m_tlFalse!]) ],
				[ new CmdTeleport(m_tlInput!, m_tlFalse!) ],
				[ new CmdSlide(m_tlFalse!, Dir) ],				
			], startIndex);
		}
	}
}
