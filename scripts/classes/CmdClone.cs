using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Clones an item into the specified tiles, it will spawn brand new items of course.
/// </summary>
public class CmdClone : Command
{
	public Vector2I SrcGridLoc { get; private set; }
	public Vector2I[] DestGridLocs { get; private set; }


	public CmdClone(Vector2I srcGLoc, IEnumerable<Vector2I> destGLocs) : base(0)
	{
		SrcGridLoc = srcGLoc;
		DestGridLocs = [.. destGLocs];
		Debug.Assert(!DestGridLocs.IsEmpty());
		AddSubParallelCmds([new CmdAwaitVacate([srcGLoc], destGLocs)]);
	}

	public CmdClone(Tile srcTl, IEnumerable<Tile> destTls) 
		: this(srcTl.GridLoc, destTls.Select(tl => tl.GridLoc))
	{ }


	protected override void OnTick(Level lv)
	{
		// TODO: make it clone incrementaly, that is whenever one of the dest tiles is free, an item is
		// is spawned there immediately instead of waiting for all dest tiles to be free at the same time
		// first!

		var value = lv.World.GetTile(SrcGridLoc).Item.Value;
		foreach (var dgloc in DestGridLocs)
		{
			lv.World.SpawnItem(dgloc, value);
		}
	}

	public override string ToString()
	{
		var myStr = DestGridLocs.Length > 1 
			? $"[{string.Join(", ", DestGridLocs)}]"
			: DestGridLocs[0].ToString();
		return Utilz.AppendToBaseToString(base.ToString(), $"Clone[at {SrcGridLoc} to {myStr}]");	
	} 
}
