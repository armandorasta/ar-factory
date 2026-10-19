using System.Collections.Generic;
using Godot;

namespace ArFactory;

public partial class UNDemander : Unit
{
	public Tile Input => m_In0!;

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

	public override void PendNewCommands(Editor.PendProxy pender)
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
			[ new CmdAwait([Input]) ],
			[ new CmdDemand(Input, PopNextValue()) ]
		]);
	}

	public bool IsDoneWithOutputSeq() => m_Index >= m_Sequence!.Length - 1;

	public override string ToString()
	{
		var seqStr = string.Join(", ", m_Sequence!);
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
		return m_Sequence![m_Index];
	}
}
