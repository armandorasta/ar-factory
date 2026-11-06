using System;
using Godot;

namespace ArFactory;

public interface ITileOwner
{
	public Tile? GetTile(Vector2I gloc);
	public bool RecieveTile(Tile tl);
	public bool TakeAwayTile(Tile tl);
}
