using Godot;

namespace ArFactory;

public partial class UNBranch : Unit
{
	public CmdIf.CheckType Check;
	public int CmpValue;

	public Tile InTile => m_tlInput!;
	public Tile OutTrueTile => m_tlOutTrue!;
	public Tile OutFalseTile => m_tlOutFalse!;

	private Tile? m_tlInput;
	private Tile? m_tlOutTrue;
	private Tile? m_tlOutFalse;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, int workRate, CmdIf.CheckType check, int value)
	{
		BaseInit(world, TickType.OnDemand, workRate, gloc, new Vector2I(1, 2), dir);
		Check = check;
		CmpValue = value;
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_tlInput = AddInput(Vector2I.Zero, Direction.West, true);
		m_tlOutTrue  = AddOutput(Vector2I.Zero, Direction.East);
		m_tlOutFalse = AddOutput(new(0, 1), Direction.East);
	}

	public override void PendNewCommands(Editor.PendProxy pender)
	{
		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		if (CmdIf.Apply(Check, InTile, CmpValue))
		{
			pender.Pend([
				[ new CmdAwait([InTile]) ],
				[ new CmdSlide(OutTrueTile, Dir) ],
			]);
		}
		else
		{
			pender.Pend([
				[ new CmdAwait([InTile]) ],
				[ new CmdVacate([OutFalseTile]) ],
				[ new CmdTeleport(InTile, OutFalseTile) ],
				[ new CmdSlide(OutFalseTile, Dir) ],		
			]);
		}
	}
}
