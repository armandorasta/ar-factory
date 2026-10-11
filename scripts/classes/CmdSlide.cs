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
	public WeakReference<Item>? TrackedItemRef { get; private set; }
	
	private Action<Level>? m_StateFunc;

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

	protected override void OnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		m_StateFunc!.Invoke(lv);
	}

	protected override void DoPerFrame(double dt, Level lv)
	{
		if (IsAwaitingAnim())
		{
			Debug.Assert(TrackedItemRef != null);
			if (TrackedItemRef.TryGetTarget(out var trackedIt))
			{
				var srcLoc = lv.World.GridToPos(GridFrom);
				var destLoc = lv.World.GridToPos(GetGridTo());
				var weight = 0.001f * lv.GetTickElapsedMillis() * lv.GetTickRate();
				trackedIt.Position = srcLoc.Lerp(destLoc, weight);
			}
		}
	}

	protected override void OnEdge(Level lv)
	{
		if (IsAwaitingAnim())
		{
			Debug.Assert(TrackedItemRef != null);
			if (TrackedItemRef.TryGetTarget(out var trackedIt))
			{
				trackedIt.SyncPosWithGrid(lv.World);
				trackedIt.SetMidAnimationFlag(false);
				m_StateFunc = HandleDefault; // We have to do this because of IsAwaitingAnim
			}
		}
	}

	private void HandleDefault(Level lv)
	{
		Debug.Assert(TrackedItemRef is null);
		Debug.Assert(HasTileWithItem(lv.World, GridFrom));

		// Assume the destination tile exists.
		var destTile = lv.World.GetTileNonNull(GetGridTo());
		if (!destTile.CanItemEnter(Dir.Invert()))
		{
			PauseThisTick();
			return; // Forever and ever probably...
		}

		if (destTile.IsReserved())
		{
			// This here is why CanItemEnter must be called before...
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
		var trackedIt = world.TeleportItemNoSync(GridFrom, GetGridTo())!;
		trackedIt.DisallowMovementThisTick();
		trackedIt.SetMidAnimationFlag(true);
		TrackedItemRef = new(trackedIt);
		m_StateFunc = null;
	}
}