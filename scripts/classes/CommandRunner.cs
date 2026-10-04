using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArFactory;

public class CommandRunner(IEnumerable<IEnumerable<Command>> cmdGroups)
{
	public enum Result
	{
		DoneWithFailure,
		DoneSuccessfully,
		NotDoneYet,
	}

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
	private bool IsDoneSuccessfully() => IsDone() && !m_bFailed;
	/// <summary>
	/// If we are in non-blocking mode and one of the commands blocked.
	/// </summary>
	private bool IsDoneWithFailure() => IsDone() && m_bFailed;

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

	/// <summary>
	/// Should be called on the beginning of the tick, this does nothing when no commands are left to be executed.
	/// </summary>
	public Result OnTick(Level lv)
	{
		while (m_Groups.Count > 0)
		{
			var g0 = m_Groups[0];
			foreach (var cmd in g0)
			{
				cmd.HandleOnTick(lv);
				if (!m_bBlocking && cmd.HasFailedThisTick())
				{
					m_Groups.Clear();
					m_bFailed = true;
					return Result.DoneWithFailure;
				}
			}

			for (var i = g0.Count - 1; i >= 0; --i)
			{
				var cmd = g0[i];
				Debug.Assert(!cmd.IsSmooth() || !cmd.IsDone()); // These should have already been bombed in OnEdge.
				if (!cmd.IsSmooth())
				{
					if (cmd.CountAndCheckIfDone())
					{
						g0.RemoveAt(i);
					}
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

		return IsDone() ? Result.DoneSuccessfully : Result.NotDoneYet;
	}

	/// <summary>
	/// Should be called at the end of the tick, this does nothing when no commands are left to be executed.
	/// </summary>
	public Result OnEdge(Level lv)
	{
		if (m_Groups.Count == 0)
		{
			return IsDoneSuccessfully() ? Result.DoneSuccessfully : Result.DoneWithFailure;
		}

		// No loop here like OnTick because we know that commands on next group have not called OnTick yet.

		var g0 = m_Groups[0];
		foreach (var cmd in g0.Where(c => c.IsSmooth()))
		{
			cmd.HandleOnEdge(lv);
			if (!m_bBlocking && cmd.HasFailedThisTick())
			{
				m_Groups.Clear();
				m_bFailed = true;
				return Result.DoneWithFailure;
			}
		}

		for (var i = g0.Count - 1; i >= 0; --i)
		{
			var cmd = g0[i];
			// Instant commands get popped in OnTick, only smooth commands are handled at the end here.
			Debug.Assert(cmd.IsSmooth() || !cmd.IsDone());
			if (cmd.IsSmooth())
			{
				if (cmd.CountAndCheckIfDone())
				{
					g0.RemoveAt(i);
				}
			}
		}

		if (g0.Count == 0)
		{
			m_Groups.RemoveAt(0);
			return Result.DoneSuccessfully;
		}

		return Result.NotDoneYet;
	}

	public Result DoPerFrame(double dt, Level lv)
	{
		if (m_Groups.Count == 0)
		{
			return IsDoneSuccessfully() ? Result.DoneSuccessfully : Result.DoneWithFailure;
		}

		var g0 = m_Groups[0];
		foreach (var cmd in g0)
		{
			cmd.HandleFrame(dt, lv);
		}

		return Result.NotDoneYet;
	}

	public void Clear() => m_Groups.Clear();
}
