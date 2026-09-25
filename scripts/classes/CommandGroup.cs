using System.Collections.Generic;

namespace ArFactory;

public class CommandGroup(List<Command> cmds)
{
	public List<Command> Commands => m_Commands;

	private List<Command> m_Commands = cmds;




}
