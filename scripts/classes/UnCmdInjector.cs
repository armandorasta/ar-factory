using System.Collections.Generic;

namespace ArFactory;

public partial class UNCmdInjector : Unit
{
	private List<List<Command>> m_CmdSequenceNextTick = [];

	public void Setup(WorldPanel world)
	{
		InjectorBaseInit(world);
	}

	protected override void BuildTilesAsIfFacingEast() { }
	
	public override void PendNewCommands(WorldPanel.PendProxy pender)
	{
		pender.Pend(m_CmdSequenceNextTick);
		m_CmdSequenceNextTick = [];
	}

	// Breaks everything if called after OnTick and before OnEdge...
	public void PendParallel(IEnumerable<Command> cmds, int index = 0)
	{
		Debug.Assert(0 <= index && index <= m_CmdSequenceNextTick.Count);
		if (index < m_CmdSequenceNextTick.Count)
		{
			m_CmdSequenceNextTick[index].AddRange(cmds);
		}
		else
		{
			m_CmdSequenceNextTick.Add([.. cmds]);
		}
	}

	public override string ToString() 
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "CmdInjector");
}
