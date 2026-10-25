using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Godot;

namespace ArFactory;

public partial class UNSlider : Unit
{
	private enum SyncState
	{
		None,
		Key,
		Lock,
	}

	private SyncState m_State = SyncState.None;
	private WeakReference<UNSlider>? m_Partner;

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir, true);
	}

	public override void _ExitTree()
	{
		if (m_State is SyncState.Key or SyncState.Lock)
		{
			Debug.AssertNotNull(m_Partner);
			Debug.Assert(m_Partner!.TryGetTarget(out var tgt));
			tgt.DisconnectSyncPirim(bBothWays: false);
		}
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddOutput(Vector2I.Zero, Direction.East);
		AddInput(Vector2I.Zero, Direction.North);
		AddInput(Vector2I.Zero, Direction.West);
		AddInput(Vector2I.Zero, Direction.South);
	}

	public override void PendNewCommands(Editor.PendProxy pender)
	{
		if (HasPendingCmds() && !AllCmdsAreEndingSlidingCmds())
		{
			PauseThisTick();
			return;
		}

		if (World.HasTile(GLocation + Dir.ToGrid()))
		{
			var tl = GetTile(Vector2I.Zero);
			// We have to do it manually like this, this way the slider is synced with the clock at all times.
			// I could also just add a third type, but I learned the hard way that simple is always the way to go.
			if (tl.HasItem())
			{
				switch (m_State) {
				case SyncState.None:
					break;

				case SyncState.Key:
					Debug.AssertNotNull(m_Partner);
					Debug.Assert(m_Partner!.TryGetTarget(out var tgt));
					tgt.Unlock();
					break;

				case SyncState.Lock:
					Lock();
					break;

				default:
					throw new NotImplementedException();
				}

				pender.Pend([ [new CmdSlide(tl, Dir)] ]);
			}
		}
	}

	public override bool CanFaceDir(Direction dir) => true;
	public bool HasSyncPirimInstalled() => m_Partner is not null;

	public override string ToString()
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "Slider");

	public override void _Draw()
	{
		base._Draw();
		switch (m_State) {
		case SyncState.None:
			break;
		
		case SyncState.Key:
			Debug.AssertNotNull(m_Partner);
			Debug.Assert(m_Partner!.TryGetTarget(out var tgt));
			DrawLine(
				0.5f*World.GetCellSize(), 
				World.GridToPos(tgt.GLocation) - Position + 0.5f*World.GetCellSize(),
				Godot.Colors.Pink);
			break;
		
		case SyncState.Lock:
			DrawRect(new Rect2(World.GetCellSize()*0.4f, 0.2f*World.GetCellSize()), Godot.Colors.Pink);
			break;
		
		default:
			throw new NotImplementedException();
		}
	}
	
	public void InstallKey(UNSlider target)
	{
		Debug.Assert(m_State is SyncState.None);
		Debug.Assert(target.m_State is SyncState.Lock);
		m_Partner = new(target);
		m_State = SyncState.Key;
		QueueRedraw();
	}

	public void DisconnectSyncPirim(bool bBothWays = true)
	{
		if (m_Partner is not null)
		{
			m_State = SyncState.None;
			var par = m_Partner;
			m_Partner = null;
			
			if (bBothWays)
			{
				Debug.Assert(par.TryGetTarget(out var tgt));
				tgt.DisconnectSyncPirim(false);
			}
			
			QueueRedraw();
		}
	}

	public void InstallLock(UNSlider locker)
	{
		Debug.Assert(m_State is SyncState.None);
		m_State = SyncState.Lock;
		m_Partner = new(locker);
		QueueRedraw();
	}
}
