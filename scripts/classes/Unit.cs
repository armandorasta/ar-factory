using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace ArFactory;

public abstract partial class Unit : Node2D
{
	protected enum TickType
	{
		Steady,   // Always counts ticks, even when no item is fed, like sliders.
		OnDemand, // Counts ticks only when all item slots are filled, like updaters.
	}


	// Nodes
	public Sprite2D Sprite;


	// Public Interface
	public Vector2I GLocation => m_GL;
	public Direction Dir => m_Dir;
	public Vector2I Dimensions => m_Dims;
	public int Rate => m_Rate;
	public WorldPanel World { get; private set; }


	// Privates
	private TickType m_TickType;
	private Vector2I m_GL;
	private Direction m_Dir = Direction.East;
	private Vector2I m_Dims;
	private int m_Rate;
	private List<Command> m_PendingCmds = [];

	/// <summary>
	/// Keeps track of <see cref="Rate"/> every tick.
	/// </summary>
	private int m_Count = 1;

	/// <summary>
	/// These are checked by the on-demand units
	/// </summary>
	private List<Tile> m_TickTiles = [];

	
	public static bool CanFitIn(WorldPanel world, Vector2I gloc, Vector2I dims, Direction dir)
	{
		var bLeftRight = dir == Direction.East || dir == Direction.West;
		var adjustedDims = bLeftRight ? dims : new(dims.Y, dims.X);
		if (!world.GetGridRect().Encloses(new(gloc, adjustedDims)))
		{
			return false;
		}
		for (int y = 0; y < adjustedDims.Y; ++y)
		{
			for (int x = 0; x < adjustedDims.X; ++x)
			{
				if (world.HasTile(gloc + new Vector2I(x, y)))
				{
					return false;
				}
			}
		}
		return true;
	}


	public override void _Ready()
	{
		this.Sprite = GetNodeOrNull<Sprite2D>("Sprite2D");
	}

	// This function is called on the `_Ready` function of sub-classes.
	protected void BaseInit(
		WorldPanel world, TickType tickType, int workRate, Vector2I gloc, Vector2I dims, Direction dir)
	{
		Debug.Assert(Unit.CanFitIn(world, gloc, dims, dir));
		Debug.Assert(CanFaceDir(dir));
		Debug.Assert(workRate > 0);
		Debug.Assert(Enum.IsDefined(tickType));

		this.World = world;
		this.m_Rate = workRate;
		this.m_GL = gloc;
		this.m_Dims = dims;
		this.m_TickType = tickType;

		Position = world.GridToPos(gloc);

		// TODO: Add back sprite code
		Sprite?.Hide();
		// Sprite.ApplyScale(World.CellWidth/128.0f * (Vector2)Dims);
		// Sprite.Translate(0.5f*World.CellWidth * Vector2.One);

		for (var y = 0; y < dims.Y; ++y)
		{
			for (var x = 0; x < dims.X; ++x)
			{
				var myLoc = gloc + new Vector2I(x, y);
				world.InstallTile(Tile.CreateSolid(), myLoc);
			}
		}

		BuildTilesAsIfFacingEast();
		Debug.Assert(tickType != TickType.OnDemand || m_TickTiles.Count > 0);

		SetDir(dir);
	}

	protected void InjectorBaseInit(WorldPanel world)
	{
		World = world;
		m_TickType = TickType.Steady;
		m_Rate = 1;
		// Position = world.GridToPos(m_Dims);
	}

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
	public abstract void PendNewCommands(WorldPanel.PendProxy pender);


	#endregion // Abstract Interface
	#region Public Interface

	public override string ToString()
	{
		var bui = new StringBuilder("Unit[");
		bui.Append(string.Join(", ", [
			GLocation, 
			$"{m_Dims.X}x{m_Dims.Y}",
			m_Dir,
			$"{m_Rate}t/s",
		]));
		return bui.Append(']').ToString();
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
	public Tile GetTile(Vector2I gloc) => World.GetTile(m_GL + gloc);

	/// <summary>
	/// Returns true if the machine is expected to operate during this tick, <see cref="m_Rate"/> determines how often 
	/// this function returns true, ex. when <see cref="m_Rate"/> is 1, it returns true every single tick.
	/// </summary>
	private bool IsWorkTick() => m_Count % m_Rate == 0;

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
	protected bool IsSafeToPendCmds() 
		=> !HasPendingCmds() || m_PendingCmds.All(c => c is CmdSlide sc && sc.IsAwaitingAnim());

	
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
				if (World.HasTile(m_GL + vec))
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
	/// TODO: See this `static` business...
	public virtual bool CanFaceDir(Direction dir) => dir == Direction.East || dir == Direction.West;

	public void SetDir(Direction newDir)
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

	public void Rotate180()
	{
		Debug.Assert(CanFaceDir(m_Dir.Invert()));

		if (m_Dims == Vector2I.One)
		{
			GetTile(Vector2I.Zero).Rotate180();
			m_Dir = m_Dir.Invert();
			return;
		}

		// (new.x, new.y) = (max_x - old.x, max_y - old.y)
		var maxGLoc = m_Dims - Vector2I.One;
		for (var y = 0; y < m_Dims.Y / 2; ++y) // Only go up to the center.
		{
			for (var x = 0; x < m_Dims.X; ++x)
			{
				var vec = new Vector2I(x, y);
				GetTile(vec).Rotate180();
				GetTile(maxGLoc - vec).Rotate180();
				World.SwapTiles(m_GL + vec, m_GL + (maxGLoc - vec));
			}
		}
		m_Dir = m_Dir.Invert();
	}

	/// <summary>
	/// Rotates the unit by 90 degrees clockwise, by default this operation is not supported, but this can be changed 
	/// by overriding <see cref="CanFaceDir"/>.
	/// </summary>
	public void Rotate90()
	{
		Debug.Assert(CanFaceDir(m_Dir.Rotate90()));
		Debug.Assert(HasSpaceForRotate90());

		if (m_Dims == Vector2I.One)
		{
			GetTile(Vector2I.Zero).Rotate90();
			m_Dir = m_Dir.Rotate90();
			return;
		}


		// Collect all the tiles and remove them from the world.
		var floatyTiles = new Tile[m_Dims.X * m_Dims.Y];
		for (var y = 0; y < m_Dims.Y; ++y)
		{
			for (var x = 0; x < m_Dims.X; ++x)
			{
				floatyTiles[y * m_Dims.X + x] = World.ExtractTile(m_GL + new Vector2I(x, y));
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
			World.InstallTile(tl, m_GL + new Vector2I(maxY - y, x));
		}

		m_Dims = new(m_Dims.Y, m_Dims.X);
		m_Dir = m_Dir.Rotate90();
	}

	/// <summary>
	/// Rotates the unit by 270 degrees clockwise (90 degress counter-clockwise), by default this operation is not 
	/// supported, but this can be changed by overriding <see cref="CanFaceDir"/>.
	/// </summary>
	public void Rotate270()
	{
		if (m_Dims == Vector2I.One)
		{
			Debug.Assert(CanFaceDir(m_Dir.Rotate270()));
			GetTile(Vector2I.Zero).Rotate270();
			m_Dir = m_Dir.Rotate270();
			return;
		}

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
		Debug.Assert(GetTile(gloc).IsSolid(dir));
		Debug.Assert(dir != Direction.East);

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(m_GL + gloc + dir.ToGrid()));

		Tile tl = World.GetTile(m_GL + gloc);
		tl.MakeInput(dir, true);
		if (bKeepTrack)
		{
			// For now I dubs are not allowed.
			Debug.Assert(!m_TickTiles.Contains(tl));
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
		Debug.Assert(GetTile(gloc).IsSolid(dir));
		Debug.Assert(dir != Direction.West);

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(m_GL + gloc + dir.ToGrid()));

		Tile tl = World.GetTile(m_GL + gloc);
		tl.MakeOutput(dir, true);
		if (bKeepTrack)
		{
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
		m_Count = m_TickType switch 
		{
			TickType.Steady => m_Count + 1,
			TickType.OnDemand => m_TickTiles.Any(tl => !tl.HasItem()) ? 0 : m_Count + 1,
			_ => throw new NotImplementedException(),
		};

		m_PendingCmds = m_PendingCmds.FindAll(c => !c.IsDone());
	}

	public void HandlePendingNewCommands(WorldPanel.PendProxy pender)
	{
		if (IsWorkTick())
		{
			if (!IsSafeToPendCmds())
			{
				PauseThisTick(); // So next tick is also work tick.
				return;
			}

			pender.AssignWaitList(m_PendingCmds);
			PendNewCommands(pender);
			pender.AssignWaitList(null);
		}
	}

	/// <summary>
	/// Pauses the tick counter for this tick.
	/// </summary>
	public void PauseThisTick()
	{
		m_Count -= 1;
	}

	/// <summary>
	/// This only resets the unit state, it does not destroy any items located within the unit. <br/>
	/// <b>Don't forget to override this function whenever you have extra fields the need to be reset
	/// when the simulation is restarted!</b>
	/// </summary>
	public virtual void Reset()
	{
		m_Count = 1;
		m_PendingCmds.Clear();
	}


	#endregion // Simulation
	#endregion // Public Interface
}
