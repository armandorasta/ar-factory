using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArFactory;

public class CommandRunner
{
	private readonly List<List<Command>> m_Groups;

	public CommandRunner(IEnumerable<IEnumerable<Command>> cmdGroups)
	{
		// !Any(_ => true) returns true for empty sequences only.
		var nonEmptySeqs = cmdGroups.Where(g => g.Any(_ => true));
		var lists = nonEmptySeqs.Select(g => g.ToList());
		m_Groups = [.. lists];
	}

	/// <summary>
	/// Checks if the runner no longer needs to execute anything, either because it already executed
	/// everything else (always the case in blocking mode), or it failed somewhere (only in non-blocking
	/// mode).
	/// </summary>
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
		if (!parallelCmds.Any(_ => true)) // !Any(_ => true) returns false for empty sequences only.
		{
			return;
		}

		m_Groups.Add([.. parallelCmds]);
	}

	/// <summary>
	/// Should be called on the beginning of the tick, this does nothing when no commands are left to be executed.
	/// </summary>
	public bool OnTick(Level lv)
	{
		if (IsDone())
		{
			return true;
		}

		while (m_Groups.Count > 0)
		{
			var g0 = m_Groups[0];
			foreach (var cmd in g0)
			{
				cmd.HandleOnTick(lv);
			}

			for (var i = g0.Count - 1; i >= 0; --i)
			{
				var cmd = g0[i];
				Debug.Assert(!cmd.IsSmooth() || !cmd.IsDone()); // These should have already been bombed in OnEdge.
				if (!cmd.IsSmooth() && cmd.CountAndCheckIfDone())
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

		return IsDone();
	}

	/// <summary>
	/// Should be called at the end of the tick, this does nothing when no commands are left to be executed.
	/// </summary>
	public bool OnEdge(Level lv)
	{
		if (IsDone())
		{
			return true;
		}

		// No loop here like OnTick because we know that commands on next group have not called OnTick yet.

		var g0 = m_Groups[0];
		foreach (var cmd in g0)
		{
			if (!cmd.HasBlockedThisTick() && cmd.IsSmooth())
			{
				cmd.HandleOnEdge(lv);
			}
		}

		for (var i = g0.Count - 1; i >= 0; --i)
		{
			var cmd = g0[i];
			// Instant commands get popped in OnTick, only smooth commands are handled at the end here.
			Debug.Assert(cmd.IsSmooth() || !cmd.IsDone());
			if (cmd.IsSmooth() && cmd.CountAndCheckIfDone())
			{
				g0.RemoveAt(i);
			}
		}

		if (g0.Count == 0)
		{
			m_Groups.RemoveAt(0);
		}
		
		return IsDone();
	}

	public bool DoPerFrame(double dt, Level lv)
	{
		if (m_Groups.Count != 0)
		{
			foreach (var cmd in m_Groups[0])
			{
				cmd.HandleFrame(dt, lv);
			}

			return false;
		}
		else return true;
	}

	public void Clear() => m_Groups.Clear();
}
