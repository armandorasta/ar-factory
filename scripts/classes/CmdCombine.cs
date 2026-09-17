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


	/// <summary>
	/// `combFunc` will take values of items in specified locations is the same order of the locations, 
	/// and returns the value will be assigned to the generated item.
	/// `destGLoc` can overlap with `srcGLocs`; the old items will be destroyed before the new item is
	/// spawned.
	/// </summary>
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
		Debug.AssertIs(lv.World.GetTile(DestGridLoc), typeof(TlHolder));
		Debug.Assert(SrcGridLocs.All((l) => lv.World.GetTile(l) is TlHolder));
		
		var destTile = lv.World.GetTile<TlHolder>(DestGridLoc);
		if (destTile.IsReserved())
		{
			PauseThisTick();
			return;
		}

		if (SrcGridLocs.Any((l) => !lv.World.GetTile<TlHolder>(l).HasItem()))
		{
			PauseThisTick();
			return;
		}

		// This asserts that await was called before this command.
		// TODO: Add sub-commands and remove the needs for the below assert.
		Debug.Assert(SrcGridLocs.All((l) => !lv.World.GetTile<TlHolder>(l).GetItem().IsMidAnimation()));

		var values = SrcGridLocs.Select((l) => lv.World.GetTile<TlHolder>(l).GetItem().GetValue());
		foreach (var sgloc in SrcGridLocs)
		{
			lv.World.GetTile<TlHolder>(sgloc).DestroyItem();
		}
		lv.World.SpawnItem(DestGridLoc, CombFunc.Invoke(values));
	}

	public override string ToString()
	{
		var myStr = string.Join(", ", SrcGridLocs);
		return Utilz.AppendToBaseToString(base.ToString(), $"Combine[at [{myStr}] into {DestGridLoc}]");	
	} 
}
