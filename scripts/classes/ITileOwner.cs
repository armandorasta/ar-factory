using System;
using System.Collections.Generic;
using Godot;

namespace ArFactory;

public interface ITileOwner
{
	public Tile? GetTile(Vector2I gloc);
	public IEnumerable<Tile> GetTilesIter();
	public bool RecieveTile(Tile tl);
	public bool TakeAwayTile(Tile tl);
}
