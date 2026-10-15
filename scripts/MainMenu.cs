using Godot;
using System;

namespace ArFactory;

public partial class MainMenu : Godot.Control
{
	public Godot.Button EditButt => m_EditButt!;
	public Godot.Button PlayButt => m_PlayButt!;

	private Godot.Button? m_EditButt;
	private Godot.Button? m_PlayButt;

	public override void _Ready()
	{
		m_EditButt = GetNode<Godot.Button>("HBoxContainer/MarginContainer/Panel/CenterContainer/VBoxContainer/EditButt");
		m_PlayButt = GetNode<Godot.Button>("HBoxContainer/MarginContainer/Panel/CenterContainer/VBoxContainer/PlayButt");

		m_EditButt.Pressed += OnEditButt_Pressed;
		m_PlayButt.Pressed += OnPlayButt_Pressed;
	}

	private void OnEditButt_Pressed()
	{
		SceneManager.MoveToEditorSelectMenu(GetTree());
	}

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(false, "No play yet");
	}
}