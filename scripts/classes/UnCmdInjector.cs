using Godot;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ArFactory;

public partial class UnCmdInjector : Unit
{
	public List<List<Command>> Commands = [];

	// This is used in the reset function to re-enqueue the commands again for next run, otherwise
	// the injector will only work for a single run.
	private List<List<Command>> m_CmdCache;

	public void Setup(WorldPanel world, IEnumerable<IEnumerable<Command>> cmdsToInject)
	{
		InjectorBaseInit(world);
		foreach (var group in cmdsToInject)
		{
			Commands.Add([.. group]);
		}
	}

	protected override void BuildTiles() { }
	
	public override void PendNewCommands()
	{
		if (HasPendingCmds() && !m_Runner.IsJustAwaitingOutSlideAnim())
		{
			return;
		}

		if (Commands is not null)
		{
			foreach (var group in Commands)
			{
				PendCmdSeq(group);
			}
			m_CmdCache = Commands;
			Commands = null;
		}
	}

	public override void Reset()
	{
		base.Reset();
		Commands = m_CmdCache;
	}

	public override string ToString() 
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "CmdInjector");

}
