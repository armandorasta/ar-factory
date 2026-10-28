using Godot;
using System;
using System.Runtime.Serialization;

namespace ArFactory;

/// <summary>
/// Slides over an item from a specified tile into another adjacent tiles.
/// </summary>
public class CmdSlide : Command
{
	public Vector2I GridFrom { get; private set; }
	public Direction Dir { get; private set; }
	public WeakReference<Item>? TrackedItemRef { get; private set; }
	
	private Action<Editor.OnTickMediator>? m_StateFunc;

	public CmdSlide(Vector2I gfrom, Direction dir) : base(1, true)
	{
		GridFrom = gfrom;
		Dir = dir;
		m_StateFunc = HandleDefault;
	}
	
	public CmdSlide(Tile tlFrom, Direction dir) : this(tlFrom.GLocation, dir) { }
	

	public bool IsAwaitingAnim() => m_StateFunc is null;
	public Vector2I GetGridTo() => GridFrom + Dir.ToGrid();
	/// <summary>
	/// Assumes the tile exists.
	/// </summary>
	public bool CanSlideNextTick(WorldPanel world)
	{
		var destTl = world.GetTileOrNull(GetGridTo());
		return destTl is not null 
			&& destTl.CanItemEnter(Dir.Invert()) // No wall in the way
			&& !destTl.IsReserved();             // No item already there/about to be there
	}


	/// <summary>
	/// If <see cref="TrackedItemRef"/> can move, it will move it and return true, otherwise it will return false.
	/// </summary>
	public bool TryMovingByOverlap(WorldPanel world)
	{
		Debug.AssertEq(m_StateFunc, HandleDefault);
		Debug.Assert(TrackedItemRef is null); // Set in PrepTrackedItemForSlidingAnim.
		Debug.Assert(world.HasTile(GetGridTo()));

		if (!world.GetTileNonNull(GetGridTo()).IsReserved())
		{
			PrepTrackedItemForSlidingAnim(world);
			CountThisTick(); // Undo the pausing.
			return true;
		}
		else return false;
	}

	protected override void OnTick(Editor.OnTickMediator ticker)
	{
		m_StateFunc!.Invoke(ticker);
	}

	protected override void DoPerFrame(Editor.PerFrameMediator ticker)
	{
		if (IsAwaitingAnim())
		{
			Debug.Assert(TrackedItemRef != null);
			if (TrackedItemRef.TryGetTarget(out var trackedIt))
			{
				var srcLoc = ticker.World.GridToPos(GridFrom);
				var destLoc = ticker.World.GridToPos(GetGridTo());
				trackedIt.Position = srcLoc.Lerp(destLoc, (float)ticker.TickElapsedPercent);
			}
		}
	}

	protected override void OnEdge(Editor.OnEdgeMediator ticker)
	{
		if (IsAwaitingAnim())
		{
			Debug.Assert(TrackedItemRef != null);
			if (TrackedItemRef.TryGetTarget(out var trackedIt))
			{
				trackedIt.SyncPosWithGrid(ticker.World);
				trackedIt.SetMidAnimationFlag(false);
				m_StateFunc = HandleDefault; // We have to do this because of IsAwaitingAnim
			}
		}
	}

	public override void ForceFinish()
	{
		if (TrackedItemRef is not null && TrackedItemRef.TryGetTarget(out var trackedIt))
		{
			// This resets the animation flag since the command was forced to stop mid-animation; this will cause a
			// visual glitch since I can't sync the position of the item with its grid position like OnEdge does.
			trackedIt.SetMidAnimationFlag(false);
		}
	}

	private void HandleDefault(Editor.OnTickMediator ticker)
	{
		Debug.Assert(TrackedItemRef is null);
		Debug.Assert(HasTileWithItemNotMidAnim(ticker.World, GridFrom));

		// Assume the destination tile exists.
		var destTile = ticker.World.GetTileNonNull(GetGridTo());
		if (!destTile.CanItemEnter(Dir.Invert()))
		{
			PauseThisTick();
			return; // Forever and ever probably...
		}

		if (destTile.IsReserved())
		{
			// This here is why CanItemEnter must be called before...
			ticker.Pender.QueueBlockedSlideCmdByAnotherItem(ticker.World, this);
			PauseThisTick();
			return;
		}

		PrepTrackedItemForSlidingAnim(ticker.World);
		// `DoPerFrame` animates until next tick.
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Slide[at {GridFrom} -> {Dir}]");

	private void PrepTrackedItemForSlidingAnim(WorldPanel world)
	{
		var trackedIt = world.TeleportItemNoSync(GridFrom, GetGridTo())!;
		trackedIt.DisallowMovementThisTick();
		trackedIt.SetMidAnimationFlag(true);
		TrackedItemRef = new(trackedIt);
		m_StateFunc = null;
	}
}