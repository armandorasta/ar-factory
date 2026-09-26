using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArFactory;

public class CmdIf : Command
{
	public enum CheckType
	{
		NoItem,
		HasItem,
		Equal,
		NotEq,
		Greater,
		GreaterEq,
		Less,
		LessEq,
	}

	/// <summary>
	/// Applies a check on a tile, the tile is considered to be left hand side for binary operations.
	/// </summary>
	public static bool Apply(CheckType check, Tile tl, int rhs)
	{ // Public for testing...
		Debug.Assert(Enum.IsDefined(check));
		return tl != null && check switch
		{
			CheckType.NoItem    => !tl.HasItem(),
			CheckType.HasItem   => tl.HasItem(),
			CheckType.Equal     => tl.HasItem() && tl.Item.Value == rhs,
			CheckType.NotEq     => tl.HasItem() && tl.Item.Value != rhs,
			CheckType.Greater   => tl.HasItem() && tl.Item.Value >  rhs,
			CheckType.GreaterEq => tl.HasItem() && tl.Item.Value >= rhs,
			CheckType.Less      => tl.HasItem() && tl.Item.Value <  rhs,
			CheckType.LessEq    => tl.HasItem() && tl.Item.Value <= rhs,
			_ => throw new NotImplementedException()
		};
	}


	public Vector2I GridLoc { get; private set; }
	public CheckType Check { get; private set; }
	public int CmpValue { get; private set; }
	public List<Command> TrueCmdSeq { get; private set; }
	public List<Command> FalseCmdSeq { get; private set; }
	
	// private CommandRunner m_BranchRunner;


	public CmdIf(Vector2I gloc, CheckType check, int cmpVal,
		IEnumerable<Command> trueBranch, 
		IEnumerable<Command> elseBranch = null) 
		: base(0)
	{
		Debug.Assert(Item.IsValueWithinRange(cmpVal));
		Debug.AssertNotNull(trueBranch);

		GridLoc = gloc;
		Check = check;
		CmpValue = Item.Clamp(cmpVal);
		TrueCmdSeq = [.. trueBranch];
		FalseCmdSeq = elseBranch is null ? null : [.. elseBranch];
	}

	public CmdIf(Tile tl, CheckType check, int cmpVal, 
		IEnumerable<Command> cmdsIfTrue, 
		IEnumerable<Command> cmdsIfFalse = null) 
		: this(tl.GridLoc, check, cmpVal, cmdsIfTrue, cmdsIfFalse)
	{ }


	protected override void OnTick(Level lv)
	{
		if (CmpValue == int.MaxValue) // Means the branch was already executed.
		{
			return;
		}

		var isCondMet = Apply(Check, lv.World.GetTile(GridLoc), CmpValue);
		var winnerBranch = isCondMet ? TrueCmdSeq : FalseCmdSeq;
		if (winnerBranch is null)
		{
			ForceFinish();
			return;
		}

		AddSubParallelCmds(winnerBranch);
		CmpValue = int.MaxValue; // For indication.
	}

	/// <summary>
	/// Does nothing in release, this operation simply doesn't make any sense here.
	/// Will make another command for that if I need it and call it CmdUntil or something.
	/// </summary>
	public override Command MakeNonBlocking()
	{
		Debug.Assert(false, "CmdIf cannot be made non-blocking");
		return this;
	}

	public override string ToString()
	{
		var condStr = Check switch
		{
			CheckType.NoItem    => $"no item in {GridLoc}",
			CheckType.HasItem   => $"has item in {GridLoc}",
			CheckType.Equal     => $"item in {GridLoc} == {CmpValue}",
			CheckType.NotEq     => $"item in {GridLoc} != {CmpValue}",
			CheckType.Greater   => $"item in {GridLoc} >  {CmpValue}",
			CheckType.GreaterEq => $"item in {GridLoc} >= {CmpValue}",
			CheckType.Less      => $"item in {GridLoc} <  {CmpValue}",
			CheckType.LessEq    => $"item in {GridLoc} <= {CmpValue}",
			_ => throw new NotImplementedException(),
		};
		var elseStr = FalseCmdSeq is null? "" : $" : {FalseCmdSeq}";

		return Utilz.AppendToBaseToString(base.ToString(), $"If[{condStr}? {TrueCmdSeq}{elseStr}]");
	}
}
