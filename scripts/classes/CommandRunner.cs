using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

public class CommandRunner(List<List<Command>> cmdGroups)
{
	private readonly List<List<Command>> m_Groups = cmdGroups;

	/// <returns>
	/// <see langword="true"/> if the sequence was fully executed, <see langword="false"/> otherwise
	/// </returns>
	public bool IsDone() => m_Groups.Count == 0;

	/// <summary>
	/// Returns <see langword="true"/> if we are waiting for the animation of a slide command to end, 
	/// that slide command is on one of the outputs of the unit.
	/// In this case, we can often just pend new commands before the end of the animation on the next
	/// tick.
	/// If there are no pending commands, it will return false!
	/// </summary>
	public bool IsJustAwaitingOutSlideAnim() // TODO: Make this function only check output tiles somehow!
		=> m_Groups.Count > 0 && m_Groups[0].All(c => c is CmdSlide sc && sc.IsAwaitingAnim());
	
	/// <summary>
	/// Pends commands to be executed in parallel, until this entire group finishes executing the
	/// runner will not move to next group.
	/// </summary>
	public void PendParallel(IEnumerable<Command> parallelCmds)
	{
		m_Groups.Add([.. parallelCmds]);
	}

	/// <summary>
	/// Pends a command to be executed on its own, until this command finishes executing the commands
	/// after it will not. Same as pending a parralel group with one command in it.
	/// </summary>
	public void PendCmd(Command newCmd) => PendParallel([newCmd]);

	public void OnTick(Level lv)
	{
        if (m_Groups.Count == 0)
		{
			return;
		}

		while (m_Groups.Count > 0)
		{
			var g0 = m_Groups[0];
			foreach (var cmd in g0)
			{
				cmd.HandleTick(lv);
			}

			for (var i = g0.Count - 1; i >= 0; --i)
			{
				if (g0[i].CountAndCheckIfDone())
				{
					g0.RemoveAt(i);
				}
			}

			if (g0.Count == 0)
			{
				// Done with the current group, move to the next one immediately until either all
				// commands get consumed, or you get to a group and not all of its commands.
				m_Groups.RemoveAt(0);
			}
			else
			{
				break; // Unfinished group? must be finished before we move on, so wait for next tick.
			}
		}
	}

	public void DoPerFrame(double dt, Level lv)
	{
		if (m_Groups.Count == 0)
		{
			return;
		}

		var g0 = m_Groups[0];
		foreach (var cmd in g0)
		{
			cmd.HandleFrame(dt, lv);
		}
	}

	public void Clear() => m_Groups.Clear();
}
