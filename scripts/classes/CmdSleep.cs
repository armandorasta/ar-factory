namespace ArFactory;

/// <summary>
/// Does nothing for a specified number of ticks, useful for testing.
/// </summary>
public partial class CmdSleep(int tickCount) : Command(tickCount - 1)
{
	public override void OnTick(Level lv)
	{
	}

	public override string ToString() => Utilz.ReplaceBaseNameInToString(base.ToString(), "Sleep");
}
