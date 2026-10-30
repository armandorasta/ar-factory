using Godot;

namespace ArFactory;

public partial class UNCloner : Unit
{
	public Tile InTile => m_tlInput!;
	public Tile OutTile0 => m_tlOut0!;
	public Tile OutTile1 => m_tlOut1!;

	private Tile? m_tlInput;
	private Tile? m_tlOut0;
	private Tile? m_tlOut1;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, int workRate)
	{
		BaseInit(world, TickType.OnDemand, workRate, gloc, new Vector2I(1, 2), dir);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_tlInput = AddInput(Vector2I.Zero, Direction.West, true);
		m_tlOut0  = AddOutput(Vector2I.Zero, Direction.East);
		m_tlOut1 = AddOutput(new(0, 1), Direction.East);
	}

	public override void PendNewCommands(Editor.PendProxy pender)
	{
		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		pender.Pend([
			[ new CmdAwait([InTile]) ],
			[ new CmdClone(InTile, [OutTile1]) ],
			[
				new CmdSlide(OutTile0, Dir),
				new CmdSlide(OutTile1, Dir),
			]
		]);
	}
}
