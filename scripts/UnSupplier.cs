using System.Collections.Generic;
using Godot;

namespace ArFactory;

public partial class UNSupplier : Unit
{
	private int[]? m_Sequence;
	private int m_Index = -1;

	private Tile? m_Out0;

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

		var startIndex = HasPendingCmds() ? 1 : 0;
		Vector2I destLoc = m_Out0!.GridLoc + Dir.ToGrid();
		pender.Pend([
			[ new CmdVacate([m_Out0!]) ],
			[ new CmdSpawn(m_Out0!, PopNextValue()) ],
			World.HasTile(destLoc) ? [ new CmdSlide(m_Out0, Dir) ] : [],
			// [ new CmdIf(m_Out0!.GridLoc + Dir.ToGrid(), CmdIf.CheckType.HasTile, -1, [
			// 	[ new CmdSlide(m_Out0, Dir) ]
			// ])]
		], startIndex);
	}

	public bool IsDoneWithInputSeq() => m_Index >= m_Sequence!.Length - 1;

	public override string ToString()
	{
		var seqStr = string.Join(", ", m_Sequence!);
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
		return m_Sequence![m_Index];
	}
}
