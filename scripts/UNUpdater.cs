using System;
using System.Diagnostics;
using Godot;

namespace ArFactory;

public partial class UNUpdater : Unit
{
	public static CmdUpdate Apply(UpdateType upT, Tile tl) 
	{
		Debug.Assert(Enum.IsDefined(upT));
		return upT switch
		{
			UpdateType.Double => new(tl, UnaryOperation.Mul, 2),
			UpdateType.Negate => new(tl, UnaryOperation.Mul, -1),
			UpdateType.Increment => new(tl, UnaryOperation.Add, 1),
			UpdateType.Decrement => new(tl, UnaryOperation.Add, -1),
			_ => throw new UnreachableException(),
		};
	}

	public UpdateType UpType { get; private set; }

	private Label? m_UpTypeLabel;
	
	public Tile Output => GetTileNonNull(Vector2I.Zero);
	public Tile Input => GetTileNonNull(Vector2I.Zero);

	public override void _Ready()
	{
		base._Ready();
		m_UpTypeLabel = GetNode<Label>("Sprite2D/Label");
	}

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir, UpdateType upT)
	{
		Debug.Assert(Enum.IsDefined(upT));
		BaseInit(world, TickType.OnDemand, workRate, gloc, Vector2I.One, dir);

		SetUpdateType(upT);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddInput(Vector2I.Zero, Direction.West, true);
		AddOutput(Vector2I.Zero, Direction.East);
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
		if (HasPendingCmds())
		{
			PauseThisTick();
			return;
		}

		var tl = Output;
		pender.Pend([
			[ Apply(UpType, tl) ],
			[ new CmdSlide(tl, Dir) ],
		]);
	}

	public override string ToString()
	{
		return Utilz.AppendToBaseToString(base.ToString(), $"Updater[{UpType}]");
	}

	public void SetUpdateType(UpdateType newUpT)
	{
		Debug.Assert(Enum.IsDefined(newUpT));
		UpType = newUpT;
		m_UpTypeLabel!.Text = UpType switch
		{
			UpdateType.Double    => "2x",
			UpdateType.Negate    => "-x",
			UpdateType.Increment => "x+1",
			UpdateType.Decrement => "x-1",
			_ => throw new NotImplementedException(),
		};
	}

	public enum UpdateType
	{
		Double,
		Negate,
		Increment,
		Decrement,
	}
}
