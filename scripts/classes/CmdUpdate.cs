using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Updates the value of an item in a specified location.
/// </summary>
public class CmdUpdate : Command
{
	public enum UpdateType
	{
		Add,
		Mul,
		Override,
	}

	public static int Apply(UpdateType upT, int itVal, int updateVal)
	{
		Debug.Assert(Enum.IsDefined(upT));
		return upT switch
		{
			UpdateType.Add => itVal + updateVal,
			UpdateType.Mul => itVal * updateVal,
			UpdateType.Override => updateVal,
			_ => throw new NotImplementedException(),
		};
	}

	public Vector2I GridLoc { get; private set; }
	public UpdateType UpType { get; private set; }
	public int Value { get; private set; }

	public CmdUpdate(Vector2I gloc, UpdateType upT, int val) : base(0)
	{
		GridLoc = gloc;
		UpType = upT;
		Value = val;
		AddSubParallelCmds([ new CmdAwait([gloc]) ]);
	}

	public CmdUpdate(Tile tl, UpdateType upT, int val) : this(tl.GridLoc, upT, val) { }


	protected override void OnTick(Level lv)
	{
		var it = lv.World.GetTile(GridLoc).Item;
		it.SetValue(Apply(UpType, it.Value, Value));
	}

	public override string ToString()
	{
		var upStr = UpType switch
		{
			UpdateType.Add => $"add {Value}",
			UpdateType.Mul => $"mul by {Value}",
			UpdateType.Override => $"change to {Value}",
			_ => throw new NotImplementedException(),
		};
		return Utilz.AppendToBaseToString(base.ToString(), $"Update[at {GridLoc} {upStr}]");
	}

}
