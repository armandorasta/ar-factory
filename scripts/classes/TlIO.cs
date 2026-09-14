using Godot;

namespace ArFactory;

public partial class TlIO(Vector2I gloc, Direction outDir, Direction inDir) : TlHolder(gloc)
{
	private Direction m_OutputDir = outDir;
	private Direction m_InputDir = inDir;

	public override bool CanItemEnterInDir(Item item, Direction movementDir)
		=> movementDir == m_InputDir.Invert();
	public override bool HasInputInDir(Direction dir) => dir == m_InputDir;
	public override bool HasOutputInDir(Direction dir) => dir == m_OutputDir;
	public override Color TypeToCol() => Colors.LightYellow;
	public override Direction GetDir() => GetOutputDir();
	public override void SetDir(Direction newDir) => SetOutputDir(newDir);
	public override void Rotate90()
	{
		m_InputDir = m_InputDir.Rotate90();
		m_OutputDir = m_OutputDir.Rotate90();
	}

	public Direction GetOutputDir() => m_OutputDir;
	public Direction GetInputDir() => m_InputDir;
	public void SetOutputDir(Direction newDir, bool bSetInToInv = false)
	{
		Debug.Assert(bSetInToInv || newDir != m_InputDir);
		m_OutputDir = newDir;
		if (bSetInToInv)
		{
			m_InputDir = newDir.Invert();
		}
		EmitSignal(SignalName.NeedsRedraw);
	}

	public void SetInputDir(Direction newDir)
	{
		Debug.Assert(newDir != m_InputDir);
		m_InputDir = newDir;
		EmitSignal(SignalName.NeedsRedraw);
	}
}
