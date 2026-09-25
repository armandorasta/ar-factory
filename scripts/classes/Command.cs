using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

public abstract class Command
{
	/// <summary>
	/// Returns the number of ticks the command takes to finish executing without getting interrupted.
	/// Most commands take 0, 1 or 2 ticks. <br/>
	/// Commands that take 0 ticks execute instantly and don't block any other commands, commands that
	/// take 1 or more ticks block and push other commands to next ticks. <br/>
	/// For example 100 kill commands on the same tile will all execute in the same tick, while 2 slide 
	/// commands will take 2 ticks; one will block the other on the first tick.
	/// </summary>
	public int TickCount => m_Ticks;

	/// <summary>
	/// Number of ticks it takes to finish.
	/// </summary>
	private readonly int m_Ticks;
	/// <summary>
	/// Keeps track of ticks passed for this command.
	/// Not that simple though, because sometimes the command has to wait for something and freezes.
	/// </summary>
	private int m_Count = 0;
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
	/// use <see cref="CountAndCheckIfDone"/>.
	/// </summary>
	public bool IsDone() => m_Count > m_Ticks;

	public override string ToString() => $"Cmd[{m_Count}/{m_Ticks} ticks]";

	/// <summary>
	/// Makes the command terminate immediately when it can't proceed.
	/// </summary>
	/// <returns>The current command for convenience such as chaining it with <see langword="new"/></returns>
	public Command MakeNonBlocking()
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
	/// I made crash the program in debug if called more than once in the same tick.
	/// </summary>
	public void PauseThisTick()
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
	/// Forces the command to be deleted this tick.
	/// </summary>
	public void ForceFinish() => m_Count = m_Ticks;

	public void HandleTick(Level lv)
	{
#if DEBUG
		m_bPausedThisTick = false;
#endif
		if (m_SubCmdRunner != null)
		{
			m_SubCmdRunner.OnTick(lv);
			if (!m_SubCmdRunner.IsDone())
			{
				PauseThisTick();
				return;
			}
		}

		OnTick(lv);
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
		if (m_SubCmdRunner is null)
		{
			m_SubCmdRunner = new([[.. newCmds]]);
		}
		else
		{
			m_SubCmdRunner.PendParallel(newCmds);
		}
	}
}
