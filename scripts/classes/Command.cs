using System;
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
	/// Code for failure of a non-blocking command.<br/>
	/// A non-blocking command fails when it encounters a unsatisfied condition such as needing an
	/// item but not finding one.
	/// </summary>
	private const int s_CodeCmdFailed = -5;

	
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
	/// Keeps track of ticks passed for this command.
	/// Not that simple though, because sometimes the command has to wait for something and freezes,
	/// sometimes the command fails as well.
	/// </summary>
	protected int m_Count = 0;
	/// <summary>
	/// Used for running sub-commands
	/// </summary>
	private CommandRunner m_SubCmdRunner;
	/// <summary>
	/// A blocking command is a command that can wait for condition to be met before proceeding,
	/// like for example a blocking spawn will wait unit target tile is empty, then proceed, then
	/// mark itself as finished. <br/>
	/// A non-blocking command will terminate the moment it's unable to proceed.
	/// </summary>
	private bool m_bBlocking = true;

	/// <summary>
	/// Indicates the command has blocked this tick.
	/// </summary>
	private bool m_bBlockedThisTick = false;


	public Command(int tickCount)
	{
		Debug.Assert(0 <= tickCount && tickCount <= MaxTickCount);
		m_Ticks = tickCount;
	}

	/// <summary>
	/// Executed every tick.
	/// </summary>
	protected abstract void OnTick(Level lv);
	/// <summary>
	/// Executed every frame. So far only used by slide...
	/// </summary>
	protected virtual void DoPerFrame(double dt, Level lv) {}


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
		Debug.Assert(m_Count <= m_Ticks);
		m_Count += 1;
	}

	/// <summary>
	/// Gaurantees the tick counter will be the same next tick, this should only be called once per
	/// tick!<br/>
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
	private void PauseEvenForNonBlocking()
	{
		Debug.Assert(!m_bBlockedThisTick);
		m_bBlockedThisTick = true;
		m_Count -= 1;
	} 
	
	/// <summary>
	/// Forces the command to be deleted by the end of this tick.
	/// </summary>
	public void ForceFinish() => m_Count = m_Ticks + 1;

	/// <summary>
	/// Should be called inside a <c>OnTick</c> function of some sort when handling commands manually.
	/// Generally speaking tho, you should be using <see cref="CommandRunner"/> for that.
	/// </summary>
	public void HandleTick(Level lv)
	{
		m_bBlockedThisTick = false;
		if (!HandleSubCmdsOnTickAndCheckIfDone(lv))
		{
			return;
		}

		OnTick(lv);

		// Commands pended this tick will utilize this tick as well.
		HandleSubCmdsOnTickAndCheckIfDone(lv);
	}

	public void HandleFrame(double dt, Level lv)
	{
		if (m_SubCmdRunner != null && !m_SubCmdRunner.IsDone())
		{
			m_SubCmdRunner.DoPerFrame(dt, lv);
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

	private bool HandleSubCmdsOnTickAndCheckIfDone(Level lv)
	{
		Debug.Assert(!IsDoneNoSubCmds());

		if (m_SubCmdRunner is null)
		{
			return true;
		}

		m_SubCmdRunner.OnTick(lv);
		
		if (m_SubCmdRunner.IsDoneSuccessfully())
		{
			return true;
		}

		if (m_SubCmdRunner.IsDoneWithFailure())
		{
			PauseThisTick();
			return false;
		}
		
		PauseEvenForNonBlocking();
		return false;
	}


	/// <summary>
	/// Checks if the command is done without considering sub-commands.
	/// Usually the command waits for sub-commands first, then finishes its work, but sometimes some
	/// commands pend commands after they are done and wait for these to finish such as
	/// <see cref="CmdIf"/> when the condition is met.
	/// </summary>
	private bool IsDoneNoSubCmds() => m_Count > m_Ticks;
	/// <summary>
	/// Checks if the command is to be done next tick without considering sub-commands.
	/// </summary>
	private bool IsLastTickNoSubCmds() => m_Count == m_Ticks;
}
