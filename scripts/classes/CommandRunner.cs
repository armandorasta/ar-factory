using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArFactory;

public class CommandRunner(IEnumerable<IEnumerable<Command>> cmdGroups)
{
	private readonly List<List<Command>> m_Groups = [.. cmdGroups.Select(g => g.ToList())];
	/// <summary>
	/// In non-blocking mode as soon as one of the commands in the sequences blocks the runner will 
	/// just stop executing and delete everything.
	/// </summary>
	private bool m_bFailed = false;
	private bool m_bBlocking = true;

	/// <summary>
	/// Checks if the runner no longer needs to execute anything, either because it already executed
	/// everything else (always the case in blocking mode), or it failed somewhere (only in non-blocking
	/// mode).
	/// </summary>
	public bool IsDone() => m_Groups.Count == 0;
	/// <summary>
	/// If all the commands in the sequences executed start to finish.
	/// This is the same as <see cref="IsDone"/> in normal blocking mode since failure is only 
	/// possible in non-blocking mode.
	/// </summary>
	public bool IsDoneSuccessfully() => IsDone() && !m_bFailed;
	/// <summary>
	/// If we are in non-blocking mode and one of the commands blocked.
	/// </summary>
	public bool IsDoneWithFailure() => IsDone() && m_bFailed;

	/// <summary>
	/// Returns <see langword="true"/> if we are waiting for the animation of a slide command to end, 
	/// that slide command is on one of the outputs of the unit.
	/// In this case, we can often just pend new commands before the end of the animation on the next
	/// tick.
	/// If there are no pending commands, it will return false!
	/// </summary>
	public bool IsJustAwaitingOutSlideAnim() // TODO: Make this function only check output tiles somehow!
		=> m_Groups.Count > 0 && m_Groups[0].All(c => c is CmdSlide sc && sc.IsAwaitingAnim());
	
	public void MakeNonBlocking()
	{
		m_bBlocking = false;
		MakeAllSubCmdsNonBlocking();
	}

	/// <summary>
	/// As it says, so adding more commands after calling this will not automatically make the new
	/// commands non-blocking.
	/// </summary>
	private void MakeAllSubCmdsNonBlocking()
	{
		foreach (var g in m_Groups)
		{
			foreach (var cmd in g)
			{
				cmd.MakeNonBlocking();
			}
		}
	}

	/// <summary>
	/// Pends commands to be executed in parallel, until this entire group finishes executing the
	/// runner will not move to next group.
	/// </summary>
	public void PendParallel(IEnumerable<Command> parallelCmds)
	{
		m_Groups.Add([.. parallelCmds.Select(c => m_bBlocking ? c : c.MakeNonBlocking())]);
	}

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
				if (!m_bBlocking && !cmd.IsBlocking() && cmd.HasBlockedThisTick())
				{
					m_Groups.Clear();
					m_bFailed = true;
					return;
				}
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
