using Godot;
using System;

namespace ArFactory;

public abstract partial class Command : RefCounted
{
	// Number of _Ticks it takes to finish.
	private int m_Ticks;

	/// Keeps track of _Ticks passed for this command.
	/// Not that simple though, because sometimes the command has to wait for something and freezes.
	private int m_Count = 0;

	public Command(int tickCount)
	{
		Debug.Assert(tickCount >= 0);
		m_Ticks = tickCount;
	}

	public abstract void OnTick(Level lv);
	
	public virtual void DoPerFrame(double dt, Level lv) {}

	/// <summary>
	/// Returns the number of ticks the command takes to finish executing without getting interrupted.
	/// Most commands take 0, 1 or 2 ticks. <br/>
	/// Commands that take 0 ticks execute instantly and don't block any other commands, commands that
	/// take 1 or more ticks block and push other commands to next ticks. <br/>
	/// For example 100 kill commands on the same tile will all execute in the same tick, while 2 slide 
	/// commands will take 2 ticks; one will block the other on the first tick.
	/// </summary>
	public int GetTickCount() => m_Ticks;

	/// <summary>
	/// Increments the tick counter.
	/// </summary>
	public void CountThisTick() => m_Count += 1;
	/// <summary>
	/// Gaurantees the tick counter will be the same next tick, this should only be called once per
	/// tick!
	/// </summary>
	public void PauseThisTick() => m_Count -= 1;
	/// <summary>
	/// Checks if the command is done based on the tick counter. If you are gonna increment and check, 
	/// use <see cref="CountAndCheckIfDone"/>.
	/// </summary>
	public bool IsDone() => m_Count > m_Ticks;
	
	/// <summary>
	/// Calls <see cref="CountThisTick"/> and <see cref="IsDone"/> in the correct order for checking.
	/// Use this if you are gonna increment the count and check if the command is done.
	/// </summary>
	public bool CountAndCheckIfDone()
	{
		CountThisTick();
		return IsDone();
	}

	public override string ToString() => $"Cmd[{m_Count}/{m_Ticks}]";
}
