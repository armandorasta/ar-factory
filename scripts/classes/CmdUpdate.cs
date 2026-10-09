using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Updates the value of an item in a specified location.
/// </summary>
public class CmdUpdate : Command
{
	public Vector2I GLocation { get; private set; }
	public UnaryOperation Op { get; private set; }
	public int Value { get; private set; }

	public CmdUpdate(Vector2I gloc, UnaryOperation op, int val) : base(1)
	{
		Debug.Assert(Enum.IsDefined(op));
		GLocation = gloc;
		Op = op;
		Value = val;
	}

	public CmdUpdate(Tile tl, UnaryOperation op, int val) : this(tl.GridLoc, op, val) { }


	protected override void OnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		Debug.Assert(HasTileWithItem(lv.World, GLocation));
		var it = lv.World.GetTileOrNull(GLocation).Item;
		if (it.IsMidAnimation())
		{
			throw new InvalidProgramException();
		}
		it.SetValue(Op.Apply(it.Value, Value));
	}

	public override string ToString()
	{
		var upStr = Op switch
		{
			UnaryOperation.Add => $"add {Value}",
			UnaryOperation.Mul => $"mul by {Value}",
			UnaryOperation.Override => $"change to {Value}",
			_ => throw new NotImplementedException(),
		};
		return Utilz.AppendToBaseToString(base.ToString(), $"Update[at {GLocation} {upStr}]");
	}

}
