using Godot;
using System;

namespace ArFactory;

public abstract partial class Command(int tickCount) : RefCounted
{
	// Number of _Ticks it takes to finish.
	private int m_Ticks = tickCount;

	/// Keeps track of _Ticks passed for this command.
	/// Not that simple though, because sometimes the command has to wait for something and freezes.
	private int m_Count = 0;

	public abstract void OnTick(Level lv);
	
	public virtual void DoPerFrame(double dt, Level lv) {}

	/// Returns the number of ticks the command takes to finish executing without getting interrupted.
	/// Most commands take 1 or 2 ticks. [br]
	/// Commands that take 0 ticks execute instantly and don't block any other commands, commands that
	/// take 1 or more ticks block and push other commands to next ticks. [br]
	/// For example 100 kill commands on the same tile will all execute in the same tick, while 2 slide 
	/// commands will take 2 ticks; one will block the other on the first tick.
	public int GetTickCount() => m_Ticks;


	public void CountThisTick() => m_Count += 1;
	public void PauseThisTick() => m_Count -= 1;
	public bool IsDone() => m_Count >= m_Ticks;
}
