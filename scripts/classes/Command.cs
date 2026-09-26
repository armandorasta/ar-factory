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
	public virtual int TickCount => m_Ticks;

	/// <summary>
	/// Number of ticks it takes to finish.
	/// </summary>
	protected int m_Ticks;
	/// <summary>
	/// Keeps track of ticks passed for this command.
	/// Not that simple though, because sometimes the command has to wait for something and freezes.
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

#if DEBUG
	/// <summary>
	/// Makes sure <see cref="PauseThisTick"/> is only called once per tick
	/// </summary>
	private bool m_bPausedThisTick = false;
#endif


	public Command(int tickCount)
	{
		Debug.Assert(tickCount >= 0);
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


	/// <summary>
	/// Checks if the command is done based on the tick counter. If you are gonna increment and check, 
	/// use <see cref="CountAndCheckIfDone"/>. <br/>
	/// </summary>
	public bool IsDone() => IsDoneNoSubCmds();
	/// <summary>
	/// Checks if the command is about to be deleted if it doesn't block this tick.
	/// </summary>
	public bool IsLastTick() => IsDoneNextTickNoSubCmds();
	/// <returns>
	/// <see langword="true"/> if the command blocked this tick
	/// </returns>
	public bool HasBlockedThisTick()
	{
		throw new NotImplementedException();	
	}

	public override string ToString() => $"Cmd[{m_Count}/{TickCount} ticks]";

	/// <summary>
	/// Makes the command terminate immediately when it can't proceed. <br/>
	/// <b>Can be overriden to for example disable this ability</b>.
	/// </summary>
	/// <returns>The current command for convenience such as chaining it with <see langword="new"/></returns>
	public virtual Command MakeNonBlocking()
	{
		Debug.Assert(m_bBlocking);
		m_bBlocking = false;
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
#if DEBUG
		Debug.Assert(!m_bPausedThisTick);
		m_bPausedThisTick = true;
#endif
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
	/// Forces the command to be deleted by the end of this tick.
	/// </summary>
	public void ForceFinish() => m_Count = m_Ticks;

	public void HandleTick(Level lv)
	{
#if DEBUG
		m_bPausedThisTick = false;
#endif
		if (!RunSubCmdsAndCheckIfFinished(lv))
		{
			return;
		}

		OnTick(lv);

		// Commands pended this tick will utilize this tick as well.
		if (!RunSubCmdsAndCheckIfFinished(lv))
		{
			return;
		}
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
		// Pending sub-commands after finishing is not supported because it causes some unnecessary
		// complications, you would have to do it manually if need that. Just use a runner...
		Debug.Assert(!IsDoneNoSubCmds());

		if (m_SubCmdRunner is null)
		{
			m_SubCmdRunner = new([[.. newCmds]]);
		}
		else
		{
			m_SubCmdRunner.PendParallel(newCmds);
		}
	}

	private bool RunSubCmdsAndCheckIfFinished(Level lv)
	{
		if (m_SubCmdRunner != null)
		{
			m_SubCmdRunner.OnTick(lv);
			if (!m_SubCmdRunner.IsDone())
			{
				PauseThisTick();
				return false;
			}

			Debug.Assert(!IsDoneNoSubCmds());
		}
		
		return true;
	}

	/// <summary>
	/// Checks if the command is done without considering sub-commands.
	/// Usually the command waits for sub-commands first, then finishes its work, but sometimes some
	/// commands pend commands after they are done and wait for these to finish such as
	/// <see cref="CmdIf"/> when the condition is met.
	/// </summary>
	private bool IsDoneNoSubCmds() => m_Count > m_Ticks;
	private bool IsDoneNextTickNoSubCmds() => m_Count == m_Ticks;
}
