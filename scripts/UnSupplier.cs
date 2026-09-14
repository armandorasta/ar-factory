using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArFactory;

public partial class UnSupplier : Unit
{
	private int[] m_Sequence;
	private int m_Index = -1;

	private TlOutput m_Out0;

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir, IEnumerable<int> seq)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, new(2, 3), dir);
		m_Sequence = seq.ToArray();
	}

	protected override void BuildTiles()
	{
		m_Out0 = AddOutput(new(1, 1), Direction.East);
	}

	public override void PendNewCommands()
	{
		if (IsDoneWithSeq())
		{
			return;
		}
		if (!IsWorkTick())
		{
			return;
		}
		if (HasPendingCmds() && !IsJustAwaitingOutSlideAnim())
		{
			PauseThisTick();
			return;
		}

		PendCmd(CmdSpawn.FromTiles(m_Out0, PopNextValue()));
		PendCmd(CmdSlide.FromTiles(m_Out0, Dir));
	}

	public override void Reset()
	{
		base.Reset();
		m_Index = -1;
	}

	private bool IsDoneWithSeq() => m_Index >= m_Sequence.Length - 1;
	
	private int PopNextValue()
	{
		Debug.Assert(!IsDoneWithSeq());
		m_Index += 1;
		return m_Sequence[m_Index];
	}
}
