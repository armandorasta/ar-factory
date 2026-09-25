using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Updates the value of an item in a specified location.
/// </summary>
public class CmdUpdate : Command
{
	public Vector2I GridLoc { get; private set; }
	public Func<int, int> UpdateFunc { get; private set; }

	public CmdUpdate(Vector2I gloc, Func<int, int> upFunc) : base(0)
	{
		GridLoc = gloc;
		UpdateFunc = upFunc;
		AddSubParallelCmds([ new CmdAwait([gloc]) ]);
	}

	public CmdUpdate(Tile tl, Func<int, int> upFunc) : this(tl.GridLoc, upFunc) { }


	protected override void OnTick(Level lv)
	{
		var it = lv.World.GetTile(GridLoc).Item;
		it.SetValue(UpdateFunc.Invoke(it.Value));
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Update[at {GridLoc}]");
}
