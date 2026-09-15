using System;
using Godot;

namespace ArFactory;

public partial class CmdCondSlide : Command
{
	public Func<Tile, bool> CheckFunc;
	public Vector2I SrcGridLoc;

	public Vector2I TrueGridLoc;
	public Direction TrueDir;

	public Vector2I FalseGridLoc;
	public Direction FalseDir;


	private CmdSlide m_SlideCmd;
	private Action<Level> m_StateFunc;

	public static CmdCondSlide FromTiles(TlHolder srcTl, TlHolder tlTrue, Direction dirTrue, 
		TlHolder tlFalse, Direction dirFalse, Func<Tile, bool> checkFunc)
		=> new(srcTl.GetGridLoc(), tlTrue.GetGridLoc(), dirTrue, tlFalse.GetGridLoc(), dirFalse, 
			checkFunc);

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
	}

	public override void OnTick(Level lv)
	{
		m_StateFunc.Invoke(lv);
	}

	public override void DoPerFrame(double dt, Level lv)
	{
		m_SlideCmd?.DoPerFrame(dt, lv);
	}

	private void HandleDefault(Level lv)
	{
		Debug.Assert(lv.World.GetTile(SrcGridLoc) is TlHolder);	
		Debug.Assert(lv.World.GetTile(TrueGridLoc) is TlHolder);	
		Debug.Assert(lv.World.GetTile(FalseGridLoc) is TlHolder);
		Debug.Assert(m_SlideCmd == null);

		var srcTile = lv.World.GetTile(SrcGridLoc) as TlHolder;
		if (!srcTile.HasItem() || srcTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		if (CheckFunc.Invoke(srcTile))
		{
			lv.World.TeleportItem(srcTile.GetGridLoc(), TrueGridLoc);
			m_SlideCmd = new(TrueGridLoc, TrueDir);
		}
		else
		{
			lv.World.TeleportItem(srcTile.GetGridLoc(), FalseGridLoc);
			m_SlideCmd = new(FalseGridLoc, FalseDir);
		}

		m_StateFunc = HandleSlide;
	}

	private void HandleSlide(Level lv)
	{
		Debug.Assert(m_SlideCmd != null);
		m_SlideCmd.OnTick(lv);
		m_SlideCmd.CountThisTick();
		if (!m_SlideCmd.IsDone())
		{
			PauseThisTick();
			return;
		}

		m_SlideCmd = null;
		m_StateFunc = HandleDefault;
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(),
			$"CondSlide[{SrcGridLoc} ? {TrueGridLoc} -> {TrueDir} : {FalseGridLoc} -> {FalseDir}]");
}
