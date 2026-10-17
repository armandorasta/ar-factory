namespace ArFactory;

/// <summary>
/// Does nothing for a specified number of ticks, useful for testing.
/// </summary>
public class CmdSleep : Command
{
	public CmdSleep(int tickCount) : base(tickCount, true)
	{
		Debug.Assert(tickCount > 0);
	}

	protected override void OnTick(Editor.OnTickMediator ticker) { }
	protected override void OnEdge(Editor.OnEdgeMediator ticker) { }


	public override string ToString() => Utilz.ReplaceBaseNameInToString(base.ToString(), "Sleep");
}
