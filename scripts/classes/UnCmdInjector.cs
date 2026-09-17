using Godot;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ArFactory;

public partial class UnCmdInjector : Unit
{
	public List<Command> Commands;

	// This is used in the reset function to re-enqueue the commands again for next run, otherwise
	// the injector will only work for a single run.
	private List<Command> m_CmdCache;

	public void Setup(WorldPanel world, IEnumerable<Command> cmdsToInject)
	{
		InjectorBaseInit(world);
		Commands = [.. cmdsToInject];
	}

	protected override void BuildTiles() { }
	
	public override void PendNewCommands()
	{
		if (HasPendingCmds() && !IsJustAwaitingOutSlideAnim())
		{
			return;
		}

		if (Commands is not null)
		{
			PendCmdSeq(Commands);
			m_CmdCache = Commands;
			Commands = null;
		}
	}

	public override void Reset()
	{
		base.Reset();
		Commands = m_CmdCache;
	}
}
