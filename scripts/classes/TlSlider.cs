using Godot;

namespace ArFactory;

public partial class TlSlider(Vector2I gloc, Direction dir) : TlHolder(gloc)
{
	private Direction m_Dir = dir;

	public override bool CanItemEnterInDir(Item item, Direction movementDir) => movementDir != m_Dir.Invert();
	public override bool HasInputInDir(Direction dir) => dir != m_Dir;
	public override bool HasOutputInDir(Direction dir) => dir == m_Dir;
	public override Color TypeToCol() => Colors.LightPink;
	public override Direction GetDir() => m_Dir;
	public override void SetDir(Direction newDir) => m_Dir = newDir;
}
