using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Spawns an item into the specified location.
/// </summary>
public class CmdSpawn(Vector2I gloc, int val) : Command(1)
{
	public Vector2I GLocation { get; private set; } = gloc;
	public int Value { get; private set; } = val;

	public CmdSpawn(Tile tl, int val) : this(tl.GLocation, val) { }


	protected override void OnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		Debug.Assert(HasTileUnreserved(lv.World, GLocation));
		if (lv.World.SpawnItem(GLocation, Value) is null)
		{
			throw new InvalidProgramException();
		}
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Spawn[at {GLocation} with val {Value}]");
}
