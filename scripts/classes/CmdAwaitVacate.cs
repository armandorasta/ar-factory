using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ArFactory;

/// <summary>
/// Combination of commands <see cref="CmdAwait"/> and <see cref="CmdVacate"/>.
/// </summary>
public class CmdAwaitVacate : Command
{
	public Vector2I[] WaitGridLocs { get; private set; }
	public Vector2I[] VacateGridLocs { get; private set; }

	public CmdAwaitVacate(IEnumerable<Vector2I> waitGLocs, IEnumerable<Vector2I> vacateGLocs) 
		: base(0)
	{
		WaitGridLocs = [.. waitGLocs];
		VacateGridLocs = [.. vacateGLocs];
		Debug.Assert(WaitGridLocs.Length > 0);
		Debug.Assert(VacateGridLocs.Length > 0);
	}

	public CmdAwaitVacate(IEnumerable<Tile> waitTiles, IEnumerable<Tile> vacateTiles) 
		: this(waitTiles.Select(tl => tl.GridLoc), vacateTiles.Select(tl => tl.GridLoc)) 
	{ }

	protected override void OnTick(Level lv)
	{
		var waitTls = WaitGridLocs.Select(lv.World.GetTile);
		var vacateTls = VacateGridLocs.Select(lv.World.GetTile);
		if (!waitTls.All(tl => tl is not null) || !vacateTls.All(tl => tl is not null))
		{
			PauseThisTick();
			return; // Forever and ever...
		}

		if (!waitTls.All(tl => tl.HasItem() && !tl.Item.IsMidAnimation()))
		{
			PauseThisTick();
			return;
		}

		if (!vacateTls.All(tl => !tl.IsReserved()))
		{
			PauseThisTick();
			return;
		}

		Debug.Assert(waitTls.All(tl => tl.Item.IsAllowedToMove()));
	}

	public override string ToString()
	{
		var wStr = (WaitGridLocs.Length > 1) 
			? $"[{string.Join(", ", WaitGridLocs)}]" 
			: WaitGridLocs[0].ToString();
		
		var vStr = (VacateGridLocs.Length > 1) 
			? $"[{string.Join(", ", VacateGridLocs)}]" 
			: VacateGridLocs[0].ToString();
		
		return Utilz.AppendToBaseToString(base.ToString(), $"AwaitVacate[await: {wStr}, vacate: {vStr}]");
	}
}
