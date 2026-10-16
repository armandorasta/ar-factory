using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Deletes an item in the specified location.
/// </summary>
public class CmdKill(Vector2I gloc) : Command(1)
{
	public Vector2I GLocation { get; private set; } = gloc;

	public CmdKill (Tile tl) : this(tl.GLocation) { }

	protected override void OnTick(Simulator.OnTickMediator ticker)
	{
		Debug.Assert(HasTileWithItemNotMidAnim(ticker.World, GLocation));
		
		Tile tl = ticker.World.GetTileNonNull(GLocation);
		if (tl.Item.IsMidAnimation())
		{
			throw new InvalidProgramException();	
		}
		tl.DestroyItem();
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Kill[at {GLocation}]");
}
