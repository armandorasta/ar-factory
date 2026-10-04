using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Waits for an item to arrive at specific locations, makes sure if it's sliding into the target 
/// tile that the animation is over.
/// </summary>
public class CmdAwait : Command
{
	public Vector2I[] GridLocs { get; private set; }

	public CmdAwait(IEnumerable<Vector2I> glocs) : base(1)
	{
		GridLocs = [.. glocs];
		Debug.Assert(GridLocs.Length > 0);
	}

	public CmdAwait(IEnumerable<Tile> tls) : this(tls.Select(tl => tl.GridLoc)) { }
	public CmdAwait(Tile tl) : this([tl.GridLoc]) { }

	protected override void OnTick(Level lv)
	{
		var myTiles = GridLocs.Select(lv.World.GetTile);
		if (!myTiles.All(tl => tl is not null))
		{
			PauseThisTick();
			return; // Forever and ever...
		}

		if (!myTiles.All(tl => tl.HasItem() && !tl.Item.IsMidAnimation()))
		{
			PauseThisTick();
			return;
		}

		Debug.Assert(myTiles.All(tl => tl.Item.IsAllowedToMove()));
	}

	public override string ToString()
	{
		var myStr = (GridLocs.Length > 1) ? $"[{string.Join(", ", GridLocs)}]" : GridLocs[0].ToString();
		return Utilz.AppendToBaseToString(base.ToString(), $"Await[at {myStr}]");
	}
}
