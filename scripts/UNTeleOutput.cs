using System;
using Godot;

namespace ArFactory;

public partial class UNTeleOutput : Unit
{
	public Tile OutTile => GetTile(Vector2I.Zero);
	public int Delay { get; private set; }

	// This can be removed because it's entirely managed by the other side.
	// There will never be a case where this is invalid and none-null; the other side will make sure to keep this up
	// to date.
	private WeakReference<UNTeleInput>? m_MatchingIn;

	public void Setup(WorldPanel world, Vector2I gloc, Direction dir, int delay)
	{
		Debug.Assert(delay > 0);
		BaseInit(world, TickType.OnDemand, 1, gloc, Vector2I.One, dir, true);
		this.Delay = delay - 1;
	}

	public override void _ExitTree()
	{
		UnassignInput();
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddOutput(Vector2I.Zero, Direction.East, true);
	}

	public override void PendNewCommands(Editor.PendProxy pender)
	{
		if (HasPendingCmds() && !AllCmdsAreEndingSlidingCmds())
		{
			PauseThisTick();
			return;
		}

		if (OutTile.HasItem())
		{
			pender.Pend([
				Delay > 0 ? [ new CmdSleep(Delay) ] : [],
				[ new CmdSlide(OutTile, Dir) ],
			]);
		}
	}

	public override bool CanFaceDir(Direction dir) => true;
	public bool IsConnectedToSomething() => m_MatchingIn is not null;
	public bool IsConnectedTo(UNTeleInput what)
		=> m_MatchingIn is not null && m_MatchingIn.TryGetTarget(out var tgt) && tgt == what;
	
	// For now I made it a method because maybe later I will need to update a visual of some sort.
	public void SetDelay(int newVal)
	{
		Debug.Assert(Delay >= 0);
		Delay = newVal;
	}

	public override string ToString()
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "TeleIn");

	public override void _Draw()
	{
		base._Draw();
		if (m_MatchingIn is not null)
		{
			DrawRect(new Rect2(World.GetCellSize()*0.4f, 0.2f*World.GetCellSize()), Godot.Colors.Cyan);
		}
	}

	public UNTeleInput? GetConnectedInput()
	{
		if (m_MatchingIn is not null && m_MatchingIn.TryGetTarget(out var tgt))
		{
			return tgt;
		}
		else return null;
	}

	/// <summary>
	/// This should be called by the input side.
	/// </summary>
	public void AssignInput(UNTeleInput ass)
	{
		UnassignInput();
		m_MatchingIn = new(ass);
		QueueRedraw();
	}

	public void UnassignInput()
	{
		if (m_MatchingIn is not null && m_MatchingIn.TryGetTarget(out var tgt))
		{
			m_MatchingIn = null;
			tgt.UnassignOutput();
			QueueRedraw();
		}
	}
}
