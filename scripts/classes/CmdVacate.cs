using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Waits for a tile in be usable by an item (not reserved), if an item starts sliding away from one
/// location, it counts it as empty immediately without waiting for the item to finish its animation.
/// </summary>
public class CmdVacate : Command
{
	public Vector2I[] GridLocs { get; private set; }

	public CmdVacate(IEnumerable<Vector2I> glocs) : base(0)
	{
		GridLocs = [.. glocs];
		Debug.Assert(GridLocs.Length > 0);
	}

	public CmdVacate(IEnumerable<Tile> tls) : this(tls.Select(tl => tl.GridLoc)) { }

	protected override void OnTick(Level lv)
	{
		var myTiles = GridLocs.Select(lv.World.GetTile);
		if (!myTiles.All(tl => tl is not null))
		{
			PauseThisTick();
			return; // Forever and ever...
		}

		if (!myTiles.All(tl => !tl.IsReserved()))
		{
			PauseThisTick();
			return;
		}
	}

	public override string ToString()
	{
		var myStr = (GridLocs.Length > 1) ? $"[{string.Join(", ", GridLocs)}]" : GridLocs[0].ToString();
		return Utilz.AppendToBaseToString(base.ToString(), $"Vacate[at {myStr}]");
	}
}
