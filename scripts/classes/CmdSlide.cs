using Godot;
using System;

namespace ArFactory;

// Waits for an item to arrive at a specific tile, makes sure if it's sliding in that the animation
// is over.
public partial class CmdSlide : Command
{
	public static CmdSlide FromTiles(TlHolder tlFrom, Direction dir) => new(tlFrom.GetGridLoc(), dir);
	public static CmdSlide FromTiles(TlIO tlIO) => new(tlIO.GetGridLoc(), tlIO.GetOutputDir());
	public static CmdSlide FromTiles(TlOutput tlIO) => new(tlIO.GetGridLoc(), tlIO.GetDir());

	public Vector2I GridFrom { get; private set; }
	public Direction Dir { get; private set; }
	public Item TrackedItem { get; private set; }
	
	private Action<Level> m_StateFunc;

	public CmdSlide(Vector2I gfrom, Direction dir) : base(1)
	{
		GridFrom = gfrom;
		Dir = dir;
		m_StateFunc = HandleDefault;
	}

	public bool IsAwaitingAnim() => m_StateFunc == HandleAfterAnimation;
	public Vector2I GetGridTo() => GridFrom + Dir.ToGrid();

	public override void DoPerFrame(double dt, Level lv)
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

		var gridTo = GetGridTo();
		if (world.GetTile<TlHolder>(gridTo).IsReserved())
		{
			return false;
		}

		TrackedItem = world.TeleportItem(GridFrom, gridTo);
		TrackedItem.DisallowMovementThisTick();
		TrackedItem.SetMidAnimationFlag(true);
		CountThisTick(); // Undo the pausing because the command has advanced.
		m_StateFunc = HandleAfterAnimation;
		return true;
	}

	public override void OnTick(Level lv)
	{
		Debug.Assert(m_StateFunc != null);
		m_StateFunc.Invoke(lv);
	}

	private void HandleDefault(Level lv)
	{
		// Debug.Assert(TrackedItem == null);
		Debug.AssertIs(lv.World.GetTile(GridFrom), typeof(TlHolder));
		
		var gridTo = GetGridTo();
		if (!lv.World.HasTile(gridTo))
		{
			PauseThisTick();
			return; // Forever and ever...
		}

		var srcTile = lv.World.GetTile<TlHolder>(GridFrom);
		if (!srcTile.HasItem() || srcTile.GetItem().IsMidAnimation())
		{
			PauseThisTick();
			return;
		}

		TrackedItem = srcTile.GetItem();

		var destTile = lv.World.GetTile<TlHolder>(gridTo);
		if (!destTile.CanItemEnterInDir(srcTile.GetItem(), Dir))
		{
			PauseThisTick();
			return; // Forever and ever probably...
		}

		if (destTile.IsReserved())
		{
			lv.World.QueueBlockedSlideCmdByAnotherItem(this);
			PauseThisTick();
			return;
		}
		
		// Can't use WorldPanel.TeleportItem because it will set the position to the destination 
		// immediately.
		destTile.SetItemUnchecked(srcTile.ExtractItem());
		TrackedItem.DisallowMovementThisTick();
		TrackedItem.SetMidAnimationFlag(true);
		m_StateFunc = HandleAfterAnimation;
		// `do_per_frame` animates until next tick.
	}

	private void HandleAfterAnimation(Level lv)
	{
		TrackedItem.SetMidAnimationFlag(false);
		TrackedItem = null;
		m_StateFunc = HandleDefault;
	}

	public override string ToString()
		=> Utilz.AppendToBaseToString(base.ToString(), $"Slide[at {GridFrom} -> {Dir}]");
}