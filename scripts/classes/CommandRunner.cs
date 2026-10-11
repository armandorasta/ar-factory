using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

public readonly struct CommandRunner()
{
	public class CmdWrapper(Unit penderUnit, Command cmd, IEnumerable<Command> dep)
	{
		public readonly Command Cmd = cmd;
		public readonly List<Command> DepCmds = [.. dep];
		public readonly Unit PenderUnit = penderUnit;
		public bool bRanThisTick = false;

		public bool AllDepsDone() => DepCmds.All(c => c.IsDone());
		public override string ToString() => Cmd.ToString();
	}

	public class OnTickPender()
	{
		public readonly List<List<CmdWrapper>> Groups = [];
		public Unit? PenderUnit;

		public void Pend(IEnumerable<IEnumerable<Command>> newSequence, int startIndex = 0)
		{
			Debug.Assert(0 <= startIndex && startIndex <= Groups.Count);
			Debug.AssertNotNull(PenderUnit);
			
			IEnumerable<Command> lastNewCmdGroup = [];
			var i = startIndex;
			foreach (var ng in newSequence)
			{
				if (i < Groups.Count)
				{
					Groups[i].AddRange(ng.Select(c => new CmdWrapper(PenderUnit!, c, lastNewCmdGroup)));
				}
				else
				{
					Groups.Add([.. ng.Select( c => new CmdWrapper(PenderUnit!, c, lastNewCmdGroup) )]);
				}
				lastNewCmdGroup = ng;
				++i;
			}

			PenderUnit!.AddCommandsToWaitList(newSequence);
		}
	}

	private static readonly Type[] s_CmdPiorities =
	[
		typeof(CmdIf),
		typeof(CmdVacate),
		typeof(CmdAwait),
		typeof(CmdAwaitVacate),
		typeof(CmdDemand),
		typeof(CmdSpawn),
		typeof(CmdClone),
		typeof(CmdUpdate),
		typeof(CmdTeleport),
		typeof(CmdCombine),
		typeof(CmdKill),
		typeof(CmdSlide),
		typeof(CmdSleep),
	];

	private static int GetCmdPiority<T>(T cmd) where T : Command
	{
		var tp = cmd.GetType();
		for (var i = 0; i < s_CmdPiorities.Length; ++i)
		{
			if (s_CmdPiorities[i] == tp)
			{
				return i;
			}
		}

		throw new InvalidProgramException($"GetCmdPiority: Invalid command type '{tp}'");
	}


	private readonly List<List<CmdWrapper>> m_Groups = [];
	private readonly List<CmdSlide> m_BlockedSlideCmdsByItems = [];
	private readonly List<CmdWrapper> m_OnEdgeQueue = [];

	/// <summary>
	/// Checks if the runner no longer needs to execute anything, either because it already executed
	/// everything else (always the case in blocking mode), or it failed somewhere (only in non-blocking
	/// mode).
	/// </summary>
	public bool IsDone() => m_Groups.Count == 0;

	public void PendFullSequence(Unit penderUnit, IEnumerable<IEnumerable<Command>> newSequence, int startIndex = 0)
	{
		Debug.Assert(0 <= startIndex && startIndex <= m_Groups.Count);
		IEnumerable<Command> lastGroup = [];
		var i = startIndex;
		foreach (var ng in newSequence)
		{
			if (i < m_Groups.Count)
			{
				m_Groups[i].AddRange(ng.Select(c => new CmdWrapper(penderUnit, c, lastGroup)));
			}
			else
			{
				m_Groups.Add([.. ng.Select(c => new CmdWrapper(penderUnit, c, lastGroup))]);
			}
			lastGroup = ng;
			++i;
		}

		penderUnit.AddCommandsToWaitList(newSequence);
	}

	/// <summary>
	/// Should be called on the beginning of the tick, this does nothing when no commands are left to be executed.
	/// </summary>
	public bool OnTick(Level lv)
	{
		if (IsDone()) return true;
		for (var gIndex = 0; gIndex < m_Groups.Count; ++gIndex)
		{
			var currGroup = m_Groups[gIndex];
			var bGoingNextGroup = false;
			for (var otPender = new OnTickPender();;)
			{
				currGroup.Sort((l, r) => GetCmdPiority(l.Cmd) - GetCmdPiority(r.Cmd));
				foreach (var cw in currGroup)
				{
					if (cw.AllDepsDone() && !cw.bRanThisTick)
					{
						otPender.PenderUnit = cw.PenderUnit;
						cw.Cmd.HandleOnTick(lv, otPender);
						cw.bRanThisTick = true;
						if (!cw.Cmd.IsSmooth()) 
						{
							cw.Cmd.CountThisTick();
						}

						bGoingNextGroup = true;
					}
				}
				currGroup.RemoveAll(c => c.Cmd.IsDone());
				if (otPender.Groups.Count > 0)
				{
					var i = gIndex;
					foreach (var ng in otPender.Groups)
					{
						if (i < m_Groups.Count)
						{
							m_Groups[i].AddRange(ng);
						}
						else
						{
							m_Groups.Add([.. ng]);
						}
						++i;
					}
					otPender.Groups.Clear();
				}
				else break;
			}

			if (!bGoingNextGroup)
			{
				break;
			}
		}

		m_Groups.RemoveAll(g => g.Count == 0);

		HandleSlideCmdOverlapping(lv.World);
		return IsDone();
	}

	public bool DoPerFrame(double dt, Level lv)
	{
		if (IsDone())
		{
			return true;
		}

		foreach (var group in m_Groups)
		{
			var bSomethingRan = false;
			foreach (var cw in group)
			{
				if (cw.bRanThisTick && cw.Cmd.IsSmooth())
				{
					cw.Cmd.HandleFrame(dt, lv);
					bSomethingRan = true;
				}
			}
			if (!bSomethingRan)
			{
				break;
			}
		}

		return false;
	}

	/// <summary>
	/// Should be called at the end of the tick, this does nothing when no commands are left to be executed.
	/// </summary>
	public bool OnEdge(Level lv)
	{
		foreach (var group in m_Groups)
		{
			var bSomethingRan = false;
			foreach (var cw in group)
			{
				if (cw.bRanThisTick && cw.Cmd.IsSmooth())
				{
					cw.Cmd.HandleOnEdge(lv);
					cw.Cmd.CountThisTick();
					bSomethingRan = true;
				}
				cw.bRanThisTick = false;
			}
			group.RemoveAll(c => c.Cmd.IsDone());
			if (!bSomethingRan)
			{
				break;
			}
		}

		m_Groups.RemoveAll(g => g.Count == 0);

		return IsDone();
	}

	public void Reset()
	{
		m_Groups.Clear();
		m_BlockedSlideCmdsByItems.Clear();
	}

	public void QueueBlockedSlideCmdByAnotherItem(WorldPanel world, CmdSlide cmd)
	{
		Debug.Assert(world.HasTile(cmd.GridFrom));
		Debug.Assert(world.GetTileNonNull(cmd.GridFrom).HasItem()); // Is there an actual item?
		Debug.Assert(world.GetTileNonNull(cmd.GetGridTo()).IsReserved()); // And is it blocked?
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
