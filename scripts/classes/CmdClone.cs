using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

public partial class CmdClone : Command
{
	public Vector2I SrcGridLoc;
	public Vector2I[] DestGridLocs;

	public static CmdClone FromTiles(TlHolder srcTl, IEnumerable<TlHolder> destTls) 
		=> new(srcTl.GetGridLoc(), destTls.Select((tl) => tl.GetGridLoc()));

	public CmdClone(Vector2I srcGLoc, IEnumerable<Vector2I> destGLocs) : base(0)
	{
		SrcGridLoc = srcGLoc;
		DestGridLocs = destGLocs.ToArray();
		Debug.Assert(!DestGridLocs.IsEmpty());
	}

	public override void OnTick(Level lv)
	{
		// TODO: make it clone incrementaly, that is whenever one of the dest tiles is free, an item is
		// is spawned there immediately instead of waiting for all dest tiles to be free at the same time
		// first!

		Debug.Assert(lv.World.GetTile(SrcGridLoc) is TlHolder);
		Debug.Assert(DestGridLocs.All((l) => lv.World.GetTile(l) is TlHolder));
		var srcTile = lv.World.GetTile(SrcGridLoc) as TlHolder;
		if (!srcTile.HasItem() || srcTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		if (DestGridLocs.Any((l) => lv.World.GetTile(l).IsReserved()))
		{
			PauseThisTick();
			return;			
		}

		foreach (var dgloc in DestGridLocs)
		{
			lv.World.CloneItem(SrcGridLoc, dgloc);
		}
	}
}
