using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArFactory;

public class CommandRunner
{
	private readonly List<List<Command>> m_Groups;
	private readonly List<CmdSlide> m_BlockedSlideCmdsByItems = [];

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

	public void PendFullSequence(IEnumerable<IEnumerable<Command>> groups, int startIndex = 0)
	{
		Debug.Assert(0 <= startIndex && startIndex <= m_Groups.Count);

		// We pend in parallel with all other pend calls:
		// First  call: Pend([ [g1], [g2] ])          => m_Groups = [ [g1], [g2] ]
		// Second call: Pend([ [g3], [g4], [g5] ])    => m_Groups = [ [g1, g3], [g2, g4], [g5] ]
		// Third  call: Pend([ [g6], [g7], [g8] ], 1) => m_Groups = [ [g1, g3], [g2, g4, g6], [g5, g7], [g8] ]
		// etc...

		var i = startIndex;
		foreach (var g in groups)
		{
			PendParallel(g, i);
			i += 1;
		}
	}

	public void PendParallel(IEnumerable<Command> group, int index)
	{
		Debug.Assert(0 <= index && index <= m_Groups.Count);
		if (index < m_Groups.Count)
		{
			m_Groups[index].AddRange(group);
		}
		else
		{
			m_Groups.Add([.. group]);
		}
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

		HandleSlideCmdOverlapping(lv.World);
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

	public void Reset()
	{
		m_Groups.Clear();
		m_BlockedSlideCmdsByItems.Clear();
	}

	public void QueueBlockedSlideCmdByAnotherItem(WorldPanel world, CmdSlide cmd)
	{
		Debug.Assert(world.HasTile(cmd.GridFrom));
		Debug.Assert(world.GetTile(cmd.GridFrom).HasItem()); // Is there an actual item?
		Debug.Assert(world.GetTile(cmd.GetGridTo()).IsReserved()); // And is it blocked?
		Debug.Assert(!m_BlockedSlideCmdsByItems.Contains(cmd));
		
		m_BlockedSlideCmdsByItems.Add(cmd);
	}

	private void HandleSlideCmdOverlapping(WorldPanel world)
	{
		// If any update happens in a pass, end that pass and start over,
		// repeat until a full pass happens with no updates.
		while (1 + 1 == 2)
		{
			var bSomethingMoved = false;
			for (var i = 0; i < m_BlockedSlideCmdsByItems.Count; ++i)
			{
				if (m_BlockedSlideCmdsByItems[i].TryMovingByOverlap(world))
				{
					// Something moved? can only move once per tick so bye-bye.
					m_BlockedSlideCmdsByItems.RemoveAt(i);
					bSomethingMoved = true;
					break;
				}
			}
			if (!bSomethingMoved) // None has updated? it's over.
			{
				break;
			}
		}
		m_BlockedSlideCmdsByItems.Clear();
	}
}
