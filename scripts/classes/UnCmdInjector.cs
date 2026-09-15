using Godot;
using System.Collections.Generic;

namespace ArFactory;

public partial class UnCmdInjector : Unit
{
	public List<Command> Commands;

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

		PendCmdSeq(Commands);
		Commands.Clear();
	}
}
