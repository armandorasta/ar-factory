namespace ArFactory;

/// <summary>
/// Does nothing for a specified number of ticks, useful for testing.
/// </summary>
public partial class CmdSleep : Command
{
	public CmdSleep(int tickCount) : base(tickCount - 1)
	{
		Debug.Assert(tickCount > 0);
	}

	public override void OnTick(Level lv)
	{
	}

	public override string ToString() => Utilz.ReplaceBaseNameInToString(base.ToString(), "Sleep");
}
