using Godot;
using System;
using System.Reflection.Metadata;

namespace ArFactory;

/// <summary>
/// Slides over an item from a specified tile into another adjacent tiles.
/// </summary>
public class CmdSlide : Command
{
	public Vector2I GridFrom { get; private set; }
	public Direction Dir { get; private set; }
	public Item TrackedItem { get; private set; }
	
	private Action<Level> m_StateFunc;

	public CmdSlide(Vector2I gfrom, Direction dir) : base(1)
	{
		GridFrom = gfrom;
		Dir = dir;
		m_StateFunc = HandleDefault;
		AddSubParallelCmds([ new CmdAwait([gfrom]) ]);
	}
	
	public CmdSlide(Tile tlFrom, Direction dir) : this(tlFrom.GridLoc, dir) { }
	

	public bool IsAwaitingAnim() => m_StateFunc == HandleAfterAnimation;
	public Vector2I GetGridTo() => GridFrom + Dir.ToGrid();

	protected override void DoPerFrame(double dt, Level lv)
	{
		if (m_StateFunc != HandleAfterAnimation)
		{
			return;
		}

		Debug.Assert(TrackedItem != null);

		var srcLoc = lv.World.GridToPos(GridFrom);
		var destLoc = lv.World.GridToPos(GetGridTo());
		var weight = 0.001f*lv.GetTickElapsedMillis() * lv.GetTickRate();
		TrackedItem.Position = srcLoc.Lerp(destLoc, weight);
	}

	/// <summary>
	/// If <see cref="TrackedItem"/> can move, it will move it and return true, otherwise it will 
	/// return false.
	/// </summary>
	public bool TryMovingByOverlap(WorldPanel world)
	{
		Debug.Assert(m_StateFunc == HandleDefault);
		Debug.AssertNotNull(TrackedItem);
		Debug.Assert(TrackedItem.IsAllowedToMove());

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
		Debug.AssertNotNull(m_StateFunc);
		m_StateFunc.Invoke(lv);
	}

	private void HandleDefault(Level lv)
	{
		// Debug.Assert(TrackedItem == null);
		var srcTile = lv.World.GetTile(GridFrom);
		var destTile = lv.World.GetTile(GetGridTo());
		TrackedItem = srcTile.Item;

		if (destTile is null)
		{ 
			// Only hits when the command is made non-blocking and it is directly at a null tile or
			// the world border...
			PauseThisTick();
			return;
		}

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

	private void HandleAfterAnimation(Level lv)
	{
		TrackedItem.SetMidAnimationFlag(false);
		TrackedItem = null;
		m_StateFunc = HandleDefault;
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Slide[at {GridFrom} -> {Dir}]");

	private void PrepTrackedItemForSlidingAnim(WorldPanel world)
	{
		TrackedItem = world.TeleportItemNoSync(GridFrom, GetGridTo());
		TrackedItem.DisallowMovementThisTick();
		TrackedItem.SetMidAnimationFlag(true);
		m_StateFunc = HandleAfterAnimation;
	}
}