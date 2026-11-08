using System;
using System.Runtime.InteropServices.Swift;
using Godot;

namespace ArFactory;

public partial class UNSlider : Unit
{
	public abstract class SliderExtension
	{
		public abstract void OnItemLeave(UNSlider parent);
		public abstract void OnDraw(UNSlider parent);
		public abstract void OnRemoval(UNSlider parent);
		public abstract void OnSignal(UNSlider parent);
		public abstract void Reset(UNSlider parent);
	}

	class SliderExtensionRuntimeLock : SliderExtension
	{
		private int m_InitState = -1;

		public override void OnDraw(UNSlider parent)
		{
			var cellSize = parent.World.GetCellSize();
			parent.DrawRect(new Rect2(cellSize*0.4f, 0.2f*cellSize), Godot.Colors.Pink);
		}

		public override void OnItemLeave(UNSlider parent)
		{
			if (m_InitState == -1)
			{
				m_InitState = parent.IsLocked() ? 1 : 0;
			}
			parent.Lock();
		}

		public override void OnSignal(UNSlider parent)
		{
			if (m_InitState == -1)
			{
				m_InitState = parent.IsLocked() ? 1 : 0;
			}
			parent.Unlock();
		}

		public override void OnRemoval(UNSlider parent) { }
		public override void Reset(UNSlider parent)
		{
			if (m_InitState == 0)
			{
				parent.Unlock();
			}
			else
			{
				parent.Lock();
			}
			m_InitState = -1;
		}
	}

	public class SliderExtensionDirectionToggle(SliderExtensionDirectionToggle.Kind tt) : SliderExtension
	{
		public enum Kind
		{
			_90,
			_270,
			_180,
		}

		private Kind m_ToggleType = tt;
		private bool m_bIsToggled = false;


		public override void OnDraw(UNSlider parent)
		{
			var cellSize = parent.World.GetCellSize();
			var dirVec = m_ToggleType switch
			{
				Kind._90 => m_bIsToggled ? parent.Dir.Rotate90().ToGrid() : parent.Dir.Rotate270().ToGrid(),
				Kind._270 => m_bIsToggled ? parent.Dir.Rotate270().ToGrid() : parent.Dir.Rotate90().ToGrid(),
				Kind._180 => parent.Dir.Invert().ToGrid(),
				_ => throw new NotImplementedException(),
			};
			parent.DrawLine(0.5f*cellSize, 0.5f*cellSize + 0.4f*parent.World.CellWidth*(Vector2)dirVec,
				Godot.Colors.Pink, 5.0f);
		}

		public override void OnItemLeave(UNSlider parent)
		{
		}

		public override void OnRemoval(UNSlider parent) { }
		public override void Reset(UNSlider parent)
		{
			if (m_bIsToggled)
			{
				ToggleParentDir(parent);
			}
		}

		private void ToggleParentDir(UNSlider parent)
		{
			switch (m_ToggleType) {
			case Kind._90:
				if (m_bIsToggled)
				{
					parent.Rotate90();
				}
				else
				{
					parent.Rotate270();
				}
				break;
			
			case Kind._270:
				if (m_bIsToggled)
				{
					parent.Rotate270();
				}
				else
				{
					parent.Rotate90();
				}
				break;
			
			case Kind._180:
				parent.Rotate180();
				break;

			default:
				throw new NotImplementedException();
			}
			
			m_bIsToggled ^= true;
		}

		public override void OnSignal(UNSlider parent)
		{
			ToggleParentDir(parent);
			parent.CancelPendingCmds();
		}
	}

	class SliderExtensionConnector(UNSlider other) : SliderExtension
	{
		private readonly WeakReference<UNSlider> m_Partner = new(other);

		public override void OnDraw(UNSlider parent)
		{
			Debug.AssertNotNull(m_Partner);
			if (m_Partner.TryGetTarget(out var tgt) && GodotObject.IsInstanceValid(tgt))
			{
				var cellSize = parent.World.GetCellSize();
				parent.DrawLine(0.5f*cellSize, tgt!.Position - parent.Position + 0.5f*cellSize, Godot.Colors.Pink);
			}
		}

		public override void OnItemLeave(UNSlider parent)
		{
			if (m_Partner.TryGetTarget(out var tgt) && GodotObject.IsInstanceValid(tgt))
			{
				tgt.m_Ext?.OnSignal(tgt);
			}
		}

		public override void OnRemoval(UNSlider parent)
		{
		}

		public override void OnSignal(UNSlider parent)
		{
		}

		public override void Reset(UNSlider parent)
		{
		}
	}

	public Tile OnlyTile => GetTileNonNull(Vector2I.Zero);

	private SliderExtension? m_Ext;

	public void Setup(WorldPanel world, Vector2I gloc, int workRate, Direction dir)
	{
		BaseInit(world, TickType.Steady, workRate, gloc, Vector2I.One, dir, true);
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		m_Ext?.OnRemoval(this);
	}

	protected override void BuildTilesAsIfFacingEast()
	{
		AddOutput(Vector2I.Zero, Direction.East);
		AddInput(Vector2I.Zero, Direction.North);
		AddInput(Vector2I.Zero, Direction.West);
		AddInput(Vector2I.Zero, Direction.South);
	}

	public override void PendNewCommands(Simulator.PendProxy pender)
	{
		if (HasPendingCmds() && !AllCmdsAreEndingSlidingCmds())
		{
			PauseThisTick();
			return;
		}

		var tl = GetTile(Vector2I.Zero);
		// We have to do it manually like this, this way the slider is synced with the clock at all times.
		// I could also just add a third type, but I learned the hard way that simple is always the way to go.
		if (tl.HasItem())
		{
			pender.Pend([ [new CmdSlide(tl, Dir)] ]);
			m_Ext?.OnItemLeave(this);
		}
	}

	public override bool CanFaceDir(Direction dir) => true;
	public bool IsKey() => m_Ext is SliderExtensionConnector;
	public bool IsLock() => m_Ext is SliderExtensionRuntimeLock;
	public bool IsToggle() => m_Ext is SliderExtensionDirectionToggle;
	public bool IsNormal() => m_Ext is null;

	public override string ToString()
		=> Utilz.ReplaceBaseNameInToString(base.ToString(), "Slider");

	public override void Reset()
	{
		base.Reset();
		m_Ext?.Reset(this);
	}

	public override void _Draw()
	{
		base._Draw();
		m_Ext?.OnDraw(this);
	}

	public void MakeKey(UNSlider target)
	{
		Debug.Assert(m_Ext is null);
		m_Ext = new SliderExtensionConnector(target);
		World.QueueRedraw();
	}

	public void InstallRuntimeLock()
	{
		Debug.Assert(m_Ext is null);
		m_Ext = new SliderExtensionRuntimeLock();
		World.QueueRedraw();
	}

	public void InstallDirectionToggle(SliderExtensionDirectionToggle.Kind tt)
	{
		Debug.Assert(m_Ext is null);
		m_Ext = new SliderExtensionDirectionToggle(tt);
		World.QueueRedraw();
	}

	public void RemoveExtension()
	{
		m_Ext?.OnRemoval(this);
		m_Ext = null;
		World.QueueRedraw();
	}
}
