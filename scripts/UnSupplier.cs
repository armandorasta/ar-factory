using System.Collections.Generic;
using Godot;

namespace ArFactory;

/// <summary>
/// Acts as the input for the level. Supplies the player with values to work with.
/// </summary>
public partial class UNSupplier : Unit
{
	public Tile Output => m_Out0!;
	public int[] Sequence => m_Sequence!;

	private Godot.Label? m_Label;

	private int[]? m_Sequence;
	private int m_Index = -1;

	private Tile? m_Out0;

	public override void _Ready()
	{
		base._Ready();
		m_Label = GetNode<Godot.Label>("Sprite2D/Label");
	}

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir, IEnumerable<int> seq)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir);
		m_Sequence = [.. seq];
		UpdateLabel();
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		m_Out0 = AddOutput(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
		if (IsDoneWithInputSeq())
		{
			return;
		}

		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		pender.Pend([
			[ new CmdVacate([Output]) ],
			[ new CmdSpawn(Output, PopNextValue()) ],
			[ new CmdSlide(Output, Dir) ],
		]);

		UpdateLabel();
	}

	public bool IsDoneWithInputSeq() => m_Index >= Sequence.Length - 1;

	public override string ToString()
	{
		var seqStr = string.Join(", ", Sequence);
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
		return Sequence[m_Index];
	}

	private int PeekNextValue() => Sequence[m_Index + 1];

	private void UpdateLabel()
	{
		m_Label!.Text = IsDoneWithInputSeq() ? "DONE" : $"{PeekNextValue()}";
	}
}
