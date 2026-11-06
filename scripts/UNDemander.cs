using System.Collections.Generic;
using Godot;

namespace ArFactory;

/// <summary>
/// Acts as the output for the current level. Needs to be fed a certain sequence of items to pass the level.
/// </summary>
public partial class UNDemander : Unit
{
	public Tile InTile => m_In0!;
	public int[] Sequence => m_Sequence!;

	private int[]? m_Sequence;
	private int m_Index = -1;

	private Tile? m_In0;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, IEnumerable<int> seq)
	{
		BaseInit(world, TickType.OnDemand, 1, gloc, Vector2I.One, dir);
		m_Sequence = [.. seq];
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_In0 = AddInput(Vector2I.Zero, Direction.East, true);
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
		if (IsDoneWithOutputSeq())
		{
			return;
		}

		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		pender.Pend([
			[ new CmdAwait([InTile]) ],
			[ new CmdDemand(InTile, PopNextValue()) ]
		]);
	}

	public bool IsDoneWithOutputSeq() => m_Index >= Sequence.Length - 1;

	public override string ToString()
	{
		var seqStr = string.Join(", ", Sequence);
		return Utilz.AppendToBaseToString(base.ToString(), $"Demander[i: {m_Index}, [{seqStr}]]");
	}

	public override void Reset()
	{
		base.Reset();
		m_Index = -1;
	}

	private int PopNextValue()
	{
		Debug.Assert(!IsDoneWithOutputSeq());
		m_Index += 1;
		return Sequence[m_Index];
	}
}
