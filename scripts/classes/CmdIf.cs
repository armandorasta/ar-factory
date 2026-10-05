using System;
using System.Collections.Generic;
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
		Debug.AssertNotNull(tl);
		
		return check switch
		{
			CheckType.NoItem    => tl.HasItem() is false,
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


	public Vector2I GLocation { get; private set; }
	public CheckType Check { get; private set; }
	public int CmpValue { get; private set; }
	public List<Command> TrueCmdSeq { get; private set; }
	public List<Command> FalseCmdSeq { get; private set; }

	private CommandRunner m_BranchRunner;
	private Action<Level> m_StateFunc;


	public CmdIf(Vector2I gloc, CheckType check, int cmpVal,
		IEnumerable<Command> trueBranch, 
		IEnumerable<Command> elseBranch = null) 
		: base(1, true)
	{
		Debug.Assert(Item.IsValueWithinRange(cmpVal));
		Debug.AssertNotNull(trueBranch);

		GLocation = gloc;
		Check = check;
		CmpValue = Item.Clamp(cmpVal);
		TrueCmdSeq = [.. trueBranch];
		FalseCmdSeq = elseBranch is null ? null : [.. elseBranch];

		m_StateFunc = HandleDefault;
	}

	public CmdIf(Tile tl, CheckType check, int cmpVal, 
		IEnumerable<Command> cmdsIfTrue, 
		IEnumerable<Command> cmdsIfFalse = null) 
		: this(tl.GridLoc, check, cmpVal, cmdsIfTrue, cmdsIfFalse)
	{ }


	protected override void OnTick(Level lv)
	{
		m_StateFunc.Invoke(lv);
	}

	protected override void DoPerFrame(double dt, Level lv)
	{
		m_BranchRunner?.DoPerFrame(dt, lv);
	}

	protected override void OnEdge(Level lv)
	{
		if (!m_BranchRunner.OnEdge(lv))
		{
			PauseThisTick();
		}
	}


	private void HandleDefault(Level lv)
	{
		Debug.Assert(lv.World.HasTile(GLocation));

		var bCondMet = Apply(Check, lv.World.GetTile(GLocation), CmpValue);
		var winnerBranch = bCondMet ? TrueCmdSeq : FalseCmdSeq;
		if (winnerBranch is null)
		{
			ForceFinish();
			return;
		}
		
		// So they don't keep commands alive for no reason.
		TrueCmdSeq = null;
		FalseCmdSeq = null;

		m_BranchRunner = new([winnerBranch]);
		m_StateFunc = HandleBranch;
		m_StateFunc.Invoke(lv);
	}

	private void HandleBranch(Level lv)
	{
		m_BranchRunner.OnTick(lv);
	}

	public override string ToString()
	{
		var condStr = Check switch
		{
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
