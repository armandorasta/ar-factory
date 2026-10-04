using Godot;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ArFactory;

public partial class UnCmdInjector : Unit
{
	private List<List<Command>> m_CmdsNextTick = [];

	// This is used in the reset function to re-enqueue the commands again for next run, otherwise
	// the injector will only work for a single run.
	private List<List<Command>> m_DefaultCmds;

	public void Setup(WorldPanel world, IEnumerable<IEnumerable<Command>> cmdsToInject)
	{
		InjectorBaseInit(world);
		foreach (var group in cmdsToInject)
		{
			m_CmdsNextTick.Add([.. group]);
		}
		m_DefaultCmds = [.. m_CmdsNextTick];
	}

	protected override void BuildTiles() { }
	
	public override void PendNewCommands()
	{
		if (!IsSafeToPendCmds())
		{
			return;
		}

		foreach (var group in m_CmdsNextTick)
		{
			PendCmdSeq(group);
		}

		m_DefaultCmds ??= m_CmdsNextTick;
		m_CmdsNextTick = [];
	}

	public void PendParallel(IEnumerable<Command> cmds)
	{
		PendCmdSeq(cmds);
	}

	public override void Reset()
	{
		base.Reset();
		m_CmdsNextTick = m_DefaultCmds;
		m_DefaultCmds = null;
	}

	public override string ToString() 
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "CmdInjector");

}
