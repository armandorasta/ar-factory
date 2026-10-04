using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	/// <summary>
	/// A blocking command is a command that can wait for condition to be met before proceeding,
	/// like for example a blocking spawn will wait unit target tile is empty, then proceed, then
	/// mark itself as finished. <br/>
	/// A non-blocking command will terminate the moment it's unable to proceed.
	/// </summary>
	private bool m_bBlocking = true;
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
	/// <summary>
	/// Used for running sub-commands
	/// </summary>
	protected CommandRunner m_SubCmdRunner;


	public Command(int tickCount, bool bSmooth = false)
	{
		Debug.Assert(0 <= tickCount && tickCount <= MaxTickCount);
		m_Ticks = tickCount;
		m_bSmooth = bSmooth;
	}

	/// <summary>
	/// Executed at the very beginning of every tick.
	/// </summary>
	protected abstract void OnTick(Level lv);
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
	public bool IsBlocking() => m_bBlocking;

	/// <summary>
	/// Checks if the command is done based on the tick counter. If you are gonna increment and check, 
	/// use <see cref="CountAndCheckIfDone"/>. <br/>
	/// </summary>
	public bool IsDone() => IsDoneNoSubCmds();
	/// <summary>
	/// Checks if the command is about to be deleted if it doesn't block this tick.
	/// </summary>
	public bool IsLastTick() => IsLastTickNoSubCmds();
	public bool HasBlockedThisTick() => m_bBlockedThisTick;
	public bool HasFailedThisTick() => !IsBlocking() && HasBlockedThisTick();

	public override string ToString()
	{
		var blockStr = !m_bBlocking ? ", nonblk" : "";
		return $"Cmd[{m_Count}/{TickCount} ticks{blockStr}]";
	}


	/// <summary>
	/// Makes the command terminate immediately when it can't proceed. <br/>
	/// <b>Can be overriden to for example disable this ability</b>.
	/// </summary>
	/// <returns>
	/// The current command for convenience such as chaining it with <see langword="new"/>
	/// </returns>
	public virtual Command MakeNonBlocking()
	{
		// Debug.Assert(m_bBlocking); // Because of CommandRunner.
		m_bBlocking = false;
		m_SubCmdRunner?.MakeNonBlocking();
		return this;
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
		if (m_bBlocking)
		{
			m_Count -= 1;
		}
		else
		{
			ForceFinish();
		}
	}

	/// <summary>
	/// So far only used to pause until sub-commands finish executing... Really what I should be 
	/// doing is finding the number of ticks that includes sub-commands and stuff, but meh... Already
	/// wasted an entire day on nothing, that's more than enough.
	/// </summary>
	private void BlockForSubCmds()
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
	public void HandleOnTick(Level lv)
	{
		if (HandleSubCmdsOnTickAndCheck(lv))
		{
			OnTick(lv);
			HandleSubCmdsOnTickAndCheck(lv);
		}
	}

	/// <summary>
	/// Should be called inside a <see cref="OnEdge"/> function of some sort when handling commands 
	/// manually. Generally speaking tho, you should be using <see cref="CommandRunner"/> for that.
	/// </summary>
	public void HandleOnEdge(Level lv)
	{
		Debug.Assert(m_bSmooth);
		if (HandleSubCmdsOnEdgeAndCheck(lv) && !m_bBlockedThisTick)
		{ // Order is important! OnEdge for sub-commands must run even if the parent command blocks.
			OnEdge(lv);
			HandleSubCmdsOnEdgeAndCheck(lv);
		}
	}

	private bool HandleSubCmdsOnTickAndCheck(Level lv)
	{
		Debug.Assert(!IsDoneNoSubCmds());
		return m_SubCmdRunner is null || HandleSubCmdsPostCheck(m_SubCmdRunner.OnTick(lv));
	}

	private bool HandleSubCmdsOnEdgeAndCheck(Level lv)
	{
		Debug.Assert(!IsDoneNoSubCmds());
		return m_SubCmdRunner is null || HandleSubCmdsPostCheck(m_SubCmdRunner.OnEdge(lv));
	}

	private bool HandleSubCmdsPostCheck(CommandRunner.Result res)
	{
		switch (res) {
		case CommandRunner.Result.NotDoneYet:
			BlockForSubCmds();
			return false;
		
		case CommandRunner.Result.DoneWithFailure:
			// Sub-runners can only fail in non-blocking commands, so we pause to fail the this parent command as
			// well and keep it propagating.
			PauseThisTick();
			return false;
		
		case CommandRunner.Result.DoneSuccessfully:
			return true;
		
		default:
			throw new NotImplementedException();
		}

	}

	public void HandleFrame(double dt, Level lv)
	{
		if (m_SubCmdRunner != null && m_SubCmdRunner.DoPerFrame(dt, lv) == CommandRunner.Result.NotDoneYet)
		{
			return;
		}

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
	
	/// <summary>
	/// Adds sub-commands that will be executed in parallel before the command itself, every call
	/// to this function adds a parallel set, each set will executed after the other finishes.
	/// </summary>
	protected void AddSubParallelCmds(IEnumerable<Command> newCmds)
	{
		// This should be impossible, pending commands on the last tick of a command will pause it
		// automatically.
		Debug.Assert(!IsDoneNoSubCmds());
		Debug.Assert(newCmds.Any(_ => true)); // Any returns false for empty sequences.

		var adjustedCmds = newCmds.Select(c => m_bBlocking ? c : c.MakeNonBlocking());
		if (m_SubCmdRunner is null)
		{
			m_SubCmdRunner = new([adjustedCmds]);
			if (!m_bBlocking)
			{
				m_SubCmdRunner.MakeNonBlocking();	
			}
		}
		else
		{
			m_SubCmdRunner.PendParallel(adjustedCmds);
		}
	}


	/// <summary>
	/// Checks if the command is done without considering sub-commands.
	/// Usually the command waits for sub-commands first, then finishes its work, but sometimes some
	/// commands pend commands after they are done and wait for these to finish such as
	/// <see cref="CmdIf"/> when the condition is met.
	/// </summary>
	private bool IsDoneNoSubCmds() => m_Count >= m_Ticks;
	/// <summary>
	/// Checks if the command is to be done next tick without considering sub-commands.
	/// </summary>
	private bool IsLastTickNoSubCmds() => m_Count < m_Ticks;
}
