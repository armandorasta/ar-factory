using Godot;
using System;

namespace ArFactory;

/// <summary>
/// Waits for an item to arrive at a specific tile, makes sure if it's sliding into the target tile
/// that the animation is over.
/// </summary>
public partial class CmdAwait(Vector2I gloc) : Command(1)
{
	public Vector2I GridLoc = gloc;
	
	public static CmdAwait FromTiles(Tile tl) => new(tl.GridLoc);

	public override void OnTick(Level lv)
	{
		var targetTile = lv.World.GetTile(GridLoc);
		if (!targetTile.HasItem() || targetTile.Item.IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		Debug.Assert(targetTile.Item.IsAllowedToMove());
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Await[at {GridLoc}]");
}
