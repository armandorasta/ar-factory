using System;
using System.Collections.Generic;
using Godot;

namespace ArFactory;

public abstract class Command
{
	/// <summary>
	/// Maximum number of ticks a command can take without ever blocking.
	/// </summary>
	public const int MaxTickCount = int.MaxValue >> 2;

	
	/// <summary>
	/// Returns the number of ticks the command takes to finish executing without getting interrupted.
	/// Most commands take 0, 1 or 2 ticks. <br/>
	/// Commands that take 0 ticks execute instantly and don't block any other commands, commands that
	/// take 1 or more ticks block and push other commands to next ticks. <br/>
	/// For example 100 kill commands on the same tile will all execute in the same tick, while 2 slide 
	/// commands will take 2 ticks; one will block the other on the first tick. <br/>
	/// <b>Can be overriden by commands that change tick count dynamically</b>
	/// </summary>
	public virtual int TickCount
	{
		get => m_Ticks;
		protected set => m_Ticks = value;
	}


	/// <summary>
	/// Number of ticks it takes to finish.
	/// </summary>
	private int m_Ticks;
	private readonly bool m_bSmooth = false;

	/// <summary>
	/// Indicates the command has blocked this tick.
	/// </summary>
	private bool m_bBlockedThisTick = false;
	
	/// <summary>
	/// Keeps track of ticks passed for this command.
	/// Not that simple though, because sometimes the command has to wait for something and freezes,
	/// sometimes the command fails as well.
	/// </summary>
	protected int m_Count = 0;


	protected static bool HasTileWithItem(WorldPanel world, Vector2I gloc)
	{
		return world.HasTile(gloc) 
			&& world.GetTileNonNull(gloc).HasItem() 
			&& !world.GetTileNonNull(gloc).Item.IsMidAnimation();
	}

	protected static bool HasTileUnreserved(WorldPanel world, Vector2I gloc)
	{
		return world.HasTile(gloc) && !world.GetTileNonNull(gloc).IsReserved();
	}


	public Command(int tickCount, bool bSmooth = false)
	{
		Debug.Assert(0 <= tickCount && tickCount <= MaxTickCount);
		m_Ticks = tickCount;
		m_bSmooth = bSmooth;
	}

	/// <summary>
	/// Executed at the very beginning of every tick.
	/// </summary>
	protected abstract void OnTick(Level lv, CommandRunner.OnTickPender pender);
	/// <summary>
	/// Executed every frame. So far only used by slide...
	/// </summary>
	protected virtual void DoPerFrame(double dt, Level lv) {}
	/// <summary>
	/// Executed at the very end of every tick; used for commands that take up the whole tick.
	/// Must be implemented for smooth commands!
	/// </summary>
	protected virtual void OnEdge(Level lv) 
	{ 
		throw new NotImplementedException("Smooth commands must override OnEdge"); 
	}


	/// <summary>
	/// A smooth command is a command that takes up the entire tick rather than just instantly be done
	/// at the start of one.
	/// </summary>
	public bool IsSmooth() => m_bSmooth;

	/// <summary>
	/// Checks if the command is done based on the tick counter. If you are gonna increment and check, 
	/// use <see cref="CountAndCheckIfDone"/>. <br/>
	/// </summary>
	public bool IsDone() => m_Count >= m_Ticks;
	/// <summary>
	/// Checks if the command is about to be deleted if it doesn't block this tick.
	/// </summary>
	public bool IsLastTick() => m_Count == m_Ticks - 1;
	public bool HasBlockedThisTick() => m_bBlockedThisTick;

	public override string ToString()
	{
		return $"Cmd[{m_Count}/{TickCount} ticks]";
	}

	/// <summary>
	/// Increments the tick counter.
	/// </summary>
	public void CountThisTick() 
	{
		Debug.Assert(m_Count < m_Ticks);
		m_Count += 1;
		m_bBlockedThisTick = false;
	}

	/// <summary>
	/// Gaurantees the tick counter will be the same next tick, this should only be called once per tick!<br/>
	/// Calling this function in <see cref="OnTick"/> prevents <see cref="OnEdge"/> from executing in that tick.<br/>
	/// I made it crash the program in debug if called more than once in the same tick.
	/// </summary>
	public void PauseThisTick() // Probably should rename this damn thing to just Block...
	{
		Debug.Assert(!m_bBlockedThisTick);
		m_bBlockedThisTick = true;
		m_Count -= 1;
	}
	
	/// <summary>
	/// Forces the command to be deleted right after <see cref="OnTick"/> or <see cref="OnEdge"/>.
	/// </summary>
	public void ForceFinish() => m_Count = m_Ticks;

	/// <summary>
	/// Should be called inside a <see cref="OnTick"/> function of some sort when handling commands manually. 
	/// Generally speaking tho, you should be using <see cref="CommandRunner"/> for that.
	/// </summary>
	public void HandleOnTick(Level lv, CommandRunner.OnTickPender pender)
	{
		OnTick(lv, pender);
	}

	/// <summary>
	/// Should be called inside a <see cref="OnEdge"/> function of some sort when handling commands 
	/// manually. Generally speaking tho, you should be using <see cref="CommandRunner"/> for that.
	/// </summary>
	public void HandleOnEdge(Level lv)
	{
		Debug.Assert(m_bSmooth);
		OnEdge(lv);
	}

	public void HandleFrame(double dt, Level lv)
	{
		DoPerFrame(dt, lv);
	}

	/// <summary>
	/// Calls <see cref="CountThisTick"/> and <see cref="IsDone"/> in the correct order for checking.
	/// Use this if you are gonna increment the count and check if the command is done.
	/// </summary>
	public bool CountAndCheckIfDone()
	{
		CountThisTick();
		return IsDone();
	}
}
