using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ArFactory;

public class Key(UNSlider tgt)
{
	private readonly WeakReference<UNSlider> m_Target = new(tgt);

	public void OnItemLeave(UNSlider parent)
	{
		Debug.Assert(m_Target!.TryGetTarget(out var tgt));
		tgt.Unlock();
	}

	public void DebugDraw(UNSlider parent)
	{
		Debug.AssertNotNull(m_Target);
		Debug.Assert(m_Target!.TryGetTarget(out var tgt));
		var world = parent.World;
		var cellSize = world.GetCellSize();

		parent.DrawLine(
			0.5f*cellSize, world.GridToPos(tgt.GLocation) - parent.Position + 0.5f*cellSize, Godot.Colors.Pink);
		parent.DrawRect(new Rect2(tgt.Position - parent.Position + 0.4f*cellSize, 0.2f*cellSize), Godot.Colors.Pink);
	}
}
