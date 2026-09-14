using Godot;
using System;

namespace ArFactory;

// Waits for an item to arrive at a specific tile, makes sure if it's sliding in that the animation
// is over.
public partial class CmdAwait(Vector2I gloc) : Command(1)
{
	public static CmdAwait FromTiles(TlHolder tl) => new(tl.GetGridLoc());

	public Vector2I GridLoc = gloc;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(GridLoc) is TlHolder);
		var targetTile = lv.World.GetTile(GridLoc) as TlHolder;
		if (!targetTile.HasItem() || targetTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		Debug.Assert(targetTile.GetItem().IsAllowedToMove());
	}
}
