using Godot;
using System;

namespace ArFactory;

public partial class Cam : Camera2D
{
	public const float DefaultZoom = 0.7f;
	public const MouseButton DragButt = MouseButton.Middle;
	
	public float MoveSpeed = 1000.0f;
	public float ZoomStep = 0.1f;
	public float MinZoom = 0.5f;
	public float MaxZoom = 3.0f;

	private bool m_bMouseLanded = false;
	private Vector2 m_DragOffset = Vector2.Zero;


	public override void _Ready()
	{
		Zoom = DefaultZoom * Vector2.One;
	}

	public override void _Process(double dt)
	{
		var dx = Input.GetVector("cam_left", "cam_right", "cam_up", "cam_down");
		Position += dx / Zoom.X * MoveSpeed * (float)dt;
	
		if (m_bMouseLanded)
		{
			Position = m_DragOffset - GetLocalMousePosition();
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mev && mev.ButtonIndex == DragButt)
		{
			if (mev.IsPressed())
			{
				m_bMouseLanded = true;
				m_DragOffset = Position + GetLocalMousePosition();
			}
			else if (mev.IsReleased())
			{
				m_bMouseLanded = false;
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mev && mev.IsPressed())
		{
			Vector2 newZoom = Zoom;
			if (mev.ButtonIndex == MouseButton.WheelDown)
			{
				newZoom -= ZoomStep * Vector2.One;
			}
			else if (mev.ButtonIndex == MouseButton.WheelUp)
			{
				newZoom += ZoomStep * Vector2.One;
			}

			Zoom = newZoom.Clamp(MinZoom, MaxZoom);
		}
	}
}
