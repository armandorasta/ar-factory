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
	public WorldPanel World { get; private set; }
	public Vector2I GridLoc { get; private set; }
	public int Rate { get; private set; }
	public Vector2I Dims { get; private set; }
	public Direction Dir { get; private set; } = Direction.East;


	// Privates
	private TickType m_TickType;

	// Keeps track of `Rate` every tick.
	private int m_Count = 0;

	//  Commands that need to finish for the unit to keep operating again
	private List<Command> m_PendingCmds = [];

	// These are checked by the `ON_DEMAND` units
	// TODO: Add a way to add blocks to this list explicitly.
	private List<TlHolder> m_HoldingTiles = [];

	
	public static bool CanFitIn(WorldPanel world, Vector2I gloc, Vector2I dims)
	{
		if (!world.GetRectGrid().Encloses(new Rect2I(gloc, dims)))
		{
			return false;
		}


		for (var y = 0; y < dims.Y; ++y)
		{
			for (var x = 0; x < dims.X; ++x)
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
		if (HasNode("Sprite2D"))
		{
			this.Sprite = GetNode<Sprite2D>("Sprite2D");
		}
	}

	// This function is called on the `_Ready` function of sub-classes.
	protected void BaseInit(WorldPanel world, TickType tickType, int workRate, Vector2I gloc,
		Vector2I dims, Direction dir)
	{
		Debug.Assert(Unit.CanFitIn(world, gloc, dims));
		Debug.Assert(CanFaceDir(dir));

		World = world;
		Rate = workRate;
		GridLoc = gloc;
		Dims = dims;
		m_TickType = tickType;

		Position = world.GridToPos(gloc);

		// TODO: Add back sprite code
		Sprite.Hide();
		// Sprite.ApplyScale(World.CellWidth/128.0f * (Vector2)Dims);
		// Sprite.Translate(0.5f*World.CellWidth * Vector2.One);

		for (var y = 0; y < dims.Y; ++y)
		{
			for (var x = 0; x < dims.X; ++x)
			{
				var myLoc = gloc + new Vector2I(x, y);
				world.InstallTile(new TlSolid(myLoc));
			}
		}

		BuildTiles();
		SetDir(dir);
	}

	protected void InjectorBaseInit(WorldPanel world)
	{
		World = world;
		m_TickType = TickType.Steady;
		Position = world.GridToPos(Dims);
	}

	#region Abstract Interface
	
	/// <summary>
	/// Used to specify:
	/// what tiles the unit will occupy, 
	/// where the inputs and outputs are and their directions.
	/// <b>The tiles should be built as if the unit is facing east.</b>
	/// </summary>
	protected abstract void BuildTiles();

	/// <summary>
 	/// Used to add commands to the queue using `pend_cmd`.
	/// Checks like <see cref="IsWorkTick"/>, <see cref="HasPendingCmds"/> and 
	/// <see cref="IsJustAwaitingOutSlideAnim"/> are used quite often here.
	/// </summary>
	public abstract void PendNewCommands();


	#endregion // Abstract Interface
	#region Public Interface

	public override string ToString()
	{
		var bui = new StringBuilder("Unit[");
		bui.Append(string.Join(", ", [
			GridLoc, 
			$"{Dims.X}x{Dims.Y}",
			Dir,
			$"{Rate}t/s",
		]));
		return bui.Append(']').ToString();
	}

	/// <summary>
	/// Returns enclosing Rect2I in grid-space.
	/// </summary>
	public Rect2I GetRect2I() => new(GridLoc, Dims);
	
	/// <summary>
	/// Returns enclosing Rect2 in world-space.
	/// </summary>
	public Rect2 GetRect2() 
		=> new(World.CellWidth * (Vector2)GridLoc, World.CellWidth * (Vector2)Dims);
	
	/// <summary>
	/// Checks if <paramref name="gloc"/> is within the world in grid-space.
	/// </summary>
	public bool IsWithin(Vector2I gloc) => GetRect2I().HasPoint(gloc);
	public Tile GetTile(Vector2I gloc) => World.GetTile(GridLoc + gloc);
	public T GetTile<T>(Vector2I gloc) where T : Tile => (T)GetTile(gloc);

	/// <summary>
	/// Returns true if the machine is expected to operate during this tick, <see cref="Rate"/> 
	/// determines how 
	/// often this function returns true, ex. when <see cref="Rate"/> is 1, it returns true every single tick.
	/// </summary>
	public bool IsWorkTick() => m_Count > 0 && m_Count % Rate == 0;

	/// <summary>
	/// Returns true if the command queue still has commands to be executed. You usually pend all the 
	/// commands you want executed at once when the queue is empty, then wait until the whole sequence
	/// is executed.
	/// </summary>
	/// <returns></returns>
	public bool HasPendingCmds() => m_PendingCmds.Count > 0;

	/// <summary>
	/// Called everytime on the tick pending commands queue size goes to zero.
	/// </summary>
	protected virtual void OnFinishingAllPendingCmds() { }

	/// <summary>
	/// Returns true if we are waiting for the animation of a slide command to end, that slide command
	/// is on one of the outputs of the unit.
	/// In this case, we can often just pend new commands before the end of the animation on the next
	/// tick.
	/// If there are no pending commands, it will return false!
	/// </summary>
	public bool IsJustAwaitingOutSlideAnim() // TODO: Make this function only check output tiles somehow!
		=> m_PendingCmds.Count > 0 && m_PendingCmds.All((c) => c is CmdSlide sc && sc.IsAwaitingAnim());

	
	#region Direction Related Methods

	/// <summary>
	/// Returns true if the unit has enough space for version of itself rotated 90 degrees clockwise or 
	/// counter-clockwise.
	/// Rotating 180 degrees needs no check as it takes the same space as not rotating.
	/// </summary>
	public bool HasSpaceForRotate90()
	{
		if (Dims.X == Dims.Y)
		{
			return true;
		}

		Vector2I vec = Dims.X > Dims.Y ? new(0, Dims.Y) :  new(Dims.X, 0);
		for (; vec.Y < Dims.X; ++vec.Y)
		{
			for (; vec.X < Dims.Y; ++vec.X)
			{
				if (World.HasTile(GridLoc + vec))
				{
					return false;
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Returns true if the unit can face that direction, by default units can only face east and west,
	/// but this behaviour can be changed by overriding this function.
	/// </summary>
	/// TODO: See this `static` business...
	public virtual bool CanFaceDir(Direction dir) => dir == Direction.East || dir == Direction.West;

	public void SetDir(Direction newDir)
	{
		Debug.Assert(Enum.IsDefined(newDir));
		if (newDir == Dir)
		{
			return;
		}
		else if (newDir == Dir.Invert())
		{
			// Checking the inverse direction first is crucial because most units can only face east
			// or west!
			Rotate180();
		}
		else if (newDir == Dir.Rotate90())
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
		Debug.Assert(CanFaceDir(Dir.Invert()));

		if (Dims == Vector2I.One)
		{
			GetTile(Vector2I.Zero).Rotate180();
			Dir = Dir.Invert();
			return;
		}

		// (new.x, new.y) = (max_x - old.x, max_y - old.y)
		var maxGLoc = Dims - Vector2I.One;
		for (var y = 0; y < Dims.Y / 2; ++y) // Only go up to the center.
		{
			for (var x = 0; x < Dims.X; ++x)
			{
				var vec = new Vector2I(x, y);
				GetTile(vec).Rotate180();
				GetTile(maxGLoc - vec).Rotate180();
				World.SwapTiles(GridLoc + vec, GridLoc + (maxGLoc - vec));
			}
		}
		Dir = Dir.Invert();
	}

	/// <summary>
	/// Rotates the unit by 90 degrees clockwise, by default this operation is not supported, but this 
	/// can be changed by overriding <see cref="CanFaceDir"/>.
	/// </summary>
	public void Rotate90()
	{
		Debug.Assert(CanFaceDir(Dir.Rotate90()));
		Debug.Assert(HasSpaceForRotate90());

		if (Dims == Vector2I.One)
		{
			GetTile(Vector2I.Zero).Rotate90();
			Dir = Dir.Rotate90();
			return;
		}


		// Collect all the tiles and remove them from the world.
		var floatyTiles = new Tile[Dims.X * Dims.Y];
		for (var y = 0; y < Dims.Y; ++y)
		{
			for (var x = 0; x < Dims.X; ++x)
			{
				floatyTiles[y * Dims.X + x] = World.ExtractTile(GridLoc + new Vector2I(x, y));
			}
		}

		// (new.x, new.y) = (max_y - old.y, old.x)
		var maxY = Dims.Y - 1;
		for (var i = 0; i < floatyTiles.Length; ++i)
		{
			var tl = floatyTiles[i];
			tl.Rotate90();
			var x = i % Dims.X;
			var y = i / Dims.X;
			tl.SetGridLocUnchecked(GridLoc + new Vector2I(maxY - y, x));
			World.InstallTile(tl);
		}

		Dims = new(Dims.Y, Dims.X);
		Dir = Dir.Rotate90();
	}

	/// <summary>
	/// Rotates the unit by 270 degrees clockwise (90 degress counter-clockwise), by default this 
	/// operation is not supported, but this can be changed by overriding <see cref="CanFaceDir"/>.
	/// </summary>
	public void Rotate270()
	{
		if (Dims == Vector2I.One)
		{
			Debug.Assert(CanFaceDir(Dir.Rotate270()));
			GetTile(Vector2I.Zero).Rotate270();
			Dir = Dir.Rotate270();
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
	public TlInput AddInput(Vector2I gloc, Direction dir)
	{
		Debug.Assert(IsWithin(GridLoc + gloc));
		Debug.AssertIs(GetTile(gloc), typeof(TlSolid));
		Debug.Assert(dir != Direction.East);

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(GridLoc + gloc + dir.ToGrid()));

		var newTile = new TlInput(GridLoc + gloc, dir);
		World.InstallTile(newTile, true);
		m_HoldingTiles.Add(newTile);
		return newTile;
	}

	/// <summary>
	/// Returns the adjusted tile. 
	/// Output tiles are meant to generate tiles and slide them out in a specified direction.
	/// </summary>
	public TlOutput AddOutput(Vector2I gloc, Direction dir)
	{
		Debug.Assert(IsWithin(GridLoc + gloc));
		Debug.AssertIs(GetTile(gloc), typeof(TlSolid));
		Debug.Assert(dir != Direction.West);

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(GridLoc + gloc + dir.ToGrid()));

		var newTile = new TlOutput(GridLoc + gloc, dir);
		World.InstallTile(newTile, true);
		m_HoldingTiles.Add(newTile);
		return newTile;
	}

	/// <summary>
	/// Returns the adjusted tile. 
	/// IO tiles are an input tile and output tile combined in one, they let items slide in in one 
	/// direction, and are meant to let them slide out in another, the 2 directions may not coincide.
	/// </summary>
	public TlIO AddIO(Vector2I gloc, Direction outDir, Direction inDir)
	{
		Debug.Assert(IsWithin(GridLoc + gloc));
		Debug.AssertIs(GetTile(gloc), typeof(TlSolid));
		Debug.Assert(outDir != inDir);
		Debug.Assert(outDir != Direction.West);
		Debug.Assert(inDir != Direction.East);

		// Both input and output must be facing out of the unit.
		Debug.Assert(!IsWithin(GridLoc + gloc + inDir.ToGrid()));
		Debug.Assert(!IsWithin(GridLoc + gloc + outDir.ToGrid()));

		var newTile = new TlIO(GridLoc + gloc, outDir, inDir);
		World.InstallTile(newTile, true);
		m_HoldingTiles.Add(newTile);
		return newTile;
	}

	/// <summary>
	/// Returns the adjusted tile. 
	/// Output tiles are meant to generate tiles and slide them out in a specified direction.
	/// Sliders are not considered in tick counting for on-demand units.
	/// </summary>
	public TlSlider AddSlider(Vector2I gloc, Direction dir)
	{
		Debug.Assert(IsWithin(GridLoc + gloc));
		Debug.AssertIs(GetTile(gloc), typeof(TlSolid));

		// Must be facing out of the unit.
		Debug.Assert(!IsWithin(GridLoc + gloc + dir.ToGrid()));

		// For now sliders can be placed anywhere, and can face any direction.
		// TODO: Assert that the slider's output and one input are connected.
		// I will need tests for that of course...

		var newTile = new TlSlider(GridLoc + gloc, dir);
		World.InstallTile(newTile, true);
		
		// Sliders are not considered in tick counting for on-demand units.
		// m_HoldingTiles.Add(newTile);
		
		return newTile;
	}

	/// <summary>
	/// Returns the adjusted tile. 
	/// Blackhole tiles allow in items from all directions.
	/// Blackholes are not considered in tick counting for on-demand units.
	/// </summary>
	public TlBlackhole AddBlackhole(Vector2I gloc)
	{
		Debug.Assert(IsWithin(GridLoc + gloc));
		Debug.AssertIs(GetTile(gloc), typeof(TlSolid));

		// At least one direction must be facing out.
		Debug.Assert(
			!IsWithin(GridLoc + gloc + Direction.North.ToGrid()) ||
			!IsWithin(GridLoc + gloc + Direction.East.ToGrid())  ||
			!IsWithin(GridLoc + gloc + Direction.South.ToGrid()) ||
			!IsWithin(GridLoc + gloc + Direction.West.ToGrid())   );

		var newTile = new TlBlackhole(GridLoc + gloc);
		World.InstallTile(newTile, true);
		
		// Blackholes are not considered in tick counting for on-demand units.
		// m_HoldingTiles.Add(newTile);
		
		return newTile;
	}


	#endregion // Building Methods
	#region Simulation

	/// <summary>
	/// This HAS to be called before <see cref="PendNewCommands"/>.
	/// </summary>
	public void PreprocessTick()
	{
		switch (m_TickType)
		{
			case TickType.Steady:
				m_Count += 1;
				break;
			
			case TickType.OnDemand:
				if (m_HoldingTiles.All((tl) => tl.HasItem()))
				{
					m_Count += 1;
				}
				else
				{
					m_Count = 0;
				}
				break;
			
			default:
				Debug.AssertUnreachable();
				break;
		}
	}

	/// <summary>
	/// Calls <see cref="Command.DoPerFrame"/> on all pending commands for now.
	/// </summary>
	public void DoPerFrame(double dt, Level lv)
	{
		Debug.AssertRefEq(lv.World, this.World);
		foreach (var cmd in m_PendingCmds)
		{
			cmd.DoPerFrame(dt, lv);
		}
	}

	/// <summary>
	/// Calls <see cref="Command.OnTick"/> on all pending commands.
	/// </summary>
	public void HandleCmdTick(Level lv)
	{
        Debug.AssertRefEq(lv.World, this.World);
        if (m_PendingCmds.Count == 0)
		{
			return;
		}

		// We have to execute all of them, because so of them are executed in different tiles in
		// parallel. We will move the resposbility of them not clashing to the commands themselves.
		foreach (var currCmd in m_PendingCmds)
		{
			currCmd.OnTick(lv);
		}

		// TODO: Fix this nonsense! I really don't like this zero tick command business, so solve
		// it a different way ffs!
		// Filtering below has to happen before counting ticks, otherwise things go wack.
		m_PendingCmds = m_PendingCmds.FindAll((c) => !c.IsDone());
		
		foreach (var cmd in m_PendingCmds)
		{
			cmd.CountThisTick();
		}

		if (m_PendingCmds.Count == 0)
		{
			OnFinishingAllPendingCmds();
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
	/// This only resets the unit state, it does not destroy any items located with the unit. <br/>
	/// <b>Don't forget to override this function whenever you have extra fields the need to be reset
	/// when the simulation is restarted!</b>
	/// </summary>
	public virtual void Reset()
	{
		m_Count = 0;
		m_PendingCmds.Clear();
	}

	/// <summary>
	/// Adds a command to the queue, when this function is called outside of `pend_new_cmds` the 
	/// behaviour is undefined.
	/// I wanted to add a flag and stuff, but meh...
	/// </summary>
	protected void PendCmd(Command newCmd)
	{
		m_PendingCmds.Add(newCmd);
	}

	/// <summary>
	/// Adds a sequence of commands to the queue, when this function is called outside of 
	/// `pend_new_cmds` the behaviour is undefined.
	/// </summary>
	protected void PendCmdSeq(IEnumerable<Command> cmdSeq)
	{
		m_PendingCmds.AddRange(cmdSeq);
	}


	#endregion // Simulation
	#endregion // Public Interface
}
