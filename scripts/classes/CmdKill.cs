using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Deletes an item in the specified location.
/// </summary>
public class CmdKill(Vector2I gloc) : Command(1)
{
	public Vector2I GLocation { get; private set; } = gloc;

	public CmdKill (Tile tl) : this(tl.GridLoc) { }

	protected override void OnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		Debug.Assert(HasTileWithItem(lv.World, GLocation));
		Tile tl = lv.World.GetTileOrNull(GLocation);
		if (tl.Item.IsMidAnimation() || !tl.DestroyItem())
		{
			throw new InvalidProgramException();	
		}
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Kill[at {GLocation}]");
}
