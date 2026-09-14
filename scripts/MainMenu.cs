using Godot;
using System;

namespace ArFactory;

public partial class MainMenu : Control
{
	public Button EditButt;
	public Button PlayButt;

	public override void _Ready()
	{
		EditButt = GetNode<Button>("HBoxContainer/MarginContainer/Panel/CenterContainer/VBoxContainer/EditButt");
		PlayButt = GetNode<Button>("HBoxContainer/MarginContainer/Panel/CenterContainer/VBoxContainer/PlayButt");

		EditButt.Connect(Button.SignalName.Pressed, Callable.From(OnEditButt_Pressed));
		PlayButt.Connect(Button.SignalName.Pressed, Callable.From(OnPlayButt_Pressed));
	}

	private void OnEditButt_Pressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/editor_select_menu.tscn");
	}

	private void OnPlayButt_Pressed()
	{
		Debug.Assert(false, "No play yet");
	}
}