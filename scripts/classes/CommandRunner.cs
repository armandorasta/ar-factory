using System;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

public class CommandRunner : IDestroyable
{
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

	/// <summary>
	/// Checks if the runner no longer needs to execute anything, either because it already executed
	/// everything else (always the case in blocking mode), or it failed somewhere (only in non-blocking
	/// mode).
	/// </summary>
	public bool IsDone() => m_Groups.Count is 0;

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
	public bool OnTick(Simulator.OnTickMediator ticker)
	{
		if (IsDone())
		{
			return true;
		}
		for (var gIndex = 0; gIndex < m_Groups.Count; ++gIndex)
		{
			var currGroup = m_Groups[gIndex];
			var bGoingNextGroup = false;
			for (;;)
			{
				currGroup.Sort((l, r) => GetCmdPiority(l.Cmd) - GetCmdPiority(r.Cmd));
				foreach (var cw in currGroup)
				{
					// Commands that were forced to end can be done before first tick so...
					if (!cw.Cmd.IsDone() && !cw.HasRanThisTick() && cw.AllDepsDone())
					{
						ticker.Pender._SetPenderUnit(new(this), cw.PenderUnit);
						cw.OnTick(ticker);
						bGoingNextGroup = true;
					}
				}
				currGroup.RemoveAll(c => c.Cmd.IsDone());
				if (ticker.Pender.Groups.Count > 0)
				{
					PendFullSequence(ticker.Pender.PenderUnit!, ticker.Pender.Groups, gIndex);
					ticker.Pender.Groups.Clear();
				}
				else break;
			}

			if (!bGoingNextGroup)
			{
				break;
			}
		}

		m_Groups.RemoveAll(g => g.Count == 0);

		HandleSlideCmdOverlapping(ticker.World);
		return IsDone();
	}

	public bool DoPerFrame(Simulator.PerFrameMediator ticker)
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
				if (cw.HasRanThisTick() && cw.Cmd.IsSmooth())
				{
					cw.Cmd.HandleFrame(ticker);
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
	public bool OnEdge(Simulator.OnEdgeMediator ticker)
	{
		foreach (var group in m_Groups)
		{
			var bSomethingRan = false;
			foreach (var cw in group)
			{
				// Smooth commands that were forced to finish after OnTick was called can be done by this point.
				if (!cw.Cmd.IsDone() && cw.HasRanThisTick() && cw.Cmd.IsSmooth())
				{
					cw.OnEdge(ticker);
					bSomethingRan = true;
				}
				cw.ResetRunFlag();
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

	/// <summary>
	/// Same as <see cref="Reset"/>. Just resets the runner like it was when it was created.
	/// </summary>
	public void Destroy()
	{
		Reset();
	}

	public void QueueBlockedSlideCmdByAnotherItem(WorldPanel world, CmdSlide cmd)
	{
		Debug.Assert(world.HasTileOn(cmd.GridFrom));
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

	#region .Inner Classes

	public class CmdWrapper(Unit penderUnit, Command cmd, IEnumerable<Command> dep)
	{
		public readonly Unit PenderUnit = penderUnit;
		public readonly Command Cmd = cmd;
		private readonly List<Command> m_DepCmds = [.. dep];
		private bool m_bRanThisTick = false;

		public bool AllDepsDone() => m_DepCmds.All(c => c.IsDone());
		public bool HasRanThisTick() => m_bRanThisTick;
		public override string ToString() => Cmd.ToString();

		public void OnTick(Simulator.OnTickMediator ticker)
		{
			Debug.Assert(!m_bRanThisTick);
			Cmd.HandleOnTick(ticker);
			m_bRanThisTick = true;
			if (!Cmd.IsSmooth())
			{
				Cmd.CountThisTick();
			}
		}

		public void OnEdge(Simulator.OnEdgeMediator ticker)
		{
			Debug.Assert(m_bRanThisTick);
			Debug.Assert(Cmd.IsSmooth());
			Cmd.HandleOnEdge(ticker);
			Cmd.CountThisTick();
		}

		public void ResetRunFlag() => m_bRanThisTick = false;
	}

	public class OnTickPender(CommandRunner runner)
	{
		public readonly struct Perm(CommandRunner runner)
		{
			private readonly CommandRunner m_Runner = runner;
			public bool IsFine(OnTickPender proxy) => m_Runner == proxy.m_Runner;
		}

		// Commands have to be stored separately since Pend gets called mid iteration of the command groups.
		public readonly List<List<Command>> Groups = [];
		public Unit? PenderUnit { get; private set; }
		private readonly CommandRunner m_Runner = runner;

		public void Pend(IEnumerable<IEnumerable<Command>> newSequence, int startIndex = 0)
		{
			Debug.Assert(0 <= startIndex && startIndex <= Groups.Count);
			Debug.AssertNotNull(PenderUnit);

			var i = startIndex;
			foreach (var ng in newSequence)
			{
				if (i < Groups.Count)
				{
					Groups[i].AddRange(ng);
				}
				else
				{
					Groups.Add([.. ng]);
				}
				++i;
			}
		}

		public void QueueBlockedSlideCmdByAnotherItem(WorldPanel world, CmdSlide cmd) 
			=> m_Runner.QueueBlockedSlideCmdByAnotherItem(world, cmd);
		
		public void _SetPenderUnit(Perm perm, Unit newUnit)
		{
			Debug.AssertNotNull(newUnit);
			Debug.Assert(perm.IsFine(this));
			PenderUnit = newUnit;
		}
	}

	#endregion // .Inner Classes
}
