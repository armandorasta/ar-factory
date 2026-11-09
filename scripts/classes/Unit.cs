using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace ArFactory;

/// <summary>
/// Some semantics for clarity. 
/// For now units own the tiles they engolf, so anything from outside touching those tiles is instant UB. 
/// This means the unit is reponsible for deleting those tiles when it's deleted.
/// </summary>
public abstract partial class Unit : Node2D, ITileOwner, IDestroyable
{
	public readonly Vector2I InvalidGLocation = new(-1, -1);


	// Nodes
	public Sprite2D Sprite => m_Sprite!;
	private Sprite2D? m_Sprite;

	
	// Events
	/// <summary>
	/// Here you can use the world's draw functions. This is called after the world is done first.
	/// </summary>
	public event Action? RedrawingSelf;
	
	/// <summary>
	/// When <see cref="Destory"/> is called, before <see cref="QueueFree"/>.
	/// </summary>
	public event Action? Destoryed;
	
	/// <summary>
	/// When a signal is recieved from a connector (hopefully).
	/// </summary>
	public event Action? RecievedSignal; // TODO: Handle this!
	
	/// <summary>
	/// When <see cref="Reset"/> is called. Called after it's done resetting, in the base class at least.
	/// </summary>
	public event Action? DidAReset;

	/// <summary>
	/// Varries per unit, some units never call this.
	/// </summary>
	public event Action? TriggerHappened;


	// Public Interface
	public Vector2I GLocation => m_GL;
	public Direction Dir => m_Dir;
	public Vector2I Dimensions => m_Dims;
	public int Rate => m_Rate; // Ticks per work
	public WorldPanel World => m_World!;

	// Privates
	private TickType m_TickType;
	private Vector2I m_GL;
	private Direction m_Dir = Direction.East;
	private Vector2I m_Dims;
	private int m_Rate;
	private readonly List<Command> m_PendingCmds = [];

	private bool m_bLocked = false;
	private bool m_bCanBeLocked = false;
	private bool m_bFloating = false;

	/// <summary>
	/// Keeps track of <see cref="Rate"/> every tick.
	/// </summary>
	private int m_Count = 0;

	/// <summary>
	/// These are checked by the on-demand units
	/// </summary>
	private readonly List<Tile> m_TickTiles = [];
	private WorldPanel? m_World;


	public static bool CanFitIn(WorldPanel world, Vector2I gloc, Vector2I dims, Direction dir)
	{
		var adjustedDims = GetDirAdjustedDims(dims, dir);
		if (!world.GetGridRect().Encloses(new Rect2I(gloc, adjustedDims)))
		{
			return false;
		}
		else for (int y = 0; y < adjustedDims.Y; ++y)
		{
			for (int x = 0; x < adjustedDims.X; ++x)
			{
				if (world.HasTileOn(gloc + new Vector2I(x, y)))
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool CanFitIn(WorldPanel world, Vector2I gloc, Unit u)
	{
		return CanFitIn(world, gloc, u.Dimensions, u.Dir);
	}

	public static Vector2I GetDirAdjustedDims(Vector2I dims, Direction dir)
	{
		return dir is Direction.East or Direction.West ? dims : new(dims.Y, dims.X);
	}

	// This function is called on the `_Ready` function of sub-classes.
	protected void BaseInit(
		WorldPanel world, TickType tickType, int workRate, Vector2I gloc, Vector2I dims, Direction dir,
		bool bLockable = false)
	{
		Debug.Assert(Unit.CanFitIn(world, gloc, dims, dir));
		Debug.Assert(CanFaceDir(dir));
		Debug.Assert(workRate > 0);
		Debug.Assert(Enum.IsDefined(tickType));

		this.m_World = world;
		this.m_Rate = workRate;
		this.m_GL = gloc;
		this.m_Dims = dims;
		this.m_TickType = tickType;
		this.m_bCanBeLocked = bLockable;

		World.Redrawing += OnWorldRedrawing;
		Position = world.GridToPos(gloc);
		InitSprite();
		m_Dir = dir;
		Construct();
	}

	private void InitSprite()
	{
		Sprite.ApplyScale(World.CellWidth / 128.0f * Vector2.One);
		Sprite.Translate(0.5f * World.CellWidth * (Vector2)Dimensions);
		Sprite.Hide();
	}

	protected void InjectorBaseInit(WorldPanel world, int workRate)
	{
		Debug.Assert(workRate > 0);
		m_World = world;
		m_TickType = TickType.Steady;
		m_Rate = workRate;
	}


	#region .Godot Overrides
	
	public override void _Ready()
	{
		this.m_Sprite = GetNodeOrNull<Godot.Sprite2D>("Sprite2D");
	}

	public virtual void Destroy()
	{
		World.Redrawing -= OnWorldRedrawing;

		if (!IsFloating())
		{
			CancelPendingCmds();
			Deconstruct();
		}
		Destoryed?.Invoke();
		QueueFree();
	}

	/// <summary>
	/// Called by WorldPanel.
	/// </summary>
	public override void _Draw()
	{
		if (World.DebugDrawEnabled)
		{
			if (IsFloating())
			{
				return;
			}

			DrawRect(new(Vector2.Zero, World.CellWidth*(Vector2)Dimensions), Godot.Colors.Black, false, -2.0f);
			DrawString(WorldPanel.DebugFont, new Vector2(0.0f, 15.0f), GetType().Name,
				Godot.HorizontalAlignment.Left, World.CellWidth, 16, Godot.Colors.Black);
			if (m_bLocked)
			{
				DrawString(WorldPanel.DebugFont, 0.5f*World.CellWidth * Vector2.One, "L", HorizontalAlignment.Center,
					-1, 30, Godot.Colors.Red);
			}
		}

		RedrawingSelf?.Invoke();
	}

	public void OnWorldRedrawing()
	{
		if (World.DebugDrawEnabled)
		{
			if (!IsFloating())
			{
				foreach (var tl in GetTilesIter())
				{
					tl.DebugDraw(World);
				}
			}
		}
		QueueRedraw();
	}

	#endregion // .Godot Overrides
	#region Abstract Interface

	/// <summary>
	/// Used to specify:
	/// what tiles the unit will occupy,
	/// where the inputs and outputs are and their directions.
	/// <b>The tiles should be built as if the unit is facing east.</b>
	/// </summary>
	protected abstract void BuildTilesAsIfFacingEast();

	/// <summary>
 	/// Used to add commands to the queue using `pend_cmd`.
	/// Checks like <see cref="IsWorkTick"/>, <see cref="HasPendingCmds"/> and <see cref="IsJustAwaitingOutSlideAnim"/>
	/// are used quite often here.
	/// </summary>
	public abstract void PendNewCommands(Simulator.PendProxy pender);

	public virtual void HandleSignal()
	{
		RecievedSignal?.Invoke();
	}


	#endregion // Abstract Interface
	#region Public Interface

	/// <summary>
	/// Returns true if the machine is expected to operate during this tick, <see cref="m_Rate"/> determines how often
	/// this function returns true, ex. when <see cref="m_Rate"/> is 1, it returns true every single tick.
	/// </summary>
	private bool IsWorkTick() => m_Count > 0 && m_Count % m_Rate == 0;

	/// <summary>
	/// Returns true if the command queue still has commands to be executed. You usually pend all the commands you want
	/// executed at once when the queue is empty, then wait until the whole sequence
	/// is executed.
	/// </summary>
	public bool HasPendingCmds() => !m_PendingCmds.All(c => c.IsDone());

	/// <summary>
	/// Checks if there's either no commands left, or there are only slide commands that are just animating the slide
	/// for the user.
	/// </summary>
	protected bool IsSafeToPendCmds() => !HasPendingCmds() || AllCmdsAreEndingSlidingCmds();

	/// <summary>
	/// All the rest of the pending commands are sliding commands that will complete next tick, this means you can go
	/// ahead and overlap a spawn where the item is about to slide away from.
	/// </summary>
	protected bool AllCmdsAreEndingSlidingCmds()
		=> m_PendingCmds.All(c => c is CmdSlide sc && sc.CanSlideNextTick(World));

	public override string ToString()
	{
		var bui = new StringBuilder("Unit[");
		bui.Append(string.Join(", ", [
			GLocation,
			$"{m_Dims.X}x{m_Dims.Y}",
			m_Dir,
			$"{m_Rate}s/t",
		]));
		return bui.Append(']').ToString();
	}

	public bool IsLocked() => m_bLocked;
	public bool CanBeLocked() => m_bCanBeLocked;

	public void Lock()
	{
		Debug.Assert(m_bCanBeLocked);
		m_bLocked = true;
		Sprite.Modulate = Godot.Colors.Red;
		World.QueueRedraw();
	}

	public void Unlock()
	{
		m_bLocked = false;
		Sprite.Modulate = Godot.Colors.White;
		World.QueueRedraw();
	}

	public void ToggleLock()
	{
		if (IsLocked())
		{
			Unlock();
		}
		else
		{
			Lock();
		}
	}

	public bool IsFloating() => m_bFloating;

	/// <summary>
	/// Only works when floating.
	/// </summary>
	public void SetGLocation(Vector2I gloc, bool bNoSync = false)
	{
		Debug.Assert(IsFloating());
		m_GL = gloc;
		if (!bNoSync)
		{
			SyncPosWithGrid();
		}
	}

	/// <summary>
	/// Only works when floating.
	/// </summary>
	public void SyncPosWithGrid()
	{
		Debug.Assert(IsFloating());
		Position = World.GridToPos(GLocation);
	}

	/// <summary>
	/// Must fit not have any other tiles in the way.
	/// </summary>
	public void Construct()
	{
		Debug.Assert(Unit.CanFitIn(World, GLocation, Dimensions, Dir));
		Debug.Assert(!Sprite.Visible);

		Sprite.Show();
		Sprite.Rotation = 0f;

		for (var y = 0; y < Dimensions.Y; ++y)
		{
			for (var x = 0; x < Dimensions.X; ++x)
			{
				var mygloc = GLocation + new Vector2I(x, y);
				Debug.Assert(World.HasTileOn(mygloc) is false);
				var tl = World.AddEmptyTile(mygloc, this);
				tl.MakeSolid(Direction.North)
					.MakeSolid(Direction.East)
					.MakeSolid(Direction.South)
					.MakeSolid(Direction.West);
			}
		}

		BuildTilesAsIfFacingEast();
		Debug.Assert(m_TickType != TickType.OnDemand || m_TickTiles.Count > 0);

		var dir = Dir;
		m_Dir = Direction.East;
		m_bFloating = false;
		SetDirection(dir);
		World.QueueRedraw();
	}

	/// <summary>
	/// Must not be floating already, has pending commands (just call <see cref="Reset"/>), or have any items within.
	/// </summary>
	public void Deconstruct()
	{
		Debug.Assert(!IsFloating());
		Debug.Assert(!HasPendingCmds());
		Debug.Assert(Sprite.Visible);

		Sprite.Hide();

		var limVec = Unit.GetDirAdjustedDims(Dimensions, Dir);
		for (var y = 0; y < limVec.Y; ++y)
		{
			for (var x = 0; x < limVec.X; ++x)
			{
				var mygloc = GLocation + new Vector2I(x, y);
				Debug.Assert(!World.GetTileNonNull(mygloc).IsReserved());
				World.DeleteTile(mygloc);
			}
		}

		m_TickTiles.Clear();
		m_bFloating = true;
		World.QueueRedraw();
	}

	/// <summary>
	/// Returns enclosing Rect2I in grid-space.
	/// </summary>
	public Rect2I GetRect2I() => new(m_GL, m_Dims);

	/// <summary>
	/// Returns enclosing Rect2 in world-space.
	/// </summary>
	public Rect2 GetRect2() => new(World.CellWidth * (Vector2)m_GL, World.CellWidth * (Vector2)m_Dims);

	/// <summary>
	/// Checks if <paramref name="gloc"/> is within the world in grid-space.
	/// </summary>
	public bool IsWithin(Vector2I gloc) => GetRect2I().HasPoint(gloc);
	

	#region .ITileOwner Stuff

	public Tile? GetTile(Vector2I gloc) => World.GetTileOrNull(m_GL + gloc);
	public Tile GetTileNonNull(Vector2I gloc) => World.GetTileNonNull(m_GL + gloc);
	public bool RecieveTile(Tile tl) // Called whenever Deconstruct is called.
	{
		return IsWithin(tl.GLocation);
	}
	public bool TakeAwayTile(Tile tl) => true; // Called whenever Deconstruct is called.
	public IEnumerable<Tile> GetTilesIter()
	{
		var limVec = GetDirAdjustedDims(Dimensions, Dir);
		for (var y = 0; y < limVec.Y; ++y)
		{
			for (var x = 0; x < limVec.X; ++x)
			{
				yield return GetTileNonNull(new Vector2I(x, y));
			}
		}
	}

	#endregion // .ITileOwner Stuff
	#region Direction Related Methods

	/// <summary>
	/// Returns true if the unit has enough space for version of itself rotated 90 degrees clockwise or counter-clockwise.
	/// Rotating 180 degrees needs no check as it takes the same space as not rotating.
	/// </summary>
	public bool HasSpaceForRotate90()
	{
		if (m_Dims.X == m_Dims.Y)
		{
			return true;
		}

		Vector2I vec = m_Dims.X > m_Dims.Y ? new(0, m_Dims.Y) :  new(m_Dims.X, 0);
		for (; vec.Y < m_Dims.X; ++vec.Y)
		{
			for (; vec.X < m_Dims.Y; ++vec.X)
			{
				if (World.HasTileOn(m_GL + vec))
				{
					return false;
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Returns true if the unit can face that direction, by default units can only face east and west, but this
	/// behaviour can be changed by overriding this function.
	/// </summary>
	public virtual bool CanFaceDir(Direction dir) => dir == Direction.East || dir == Direction.West;

	public void SetDirection(Direction newDir)
	{
		Debug.Assert(Enum.IsDefined(newDir));
		if (newDir == m_Dir)
		{
			return;
		}
		else if (newDir == m_Dir.Invert())
		{
			// Checking the inverse direction first is crucial because most units can only face east
			// or west!
			Rotate180();
		}
		else if (newDir == m_Dir.Rotate90())
		{
			Rotate90();
		}
		else
		{
			Rotate270();
		}
	}

	private void SyncSpriteRotation()
	{
		switch (m_Dir) {
		case Direction.North: 
			Sprite.FlipH = false; 
			Sprite.Rotation = -0.5f * Mathf.Pi; 
			break;
		
		case Direction.East : 
			Sprite.FlipH = false; 
			Sprite.Rotation = 0f; 
			break;
		
		case Direction.South: 
			Sprite.FlipH = false; 
			Sprite.Rotation = +0.5f * Mathf.Pi; 
			break;
		
		case Direction.West: // special 
			Sprite.FlipH = true; 
			Sprite.Rotation = 0f;
			break;
		
		default:
			throw new NotImplementedException();
		}
	}

	public void Rotate180()
	{
		Debug.Assert(CanFaceDir(m_Dir.Invert()));

		if (IsFloating())
		{
			m_Dir = m_Dir.Invert();
			SyncSpriteRotation();
			return;
		}

		if (m_Dims == Vector2I.One)
		{
			GetTileNonNull(Vector2I.Zero).Rotate180();
			m_Dir = m_Dir.Invert();
			SyncSpriteRotation();
			return;
		}

		// (new.x, new.y) = (max_x - old.x, max_y - old.y)
		var maxGL = m_Dims - Vector2I.One;
		for (var y = 0; y < m_Dims.Y / 2; ++y) // Only go up to the center.
		{
			for (var x = 0; x < m_Dims.X; ++x)
			{
				var vec = new Vector2I(x, y);
				GetTileNonNull(vec).Rotate180();
				GetTileNonNull(maxGL - vec).Rotate180();
				World.SwapTiles(m_GL + vec, m_GL + (maxGL - vec));
			}
		}

		m_Dir = m_Dir.Invert();
		SyncSpriteRotation();
	}

	/// <summary>
	/// Rotates the unit by 90 degrees clockwise, by default this operation is not supported, but this can be changed
	/// by overriding <see cref="CanFaceDir"/>.
	/// </summary>
	public void Rotate90()
	{
		Debug.Assert(CanFaceDir(m_Dir.Rotate90()));
		Debug.Assert(HasSpaceForRotate90());

		Sprite.Rotate(0.5f * Mathf.Pi);

		if (IsFloating())
		{
			m_Dir = m_Dir.Rotate90();
			SyncSpriteRotation();
			return;
		}

		if (m_Dims == Vector2I.One)
		{
			GetTileNonNull(Vector2I.Zero).Rotate90();
			m_Dir = m_Dir.Rotate90();
			SyncSpriteRotation();
			return;
		}

		// Collect all the tiles and remove them from the world.
		var floatyTiles = new Tile[m_Dims.X * m_Dims.Y];
		for (var y = 0; y < m_Dims.Y; ++y)
		{
			for (var x = 0; x < m_Dims.X; ++x)
			{
				floatyTiles[y * m_Dims.X + x] = World.ExtractTile(m_GL + new Vector2I(x, y))!;
			}
		}

		// (new.x, new.y) = (max_y - old.y, old.x)
		var maxY = m_Dims.Y - 1;
		for (var i = 0; i < floatyTiles.Length; ++i)
		{
			var tl = floatyTiles[i];
			tl.Rotate90();
			var x = i % m_Dims.X;
			var y = i / m_Dims.X;
			var mygloc = m_GL + new Vector2I(maxY - y, x);
			World.InstallTile(tl, mygloc);
		}

		m_Dims = new(m_Dims.Y, m_Dims.X);
		m_Dir = m_Dir.Rotate90();
		SyncSpriteRotation();
	}

	/// <summary>
	/// Rotates the unit by 270 degrees clockwise (90 degress counter-clockwise), by default this operation is not
	/// supported, but this can be changed by overriding <see cref="CanFaceDir"/>.
	/// </summary>
	public void Rotate270()
	{
		Rotate180();
		Rotate90();
	}


	#endregion Direction Related Methods
	#region Building Methods

	/// <summary>
	/// Returns the adjusted tile.
	/// Input tiles let items slide in from a specified direction.
	/// </summary>
	/// <param name="bKeepTrack">
	/// All tiles kept track of must be filled before the tick counter starts counting for on-demand units.
	/// </param>
	public Tile AddInput(Vector2I gloc, Direction dir, bool bKeepTrack = false)
	{
		Debug.Assert(IsWithin(m_GL + gloc));
		Debug.Assert(GetTileNonNull(gloc).IsSolid(dir));
		Debug.Assert(m_TickType == TickType.OnDemand || !bKeepTrack);

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(m_GL + gloc + dir.ToGrid()));

		var tl = GetTileNonNull(gloc);
		tl.MakeInput(dir, true);
		if (bKeepTrack)
		{
			// For now dubs are not allowed.
			Debug.Assert(!m_TickTiles.Any(tl => tl.GLocation == m_GL + gloc));
			m_TickTiles.Add(tl);
		}
		return tl;
	}

	/// <summary>
	/// Returns the adjusted tile.
	/// Output tiles are meant to generate tiles and slide them out in a specified direction.
	/// </summary>
	/// <param name="bKeepTrack">
	/// All tiles kept track of must be filled before the tick counter starts counting for on-demand units.
	/// </param>
	public Tile AddOutput(Vector2I gloc, Direction dir, bool bKeepTrack = false)
	{
		Debug.Assert(IsWithin(m_GL + gloc));
		Debug.Assert(GetTileNonNull(gloc).IsSolid(dir));
		Debug.Assert(m_TickType == TickType.OnDemand || !bKeepTrack);

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(m_GL + gloc + dir.ToGrid()));

		var tl = GetTileNonNull(gloc);
		tl.MakeOutput(dir, true);
		if (bKeepTrack)
		{
			// For now dubs are not allowed.
			Debug.Assert(!m_TickTiles.Contains(tl));
			m_TickTiles.Add(tl);
		}
		return tl;
	}


	#endregion // Building Methods
	#region Simulation

	/// <summary>
	/// This HAS to be called before <see cref="PendNewCommands"/>.
	/// </summary>
	public void PreprocessTick()
	{
		if (m_bLocked)
		{
			m_Count = 0;
		}
		else switch (m_TickType) {
		case TickType.Steady:
			m_Count += 1;
			break;

		case TickType.OnDemand:
			if (IsWorkTick()) // Checking last tick btw.
			{
				// Reset after work tick, IsWorkTick always returns false for 0; this forces a one tick break.
				m_Count = 0;
			}

			if (AreAllTickTilesFilled())
			{
				m_Count += 1;
			}
			else
			{
				m_Count = 0;
			}
			break;

		default:
			throw new NotImplementedException("New tick type?");
		}

		m_PendingCmds.RemoveAll(c => c.IsDone());
	}

	private bool AreAllTickTilesFilled()
	{
		return m_TickTiles.Count > 0 && m_TickTiles.All(tl => tl.HasItem() && !tl.Item.IsMidAnimation());
	}


	public void HandlePendingNewCommands(Simulator.PendProxy pender)
	{
		if (IsWorkTick())
		{
			PendNewCommands(pender);
		}
	}

	/// <summary>
	/// Pauses the tick counter for this tick.
	/// </summary>
	public void PauseThisTick()
	{
		m_Count -= 1;
	}

	public void AddCommandsToWaitList(IEnumerable<IEnumerable<Command>> sequence)
	{
		Debug.Assert(sequence.SelectMany(c => c).All(c => !m_PendingCmds.Contains(c)));
		m_PendingCmds.AddRange(sequence.SelectMany(c => c));
	}

	public void CancelPendingCmds()
	{
		foreach (var cmd in m_PendingCmds)
		{
			cmd.ForceFinish();
		}
		m_PendingCmds.Clear();
	}

	/// <summary>
	/// This only resets the unit state, it does not destroy any items located within the unit. <br/>
	/// <b>Don't forget to override this function whenever you have extra fields the need to be reset
	/// when the simulation is restarted!</b>
	/// </summary>
	public virtual void Reset()
	{
		m_Count = 1;
		CancelPendingCmds();
		DidAReset?.Invoke();
	}

	protected void InvokeEventTriggerHappened() => TriggerHappened?.Invoke();

	#endregion // Simulation
	#endregion // Public Interface

	protected enum TickType
	{
		Steady,   // Always counts ticks, even when no item is fed, like sliders.
		OnDemand, // Counts ticks only when all item slots are filled, like updaters.
	}
}