using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Clones an item into the specified tiles, it will spawn brand new items of course.
/// </summary>
public class CmdClone : Command
{
	public Vector2I SourceGL { get; private set; }
	public Vector2I[] DestinationGLs { get; private set; }


	public CmdClone(Vector2I srcGLoc, IEnumerable<Vector2I> destGLocs) : base(1)
	{
		SourceGL = srcGLoc;
		DestinationGLs = [.. destGLocs];
		Debug.Assert(!DestinationGLs.IsEmpty());
		Debug.Assert(DestinationGLs.All(l => l != SourceGL));
	}

	public CmdClone(Tile srcTl, IEnumerable<Tile> destTls) 
		: this(srcTl.GLocation, destTls.Select(tl => tl.GLocation))
	{ }


	protected override void OnTick(Editor.OnTickMediator ticker)
	{
		Debug.Assert(HasTileWithItemNotMidAnim(ticker.World, SourceGL));
		Debug.Assert(DestinationGLs.All( l => HasTileUnreserved(ticker.World, l) ));

		Item it = ticker.World.GetTileNonNull(SourceGL).Item;
		if (it.IsMidAnimation())
		{
			throw new InvalidProgramException("Forgot to call CmdAwait before CmdClone");
		}

		foreach (var dgloc in DestinationGLs)
		{
			if (ticker.World.SpawnItem(dgloc, it.Value) is null)
			{
				throw new InvalidProgramException($"CmdClone failed to spawn clone on {dgloc}");
			}
		}
	}

	public override string ToString()
	{
		var myStr = DestinationGLs.Length > 1 
			? $"[{string.Join(", ", DestinationGLs)}]"
			: DestinationGLs[0].ToString();
		return Utilz.AppendToBaseToString(base.ToString(), $"Clone[at {SourceGL} to {myStr}]");	
	} 
}
