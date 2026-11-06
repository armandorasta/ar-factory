using System;
using System.Runtime.CompilerServices;
using Godot;

namespace ArFactory;

public partial class UNPlatform : Unit
{
	public Tile FreeTile => GetTileNonNull(Vector2I.Zero);

	public void Setup(WorldPanel world, Vector2I gloc)
	{
		BaseInit(world, TickType.Steady, 1, gloc, Vector2I.One, Direction.East);
	}

	public override void Destroy()
	{
		FreeTile.DestroyItem(bMaybeNull: true, bMaybeMidAnim: true);
		base.Destroy();
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		var tl = GetTileNonNull(Vector2I.Zero);
		tl.ClearWall(Direction.North);
		tl.ClearWall(Direction.East);
		tl.ClearWall(Direction.South);
		tl.ClearWall(Direction.West);
	}

	public override string ToString() => Utilz.AppendToBaseToString(base.ToString(), "Platform[]");
}
