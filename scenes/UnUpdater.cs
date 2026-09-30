using System;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public partial class UnUpdater : Unit
{
	public enum UpdateType
	{
		Double,
		Negate,
		Increment,
	}

	public static CmdUpdate Apply(UpdateType upT, Tile tl) 
	{
		Debug.Assert(Enum.IsDefined(upT));
		return upT switch
		{
			UpdateType.Double => new(tl, CmdUpdate.UpdateType.Mul, 2),
			UpdateType.Negate => new(tl, CmdUpdate.UpdateType.Mul, -1),
			UpdateType.Increment => new(tl, CmdUpdate.UpdateType.Add, 1),
			_ => throw new UnreachableException(),
		};
	}


	public Label UpTypeLabel { get; private set; }

	public UpdateType m_UpType { get; private set; }


	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir,
		UpdateType upT)
	{
		BaseInit(world, TickType.OnDemand, workRate, gloc, Vector2I.One, dir);
		UpTypeLabel = GetNode<Label>("Sprite2D/CenterContainer/HBoxContainer/Label");
		
		Debug.Assert(Enum.IsDefined(upT));
		m_UpType = upT;
		UpTypeLabel.Text = upT switch
		{
			UpdateType.Double => "2x",
			UpdateType.Negate => "-x",
			UpdateType.Increment => "x+1",
			_ => throw new UnreachableException(),
		};
	}

	protected override void BuildTiles()
	{
		AddInput(Vector2I.Zero, Direction.West, true);
		AddOutput(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands()
	{
		if (!IsWorkTick())
		{
			return;
		}
		if (HasPendingCmds() && !m_Runner.IsJustAwaitingOutSlideAnim())
		{
			PauseThisTick();
			return;
		}

		var tl = GetTile(Vector2I.Zero);
		PendCmd(Apply(m_UpType, tl));
		PendCmd(new CmdSlide(tl, Dir));
	}

	public override string ToString()
	{
		return Utilz.AppendToBaseToString(base.ToString(), $"Updater[{m_UpType}]");
	}
}
