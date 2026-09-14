using System;
using Godot;

namespace ArFactory;

public partial class CmdUpdate(Vector2I gloc, Func<int, int> upFunc) : Command(0)
{
	public static CmdUpdate FromTiles(TlHolder tl, Func<int, int> upFunc) 
		=> new(tl.GetGridLoc(), upFunc);

	public Vector2I GridLoc = gloc;
	public Func<int, int> UpdateFunc = upFunc;

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(GridLoc) is TlHolder);
		var targetTile = lv.World.GetTile(GridLoc) as TlHolder;
		if (!targetTile.HasItem() || targetTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		var it = targetTile.GetItem();
		it.SetValue(UpdateFunc.Invoke(it.GetValue()));
	}
}
