using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Combines items in multiple locations into one.
/// </summary>
public class CmdCombine : Command
{
	public Vector2I[] SrcGridLocs { get; private set; }
	public Vector2I DestGridLoc { get; private set; }
	public Func<IEnumerable<int>, int> CombFunc { get; private set; }

	/// <summary>
	/// <paramref name="combFunc"/> will take values of items in specified locations is the same 
	/// order of the locations, and returns the value will be assigned to the generated item.
	/// <paramref name="destGLoc"/> can overlap with <paramref name="srcGLocs"/>; the old items will 
	/// be destroyed before the new item is spawned.
	/// </summary>
	public CmdCombine(
		IEnumerable<Vector2I> srcGLocs, Vector2I destGLoc, Func<IEnumerable<int>, int> combFunc) 
		: base(0)
	{
		SrcGridLocs = [.. srcGLocs];
		Debug.Assert(!SrcGridLocs.IsEmpty());
		DestGridLoc = destGLoc;
		CombFunc = combFunc;

		if (srcGLocs.Contains(destGLoc))
		{
			// If the destination overlaps with sources, we don't need to wait for the destination 
			// to be empty as that's literally impossible
			AddSubParallelCmds([ new CmdAwait(srcGLocs) ]);
		}
		else
		{
			AddSubParallelCmds([ new CmdAwaitVacate(srcGLocs, [destGLoc]) ]);
		}
	}

	public CmdCombine(IEnumerable<Tile> srcTls, Tile destTl, Func<IEnumerable<int>, int> combFunc) 
		: this(srcTls.Select(tl => tl.GridLoc), destTl.GridLoc, combFunc) 
	{ }


	protected override void OnTick(Level lv)
	{
		// Collect the values, 
		// destroy the source items first just in case the destination overlaps with one, 
		// spawn the combined item by processing the values.

		var srcTls = SrcGridLocs.Select(lv.World.GetTile);
		var finalVal = CombFunc.Invoke(srcTls.Select(tl => tl.Item.Value));
		foreach (var tl in srcTls)
		{
			tl.DestroyItem();
		}
		
		// Before I tried collecting the values first using Linq, then calling CombFunc after
		// destroying the items, which carshed because Linq is lazily evaluated.

		lv.World.SpawnItem(DestGridLoc, finalVal);
	}

	public override string ToString()
	{
		var myStr = string.Join(", ", SrcGridLocs);
		return Utilz.AppendToBaseToString(base.ToString(), $"Combine[at [{myStr}] into {DestGridLoc}]");	
	} 
}
