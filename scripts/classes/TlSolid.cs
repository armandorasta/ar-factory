using Godot;

namespace ArFactory;

public partial class TlSolid(Vector2I gloc) : Tile(gloc)
{
	public override bool CanItemEnterInDir(Item item, Direction movementDir) => false;
	public override bool HasInputInDir(Direction dir) => false;
	public override bool HasOutputInDir(Direction dir) => false;
	public override Color TypeToCol() => Colors.DarkGray;
	public override bool IsReserved() => true;
	public override void SetReserved(bool toWhat) {}
}

