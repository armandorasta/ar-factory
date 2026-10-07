using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Godot;

namespace ArFactory;

public readonly struct CommandRunner()
{
	private class CmdWrapper(Command cmd, IEnumerable<Command> dep)
	{
		public readonly Command Cmd = cmd;
		public readonly List<Command> DepCmds = [.. dep];
		public bool bRanThisTick = false;

		public bool IsReady() => DepCmds.All(c => c.IsDone());
		public void OnTick(Level lv)
		{
			if (!bRanThisTick)
			{
				Cmd.HandleOnTick(lv);
				bRanThisTick = true;
			}
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

	private static int GetCmdPiority<T>(T cmd) where T: Command
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


	private readonly List<CmdWrapper> m_Cmds = [];
	private readonly List<CmdSlide> m_BlockedSlideCmdsByItems = [];

	/// <summary>
	/// Checks if the runner no longer needs to execute anything, either because it already executed
	/// everything else (always the case in blocking mode), or it failed somewhere (only in non-blocking
	/// mode).
	/// </summary>
	public bool IsDone() => m_Cmds.Count == 0;

	public void PendFullSequence(IEnumerable<IEnumerable<Command>> groups)
	{
		IEnumerable<Command> lastGroup = [];
		foreach (var g in groups)
		{
			foreach (var cmd in g)
			{
				m_Cmds.Add(new CmdWrapper(cmd, lastGroup));
			}
			lastGroup = g;
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

		var i = 0;
		do
		{
			m_Cmds.Sort((l, r) => GetCmdPiority(l.Cmd) - GetCmdPiority(r.Cmd));

			var count = m_Cmds.Count;
			for (i = 0; i < count; ++i)
			{
				var cmdWrap = m_Cmds[i];
				if (cmdWrap.IsReady())
				{
					cmdWrap.OnTick(lv);
					if (m_Cmds.Count != count)
					{
						break;
					}
				}
			}
		} while (i < m_Cmds.Count);

		for (i = m_Cmds.Count - 1; i >= 0; --i)
		{
			var cmd = m_Cmds[i];
			Debug.Assert(!cmd.Cmd.IsSmooth() || !cmd.Cmd.IsDone()); // These should have already been bombed in OnEdge.
			if (cmd.IsReady() && !cmd.Cmd.IsSmooth() && cmd.Cmd.CountAndCheckIfDone())
			{
				m_Cmds.RemoveAt(i);
			}
			else
			{
				cmd.bRanThisTick = false;
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

		foreach (var cmd in m_Cmds)
		{
			if (cmd.IsReady() && !cmd.Cmd.HasBlockedThisTick() && cmd.Cmd.IsSmooth())
			{
				cmd.Cmd.HandleOnEdge(lv);
			}
		}

		for (var i = m_Cmds.Count - 1; i >= 0; --i)
		{
			var cmd = m_Cmds[i];
			// Instant commands get popped in OnTick, only smooth commands are handled at the end here.
			Debug.Assert(cmd.Cmd.IsSmooth() || !cmd.Cmd.IsDone());
			if (cmd.Cmd.IsSmooth() && cmd.Cmd.CountAndCheckIfDone())
			{
				m_Cmds.RemoveAt(i);
			}
		}
		
		return IsDone();
	}

	public void DoPerFrame(double dt, Level lv)
	{
		foreach (var cmd in m_Cmds)
		{
			cmd.Cmd.HandleFrame(dt, lv);
		}
	}

	public void Reset()
	{
		m_Cmds.Clear();
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
