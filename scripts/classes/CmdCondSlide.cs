using System;
using Godot;

namespace ArFactory;

/// <summary>
/// Teleports and slides an item based on a condition.
/// </summary>
public class CmdCondSlide : Command
{
	public Func<Tile, bool> CheckFunc;
	public Vector2I SrcGridLoc;

	public Vector2I TrueGridLoc { get; private set; }
	public Direction TrueDir { get; private set; }

	public Vector2I FalseGridLoc { get; private set; }
	public Direction FalseDir { get; private set; }

	private Action<Level> m_StateFunc;

	public CmdCondSlide(Vector2I srcGLoc, Vector2I gtrue, Direction dirTrue, Vector2I gfalse,
		Direction dirFalse, Func<Tile, bool> checkFunc) : base(1)
	{
		CheckFunc = checkFunc;
		SrcGridLoc = srcGLoc;
		TrueGridLoc = gtrue;
		TrueDir = dirTrue;
		FalseGridLoc = gfalse;
		FalseDir = dirFalse;

		m_StateFunc = HandleDefault;
		AddSubParallelCmds([ new CmdAwait([srcGLoc]) ]);
	}

	public CmdCondSlide(Tile srcTl, Tile tlTrue, Direction dirTrue, Tile tlFalse, 
		Direction dirFalse, Func<Tile, bool> checkFunc)
		: this(srcTl.GridLoc, tlTrue.GridLoc, dirTrue, tlFalse.GridLoc, dirFalse, checkFunc)
	{ }


	protected override void OnTick(Level lv)
	{
		m_StateFunc.Invoke(lv);
	}

	private void HandleDefault(Level lv)
	{
		var srcTile = lv.World.GetTile(SrcGridLoc);

		if (CheckFunc.Invoke(srcTile))
		{
			lv.World.TeleportItem(SrcGridLoc, TrueGridLoc);
			AddSubParallelCmds([new CmdSlide(TrueGridLoc, TrueDir)]);
		}
		else
		{
			lv.World.TeleportItem(srcTile.GridLoc, FalseGridLoc);
			AddSubParallelCmds([new CmdSlide(FalseGridLoc, FalseDir)]);
		}

		m_StateFunc = HandleSlide;
	}

	private void HandleSlide(Level lv) // Gets called after the slide is handled in the bg.
	{
		// Empty function needed so the tick counter goes up...
		m_StateFunc = HandleDefault;
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(),
			$"CondSlide[{SrcGridLoc} ? {TrueGridLoc} -> {TrueDir} : {FalseGridLoc} -> {FalseDir}]");
}
