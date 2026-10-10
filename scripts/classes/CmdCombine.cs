using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Combines items in multiple locations into one.
/// </summary>
/// <remarks>
/// <paramref name="glDest"/> can overlap with one of the sources as the old items will be destroyed before the new 
/// item is spawned.
/// </remarks>
public class CmdCombine(Vector2I glLeft, Vector2I glRight, Vector2I glDest, BinaryOperation op) : Command(1)
{
	public readonly Vector2I LeftGL = glLeft;
	public readonly Vector2I RightGL = glRight;
	public readonly Vector2I ResultGL = glDest;
	public readonly BinaryOperation Op = op;

	public CmdCombine(Tile tlLeft, Tile tlRight, Tile tlDest, BinaryOperation op) 
		: this(tlLeft.GridLoc, tlRight.GridLoc, tlDest.GridLoc, op)
	{ }


	protected override void OnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		Debug.Assert(HasTileWithItem(lv.World, LeftGL));
		Debug.Assert(HasTileWithItem(lv.World, RightGL));
		Debug.Assert(HasTileUnreserved(lv.World, ResultGL));

		Tile tlLeft = lv.World.GetTileNonNull(LeftGL);
		Tile tlRight = lv.World.GetTileNonNull(RightGL);

		if (tlLeft.Item.IsMidAnimation() || tlRight.Item.IsMidAnimation())
		{
			throw new InvalidProgramException();
		}

		var finalVal = Op.Apply(tlLeft.Item.Value, tlRight.Item.Value);
		tlLeft.DestroyItem();
		tlRight.DestroyItem();
		if (lv.World.SpawnItem(ResultGL, finalVal) is null)
		{
			throw new InvalidProgramException();
		}
	}

	public override string ToString()
	{
		return Utilz.AppendToBaseToString(base.ToString(), 
			$"Combine[{LeftGL} {Op.ToString()} {RightGL} -> {ResultGL}]");	
	} 
}