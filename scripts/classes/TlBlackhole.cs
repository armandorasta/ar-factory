using Godot;

namespace ArFactory;

public partial class TlBlackhole(Vector2I gloc) : TlHolder(gloc)
{
	public override bool CanItemEnterInDir(Item item, Direction movementDir) => true;
	public override bool HasInputInDir(Direction dir) => true;
	public override bool HasOutputInDir(Direction dir) => false;
	public override Color TypeToCol() => Colors.Brown;
}
