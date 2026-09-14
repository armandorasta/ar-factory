using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

public partial class CmdCombine : Command
{
	public Vector2I[] SrcGridLocs;
	public Vector2I DestGridLoc;
	public Func<IEnumerable<int>, int> CombFunc;

	public static CmdCombine FromTiles(TlHolder[] srcTls, TlHolder destTl, 
		Func<IEnumerable<int>, int> combFunc) 
		=> new(srcTls.Select((tl) => tl.GetGridLoc()), destTl.GetGridLoc(), combFunc);


	// `combFunc` will take values of items in specified locations is the same order of the locations, 
	// and returns the value will be assigned to the generated item.
	// `destGLoc` can overlap with `srcGLocs`; the old items will be destroyed before the new item is
	// spawned.
	public CmdCombine(IEnumerable<Vector2I> srcGLocs, Vector2I destGLoc, 
		Func<IEnumerable<int>, int> combFunc) : base(0)
	{
		SrcGridLocs = srcGLocs.ToArray();
		Debug.Assert(!SrcGridLocs.IsEmpty());
		DestGridLoc = destGLoc;
		CombFunc = combFunc;
	}

	public override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.GetTile(DestGridLoc) is TlHolder);
		Debug.Assert(SrcGridLocs.All((l) => lv.World.GetTile(l) is TlHolder));
		var destTile = lv.World.GetTile(DestGridLoc) as TlHolder;
		if (destTile.IsReserved())
		{
			PauseThisTick();
			return;
		}

		if (SrcGridLocs.Any((l) => !lv.World.GetTile(l).HasItem()))
		{
			PauseThisTick();
			return;
		}

		// This asserts that await was called before this command.
		// TODO: Add sub-commands and remove the needs for the below assert.
		Debug.Assert(SrcGridLocs.All((l) => !lv.World.GetTile(l).GetItem().IsMidAnimation()));

		var values = SrcGridLocs.Select((l) => lv.World.GetTile(l).GetItem().GetValue());
		foreach (var sgloc in SrcGridLocs)
		{
			(lv.World.GetTile(sgloc) as TlHolder).DestroyItem();
		}
		lv.World.SpawnItem(DestGridLoc, CombFunc.Invoke(values));
	}
}
