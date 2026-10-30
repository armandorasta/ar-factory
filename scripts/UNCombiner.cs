using Godot;

namespace ArFactory;

public partial class UNCombiner : Unit
{
	public BinaryOperation Operation;

	public Tile OutTile => m_tlOutput!;
	public Tile InLeftTile => m_tlLhs!;
	public Tile InRightTile => m_tlRhs!;

	private Tile? m_tlLhs;
	private Tile? m_tlRhs;
	private Tile? m_tlOutput;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, int workRate, BinaryOperation op)
	{
		BaseInit(world, TickType.OnDemand, workRate, gloc, new Vector2I(1, 2), dir);
		Operation = op;
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_tlLhs = AddInput(Vector2I.Zero, Direction.West, true);
		m_tlRhs = AddInput(new Vector2I(0, 1), Direction.West, true);
		m_tlOutput = AddOutput(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands(Editor.PendProxy pender)
	{
		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		pender.Pend([
			[ new CmdAwait([InLeftTile, InRightTile]) ],
			[ new CmdCombine(InLeftTile, InRightTile, OutTile, Operation) ],
			[ new CmdSlide(OutTile, Dir) ],
		]);
	}
}
