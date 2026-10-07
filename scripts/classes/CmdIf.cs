using System;
using System.Collections.Generic;
using Godot;

namespace ArFactory;

public class CmdIf : Command
{
	public enum CheckType
	{
		NoTile,
		HasTile,
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
	public static bool Apply(CheckType check, Tile? tl, int rhs)
	{ // Public for testing...
		Debug.Assert(Enum.IsDefined(check));
		return check switch
		{
			CheckType.NoTile    => tl is null,
			CheckType.HasTile   => tl != null,
			CheckType.NoItem    => tl is null || !tl.HasItem(),
			CheckType.HasItem   => tl != null && tl.HasItem(),
			CheckType.Equal     => tl != null && tl.HasItem() && tl.Item.Value == rhs,
			CheckType.NotEq     => tl != null && tl.HasItem() && tl.Item.Value != rhs,
			CheckType.Greater   => tl != null && tl.HasItem() && tl.Item.Value >  rhs,
			CheckType.GreaterEq => tl != null && tl.HasItem() && tl.Item.Value >= rhs,
			CheckType.Less      => tl != null && tl.HasItem() && tl.Item.Value <  rhs,
			CheckType.LessEq    => tl != null && tl.HasItem() && tl.Item.Value <= rhs,
			_ => throw new NotImplementedException()
		};
	}


	public Vector2I GLocation { get; private set; }
	public CheckType Check { get; private set; }
	public int CmpValue { get; private set; }
	public List<Command> TrueCmdSeq { get; private set; }
	public List<Command>? FalseCmdSeq { get; private set; }

	private WorldPanel.PendProxy m_Pender;


	public CmdIf(Vector2I gloc, CheckType check, int cmpVal, WorldPanel.PendProxy pender,
		IEnumerable<Command> trueBranch, 
		IEnumerable<Command>? elseBranch = null)
		: base(1)
	{
		Debug.Assert(Item.IsValueWithinRange(cmpVal));
		Debug.AssertNotNull(trueBranch);

		GLocation = gloc;
		Check = check;
		CmpValue = Item.Clamp(cmpVal);
		TrueCmdSeq = [.. trueBranch];
		FalseCmdSeq = elseBranch is null ? null : [.. elseBranch];
		m_Pender = pender;
	}

	public CmdIf(Tile tl, CheckType check, int cmpVal,  WorldPanel.PendProxy pender,
		IEnumerable<Command> cmdsIfTrue, 
		IEnumerable<Command>? cmdsIfFalse = null)
		: this(tl.GridLoc, check, cmpVal, pender, cmdsIfTrue, cmdsIfFalse)
	{ }


	protected override void OnTick(Level lv)
	{
		Debug.Assert(lv.World.HasTile(GLocation));

		var bCondMet = Apply(Check, lv.World.GetTile(GLocation), CmpValue);
		var winnerBranch = bCondMet ? TrueCmdSeq : FalseCmdSeq;
		if (winnerBranch is not null)
		{
			m_Pender.Pend([winnerBranch]);
		}
	}

	public override string ToString()
	{
		var condStr = Check switch
		{
			CheckType.NoTile    => $"no tile in {GLocation}",
			CheckType.HasTile   => $"has tile in {GLocation}",
			CheckType.NoItem    => $"no item in {GLocation}",
			CheckType.HasItem   => $"has item in {GLocation}",
			CheckType.Equal     => $"{GLocation} == {CmpValue}",
			CheckType.NotEq     => $"{GLocation} != {CmpValue}",
			CheckType.Greater   => $"{GLocation} >  {CmpValue}",
			CheckType.GreaterEq => $"{GLocation} >= {CmpValue}",
			CheckType.Less      => $"{GLocation} <  {CmpValue}",
			CheckType.LessEq    => $"{GLocation} <= {CmpValue}",
			_ => throw new NotImplementedException(),
		};
		return Utilz.AppendToBaseToString(base.ToString(), $"If[{condStr}]");
	}
}
