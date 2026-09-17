using System;
using Godot;

namespace ArFactory;

public partial class CmdUpdate(Vector2I gloc, Func<int, int> upFunc) : Command(0)
{
	public static CmdUpdate FromTiles(Tile tl, Func<int, int> upFunc) 
		=> new(tl.GridLoc, upFunc);

	public Vector2I GridLoc = gloc;
	public Func<int, int> UpdateFunc = upFunc;

	public override void OnTick(Level lv)
	{
		Debug.AssertIs(lv.World.GetTile(GridLoc), typeof(Tile));
		var targetTile = lv.World.GetTile(GridLoc);
		if (!targetTile.HasItem() || targetTile.Item.IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		var it = targetTile.Item;
		it.SetValue(UpdateFunc.Invoke(it.GetValue()));
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Update[at {GridLoc}]");
}
