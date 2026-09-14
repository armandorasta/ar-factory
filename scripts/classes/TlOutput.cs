using Godot;

namespace ArFactory;

public partial class TlOutput(Vector2I gloc, Direction dir) : TlHolder(gloc)
{
	private Direction m_Dir = dir;

	public override bool CanItemEnterInDir(Item item, Direction movementDir) => false;
	public override bool HasInputInDir(Direction dir) => false;
	public override bool HasOutputInDir(Direction dir) => dir == m_Dir;
	public override Color TypeToCol() => Colors.LightGreen;
	public override Direction GetDir() => m_Dir;
	public override void SetDir(Direction newDir) => m_Dir = newDir;
}
