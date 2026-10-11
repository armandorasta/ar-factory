using System;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public partial class UNUpdater : Unit
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
			UpdateType.Double => new(tl, UnaryOperation.Mul, 2),
			UpdateType.Negate => new(tl, UnaryOperation.Mul, -1),
			UpdateType.Increment => new(tl, UnaryOperation.Add, 1),
			_ => throw new UnreachableException(),
		};
	}


	public Label UpTypeLabel => m_UpTypeLabel!;
	public Label? m_UpTypeLabel;

	public UpdateType m_UpType { get; private set; }

	public override void _Ready()
	{
		m_UpTypeLabel = GetNode<Label>("Sprite2D/CenterContainer/HBoxContainer/Label");
	}

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir, UpdateType upT)
	{
		Debug.Assert(Enum.IsDefined(upT));
		BaseInit(world, TickType.OnDemand, workRate, gloc, Vector2I.One, dir);

		m_UpType = upT;
		UpTypeLabel.Text = upT switch
		{
			UpdateType.Double => "2x",
			UpdateType.Negate => "-x",
			UpdateType.Increment => "x+1",
			_ => throw new NotImplementedException(),
		};
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddInput(Vector2I.Zero, Direction.West, true);
		AddOutput(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands(WorldPanel.PendProxy pender)
	{
		var tl = GetTile(Vector2I.Zero);
		var destLoc = tl.GLocation + Dir.ToGrid();
		pender.Pend([
			[ Apply(m_UpType, tl) ],
			World.HasTile(destLoc) ? [ new CmdSlide(tl, Dir) ] : [],
		]);
	}

	public override string ToString()
	{
		return Utilz.AppendToBaseToString(base.ToString(), $"Updater[{m_UpType}]");
	}
}
