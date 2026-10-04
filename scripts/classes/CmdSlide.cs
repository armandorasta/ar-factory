using Godot;
using System;

namespace ArFactory;

/// <summary>
/// Slides over an item from a specified tile into another adjacent tiles.
/// </summary>
public class CmdSlide : Command
{
	public Vector2I GridFrom { get; private set; }
	public Direction Dir { get; private set; }
	public WeakReference<Item> TrackedItemRef { get; private set; }
	
	private Action<Level> m_StateFunc;

	public CmdSlide(Vector2I gfrom, Direction dir) : base(1, true)
	{
		GridFrom = gfrom;
		Dir = dir;
		m_StateFunc = HandleDefault;

		// No AwaitVacate because we have multiple passes, AwaitVacate will block the command until
		// no item is present in the destination tile, but we need to proceed then because we have
		// an overlap algorithm that might clear the item away.
		AddSubParallelCmds([ new CmdAwait([gfrom]) ]);
	}
	
	public CmdSlide(Tile tlFrom, Direction dir) : this(tlFrom.GridLoc, dir) { }
	

	public bool IsAwaitingAnim() => m_StateFunc is null;
	public Vector2I GetGridTo() => GridFrom + Dir.ToGrid();

	/// <summary>
	/// If <see cref="TrackedItemRef"/> can move, it will move it and return true, otherwise it will 
	/// return false.
	/// </summary>
	public bool TryMovingByOverlap(WorldPanel world)
	{
		Debug.Assert(m_StateFunc == HandleDefault);
		Debug.AssertNotNull(TrackedItemRef);
		Debug.Assert(TrackedItemRef.TryGetTarget(out var it) && it.IsAllowedToMove());

		// Can't really use vacate here, because this function is called in the same tick, and is
		// called potentially multiple times.

		if (world.GetTile(GetGridTo()).IsReserved())
		{
			return false;
		}

		PrepTrackedItemForSlidingAnim(world);
		CountThisTick(); // Undo the pausing because the command has advanced.
		return true;
	}

	protected override void OnTick(Level lv)
	{
		m_StateFunc.Invoke(lv);
	}

	protected override void DoPerFrame(double dt, Level lv)
	{
		if (!IsAwaitingAnim())
		{
			return;
		}

		Debug.Assert(TrackedItemRef != null);
		if (TrackedItemRef.TryGetTarget(out var trackedIt))
		{
			var srcLoc = lv.World.GridToPos(GridFrom);
			var destLoc = lv.World.GridToPos(GetGridTo());
			var weight = 0.001f*lv.GetTickElapsedMillis() * lv.GetTickRate();
			trackedIt.Position = srcLoc.Lerp(destLoc, weight);
		}
	}

	protected override void OnEdge(Level lv)
	{
		if (!IsAwaitingAnim())
		{
			return;
		}

		Debug.Assert(TrackedItemRef != null);
		if (TrackedItemRef.TryGetTarget(out var trackedIt))
		{
			trackedIt.SyncPosWithGrid(lv.World);
			trackedIt.SetMidAnimationFlag(false);
			m_StateFunc = HandleDefault; // We have to do this because of IsAwaitingAnim
		}
	}

	private void HandleDefault(Level lv)
	{
		Debug.Assert(TrackedItemRef is null);
		if (!lv.World.HasTile(GetGridTo()))
		{ 
			// Only hits when the command is made non-blocking and it is directly at a null tile or
			// the world border...
			PauseThisTick();
			return;
		}

		var destTile = lv.World.GetTile(GetGridTo());
		if (!destTile.CanItemEnter(Dir.Invert()))
		{
			// Probably will be blocked forever tho...
			AddSubParallelCmds([ new CmdAwait([GridFrom]) ]);
			PauseThisTick();
			return; // Forever and ever probably...
		}

		if (destTile.IsReserved())
		{
			// This here is why CanItemEnter must be called before...
			// The await command will be queued when we make sure we can't move in TryMovingByOverlap later.
			lv.World.QueueBlockedSlideCmdByAnotherItem(this);
			PauseThisTick();
			return;
		}

		PrepTrackedItemForSlidingAnim(lv.World);
		// `DoPerFrame` animates until next tick.
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Slide[at {GridFrom} -> {Dir}]");

	private void PrepTrackedItemForSlidingAnim(WorldPanel world)
	{
		var trackedIt = world.TeleportItemNoSync(GridFrom, GetGridTo());
		trackedIt.DisallowMovementThisTick();
		trackedIt.SetMidAnimationFlag(true);
		TrackedItemRef = new(trackedIt);
		m_StateFunc = null;
	}
}