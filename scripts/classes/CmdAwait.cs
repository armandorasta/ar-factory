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
	
	public static CmdAwait FromTiles(TlHolder tl) => new(tl.GetGridLoc());

	public override void OnTick(Level lv)
	{
		Debug.AssertIs(lv.World.GetTile(GridLoc), typeof(TlHolder));
		var targetTile = lv.World.GetTile<TlHolder>(GridLoc);
		if (!targetTile.HasItem() || targetTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		Debug.Assert(targetTile.GetItem().IsAllowedToMove());
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Await[at {GridLoc}]");
}
