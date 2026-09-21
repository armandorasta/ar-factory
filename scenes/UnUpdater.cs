using System;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public partial class UnUpdater : Unit
{
	public Label UpTypeLabel { get; private set; }

	private UpdateType m_UpType;


	public static int Apply(UpdateType upT, int val) 
	{
		Debug.Assert(Enum.IsDefined(upT));
		return upT switch
		{
			UpdateType.Double => val * 2,
			UpdateType.Negate => -val,
			UpdateType.Increment => val + 1,
			_ => throw new UnreachableException(),
		};
	}


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
		if (HasPendingCmds() && !IsJustAwaitingOutSlideAnim())
		{
			PauseThisTick();
			return;
		}

		var tl = GetTile(Vector2I.Zero);
		PendCmd(CmdUpdate.FromTiles(tl, (val) => Apply(m_UpType, val)));
		PendCmd(CmdSlide.FromTiles(tl, Dir));
	}

	public override string ToString()
	{
		return Utilz.AppendToBaseToString(base.ToString(), $"Updater[{m_UpType}]");
	}
}
