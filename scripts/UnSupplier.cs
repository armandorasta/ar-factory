using System.Collections.Generic;
using Godot;

namespace ArFactory;

public partial class UNSupplier : Unit
{
	private int[] m_Sequence;
	private int m_Index = -1;

	private Tile m_Out0;

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir, IEnumerable<int> seq)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir);
		m_Sequence = [.. seq];
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_Out0 = AddOutput(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands(WorldPanel.PendProxy pender)
	{
		if (IsDoneWithInputSeq())
		{
			return;
		}

		pender.PendParallel([ new CmdVacate([m_Out0]) ], 0);
		pender.PendParallel([ new CmdSpawn(m_Out0, PopNextValue()) ], 1);
		if (World.HasTile(m_Out0.GridLoc + Dir.ToGrid()))
		{
			pender.PendParallel([ new CmdSlide(m_Out0, Dir) ], 1);
		}
	}

	public bool IsDoneWithInputSeq() => m_Index >= m_Sequence.Length - 1;

	public override string ToString()
	{
		var seqStr = string.Join(", ", m_Sequence);
		return Utilz.AppendToBaseToString(base.ToString(), $"Supplier[i: {m_Index}, [{seqStr}]]");
	}

	public override void Reset()
	{
		base.Reset();
		m_Index = -1;
	}
	
	private int PopNextValue()
	{
		Debug.Assert(!IsDoneWithInputSeq());
		m_Index += 1;
		return m_Sequence[m_Index];
	}
}
